using HarmonyLib;
using SE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEModLoader
{
    internal class LanguageScene_Patch
    {
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
    }
}
