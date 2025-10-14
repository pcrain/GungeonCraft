namespace CwaffingTheGungy;

/// <summary>Class for temporary mod compatibility fixes that haven't been merged upstream yet</summary>
internal static class CompatibilityPatches
{
  const BindingFlags ANY_FLAGS = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

  internal static void Init(Harmony harmony)
  {
      foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
          string assemblyName = assembly.GetName().Name;
          if (assemblyName == "JuneLib")
              harmony.HandleJuneLibCompatibilityPatches(assembly);
      }
  }

  private static void HandleJuneLibCompatibilityPatches(this Harmony harmony, Assembly assembly)
  {
    Lazy.DebugLog($"  applying compatibility patches to JuneLib");
    if (assembly.GetType("JunePlayerEvents") is Type JPE && JPE.GetMethod("OverrideShootSingleProjectile", bindingAttr: ANY_FLAGS) is MethodInfo ossp)
    {
      //HACK: JuneLib compatibility until its full method override for ShootSingleProjectile gets replaced by ILManipulators
      Lazy.DebugLog($"    patching OverrideShootSingleProjectile()");
      Type sspp = typeof(ShootSingleProjectilePatch);
      harmony.Patch(ossp, ilmanipulator: new HarmonyMethod(sspp.GetMethod(nameof(ShootSingleProjectilePatch.ReduceSpreadWhenIdleIL), bindingAttr: ANY_FLAGS)));
      harmony.Patch(ossp, ilmanipulator: new HarmonyMethod(sspp.GetMethod(nameof(ShootSingleProjectilePatch.AimBotZeroSpreadIL), bindingAttr: ANY_FLAGS)));
      harmony.Patch(ossp, ilmanipulator: new HarmonyMethod(sspp.GetMethod(nameof(ShootSingleProjectilePatch.DynamicAccuracyIL), bindingAttr: ANY_FLAGS)));
      harmony.Patch(ossp, ilmanipulator: new HarmonyMethod(sspp.GetMethod(nameof(ShootSingleProjectilePatch.CheckFreebieIL), bindingAttr: ANY_FLAGS)));
      Lazy.DebugLog($"    patched OverrideShootSingleProjectile()");
    }
  }
}
