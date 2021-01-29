﻿using ICities;

namespace CitiesHarmony {
    public class Mod : IUserMod {
        public string Name => $"Harmony 2.0.4";

        public string Description => "Mod Dependency";

        // comment out to install harmony right at the begining.
        static Mod() => Installer.Run();
    }
}
