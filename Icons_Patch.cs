using HarmonyLib;
using SE.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;
namespace SEModLoader
{
    internal class Icons_Patch
    {
        //Change icons - 3 patches. Used to be one, but I'm debugging stuff, so I need 3. 
        [HarmonyPatch]
        static class SE_Icons_Patch_0
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                yield return typeof(Item).GetMethod("GetIcon");

            }

            static void Postfix(Item __instance, ref Texture2D __result)
            {
                SEModLoader.log.LogInfo("__instance.model : " + __instance.model);
                //Handling icons modifications (for existing objects)

                foreach (var x in SEModLoader.moddedicons) { SEModLoader.log.LogInfo(x.Key); }
                if (SEModLoader.moddediconsTex.ContainsKey(__instance.model))
                {
                    SEModLoader.log.LogInfo("Found new icon in moddedicons dict for " + __instance.model);
                    SEModLoader.log.LogInfo("Path for new icon =  " + SEModLoader.moddedicons[__instance.model]);
                    __result = new Texture2D(2, 2);
                    __result = SEModLoader.moddediconsTex[__instance.model];
                    SEModLoader.log.LogInfo("Loaded Icon ! :  " + SEModLoader.moddedicons[__instance.model]);
                }
            }
        }

        [HarmonyPatch]
        static class SE_Icons_Patch_1
        {
            static IEnumerable<MethodBase> TargetMethods()
            {

                yield return typeof(SE.Buildings.Building).GetMethod("GetIcon");

            }

            static void Postfix(Texture2D __result)
            {
                SEModLoader.log.LogInfo("__result.name : " + __result.name);
                if (SEModLoader.moddedicons.ContainsKey(__result.name))
                {
                    SEModLoader.log.LogInfo("Attempting to replace icon... : " + __result.name);
                    var x = File.ReadAllBytes(SEModLoader.moddedicons[__result.name]);
                    __result.LoadImage(x);
                    SEModLoader.log.LogInfo("Replaced Icon... : " + __result.name);

                }


            }
        }
        [HarmonyPatch]
        static class SE_Icons_Patch_2
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                yield return typeof(UICommon).GetMethod("GetIcon");
            }

            static void Postfix(Texture2D __result)
            {
                SEModLoader.log.LogInfo("__result.name : " + __result.name);
                if (SEModLoader.moddedicons.ContainsKey(__result.name))
                {
                    SEModLoader.log.LogInfo("Attempting to replace icon... : " + __result.name);
                    var x = File.ReadAllBytes(SEModLoader.moddedicons[__result.name]);
                    __result.LoadImage(x);
                    SEModLoader.log.LogInfo("Replaced Icon... : " + __result.name);

                }


            }
        }
    }
}
