namespace CwaffingTheGungy;

/*
  TODO:
    - fix pitfall doorway frame resetting to idle animation
    - add sounds to animations (pitfall, pit return, spinfall)
    - add foyer card
    - add boss card
    - add loadout card
    - add punchout sprites
    - implement pogo stick dodge roll item
    - implement starter weapon
    - flesh out stats
*/

public class Rogo
{
  public static string Name = "Rogo";
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
      health            = 2,
      armor             = 2,
      foyerPos          = new Vector3(32.5f, 20.5f),
      loadout           = new(){
        new(Lazy.Pickup<PaintballCannon>(), false),
        new(Lazy.Pickup<TryhardSnacks>(), false),
      },
    };

    PlayerController pc = data.MakeNewCustomCharacter();
    pc.UpdateAnimations(data, _AnimFPS);
    pc.spriteAnimator
      // .SetLoopPoint("select_casing", 0)
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
      ;
    pc.FinalizeCharacter(data);
  }

  private static Dictionary<string, float> _AnimFPS = new()
  {
    { "death",                6 },
    { "death_coop",           6 },
    { "death_shot",           6 },
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
    { "select_choose",        10 },
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
