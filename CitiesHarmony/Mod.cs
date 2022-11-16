using ICities;

namespace CitiesHarmony
{
    public class Mod : IUserMod
    {
        public string Name => $"Harmony 2.2.2-0";
        public string Description => "Mod Dependency";

        public Mod()
        {
            // Try to patch all Harmony 1 assemblies before they are used for patching
            // (this is possible due to a change in 1.15.1-f4 that constructs all mods first before calling OnEnabled on any of them)
            Installer.Run();
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            _ = new Settings(helper);
        }
        
    }
}
