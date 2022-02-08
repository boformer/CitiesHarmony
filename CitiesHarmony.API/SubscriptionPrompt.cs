using ColossalFramework.PlatformServices;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using System;
using System.Reflection;
using System.Text;

namespace CitiesHarmony.API
{
    public static class SubscriptionPrompt
    {
        private const string Marker = "Harmony2SubscriptionWarning";

        public static void ShowOnce()
        {
            if (UnityEngine.GameObject.Find(Marker)) return;

            var go = new UnityEngine.GameObject(Marker);
            UnityEngine.Object.DontDestroyOnLoad(go);

            if (LoadingManager.instance.m_currentlyLoading || UIView.library == null)
            {
                LoadingManager.instance.m_introLoaded += OnIntroLoaded;
                LoadingManager.instance.m_levelLoaded += OnLevelLoaded;
            }
            else
            {
                Show();
            }
        }

        private static void OnIntroLoaded()
        {
            LoadingManager.instance.m_introLoaded -= OnIntroLoaded;
            LoadingManager.instance.m_levelLoaded -= OnLevelLoaded;
            Show();
        }

        private static void OnLevelLoaded(SimulationManager.UpdateMode updateMode)
        {
            LoadingManager.instance.m_introLoaded -= OnIntroLoaded;
            LoadingManager.instance.m_levelLoaded -= OnLevelLoaded;
            Show();
        }

        private static void Show()
        {
            if (!HarmonyHelper.IsInstallerLoaded)
            {
                ShowSubscriptionPrompt();
            }
            else if (!HarmonyHelper.IsCurrentHarmonyVersionLoaded)
            {
                ShowOutdatedPrompt();
            }
        }

        private static void ShowSubscriptionPrompt()
        {
            if (!GetSubscriptionHelpMessages(out var reason, out var solution))
            {
                ShowError(reason, solution);
                return;
            }
            else
            {
                ConfirmPanel.ShowModal("Missing dependency: Harmony",
                    "The dependency 'Harmony' is required for some mod(s) to work correctly. Do you want to subscribe to it in the Steam Workshop?",
                    OnConfirm);
            }
        }

        private static void OnConfirm(UIComponent component, int result)
        {
            if (result == 1)
            {
                UnityEngine.Debug.Log("Subscribing to CitiesHarmony workshop item!");

                if (PlatformService.workshop.Subscribe(new PublishedFileId(HarmonyHelper.CitiesHarmonyWorkshopId)))
                {
                    UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Success!",
                        "Harmony has been installed successfully. It is recommended to restart the game now!", false);
                }
                else
                {
                    ShowError("An error occured while attempting to automatically subscribe to Harmony (no network connection?)",
                        "You can manually download the Harmony mod from github.com/boformer/CitiesHarmony/releases");
                }
            }
            else
            {
                ShowError("You have rejected to automatically subscribe to Harmony :(",
                    "Either unsubscribe those mods or subscribe to the Harmony mod, then restart the game!");
            }
        }

        private static void ShowError(string reason, string solution)
        {
            var affectedAssemblyNames = new StringBuilder();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (RequiresHarmony2(assembly))
                {
                    affectedAssemblyNames.Append("• ").Append(GetModName(assembly)).Append('\n');
                }
            }

            var message = $"The mod(s):\n{affectedAssemblyNames}require the dependency 'Harmony' to work correctly!\n\n{reason}\n\nClose the game, {solution}";

            UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Missing dependency: Harmony", message, false);
        }

        private static void ShowOutdatedPrompt()
        {
            var loadedVersion = HarmonyHelper.GetLoadedHarmonyVersion();

            var affectedAssemblyNames = new StringBuilder();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (RequiresHarmony2(assembly))
                {
                    Version requiredVersion = GetRequiredHarmonyVersion(assembly);
                    if (requiredVersion > loadedVersion)
                    {
                        affectedAssemblyNames.Append("• ").Append(GetModName(assembly)).Append(" (requires ").Append(requiredVersion).Append(")\n");
                    }

                }
            }

            GetSubscriptionHelpMessages(out _, out var solution);
            var message = $"The mod(s):\n{affectedAssemblyNames}require a newer version of the dependency 'Harmony' to work correctly!\n\nClose the game, {solution}";

            UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage($"Outdated Harmony version {loadedVersion}", message, false);
        }

        private static bool GetSubscriptionHelpMessages(out string reason, out string solution)
        {
            if (PlatformService.platformType != PlatformType.Steam)
            {
                UnityEngine.Debug.LogError("Cannot auto-subscribe CitiesHarmony on platforms other than Steam!");
                reason = "Harmony could not be installed automatically because you are using a platform other than Steam.";
                solution = "then manually download and install the Harmony mod from github.com/boformer/CitiesHarmony/releases";
                return false;
            }

            if (PluginManager.noWorkshop)
            {
                UnityEngine.Debug.LogError("Cannot auto-subscribe CitiesHarmony in --noWorkshop mode!");
                reason = "Harmony could not be installed automatically because you are playing in --noWorkshop mode!";
                solution = "then restart without --noWorkshop or manually download and install the Harmony mod from github.com/boformer/CitiesHarmony/releases";
                return false;
            }

            if (!PlatformService.workshop.IsAvailable())
            {
                UnityEngine.Debug.LogError("Cannot auto-subscribe CitiesHarmony while workshop is not available");
                reason = "Harmony could not be installed automatically because the Steam workshop is not available (no network connection?)";
                solution = "then manually download and install the Harmony mod from github.com/boformer/CitiesHarmony/releases";
                return false;
            }


            if (HarmonyHelper.IsCitiesHarmonyWorkshopItemSubscribed)
            {
                UnityEngine.Debug.LogError("CitiesHarmony workshop item is subscribed, but assembly is not loaded or outdated!");
                reason = "It seems that Harmony has already been subscribed, but Steam failed to download the files correctly or they were deleted.";
                solution = "uninstall all local or alternative versions of the Harmony mod, then (re)subscribe the Harmony workshop item from steamcommunity.com/sharedfiles/filedetails/?id=2040656402";
                return false;
            }


            reason = "";
            solution = "uninstall all local or alternative versions of the Harmony mod, then (re)subscribe the Harmony workshop item from steamcommunity.com/sharedfiles/filedetails/?id=2040656402";
            return true;
        }

        private static bool RequiresHarmony2(Assembly assembly)
        {
            if (assembly.GetName().Name == "0Harmony" || assembly.GetName().Name == "CitiesHarmony.Harmony") return false;

            foreach (var assemblyName in assembly.GetReferencedAssemblies())
            {
                if ((assemblyName.Name == "0Harmony" || assemblyName.Name == "CitiesHarmony.Harmony") && assemblyName.Version.Major >= 2)
                {
                    return true;
                }
            }
            return false;
        }

        private static Version GetRequiredHarmonyVersion(Assembly assembly)
        {
            foreach (var assemblyName in assembly.GetReferencedAssemblies())
            {
                if (assemblyName.Name == "CitiesHarmony.API")
                {
                    try
                    {
                        var versionGetter = Assembly.Load(assemblyName)
                            ?.GetType("CitiesHarmony.API.HarmonyHelper")
                            ?.GetProperty("MinHarmonyVersion", BindingFlags.Static | BindingFlags.Public)
                            ?.GetGetMethod();

                        if (versionGetter != null && versionGetter.ReturnType == typeof(Version))
                        {
                            return (Version)versionGetter.Invoke(null, new object[0]);
                        }
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                }
            }

            return new Version(2, 0, 4, 0); // fallback: last version before we added the FileVersion logic
        }

        private static string GetModName(Assembly assembly)
        {
            foreach (var plugin in PluginManager.instance.GetPluginsInfo())
            {
                if (plugin.userModInstance is IUserMod mod && mod.GetType().Assembly == assembly)
                    return mod.Name;
            }

            return assembly.GetName().Name;
        }
    }
}
