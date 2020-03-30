using System;
using System.Reflection;

namespace CitiesHarmony {
    internal static class TypeExtensions {
        public static FieldInfo GetFieldOrThrow(this Type type, string name) {
            return type?.GetField(name) ?? throw new Exception($"{name} field not found");
        }

        public static FieldInfo GetFieldOrThrow(this Type type, string name, BindingFlags flags) {
            return type?.GetField(name, flags) ?? throw new Exception($"{name} field not found");
        }

        public static MethodInfo GetMethodOrThrow(this Type type, string name) {
            return type?.GetMethod(name) ?? throw new Exception($"{name} method not found");
        }

        public static MethodInfo GetMethodOrThrow(this Type type, string name, BindingFlags flags) {
            return type?.GetMethod(name, flags) ?? throw new Exception($"{name} method not found");
        }

        public static MethodInfo GetMethodOrThrow(this Type type, string name, Type[] types) {
            return type?.GetMethod(name, types) ?? throw new Exception($"{name} method not found");
        }
    }
}
