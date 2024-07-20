namespace CwaffingTheGungy;

public static class CwaffConfig
{
  internal static Gunfig _Gunfig = null;

  internal const string _SHOP_KEY = "Shop Spawning Behaviour";
  internal const string _SECONDARY_RELOAD = "Secondary Reload Button";
  internal const string _SECONDARY_RELOAD_DESC = "Change the ";

  public enum SecondaryReloadKey { None, Left, Right }
  internal static SecondaryReloadKey _SecondaryReload = SecondaryReloadKey.None;

  public static void Init()
  {
    _Gunfig = Gunfig.Get("GungeonCraft".WithColor(C.MOD_COLOR));

    _Gunfig.AddScrollBox(
      key     : _SECONDARY_RELOAD,
      options : new(){
        "Disabled",
        "Left Stick".Yellow(),
        "Right Stick".Yellow(),
        },
      info    : new(){
        "Disables all secondary reload buttons.\nRecommended if you reload with a trigger or bumper.".Green(),
        "Pressing the left stick triggers a reload.\nRecommended if you reload with a face button.\nNot recommended with dual stick blanks.".Green(),
        "Pressing the right stick triggers a reload.\nRecommended if you reload with a face button.\nNot recommended with dual stick blanks.".Green(),
        },
      callback: OnSecondaryReloadChange,
      updateType: Gunfig.Update.Immediate
      );

    _Gunfig.AddScrollBox(
      key     : HeckedMode._CONFIG_KEY,
      options : new(){
        "Disabled",
        "Hecked".Yellow(),
        // "Retrashed".Yellow(),
        },
      info    : new(){
        "Enemies spawn with their normal guns.\n\nTakes effect next run.".Green(),
        "Enemies spawn with completely random guns.\nNot for the faint of heart.\nTakes effect next run.".Green(),
        // "All enemies have strong guns and ignore stealth.\nAll bosses are jammed. All chests are fused.\nShop prices x10. Takes effect next run.".Green(),
        }
      );

    _Gunfig.AddScrollBox(
      key     : _SHOP_KEY,
      options : new(){
        "Default",
        "Classic".Yellow(),
        },
      info    : new(){
        "Companion, Barter, and Insurance Shops\nspawn randomly.\nTakes effect on game restart.".Green(),
        "Spawn Companion Shop floor 1, Barter Shop floor 2-3,\nand Insurance Shop with S or A tier item.\nTakes effect on game restart.".Green(),
        },
      updateType: Gunfig.Update.OnRestart
      );

    // Make sure our initial keybind preferences are set up for seconday reload button
    OnSecondaryReloadChange(_SECONDARY_RELOAD, _Gunfig.Value(_SECONDARY_RELOAD));

    // "All enemies are armed to the teeth.\nNowhere is safe.\nTakes effect next run.".Green(),
  }

  private static void OnSecondaryReloadChange(string key, string value)
  {
    if (value == "Left Stick")
      _SecondaryReload = SecondaryReloadKey.Left;
    else if (value == "Right Stick")
      _SecondaryReload = SecondaryReloadKey.Right;
    else
      _SecondaryReload = SecondaryReloadKey.None;
  }
}
