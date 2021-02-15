using System;
using System.Reflection;
using ICities;

namespace CitiesHarmony.DomainPoker {
    public class Mod : IUserMod {
        public string Name => "DomainPoker";
        public string Description => "";

        public void OnEnabled() {
            UnityEngine.Debug.Log($"[DomainPoker] Get ready to be poked!");

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
                try {
                    UnityEngine.Debug.Log($"Calling GetExportedTypes() for {asm}");
                    asm.GetExportedTypes();
                    UnityEngine.Debug.Log($"GetExportedTypes successful!");

                    UnityEngine.Debug.Log($"Calling GetTypes() for {asm}");
                    asm.GetTypes();
                    UnityEngine.Debug.Log($"GetTypes successful!");
                } catch (Exception e) {
                    UnityEngine.Debug.LogException(e);
                }
            }
        }
    }
}
