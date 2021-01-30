using Mono.Cecil;
using Mono.Cecil.Cil;
using Patch.API;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CitiesHarmony.API {
    public class CMPatch : IPatch {
        public int PatchOrderAsc { get; } = 1000;
        public AssemblyToPatch PatchTarget { get; } = new AssemblyToPatch("ColossalManaged", new Version());
        private ILogger logger_;
        private string workingPath_;
        private static Version MIN_HARMONY1_VERSION = new Version(1, 1, 0, 0);
        private static string HARMONY1_NAME = "0Harmony";

        public AssemblyDefinition Execute(AssemblyDefinition assemblyDefinition, ILogger logger, string patcherWorkingPath) {
            logger_ = logger;
            workingPath_ = patcherWorkingPath;
            LoadDLL(Path.Combine(workingPath_, "0Harmony1.2.0.1.dll"));
            LoadPluginPatch(assemblyDefinition);
            return assemblyDefinition;
        }

        public Assembly LoadDLL(string dllPath) {
            logger_.Info("Loading " + dllPath);
            Assembly assembly = Assembly.Load(File.ReadAllBytes(dllPath));
            if (assembly != null) {
                logger_.Info("Assembly " + assembly.FullName + " loaded.\n");
            } else {
                logger_.Error("Assembly at " + dllPath + " failed to load.\n");
            }
            return assembly;
        }

        /// <summary>
        /// patch LoadPlugin to replace old harmony libraries with harmony 1.2.0.1
        /// this will provide better backward compatiblity even with very old mods.
        /// </summary>
        /// <param name="CM"></param>
        void LoadPluginPatch(AssemblyDefinition CM) {
            logger_.Info("LoadPluginPatch() called ...");
            var module = CM.MainModule;
            //private Assembly ColossalFramework.Plugins.PluginManager.LoadPlugin(string dllPath)
            var type = module.GetType("ColossalFramework.Packaging.PackageManager");
            var mLoadPlugin = type.Methods.Single(_m => _m.Name.StartsWith("LoadPlugin"));
            ILProcessor ilProcessor = mLoadPlugin.Body.GetILProcessor();
            var instructions = mLoadPlugin.Body.Instructions;

            Instruction first = instructions.First(); // first instruction of the original method
            Instruction loadDllPath = Instruction.Create(OpCodes.Ldarg_1);
            MethodInfo mIsOldHarmony = GetType().GetMethod(nameof(IsOldHarmony));
            Instruction callIsOldHarmony = Instruction.Create(OpCodes.Call, module.ImportReference(mIsOldHarmony));
            Instruction branchToFirst = Instruction.Create(OpCodes.Brfalse, first);
            Instruction loadNull = Instruction.Create(OpCodes.Ldnull);
            Instruction ret = Instruction.Create(OpCodes.Ret);

            /* 
            if(IsOldHarmony(dllPath))
                return null;
            [ first instruction of the original method ]
            [ rest of the instructions ]
            */
            ilProcessor.InsertBefore(first, loadDllPath);
            ilProcessor.InsertAfter(loadDllPath, callIsOldHarmony);
            ilProcessor.InsertAfter(callIsOldHarmony, branchToFirst);
            ilProcessor.InsertAfter(branchToFirst, loadNull);
            ilProcessor.InsertAfter(loadNull, ret); // return null

            logger_.Info("LoadPluginPatch() succeeded!");
        }

        public static bool IsOldHarmony(string path) {
            var asm = AssemblyDefinition.ReadAssembly(path);
            return asm.Name.Name == HARMONY1_NAME && asm.Name.Version < MIN_HARMONY1_VERSION;
        }

    }
}
