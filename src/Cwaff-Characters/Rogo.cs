namespace CwaffingTheGungy;

using static Alexandria.CharacterAPI.FoyerCharacterHandler;

public class Rogo
{
  public static PlayableCharacters Character;
  public static string Name = "rogo";

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
    { "run_down",             24 },
    { "run_down_hand",        24 },
    { "run_right",            24 },
    { "run_right_bw",         24 },
    { "run_right_hand",       24 },
    { "run_up",               24 },
    { "run_up_hand",          24 },
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

  public static void Init()
  {
    Character = Name.ExtendEnum<PlayableCharacters>();

    CustomCharacterData data = new();
    data.baseCharacter = PlayableCharacters.Robot;
    data.identity = Character;
    data.name = Name;
    data.nameShort = Name;
    data.nameInternal = Name;
    data.nickname = Name;
    data.health = 2;
    data.armor = 2;
    data.removeFoyerExtras = false;
    data.metaCost = 0;
    data.useGlow = false;
    data.normalMaterial = new Material(ShaderCache.Acquire("Brave/PlayerShader"));
    data.hasPast = false;
    data.altObjSprite1 = null;
    data.altObjSprite2 = null;
    data.foyerPos = new Vector3(30.125f, 29.5f);

    GameObject basePrefab = CharacterBuilder.GetPlayerPrefab(data.baseCharacter);
    GameObject gameObject = UnityEngine.GameObject.Instantiate(basePrefab);
    PlayerController playerController = gameObject.GetComponent<PlayerController>();

    playerController.UpdateAnimations(data, _AnimFPS);

    CustomCharacter customCharacter = gameObject.AddComponent<CustomCharacter>();
    customCharacter.data = data;
    data.characterID = CharacterBuilder.storedCharacters.Count;
    GameObject.DontDestroyOnLoad(gameObject);
    CharacterBuilder.CustomizeCharacterNoSprites(
      player: playerController,
      data: data,
      d1: null,
      tk2DSpriteAnimation1: null,
      d2: null,
      tk2DSpriteAnimation2: null,
      paradoxUsesSprites: false
      );

    basePrefab = null;
    CharacterBuilder.storedCharacters.Add(data.nameInternal.ToLower(), new Tuple<CustomCharacterData, GameObject>(data, gameObject));
    customCharacter.past = null;
    customCharacter.hasPast = false;
    gameObject.SetActive(false);
    FakePrefab.MarkAsFakePrefab(gameObject);
    ETGModConsole.Characters.Add(data.nameShort.ToLowerInvariant(), data.nameShort); //Adds characters to MTGAPIs character database

    if (!customCharacter.GetData())
      System.Console.WriteLine($"  FAILED TO GET CUSTOM CHARACTER DATA");
  }
}

public static class CwaffCharacterHelpers
{
  public static void UpdateAnimations(this PlayerController pc, CustomCharacterData data, Dictionary<string, float> fpsDict)
  {
    string charName = data.nameInternal.ToLower();
    tk2dSpriteCollectionData col = AtlasHelper.CharacterCollection;
    pc.spriteAnimator.Library = UnityEngine.Object.Instantiate(pc.spriteAnimator.Library);
    GameObject.DontDestroyOnLoad(pc.spriteAnimator.Library);
    tk2dSpriteAnimationClip[] clips = pc.spriteAnimator.Library.clips;
    for (int i = 0; i < clips.Length; ++i)
    {
      tk2dSpriteAnimationClip clip = clips[i];
      if (!fpsDict.TryGetValue(clip.name, out float newFps))
        newFps = clip.fps;
      int loopStart = clip.wrapMode == tk2dSpriteAnimationClip.WrapMode.Once ? -1 : clip.loopStart;
      if (col.AddAnimation($"{charName}_{clip.name}", animName: clip.name, fps: newFps, loopStart: loopStart) is not tk2dSpriteAnimationClip newClip)
      {
        string frameName = clip.frames[0].spriteCollection.spriteDefinitions[clip.frames[0].spriteId].name;
        int firstUnderscore = frameName.IndexOf("_") + 1;
        int lastUnderscore = frameName.LastIndexOf("_");
        string baseFrameName = frameName.Substring(firstUnderscore, lastUnderscore - firstUnderscore);
        if (col.AddAnimation($"{charName}_{baseFrameName}", animName: clip.name, fps: newFps, loopStart: loopStart) is not tk2dSpriteAnimationClip newClipAlt)
        {
          System.Console.WriteLine($"    COULD NOT FIND animation for {clip.name} aka {baseFrameName}");
          continue;
        }
        newClip = newClipAlt;
      }
      System.Console.WriteLine($"  updating animations for {clip.name}");
      clips[i] = newClip;
    }
  }
}

//HACK: prevent skin swapper setup for Rogo, build this check into Alexandria at some point
[HarmonyPatch]
internal static class FoyerCharacterHandlerMakeSkinSwapperPatch
{
    [HarmonyPatch(typeof(FoyerCharacterHandler), "MakeSkinSwapper")]
    static bool Prefix(CustomCharacterData data)
    {
      if (data.altObjSprite1 == null && data.altObjSprite2 == null)
          return false;    // skip the original method
      return true;     // call the original method
    }
}

//HACK: set up an actual foyer card at some point
[HarmonyPatch]
internal static class FoyerCharacterHandlerCreateOverheadCardPatch
{
    [HarmonyPatch(typeof(FoyerCharacterHandler), "CreateOverheadCard")]
    static bool Prefix(FoyerCharacterSelectFlag selectCharacter, CustomCharacterData data)
    {
      if (data.name == Rogo.Name)
          return false;    // skip the original method
      return true;     // call the original method
    }
}

//HACK: set up actual bundle / alternative at some point
[HarmonyPatch]
internal static class SpriteHandlerHandleSpritesBundlePatch
{
    [HarmonyPatch(typeof(SpriteHandler), nameof(SpriteHandler.HandleSpritesBundle))]
      // public static void HandleSpritesBundle(PlayerController player, tk2dSpriteAnimation d1, tk2dSpriteCollectionData spr1, tk2dSpriteAnimation d2, tk2dSpriteCollectionData spr2, CustomCharacterData data, Assembly assembly = null)
    static bool Prefix(PlayerController player, tk2dSpriteAnimation d1, tk2dSpriteCollectionData spr1, tk2dSpriteAnimation d2, tk2dSpriteCollectionData spr2, CustomCharacterData data, Assembly assembly)
    {
        if (data.name == Rogo.Name)
            return false;    // skip the original method
        return true;     // call the original method
    }
}

[HarmonyPatch]
internal static class CustomCharacterGetDataPatch
{
    [HarmonyPatch(typeof(CustomCharacter), nameof(CustomCharacter.GetData))]
    static void Prefix(CustomCharacter __instance)
    {
        System.Console.WriteLine($"attempting to get data for {__instance.gameObject.name}");
    }
}
