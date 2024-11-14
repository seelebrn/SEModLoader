using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SEModLoader
{
    internal class Helpers
    {
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
        }
    }
}
