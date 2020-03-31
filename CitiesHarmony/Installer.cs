using HarmonyLib;
using System;
using System.Reflection;

namespace CitiesHarmony {
    public static class Installer {
        private const string PatchedMarker = "Harmony1201_PatchedMarker";

        public static void Run() {
            // Empty method, called by CitiesHarmony.API.HarmonyHelper
        }

        static Installer() {
            UnityEngine.Debug.Log($"Installing Harmony {typeof(Harmony).GetAssemblyVersion()}!");

            var go = UnityEngine.GameObject.Find(PatchedMarker);
            if (go != null) {
                UnityEngine.Debug.Log("Harmony 1.2.0.1 has already been patched!");
                return;
            }

            // Create a marker to ensure that this code only runs once
            UnityEngine.Object.DontDestroyOnLoad(new UnityEngine.GameObject(PatchedMarker));

            var harmony = new Harmony("CitiesHarmony");

            // Self-patch Harmony 1.2.0.1 assemblies
            var oldHarmonyStateTransferred = false;

            void ProcessAssembly(Assembly assembly) {
                var assemblyName = assembly.GetName();
                if (assemblyName.Name == "0Harmony" && assemblyName.Version == new Version(1, 2, 0, 1)) {
                    try {
                        if (!oldHarmonyStateTransferred) {
                            oldHarmonyStateTransferred = true;
                            var patcher = new Harmony1201StateTransfer(harmony, assembly);
                            patcher.Patch();
                        } else {
                            Harmony1201SelfPatcher.Apply(harmony, assembly);
                        }
                    } catch (Exception e) {
                        UnityEngine.Debug.LogException(e);
                    }
                }
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies) ProcessAssembly(assembly);

            // Patch assembly resolver to make sure that missing 2.x Harmony assembly references are never resolved to 1.2 Harmony!
            // This will naturally occur when this mod gets updated to newer Harmony versions.
            AssemblyResolvePatch.Apply(harmony);

            // Process all assemblies that are loaded after this
            AppDomain.CurrentDomain.AssemblyLoad += (object sender, AssemblyLoadEventArgs args) => ProcessAssembly(args.LoadedAssembly);
        }
    }
}
