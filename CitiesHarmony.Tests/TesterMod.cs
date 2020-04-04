using ExampleMod.HarmonyTests;
using HarmonyLib;
using ICities;

namespace CitiesHarmony.Tests {
    public class TesterMod : IUserMod {
        public string Name => "_CitiesHarmony.Tests";
        public string Description => "Test mod for Harmony features";

        public void OnEnabled() {
            TestRunner.Run();
        }
    }

    public static class TestRunner {
        public static void Run() {
            Harmony.DEBUG = true;

            ReturnColorTest.Run();

            int[] values = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
            foreach (var n in values) Specials.Test_Patch_Returning_Structs(n, "S");
            foreach (var n in values) Specials.Test_Patch_Returning_Structs(n, "I");
        }
    }

    public static class ReturnColorTest {
        public static void Run() {
            var harmony = new Harmony("boformer.Harmony2Example");
            harmony.Patch(typeof(MyClass).GetMethod("GetColorStatic"), null, new HarmonyMethod(typeof(ReturnColorTest).GetMethod("GetColor_Postfix")));
            harmony.Patch(typeof(MyClass).GetMethod("GetColor"), null, new HarmonyMethod(typeof(ReturnColorTest).GetMethod("GetColor_Postfix")));


            var colorStatic = MyClass.GetColorStatic();
            UnityEngine.Debug.Log("colorStatic.g: " + colorStatic.g);
            UnityEngine.Debug.Log(colorStatic.ToString());

            var myClass = new MyClass();

            var color = myClass.GetColor();
            UnityEngine.Debug.Log("color.g: " + color.g);
            UnityEngine.Debug.Log(color.ToString());
        }

        public static void GetColor_Postfix(ref UnityEngine.Color __result) {
            UnityEngine.Debug.Log("GetColor__Postfix: __result.g: " + __result.g);
        }

        public class MyClass {
            public static UnityEngine.Color GetColorStatic() {
                UnityEngine.Debug.Log("GetColorStatic");
                return new UnityEngine.Color(1f, 0.75f, 0.5f, 0.25f);
            }

            public UnityEngine.Color GetColor() {
                UnityEngine.Debug.Log("GetColor");
                return new UnityEngine.Color(1f, 0.75f, 0.5f, 0.25f);
            }
        }
    }
}
