using HarmonyLib;
using SE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEModLoader
{
    internal class Language_Patch
    {
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
    }
}
