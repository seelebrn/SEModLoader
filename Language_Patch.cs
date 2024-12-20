﻿using HarmonyLib;
using SE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace SEModLoader
{
    internal class Language_Patch
    {
        //Modify language strings
        [HarmonyPatch(typeof(Language), "LoadLanguages")]
        static class SE_Language_Patch_LoadLanguages
        {
            static void Postfix(Language __instance)
            {
                string text = UnityEngine.Application.dataPath + "/" + __instance.dirPath;
                foreach (string text2 in Directory.GetDirectories(text))
                {
                    string fileName = Path.GetFileName(text2);
                    Language.languages.AddLanguage(fileName, text2);
                    SEModLoader.log.LogInfo("LoadLanguages - FileName : " + fileName + " // text2 : " + text2);
                }
                foreach (var mod in SEModLoader.mods)
                {
                    text = Path.Combine(mod.Key);

                    foreach (string text2 in Directory.GetDirectories(text))
                    {

                        string fileName = Path.GetFileName(text2);
                        Language.languages.AddLanguage(fileName, text2);
                        SEModLoader.log.LogInfo("LoadLanguages - mod.Key : " + fileName + " // text2 : " + text2);
                    }
                }
                foreach (var scene in SEModLoader.scenesDict)
                {
                    Language.languages.AddLanguage("en_UK", scene.Value);
                    SEModLoader.log.LogInfo("LoadLanguages - scene.Key : " + "en_UK" + " // scene.Value : " + scene.Value);

                }
            }
        }

            [HarmonyPatch(typeof(Languages), "AddLanguage")]
            static class SE_Languages_Patch_AddLanguage
            {
                static void Prefix(Languages __instance, string languageString, string directoryPath)
                {
                    if (languageString == "Language")
                    {
                        languageString = "en_UK";
                    }

                }
            }
        }
    }

