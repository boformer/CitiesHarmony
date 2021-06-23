using ICities;
using System.Linq;

namespace CitiesHarmony
{
    public class Mod : IUserMod
    {
        public string Name => $"Harmony 2.0.4-5";
        public string Description => "Mod Dependency";


        public void OnSettingsUI(UIHelperBase helper)
        {
            _ = new Settings(helper);
        }
        
    }
}
