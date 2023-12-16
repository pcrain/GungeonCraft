namespace CwaffingTheGungy;

public static class QoLConfig
{
  public static ModConfig Gunfig = null;

  public const string FINGER_SAVER    = "Auto-fire Semi-Automatic Weapons";
  public const string PLAYER_TWO_CHAR = "Co-op Character";

  public static void Init()
  {
    Gunfig = ModConfig.GetConfigForMod("Quality of Life");

    Gunfig.AddToggle(key: FINGER_SAVER);
    Gunfig.AddToggle(key: PLAYER_TWO_CHAR);
  }
}
