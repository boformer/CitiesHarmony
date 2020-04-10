using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace CitiesHarmony {
    /// <summary>
    /// Self-patches a Harmony 1.x assembly so that it redirects all patch/unpatch calls to Harmony 2.x
    /// </summary>
    public static class Harmony1SelfPatcher {
        public static void Apply(Harmony harmony, Assembly assembly) {
            UnityEngine.Debug.Log($"Patching Harmony {assembly.GetName().Version} assembly ({assembly.FullName})");

            var harmonyInstanceType = assembly.GetType("Harmony.HarmonyInstance");
            var patchProcessorType = assembly.GetType("Harmony.PatchProcessor");
            var harmonyPatchTypeType = assembly.GetType("Harmony.HarmonyPatchType");

            var HarmonyInstance_GetPatchedMethods = harmonyInstanceType.GetMethodOrThrow("GetPatchedMethods");
            var HarmonyInstance_GetPatchInfo = harmonyInstanceType.GetMethodOrThrow("GetPatchInfo");
            var HarmonyInstance_UnpatchAll = harmonyInstanceType.GetMethod("UnpatchAll");
            var HarmonyInstance_VersionInfo = harmonyInstanceType.GetMethodOrThrow("VersionInfo");
            var PatchProcessor_Patch = patchProcessorType.GetMethodOrThrow("Patch");
            var PatchProcessor_Unpatch1 = patchProcessorType.GetMethodOrThrow("Unpatch", new Type[] { typeof(MethodInfo) });
            var PatchProcessor_Unpatch2 = patchProcessorType?.GetMethodOrThrow("Unpatch", new Type[] { harmonyPatchTypeType, typeof(string) });

            harmony.Patch(HarmonyInstance_GetPatchedMethods, new HarmonyMethod(typeof(Harmony1SelfPatcher).GetMethod(nameof(HarmonyInstance_GetPatchedMethods_Prefix))));

            harmony.Patch(HarmonyInstance_GetPatchInfo, new HarmonyMethod(typeof(Harmony1SelfPatcher).GetMethod(nameof(HarmonyInstance_GetPatchInfo_Prefix))));

            if (HarmonyInstance_UnpatchAll != null)
                harmony.Patch(HarmonyInstance_UnpatchAll, new HarmonyMethod(typeof(Harmony1SelfPatcher).GetMethod(nameof(HarmonyInstance_UnpatchAll_Prefix))));
            else
                UnityEngine.Debug.Log("HarmonyInstance.UnpatchAll not found (probably an older Harmony version)");

            harmony.Patch(HarmonyInstance_VersionInfo, new HarmonyMethod(typeof(Harmony1SelfPatcher).GetMethod(nameof(HarmonyInstance_VersionInfo_Prefix))));

            harmony.Patch(PatchProcessor_Patch, new HarmonyMethod(typeof(Harmony1SelfPatcher).GetMethod(nameof(PatchProcessor_Patch_Prefix))));
            harmony.Patch(PatchProcessor_Unpatch1, new HarmonyMethod(typeof(Harmony1SelfPatcher).GetMethod(nameof(PatchProcessor_Unpatch1_Prefix))));
            harmony.Patch(PatchProcessor_Unpatch2, new HarmonyMethod(typeof(Harmony1SelfPatcher).GetMethod(nameof(PatchProcessor_Unpatch2_Prefix))));
        }

        public static bool HarmonyInstance_GetPatchedMethods_Prefix(string ___id, out IEnumerable<MethodBase> __result) {
#if DEBUG
            UnityEngine.Debug.Log($"GetPatchedMethods (HarmonyId: {___id})");
#endif
            __result = Harmony.GetAllPatchedMethods();
            return false;
        }

        public static bool HarmonyInstance_GetPatchInfo_Prefix(/* HarmonyInstance */ object __instance, string ___id, MethodBase method, out object __result) {
#if DEBUG
            UnityEngine.Debug.Log($"GetPatchInfo for {method} (HarmonyId: {___id})");
#endif
            __result = CreateOldPatches(__instance.GetType().Assembly, Harmony.GetPatchInfo(method));
            return false;
        }

        public static bool HarmonyInstance_UnpatchAll_Prefix(string ___id, string harmonyID) {
#if DEBUG
            UnityEngine.Debug.Log($"Unpatching all (HarmonyId: {harmonyID})");
#endif
            var harmony = new Harmony(___id);
            harmony.UnpatchAll(harmonyID);

            return false;
        }
        public static bool HarmonyInstance_VersionInfo_Prefix(string ___id, out Version currentVersion, out Dictionary<string, Version> __result) {
#if DEBUG
            UnityEngine.Debug.Log($"VersionInfo (HarmonyId: {___id})");
#endif
            __result = Harmony.VersionInfo(out currentVersion);
            return false;
        }

        public static bool PatchProcessor_Patch_Prefix(object ___instance, List<MethodBase> ___originals,
            object ___prefix, object ___postfix, object ___transpiler, ref List<System.Reflection.Emit.DynamicMethod> __result) {
            if (___prefix != null || ___postfix != null || ___transpiler != null) {
                var harmony = CreateHarmony(___instance);
                var prefix = CreateHarmonyMethod(___prefix);
                var postfix = CreateHarmonyMethod(___postfix);
                var transpiler = CreateHarmonyMethod(___transpiler);

                foreach (var method in ___originals) {
#if DEBUG
                    UnityEngine.Debug.Log($"Patching method {method.FullDescription()} (HarmonyId: {harmony.Id})");
#endif
                    if (!method.IsDeclaredMember()) {
                        UnityEngine.Debug.Log($"Attempting to patch non-declared member {method.FullDescription()} (forbidden in Harmony 2.x)! Getting closest declared member for backwards compatibility...");
                    }

                    var processor = harmony.CreateProcessor(method.GetDeclaredMember());

                    if (prefix != null) processor.AddPrefix(prefix);
                    if (postfix != null) processor.AddPostfix(postfix);
                    if (transpiler != null) processor.AddTranspiler(transpiler);

                    processor.Patch();
                }
            }

            // return empty list (new harmony doesn't return DynamicMethod so we can't do better things here)
            __result = new List<System.Reflection.Emit.DynamicMethod>();
            return false;
        }

        public static bool PatchProcessor_Unpatch1_Prefix(MethodInfo patch, object ___instance, List<MethodBase> ___originals) {
            var harmony = CreateHarmony(___instance);

            foreach (var method in ___originals) {
#if DEBUG
                UnityEngine.Debug.Log($"Unpatching method {method.FullDescription()} (HarmonyId: {harmony.Id})");
#endif
                harmony.Unpatch(method, patch);
            }

            return false;
        }

        public static bool PatchProcessor_Unpatch2_Prefix(HarmonyPatchType type, string harmonyID, object ___instance, List<MethodBase> ___originals) {
            var harmony = CreateHarmony(___instance);

            foreach (var method in ___originals) {
#if DEBUG
                UnityEngine.Debug.Log($"Unpatching patch ({type}) from method {method.FullDescription()} (HarmonyId: {harmony.Id})");
#endif
                harmony.Unpatch(method, type, harmonyID);
            }

            return false;
        }

        private static Harmony CreateHarmony(object oldHarmonyInstance) {
            var HarmonyInstance__id = oldHarmonyInstance.GetType().GetFieldOrThrow("id", BindingFlags.NonPublic | BindingFlags.Instance);
            var harmonyId = (string)HarmonyInstance__id.GetValue(oldHarmonyInstance);

            return new Harmony(harmonyId);
        }

        private static HarmonyMethod CreateHarmonyMethod(object oldHarmonyMethod) {
            if (oldHarmonyMethod == null) return null;

            var type = oldHarmonyMethod.GetType();
            var HarmonyMethod__method = type.GetFieldOrThrow("method");
            var HarmonyMethod__declaringType = type.GetField("originalType") // Harmony 1.1.0.0
                ?? type.GetFieldOrThrow("declaringType"); // Harmony 1.2.0.1
            var HarmonyMethod__methodName = type.GetFieldOrThrow("methodName");
            var HarmonyMethod__methodType = type.GetField("methodType"); // doesn't exist in 1.1.0.0
            var HarmonyMethod__argumentTypes = type.GetField("parameter") // Harmony 1.1.0.0
                ?? type.GetFieldOrThrow("argumentTypes"); // Harmony 1.2.0.1
            var HarmonyMethod__prioritiy = type.GetFieldOrThrow("prioritiy"); // typo is intentional
            var HarmonyMethod__before = type.GetFieldOrThrow("before");
            var HarmonyMethod__after = type.GetFieldOrThrow("after");

            return new HarmonyMethod {
                method = (MethodInfo)HarmonyMethod__method.GetValue(oldHarmonyMethod),
                declaringType = (Type)HarmonyMethod__declaringType.GetValue(oldHarmonyMethod),
                methodName = (string)HarmonyMethod__methodName.GetValue(oldHarmonyMethod),
                methodType = (MethodType?)HarmonyMethod__methodType?.GetValue(oldHarmonyMethod),
                argumentTypes = (Type[])HarmonyMethod__argumentTypes.GetValue(oldHarmonyMethod),
                priority = (int)HarmonyMethod__prioritiy.GetValue(oldHarmonyMethod),
                before = (string[])HarmonyMethod__before.GetValue(oldHarmonyMethod),
                after = (string[])HarmonyMethod__after.GetValue(oldHarmonyMethod)
            };
        }

        private static object CreateOldPatches(Assembly oldAssembly, Patches newPatches) {
            if (newPatches == null) return null;

            var oldPatchesType = oldAssembly.GetType("Harmony.Patches");
            var oldPatchType = oldAssembly.GetType("Harmony.Patch");

            // Patches(Patch[] prefixes, Patch[] postfixes, Patch[] transpilers)
            var oldPatches = Activator.CreateInstance(oldPatchesType, new object[] {
                CreateOldPatchArray(oldPatchType, newPatches.Prefixes),
                CreateOldPatchArray(oldPatchType, newPatches.Postfixes),
                CreateOldPatchArray(oldPatchType, newPatches.Transpilers)
            });

            return oldPatches;
        }

        private static Array CreateOldPatchArray(Type oldPatchType, ReadOnlyCollection<Patch> array) {
            var oldArray = Array.CreateInstance(oldPatchType, array.Count);

            for (var i = 0; i < array.Count; i++) {
                var newPatch = array[i];

                // Patch(MethodInfo patch, int index, string owner, int priority, string[] before, string[] after)
                oldArray.SetValue(Activator.CreateInstance(oldPatchType, new object[] {
                    newPatch.PatchMethod,
                    newPatch.index,
                    newPatch.owner,
                    newPatch.priority,
                    newPatch.before,
                    newPatch.after
                }), i);
            }
            return oldArray;
        }
    }
}
