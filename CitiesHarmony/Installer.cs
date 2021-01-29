using HarmonyLib;
using System;
using System.Reflection;

namespace CitiesHarmony {
    public static class Installer {
        private const string PatchedMarker = "Harmony1_PatchedMarker";

        public static void Run() {
            // Empty method, called by CitiesHarmony.API.HarmonyHelper
        }

        static Installer() {
            try {
                UnityEngine.Debug.Log($"Installing Harmony {typeof(Harmony).GetAssemblyVersion()}!");

                var go = UnityEngine.GameObject.Find(PatchedMarker);
                if (go != null) {
                    UnityEngine.Debug.Log("Harmony 1.x has already been patched!");
                    return;
                }

                // Create a marker to ensure that this code only runs once
                UnityEngine.Object.DontDestroyOnLoad(new UnityEngine.GameObject(PatchedMarker));
                
                var harmony = new Harmony("CitiesHarmony");

                // Patch assembly resolver to make sure that missing 2.x Harmony assembly references are never resolved to 1.x Harmony!
                // This will naturally occur when this mod gets updated to newer Harmony versions.
                AssemblyResolvePatch.Apply(harmony);

                // Self-patch Harmony 1.x assemblies
                var oldHarmonyStateTransferred = false;

                void ProcessAssembly(Assembly assembly) {
                    try {
                        if (assembly == typeof(Harmony).Assembly) return; // skip our own Harmony assembly

                        var assemblyName = assembly.GetName();
                        if (assemblyName.Name == "0Harmony") {
                            if(assemblyName.Version < new Version(1, 1, 0, 0)) {
                                UnityEngine.Debug.Log($"Detected stone age version of Harmony ({assemblyName.Version}). Skipping this one!");
                            } else if(assemblyName.Version < new Version(1, 3, 0, 0)) {
                                try {
                                    if (!oldHarmonyStateTransferred) {
                                        oldHarmonyStateTransferred = true;
                                        var patcher = new Harmony1StateTransfer(harmony, assembly);
                                        patcher.Patch();
                                    } else {
                                        Harmony1SelfPatcher.Apply(harmony, assembly);
                                    }
                                } catch (Exception e) {
                                    UnityEngine.Debug.LogException(e);
                                }
                            } else {
                                UnityEngine.Debug.LogError($"Detected conflicting Harmony 2.x assembly ({assemblyName.Version})!");
                            }
                        }
                    } catch (Exception e) {
                        UnityEngine.Debug.LogException(e);
                    }
                }

                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Array.Sort(assemblies, (a, b) => -a.GetName().Version.CompareTo(b.GetName().Version)); // higher Harmony versions first!

                foreach (var assembly in assemblies) ProcessAssembly(assembly);

                // Process all assemblies that are loaded after this
                AppDomain.CurrentDomain.AssemblyLoad += (object sender, AssemblyLoadEventArgs args) => ProcessAssembly(args.LoadedAssembly);
            } catch (Exception e) {
                UnityEngine.Debug.LogException(e);
            }
        }
    }
}
