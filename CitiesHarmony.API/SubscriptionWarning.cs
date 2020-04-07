using ColossalFramework.PlatformServices;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using System;
using System.Reflection;
using System.Text;

namespace CitiesHarmony.API {
    public static class SubscriptionWarning {
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
            string reason = "An error occured while attempting to automatically subscribe to Harmony (no network connection?)";
            string solution = "You can manually download the Harmony mod from github.com/boformer/CitiesHarmony/releases";
            if (PlatformService.platformType != PlatformType.Steam) {
                reason = "Harmony could not be installed automatically because you are using a platform other than Steam.";
            } else if (PluginManager.noWorkshop) {
                reason = "Harmony could not be installed automatically because you are playing in --noWorkshop mode!";
                solution = "Restart without --noWorkshop or manually download the Harmony mod from github.com/boformer/CitiesHarmony/releases";
            } else if(!PlatformService.workshop.IsAvailable()) {
                reason = "Harmony could not be installed automatically because the Steam workshop is not available (no network connection?)";
            } else if(HarmonyHelper.IsCitiesHarmonyWorkshopItemSubscribed) {
                reason = "It seems that Harmony has already been subscribed, but Steam failed to download the files correctly or they were deleted.";
                solution = "Close the game, then unsubscribe and resubscribe the Harmony workshop item from steamcommunity.com/sharedfiles/filedetails/?id=2040656402";
            }

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
            if (assembly.GetName().Name == "0Harmony") return false;

            foreach (var assemblyName in assembly.GetReferencedAssemblies()) {
                if (assemblyName.Name == "0Harmony" && assemblyName.Version.Major >= 2) {
                    return true;
                }
            }
            return false;
        }
    }
}
