namespace CwaffingTheGungy;

/*
  TODO:
    - fix colliding with enemies after taking damage mid bounce
    - (eventually) add punchout sprites
*/

#if DEBUG
[HarmonyPatch]
static class MetaInjectionDataPreprocessRunPatch
{
    [HarmonyPatch(typeof(MetaInjectionData), nameof(MetaInjectionData.PreprocessRun))]
    static void Prefix(MetaInjectionData __instance, bool doDebug)
    {
        if (!GameManager.SKIP_FOYER)
            return;
        GameManager.PlayerPrefabForNewGame = CharacterBuilder.storedCharacters["rogo"].Second;
        Lazy.DebugLog($"  quickstart character set to {GameManager.PlayerPrefabForNewGame.name} for debugging");
    }
}
#endif

public class Rogo
{
  public static string Name = "Rogo";
  public static readonly PlayableCharacters Character = Name.ExtendEnum<PlayableCharacters>();

  public static void Init()
  {
    // Item setup
    PogoStick.Init();
    PogoGun.Init();

    // Basic setup
    CustomCharacterData data = new() {
      baseCharacter     = PlayableCharacters.Robot,
      identity          = Character,
      name              = Name,
      nameShort         = Name,
      nickname          = Name,
      health            = 2,
      armor             = 2,
      foyerPos          = new Vector3(30.25f, 21.25f),
      loadout           = new(){
        new(Lazy.Pickup<PogoGun>(), false),
        new(Lazy.Pickup<PogoStick>(), false),
      },
      idleDoer          = new GameObject().RegisterPrefab().InitComponent<CharacterSelectIdleDoer>(i => {
          i.phases = new CharacterSelectIdlePhase[]{
            new(){ inAnimation = "select_error",    holdMin = 0, holdMax = 0 },
            new(){ inAnimation = "select_off",      holdMin = 1, holdMax = 1 },
            new(){ inAnimation = "select_casing",   holdMin = 0, holdMax = 0 },
            new(){ inAnimation = "select_headspin", holdMin = 0, holdMax = 0 },
            new(){ inAnimation = "select_stargaze", holdMin = 2, holdMax = 4 },
          };
      }),
    };
    PlayerController pc = data.MakeNewCustomCharacter();

    // Sprite setup
    pc.InitAnimations(data, _AnimFPS)
      .AddOrReplaceAnimation("doorway", "rogo_doorway", fps: 10, loopStart: 8)
      .AddOrReplaceAnimation("select_error", "rogo_select_error", fps: 10)
      .AddOrReplaceAnimation("select_off", "rogo_select_off", fps: 10)
      .SetLoopPoint("select_casing", 0)
      .SetAudio("dodge",          "rogo_dodge_sound", 0)
      .SetAudio("dodge_bw",       "rogo_dodge_sound", 0)
      .SetAudio("dodge_left",     "rogo_dodge_sound", 0)
      .SetAudio("dodge_left_bw",  "rogo_dodge_sound", 0)
      .SetAudio("run_down",       "rogo_step_sound", 3, 7)
      .SetAudio("run_down_hand",  "rogo_step_sound", 3, 7)
      .SetAudio("run_right",      "rogo_step_sound", 3, 7)
      .SetAudio("run_right_bw",   "rogo_step_sound", 3, 7)
      .SetAudio("run_right_hand", "rogo_step_sound", 3, 7)
      .SetAudio("run_up",         "rogo_step_sound", 3, 7)
      .SetAudio("run_up_hand",    "rogo_step_sound", 3, 7)
      .SetAudio("pitfall",        "Play_Fall", 0)
      .SetAudio("pitfall_down",   "Play_Fall", 0)
      .SetAudio("pitfall_return", "rogo_shake_sound", 3, 7, 11, 15)
      .SetAudio("pet",            "rogo_pet_sound", 1)
      .SetAudio("select_choose",  "rogo_off_balance_sound", 9, 11, 13)
      .SetAudio("select_choose",  "rogo_more_off_balance_sound", 15, 17, 19)
      .SetAudio("select_choose",  "rogo_stumble_sound", 22)
      .SetAudio("select_choose",  "rogo_recover_sound", 28)
      ;

    // Hat offset setup
    pc.SetupHatOffsets(0, -2, 0, -7);
    pc.AddHatOffset("rogo_run_front_001",  1);
    pc.AddHatOffset("rogo_run_front_002",  2);
    pc.AddHatOffset("rogo_run_front_003",  1);
    pc.AddHatOffset("rogo_run_front_004",  0);
    pc.AddHatOffset("rogo_run_front_005", -1);
    pc.AddHatOffset("rogo_run_front_006", -2);
    pc.AddHatOffset("rogo_run_front_007", -1);
    pc.AddHatOffset("rogo_run_front_008",  0);
    pc.AddHatOffset("rogo_run_back_001",   1);
    pc.AddHatOffset("rogo_run_back_002",   2);
    pc.AddHatOffset("rogo_run_back_003",   1);
    pc.AddHatOffset("rogo_run_back_004",   0);
    pc.AddHatOffset("rogo_run_back_005",  -1);
    pc.AddHatOffset("rogo_run_back_006",  -2);
    pc.AddHatOffset("rogo_run_back_007",  -1);
    pc.AddHatOffset("rogo_run_back_008",   0);
    pc.AddHatOffset("rogo_run_side_001",   1);
    pc.AddHatOffset("rogo_run_side_002",   2);
    pc.AddHatOffset("rogo_run_side_003",   1);
    pc.AddHatOffset("rogo_run_side_004",   0);
    pc.AddHatOffset("rogo_run_side_005",  -1);
    pc.AddHatOffset("rogo_run_side_006",  -2);
    pc.AddHatOffset("rogo_run_side_007",  -1);
    pc.AddHatOffset("rogo_run_side_008",   0);
    pc.AddHatOffset("rogo_run_bw_001",     1);
    pc.AddHatOffset("rogo_run_bw_002",     2);
    pc.AddHatOffset("rogo_run_bw_003",     1);
    pc.AddHatOffset("rogo_run_bw_004",     0);
    pc.AddHatOffset("rogo_run_bw_005",    -1);
    pc.AddHatOffset("rogo_run_bw_006",    -2);
    pc.AddHatOffset("rogo_run_bw_007",    -1);
    pc.AddHatOffset("rogo_run_bw_008",     0);
    pc.AddHatOffset("rogo_item_get_001",   1);
    pc.AddHatOffset("rogo_item_get_002",   2);
    pc.AddHatOffset("rogo_item_get_003",   1);
    pc.AddHatOffset("rogo_item_get_004",  -1);
    pc.AddHatOffset("rogo_item_get_005",  -2);
    pc.AddHatOffset("rogo_item_get_006",  -1);
    pc.AddHatOffset("rogo_item_get_007",   1);
    pc.AddHatOffset("rogo_item_get_008",   2);
    pc.AddHatOffset("rogo_item_get_009",   1);
    pc.AddHatOffset("rogo_item_get_010",  -1);
    pc.AddHatOffset("rogo_item_get_011",  -2);
    pc.AddHatOffset("rogo_item_get_012",  -1);
  }

  private static Dictionary<string, float> _AnimFPS = new()
  {
    { "death",                6 },
    { "death_coop",           6 },
    { "death_shot",           18 },
    { "dodge",                18 }, //NOTE: FPS overrides apparently have no effect on dodge roll animations
    { "dodge_bw",             18 },
    { "dodge_left",           24 },
    { "dodge_left_bw",        24 },
    // { "doorway",              4 },
    // { "ghost_idle_back",      4 },
    // { "ghost_idle_back_left", 4 },
    // { "ghost_idle_back_right",4 },
    // { "ghost_idle_front",     4 },
    // { "ghost_idle_left",      4 },
    // { "ghost_idle_right",     4 },
    { "ghost_sneeze_left",    12 },
    { "ghost_sneeze_right",   12 },
    { "idle",                 9 },
    { "idle_backward",        9 },
    { "idle_backward_hand",   9 },
    { "idle_bw",              9 },
    { "idle_forward",         9 },
    { "idle_forward_hand",    9 },
    { "idle_hand",            9 },
    // { "item_get",             4 },
    // { "jetpack_down",         4 },
    // { "jetpack_right",        4 },
    // { "jetpack_right_bw",     4 },
    // { "jetpack_up",           4 },
    // { "past_off",             4 },
    { "pet",                  4 },
    // { "pitfall",              4 },
    // { "pitfall_down",         4 },
    { "pitfall_return",       27 },
    { "run_down",             20 },
    { "run_down_hand",        20 },
    { "run_right",            20 },
    { "run_right_bw",         20 },
    { "run_right_hand",       20 },
    { "run_up",               20 },
    { "run_up_hand",          20 },
    // { "select_casing",        4 },  // a.k.a. robot_select_tummy
    { "select_choose",        14 },
    { "select_choose_long",   9 },
    // { "select_headspin",      4 },
    { "select_idle",          9 },
    { "select_stargaze",      9 },
    { "select_stargaze_cry",  9 },
    // { "slide_down",           4 },
    // { "slide_right",          4 },
    // { "slide_up",             4 },
    // { "spinfall",             4 },
    // { "spit_out",             4 },
    // { "tablekick_down",       4 },
    // { "tablekick_down_hand",  4 },
    // { "tablekick_right",      4 },
    // { "tablekick_right_hand", 4 },
    // { "tablekick_up",         4 },
    // { "timefall",             4 },
  };
}
