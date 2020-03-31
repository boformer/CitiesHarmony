using ColossalFramework.PlatformServices;
using ColossalFramework.Plugins;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CitiesHarmony.API {
    public static class HarmonyHelper {
        private const ulong CitiesHarmonyWorkshopId = 2040656402uL;

        private static bool _workshopItemInstalledSubscribed = false;
        private static List<Action> _harmonyReadyActions = new List<Action>();

        public static bool IsHarmonyInstalled => InvokeHarmonyInstaller();

        public static void EnsureHarmonyInstalled() {
            if (!IsHarmonyInstalled) {
                InstallHarmonyWorkshopItem();
            }
        }

        public static void DoOnHarmonyReady(Action action) {
            if (IsHarmonyInstalled) {
                action();
            } else {
                _harmonyReadyActions.Add(action);

                if (!_workshopItemInstalledSubscribed && SteamWorkshopAvailable) {
                    _workshopItemInstalledSubscribed = true;
                    PlatformService.workshop.eventWorkshopItemInstalled += OnWorkshopItemInstalled;
                }

                InstallHarmonyWorkshopItem();
            }
        }

        private static bool InvokeHarmonyInstaller() {
            var installerRunMethod = Type.GetType("CitiesHarmony.Installer, CitiesHarmony", false)?.GetMethod("Run", BindingFlags.Public | BindingFlags.Static);
            if (installerRunMethod == null) return false;

            installerRunMethod.Invoke(null, new object[0]);
            return true;
        }

        private static bool SteamWorkshopAvailable => PlatformService.platformType == PlatformType.Steam && !PluginManager.noWorkshop;

        private static void InstallHarmonyWorkshopItem() {
            // TODO show error message to the user

            if (PlatformService.platformType != PlatformType.Steam) {
                UnityEngine.Debug.LogError("Cannot auto-subscribe CitiesHarmony on platforms other than Steam!");
                SubscriptionWarning.ShowOnce();
                return;
            }

            if (PluginManager.noWorkshop) {
                UnityEngine.Debug.LogError("Cannot auto-subscribe CitiesHarmony in noWorkshop mode!");
                SubscriptionWarning.ShowOnce();
                return;
            }

            UnityEngine.Debug.Log("Subscribing to CitiesHarmony workshop item!");
            if (!PlatformService.workshop.Subscribe(new PublishedFileId(CitiesHarmonyWorkshopId))) {
                SubscriptionWarning.ShowOnce();
            }
        }

        private static void OnWorkshopItemInstalled(PublishedFileId id) {
            if (id.AsUInt64 == CitiesHarmonyWorkshopId) {
                UnityEngine.Debug.Log("CitiesHarmony workshop item subscribed and loaded!");

                if (InvokeHarmonyInstaller()) {
                    foreach (var action in _harmonyReadyActions) RunHarmonyReadyAction(action);
                    _harmonyReadyActions.Clear();
                } else {
                    UnityEngine.Debug.LogError("Failed to invoke Harmony installer!");
                }
            }
        }

        private static void RunHarmonyReadyAction(Action action) {
            try {
                action();
            } catch (Exception e) {
                UnityEngine.Debug.LogException(e);
            }
        }
    }
}
