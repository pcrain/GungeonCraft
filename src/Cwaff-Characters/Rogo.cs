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
      health            = 2,
      armor             = 2,
      normalMaterial    = new Material(ShaderCache.Acquire("Brave/PlayerShader")),
      foyerPos          = new Vector3(30.125f, 29.5f),
      characterID       = CharacterBuilder.storedCharacters.Count,
    };

    GameObject gameObject = CharacterBuilder.GetPlayerPrefab(data.baseCharacter).ClonePrefab();
    gameObject.AddComponent<CustomCharacter>().data = data;

    PlayerController playerController = gameObject.GetComponent<PlayerController>();
    playerController.UpdateAnimations(data, _AnimFPS);

    CharacterBuilder.CustomizeCharacterNoSprites(
      player               : playerController,
      data                 : data,
      d1                   : null,
      tk2DSpriteAnimation1 : null,
      d2                   : null,
      tk2DSpriteAnimation2 : null,
      paradoxUsesSprites   : false
      );

    CharacterBuilder.storedCharacters.Add(data.nameInternal.ToLower(), new(data, gameObject));
    ETGModConsole.Characters.Add(data.nameShort.ToLowerInvariant(), data.nameShort); //Adds characters to MTGAPIs character database
  }

  private static Dictionary<string, float> _AnimFPS = new()
  {
    // { "death",                4 },
    // { "death_coop",           4 },
    // { "death_shot",           4 },
    // { "dodge",                4 },
    // { "dodge_bw",             4 },
    // { "dodge_left",           4 },
    // { "dodge_left_bw",        4 },
    // { "doorway",              4 },
    // { "ghost_idle_back",      4 },
    // { "ghost_idle_back_left", 4 },
    // { "ghost_idle_back_right",4 },
    // { "ghost_idle_front",     4 },
    // { "ghost_idle_left",      4 },
    // { "ghost_idle_right",     4 },
    // { "ghost_sneeze_left",    4 },
    // { "ghost_sneeze_right",   4 },
    // { "idle",                 4 },
    // { "idle_backward",        4 },
    // { "idle_backward_hand",   4 },
    // { "idle_bw",              4 },
    // { "idle_forward",         4 },
    // { "idle_forward_hand",    4 },
    // { "idle_hand",            4 },
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
