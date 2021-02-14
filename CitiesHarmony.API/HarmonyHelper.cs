using ColossalFramework.PlatformServices;
using ColossalFramework.Plugins;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CitiesHarmony.API {
    public static class HarmonyHelper {
        internal const ulong CitiesHarmonyWorkshopId = 2040656402uL;

        private static bool _workshopItemInstalledSubscribed = false;
        private static List<Action> _harmonyReadyActions = new List<Action>();

        public static bool IsHarmonyInstalled => InvokeHarmonyInstaller();

        public static void EnsureHarmonyInstalled() {
            if (!IsHarmonyInstalled) {
                SubscriptionPrompt.ShowOnce();
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

                SubscriptionPrompt.ShowOnce();
            }
        }

        private static bool InvokeHarmonyInstaller() {
            var installerRunMethod = Type.GetType("CitiesHarmony.Installer, CitiesHarmony", false)?.GetMethod("Run", BindingFlags.Public | BindingFlags.Static);
            if (installerRunMethod == null) return false;

            installerRunMethod.Invoke(null, new object[0]);
            return true;
        }

        private static bool SteamWorkshopAvailable => PlatformService.platformType == PlatformType.Steam && !PluginManager.noWorkshop;

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

        internal static bool IsCitiesHarmonyWorkshopItemSubscribed {
            get {
                var subscribedIds = PlatformService.workshop.GetSubscribedItems();
                if (subscribedIds == null) return false;

                foreach (var id in subscribedIds) {
                    if (id.AsUInt64 == CitiesHarmonyWorkshopId) return true;
                }

                return false;
            }
        }
    }
}
