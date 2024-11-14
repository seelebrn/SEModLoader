using HarmonyLib;
using SE.Interactable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SEModLoader
{
    internal class ItemController_Patch
    {
        //Change mesh for new object
        [HarmonyPatch(typeof(ItemController), "Sync")]
        public static class ItemController_Sync_Patch
        {

            public static void CreateAndRegisterItem(string itemName, ItemType itemType, string modelName, GameObject registrar)
            {
                SEModLoader.log.LogInfo($"Starting CreateAndRegisterItem for '{itemName}' with model '{modelName}'.");

                // Check if the item exists in the registrar
                if (!SEModLoader.moddedMeshes.ContainsKey(itemName))
                {
                    SEModLoader.log.LogError($"Item '{itemName}' not found in registrar. Cannot create item without a GameObject.");
                    return;
                }

                // Retrieve the GameObject containing the mesh and material
                GameObject sourceObject = registrar;
                if (sourceObject == null)
                {
                    SEModLoader.log.LogError($"GameObject for '{itemName}' is null. Cannot create item.");
                    return;
                }

                // Get all MeshFilter components and log them
                SEModLoader.log.LogInfo($"Checking components for '{itemName}' in registrar...");
                MeshFilter[] meshFilters = sourceObject.GetComponentsInChildren<MeshFilter>();

                MeshFilter validMeshFilter = null;
                foreach (var meshFilter in meshFilters)
                {
                    if (meshFilter.sharedMesh != null)
                    {
                        validMeshFilter = meshFilter;
                        SEModLoader.log.LogInfo($"Found valid MeshFilter with mesh '{meshFilter.sharedMesh.name}' on '{itemName}'.");
                        break;
                    }
                    else
                    {
                        SEModLoader.log.LogWarning($"MeshFilter found on '{itemName}' but mesh is null.");
                    }
                }

                if (validMeshFilter == null)
                {
                    SEModLoader.log.LogError($"No valid MeshFilter with a mesh was found on '{itemName}'. Cannot create item without a mesh.");
                    return;
                }

                // Check for MeshRenderer and Material
                MeshRenderer sourceMeshRenderer = sourceObject.GetComponentInChildren<MeshRenderer>();
                if (sourceMeshRenderer == null || sourceMeshRenderer.sharedMaterial == null)
                {
                    SEModLoader.log.LogError($"MeshRenderer or material is missing on '{itemName}'. Cannot create item without a material.");
                    return;
                }

                // Create a new GameObject for the item
                GameObject itemModel = new GameObject(modelName);
                itemModel.transform.position = new Vector3(0, 1, 0); // Position for visibility during testing

                // Add MeshFilter and assign the valid mesh
                MeshFilter itemMeshFilter = itemModel.AddComponent<MeshFilter>();
                itemMeshFilter.sharedMesh = validMeshFilter.sharedMesh;

                // Add MeshRenderer and assign the material from sourceObject
                MeshRenderer itemMeshRenderer = itemModel.AddComponent<MeshRenderer>();
                itemMeshRenderer.sharedMaterial = sourceMeshRenderer.sharedMaterial;

                SEModLoader.log.LogInfo($"Mesh '{validMeshFilter.sharedMesh.name}' and material '{sourceMeshRenderer.sharedMaterial.name}' applied to '{itemName}'.");


                // Check if the shader is available, and apply it to the material
                Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpShader != null)
                {
                    itemMeshRenderer.material = new Material(urpShader);
                    itemMeshRenderer.material.CopyPropertiesFromMaterial(sourceMeshRenderer.sharedMaterial);
                    SEModLoader.log.LogInfo($"Applied Universal Render Pipeline/Lit shader to '{itemName}'.");
                }
                else
                {
                    SEModLoader.log.LogWarning("Universal Render Pipeline/Lit shader not found. Defaulting to the original material.");
                    itemMeshRenderer.sharedMaterial = sourceMeshRenderer.sharedMaterial;
                }

                // Add Item component and configure it
                Item customItem = itemModel.AddComponent<Item>();
                customItem.name = itemName;
                customItem.type = itemType;
                customItem.model = modelName;
                customItem.isReference = true; // Mark as reference to ensure it’s added to `references`

                // Add InteractableObject component
                SEModLoader.log.LogInfo($"Adding InteractableObject component to '{itemName}'...");
                InteractableObject referenceInteractable = Resources.FindObjectsOfTypeAll<InteractableObject>().FirstOrDefault(x => x.name.Contains("plank"));

                itemModel.AddComponent(referenceInteractable);
                // Add Rigidbody component
                //SEModLoader.log.LogInfo($"Adding Rigidbody component to '{itemName}'...");
                Rigidbody referenceRigidBody = Resources.FindObjectsOfTypeAll<UnityEngine.Rigidbody>().FirstOrDefault(x => x.name.Contains("plank"));
                Rigidbody customRigidBody = itemModel.AddComponent<Rigidbody>();
                //Rigidbody customRigidBody = itemModel.AddComponent(referenceRigidBody);


                // Add BoxCollider component, adjusting it to match the mesh bounds
                BoxCollider referenceCollider = Resources.FindObjectsOfTypeAll<UnityEngine.BoxCollider>().FirstOrDefault(x => x.name.Contains("plank"));

                SEModLoader.log.LogInfo($"Adding BoxCollider component to '{itemName}'...");
                BoxCollider customBoxCollider = itemModel.AddComponent(referenceCollider);


                // Register the item in ItemController references
                ItemController.AddReference(customItem);
                SEModLoader.log.LogInfo($"'{itemName}' added to ItemController references successfully.");

                // Activate the GameObject to ensure it’s visible in the scene
                itemModel.SetActive(true);

                SEModLoader.log.LogInfo($"Finished CreateAndRegisterItem for '{itemName}'. Item is correctly set up and visible.");
            }

            static void Postfix(ItemController __instance)
            {
                Dictionary<string, GameObject> registrar = SEModLoader.moddedMeshes;

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

                    // Check if the item already exists in ItemController
                    if (ItemExists(itemName, itemType, modelName))
                    {
                        SEModLoader.log.LogInfo($"Item '{itemName}' with model '{modelName}' already exists in ItemController. Skipping.");
                        continue;
                    }

                    // Register the item (will automatically add required components like Item, InteractableObject, etc.)
                    SEModLoader.log.LogInfo($"Registering item '{itemName}' from moddedMeshes.");
                    SEModLoader.log.LogInfo($"Model name : '{modelName}'");
                    entry.Value.SetActive(true);

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
    }
}
