using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CitiesHarmony {
    /// <summary>
    /// Self-patches a Harmony 1.2.0.1 assembly so that it redirects all patch/unpatch calls to Harmony 2.x
    /// </summary>
    public static class Harmony1201SelfPatcher {
        public static void Apply(Harmony harmony, Assembly assembly) {
            UnityEngine.Debug.Log($"Patching Harmony {assembly.GetName().Version} assembly ({assembly.FullName})");

            var harmonyInstanceType = assembly.GetType("Harmony.HarmonyInstance");
            var patchProcessorType = assembly.GetType("Harmony.PatchProcessor");
            var harmonyPatchTypeType = assembly.GetType("Harmony.HarmonyPatchType");

            var HarmonyInstance_UnpatchAll = harmonyInstanceType.GetMethodOrThrow("UnpatchAll");
            var PatchProcessor_Patch = patchProcessorType.GetMethodOrThrow("Patch");
            var PatchProcessor_Unpatch1 = patchProcessorType.GetMethodOrThrow("Unpatch", new Type[] { typeof(MethodInfo) });
            var PatchProcessor_Unpatch2 = patchProcessorType?.GetMethodOrThrow("Unpatch", new Type[] { harmonyPatchTypeType, typeof(string) });

            harmony.Patch(HarmonyInstance_UnpatchAll, new HarmonyMethod(typeof(Harmony1201SelfPatcher).GetMethod(nameof(HarmonyInstance_UnpatchAll_Prefix))));
            harmony.Patch(PatchProcessor_Patch, new HarmonyMethod(typeof(Harmony1201SelfPatcher).GetMethod(nameof(PatchProcessor_Patch_Prefix))));
            harmony.Patch(PatchProcessor_Unpatch1, new HarmonyMethod(typeof(Harmony1201SelfPatcher).GetMethod(nameof(PatchProcessor_Unpatch1_Prefix))));
            harmony.Patch(PatchProcessor_Unpatch2, new HarmonyMethod(typeof(Harmony1201SelfPatcher).GetMethod(nameof(PatchProcessor_Unpatch2_Prefix))));
        }

        public static bool HarmonyInstance_UnpatchAll_Prefix(string ___id, string harmonyID) {
#if DEBUG
            UnityEngine.Debug.Log($"Unpatching all (HarmonyId: {harmonyID})");
#endif
            var harmony = new Harmony(___id);
            harmony.UnpatchAll(harmonyID);

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

            var HarmonyMethod__method = oldHarmonyMethod.GetType().GetFieldOrThrow("method");
            var HarmonyMethod__declaringType = oldHarmonyMethod.GetType().GetFieldOrThrow("declaringType");
            var HarmonyMethod__methodName = oldHarmonyMethod.GetType().GetFieldOrThrow("methodName");
            var HarmonyMethod__methodType = oldHarmonyMethod.GetType().GetFieldOrThrow("methodType");
            var HarmonyMethod__argumentTypes = oldHarmonyMethod.GetType().GetFieldOrThrow("argumentTypes");
            var HarmonyMethod__prioritiy = oldHarmonyMethod.GetType().GetFieldOrThrow("prioritiy"); // typo is intentional
            var HarmonyMethod__before = oldHarmonyMethod.GetType().GetFieldOrThrow("before");
            var HarmonyMethod__after = oldHarmonyMethod.GetType().GetFieldOrThrow("after");

            return new HarmonyMethod {
                method = (MethodInfo)HarmonyMethod__method.GetValue(oldHarmonyMethod),
                declaringType = (Type)HarmonyMethod__declaringType.GetValue(oldHarmonyMethod),
                methodName = (string)HarmonyMethod__methodName.GetValue(oldHarmonyMethod),
                methodType = (MethodType?)HarmonyMethod__methodType.GetValue(oldHarmonyMethod),
                argumentTypes = (Type[])HarmonyMethod__argumentTypes.GetValue(oldHarmonyMethod),
                priority = (int)HarmonyMethod__prioritiy.GetValue(oldHarmonyMethod),
                before = (string[])HarmonyMethod__before.GetValue(oldHarmonyMethod),
                after = (string[])HarmonyMethod__after.GetValue(oldHarmonyMethod)
            };
        }
    }
}
