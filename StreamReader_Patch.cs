using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
namespace SEModLoader
{
    [HarmonyPatch(typeof(StreamReader))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new Type[] { typeof(string) })]
    public static class StreamReaderPatch
    {
        public static void Prefix(ref string path)
        {
            if (path.StartsWith(Application.dataPath))
            {
                var AADP = path.Replace(Application.dataPath, "");
                if(path.Contains("Language"))
                {
                    //SEModLoader.log.LogInfo("HFM : We've got a Language file here ! path : " + path);
                }
                var Filename = Path.GetFileName(path);
                //SEModLoader.log.LogInfo("path : " + path);
                //SEModLoader.log.LogInfo("AADP : " + AADP);

                foreach (var x in SEModLoader.mods)
                {

                    //SEModLoader.log.LogInfo("SEModLoader.mods Key : " + x.Key);
                    var newFile = x.Key + "/" + AADP;
                    newFile = newFile.Replace("\\", "/");
                    //SEModLoader.log.LogInfo("Potential New File : " + newFile);

                    if (File.Exists(newFile))
                    {
                        SEModLoader.log.LogInfo("Found Replacement file ! : " + newFile);
                        path = newFile;
                    }



                }
            }
        }
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
}
    
