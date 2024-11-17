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
        [HarmonyPatch(typeof(LanguageScenes), "AddScene")]
        public class LanguageScenes_AddScene
        {



                static void Postfix(LanguageScenes __instance, string sceneString, string directoryPath)
            {
               
                if(sceneString.Contains("campaign-"))
                {
                    SEModLoader.log.LogInfo("LanguageScenes_AddScene __instance.language : " + __instance.language);
                    SEModLoader.log.LogInfo("LanguageScenes_AddScene - SceneString : " + sceneString + " // directoryPath : " + directoryPath);
                        var fieldInfo = typeof(LanguageScenes).GetField("scenes", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
                        var value = (List<LanguageSceneStrings>)fieldInfo;
                        foreach(var x in value)
                        {
                        SEModLoader.log.LogInfo("LanguageScenes_AddScene - x in List<LanguageSceneStrings> with campaign- : " + x.scene);
                        foreach(var s in x.strings) 
                        {
                            SEModLoader.log.LogInfo("LanguageScenes_AddScene - string in List<LanguageSceneStrings>.strings with campaign- : " + "Key : " + s.key + " // Value : " +s.value);
                        }
                        }
              
                }
            }
        }
        [HarmonyPatch(typeof(LanguageScenes), "SetLanguage")]
        static class RepairingMyMistakes_Patch
        {
            static void Postfix(LanguageScenes __instance, ref string languageString)
            {
                if (__instance.language.Contains("campaign") || __instance.language == null) 
                {
                    __instance.language = "en_UK";
                    
                }
               
            }
        }
        [HarmonyPatch(typeof(LanguageSceneStrings), "SetScene")]
        static class Language_SceneStrings_SetScene
        {
            static void Postfix(LanguageSceneStrings __instance, ref string sceneString)
            {
                
                SEModLoader.log.LogInfo("Hello, is there anyone out there ?");
                SEModLoader.log.LogInfo("Language_SceneStrings_SetScene : SceneString : " + sceneString);
            }
        }
        //Add new language strings
        [HarmonyPatch(typeof(LanguageSceneStrings), "AddString")]
        static class SE_Languages_Patch_ModifyValue
        {
            static void Postfix(LanguageSceneStrings __instance, string key, string value)
            {
             bool added = false;

                if (SEModLoader.modifiedstrings.ContainsKey(key))
                {
                    SEModLoader.log.LogInfo("Modifying string : " + value);
                    value = SEModLoader.modifiedstrings[key];
                    SEModLoader.log.LogInfo("New string value : " + value);
                }
                else
                {





                        if (SEModLoader.CampaignScenesDict.ContainsValue(__instance.scene) && added == false) //(Language, Name), directory
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
                            added = true;
                        }
                    }
                }
            }
        }
        /*[HarmonyPatch(typeof(Languages), "GetLanguageScenes")]
        static class Languages_GetLanguageScenes
        {
            static void Postfix(Languages __instance, string languageString) 
            {
                var fieldInfo = typeof(Languages).GetField("languages", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
                var value = (List<LanguageScenes>)fieldInfo;
                foreach (LanguageScenes languageScenes in value)
                {
                    SEModLoader.log.LogInfo("Languages_GetLanguageScenes languageScenes in __instance.languages : " + languageString);
                }
                    SEModLoader.log.LogInfo("Languages_GetLanguageScenes : " + languageString);
            }
            	
        }*/
        [HarmonyPatch(typeof(Language), "Get", new Type[] {typeof(string), typeof(string), typeof(string)})]
        static class Language_Get
        {
            static void Prefix(Language __instance, string language, string scene, string key)
            {
                var x = Language.languages.GetLanguageScenes(language);
                { 
                try
                    {
                        SEModLoader.log.LogInfo("Language_Get GetLanguageScene : " + x.GetScene(scene).scene);
                    }
                    catch
                    {

                    }
                }
                if (scene.Contains("campaign"))
                {

                    SEModLoader.log.LogInfo("Language_Get : " + " language : " + language + " scene : " + scene + " key : " + key);
                }
            }

        }

    }
}
