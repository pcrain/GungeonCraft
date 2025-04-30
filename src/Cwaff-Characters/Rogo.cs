namespace CwaffingTheGungy;

public class Rogo
{
  public static string Name = "rogo";
  public static PlayableCharacters Character = Name.ExtendEnum<PlayableCharacters>();

  public static void Init()
  {
    CustomCharacterData data = new() {
      baseCharacter     = PlayableCharacters.Robot,
      identity          = Character,
      name              = Name,
      nameShort         = Name,
      nameInternal      = Name,
      nickname          = Name,
      health            = 4,
      armor             = 2,
      normalMaterial    = new Material(ShaderCache.Acquire("Brave/PlayerShader")),
      foyerPos          = new Vector3(30.125f, 29.5f),
      characterID       = CharacterBuilder.storedCharacters.Count,
    };

    GameObject gameObject = CharacterBuilder.GetPlayerPrefab(data.baseCharacter).ClonePrefab();
    gameObject.AddComponent<CustomCharacter>().data = data;

    PlayerController pc = gameObject.GetComponent<PlayerController>();
    pc.UpdateAnimations(data, _AnimFPS);
    pc.spriteAnimator
      .SetAudio("dodge",          "rogo_dodge_sound", 0)
      .SetAudio("dodge_bw",       "rogo_dodge_sound", 0)
      .SetAudio("dodge_left",     "rogo_dodge_sound", 0)
      .SetAudio("dodge_left_bw",  "rogo_dodge_sound", 0)
      .SetAudio("run_down",       "rogo_step_sound", 3, 7)
      // .SetAudio("run_down_hand",  "rogo_step_sound", 3, 7)
      .SetAudio("run_right",      "rogo_step_sound", 3, 7)
      .SetAudio("run_right_bw",   "rogo_step_sound", 3, 7)
      // .SetAudio("run_right_hand", "rogo_step_sound", 3, 7)
      .SetAudio("run_up",         "rogo_step_sound", 3, 7)
      // .SetAudio("run_up_hand",    "rogo_step_sound", 3, 7)
      ;
    pc.AllowZeroHealthState = false;
    pc.ForceZeroHealthState = false;
    pc.FinalizeCharacter(data);
  }

  private static Dictionary<string, float> _AnimFPS = new()
  {
    // { "death",                4 },
    // { "death_coop",           4 },
    // { "death_shot",           4 },
    { "dodge",                18 },
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
    // { "ghost_sneeze_left",    4 },
    // { "ghost_sneeze_right",   4 },
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
    // { "pet",                  4 },
    // { "pitfall",              4 },
    // { "pitfall_down",         4 },
    // { "pitfall_return",       4 },
    { "run_down",             20 },
    { "run_down_hand",        20 },
    { "run_right",            20 },
    { "run_right_bw",         20 },
    { "run_right_hand",       20 },
    { "run_up",               20 },
    { "run_up_hand",          20 },
    // { "select_casing",        4 },
    // { "select_choose",        4 },
    // { "select_choose_long",   4 },
    // { "select_headspin",      4 },
    // { "select_idle",          4 },
    // { "select_stargaze",      4 },
    // { "select_stargaze_cry",  4 },
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
