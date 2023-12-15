namespace CwaffingTheGungy;

public static class QoLConfig
{
  public static ModConfig Gunfig = null;

  public const string FINGER_SAVER = "Auto-fire Semi-Automatic Weapons";

  public static void Init()
  {
    Gunfig = ModConfig.GetConfigForMod("Quality of Life");

    Gunfig.AddToggle(key: FINGER_SAVER);
  }
}
