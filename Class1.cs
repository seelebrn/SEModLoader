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
        static bool Prefix(ref string path, ref StreamReader __instance)
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

                        var jsonBytes = Encoding.UTF8.GetBytes(moddedjson);
                        var memoryStream = new MemoryStream(jsonBytes);

                        __instance = new StreamReader(memoryStream);

                        return false;
                    }
                }
            }
            return true;
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
            var o = File.ReadAllText(original);
            JObject jsonObject = JObject.Parse(o);

            var m = File.ReadAllText(modded);
            Dictionary<string, string> moddedkeyValuePairs = new Dictionary<string, string>();
            PopulateDictionary(JObject.Parse(m), moddedkeyValuePairs);

            foreach (var kvp in moddedkeyValuePairs)
            {
                UpdateJsonValue(jsonObject, kvp.Key, kvp.Value);
            }

            string formattedJson = jsonObject.ToString(Newtonsoft.Json.Formatting.Indented);
            SEModLoader.log.LogInfo("New text ! " + formattedJson);

            return formattedJson;
        }
        static void UpdateJsonValue(JObject jsonObject, string keyPath, string value)
        {
            string[] keys = keyPath.Split('.');
            JToken current = jsonObject;

            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];

                // Handle array indices if present (e.g., "array[0]")
                if (key.Contains("["))
                {
                    var arrayKey = key.Substring(0, key.IndexOf("["));
                    int index = int.Parse(key.Substring(key.IndexOf("[") + 1, key.IndexOf("]") - key.IndexOf("[") - 1));

                    // Ensure array exists at this key
                    if (current[arrayKey] == null)
                        current[arrayKey] = new JArray();

                    JArray array = (JArray)current[arrayKey];

                    // Expand array to required index if necessary
                    while (array.Count <= index)
                        array.Add(null);

                    if (i == keys.Length - 1)
                    {
                        array[index] = value;
                    }
                    else
                    {
                        if (array[index] == null)
                            array[index] = new JObject();

                        current = array[index];
                    }
                }
                else
                {
                    // Handle nested objects
                    if (i == keys.Length - 1)
                    {
                        current[key] = value;
                    }
                    else
                    {
                        if (current[key] == null)
                            current[key] = new JObject();

                        current = current[key];
                    }
                }
            }

        }
    }
}
