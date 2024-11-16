using SE.World;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using BepInEx;
using SE.UI;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using SE;

namespace SEModLoader
{
    //Doesn't work, need to talk to @Lobico
    internal class Campaign_Patch
    {
        [HarmonyPatch(typeof(Campaign), "Load")]
        static class Campaign_Load_Patch
        {
            static void Postfix(Campaign __instance, ref string name)
            {
                //SEModLoader.log.LogInfo("Campaign_Load_Patch_Name : " + __instance.config.id);
            }
        }
        [HarmonyPatch(typeof(Biome), "Load")]
        static class Biome_Load_Patch
        {
            static bool Prefix(Biome __instance, string campaign, string id)
            {
                //SEModLoader.log.LogWarning("Pre Biome_Load_Patch_Campaign : " + campaign + " // Biome_Load_Patch_id : " + id);
                var listPaths = new List<string>();
                foreach (var path in SEModLoader.mods)
                {
                    listPaths.Add(path.Key);
                }
                foreach (var path in SEModLoader.mods.Keys)
                {
                    //SEModLoader.log.LogInfo("SEModLoader.mods.key : " + path);
                    //SEModLoader.log.LogInfo("campaign : " + campaign);
                    if (path.Contains(campaign))
                    {
                        

                        var moddedCampaignDir = Path.Combine(path, "Campaigns");
                        string[] dir2 = Directory.GetDirectories(moddedCampaignDir);
                        if (Directory.Exists(moddedCampaignDir))
                        {
                            //SEModLoader.log.LogInfo("Biome_Load_Patch_Campaign moddedCampaignDir exists : " + moddedCampaignDir);
                            string[] directories2 = Directory.GetDirectories(moddedCampaignDir);
                            for (int i = 0; i < directories2.Length; i++)
                            {
                                string fileName = Path.GetFileName(directories2[i]);
                                if (File.Exists(Path.Combine(directories2[i], "config.json")))
                                {
                                    //SEModLoader.log.LogInfo("Biome_Load_Patch_Campaign Path.Combine(directories2[i], \"config.json\") exists");

                                    string directoryPath = Path.GetDirectoryName(Path.Combine(directories2[i], "config.json"));
                                    string lastDirectory = Path.GetFileName(directoryPath);
                                    //SEModLoader.log.LogWarning("Biome_LastDirectory : " + lastDirectory);
                                    if (campaign == lastDirectory)
                                    {

                                        var split = id.Split('-');
                                        //SEModLoader.log.LogWarning("Biome_Load_Patch_Campaign - modifying : " + split[0] + " // Changing to : " + campaign);
                                        if (split[0] != campaign)
                                        {
                                            id = id.Replace(split[0], campaign);
                                            //SEModLoader.log.LogWarning("Post Biome_Load_Patch_Campaign : " + campaign + " // Biome_Load_Patch_id : " + id);
                                            return false;
                                        }
                                    }
                                }
                            }

                        }
                    }
                }

                return true;


            }
        }
        [HarmonyPatch(typeof(StartMenu), "LoadCampaigns")]
        public static class StartMenu_LoadCampaignsPatch
        {
            public static void LogChildTransforms(Transform parentTransform)
            {
                SEModLoader.log.LogInfo($"Inspecting children of {parentTransform.name}:");
                foreach (Transform child in parentTransform)
                {
                    SEModLoader.log.LogInfo($"Child name: {child.name}, Active: {child.gameObject.activeInHierarchy}");
                }
            }
            static void Postfix(StartMenu __instance)
            {


                // Use reflection to get the campaignBannerRef and campaignMap fields
                FieldInfo bannerRefField = __instance.GetType().GetField("campaignBannerRef", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                FieldInfo mapField = __instance.GetType().GetField("campaignMap", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                FieldInfo campaignField = __instance.GetType().GetField("campaign", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (bannerRefField == null || mapField == null)
                {
                    SEModLoader.log.LogError("Could not access required fields (campaignBannerRef or campaignMap) in the target class.");

                }

                // Get the values of campaignBannerRef and campaignMap
                GameObject campaignBannerRef = (GameObject)bannerRefField.GetValue(__instance);
                GameObject campaignMap = (GameObject)mapField.GetValue(__instance);

                var listPaths = new List<string>();
                foreach (var path in SEModLoader.mods)
                {
                    listPaths.Add(path.Key);
                }



                /*string text = Application.dataPath + "/Campaigns";
                if (!Directory.Exists(text))
                {
                    Directory.CreateDirectory(text);
                }
                foreach (object obj in campaignMap.transform)
                {
                    UnityEngine.Object.Destroy(((Transform)obj).gameObject);
                }
                string[] directories = Directory.GetDirectories(text);
                for (int i = 0; i < directories.Length; i++)
                {
                    string fileName = Path.GetFileName(directories[i]);
                    if (File.Exists(fileName))
                    {
                        Campaign campaign = new Campaign(fileName);
                        SEModLoader.log.LogInfo("New Campaign Initialized ! " + campaign.config.id);
                        GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(campaignBannerRef);
                        gameObject.name = fileName;
                        gameObject.transform.parent = campaignMap.transform;
                        gameObject.transform.localRotation = Quaternion.Euler(campaign.config.banner.rotation);
                        gameObject.transform.localPosition = campaign.config.banner.position;
                        InteractableMouse interactableMouse = gameObject.AddComponent<InteractableMouse>();
                        if (campaign == null)
                        {
                            MethodInfo selectCampaignMethod = __instance.GetType().GetMethod("SelectCampaign", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                            if (selectCampaignMethod != null)
                            {
                                selectCampaignMethod.Invoke(__instance, new object[] { interactableMouse });
                                interactableMouse.Active(false);
                            }
                            else
                            {
                                SEModLoader.log.LogError("Could not find SelectCampaign method on the target class.");
                            }
                            interactableMouse.Active(false);
                        }
                    }
                }*/

                foreach (var file in listPaths)
                {

                    var moddedCampaignDir = Path.Combine(file, "Campaigns");
                    SEModLoader.log.LogInfo("Now processing Campaign Mod... " + file);
                    //SEModLoader.log.LogInfo("Looking for subfolder ... " + moddedCampaignDir);
                    if (Directory.Exists(moddedCampaignDir))
                    {

                        string[] dir2 = Directory.GetDirectories(moddedCampaignDir);
                        if (Directory.Exists(moddedCampaignDir))
                        {
                            string[] directories2 = Directory.GetDirectories(moddedCampaignDir);
                            for (int i = 0; i < directories2.Length; i++)
                            {
                                //SEModLoader.log.LogInfo("Now processing Campaign file... " + directories2[i]);
                                string fileName = Path.GetFileName(directories2[i]);
                                //SEModLoader.log.LogInfo("Now processing subfile... " + fileName);
                                if (File.Exists(Path.Combine(directories2[i], "config.json")))
                                {
                                    //SEModLoader.log.LogInfo("Found file for modded campaign ! : " + Path.Combine(directories2[i], "config.json"));
                                    string directoryPath = Path.GetDirectoryName(Path.Combine(directories2[i], "config.json"));
                                    string lastDirectory = Path.GetFileName(directoryPath);
                                    //SEModLoader.log.LogInfo("Trimmed directory name : " + lastDirectory);
                                    Type campaignType = typeof(SE.World.Campaign);
                                    MethodInfo normalizeCampaignMethod = campaignType.GetMethod("NormalizeCampaign", BindingFlags.NonPublic | BindingFlags.Instance);
                                    StreamReader streamReader = new StreamReader(Path.Combine(directories2[i], "config.json"));
                                    string config = streamReader.ReadToEnd();
                                    streamReader.Close();


                                    CampaignConfig newConfig = JsonUtility.FromJson<CampaignConfig>(config);
                                    Campaign campaign = new Campaign(newConfig);


                                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.campaignBannerRef);

                                    gameObject.name = fileName; // Ensure unique name
                                    gameObject.transform.parent = campaignMap.transform;
                                    gameObject.transform.localRotation = Quaternion.Euler(campaign.config.banner.rotation);
                                    gameObject.transform.localPosition = campaign.config.banner.position - new Vector3(-0.3f, 0, 0); // Adjust position

                                    // Add InteractableMouse
                                    InteractableMouse interactableMouse = gameObject.AddComponent<InteractableMouse>();


                                    try
                                    {

                                        var im = gameObject.GetComponent<InteractableMouse>();
                                        SEModLoader.log.LogInfo("IM.Name : " + im.name);
                                        foreach(var x in campaignMap.GetComponentsInChildren<InteractableMouse>())
                                        {
                                            SEModLoader.log.LogInfo("InteractableMouse in campaignMap : " + x.name);
                                            
                                        }

                                    }
                                    catch (Exception ex)
                                    {
                                        SEModLoader.log.LogError($"Failed to add InteractableMouse to {gameObject.name}: {ex.Message}");
                                    }
                                    SEModLoader.log.LogInfo($"Parent transform of campaignMap: {campaignMap.transform.name}");
                                    SEModLoader.log.LogInfo($"Loaded campaign config: ID={newConfig.id}, BannerRotation={newConfig.banner.rotation}, BannerPosition={newConfig.banner.position}");
                                    SEModLoader.log.LogError("Campaign added : " + campaign.config.id);
                                    ;
                                    SEModLoader.log.LogInfo($"Child count of campaignMap after adding {gameObject.name}: {campaignMap.transform.childCount}");


                                    foreach (Transform child in campaignMap.transform)
                                    {
                                        if (child.name == "cadenza")
                                        {


                                            SEModLoader.log.LogInfo($"Updated position for cadenza: {gameObject.transform.localPosition}");
                                            SEModLoader.log.LogInfo($"Child found: {child.name}");
                                            SEModLoader.log.LogInfo($"Active in hierarchy: {child.gameObject.activeInHierarchy}");
                                            SEModLoader.log.LogInfo($"Position: {child.transform.localPosition}");
                                            SEModLoader.log.LogInfo($"Rotation: {child.transform.localRotation}");
                                            int layer = gameObject.layer;
                                            Camera mainCamera = Camera.main;
                                            if ((mainCamera.cullingMask & (1 << layer)) == 0)
                                            {
                                                mainCamera.cullingMask |= (1 << layer);
                                                SEModLoader.log.LogInfo($"Added cadenza layer to camera culling mask.");
                                            }

                                            Renderer renderer = gameObject.GetComponent<Renderer>();
                                            if (renderer != null && renderer.material != null)
                                            {
                                                SEModLoader.log.LogInfo($"Material for cadenza: {renderer.material.name}");
                                                renderer.material.color = Color.white; // Ensure it’s visible
                                            }
                                            else
                                            {
                                                SEModLoader.log.LogError("Renderer or material missing for cadenza. Assigning default material.");
                                                renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                                            }
                                            /*if (renderer != null)
                                            {
                                                renderer.enabled = true; // Ensure enabled
                                                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // Disable shadows
                                                renderer.receiveShadows = false; // Ignore shadow effects
                                                renderer.sortingOrder = 100; // Bring to the front
                                                SEModLoader.log.LogInfo($"Force enabled renderer for cadenza: {renderer.isVisible}");
                                            }
                                            else
                                            {
                                                SEModLoader.log.LogError("Renderer missing for cadenza.");
                                            }
                                            gameObject.layer = LayerMask.NameToLayer("UI"); // Replace "UI" with the correct layer
                                            SEModLoader.log.LogInfo($"Set layer for cadenza: {gameObject.layer}");
                                            Renderer r = child.GetComponent<Renderer>();
                                            if (r != null)
                                            {
                                                SEModLoader.log.LogInfo($"Renderer enabled: {r.enabled}, Visible: {r.isVisible}");
                                            }
                                            else
                                            {
                                                SEModLoader.log.LogError($"Renderer is missing for 'cadenza'.");
                                            }*/

                                            InteractableMouse im = child.GetComponent<InteractableMouse>();
                                            if (im != null)
                                            {
                                                SEModLoader.log.LogInfo($"InteractableMouse is attached to 'cadenza'.");
                                            }
                                            else
                                            {
                                                SEModLoader.log.LogError($"InteractableMouse is missing for 'cadenza'.");
                                            }

                                        }
                                    }
                                    SEModLoader.log.LogInfo($"Is {gameObject.name} active: {gameObject.activeSelf}");
                                    

                                    if (campaign == null)
                                    {
                                        MethodInfo selectCampaignMethod = __instance.GetType().GetMethod("SelectCampaign", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                                        if (selectCampaignMethod != null)
                                        {
                                            selectCampaignMethod.Invoke(__instance, new object[] { interactableMouse });
                                            interactableMouse.Active(false);
                                        }
                                        else
                                        {
                                            SEModLoader.log.LogError("Could not find SelectCampaign method on the target class.");
                                        }
                                        
                                    }

                                }
                                else
                                {
                                    SEModLoader.log.LogInfo("Found file for modded campaign ! : " + file);
                                }
                            }

                        }
                    }
                }
                if (campaignBannerRef == null || campaignMap == null)
                {
                    SEModLoader.log.LogError("One of the required fields (campaignBannerRef or campaignMap) is null.");
                }

                SEModLoader.log.LogInfo($"CampaignBannerRef retrieved: {campaignBannerRef.name}");
                SEModLoader.log.LogInfo($"CampaignMap retrieved: {campaignMap.name}");
                SEModLoader.log.LogInfo($"CampaignMap retrieved: {campaignMap.name}");

               
                
            }

            
        }
/*        //for Debug only
        [HarmonyPatch(typeof(StartMenu), "SelectCampaign")]
        static class StartMenu_SelectCampaign_Patch
        {
            static void Postfix(StartMenu __instance, ref InteractableMouse campaignSelected)
            {
                SEModLoader.log.LogInfo($"Entering SelectCampaign with campaignSelected: {campaignSelected?.gameObject.name ?? "NULL"}");

                // Access the private 'campaign' field using reflection
                var campaignField = AccessTools.Field(typeof(StartMenu), "campaign");
                if (campaignField == null)
                {
                    SEModLoader.log.LogError("Failed to access the private field 'campaign'.");
                    return;
                }

                // Get the current campaign from the private field
                InteractableMouse currentCampaign = (InteractableMouse)campaignField.GetValue(__instance);
                SEModLoader.log.LogInfo($"Current campaign: {currentCampaign?.gameObject.name ?? "NULL"}");

                // Debugging campaign selection
                try
                {
                    Campaign campaign = new Campaign(campaignSelected?.gameObject.name);
                    SEModLoader.log.LogInfo($"Loaded campaign: {campaign.config.id}");

                    // UI Updates
                    SEModLoader.log.LogInfo($"Campaign name: {Language.Get("campaign-" + campaign.config.id, "name")}");
                    SEModLoader.log.LogInfo($"Campaign description: {Language.Get("campaign-" + campaign.config.id, "description")}");
                }
                catch (Exception ex)
                {
                    SEModLoader.log.LogError($"Error processing selected campaign: {ex.Message}");
                }

                SEModLoader.log.LogInfo($"Exiting SelectCampaign for campaignSelected: {campaignSelected?.gameObject.name ?? "NULL"}");
            }
        }
        [HarmonyPatch(typeof(StartMenu), "SelectCampaign")]
        static class StartMenu_SelectCampaign_DebugPatch
        {
            static void Prefix(StartMenu __instance, InteractableMouse campaignSelected)
            {
                SEModLoader.log.LogInfo($"Entering SelectCampaign. Campaign selected: {campaignSelected?.gameObject.name ?? "NULL"}");

                // Access the private campaign field
                var campaignField = AccessTools.Field(typeof(StartMenu), "campaign");
                if (campaignField != null)
                {
                    var currentCampaign = (InteractableMouse)campaignField.GetValue(__instance);
                    SEModLoader.log.LogInfo($"Current campaign: {currentCampaign?.gameObject.name ?? "NULL"}");
                }
                else
                {
                    SEModLoader.log.LogError("Could not access the private 'campaign' field.");
                }
            }

            static void Postfix(StartMenu __instance, InteractableMouse campaignSelected)
            {
                SEModLoader.log.LogInfo($"Exiting SelectCampaign. Campaign selected: {campaignSelected?.gameObject.name ?? "NULL"}");

                // Access and log the current campaign after selection
                var campaignField = AccessTools.Field(typeof(StartMenu), "campaign");
                if (campaignField != null)
                {
                    var currentCampaign = (InteractableMouse)campaignField.GetValue(__instance);
                    SEModLoader.log.LogInfo($"Post-selection, current campaign: {currentCampaign?.gameObject.name ?? "NULL"}");
                }
            }
        }*/


    }
}

