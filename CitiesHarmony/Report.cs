using ColossalFramework.Plugins;
using HarmonyLib;
using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using static ColossalFramework.Plugins.PluginManager;

namespace CitiesHarmony
{
    public class Report
    {
        private MethodPatchInfo[] PatchInfos { get; }

        public static Report Get() => new Report();
        private Report()
        {
            var patchedMethods = Harmony.GetAllPatchedMethods().ToArray();
            PatchInfos = patchedMethods.Select(p => new MethodPatchInfo(p)).ToArray();
        }

        public bool IsPossibleConflicts(PluginInfo checkPlugin) => PatchInfos.Any(i => !i.Exclusive(checkPlugin));
        public Conflict[] GetConflicts(PluginInfo checkPlugin)
        {
            var conflicts = new Dictionary<PluginInfo, Conflict>();

            foreach (var patchInfo in PatchInfos)
            {
                if (patchInfo.Exclusive(checkPlugin))
                    continue;

                foreach (var info in patchInfo.Infos)
                {
                    if (info.Key == checkPlugin)
                        continue;

                    if (!conflicts.TryGetValue(info.Key, out var conflict))
                    {
                        conflict = new Conflict(info.Key);
                        conflicts[info.Key] = conflict;
                    }
                    if (!conflict.Infos.TryGetValue(patchInfo.Method, out var patchInfos))
                    {
                        patchInfos = new List<PatchInfo>();
                        conflict.Infos[patchInfo.Method] = patchInfos;
                    }
                    patchInfos.AddRange(info.Value);
                }
            }

            return conflicts.Values.ToArray();
        }

        public string PrintConflicts() => PrintConflicts(Assembly.GetExecutingAssembly());
        public string PrintConflicts(Assembly assembly)
        {
            if (GetPlugin(assembly) is PluginInfo plugin)
                return PrintConflicts(plugin);
            else
                return "Plugin not found";
        }
        private string Print(string title, params string[] messages) =>
            $"\n===================={(string.IsNullOrEmpty(title) ? "Start Harmony report" : title)}====================\n"
            + string.Join("\n--------------------------------------------------\n", messages)
            + "\n====================End Harmony report====================\n";
        public string PrintConflicts(PluginInfo checkPlugin)
        {
            var conflictText = GetConflicts(checkPlugin).Select(c => PrintConflict(c)).ToArray();
            var title = $"Start Harmony conflict report for {(checkPlugin.userModInstance as IUserMod).Name}";
            if (conflictText.Length != 0)
                return Print(title, conflictText);
            else
                return Print(title, "No one possible conflict found");
        }
        public string Print()
        {
            var infosText = PatchInfos.Select(i => i.Print()).ToArray();

            if (infosText.Length != 0)
                return Print(null, infosText);
            else
                return Print(null, "No one patched method");
        }
        private string PrintConflict(Conflict conflict)
        {
            var text = $"Possible conflict with {(conflict.Plugin.userModInstance as IUserMod)?.Name ?? "Unknown mod"} by methods:";

            foreach (var info in conflict.Infos)
            {
                text += $"\n--- {info.Key.DeclaringType.FullName}.{info.Key.Name}";
                foreach (var patch in info.Value)
                    text += $"\n------ [{patch.Type}] {patch.Method.DeclaringType.FullName}.{patch.Method.Name}";
            }
            return text;
        }

        private class MethodPatchInfo
        {
            public MethodBase Method { get; private set; }
            private Dictionary<PluginInfo, List<PatchInfo>> PluginsDic { get; } = new Dictionary<PluginInfo, List<PatchInfo>>();

            public IEnumerable<KeyValuePair<PluginInfo, PatchInfo[]>> Infos
            {
                get
                {
                    foreach (var pair in PluginsDic)
                        yield return new KeyValuePair<PluginInfo, PatchInfo[]>(pair.Key, pair.Value.ToArray());
                }
            }
            public bool IsSingle => PluginsDic.Count <= 1;

            public MethodPatchInfo(MethodBase method)
            {
                Method = method;

                var info = Harmony.GetPatchInfo(Method);

                GetPlugins(PatchType.PREFIX, info.Prefixes);
                GetPlugins(PatchType.POSTFIX, info.Postfixes);
                GetPlugins(PatchType.TRANSPILER, info.Transpilers);
                GetPlugins(PatchType.FINALIZER, info.Finalizers);
            }

            private void GetPlugins(PatchType type, IEnumerable<Patch> patches)
            {
                foreach (var patch in patches)
                {
                    var assembly = patch.PatchMethod.DeclaringType.Assembly;

                    if (GetPlugin(assembly) is PluginInfo plugin)
                    {
                        if (!PluginsDic.TryGetValue(plugin, out var methods))
                        {
                            methods = new List<PatchInfo>();
                            PluginsDic[plugin] = methods;
                        }

                        methods.Add(new PatchInfo(type, patch.PatchMethod));
                    }
                }
            }

            public bool Contains(PluginInfo plugin) => PluginsDic.ContainsKey(plugin);
            public bool Exclusive(PluginInfo plugin) => !Contains(plugin) || IsSingle;

            public string Print()
            {
                var text = $"{Method.DeclaringType.FullName}.{Method.Name} patched by mods:";
                foreach (var info in Infos)
                {
                    text += $"\n--- {(info.Key.userModInstance as IUserMod)?.Name ?? "Unknown"}";
                    foreach (var patch in info.Value)
                        text += $"\n------ [{patch.Type}] {patch.Method.DeclaringType.FullName}.{patch.Method.Name}";
                }

                return text;
            }
        }
        public class PatchInfo
        {
            public PatchType Type { get; }
            public MethodBase Method { get; }
            public PatchInfo(PatchType type, MethodBase method)
            {
                Type = type;
                Method = method;
            }
        }
        public enum PatchType
        {
            PREFIX,
            POSTFIX,
            TRANSPILER,
            FINALIZER

        }
        public class Conflict
        {
            public PluginInfo Plugin { get; }
            public Dictionary<MethodBase, List<PatchInfo>> Infos { get; } = new Dictionary<MethodBase, List<PatchInfo>>();

            public Conflict(PluginInfo plugin)
            {
                Plugin = plugin;
            }
        }

        private static PluginInfo GetPlugin(Func<PluginInfo, bool> predicate)
        {
            var plugins = PluginManager.instance.GetPluginsInfo().ToArray();
            return plugins.FirstOrDefault(predicate);
        }
        private static PluginInfo GetPlugin(Assembly assembly) => GetPlugin(p => (p.userModInstance as IUserMod)?.GetType().Assembly == assembly);
    }
}
