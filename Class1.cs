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
using SE.UI;
using System.Resources;
using static System.Net.Mime.MediaTypeNames;
using SE;
using SE.Buildings;
using UnityEngine.Windows;
using SE.Player;
using SE.Development.Commands;
using SE.UI.MainMenu;
using MalbersAnimations.Utilities;
using SE.Interactable;
//using UnityMeshImporter;
using UnityEngine.InputSystem;
using SE.World.Resources;
using static Assimp.Metadata;
using MalbersAnimations.Conditions;

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
                            if (Path.GetFullPath(file).Contains(".fbx"))
                            {
                                SEModLoader.log.LogInfo("Trying to load Mesh ... : " + file);
                                var gameObject = UnityMeshImporter.MeshImporter.Load(file, hdrp: false);
                                gameObject.name = Path.GetFileNameWithoutExtension(file);

                                if (gameObject == null)
                                {
                                    SEModLoader.log.LogError("Failed to load AssetBundle at " + file);
                                }
                                else
                                {
                                    SEModLoader.log.LogInfo("New Mesh : " + file);

                                    // Check if the material is null or does not have a shader, and assign a default shader

                                    foreach (var component in gameObject.GetComponentsInChildren<Component>())
                                    {
                                        SEModLoader.log.LogInfo($"Component in {gameObject.name} : " + component.GetType().Name);
                                    }
                                    if (!dict.ContainsKey(Path.GetFileNameWithoutExtension(file)))
                                    {
                                        dict.Add(Path.GetFileNameWithoutExtension(file), gameObject);
                                        if (dict[Path.GetFileNameWithoutExtension(file)] == null)
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
    //Necessary patch, idk why
    [HarmonyPatch(typeof(StreamReader), "ReadToEnd")]
    static class ReadToEnd_Patch
    {
        static void Postfix(StreamReader __instance, ref string __result)
        {
            if (SEModLoader.substitution.ContainsKey(__instance))
            {
                SEModLoader.log.LogInfo("ReadToEnd() result modified !");
                __result = SEModLoader.substitution[__instance];
                SEModLoader.log.LogInfo("ReadToEnd() result modified !");
            }
        }
    }
    //Change icons - 3 patches. Used to be one, but I'm debugging stuff, so I need 3. 
    [HarmonyPatch]
    static class SE_Icons_Patch_0
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return typeof(Item).GetMethod("GetIcon");

        }

        static void Postfix(Item __instance, ref Texture2D __result)
        {
            SEModLoader.log.LogInfo("__instance.model : " + __instance.model);
            //Handling icons modifications (for existing objects)

            foreach (var x in SEModLoader.moddedicons) { SEModLoader.log.LogInfo(x.Key); }
            if (SEModLoader.moddediconsTex.ContainsKey(__instance.model))
            {
                SEModLoader.log.LogInfo("Found new icon in moddedicons dict for " + __instance.model);
                SEModLoader.log.LogInfo("Path for new icon =  " + SEModLoader.moddedicons[__instance.model]);
                __result = new Texture2D(2, 2);
                __result = SEModLoader.moddediconsTex[__instance.model];
                SEModLoader.log.LogInfo("Loaded Icon ! :  " + SEModLoader.moddedicons[__instance.model]);
            }
        }
    }

    [HarmonyPatch]
    static class SE_Icons_Patch_1
    {
        static IEnumerable<MethodBase> TargetMethods()
        {

            yield return typeof(SE.Buildings.Building).GetMethod("GetIcon");

        }

        static void Postfix(Texture2D __result)
        {
            SEModLoader.log.LogInfo("__result.name : " + __result.name);
            if (SEModLoader.moddedicons.ContainsKey(__result.name))
            {
                SEModLoader.log.LogInfo("Attempting to replace icon... : " + __result.name);
                var x = File.ReadAllBytes(SEModLoader.moddedicons[__result.name]);
                __result.LoadImage(x);
                SEModLoader.log.LogInfo("Replaced Icon... : " + __result.name);

            }


        }
    }
    [HarmonyPatch]
    static class SE_Icons_Patch_2
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return typeof(UICommon).GetMethod("GetIcon");
        }

        static void Postfix(Texture2D __result)
        {
            SEModLoader.log.LogInfo("__result.name : " + __result.name);
            if (SEModLoader.moddedicons.ContainsKey(__result.name))
            {
                SEModLoader.log.LogInfo("Attempting to replace icon... : " + __result.name);
                var x = File.ReadAllBytes(SEModLoader.moddedicons[__result.name]);
                __result.LoadImage(x);
                SEModLoader.log.LogInfo("Replaced Icon... : " + __result.name);

            }


        }
    }
    //Modify language strings
    [HarmonyPatch(typeof(Language), "LoadLanguages")]
    static class SE_Languages_Patch_AddNewValue
    {
        static void Postfix(Language __instance)
        {
            string text = UnityEngine.Application.dataPath + "/" + __instance.dirPath;
            foreach (string text2 in Directory.GetDirectories(text))
            {
                string fileName = Path.GetFileName(text2);
                Language.languages.AddLanguage(fileName, text2);
            }

            text = Path.Combine(BepInEx.Paths.PluginPath, "ModdedContent", "Language");

            foreach (string text2 in Directory.GetDirectories(text))
            {
                string fileName = Path.GetFileName(text2);
                Language.languages.AddLanguage(fileName, text2);
                SEModLoader.log.LogInfo("FileName : " + fileName + " // text2 : " + text2);
            }




        }
    }
    //Add new language strings
    [HarmonyPatch(typeof(LanguageSceneStrings), "AddString")]
    static class SE_Languages_Patch_ModifyValue
    {
        static void Postfix(LanguageSceneStrings __instance, string key, string value)
        {
            if (SEModLoader.modifiedstrings.ContainsKey(key))
            {
                value = SEModLoader.modifiedstrings[key];
            }
        }
    }

    //Change mesh for new object
    [HarmonyPatch(typeof(ItemController), "Sync")]
    public static class ItemController_Sync_Patch
    {
        static void CreateAndRegisterItem(string itemName, ItemType itemType, string modelName, GameObject itemModel)
        {



            // Retrieve the GameObject from moddedMeshes if it exists
            /*if (SEModLoader.moddedMeshes.TryGetValue(itemName, out itemModel))
            {
                itemModel = SEModLoader.moddedMeshes[itemName];
                if (itemModel != SEModLoader.moddedMeshes[itemName])
                {
                    SEModLoader.log.LogInfo($"Reusing existing GameObject '{itemName}' from moddedMeshes.");
                    itemModel.SetActive(true);

                    // Log all entries in moddedMeshes, handling any null values
                    foreach (var entry in itemModel.GetComponents<Component>())
                    {
                        SEModLoader.log.LogInfo("Component Name : " + entry.name + " // Component Type : " + entry.GetType().Name);
                    }
                }
            }*/
            /*else
            {
                // If the GameObject is not in moddedMeshes, create a new one
                itemModel = new GameObject(modelName);

                SEModLoader.log.LogInfo($"Created new GameObject '{modelName}' because it was not found in moddedMeshes.");

            }*/

            // Check if the Item component exists, and log if it doesn't
            SEModLoader.log.LogWarning($"Looking for an Item Component for '{itemName}'");
            Item customItem = itemModel.AddComponent<Item>();
            SEModLoader.log.LogInfo($"Item component added on '{itemName}'.");

            customItem.name = itemName;
            customItem.type = itemType;
            customItem.model = modelName;
            customItem.isReference = true;

            // Check and add the other required components with logging
            InteractableObject customInteractableObject = itemModel.AddComponent<InteractableObject>();

            customInteractableObject = itemModel.AddComponent<InteractableObject>();
            SEModLoader.log.LogWarning($"InteractableObject component added on '{itemName}'");

            Rigidbody customRigidBody = itemModel.GetComponentInChildren<Rigidbody>();
            customRigidBody = itemModel.AddComponent<Rigidbody>();
            SEModLoader.log.LogWarning($"Rigidbody component added on '{itemName}'");


            BoxCollider customBoxCollider = itemModel.GetComponentInChildren<BoxCollider>();

            customBoxCollider = itemModel.AddComponent<BoxCollider>();
            SEModLoader.log.LogWarning($"BoxCollider component added on '{itemName}'");

            // Add the item to references
            ItemController.AddReference(customItem);

            // Check if the moddedMeshes GameObject has necessary components for mesh and material
            MeshFilter[] customMeshFilter = itemModel.GetComponentsInChildren<MeshFilter>();
            if (customMeshFilter == null)
            {
                SEModLoader.log.LogInfo($"MeshFilter is missing on '{itemName}' GameObject.");
                itemModel.AddComponent<MeshFilter>();
                SEModLoader.log.LogInfo($"Added MeshFilter component on '{itemName}' GameObject.");
            }
            else
            {
                SEModLoader.log.LogInfo($"MeshFilter found on '{itemName}' GameObject.");
                MeshFilter meshFilter = itemModel.GetComponentInChildren<MeshFilter>();
                meshFilter.mesh = customMeshFilter.First().mesh;
            }

            MeshRenderer[] customMeshRenderer = itemModel.GetComponentsInChildren<MeshRenderer>();
            if (customMeshRenderer == null)
            {
                SEModLoader.log.LogInfo($"MeshRenderer is missing on '{itemName}' GameObject.");
                itemModel.AddComponent<MeshRenderer>();
                SEModLoader.log.LogInfo($"Added MeshRenderer component on '{itemName}' GameObject.");
            }
            else
            {
                MeshRenderer meshRenderer = itemModel.GetComponentInChildren<MeshRenderer>();
                meshRenderer.material = customMeshRenderer.First().material;
                SEModLoader.log.LogInfo($"MeshRenderer found on '{itemName}' GameObject.");
            }

            SEModLoader.log.LogInfo($"Mesh and material applied to '{itemName}'.");
        }





        static void Postfix(ItemController __instance)
        {
            Dictionary<string, GameObject> registrar = new Dictionary<string, GameObject>();
        // Ensure that all items from moddedMeshes are added to the ItemController
        SEModLoader.log.LogInfo("Filling Registrar");
            SEModLoader.RegisterModdedMeshes(registrar);
            SEModLoader.log.LogInfo("Registrar Filled !");
            foreach (var entry in registrar)
            {
                SEModLoader.log.LogInfo("Entries : " + entry.Key + " // " + entry.Value.name);
                string itemName = entry.Key;
                GameObject itemModel = entry.Value;
                itemModel.name = entry.Key;
                // Define the item properties (modify this as necessary based on your requirements)
                SEModLoader.log.LogInfo($"Setting item properties : modelName && itemModel for : '{itemName}'.");
                ItemType itemType = ItemType.food;  // Adjust this based on your logic
                string modelName = itemModel != null ? itemModel.name : "DefaultModelName";

                // Create and register the item (will automatically add required components like Item, InteractableObject, etc.)
                SEModLoader.log.LogInfo($"Registering item '{itemName}' from moddedMeshes.");
                SEModLoader.log.LogInfo($"Model name : '{itemName}'");
                entry.Value.SetActive(true);
                foreach (var x in entry.Value.GetComponentsInChildren<Component>(true))
                {
                    SEModLoader.log.LogInfo($"Components existing in '{itemName}' : " + x.name + " // " + x.GetType().Name);
                }
                CreateAndRegisterItem(itemName, itemType, modelName, entry.Value);

                // Use reflection to access the private fields and methods in ItemController (if necessary)
                var instance = ItemController.instance;
                var elementsField = typeof(ItemController).GetField("elements", BindingFlags.Instance | BindingFlags.NonPublic);
                var configDataField = typeof(ItemController).GetField("configData", BindingFlags.Instance | BindingFlags.NonPublic);
                var writeDataMethod = typeof(ItemController).GetMethod("WriteData", BindingFlags.Instance | BindingFlags.NonPublic);

                if (elementsField == null || configDataField == null || writeDataMethod == null)
                {
                    SEModLoader.log.LogError("Reflection failed: Could not find required private fields or methods in ItemController.");
                    return;
                }

                // Access the elements and configData
                var elements = (Dictionary<string, ItemConfigElement>)elementsField.GetValue(instance);
                var configData = (ItemConfig)configDataField.GetValue(instance);

                // Add missing item from moddedMeshes to elements
                string key = itemType.ToString() + "*-*" + modelName;
                if (!elements.ContainsKey(key))
                {
                    // Create and add the new ItemConfigElement if it doesn't exist
                    ItemConfigElement newItemConfig = new ItemConfigElement
                    {
                        type = itemType,
                        model = modelName
                    };

                    // Add to the dictionary
                    elements[key] = newItemConfig;

                    SEModLoader.log.LogInfo($"Registered new item: {itemName} from moddedMeshes.");
                }
                else
                {
                    SEModLoader.log.LogInfo($"Item '{itemName}' already exists in elements. Skipping.");
                }

                // Update the configData list from the elements dictionary
                ItemConfigElement[] array = new ItemConfigElement[elements.Count];
                int num = 0;
                foreach (KeyValuePair<string, ItemConfigElement> keyValuePair in elements)
                {
                    array[num] = keyValuePair.Value;
                    num++;
                }
                configData.list = array;

                // Use reflection to call WriteData()
                writeDataMethod.Invoke(instance, null);

                SEModLoader.log.LogInfo("Finished updating and writing data.");
            }
        }



        // Helper function to check if the item already exists
        private static bool ItemExists(string itemName, ItemType itemType, string modelName)
        {
            var referencesField = typeof(ItemController).GetField("references", BindingFlags.Instance | BindingFlags.NonPublic);
            var references = (Dictionary<ItemType, Dictionary<string, List<GameObject>>>)referencesField.GetValue(ItemController.instance);


            // Check if the specified type and model already exist in the references
            return references.TryGetValue(itemType, out var typeDictionary) && typeDictionary.ContainsKey(modelName);

        }
    }

    //Update Mesh Texture

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

        public static string JsonHandler(string original, string modded)
        {
            var originalJson = File.ReadAllText(original);
            JObject originalObject = JObject.Parse(originalJson);

            var moddedJson = File.ReadAllText(modded);
            JObject moddedObject = JObject.Parse(moddedJson);

            // Perform the comparison and update
            CompareAndUpdateJson(originalObject, moddedObject);

            // Format the updated JSON
            string formattedJson = originalObject.ToString(Newtonsoft.Json.Formatting.Indented);
            //SEModLoader.log.LogInfo("Newtext ! : " + formattedJson);
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
                    // Key doesn't exist in the original JSON, so add it
                    SEModLoader.log.LogInfo($"Adding new key {key} to original JSON.");
                    original[key] = moddedValue;
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
                        SEModLoader.log.LogInfo($"No matching item with id {moddedId} in original array. Adding new item.");
                        // If no match is found, add the new item to the original array
                        originalArray.Add(moddedItem);
                        var obj = new GameObject();

                    }
                }
            }
        }







        /*    static void CreateAndRegisterItem(string itemName, ItemType itemType, string modelName)
    {
        // Create the GameObject and Item component
        GameObject itemModel = new GameObject(modelName);
        Item customItem = itemModel.AddComponent<Item>();
        InteractableObject customInteractableObject = itemModel.AddComponent<InteractableObject>();
        Rigidbody customRigidBody = itemModel.AddComponent<Rigidbody>();
        BoxCollider customBoxCollider = itemModel.AddComponent<BoxCollider>();
        customItem.name = itemName;
        customItem.type = itemType;
        customItem.model = modelName;
        customItem.isReference = true;

        // Add the item to references
        ItemController.AddReference(customItem);

        // Load and apply the mesh
        Mesh newMesh = SEModLoader.moddedMeshes.Where(x => x.Key == customItem.name).First().Value.Key;
        Material newMaterial = SEModLoader.moddedMeshes.Where(x => x.Key == customItem.name).First().Value.Value;
        MeshRenderer referenceRenderer = Resources.FindObjectsOfTypeAll<MeshRenderer>().FirstOrDefault(x => x.name.Contains("plank"));
        InteractableObject referenceInteractable = Resources.FindObjectsOfTypeAll<InteractableObject>().FirstOrDefault(x => x.name.Contains("plank"));
        Rigidbody referenceRigidBody = Resources.FindObjectsOfTypeAll<UnityEngine.Rigidbody>().FirstOrDefault(x => x.name.Contains("plank"));
        BoxCollider referenceCollider = Resources.FindObjectsOfTypeAll<UnityEngine.BoxCollider>().FirstOrDefault(x => x.name.Contains("plank"));
        if (referenceRenderer == null)
        {
            SEModLoader.log.LogWarning("MeshRenderer for 'plank' not found!");
        }

        if (newMesh != null)
        {
            SEModLoader.log.LogInfo($"Mesh {newMesh.name} successfully loaded.");

            // Ensure MeshFilter is present and assign the mesh
            MeshFilter meshFilter = itemModel.GetComponent<MeshFilter>() ?? itemModel.AddComponent<MeshFilter>();
            meshFilter.mesh = newMesh;

            // Ensure MeshRenderer is present and copy material if available
            MeshRenderer meshRenderer = itemModel.GetComponent<MeshRenderer>() ?? itemModel.AddComponent<MeshRenderer>();
            if (referenceRenderer != null && referenceRenderer.material != null)
            {
                meshRenderer.material = newMaterial;
                SEModLoader.log.LogInfo("Material from 'plank' applied to custom item model.");
            }
            if (referenceInteractable != null)
            {
                customInteractableObject = referenceInteractable;
            }
            if (referenceRigidBody != null)
            {
                customRigidBody = referenceRigidBody;
            }
            if (referenceCollider != null)
            {
                customBoxCollider = referenceCollider;
            }
            if (referenceCollider != null)
            {
                customBoxCollider = referenceCollider;
            }
            else
            {
                SEModLoader.log.LogWarning("Failed to apply material from 'plank'.");
            }

            SEModLoader.log.LogInfo("Mesh 'bucellatum' applied to custom item model.");
        }
        else
        {
            SEModLoader.log.LogWarning("Failed to load mesh 'bucellatum'.");
        }
    }*/




    }
}
