namespace CwaffingTheGungy;

public static class QoLConfig
{
  public static ModConfig Gunfig = null;

  public const string MENU_SOUNDS     = "Better Menu Sounds";
  public const string FINGER_SAVER    = "Auto-fire Semi-Automatic Weapons";
  public const string PLAYER_TWO_CHAR = "Co-op Character";
  public const string HEROBRINE       = "Disable Herobrine";

  public static void Init()
  {
    Gunfig = ModConfig.GetConfigForMod("Quality of Life");

    Gunfig.AddToggle(key: MENU_SOUNDS);
    Gunfig.AddToggle(key: FINGER_SAVER);
    Gunfig.AddScrollBox(key: PLAYER_TWO_CHAR, options: new(){
      "Cultist",
      "Pilot".Yellow(),
      "Marine".Yellow(),
      "Convict".Yellow(),
      "Hunter".Yellow(),
      "Bullet".Yellow(),
      "Robot".Yellow(),
      "Paradox".Yellow(),
      "Gunslinger".Yellow(),
      });
    Gunfig.AddToggle(key: HEROBRINE, label: HEROBRINE.Red());
  }
}
