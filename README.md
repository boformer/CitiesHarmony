# CitiesHarmony

This C:SL mod provides Andreas Pardeike's [Harmony patching library](https://github.com/pardeike/Harmony) (version 2.0.0.8) to all mods that require it.

It also hotpatches older Harmony versions (1.2.0.1) that are still used by various mods. This is necessary because Harmony 1.2.0.1 is incompatible with 2.0.0.8.

The associated Steam workshop item will be updated to the latest stable Harmony 2.x version in periodic intervals. By relying on a single `0Harmony.dll` for all mods, we can make sure that all mods use the latest version it that includes all bug fixes.

**By using auto-subscription, it is possible to migrate existing mods to Harmony 2.x without causing disruptions for users!**

## Documentation for Mod Developers

To use Harmony 2.0.0.8 in your mod, add the `Lib.Harmony` nuget package to your project. Make sure that the `0Harmony.dll` is **not** copied to the output directory when you build your mod.

Also add a reference `CitiesHarmony.API.dll` (this one must be copied to your output directory on post build).

Make sure that there are no references to `HarmonyLib` in your `IUserMod` implementation. 
Otherwise the mod could not be loaded if CitiesHarmony is not subscribed. Instead, it is recommended to keep `HarmonyLib`-related code (such as calls to `PatchAll` and `UnpatchAll`) in a separate static `Patcher` class.

Before making calls to harmony in your code, you need to query `CitiesHarmony.API.HarmonyHelper` to see if it is available. There are 3 different hooks for that purpose:

* `void HarmonyHelper.DoOnHarmonyReady(Action)`: Will invoke the passed action when Harmony 2.0.0.8 is ready to use. This hook should be called from `IUserMod.OnEnabled`. If the Harmony mod is not installed, this hook will attempt to auto-subscribe to it.
* `void HarmonyHelper.EnsureHarmonyInstalled()`: If you don't want to apply your patches in `IUserMod.OnEnabled`, call this method instead. It will perform the same auto-subscription as `DoOnHarmonyReady`
* `bool HarmonyHelper.IsHarmonyInstalled`: Returns `true` is Harmony is ready to be used. When queried, this hook will *not* attempt to auto-subscribe to the Harmony workshop item. Use this hook for all kinds of unpatching, applying patches in the `LoadingExtension` or while the simulation is running.

Take a look at the example mod in this repository for further inspiration!

It is recommened to add this mod as a dependency to your workshop item for transparency reasons.

### Alternative A: Applying your patches in `OnEnabled`/`OnDisabled`

```c#
public class Mod : IUserMod {
    // ...

    public void OnEnabled() {
        HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());
    }

    public void OnDisabled() {
        if (HarmonyHelper.IsHarmonyInstalled) Patcher.UnpatchAll();
    }
}
```

### Alternative B: Applying your patches in `LoadingExtensionBase`

```c#
public class Mod : LoadingExtensionBase, IUserMod {
    // ...

    public void OnEnabled() {
        HarmonyHelper.EnsureHarmonyInstalled();
    }

    public override void OnCreated(ILoading loading) {
        if (HarmonyHelper.IsHarmonyInstalled) Patcher.PatchAll();
    }

    public override void OnReleased() {
        if (HarmonyHelper.IsHarmonyInstalled) Patcher.UnpatchAll();
    }
}
```

### Example Patcher Class

```c#
public static class Patcher {
    private const string HarmonyId = "yourname.YourModName";
    private static bool patched = false;

    public static void PatchAll() {
        if (patched) return;

        patched = true;
        var harmony = new Harmony(HarmonyId);
        harmony.PatchAll(typeof(Patcher).GetType().Assembly); // you can also do manual patching here!
    }

    public static void UnpatchAll() {
        if (!patched) return;

        var harmony = new Harmony(HarmonyId);
        harmony.UnpatchAll(HarmonyId);
        patched = false;
    }
}
```
