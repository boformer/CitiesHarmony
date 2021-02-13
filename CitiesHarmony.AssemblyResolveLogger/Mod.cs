using System;
using System.Reflection;
using ICities;

namespace CitiesHarmony.AssemblyResolveLogger
{
    public class Mod : IUserMod
    {
        public string Name => "AssemblyResolveLogger";
        public string Description => "";

        public void OnEnabled() {
            UnityEngine.Debug.Log($"Installing AssemblyResolveLogger");

            var vanillaResolver = typeof(BuildConfig).GetMethod("CurrentDomain_AssemblyResolve", BindingFlags.NonPublic | BindingFlags.Static);
            ResolveEventHandler dCSResolver = (ResolveEventHandler)Delegate.CreateDelegate(typeof(ResolveEventHandler), vanillaResolver);

            AppDomain.CurrentDomain.AssemblyResolve -= dCSResolver;
            AppDomain.CurrentDomain.TypeResolve -= dCSResolver;
            AppDomain.CurrentDomain.AssemblyResolve += LogAssemblyResolve;
            AppDomain.CurrentDomain.TypeResolve += LogAssemblyResolve;
            AppDomain.CurrentDomain.AssemblyResolve += dCSResolver;
            AppDomain.CurrentDomain.TypeResolve += dCSResolver;
        }

        public void OnDisabled() {
            UnityEngine.Debug.Log($"Uninstalling AssemblyResolveLogger");

            AppDomain.CurrentDomain.AssemblyResolve -= LogAssemblyResolve;
            AppDomain.CurrentDomain.TypeResolve -= LogAssemblyResolve;
        }

        private static Assembly LogAssemblyResolve(object sender, ResolveEventArgs args) {
            if (args.Name.Contains("0Harmony")) {
                UnityEngine.Debug.Log($"[AssemblyResolveLogger] Resolving '{args.Name}' from:\n{Environment.StackTrace}");
            }
            return null;
        }
    }
}
