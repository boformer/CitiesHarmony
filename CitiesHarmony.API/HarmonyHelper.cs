using ColossalFramework.PlatformServices;
using ColossalFramework.Plugins;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CitiesHarmony.API
{
    public static class HarmonyHelper
    {
        public static Version MinHarmonyVersion => new Version(2, 2, 0, 0);

        internal const ulong CitiesHarmonyWorkshopId = 2040656402uL;

        private static bool _workshopItemInstalledSubscribed = false;
        private static List<Action> _harmonyReadyActions = new List<Action>();

        public static bool IsHarmonyInstalled => InvokeHarmonyInstaller();

        public static void EnsureHarmonyInstalled()
        {
            if (!IsHarmonyInstalled)
            {
                SubscriptionPrompt.ShowOnce();
            }
        }

        public static void DoOnHarmonyReady(Action action)
        {
            if (IsHarmonyInstalled)
            {
                action();
            }
            else
            {
                _harmonyReadyActions.Add(action);

                if (!_workshopItemInstalledSubscribed && SteamWorkshopAvailable)
                {
                    _workshopItemInstalledSubscribed = true;
                    PlatformService.workshop.eventWorkshopItemInstalled += OnWorkshopItemInstalled;
                }

                SubscriptionPrompt.ShowOnce();
            }
        }

        private static bool InvokeHarmonyInstaller()
        {
            var installerRunMethod = GetInstallerRunMethod();
            if (installerRunMethod == null)
                return false;

            installerRunMethod.Invoke(null, new object[0]);

            if (!IsCurrentHarmonyVersionLoaded)
                return false;

            return true;
        }

        internal static bool IsInstallerLoaded => GetInstallerRunMethod() != null;

        private static MethodInfo GetInstallerRunMethod()
        {
            return Type.GetType("CitiesHarmony.Installer, CitiesHarmony", false)?.GetMethod("Run", BindingFlags.Public | BindingFlags.Static);
        }

        internal static bool IsCurrentHarmonyVersionLoaded => GetLoadedHarmonyVersion() >= MinHarmonyVersion;

        internal static Version GetLoadedHarmonyVersion()
        {
            try
            {
                // we are using this dict from PluginManager to get the assembly locations
                // (assembly.Location and assembly.CodeBase return empty/incorrect paths)
                var assemblyLocationsField = typeof(PluginManager).GetField("m_AssemblyLocations", BindingFlags.NonPublic | BindingFlags.Instance);
                var assemblyLocations = (Dictionary<Assembly, string>)assemblyLocationsField.GetValue(PluginManager.instance);
                Version result = default;
                foreach (var pair in assemblyLocations)
                {
                    var assemblyName = pair.Key.GetName();
                    if ((assemblyName.Name == "CitiesHarmony.Harmony") && assemblyName.Version.Major >= 2)
                    {
                        // we are using the file version to determine the minor version
                        // because increasing the assembly version breaks the game's assembly resolution
                        // (we are stuck at assembly version 2.0.4.0 forever)
                        var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(pair.Value);
                        var fileVersion = new Version(fvi.FileVersion);
                        if (result == default || fileVersion < result)
                            result = fileVersion;
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                return MinHarmonyVersion; // safety in case future game code changes are breaking this
            }
        }


        private static bool SteamWorkshopAvailable => PlatformService.platformType == PlatformType.Steam && !PluginManager.noWorkshop;

        private static void OnWorkshopItemInstalled(PublishedFileId id)
        {
            if (id.AsUInt64 == CitiesHarmonyWorkshopId)
            {
                UnityEngine.Debug.Log("CitiesHarmony workshop item subscribed and loaded!");

                if (InvokeHarmonyInstaller())
                {
                    foreach (var action in _harmonyReadyActions) RunHarmonyReadyAction(action);
                    _harmonyReadyActions.Clear();
                }
                else
                {
                    UnityEngine.Debug.LogError("Failed to invoke Harmony installer!");
                }
            }
        }

        private static void RunHarmonyReadyAction(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        internal static bool IsCitiesHarmonyWorkshopItemSubscribed
        {
            get
            {
                var subscribedIds = PlatformService.workshop.GetSubscribedItems();
                if (subscribedIds == null) return false;

                foreach (var id in subscribedIds)
                {
                    if (id.AsUInt64 == CitiesHarmonyWorkshopId) return true;
                }

                return false;
            }
        }
    }
}
