using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static ColossalFramework.Plugins.PluginManager;

namespace CitiesHarmony
{
    public class Settings
    {
        private PluginInfo[] HarmonyPlugins { get; set; }

        private UIDropDown ModList { get; set; }
        private UIButton ConflictButton { get; set; }

        public Settings(UIHelperBase helper)
        {
            HarmonyPlugins = GetHarmonyMods().ToArray();

            var reportGroup = helper.AddGroup("Report");
            reportGroup.AddButton("Print Harmony report", OnReport);

            var conflictGroup = helper.AddGroup("Conflict report");
            ModList = conflictGroup.AddDropdown("Select mod for conflict report", HarmonyPlugins.Select(p => (p.userModInstance as IUserMod).Name).ToArray(), -1, OnIndexChange) as UIDropDown;
            ModList.width = 500;

            ConflictButton = conflictGroup.AddButton("Print Harmony conflict report", OnConflictReport) as UIButton;

            OnIndexChange(-1);
        }

        private void OnReport()
        {
            var report = Report.Get();
            Debug.Log(report.Print());
        }
        private void OnConflictReport()
        {
            var report = Report.Get();
            Debug.Log(report.PrintConflicts(HarmonyPlugins[ModList.selectedIndex]));
        }
        private void OnIndexChange(int index)
        {
            ConflictButton.isEnabled = index >= 0;
        }
        private IEnumerable<PluginInfo> GetHarmonyMods()
        {
            var plugins = PluginManager.instance.GetPluginsInfo().ToArray();
            foreach (var plugin in plugins)
            {
                if (plugin.userModInstance is IUserMod mod)
                {
                    var assembly = mod.GetType().Assembly;
                    if (assembly.GetReferencedAssemblies().Any(a => RequestHarmony(a)))
                        yield return plugin;
                }
            }

            bool RequestHarmony(AssemblyName assemblyName) => (assemblyName.Name == "0Harmony" || assemblyName.Name == "CitiesHarmony.Harmony") && assemblyName.Version.Major >= 2;
        }
    }
}
