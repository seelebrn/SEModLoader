using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEngine;
using System.Text.RegularExpressions;
using SE.Food;
using System.Reflection.Emit;
using MalbersAnimations;
using System.Reflection;

namespace SEModLoader
{

    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class SEModLoader : BaseUnityPlugin
    {
        public static List<string> ExposedPaths = new List<string> { "Animals", "Buildings", "Civilizations", "Craft", "Human", "Items", "Formations" };
        public const string pluginGuid = "Cadenza.SE.SEModLoader";
        public const string pluginName = "SE Mod Loader by Cadenza";
        public const string pluginVersion = "0.1";
        public static string pluginspath = BepInEx.Paths.PluginPath.ToString();
        private static Harmony harmony;
        public static Dictionary<string, string> moddedresources = new Dictionary<string, string>();
        public static BepInEx.Logging.ManualLogSource log;
        public static string oldsr = "";
        public static string newsr = "";
        public static Dictionary<StreamReader, string> substitution = new Dictionary<StreamReader, string>();

        public void Awake()
        {
            log = Logger;
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Logger.LogInfo("Hello World ! Welcome to Cadenza's SE Mod Loader!");
            RegisterModdedData();
            harmony = new Harmony("Cadenza.SE.SEModLoader");
            harmony.PatchAll();


        }
        private void RegisterModdedData()
        {
            foreach (var folder in Directory.GetDirectories(Path.Combine(pluginspath, "ModdedContent")))
            {
                foreach (string path in ExposedPaths)
                {
                    if (folder.Contains(path))
                    {
                        foreach (var file in Directory.GetFiles(folder))
                        {
                            if (Path.GetFullPath(file).Contains(".json"))
                            {
                                //log.LogInfo(Path.GetFileName(file) + " // " + Path.GetFullPath(file));
                                moddedresources.Add(Path.GetFullPath(file), Path.GetFileName(file));
                            }
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(StreamReader))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new Type[] { typeof(string) })]
    static class SE_StreamReaderPatch
    {

        static void Postfix(ref string path, ref StreamReader __instance)
        {
            if (path.Contains("Summa Expeditionis_Data") && path.Contains(".json") && !path.Contains(".meta"))
            {
                foreach (var moddedres in SEModLoader.moddedresources.Values)
                {
                    if (path.Contains(moddedres))
                    {
                        var key = SEModLoader.moddedresources.FirstOrDefault(x => x.Value == moddedres).Key;
                        SEModLoader.log.LogInfo("Found matching modded resource for " + path + " : " + key);

                        var moddedjson = Helpers.JsonHandler(path, key);

                        // Use a MemoryStream with the modified JSON content
                        var jsonBytes = Encoding.UTF8.GetBytes(moddedjson);
                        var memoryStream = new MemoryStream(jsonBytes);
                        SEModLoader.substitution.Add(__instance, new StreamReader(memoryStream).ReadToEnd());

                        // Replace __instance with a StreamReader that reads from memory
                        __instance = new StreamReader(memoryStream);

                        // Skip the original constructor
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(StreamReader), "ReadToEnd")]
    static class ReadToEnd_Patch
    {
        static void Postfix(StreamReader __instance, ref string __result)
        {
        if(SEModLoader.substitution.ContainsKey(__instance))
            {
                __result = SEModLoader.substitution[__instance];
                SEModLoader.log.LogInfo("ReadToEnd() result modified !");
            }
        }
    }





    public class Helpers
    {
        static void PopulateDictionary(JToken token, Dictionary<string, string> dict, string prefix = "")
        {
            if (token is JObject jsonObject)
            {
                // Iterate through each property in an object
                foreach (var property in jsonObject.Properties())
                {
                    string key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                    PopulateDictionary(property.Value, dict, key);
                }
            }
            else if (token is JArray jsonArray)
            {
                // Iterate through each element in an array
                for (int i = 0; i < jsonArray.Count; i++)
                {
                    string key = $"{prefix}[{i}]";
                    PopulateDictionary(jsonArray[i], dict, key);
                }
            }
            else
            {
                // Store the value for non-container types
                dict[prefix] = token.ToString();
            }
        }

        static public string JsonHandler(string original, string modded)
        {
            var originalJson = File.ReadAllText(original);
            JObject originalObject = JObject.Parse(originalJson);

            var moddedJson = File.ReadAllText(modded);
            JObject moddedObject = JObject.Parse(moddedJson);

            // Perform the comparison and update
            CompareAndUpdateJson(originalObject, moddedObject);

            // Format the updated JSON
            string formattedJson = originalObject.ToString(Newtonsoft.Json.Formatting.Indented);
            //SEModLoader.log.LogInfo("New text ! " + formattedJson);

            return formattedJson;
        }

        static void CompareAndUpdateJson(JObject original, JObject modded)
        {
            SEModLoader.log.LogInfo("Start comparing and updating values");

            // Iterate over the keys in the modded object
            foreach (var moddedProperty in modded)
            {
                string key = moddedProperty.Key;
                JToken moddedValue = moddedProperty.Value;

                // Check if the key exists in the original JSON
                if (original.ContainsKey(key))
                {
                    JToken originalValue = original[key];

                    // If both values are objects, recurse to compare nested structures
                    if (moddedValue.Type == JTokenType.Object && originalValue.Type == JTokenType.Object)
                    {
                        SEModLoader.log.LogInfo($"Comparing nested object at key: {key}");
                        CompareAndUpdateJson((JObject)originalValue, (JObject)moddedValue);
                    }
                    // If both values are arrays, compare array elements
                    else if (moddedValue.Type == JTokenType.Array && originalValue.Type == JTokenType.Array)
                    {
                        SEModLoader.log.LogInfo($"Comparing arrays at key: {key}");
                        CompareAndUpdateJsonArrays((JArray)originalValue, (JArray)moddedValue, key);
                    }
                    // If values are simple (string, number, etc.), compare and update directly
                    else if (!JToken.DeepEquals(originalValue, moddedValue))
                    {
                        SEModLoader.log.LogInfo($"Updating key {key}: original value = {originalValue}, modded value = {moddedValue}");
                        original[key] = moddedValue;  // Update the value
                    }
                }
                else
                {
                    SEModLoader.log.LogInfo($"Key {key} not found in original JSON.");
                }
            }
        }

        static void CompareAndUpdateJsonArrays(JArray originalArray, JArray moddedArray, string arrayKey)
        {
            SEModLoader.log.LogInfo($"Updating array at key: {arrayKey}");

            // Loop through each item in the modded array
            foreach (var moddedItem in moddedArray)
            {
                // Find a matching item in the original array (assuming `id` is the unique identifier)
                string moddedId = moddedItem["id"]?.ToString();

                if (moddedId != null)
                {
                    var originalItem = originalArray.FirstOrDefault(item => item["id"]?.ToString() == moddedId);

                    if (originalItem != null)
                    {
                        SEModLoader.log.LogInfo($"Found matching item in original array with id: {moddedId}");
                        // Recursively compare and update the properties of this item
                        CompareAndUpdateJson((JObject)originalItem, (JObject)moddedItem);
                    }
                    else
                    {
                        SEModLoader.log.LogInfo($"No matching item with id {moddedId} in original array.");
                        // If no match is found, you could decide whether to add the new item or not
                    }
                }
            }
        }










    }
}
