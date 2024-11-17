using HarmonyLib;
using SE;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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
                    SEModLoader.log.LogInfo("Modifying string : " + value);
                    value = SEModLoader.modifiedstrings[key];
                    SEModLoader.log.LogInfo("New string value : " + value);
                }
                else
                {

                        if (SEModLoader.modifiedstrings.ContainsValue(__instance.scene)) //(Language, Name), directory
                        {
                            var dictionary = new Dictionary<string, string>();
                            foreach (var x in __instance.strings)
                            {
                                SEModLoader.log.LogInfo(x.key + " : " + x.value);
                                dictionary.Add(x.key, x.value);
                            }
                            SEModLoader.log.LogInfo("Populating new Scene ! " + " : " + __instance.scene);
                        foreach (var s in SEModLoader.modifiedstrings)
                        {
                            if (!dictionary.ContainsKey(s.Key))
                            {
                                LanguageString languageString = new LanguageString();
                                languageString.Add(s.Key, s.Value);
                                if (!__instance.strings.Contains(languageString))
                                {
                                    SEModLoader.log.LogInfo("Added new string : " + s.Key + " // " + s.Value);

                                    __instance.strings.Add(languageString);
                                }
                            }
                            
                        }
                    }
                }
            }
        }     
    }
}
