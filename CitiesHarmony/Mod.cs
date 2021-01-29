using ICities;

namespace CitiesHarmony {
    public class Mod : IUserMod {
        public string Name => $"Harmony 2.0.4";

        public string Description => "Mod Dependency";

        // Install Harmony as soon as possible to avoid problems with mods not following the guidelines
        static Mod() => Installer.Run();
    }
}
