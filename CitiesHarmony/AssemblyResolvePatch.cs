using HarmonyLib;
using System;
using System.Reflection;

namespace CitiesHarmony {
    /// <summary>
    /// Patch for assembly resolver that resolves Harmony 2.0.0.8+ assemblies to the latest 0Harmony.dll shipped with the mod.
    /// This is required because the game's default assembly resolver simply returns 
    /// the first assembly with a matching name without comparing the version numbers!
    /// </summary>
    public static class AssemblyResolvePatch {
        /// <summary>
        /// The minimum compatible version of Harmony that can be resolved to the shipped Harmony assembly
        /// </summary>
        private static readonly Version MinResolvableHarmonyVersion = new Version(2, 0, 0, 8);

        public static void Apply(Harmony harmony) {
            harmony.Patch(
                typeof(BuildConfig).GetMethod("CurrentDomain_AssemblyResolve", BindingFlags.NonPublic | BindingFlags.Static),
                new HarmonyMethod(typeof(AssemblyResolvePatch).GetMethod(nameof(CurrentDomain_AssemblyResolve__Prefix), BindingFlags.NonPublic | BindingFlags.Static))
            );
        }

        private static bool CurrentDomain_AssemblyResolve__Prefix(ResolveEventArgs args, ref Assembly __result) {
            __result = Resolver.ResolveHarmony(null, args);
            return __result == null; // if unsuccessful, run original method.
        }
    }
}
