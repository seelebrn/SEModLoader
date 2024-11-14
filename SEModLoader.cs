using BepInEx;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using SE;
using SE.Interactable;
using SE.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
//using UnityMeshImporter;

namespace SEModLoader
{

    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class SEModLoader : BaseUnityPlugin
    {
        public static List<string> ExposedPaths = new List<string> { "Animals", "Buildings", "Civilizations", "Craft", "Human", "Items", "Formations", "Language", "MeshesTextures" };
        public const string pluginGuid = "Cadenza.SE.SEModLoader";
        public const string pluginName = "SE Mod Loader by Cadenza";
        public const string pluginVersion = "0.1";
        public static string pluginspath = BepInEx.Paths.PluginPath.ToString();
        private static Harmony harmony;
        public static Dictionary<string, string> moddedresources = new Dictionary<string, string>();
        public static Dictionary<string, string> moddedicons = new Dictionary<string, string>((StringComparer.OrdinalIgnoreCase));
        public static Dictionary<string, Texture2D> moddediconsTex = new Dictionary<string, Texture2D>((StringComparer.OrdinalIgnoreCase));
        public static Dictionary<string, GameObject> moddedMeshes = new Dictionary<string, GameObject>((StringComparer.OrdinalIgnoreCase));
        public static Dictionary<string, string> modifiedstrings = new Dictionary<string, string>();
        public static BepInEx.Logging.ManualLogSource log;
        public static string oldsr = "";
        public static string newsr = "";
        public static Dictionary<StreamReader, string> substitution = new Dictionary<StreamReader, string>();
        public static bool isModdedMeshesReady = false;
        public void Awake()
        {

            log = Logger;
            Logger.LogInfo("Hello World ! Welcome to Cadenza's SE Mod Loader!");
            RegisterModdedData();
            RegisterModdedMeshes(moddedMeshes);
            harmony = new Harmony("Cadenza.SE.SEModLoader");
            harmony.PatchAll();


        }
        static public void RegisterModdedMeshes(Dictionary<string, GameObject> dict)
        {

            foreach (var folder in Directory.GetDirectories(Path.Combine(pluginspath, "ModdedContent")))
            {
                foreach (string path in ExposedPaths)
                {
                    if (folder.Contains(path))
                    {
                        foreach (var file in Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories))
                        {
                            if (Path.GetFullPath(file).Contains("AssetBundle-"))
                            {
                                SEModLoader.log.LogInfo("Trying to load Mesh ... : " + file);
                                var assetBundle = UnityEngine.AssetBundle.LoadFromFile(file);

                                if (assetBundle == null)
                                {
                                    SEModLoader.log.LogError("Failed to load AssetBundle at " + file);
                                }
                                else
                                {
                                    SEModLoader.log.LogInfo("New Mesh : " + file);

                                    // Check if the material is null or does not have a shader, and assign a default shader

                                    foreach (var component in assetBundle.LoadAllAssets<Component>())
                                    {
                                        SEModLoader.log.LogInfo($"Component in {assetBundle.name} : " + component.GetType().Name);
                                    }
                                    if (!dict.ContainsKey(Path.GetFileNameWithoutExtension(file).Split('-')[1]))
                                    {
                                        foreach (var gameObject in assetBundle.LoadAllAssets<GameObject>())
                                        {
                                            gameObject.name = Path.GetFileNameWithoutExtension(file).Split('-')[1];

                                            log.LogInfo("gameObject found : " + gameObject.name);
                                            log.LogInfo("ModdedMeshes.Key : " + Path.GetFileNameWithoutExtension(file).Split('-')[1]);
                                            foreach (var component in gameObject.GetComponentsInChildren<Component>())
                                            {
                                                log.LogInfo("gameObject Components : " + component.GetType().Name);
                                            }
                                        }

                                        dict.Add(Path.GetFileNameWithoutExtension(file).Split('-')[1], assetBundle.LoadAllAssets<GameObject>().First());
                                        if (dict[Path.GetFileNameWithoutExtension(file).Split('-')[1]] == null)
                                        {
                                            log.LogInfo("WARNING : GO = NULL ! ");
                                        }
                                        else
                                        {
                                            log.LogInfo("GO is not null, you're good to go !");
                                        }
                                    }
                                    foreach (var x in dict)
                                    {

                                        SEModLoader.log.LogInfo("Added Mesh ! " + " Name : " + x.Key + " and GO : " + x.Value);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        private void RegisterModdedData()
        {


            foreach (var file in Directory.GetFiles(Path.Combine(pluginspath, "ModdedContent")))
            {
                if (Path.GetFullPath(file).Contains(".json"))
                {
                    log.LogInfo("Added new json to moddedresources : " + Path.GetFileName(file) + " // " + Path.GetFullPath(file));
                    moddedresources.Add(Path.GetFullPath(file), Path.GetFileName(file));
                }
            }
            foreach (var folder in Directory.GetDirectories(Path.Combine(pluginspath, "ModdedContent")))
            {
                foreach (string path in ExposedPaths)
                {
                    if (folder.Contains(path))
                    {
                        foreach (var file in Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories))
                        {
                            if (Path.GetFullPath(file).Contains(".json"))
                            {
                                log.LogInfo("Added new json to moddedresources : " + Path.GetFileName(file) + " // " + Path.GetFullPath(file));
                                moddedresources.Add(Path.GetFullPath(file), Path.GetFileName(file));
                            }
                            if (Path.GetFullPath(file).Contains(".png"))
                            {
                                log.LogInfo("Added new icon to moddedicons : " + Path.GetFileNameWithoutExtension(file) + " // " + Path.GetFullPath(file));
                                moddedicons.Add(Path.GetFileNameWithoutExtension(file), Path.GetFullPath(file));
                                var tex = File.ReadAllBytes(Path.GetFullPath(file));
                                var x = new Texture2D(2, 2);
                                x.LoadImage(tex);
                                moddediconsTex.Add(Path.GetFileNameWithoutExtension(file), x);
                            }
                            if (Path.GetFullPath(file).Contains(".csv") && !Path.GetFullPath(file).Contains("Conversation"))
                            {
                                string[] lines = File.ReadAllLines(file);
                                log.LogInfo("Added strings to modifiedstrings from " + Path.GetFullPath(file));
                                foreach (var line in lines)
                                {
                                    string key = line.Split(';')[0];
                                    string value = line.Split(';')[1];
                                    modifiedstrings.Add(key, value);
                                    log.LogInfo("Added strings : " + key + " : " + value);
                                }


                            }
                        }
                    }
                }
            }
            isModdedMeshesReady = true;
        }


    }

}
