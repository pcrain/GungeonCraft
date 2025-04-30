namespace CwaffingTheGungy;

using static Alexandria.CharacterAPI.FoyerCharacterHandler;
using static Alexandria.Misc.ReflectionUtility;

public static class CwaffCharacter
{
  private static HashSet<CustomCharacterData> _CwaffCharacters = new();

  public static readonly tk2dSpriteCollectionData Collection =
     SpriteBuilder.ConstructCollection(new GameObject().RegisterPrefab(false, false, true), $"{C.MOD_NAME}_Character_Collection");

  public static PlayerController MakeNewCustomCharacter(this CustomCharacterData data)
  {
    data.normalMaterial = new Material(ShaderCache.Acquire("Brave/PlayerShader"));
    data.characterID = CharacterBuilder.storedCharacters.Count;
    data.altGun = new();
    _CwaffCharacters.Add(data);

    GameObject gameObject = CharacterBuilder.GetPlayerPrefab(data.baseCharacter).ClonePrefab();
    gameObject.AddComponent<CustomCharacter>().data = data;
    return gameObject.GetComponent<PlayerController>();
  }

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
      string frameName = clip.frames[0].spriteCollection.spriteDefinitions[clip.frames[0].spriteId].name;

      int firstUnderscore = frameName.IndexOf("_") + 1;
      int lastUnderscore = frameName.LastIndexOf("_");
      string baseFrameName = frameName.Substring(firstUnderscore, lastUnderscore - firstUnderscore);
      if (col.AddAnimation($"{charName}_{baseFrameName}", animName: clip.name, fps: newFps, loopStart: loopStart) is not tk2dSpriteAnimationClip newClipAlt)
      {
        System.Console.WriteLine($"    COULD NOT FIND animation for {clip.name} aka {baseFrameName}");
        continue;
      }
      tk2dSpriteAnimationClip newClip = newClipAlt;

      if (clip.name.Contains("dodge"))
      {
        int airFrames = Mathf.CeilToInt(0.5f * newClip.frames.Length);
        for (int f = 0; f < airFrames; ++f)
        {
          newClip.frames[f].invulnerableFrame = true;
          newClip.frames[f].groundedFrame = false;
        }
      }

      // System.Console.WriteLine($"  updated animations for {clip.name} a.k.a. {frameName} with framerate {newClip.fps}");
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

  /// <summary>Set an audio event for a specific frame of a specific animation</summary>
  public static tk2dSpriteAnimator SetLoopPoint(this tk2dSpriteAnimator animator, string name = null, int loopPoint = 0)
  {
    tk2dSpriteAnimationClip clip = animator.GetClipByName(name);
    clip.wrapMode = (loopPoint < 0) ? tk2dSpriteAnimationClip.WrapMode.Once : tk2dSpriteAnimationClip.WrapMode.Loop;
    clip.loopStart = loopPoint;
    return animator;
  }

  public static void FinalizeCharacter(this PlayerController pc, CustomCharacterData data)
  {
    pc.AllowZeroHealthState = data.health == 0;
    pc.ForceZeroHealthState = data.health == 0;
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

  private static readonly Dictionary<CustomCharacterData, GameObject> _OverheadPrefabs = new();
  private static readonly Vector3 BASEGAME_FACECARD_POSITION = new Vector3(0, 1.687546f, 0.2250061f);
  private static readonly tk2dSpriteCollectionData UICollection = ((GameObject)ResourceCache.Acquire("ControllerButtonSprite")).GetComponent<tk2dBaseSprite>().Collection;
  private static readonly dfAtlas UIAtlas = GameUIRoot.Instance.ConversationBar.portraitSprite.Atlas;

  private static GameObject CwaffCreateOverheadCard(this CustomCharacterData data, FoyerCharacterSelectFlag selectCharacter)
  {
      // Create new card prefab
      GameObject newOverheadElement = selectCharacter.OverheadElement.ClonePrefab(markFake: false);
      newOverheadElement.name = $"CHR_{data.nameShort}Panel";

      // Change text
      FoyerInfoPanelController infoPanel = newOverheadElement.GetComponent<FoyerInfoPanelController>();
      infoPanel.followTransform = selectCharacter.transform;
      infoPanel.textPanel.transform.Find("NameLabel").GetComponent<dfLabel>().Text = "#CHAR_" + data.nameShort.ToUpper();

      // Loadout setup
      var referenceSprite = FakePrefab.Clone(infoPanel.itemsPanel.GetComponentInChildren<dfSprite>().gameObject);
      foreach (dfSprite child in infoPanel.itemsPanel.GetComponentsInChildren<dfSprite>())
          UnityEngine.Object.DestroyImmediate(child.gameObject);
      for (int i = 0; i < data.loadout.Count; i++)
      {
          string uiSpriteName = data.loadout[i].First.itemName.InternalName() + "_ui";
          if (!UIAtlas.map.TryGetValue(uiSpriteName, out dfAtlas.ItemInfo itemInfo))
          {
            Lazy.DebugWarn($"failed to find ui sprite {uiSpriteName} for character {data.nameShort}, skipping");
            continue;
          }
          dfSprite sprite = FakePrefab.Clone(referenceSprite).GetComponent<dfSprite>();
          sprite.SpriteName = uiSpriteName;
          sprite.Size = 3f * itemInfo.sizeInPixels;
          sprite.transform.parent = infoPanel.itemsPanel.transform;
          sprite.transform.localPosition = new Vector3(((i + 0.1f) * 0.1f), 0, 0);
          infoPanel.itemsPanel.Controls.Add(sprite);
      }

      // Facecard setup
      CharacterSelectFacecardIdleDoer facecard = newOverheadElement.GetComponentInChildren<CharacterSelectFacecardIdleDoer>();
      facecard.gameObject.name = data.nameShort + " Sprite FaceCard";// <---------------- this object needs to be shrank
      facecard.spriteAnimator = facecard.gameObject.GetComponent<tk2dSpriteAnimator>();
      facecard.transform.localPosition = BASEGAME_FACECARD_POSITION;
      facecard.transform.parent.localPosition = Vector3.zero;
      facecard.spriteAnimator.sprite.scale = 8f * Vector3.one; //TODO: magic number, why does this work (Alexandria uses 7f)
      string appearAnimName = $"{data.nameShort.ToLower()}_facecard_appear";
      if (UICollection.AddAnimation($"{appearAnimName}", fps: 17, loopStart: -1) is tk2dSpriteAnimationClip clipA)
      {
        foreach (tk2dSpriteAnimationFrame frame in clipA.frames)
          frame.spriteCollection.spriteDefinitions[frame.spriteId].BetterConstructOffsetsFromAnchor(anchor: Anchor.LowerCenter);
        facecard.spriteAnimator.AddClip(clipA);
        facecard.appearAnimation = appearAnimName;
        facecard.spriteAnimator.DefaultClipId = facecard.spriteAnimator.Library.clips.Length - 1;
      }
      string idleAnimName = $"{data.nameShort.ToLower()}_facecard_idle";
      if (UICollection.AddAnimation($"{idleAnimName}", fps: 17) is tk2dSpriteAnimationClip clipB)
      {
        foreach (tk2dSpriteAnimationFrame frame in clipB.frames)
          frame.spriteCollection.spriteDefinitions[frame.spriteId].BetterConstructOffsetsFromAnchor(anchor: Anchor.LowerCenter);
        facecard.spriteAnimator.AddClip(clipB);
        facecard.coreIdleAnimation = idleAnimName;
      }
      infoPanel.scaledSprites = new tk2dSprite[1] { facecard.spriteAnimator.sprite as tk2dSprite };

      return newOverheadElement.RegisterPrefab(); // cache the prefab for quick lookup later
  }

  //NOTE: we handle our own foyer card setup, so don't call CreateOverheadCard()
  [HarmonyPatch]
  private static class FoyerCharacterHandlerCreateOverheadCardPatch
  {
      [HarmonyPatch(typeof(FoyerCharacterHandler), "CreateOverheadCard")]
      static bool Prefix(FoyerCharacterSelectFlag selectCharacter, CustomCharacterData data)
      {
          if (!_CwaffCharacters.Contains(data))
            return true; // call the original method

          selectCharacter.ClearOverheadElement();
          if (!_OverheadPrefabs.TryGetValue(data, out GameObject oPrefab))
            oPrefab = _OverheadPrefabs[data] = data.CwaffCreateOverheadCard(selectCharacter);
          selectCharacter.OverheadElement = oPrefab;
          return false; // skip the original method
      }
  }

  //HACK: prevent skin swapper setup for Rogo, build this check into Alexandria at some point
  [HarmonyPatch]
  private static class FoyerCharacterHandlerMakeSkinSwapperPatch
  {
      [HarmonyPatch(typeof(FoyerCharacterHandler), "MakeSkinSwapper")]
      static bool Prefix(CustomCharacterData data)
      {
          if (data.altObjSprite1 == null && data.altObjSprite2 == null)
              return false;    // skip the original method
          return true;     // call the original method
      }
  }

  //NOTE: we handle our own sprite setup, so don't call HandleSpritesBundle()
  [HarmonyPatch]
  private static class SpriteHandlerHandleSpritesBundlePatch
  {
      [HarmonyPatch(typeof(SpriteHandler), nameof(SpriteHandler.HandleSpritesBundle))]
      static bool Prefix(PlayerController player, tk2dSpriteAnimation d1, tk2dSpriteCollectionData spr1, tk2dSpriteAnimation d2, tk2dSpriteCollectionData spr2, CustomCharacterData data, Assembly assembly)
      {
          if (_CwaffCharacters.Contains(data))
              return false;    // skip the original method
          return true;     // call the original method
      }
  }
}
