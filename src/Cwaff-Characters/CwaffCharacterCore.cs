namespace CwaffingTheGungy;

using static Alexandria.CharacterAPI.FoyerCharacterHandler;

public static class CwaffCharacter
{
  public static readonly tk2dSpriteCollectionData Collection =
     SpriteBuilder.ConstructCollection(new GameObject().RegisterPrefab(false, false, true), $"{C.MOD_NAME}_Character_Collection");

  public static void UpdateAnimations(this PlayerController pc, CustomCharacterData data, Dictionary<string, float> fpsDict)
  {
    string charName = data.nameInternal.ToLower();
    tk2dSpriteCollectionData col = Collection;
    pc.spriteAnimator.Library = UnityEngine.Object.Instantiate(pc.spriteAnimator.Library);
    GameObject.DontDestroyOnLoad(pc.spriteAnimator.Library);
    tk2dSpriteAnimationClip[] clips = pc.spriteAnimator.Library.clips;
    for (int i = 0; i < clips.Length; ++i)
    {
      tk2dSpriteAnimationClip clip = clips[i];
      if (!fpsDict.TryGetValue(clip.name, out float newFps))
        newFps = clip.fps;
      int loopStart = clip.wrapMode == tk2dSpriteAnimationClip.WrapMode.Once ? -1 : clip.loopStart;
      // for (int f = 0; f < clip.frames.Length; ++f)
      // {
      //   if (!clip.frames[f].groundedFrame)
      //     System.Console.WriteLine($"    frame {f}/{clip.frames.Length-1} in {clip.name} is airborne");
      //   if (clip.frames[f].invulnerableFrame)
      //     System.Console.WriteLine($"    frame {f}/{clip.frames.Length-1} in {clip.name} is invulnerable");
      // }
      string frameName = clip.frames[0].spriteCollection.spriteDefinitions[clip.frames[0].spriteId].name;
      // if (col.AddAnimation($"{charName}_{clip.name}", animName: clip.name, fps: newFps, loopStart: loopStart) is not tk2dSpriteAnimationClip newClip)
      // {
        int firstUnderscore = frameName.IndexOf("_") + 1;
        int lastUnderscore = frameName.LastIndexOf("_");
        string baseFrameName = frameName.Substring(firstUnderscore, lastUnderscore - firstUnderscore);
        if (col.AddAnimation($"{charName}_{baseFrameName}", animName: clip.name, fps: newFps, loopStart: loopStart) is not tk2dSpriteAnimationClip newClipAlt)
        {
          System.Console.WriteLine($"    COULD NOT FIND animation for {clip.name} aka {baseFrameName}");
          continue;
        }
        tk2dSpriteAnimationClip newClip = newClipAlt;
      // }

      if (clip.name.Contains("dodge"))
      {
        int airFrames = Mathf.CeilToInt(0.5f * newClip.frames.Length);
        for (int f = 0; f < airFrames; ++f)
        {
          newClip.frames[f].invulnerableFrame = true;
          newClip.frames[f].groundedFrame = false;
        }
      }

      System.Console.WriteLine($"  updated animations for {clip.name} a.k.a. {frameName} with framerate {newClip.fps}");
      clips[i] = newClip;
    }
  }

  /// <summary>Set an audio event for a specific frame of a specific animation</summary>
  public static tk2dSpriteAnimator SetAudio(this tk2dSpriteAnimator animator, string name = null, string audio = "", params int[] frameIds)
  {
    tk2dSpriteAnimationFrame[] frames = animator.GetClipByName(name).frames;
    foreach (int f in frameIds)
    {
      frames[f].triggerEvent = true;
      frames[f].eventAudio = audio;
    }
    return animator;
  }

  public static void FinalizeCharacter(this PlayerController pc, CustomCharacterData data)
  {
    pc.healthHaver.CursedMaximum = data.health;
    pc.healthHaver.maximumHealth = data.health;
    pc.stats.SetBaseStatValue(StatType.Health, data.health, pc);
    CharacterBuilder.CustomizeCharacterNoSprites(
      player               : pc,
      data                 : data,
      d1                   : null,
      tk2DSpriteAnimation1 : null,
      d2                   : null,
      tk2DSpriteAnimation2 : null,
      paradoxUsesSprites   : false
      );

    CharacterBuilder.storedCharacters.Add(data.nameInternal.ToLower(), new(data, pc.gameObject));
    ETGModConsole.Characters.Add(data.nameShort.ToLowerInvariant(), data.nameShort); //Adds characters to MTGAPIs character database
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

// [HarmonyPatch]
// internal static class CustomCharacterGetDataPatch
// {
//     [HarmonyPatch(typeof(CustomCharacter), nameof(CustomCharacter.GetData))]
//     static void Prefix(CustomCharacter __instance)
//     {
//         System.Console.WriteLine($"attempting to get data for {__instance.gameObject.name}");
//     }
// }
