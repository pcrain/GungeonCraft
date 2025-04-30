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

  private static readonly HashSet<CustomCharacterData> _ProcessedChars = new();
  private static readonly Dictionary<CustomCharacterData, GameObject> _OverheadPrefabs = new();

  private static GameObject DeepClone(this GameObject g, int indent = 2, bool topLevel = true)
  {
    // if (g.GetComponent<CharacterSelectFacecardIdleDoer>())
    //   System.Console.WriteLine($"{new string(' ', indent)}> processing '{g.name}' with active={g.activeSelf}/{g.activeInHierarchy} (with CharacterSelectFacecardIdleDoer)");
    // else
    //   System.Console.WriteLine($"{new string(' ', indent)}> processing '{g.name}' with active={g.activeSelf}/{g.activeInHierarchy}");

    GameObject newG = UnityEngine.Object.Instantiate(g);
    newG.name = g.name;
    // if (!topLevel)
      newG.SetActive(true);
    GameObject.DontDestroyOnLoad(newG);
    List<Transform> newChildren = new();
    while (newG.transform.childCount > 0)
    {
      Transform child = newG.transform.GetChild(0);
      GameObject co = child.gameObject;
      newChildren.Add(co.DeepClone(indent + 2, topLevel: false).transform);
      child.transform.parent = null;
      UnityEngine.Object.DestroyImmediate(co);
    }
    foreach (Transform newChild in newChildren)
      newChild.transform.parent = newG.transform;

    // System.Console.WriteLine($"{new string(' ', indent)}< processed '{newG.name}'");
    return newG;
  }

  [HarmonyPatch]
  private static class FoyerCharacterSelectFlagCreateOverheadElementPatch
  {
      [HarmonyPatch(typeof(FoyerCharacterSelectFlag), nameof(FoyerCharacterSelectFlag.CreateOverheadElement))]
      static void Prefix(FoyerCharacterSelectFlag __instance)
      {
          if (__instance.OverheadElement)
            return;
          System.Console.WriteLine($"creating overhead for {__instance.name}. null? {__instance.OverheadElement == null}");
          __instance.OverheadElement = _OverheadPrefabs.First().Value;
          System.Console.WriteLine($"  how about now for key {_OverheadPrefabs.First().Key.name}? {__instance.OverheadElement == null}");
      }
  }

  private static void CwaffCreateOverheadCard(this CustomCharacterData data, FoyerCharacterSelectFlag selectCharacter)
  {
      if (selectCharacter.OverheadElement == null)
      {
          ETGModConsole.Log($"CHR_{data.nameShort}Panel is null");
          return;
      }

      if (selectCharacter.OverheadElement?.name == $"CHR_{data.nameShort}Panel")
      {
          ETGModConsole.Log($"CHR_{data.nameShort}Panel already exists");
          return;
      }

      if (_OverheadPrefabs.TryGetValue(data, out GameObject oPrefab))
      {
        selectCharacter.ClearOverheadElement();
        System.Console.WriteLine($"attempting prefab load!");
        // GameObject newOverhead = UnityEngine.Object.Instantiate(oPrefab);
        // FoyerInfoPanelController fipc = newOverhead.GetComponent<FoyerInfoPanelController>();
        // fipc.followTransform = selectCharacter.transform;
        // System.Console.WriteLine($"doing a danger");
        // // fipc.scaledSprites[0].transform.localScale = Vector3.one;
        // System.Console.WriteLine($"danger done");
        // if (!fipc.arrow.GetComponentInChildren<CharacterSelectFacecardIdleDoer>())
        // {
        //   Lazy.DebugLog($"  NO IDLEDOER");
        //   if (newOverhead.GetComponentInChildren<CharacterSelectFacecardIdleDoer>() is CharacterSelectFacecardIdleDoer inChild)
        //     Lazy.DebugLog($"    prefab facecard parent is {(inChild.transform.parent ? inChild.transform.parent.gameObject.name : "NOTHING")}");
        //   else
        //     Lazy.DebugLog($"    prefab facecard not found");
        //   Lazy.DebugLog($"      setup failed, bailing out");
        //   return;
        // }
        selectCharacter.OverheadElement = oPrefab;
        selectCharacter.OverheadElement.name = $"CHR_{data.nameShort}Panel";
        System.Console.WriteLine($"  are we null here? {selectCharacter.OverheadElement == null}");
        return;
      }

      //Create new card instance
      selectCharacter.ClearOverheadElement();

      GameObject newOverheadElement = selectCharacter.OverheadElement.DeepClone();
      newOverheadElement.SetActive(true);
      newOverheadElement.name = $"CHR_{data.nameShort}Panel";
      newOverheadElement.GetComponent<FoyerInfoPanelController>().followTransform = selectCharacter.transform;

      //Change text
      FoyerInfoPanelController infoPanel = newOverheadElement.GetComponent<FoyerInfoPanelController>();
      //HACK: temporarily hardcoded to robot, should be more general
      infoPanel.arrow = newOverheadElement.transform.Find("Robot Arrow").gameObject.GetComponent<tk2dSprite>();
      infoPanel.textPanel = newOverheadElement.transform.Find("TextPanel").gameObject.GetComponent<dfPanel>();
      dfLabel nameLabel = infoPanel.textPanel.transform.Find("NameLabel").GetComponent<dfLabel>();
      nameLabel.Text = "#CHAR_" + data.nameShort.ToString().ToUpper();
      dfLabel pastKilledLabel = infoPanel.textPanel.transform.Find("PastKilledLabel").GetComponent<dfLabel>();
      pastKilledLabel.ProcessMarkup = true;
      pastKilledLabel.ColorizeSymbols = true;
      if (data.metaCost != 0)
      {
          pastKilledLabel.ModifyLocalizedText(pastKilledLabel.Text + " (" + data.metaCost.ToString() + "[sprite \"hbux_text_icon\"])");
          pastKilledLabel.ModifyLocalizedText("(Past Killed)" + " (" + data.metaCost.ToString() + "[sprite \"hbux_text_icon\"])");
      }

      // Loadout setup
      infoPanel.itemsPanel = infoPanel.textPanel.transform.Find("ItemsPanel (1)").gameObject.GetComponent<dfPanel>();
      infoPanel.itemsPanel.enabled = true;
      var spriteObject = FakePrefab.Clone(infoPanel.itemsPanel.GetComponentInChildren<dfSprite>().gameObject);
      foreach (var child in infoPanel.itemsPanel.GetComponentsInChildren<dfSprite>())
          UnityEngine.Object.DestroyImmediate(child.gameObject);
      dfAtlas uiAtlas = GameUIRoot.Instance.ConversationBar.portraitSprite.Atlas;
      for (int i = 0; i < data.loadout.Count; i++)
      {
          string uiSpriteName = data.loadout[i].First.itemName.InternalName() + "_ui";
          if (!uiAtlas.map.TryGetValue(uiSpriteName, out dfAtlas.ItemInfo itemInfo))
          {
            Lazy.DebugWarn($"failed to find ui sprite {uiSpriteName} for character {data.nameShort}, skipping");
            continue;
          }
          var sprite = FakePrefab.Clone(spriteObject).GetComponent<dfSprite>();
          sprite.gameObject.SetActive(true);

          sprite.SpriteName = uiSpriteName;
          sprite.Size = 3f * itemInfo.sizeInPixels;
          sprite.Atlas = uiAtlas;

          sprite.transform.parent = infoPanel.itemsPanel.transform;
          infoPanel.itemsPanel.Controls.Add(sprite);
          sprite.transform.position = new Vector3(1 + ((i + 0.1f) * 0.1f), -((i + 0.1f) * 0.1f), 0);
          sprite.transform.localPosition = new Vector3(((i + 0.1f) * 0.1f), 0, 0);
      }

      // Facecard setup
      CharacterSelectFacecardIdleDoer facecard = newOverheadElement.GetComponentInChildren<CharacterSelectFacecardIdleDoer>();

      facecard.gameObject.name = data.nameShort + " Sprite FaceCard";// <---------------- this object needs to be shrank
      facecard.gameObject.SetActive(true);
      facecard.spriteAnimator = facecard.gameObject.GetComponent<tk2dSpriteAnimator>();
      // facecard.spriteAnimator.sprite.scale = 8f * Vector3.one; //TODO: magic number, why does this work (Alexandria uses 7f which looks wrong at first, but right after returning to breach once????)

      string appearAnimName = $"{data.nameShort}_facecard_appear";
      string idleAnimName = $"{data.nameShort}_facecard_idle";

      if (!_ProcessedChars.Contains(data))
      {
        tk2dSpriteCollectionData uiCollection = ((GameObject)ResourceCache.Acquire("ControllerButtonSprite")).GetComponent<tk2dBaseSprite>().Collection;

        if (uiCollection.AddAnimation($"{appearAnimName}", fps: 17, loopStart: -1) is tk2dSpriteAnimationClip clipA)
        {
          foreach (tk2dSpriteAnimationFrame frame in clipA.frames)
            frame.spriteCollection.spriteDefinitions[frame.spriteId].BetterConstructOffsetsFromAnchor(anchor: Anchor.LowerCenter);
          facecard.spriteAnimator.AddClip(clipA);
        }

        if (uiCollection.AddAnimation($"{idleAnimName}", fps: 17) is tk2dSpriteAnimationClip clipB)
        {
          foreach (tk2dSpriteAnimationFrame frame in clipB.frames)
            frame.spriteCollection.spriteDefinitions[frame.spriteId].BetterConstructOffsetsFromAnchor(anchor: Anchor.LowerCenter);
          facecard.spriteAnimator.AddClip(clipB);
        }
      }

      facecard.appearAnimation = appearAnimName;
      facecard.coreIdleAnimation = idleAnimName;
      facecard.spriteAnimator.DefaultClipId = facecard.spriteAnimator.Library.GetClipIdByName(facecard.appearAnimation);
      Lazy.Append(ref infoPanel.scaledSprites, facecard.spriteAnimator.sprite as tk2dSprite);
      Array.Resize(ref infoPanel.scaledSprites, 0); //HACK: there's a caching issue here that needs to be fixed, look into it later

      _ProcessedChars.Add(data);
      _OverheadPrefabs[data] = newOverheadElement.RegisterPrefab();
      System.Console.WriteLine($"are we null? {_OverheadPrefabs[data] == null}");
      CwaffCreateOverheadCard(data, selectCharacter);
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

  //NOTE: we handle our own foyer card setup, so don't call CreateOverheadCard()
  [HarmonyPatch]
  private static class FoyerCharacterHandlerCreateOverheadCardPatch
  {
      [HarmonyPatch(typeof(FoyerCharacterHandler), "CreateOverheadCard")]
      static bool Prefix(FoyerCharacterSelectFlag selectCharacter, CustomCharacterData data)
      {
          if (_CwaffCharacters.Contains(data))
          {
              data.CwaffCreateOverheadCard(selectCharacter);
              return false;    // skip the original method
          }
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
