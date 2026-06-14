namespace CwaffingTheGungy;

// #if DEBUG
// [HarmonyPatch]
// static class MetaInjectionDataPreprocessRunPatch
// {
//     [HarmonyPatch(typeof(MetaInjectionData), nameof(MetaInjectionData.PreprocessRun))]
//     static void Prefix(MetaInjectionData __instance, bool doDebug)
//     {
//         if (!GameManager.SKIP_FOYER)
//             return;
//         GameManager.PlayerPrefabForNewGame = CharacterBuilder.storedCharacters["rancher"].Second;
//         Lazy.DebugLog($"  quickstart character set to {GameManager.PlayerPrefabForNewGame.name} for debugging");
//     }
// }
// #endif

public class Rancher
{
  public static string Name = "Rancher";
  public static readonly PlayableCharacters Character = Name.ExtendEnum<PlayableCharacters>();

  public static void Init()
  {
    // Enemy Setup
    Slimybois.Init();

    // Gun Setup
    Vacpack.Init();
    PortableHydroTurret.Init();

    // Character setup
    CustomCharacterData data = new() {
      baseCharacter     = PlayableCharacters.Robot,
      identity          = Character,
      name              = "The " + Name,
      nameShort         = Name,
      nickname          = Name,
      health            = 3,
      armor             = 0,
      foyerPos          = new Vector3(33.0f, 21.25f),
      loadout           = new(){
        new(Lazy.Pickup<Vacpack>(), false),
        new(Lazy.Pickup<PortableHydroTurret>(), false),
      },
      idleDoer          = new GameObject().RegisterPrefab().InitComponent<CharacterSelectIdleDoer>(i => {
          i.phases = new CharacterSelectIdlePhase[]{
            new(){ inAnimation = "select_slime",    holdMin = 0, holdMax = 0 },
            new(){ inAnimation = "select_run",    holdMin = 2, holdMax = 4 },
          };
      }),
      stats = new(){
        {PlayerStats.StatType.RateOfFire, 0.5f },
        {PlayerStats.StatType.ChargeAmountMultiplier, 0.5f },
        {PlayerStats.StatType.ReloadSpeed, 2.0f },
        {PlayerStats.StatType.Accuracy, 2.0f },
      },
    };
    PlayerController pc = data.MakeNewCustomCharacter();
    pc.gunAttachPoint.localPosition = new Vector3(0.5f, 0.5f, 0.0f); // NOTE: fix wonky hand offset, integrate directly into MakeNewCustomCharacter

    // Sprite setup
    pc.InitAnimations(data, _AnimFPS, remove: ["select_stargaze", "select_casing", "select_stargaze_cry", "select_headspin", "spinfall"])
      .AddOrReplaceAnimation("doorway", "rancher_doorway", fps: 10, loopStart: 8)
      .AddOrReplaceAnimation("spinfall", "rancher_spinfall", fps: 16, loopStart: 0)
      .AddOrReplaceAnimation("select_slime", "rancher_select_slime", fps: 11, loopStart: 0)
      .AddOrReplaceAnimation("select_run", "rancher_select_run", fps: 16, loopStart: 0)
      .SetAudio("dodge",          "Play_Leap", 0)
      .SetAudio("dodge_bw",       "Play_Leap", 0)
      .SetAudio("dodge_left",     "Play_Leap", 0)
      .SetAudio("dodge_left_bw",  "Play_Leap", 0)
      .SetAudio("dodge",          "Play_Roll", 4)
      .SetAudio("dodge_bw",       "Play_Roll", 4)
      .SetAudio("dodge_left",     "Play_Roll", 4)
      .SetAudio("dodge_left_bw",  "Play_Roll", 4)
      .SetAudio("pitfall",        "Play_Fall", 0)
      .SetAudio("pitfall_down",   "Play_Fall", 0)
      .SetAudio("pitfall_return", "Play_Respawn", 0)
      .SetAudio("pet",            "Play_CHR_fool_voice_01", 1)
      ;

    // Hat offset setup
    pc.SetupHatOffsets(0, -6, 0, -11);
    pc.AddHatOffset("rancher_run_side_002",  0, 1);
    pc.AddHatOffset("rancher_run_side_003",  0, 1);
    pc.AddHatOffset("rancher_run_side_005",  1, 1);
    pc.AddHatOffset("rancher_run_side_006",  1, 0);
    pc.AddHatOffset("rancher_run_bw_002",  0, 0);
    pc.AddHatOffset("rancher_run_bw_003",  0, 1);
    pc.AddHatOffset("rancher_run_bw_005",  1, 0);
    pc.AddHatOffset("rancher_run_bw_006",  1, 1);
  }

  private static Dictionary<string, float> _AnimFPS = new()
  {
    { "death",                8 },
    { "death_coop",           8 },
    { "death_shot",           8 },
    { "dodge",                18 }, //NOTE: FPS overrides apparently have no effect on dodge roll animations
    { "dodge_bw",             18 },
    { "dodge_left",           24 },
    { "dodge_left_bw",        24 },
    { "ghost_idle_back",      9 },
    { "ghost_idle_back_left", 9 },
    { "ghost_idle_back_right",9 },
    { "ghost_idle_front",     9 },
    { "ghost_idle_left",      9 },
    { "ghost_idle_right",     9 },
    { "ghost_sneeze_left",    12 },
    { "ghost_sneeze_right",   12 },
    { "idle",                 9 },
    { "idle_select",          9 },
    { "idle_backward",        9 },
    { "idle_backward_hand",   9 },
    { "idle_bw",              9 },
    { "idle_forward",         9 },
    { "idle_forward_hand",    9 },
    { "idle_hand",            9 },
    { "item_get",             9 },
    { "jetpack_down",         9 },
    { "jetpack_right",        9 },
    { "jetpack_right_bw",     9 },
    { "jetpack_up",           9 },
    { "pet",                  9 },
    { "pitfall_return",       9 },
    { "run_down",             11 },
    { "run_down_hand",        11 },
    { "run_right",            11 },
    { "run_right_bw",         11 },
    { "run_right_hand",       11 },
    { "run_up",               11 },
    { "run_up_hand",          11 },
    { "select_choose",        12 },
    { "select_idle",          9 },
    { "slide_down",           8 },
    { "slide_right",          8 },
    { "slide_up",             8 },
    { "spinfall",             12 },
  };
}
