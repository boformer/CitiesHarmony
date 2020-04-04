using HarmonyLib;
using HarmonyLibTests.Assets.Methods;
using System;

namespace ExampleMod.HarmonyTests {
	public static class Specials {
		// Based on HarmonyLib test "Test_Patch_Returning_Structs", adjusted to be run ingame
		public static void Test_Patch_Returning_Structs(int n, string type) {
			var name = $"{type}M{n:D2}";

			var patchClass = typeof(ReturningStructs_Patch);

			var prefix = SymbolExtensions.GetMethodInfo(() => ReturningStructs_Patch.Prefix(null));

			var instance = new Harmony("returning-structs");

			var cls = typeof(ReturningStructs);
			var method = AccessTools.DeclaredMethod(cls, name);
			if (method == null) throw new Exception("method == null");

			UnityEngine.Debug.Log($"Test_Returning_Structs: patching {name} start");
			try {
				var replacement = instance.Patch(method, new HarmonyMethod(prefix));
				if (replacement == null) throw new Exception("replacement == null");
			} catch (Exception ex) {
				UnityEngine.Debug.Log($"Test_Returning_Structs: patching {name} exception: {ex}");
			}
			UnityEngine.Debug.Log($"Test_Returning_Structs: patching {name} done");

			var clsInstance = new ReturningStructs();
			try {
				UnityEngine.Debug.Log($"Test_Returning_Structs: running patched {name}");

				var original = AccessTools.DeclaredMethod(cls, name);
				if (original == null) throw new Exception("original == null");
				var result = original.Invoke(type == "S" ? null : clsInstance, new object[] { "test" });
				if (result == null) throw new Exception("result == null");
				if ($"St{n:D2}" != result.GetType().Name) throw new Exception($"invalid result type name: {result.GetType().Name}");

				var field = result.GetType().GetField("b1");
				var fieldValue = (byte)field.GetValue(result);
				UnityEngine.Debug.Log(fieldValue);
				if (fieldValue != 42) throw new Exception($"result scrambled!");

				UnityEngine.Debug.Log($"Test_Returning_Structs: running patched {name} done");
			} catch (Exception ex) {
				UnityEngine.Debug.Log($"Test_Returning_Structs: running {name} exception: {ex}");
			}
		}
	}
}
