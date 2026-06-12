namespace CwaffingTheGungy;

/// <summary>Class for managing various OverrideableBools for use with common patches</summary>
public static class CwaffOverrides
{
  public static bool IsImmuneToExplosions(this PlayerController player)
    => CwaffOverrideCache.Overrides(player).immuneToExplosions.Value;
  public static void SetImmuneToExplosions(this PlayerController player, bool value, string reason)
    => CwaffOverrideCache.Overrides(player).immuneToExplosions.SetOverride(reason, value);

  private class CwaffOverrideCache
  {
    public OverridableBool immuneToExplosions = new(false);

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
}
