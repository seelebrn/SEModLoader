using BepInEx;                        // Main plugin framework
using HarmonyLib;                      // Library for patching methods at runtime
using Newtonsoft.Json.Linq;            // Library for handling JSON objects
using SE;                              // SE namespace (specific to your game environment)
using SE.Interactable;                 // Contains interactable object classes
using SE.UI;                           // Contains UI-related classes
using System;                          // Basic system functionalities
using System.Collections.Generic;      // Collection utilities like List and Dictionary
using System.IO;                       // File input/output handling
using System.Linq;                     // Linq for data manipulation
using System.Reflection;               // Reflection utilities
using System.Text;                     // String manipulation utilities
using UnityEngine;                     // Unity's main library

namespace SEModLoader
{
    // Define the main plugin with attributes for BepInEx
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class SEModLoader : BaseUnityPlugin
    {
        // List of folder names to look for modded content
        public static List<string> ExposedPaths = new List<string>
        {
            "Animals", "Buildings", "Civilizations", "Craft", "Human", "Items",
            "Formations", "Language", "MeshesTextures"
        };

        // Plugin metadata constants
        public const string pluginGuid = "Cadenza.SE.SEModLoader";
        public const string pluginName = "SE Mod Loader by Cadenza";
        public const string pluginVersion = "0.1";

        // Path to the plugin directory
        public static string pluginspath = BepInEx.Paths.PluginPath.ToString();
        private static Harmony harmony;
        // Dictionary to store various modded resources
        public static Dictionary<string, string> moddedresources = new Dictionary<string, string>();
        public static Dictionary<string, string> moddedicons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, Texture2D> moddediconsTex = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, GameObject> moddedMeshes = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, string> modifiedstrings = new Dictionary<string, string>();

        // Logger instance for logging messages
        public static BepInEx.Logging.ManualLogSource log;

        // Fields to hold JSON modifications
        public static string oldsr = "";
        public static string newsr = "";
        public static Dictionary<StreamReader, string> substitution = new Dictionary<StreamReader, string>();

        // Indicates if modded meshes are ready for use
        public static bool isModdedMeshesReady = false;

        // Method executed when the plugin is loaded
        public void Awake()
        {
            log = Logger;
            Logger.LogInfo("Hello World ! Welcome to Cadenza's SE Mod Loader!");

            // Register modded data and meshes
            RegisterModdedData();
            RegisterModdedMeshes(moddedMeshes);

            // Initialize and apply Harmony patches
            harmony = new Harmony("Cadenza.SE.SEModLoader");
            harmony.PatchAll();
        }

        // Register modded meshes into a dictionary
        static public void RegisterModdedMeshes(Dictionary<string, GameObject> dict)
        {
            // Iterate through directories in the "ModdedContent" folder
            foreach (var folder in Directory.GetDirectories(Path.Combine(pluginspath, "ModdedContent")))
            {
                // Check if the folder matches one of the exposed paths
                foreach (string path in ExposedPaths)
                {
                    if (folder.Contains(path))
                    {
                        // Search for files in subdirectories
                        foreach (var file in Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories))
                        {
                            // Load asset bundles containing "AssetBundle-"
                            if (Path.GetFullPath(file).Contains("AssetBundle-"))
                            {
                                SEModLoader.log.LogInfo("Trying to load Mesh ... : " + file);
                                var assetBundle = UnityEngine.AssetBundle.LoadFromFile(file);

                                // Log error if asset bundle failed to load
                                if (assetBundle == null)
                                {
                                    SEModLoader.log.LogError("Failed to load AssetBundle at " + file);
                                }
                                else
                                {
                                    SEModLoader.log.LogInfo("New Mesh : " + file);

                                    // Log components in the asset bundle
                                    foreach (var component in assetBundle.LoadAllAssets<Component>())
                                    {
                                        SEModLoader.log.LogInfo($"Component in {assetBundle.name} : " + component.GetType().Name);
                                    }

                                    // Add the first GameObject from the asset bundle to the dictionary
                                    if (!dict.ContainsKey(Path.GetFileNameWithoutExtension(file).Split('-')[1]))
                                    {
                                        foreach (var gameObject in assetBundle.LoadAllAssets<GameObject>())
                                        {
                                            gameObject.name = Path.GetFileNameWithoutExtension(file).Split('-')[1];
                                            log.LogInfo("gameObject found : " + gameObject.name);
                                        }

                                        dict.Add(Path.GetFileNameWithoutExtension(file).Split('-')[1], assetBundle.LoadAllAssets<GameObject>().First());
                                        log.LogInfo("GO is not null, you're good to go !");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Register modded data (JSONs, icons, strings)
        private void RegisterModdedData()
        {
            // Register .json files in "ModdedContent" folder
            foreach (var file in Directory.GetFiles(Path.Combine(pluginspath, "ModdedContent")))
            {
                if (Path.GetFullPath(file).Contains(".json"))
                {
                    log.LogInfo("Added new json to moddedresources : " + Path.GetFileName(file) + " // " + Path.GetFullPath(file));
                    moddedresources.Add(Path.GetFullPath(file), Path.GetFileName(file));
                }
            }

            // Register files in subdirectories of "ModdedContent"
            foreach (var folder in Directory.GetDirectories(Path.Combine(pluginspath, "ModdedContent")))
            {
                foreach (string path in ExposedPaths)
                {
                    if (folder.Contains(path))
                    {
                        foreach (var file in Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories))
                        {
                            // Register .json files
                            if (Path.GetFullPath(file).Contains(".json"))
                            {
                                log.LogInfo("Added new json to moddedresources : " + Path.GetFileName(file) + " // " + Path.GetFullPath(file));
                                moddedresources.Add(Path.GetFullPath(file), Path.GetFileName(file));
                            }

                            // Register .png files as icons
                            if (Path.GetFullPath(file).Contains(".png"))
                            {
                                log.LogInfo("Added new icon to moddedicons : " + Path.GetFileNameWithoutExtension(file) + " // " + Path.GetFullPath(file));
                                moddedicons.Add(Path.GetFileNameWithoutExtension(file), Path.GetFullPath(file));
                                var tex = File.ReadAllBytes(Path.GetFullPath(file));
                                var x = new Texture2D(2, 2);
                                x.LoadImage(tex);
                                moddediconsTex.Add(Path.GetFileNameWithoutExtension(file), x);
                            }

                            // Register .csv files as modified strings
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

        }
    }
}
