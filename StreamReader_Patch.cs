using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEModLoader
{
    internal class StreamReader_Patch
    {
        //Mod .json files, add or modify
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
    }
}
