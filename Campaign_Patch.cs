using SE.World;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using BepInEx;
using SE.UI;

namespace SEModLoader
{
    //Doesn't work, need to talk to @Lobico
    internal class Campaign_Patch
    {
        [HarmonyPatch(typeof(StartMenu), "LoadCampaigns")]
        public static class LoadCampaignsPatch
        {
            static void Postfix(object __instance)
            {
                // Define the default campaign path
                string defaultCampaignsPath = Application.dataPath + "/Campaigns";

                // Define the additional modded campaigns path
                string moddedCampaignsPath = Path.Combine(BepInEx.Paths.PluginPath, "ModdedContent", "Campaigns");

                // Ensure both directories exist
                if (!Directory.Exists(defaultCampaignsPath))
                {
                    Directory.CreateDirectory(defaultCampaignsPath);
                }
                if (!Directory.Exists(moddedCampaignsPath))
                {
                    Directory.CreateDirectory(moddedCampaignsPath);
                }

                // Use reflection to get the campaignBannerRef and campaignMap fields
                FieldInfo bannerRefField = __instance.GetType().GetField("campaignBannerRef", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                FieldInfo mapField = __instance.GetType().GetField("campaignMap", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                FieldInfo campaignField = __instance.GetType().GetField("campaign", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (bannerRefField == null || mapField == null)
                {
                    SEModLoader.log.LogError("Could not access required fields (campaignBannerRef or campaignMap) in the target class.");
                    return;
                }

                // Get the values of campaignBannerRef and campaignMap
                GameObject campaignBannerRef = (GameObject)bannerRefField.GetValue(__instance);
                GameObject campaignMap = (GameObject)mapField.GetValue(__instance);

                // Function to load campaigns from a given directory
                void LoadCampaignsFromDirectory(string path)
                {
                    string[] directories = Directory.GetDirectories(path);
                    for (int i = 0; i < directories.Length; i++)
                    {
                        string fileName = Path.GetFileName(directories[i]);

                        // Construct path to config.json in the current directory
                        string configPath = Path.Combine(directories[i], "config.json");

                        // Try to load the campaign, handling any potential exceptions
                        try
                        {
                            Campaign campaign = new Campaign(fileName);

                            // Instantiate a campaign banner for each campaign found
                            GameObject gameObject = UnityEngine.Object.Instantiate(campaignBannerRef);
                            gameObject.name = fileName;
                            gameObject.transform.parent = campaignMap.transform;
                            gameObject.transform.localRotation = Quaternion.Euler(campaign.config.banner.rotation);
                            gameObject.transform.localPosition = campaign.config.banner.position;

                            // Add the InteractableMouse component to the campaign banner
                            InteractableMouse interactableMouse = gameObject.AddComponent<InteractableMouse>();

                            // Check if the 'campaign' field is null and invoke SelectCampaign if so
                            if (campaignField != null && campaignField.GetValue(__instance) == null)
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
                        catch (Exception ex)
                        {
                            // Log a warning if the campaign couldn't be loaded, and continue with other campaigns
                            SEModLoader.log.LogWarning($"Could not load campaign '{fileName}' from directory '{path}': {ex.Message}");
                        }
                    }
                }

                // Load campaigns from the default directory first
                LoadCampaignsFromDirectory(defaultCampaignsPath);

                // Load campaigns from the modded directory
                LoadCampaignsFromDirectory(moddedCampaignsPath);
            }
        }
    }
}
