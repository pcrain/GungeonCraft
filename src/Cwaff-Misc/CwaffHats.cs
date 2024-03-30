namespace CwaffingTheGungy;

public static class CwaffHats
{
    public static void Init()
    {
      Alexandria.cAPI.HatUtility.SetupConsoleCommands();

      if (C.DEBUG_BUILD)
      {
        EasyHat(name: "debug_hat",      offset: null);
        EasyHat(name: "debug_glasses",  offset: null, onEyes: true);
      }
      EasyHat(name: "toad_hat",       offset: new Vector2( 0f/16f, -3f/16f));
      EasyHat(name: "bunny_hat",      offset: new Vector2( 0f/16f, -3f/16f));
      EasyHat(name: "jester_hat",     offset: new Vector2( 0f/16f, -3f/16f));
      EasyHat(name: "saiyan_hat",     offset: new Vector2( 0f/16f, -4f/16f));
      EasyHat(name: "witch_hat",      offset: new Vector2(-1f/16f, -3f/16f));
      EasyHat(name: "bunny_ears_hat", offset: new Vector2( 0f/16f, -4f/16f));
      EasyHat(name: "shades",         offset: new Vector2( 0f/16f, -2f/16f), onEyes: true);
      EasyHat(name: "samus_hat",      offset: new Vector2( 0f/16f, -8f/16f));
      EasyHat(name: "goggles",        offset: new Vector2( 0f/16f, -3f/16f), onEyes: true);
      EasyHat(name: "santa_hat",      offset: new Vector2(-1f/16f, -3f/16f));
      EasyHat(name: "crown",          offset: new Vector2( 0f/16f, -2f/16f));
      EasyHat(name: "mitre",          offset: new Vector2( 0f/16f, -2f/16f));
      EasyHat(name: "tiara",          offset: new Vector2( 0f/16f, -2f/16f));
      EasyHat(name: "top_hat",        offset: new Vector2( 0f/16f, -2f/16f));
      EasyHat(name: "beret",          offset: new Vector2( 0f/16f, -3f/16f));
      EasyHat(name: "bowler",         offset: new Vector2( 0f/16f, -3f/16f));
      EasyHat(name: "halo",           offset: new Vector2( 0f/16f,  2f/16f));
      EasyHat(name: "kasa",           offset: new Vector2( 0f/16f, -3f/16f));
      EasyHat(name: "pirate_hat",     offset: new Vector2( 0f/16f, -3f/16f));
      EasyHat(name: "red_mage_hat",   offset: new Vector2( 0f/16f, -3f/16f));
      EasyHat(name: "traffic_cone",   offset: new Vector2( 0f/16f, -3f/16f));
      EasyHat(name: "cowl",           offset: new Vector2( 0f/16f, -9f/16f));
      EasyHat(name: "fishbowl",       offset: new Vector2( 0f/16f, -8f/16f));
      EasyHat(name: "bycocket",       offset: new Vector2(-1f/16f, -3f/16f));
      EasyHat(name: "cowboy_hat",     offset: new Vector2(-1f/16f, -3f/16f));
      EasyHat(name: "tattered_hat",   offset: new Vector2( 0f/16f, -1f/16f));
      EasyHat(name: "zucchetto",      offset: new Vector2( 0f/16f, -2f/16f));
    }

    private static void EasyHat(string name, Vector2? offset = null, bool onEyes = false)
    {
      GameObject hatObj = UnityEngine.Object.Instantiate(new GameObject());
      Hat hat = hatObj.AddComponent<Hat>();
      hat.hatName = name.Replace("_", " ").ToTitleCaseInvariant();
      hat.hatOffset = offset ?? Vector2.zero;
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
