using Mono.Cecil;
using Patch.API;
using System;
using System.IO;
using System.Reflection;

namespace CitiesHarmony {
    public class ASCPatch : IPatch {
        public int PatchOrderAsc { get; } = 1000;
        public AssemblyToPatch PatchTarget { get; } = new AssemblyToPatch("Assembly-CSharp", new Version());
        private ILogger _logger;

        public AssemblyDefinition Execute(AssemblyDefinition assemblyDefinition, ILogger logger, string patcherWorkingPath) {
            _logger = logger;
            var modPath = Path.GetDirectoryName(patcherWorkingPath);
            var harmonyAssembly = LoadDLL(Path.Combine(modPath, "CitiesHarmony.Harmony.dll"));
            if(harmonyAssembly != null) InstallResolver(harmonyAssembly);

            return assemblyDefinition;
        }

        public Assembly LoadDLL(string dllPath) {
            try {
                Assembly assembly;
                string symPath = dllPath + ".mdb";
                if(File.Exists(symPath)) {
                    _logger.Info("\nLoading " + dllPath + "\nSymbols " + symPath);
                    assembly = Assembly.Load(File.ReadAllBytes(dllPath), File.ReadAllBytes(symPath));
                } else {
                    _logger.Info("Loading " + dllPath);
                    assembly = Assembly.Load(File.ReadAllBytes(dllPath));
                }
                _logger.Info("Assembly " + assembly.FullName + " loaded.\n");
                return assembly;
            } catch(Exception ex) {
                _logger.Error("Assembly at " + dllPath + " failed to load.\n" + ex.ToString());
                return null;
            }
        }

        public void InstallResolver(Assembly harmonyAssembly) {
            ResolveEventHandler resolver = (object sender, ResolveEventArgs args) => {
                try {
                    if (IsHarmony2(new AssemblyName(args.Name))) {
                        UnityEngine.Debug.Log($"[CitiesHarmony.IPatch] Resolved '{args.Name}' to {harmonyAssembly}");
                        return harmonyAssembly;
                    }
                } catch (Exception e) {
                    UnityEngine.Debug.LogException(e);
                }
                return null;
            };
            AppDomain.CurrentDomain.AssemblyResolve += resolver;
            AppDomain.CurrentDomain.TypeResolve += resolver;
            _logger.Info("Installed Harmony resolver");
        }

        private const string HarmonyName = "0Harmony";
        private const string HarmonyForkName = "CitiesHarmony.Harmony";
        private static readonly Version MinHarmonyVersionToHandle = new Version(2, 0, 0, 8);

        private static bool IsHarmony2(AssemblyName assemblyName) {
            return (assemblyName.Name == HarmonyName || assemblyName.Name == HarmonyForkName) &&
                   assemblyName.Version >= MinHarmonyVersionToHandle;
        }
    }
}
