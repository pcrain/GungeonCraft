namespace CwaffingTheGungy;

public static class CwaffHats
{
    public static void Init()
    {
      Alexandria.cAPI.HatUtility.SetupConsoleCommands();

      // if (C.DEBUG_BUILD)
      // {
      //   EasyHat(name: "debug_hat",      offset: null);
      //   EasyHat(name: "debug_glasses",  offset: null, onEyes: true);
      // }
      EasyHat(name: "toad_hat",            offset: new Vector2( 0, -3));
      EasyHat(name: "bunny_hood",          offset: new Vector2( 0, -3));
      EasyHat(name: "jester_hat",          offset: new Vector2( 0, -3));
      EasyHat(name: "saiyan_hat",          offset: new Vector2( 0, -4));
      EasyHat(name: "wizard_hat",          offset: new Vector2(-1, -3));
      EasyHat(name: "bunny_ears_hat",      offset: new Vector2( 0, -4));
      EasyHat(name: "shades",              offset: new Vector2( 0, -2), onEyes: true);
      EasyHat(name: "samus_hat",           offset: new Vector2( 0, -8));
      EasyHat(name: "goggles",             offset: new Vector2( 0, -3), onEyes: true);
      EasyHat(name: "santa_hat",           offset: new Vector2(-1, -3));
      EasyHat(name: "crown",               offset: new Vector2( 0, -2));
      EasyHat(name: "mitre",               offset: new Vector2( 0, -2));
      EasyHat(name: "tiara",               offset: new Vector2( 0, -2));
      EasyHat(name: "top_hat",             offset: new Vector2( 0, -2));
      EasyHat(name: "beret",               offset: new Vector2( 0, -3));
      EasyHat(name: "bowler",              offset: new Vector2( 0, -3));
      EasyHat(name: "halo",                offset: new Vector2( 0,  2));
      EasyHat(name: "kasa",                offset: new Vector2( 0, -3));
      EasyHat(name: "pirate_hat",          offset: new Vector2( 0, -3));
      EasyHat(name: "red_mage_hat",        offset: new Vector2( 0, -3));
      EasyHat(name: "traffic_cone",        offset: new Vector2( 0, -3));
      EasyHat(name: "cowl",                offset: new Vector2( 0, -9));
      EasyHat(name: "fishbowl",            offset: new Vector2( 0, -8));
      EasyHat(name: "bycocket",            offset: new Vector2(-1, -3));
      EasyHat(name: "cowboy_hat",          offset: new Vector2(-1, -3));
      EasyHat(name: "tattered_hat",        offset: new Vector2( 0, -1));
      EasyHat(name: "zucchetto",           offset: new Vector2( 0, -2));
      EasyHat(name: "admiral_hat",         offset: new Vector2( 0, -2));
      EasyHat(name: "apple",               offset: new Vector2( 0, -1));
      EasyHat(name: "blank_cap",           offset: new Vector2( 0, -1));
      EasyHat(name: "bullet_kin_hat",      offset: new Vector2( 0, -1));
      EasyHat(name: "german_general_hat",  offset: new Vector2( 0, -3));
      EasyHat(name: "kokiri_cap",          offset: new Vector2( 0, -3));
      EasyHat(name: "master_chief_helmet", offset: new Vector2( 0, -9));
      EasyHat(name: "mustache",            offset: new Vector2( 0, -6), onEyes: true);
      EasyHat(name: "number_2_headband",   offset: new Vector2( 0, -4));
      EasyHat(name: "pompom_hat",          offset: new Vector2(-1, -4));
      EasyHat(name: "spike_crown",         offset: new Vector2( 1, -1));
      EasyHat(name: "stovepipe",           offset: new Vector2( 0, -5));
      EasyHat(name: "u_s_a_general_hat",   offset: new Vector2( 0, -3));
      EasyHat(name: "witch_hat",           offset: new Vector2( 0, -3));
    }

    private static void EasyHat(string name, Vector2? offset = null, bool onEyes = false)
    {
      GameObject hatObj = UnityEngine.Object.Instantiate(new GameObject());
      Hat hat = hatObj.AddComponent<Hat>();
      hat.hatName = name.Replace("_", " ").ToTitleCaseInvariant();
      hat.hatOffset = 0.0625f * (offset ?? Vector2.zero);
      if (onEyes)
      {
        hat.attachLevel = Hat.HatAttachLevel.EYE_LEVEL;
        hat.hatDepthType = Hat.HatDepthType.BehindWhenFacingBack;
      }
      HatUtility.SetupHatSprites(
        spritePaths: Lazy.Combine(
          ResMap.Get($"{name}_south",     true),
          ResMap.Get($"{name}_north",     true),
          ResMap.Get($"{name}_east",      true),
          ResMap.Get($"{name}_west",      true),
          ResMap.Get($"{name}_northeast", true),
          ResMap.Get($"{name}_northwest", true)
        ),
        hatObj: hatObj,
        fps: 1);
      if (hat.hatDirectionality != Hat.HatDirectionality.NONE && hat.hatDirectionality != Hat.HatDirectionality.TWOWAYVERTICAL)
        hat.flipHorizontalWithPlayer = false;
      HatUtility.AddHatToDatabase(hatObj);
    }
}
