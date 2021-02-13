using CitiesHarmony.API;
using HarmonyLib;
using ICities;
using System.Reflection;

namespace ExampleMod {
    public class Mod : IUserMod {        
        // You can add Harmony 2.0.4 as a dependency, but make sure that 0Harmony.dll is not copied to the output directory!
        // (0Harmony.dll is provided by CitiesHarmony workshop item)

        // Also make sure that HarmonyLib is not referenced in any way in your IUserMod implementation!
        // Instead, apply your patches from a separate static patcher class!
        // (otherwise it will fail to instantiate the type when CitiesHarmony is not installed)

        public string Name => "Harmony 2 Example";
        public string Description => "Patches SimulationManager.CreateRelay and LoadingManager.MetaDataLoaded";

        public void OnEnabled() {
            HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());
        }

        public void OnDisabled() {
            if (HarmonyHelper.IsHarmonyInstalled) Patcher.UnpatchAll();
        }
    }

    public static class Patcher {
        private const string HarmonyId = "boformer.Harmony2Example";

        private static bool patched = false;

        public static void PatchAll() {
            if (patched) return;

            UnityEngine.Debug.Log("Harmony 2 Example: Patching...");

            patched = true;

            // Apply your patches here!
            // Harmony.DEBUG = true;
            var harmony = new Harmony("boformer.Harmony2Example");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void UnpatchAll() {
            if (!patched) return;

            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);

            patched = false;

            UnityEngine.Debug.Log("Harmony 2 Example: Reverted...");
        }
    }

    // Random example patch
    [HarmonyPatch(typeof(SimulationManager), "CreateRelay")]
    public static class SimulationManagerCreateRelayPatch {
        public static void Prefix() {
            UnityEngine.Debug.Log("CreateRelay Prefix");
        }
    }

    // Random example patch
    [HarmonyPatch(typeof(LoadingManager), "MetaDataLoaded")]
    public static class LoadingManagerMetaDataLoadedPatch {
        public static void Prefix() {
            UnityEngine.Debug.Log("MetaDataLoaded Prefix");
        }
    }
}

