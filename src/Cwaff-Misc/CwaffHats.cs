namespace CwaffingTheGungy;

public static class CwaffHats
{
    internal static Hat _PizzaHat = null;

    private static int _NumHats = 0;
    public static void Init()
    {
      if (C.DEBUG_BUILD)
      {
        EasyHat(name: "debug_hat",      offset: null, excluded: true);
        EasyHat(name: "debug_glasses",  offset: null, onEyes: true, excluded: true);
        EasyHat(name: "arrow_hat",      offset: null, fps: 8, locked: true, excluded: true);
        _NumHats -= 3;
      }
      EasyHat(name: "toad_hat",              offset: new IntVector2( 0, -3));
      EasyHat(name: "bunny_hood",            offset: new IntVector2( 0, -3));
      EasyHat(name: "jester_hat",            offset: new IntVector2( 0, -3));
      EasyHat(name: "saiyan_hat",            offset: new IntVector2( 0, -4));
      EasyHat(name: "wizard_hat",            offset: new IntVector2(-1, -3));
      EasyHat(name: "cat_ears",              offset: new IntVector2( 0, -4));
      EasyHat(name: "shades",                offset: new IntVector2( 0, -2), onEyes: true);
      EasyHat(name: "samus_hat",             offset: new IntVector2( 0, -8));
      EasyHat(name: "goggles",               offset: new IntVector2( 0, -3), onEyes: true);
      EasyHat(name: "santa_hat",             offset: new IntVector2( 0, -3));
      EasyHat(name: "crown",                 offset: new IntVector2( 0, -2));
      EasyHat(name: "mitre",                 offset: new IntVector2( 0, -2));
      EasyHat(name: "tiara",                 offset: new IntVector2( 0, -2));
      EasyHat(name: "top_hat",               offset: new IntVector2( 0, -2));
      EasyHat(name: "beret",                 offset: new IntVector2( 0, -3));
      EasyHat(name: "bowler",                offset: new IntVector2( 0, -3));
      EasyHat(name: "halo",                  offset: new IntVector2( 0,  2));
      EasyHat(name: "kasa",                  offset: new IntVector2( 0, -3));
      EasyHat(name: "pirate_hat",            offset: new IntVector2( 0, -3));
      EasyHat(name: "red_mage_hat",          offset: new IntVector2( 0, -3));
      EasyHat(name: "traffic_cone",          offset: new IntVector2( 0, -3));
      EasyHat(name: "cowl",                  offset: new IntVector2( 0, -9));
      EasyHat(name: "fishbowl",              offset: new IntVector2( 0, -8));
      EasyHat(name: "bycocket",              offset: new IntVector2(-1, -3));
      EasyHat(name: "fedora",                offset: new IntVector2(-1, -3));
      EasyHat(name: "tattered_hat",          offset: new IntVector2( 0, -1));
      EasyHat(name: "zucchetto",             offset: new IntVector2( 0, -2));
      EasyHat(name: "admiral_hat",           offset: new IntVector2( 0, -2));
      EasyHat(name: "apple",                 offset: new IntVector2( 0, -1));
      EasyHat(name: "blank_cap",             offset: new IntVector2( 0, -1));
      EasyHat(name: "bullet_kin_hat",        offset: new IntVector2( 0, -1));
      EasyHat(name: "german_general_hat",    offset: new IntVector2( 0, -3));
      EasyHat(name: "kokiri_cap",            offset: new IntVector2( 0, -3));
      EasyHat(name: "master_chief_helmet",   offset: new IntVector2( 0, -9));
      EasyHat(name: "mustache",              offset: new IntVector2( 1, -6), onEyes: true);
      EasyHat(name: "number_2_headband",     offset: new IntVector2( 0, -4));
      EasyHat(name: "pompom_hat",            offset: new IntVector2(-1, -4));
      EasyHat(name: "spike_crown",           offset: new IntVector2( 1, -1));
      EasyHat(name: "stovepipe",             offset: new IntVector2( 0, -5));
      EasyHat(name: "u_s_a_general_hat",     offset: new IntVector2( 0, -3), displayName: "U.S.A. General Hat");
      EasyHat(name: "witch_hat",             offset: new IntVector2( 0, -3));
      EasyHat(name: "aviator_helmet",        offset: new IntVector2( 0, -7));
      EasyHat(name: "basque_beret",          offset: new IntVector2( 0, -3));
      EasyHat(name: "beastmaster_hat",       offset: new IntVector2( 0, -3));
      EasyHat(name: "black_cat",             offset: new IntVector2( 0, -3));
      EasyHat(name: "ribbon",                offset: new IntVector2( 0, -3));
      EasyHat(name: "bullat_hat",            offset: new IntVector2( 0, -3));
      EasyHat(name: "camo_helmet",           offset: new IntVector2( 0, -5));
      EasyHat(name: "captain_falcon_helmet", offset: new IntVector2( 0, -6));
      EasyHat(name: "grenade_cap",           offset: new IntVector2( 0, -3));
      EasyHat(name: "gunzookie_helmet",      offset: new IntVector2( 0, -3));
      EasyHat(name: "megaman_helment",       offset: new IntVector2( 0, -8));
      EasyHat(name: "party_glasses",         offset: new IntVector2( 0, -3), onEyes: true);
      EasyHat(name: "bandana",               offset: new IntVector2( 0, -6));
      EasyHat(name: "winchester_hat",        offset: new IntVector2( 0, -3));
      EasyHat(name: "zero_helmet",           offset: new IntVector2( 0, -8));
      EasyHat(name: "wizbang_hat",           offset: new IntVector2( 0, -3));
      EasyHat(name: "police_hat",            offset: new IntVector2( 0, -6), autoFlip: true);
      EasyHat(name: "sheriff_hat",           offset: new IntVector2( 0, -6), autoFlip: true);
      EasyHat(name: "traffic_officer_hat",   offset: new IntVector2( 0, -6), autoFlip: true);
      EasyHat(name: "nitra_hat",             offset: new IntVector2( 0, -1));
      EasyHat(name: "rad_hat",               offset: new IntVector2( 1, -1));
      EasyHat(name: "blobulon_hat",          offset: new IntVector2( 0, -2));
      EasyHat(name: "cardboard_box",         offset: new IntVector2( 0, -3));
      EasyHat(name: "squire_helmet",         offset: new IntVector2( 0, -5), onEyes: true/*, depth: Hat.HatDepthType.ALWAYS_IN_FRONT*/);
      EasyHat(name: "jackolantern",          offset: new IntVector2( 0, -2), displayName: "Jack O'Lantern");
      _PizzaHat = EasyHat(name: "pizza_hat", offset: new IntVector2( 0, -3));

      EasyHat(name: "bicorne",               offset: new IntVector2( 0, -3));
      EasyHat(name: "chicken",               offset: new IntVector2( 0, -1));
      EasyHat(name: "coonskin",              offset: new IntVector2(-3, -5));
      EasyHat(name: "daisy",                 offset: new IntVector2( 0, -1));
      EasyHat(name: "discovered",            offset: new IntVector2( 0,  1));
      EasyHat(name: "doug_hat",              offset: new IntVector2( 0, -2));
      EasyHat(name: "dunce_cap",             offset: new IntVector2(-5, -4));
      EasyHat(name: "fez",                   offset: new IntVector2( 0, -1));
      EasyHat(name: "fish",                  offset: new IntVector2( 0, -3));
      EasyHat(name: "gameboy",               offset: new IntVector2( 0, -1));
      EasyHat(name: "nurse_hat",             offset: new IntVector2( 0, -2));
      EasyHat(name: "propeller_cap",         offset: new IntVector2( 0, -2), fps: 10);
      EasyHat(name: "saucepan",              offset: new IntVector2(-4, -1));
      EasyHat(name: "shellmet",              offset: new IntVector2( 0, -3));
      EasyHat(name: "siren",                 offset: new IntVector2( 0, -1), fps: 6);
      EasyHat(name: "sorceress_hat",         offset: new IntVector2( 0, -1));
      EasyHat(name: "tv_antennae",           offset: new IntVector2( 0, 0), displayName: "TV Antennae");
      EasyHat(name: "viking_helmet",         offset: new IntVector2( 0, -3));

      EasyHat(name: "the_infamous",          offset: new IntVector2( 0, -3), autoFlip: true);
      EasyHat(name: "bumbler",               offset: new IntVector2( 0, -3), autoFlip: true);

      EasyHat(name: "cowboy_hat",            offset: new IntVector2( 0, -4));
      EasyHat(name: "comedy_bowtie",         offset: new IntVector2( 0, -9), onEyes: true);
      EasyHat(name: "dress_bowtie",          offset: new IntVector2( 0, -9), onEyes: true);

      Lazy.DebugLog($"Successfully initialized {_NumHats} hats! C:");
    }

    internal static Hat EasyHat(string name, IntVector2? offset = null, bool onEyes = false, int fps = 1, bool locked = false, string displayName = null, bool excluded = false, bool? autoFlip = null, Hat.HatDepthType? depth = null)
    {
      ++_NumHats;
      return HatUtility.SetupHat(
        name: displayName ?? name.Replace("_", " ").ToTitleCaseInvariant(),
        spritePaths: Lazy.Combine(
          ResMap.Get($"{name}_south",     true),
          ResMap.Get($"{name}_north",     true),
          ResMap.Get($"{name}_east",      true),
          ResMap.Get($"{name}_west",      true),
          ResMap.Get($"{name}_northeast", true),
          ResMap.Get($"{name}_northwest", true)
          ),
        pixelOffset: offset,
        fps: fps,
        attachLevel: onEyes ? Hat.HatAttachLevel.EYE_LEVEL : Hat.HatAttachLevel.HEAD_TOP,
        depthType: depth ?? (onEyes ? Hat.HatDepthType.BEHIND_WHEN_FACING_BACK : Hat.HatDepthType.ALWAYS_IN_FRONT),
        flipHorizontalWithPlayer: autoFlip,
        // unlockFlags: locked ? new(){GungeonFlags.ITEMSPECIFIC_RAT_CHEESEWHEEL} : null,
        // unlockHint: "Sample Unlock Hint",
        // showSilhouetteWhenLocked: true,
        excludeFromHatRoom: excluded
        );
    }
}
