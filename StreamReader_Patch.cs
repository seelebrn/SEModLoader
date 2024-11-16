
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace SEModLoader
{

    internal class StreamReader_Patch
    {
        public class CustomStreamReader : StreamReader
        {
            public CustomStreamReader(string path) : base(path)
            {
                SEModLoader.log.LogInfo($"CustomStreamReader initialized. Path used: {path}");
            }

            public static StreamReader CreateInstance(string path)
            {
                SEModLoader.log.LogInfo($"Creating CustomStreamReader. Original path: {path}");

                var newPath = ModifyPath(path);
                SEModLoader.log.LogInfo($"Modified path: {newPath}");

                if (File.Exists(newPath))
                {
                    SEModLoader.log.LogInfo($"File found at modified path: {newPath}");
                    return new CustomStreamReader(newPath);
                }
                else
                {
                    SEModLoader.log.LogError($"File not found at modified path: {newPath}. Using original path.");
                    return new CustomStreamReader(path);
                }
            }
        }


        [HarmonyPatch]
        static class StreamReaderPatch
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                foreach (var constructor in typeof(StreamReader).GetConstructors())
                {
                    var parameters = constructor.GetParameters();
                    if (parameters.Any(param => param.ParameterType == typeof(string)))
                    {
                        yield return constructor;
                    }
                }
            }


        }
        [HarmonyPatch(typeof(StreamReader))]
        [HarmonyPatch(MethodType.Constructor)]
        [HarmonyPatch(new Type[] { typeof(string) })]
        static class SE_StreamReaderPatch
        {


            static void Postfix(StreamReader __instance, string path)
            {

                // Standard .json logic
                if (path.Contains("Summa Expeditionis_Data") && path.Contains(".json") && !path.Contains(".meta") && !path.Contains("Campaigns"))
                {
                    SEModLoader.log.LogInfo("Standard JSON Detected!");

                    foreach (var moddedres in SEModLoader.moddedresources.Values)
                    {
                        if (path.Contains(moddedres))
                        {
                            var key = SEModLoader.moddedresources.FirstOrDefault(x => x.Value == moddedres).Key;
                            SEModLoader.log.LogInfo($"Found matching modded resource for {path}: {key}");
                            if (File.Exists(key))
                            {
                                var moddedjson = Helpers.JsonHandler(path, key);

                                // Replace StreamReader with modified JSON
                                var jsonBytes = Encoding.UTF8.GetBytes(moddedjson);
                                var memoryStream = new MemoryStream(jsonBytes);
                                SEModLoader.substitution[__instance] = new StreamReader(memoryStream).ReadToEnd();
                                SEModLoader.log.LogInfo("Standard JSON replaced with modded version.");
                            }
                        }
                    }


                }
                if (path.Contains(Application.dataPath) && path.Contains(".json") && path.Contains("Campaigns") && !path.Contains("cantabriae"))
                {
                    SEModLoader.log.LogInfo($"Intercepted StreamReader constructor. Original path: {path}");

                    var newPath = ModifyPath(path);
                    SEModLoader.log.LogInfo($"Modified path: {newPath}");

                    if (File.Exists(newPath))
                    {
                        __instance = new StreamReader(newPath);
                        SEModLoader.log.LogInfo("Replaced StreamReader successfully.");

                    }
                    else
                    {
                        SEModLoader.log.LogError($"File not found at modified path: {newPath}. Using original constructor.");

                    }
                }
            }
        }

        [HarmonyPatch(typeof(StreamReader), "ReadToEnd")]
        static class ReadToEnd_Patch
        {
            static void Postfix(StreamReader __instance, ref string __result)
            {
                if (SEModLoader.substitution.ContainsKey(__instance))
                {
                    SEModLoader.log.LogInfo("ReadToEnd() result modified!");
                    __result = SEModLoader.substitution[__instance];
                }
            }
        }
        private static string ModifyPath(string path)
        {
            var listPaths = new List<string>();


            foreach (var p in SEModLoader.mods)
            {
                listPaths.Add(p.Key);
            }
            foreach (var key in listPaths)
            {


                var moddedCampaignDir = Path.Combine(key, "Campaigns");
                SEModLoader.log.LogInfo("Now processing Campaign Mod... " + key);
                SEModLoader.log.LogInfo("Looking for subfolder ... " + moddedCampaignDir);
                if (Directory.Exists(moddedCampaignDir))
                {

                    string[] dir2 = Directory.GetDirectories(moddedCampaignDir);
                    if (Directory.Exists(moddedCampaignDir))
                    {
                        string[] directories2 = Directory.GetDirectories(moddedCampaignDir);
                        for (int i = 0; i < directories2.Length; i++)
                        {
                            SEModLoader.log.LogInfo("Now processing Campaign file... " + directories2[i]);
                            string fileName = Path.GetFileName(directories2[i]);
                            SEModLoader.log.LogInfo("Now processing subfile... " + fileName);
                            if (File.Exists(Path.Combine(directories2[i], "config.json")))
                            {
                                SEModLoader.log.LogInfo("Found file for modded campaign ! : " + Path.Combine(directories2[i], "config.json"));
                                string directoryPath = Path.GetDirectoryName(Path.Combine(directories2[i], "config.json"));
                                string lastDirectory = Path.GetFileName(directoryPath);
                                SEModLoader.log.LogInfo("Trimmed directory name : " + lastDirectory);

                                SEModLoader.log.LogWarning("Transpiler Path : " + path);




                                path = path.Replace(Application.dataPath, key);
                                if (Directory.Exists(path))
                                {
                                    SEModLoader.log.LogInfo($"Modified path: {path}");
                                }
                            }
                        }
                    }
                }





            }


            return path;
        }
    }
}



