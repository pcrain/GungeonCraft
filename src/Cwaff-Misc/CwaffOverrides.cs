namespace CwaffingTheGungy;

/// <summary>Class for managing various OverrideableBools for use with common patches</summary>
[HarmonyPatch]
public static class CwaffOverrides
{
  public static bool IsImmuneToExplosionDamage(this PlayerController player)
    => CwaffOverrideCache.Overrides(player).immuneToExplosionDamage.Value;
  public static void SetImmuneToExplosionDamage(this PlayerController player, bool value, string reason)
    => CwaffOverrideCache.Overrides(player).immuneToExplosionDamage.SetOverride(reason, value);
  public static bool IsImmuneToExplosionKnockback(this PlayerController player)
    => CwaffOverrideCache.Overrides(player).immuneToExplosionKnockback.Value;
  public static void SetImmuneToExplosionKnockback(this PlayerController player, bool value, string reason)
    => CwaffOverrideCache.Overrides(player).immuneToExplosionKnockback.SetOverride(reason, value);

  private class CwaffOverrideCache
  {
    public OverridableBool immuneToExplosionDamage = new(false);
    public OverridableBool immuneToExplosionKnockback = new(false);

    private static PlayerController _P1 = null;
    private static PlayerController _P2 = null;

    private static CwaffOverrideCache _P1Data = null;
    private static CwaffOverrideCache _P2Data = null;

    internal static CwaffOverrideCache Overrides(PlayerController player)
    {
      if (player.PlayerIDX == 0)
      {
        if (player != _P1)
        {
          _P1Data = new(); // new player instance == new set of overrides
          _P1 = player;
        }
        return _P1Data;
      }
      if (player != _P2)
      {
        _P2Data = new(); // new player instance == new set of overrides
        _P2 = player;
      }
      return _P2Data;
    }
  }

  /// <summary>Patch to prevent damage / knockback from explosions.</summary>
  [HarmonyPatch(typeof(Exploder), nameof(Exploder.HandleExplosion), MethodType.Enumerator)]
  [HarmonyILManipulator]
  private static void IgnoreExplosionDamageAndKnockbackIL(ILContext il)
  {
      ILCursor cursor = new ILCursor(il);

      // Ignore all damage from explosions
      if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<PlayerController>("IsEthereal")))
          return;
      cursor.Emit(OpCodes.Ldloc_S, (byte)13); // V_13 == the PlayerController
      cursor.CallPrivate(typeof(CwaffOverrides), nameof(CheckImmuneToExplosionDamage));
      cursor.CallPrivate(typeof(PatchHelpers), nameof(PatchHelpers.Or));

      // Ignore all knockback from explosions
      if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<ExplosionData>("preventPlayerForce")))
          return;
      cursor.Emit(OpCodes.Ldloc_S, (byte)13); // V_13 == the PlayerController
      cursor.CallPrivate(typeof(CwaffOverrides), nameof(CheckImmuneToExplosionKnockback));
      cursor.CallPrivate(typeof(PatchHelpers), nameof(PatchHelpers.Or));
  }

  private static bool CheckImmuneToExplosionDamage(PlayerController player) => player && player.IsImmuneToExplosionDamage();
  private static bool CheckImmuneToExplosionKnockback(PlayerController player) => player && player.IsImmuneToExplosionKnockback();
}
