using Mono.Cecil;
using Patch.API;
using System;
using System.IO;
using System.Reflection;

namespace CitiesHarmony {
    public class ASCPatch : IPatch {
        public int PatchOrderAsc { get; } = 1000;
        public AssemblyToPatch PatchTarget { get; } = new AssemblyToPatch("Assembly-CSharp", new Version());
        private ILogger logger_;

        public AssemblyDefinition Execute(AssemblyDefinition assemblyDefinition, ILogger logger, string patcherWorkingPath) {
            logger_ = logger;
            string workingPath_ = patcherWorkingPath;

            LoadDLL(Path.Combine(workingPath_, "CitiesHarmony.Harmony.dll"));
            LoadDLL(Path.Combine(workingPath_, "CitiesHarmony.dll"));
            InstallResolver();

            return assemblyDefinition;
        }

        public Assembly LoadDLL(string dllPath) {
            try {
                Assembly assembly;
                string symPath = dllPath + ".mdb";
                if(File.Exists(symPath)) {
                    logger_.Info("\nLoading " + dllPath + "\nSymbols " + symPath);
                    assembly = Assembly.Load(File.ReadAllBytes(dllPath), File.ReadAllBytes(symPath));
                } else {
                    logger_.Info("Loading " + dllPath);
                    assembly = Assembly.Load(File.ReadAllBytes(dllPath));
                }
                if(assembly != null) {
                    logger_.Info("Assembly " + assembly.FullName + " loaded.\n");
                } else {
                    logger_.Error("Assembly at " + dllPath + " failed to load.\n");
                }
                return assembly;
            } catch(Exception ex) {
                logger_.Error("Assembly at " + dllPath + " failed to load.\n" + ex.ToString());
                return null;
            }
        }

        public void InstallResolver() {
            AppDomain.CurrentDomain.AssemblyResolve += Resolver.ResolveHarmony;
            AppDomain.CurrentDomain.TypeResolve += Resolver.ResolveHarmony;
            logger_.Info("Installed Harmony resolver");
        }
    }
}
