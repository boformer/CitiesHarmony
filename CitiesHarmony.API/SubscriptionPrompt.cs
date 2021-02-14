﻿using ColossalFramework.PlatformServices;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using System;
using System.Reflection;
using System.Text;

namespace CitiesHarmony.API {
    public static class SubscriptionPrompt {
        private const string Marker = "Harmony2SubscriptionWarning";

        public static void ShowOnce() {
            if (UnityEngine.GameObject.Find(Marker)) return;

            var go = new UnityEngine.GameObject(Marker);
            UnityEngine.Object.DontDestroyOnLoad(go);

            if(LoadingManager.instance.m_currentlyLoading || UIView.library == null) {
                LoadingManager.instance.m_introLoaded += OnIntroLoaded;
                LoadingManager.instance.m_levelLoaded += OnLevelLoaded;
            } else {
                Show();
            }
        }

        private static void OnIntroLoaded() {
            LoadingManager.instance.m_introLoaded -= OnIntroLoaded;
            Show();
        }

        private static void OnLevelLoaded(SimulationManager.UpdateMode updateMode) {
            LoadingManager.instance.m_levelLoaded -= OnLevelLoaded;
            Show();
        }

        public static void Show() {
            if (PlatformService.platformType != PlatformType.Steam) {
                UnityEngine.Debug.LogError("Cannot auto-subscribe CitiesHarmony on platforms other than Steam!");
                ShowError("Harmony could not be installed automatically because you are using a platform other than Steam.",
                    "You can manually download the Harmony mod from github.com/boformer/CitiesHarmony/releases");
                return;
            }

            if (PluginManager.noWorkshop) {
                UnityEngine.Debug.LogError("Cannot auto-subscribe CitiesHarmony in --noWorkshop mode!");
                ShowError("Harmony could not be installed automatically because you are playing in --noWorkshop mode!",
                    "Restart without --noWorkshop or manually download the Harmony mod from github.com/boformer/CitiesHarmony/releases");
                return;
            }

            if (!PlatformService.workshop.IsAvailable()) {
                UnityEngine.Debug.LogError("Cannot auto-subscribe CitiesHarmony while workshop is not available");
                ShowError("Harmony could not be installed automatically because the Steam workshop is not available (no network connection?)",
                    "You can manually download the Harmony mod from github.com/boformer/CitiesHarmony/releases");
                return;
            }


            if (HarmonyHelper.IsCitiesHarmonyWorkshopItemSubscribed) {
                UnityEngine.Debug.LogError("CitiesHarmony workshop item is subscribed, but assembly is not loaded!");
                ShowError("It seems that Harmony has already been subscribed, but Steam failed to download the files correctly or they were deleted.",
                    "Close the game, then unsubscribe and resubscribe the Harmony workshop item from steamcommunity.com/sharedfiles/filedetails/?id=2040656402");
                return;
            }

            ConfirmPanel.ShowModal("Missing dependency: Harmony", 
                "The dependency 'Harmony' is required for some mod(s) to work correctly. Do you want to subscribe to it in the Steam Workshop?", 
                OnConfirm);

            UnityEngine.Debug.Log("Subscribing to CitiesHarmony workshop item!");

        }

        private static void OnConfirm(UIComponent component, int result) {
            if (result == 1) {
                if (PlatformService.workshop.Subscribe(new PublishedFileId(HarmonyHelper.CitiesHarmonyWorkshopId))) {
                    UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Success!", 
                        "Harmony has been installed successfully. It is recommended to restart the game now!", false);
                } else {
                    ShowError("An error occured while attempting to automatically subscribe to Harmony (no network connection?)",
                        "You can manually download the Harmony mod from github.com/boformer/CitiesHarmony/releases");
                }
            } else {
                ShowError("You have rejected to automatically subscribe to Harmony :(",
                    "Either unsubscribe those mods or subscribe to the Harmony mod, then restart the game!");
            }
        }

        private static void ShowError(string reason, string solution) {
            var affectedAssemblyNames = new StringBuilder();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies) {
                if (RequiresHarmony2(assembly)) {
                    affectedAssemblyNames.Append("• ").Append(assembly.GetName().Name).Append('\n');
                }
            }

            var message = $"The mod(s):\n{affectedAssemblyNames}require the dependency 'Harmony' to work correctly!\n\n{reason}\n\n{solution}";

            UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Missing dependency: Harmony", message, false);
        }

        private static bool RequiresHarmony2(Assembly assembly) {
            if (assembly.GetName().Name == "0Harmony" || assembly.GetName().Name == "CitiesHarmony.Harmony") return false;

            foreach (var assemblyName in assembly.GetReferencedAssemblies()) {
                if ((assemblyName.Name == "0Harmony" || assemblyName.Name == "CitiesHarmony.Harmony") && assemblyName.Version.Major >= 2) {
                    return true;
                }
            }
            return false;
        }
    }
}
