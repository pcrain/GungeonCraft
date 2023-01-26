using System;
using System.Collections.Generic;
using Gungeon;
using ItemAPI;
using EnemyAPI;
using UnityEngine;
//using DirectionType = DirectionalAnimation.DirectionType;
// using AnimationType = ItemAPI.BossBuilder.AnimationType;
using System.Collections;
using Dungeonator;
using System.Linq;
using Brave.BulletScript;
using System.Text.RegularExpressions;
using ResourceExtractor = ItemAPI.ResourceExtractor;
using GungeonAPI;
using System.Reflection;
using MonoMod.RuntimeDetour;

namespace CwaffingTheGungy
{
  public static class BH
  {
    public static List<int> Range(int start, int end)
    {
      return Enumerable.Range(start, end-start+1).ToList();
    }

    public static void CopySaneDefaultBehavior(this BehaviorSpeculator self, BehaviorSpeculator other)
    {
      self.OverrideBehaviors               = other.OverrideBehaviors;
      self.OtherBehaviors                  = other.OtherBehaviors;
      self.InstantFirstTick                = other.InstantFirstTick;
      self.TickInterval                    = other.TickInterval;
      self.PostAwakenDelay                 = other.PostAwakenDelay;
      self.RemoveDelayOnReinforce          = other.RemoveDelayOnReinforce;
      self.OverrideStartingFacingDirection = other.OverrideStartingFacingDirection;
      self.StartingFacingDirection         = other.StartingFacingDirection;
      self.SkipTimingDifferentiator        = other.SkipTimingDifferentiator;
    }

    public static void SetDefaultColliders(this SpeculativeRigidbody self, int width, int height)
    {
      self.PixelColliders.Clear();
      self.PixelColliders.Add(new PixelCollider
        {
          ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
          CollisionLayer = CollisionLayer.EnemyCollider,
          IsTrigger = false,
          BagleUseFirstFrameOnly = false,
          SpecifyBagelFrame = string.Empty,
          BagelColliderNumber = 0,
          ManualOffsetX = 0,
          ManualOffsetY = 10,
          ManualWidth = 101,
          ManualHeight = 27,
          ManualDiameter = 0,
          ManualLeftX = 0,
          ManualLeftY = 0,
          ManualRightX = 0,
          ManualRightY = 0
        });
      self.PixelColliders.Add(new PixelCollider
        {
          ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
          CollisionLayer = CollisionLayer.EnemyHitBox,
          IsTrigger = false,
          BagleUseFirstFrameOnly = false,
          SpecifyBagelFrame = string.Empty,
          BagelColliderNumber = 0,
          ManualOffsetX = 0,
          ManualOffsetY = 10,
          ManualWidth = 101,
          ManualHeight = 27,
          ManualDiameter = 0,
          ManualLeftX = 0,
          ManualLeftY = 0,
          ManualRightX = 0,
          ManualRightY = 0,
        });
    }

    public static EnemyBehavior AddSaneDefaultBossBehavior(GameObject prefab, string name, string subtitle, string bossCardPath = "")
    {
      EnemyBehavior companion = prefab.AddComponent<EnemyBehavior>();
        companion.aiActor.healthHaver.PreventAllDamage = false;
        companion.aiActor.HasShadow = false;
        companion.aiActor.IgnoreForRoomClear = false;
        companion.aiActor.specRigidbody.CollideWithOthers = true;
        companion.aiActor.specRigidbody.CollideWithTileMap = true;
        companion.aiActor.PreventFallingInPitsEver = true;
        companion.aiActor.procedurallyOutlined = false;
        companion.aiActor.CanTargetPlayers = true;
        companion.aiActor.PreventBlackPhantom = false;

      // prefab.name = tableId+"_NAME";
      prefab.name = name;
      string tableId = "#"+name.Replace(" ","_").ToUpper();
      ETGMod.Databases.Strings.Enemies.Set(tableId+"_NAME", name);
      ETGMod.Databases.Strings.Enemies.Set(tableId+"_SUBTITLE", subtitle);
      ETGMod.Databases.Strings.Enemies.Set(tableId+"_QUOTE", string.Empty);
      companion.aiActor.healthHaver.overrideBossName = tableId+"_NAME";
      companion.aiActor.OverrideDisplayName = tableId+"_NAME";
      companion.aiActor.ActorName = tableId+"_NAME";
      companion.aiActor.name = tableId+"_NAME";
      GenericIntroDoer miniBossIntroDoer = BH.AddSaneDefaultIntroDoer(prefab);
        if (!String.IsNullOrEmpty(bossCardPath))
        {
          Texture2D bossCardTexture = ResourceExtractor.GetTextureFromResource(bossCardPath);
          miniBossIntroDoer.portraitSlideSettings = new PortraitSlideSettings()
          {
            bossNameString = tableId+"_NAME",
            bossSubtitleString = tableId+"_SUBTITLE",
            bossQuoteString = tableId+"_QUOTE",
            bossSpritePxOffset = IntVector2.Zero,
            topLeftTextPxOffset = IntVector2.Zero,
            bottomRightTextPxOffset = IntVector2.Zero,
            bgColor = Color.cyan
          };
          miniBossIntroDoer.portraitSlideSettings.bossArtSprite = bossCardTexture;
          miniBossIntroDoer.SkipBossCard = false;
          prefab.GetComponent<EnemyBehavior>().aiActor.healthHaver.bossHealthBar = HealthHaver.BossBarType.MainBar;
        }
        else
        {
          miniBossIntroDoer.SkipBossCard = true;
          prefab.GetComponent<EnemyBehavior>().aiActor.healthHaver.bossHealthBar = HealthHaver.BossBarType.SubbossBar;
        }
        miniBossIntroDoer.SkipFinalizeAnimation = true;
        miniBossIntroDoer.RegenerateCache();


      return companion;
    }

    public static GenericIntroDoer AddSaneDefaultIntroDoer(GameObject prefab)
    {
      GenericIntroDoer miniBossIntroDoer = prefab.AddComponent<GenericIntroDoer>();
        miniBossIntroDoer.triggerType = GenericIntroDoer.TriggerType.PlayerEnteredRoom;
        miniBossIntroDoer.specifyIntroAiAnimator = null;
        miniBossIntroDoer.initialDelay = 0.15f;
        miniBossIntroDoer.cameraMoveSpeed = 14;
        miniBossIntroDoer.introAnim = string.Empty;
        // miniBossIntroDoer.introAnim = "intro"; //TODO: check if this actually exists
        miniBossIntroDoer.introDirectionalAnim = string.Empty;
        miniBossIntroDoer.continueAnimDuringOutro = false;
        miniBossIntroDoer.BossMusicEvent = "Play_MUS_Boss_Theme_Beholster";
        miniBossIntroDoer.PreventBossMusic = false;
        miniBossIntroDoer.InvisibleBeforeIntroAnim = true;
        miniBossIntroDoer.preIntroAnim = string.Empty;
        miniBossIntroDoer.preIntroDirectionalAnim = string.Empty;
        miniBossIntroDoer.cameraFocus = null;
        miniBossIntroDoer.roomPositionCameraFocus = Vector2.zero;
        miniBossIntroDoer.restrictPlayerMotionToRoom = false;
        miniBossIntroDoer.fusebombLock = false;
        miniBossIntroDoer.AdditionalHeightOffset = 0;
      return miniBossIntroDoer;
    }

    public static void AddAnimation(this EnemyBehavior self, tk2dSpriteCollectionData collection, List<int> ids, string name, float fps, bool loop, DirectionalAnimation.DirectionType direction = DirectionalAnimation.DirectionType.None)
    {
      tk2dSpriteAnimationClip.WrapMode loopMode = loop
        ? tk2dSpriteAnimationClip.WrapMode.Loop
        : tk2dSpriteAnimationClip.WrapMode.Once;
      SpriteBuilder.AddAnimation(self.spriteAnimator, collection, ids, name, loopMode).fps = fps;
      if (direction != DirectionalAnimation.DirectionType.None)
      {
        if (name == "idle")
        {
          self.aiAnimator.IdleAnimation = new DirectionalAnimation
          {
            Type = direction,
            Prefix = name,
            AnimNames = new string[1], // TODO: this might not be one if our directional type is not single
            Flipped = new DirectionalAnimation.FlipType[1]
          };
        }
      }
    }

    public static void AdjustAnimation(this EnemyBehavior self, string name, float? fps = null, bool? loop = null)
    {
      tk2dSpriteAnimationClip clip = self.spriteAnimator.GetClipByName(name);
      if (clip == null)
      {
        ETGModConsole.Log($"tried to modify sprite {name} which does not exist");
        return;
      }
      if (fps.HasValue)
        clip.fps = fps.Value;
      if (loop.HasValue)
      {
        clip.wrapMode = loop.Value
          ? tk2dSpriteAnimationClip.WrapMode.Loop
          : tk2dSpriteAnimationClip.WrapMode.Once;
      }
    }

    public static Regex rx_anim = new Regex(@"^(?:(.*?)_)?([^_]*?)_([0-9]+)\.png$",
          RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static void InitSpritesFromResourcePath(this EnemyBehavior self, string resourcePath, int defaultFps = 15)
    {
      // TODO: maybe add warning if a path isn't added as a resource?
      string realPath = resourcePath.Replace('/', '.') + ".";

      // Load all of our sprites into a dictionary of ordered lists of names
      Dictionary<string,string[]> spriteMaps = new Dictionary<string,string[]>();
      foreach (string s in ResourceExtractor.GetResourceNames())
      {
        if (!s.StartsWith(realPath))
          continue;
        string name = s.Substring(realPath.Length);  // get name of resource relative to the path
        MatchCollection matches = rx_anim.Matches(name);
        foreach (Match match in matches)
        {
          string spriteName = match.Groups[1].Value;  //TODO: verification?
          string animName   = match.Groups[2].Value;
          string animIndex  = match.Groups[3].Value;
          if (!spriteMaps.ContainsKey(animName))
            spriteMaps[animName] = new string[0];
          int index = Int32.Parse(animIndex);
          if (index >= spriteMaps[animName].Length)
          {
            string[] sa = spriteMaps[animName];
            Array.Resize(ref sa, index+1);
            spriteMaps[animName] = sa;
          }
          spriteMaps[animName][index] = name;
        }
      }

      // create the sprite collection itself
      tk2dSpriteCollectionData bossSprites = SpriteBuilder.ConstructCollection(
        self.gameObject, (self.gameObject.name+" Collection").Replace(" ","_"));
      UnityEngine.Object.DontDestroyOnLoad(bossSprites);
      int lastAnim = 0;
      foreach(KeyValuePair<string, string[]> entry in spriteMaps)
      {
        int firstAnim = lastAnim;
        // ETGModConsole.Log($"Showing sprites for {entry.Key}");
        foreach(string v in entry.Value)
        {
          if (String.IsNullOrEmpty(v))
            continue;
          // ETGModConsole.Log($"  {v}");
          SpriteBuilder.AddSpriteToCollection($"{resourcePath}/{v}", bossSprites);
          ++lastAnim;
        }
        DirectionalAnimation.DirectionType dir;
        if (entry.Key == "idle")
          dir = DirectionalAnimation.DirectionType.Single;
        else
          dir = DirectionalAnimation.DirectionType.None;
        ETGModConsole.Log($"calling self.AddAnimation(bossSprites, BH.Range({firstAnim}, {lastAnim-1}), \"{entry.Key}\", {defaultFps}, {true}, {dir});");
        self.AddAnimation(bossSprites, BH.Range(firstAnim, lastAnim-1), entry.Key, defaultFps, true, dir);
      }

      // string [] fileEntries = Directory.GetFiles(targetDirectory);
      //   foreach(string fileName in fileEntries)
    }

    public static tk2dSpriteCollectionData LoadSpriteCollection(GameObject prefab, string[] spritePaths)
    {
      tk2dSpriteCollectionData bossSprites = SpriteBuilder.ConstructCollection(prefab, (prefab.name+" Collection").Replace(" ","_"));
      UnityEngine.Object.DontDestroyOnLoad(bossSprites);
      for (int i = 0; i < spritePaths.Length; i++)
        SpriteBuilder.AddSpriteToCollection(spritePaths[i], bossSprites);
      return bossSprites;
    }

    public static IEnumerator WaitForSecondsInvariant(float time)
    {
      for (float elapsed = 0f; elapsed < time; elapsed += GameManager.INVARIANT_DELTA_TIME) { yield return null; }
      yield break;
    }

    //Stolen from Apache
    public static void AddObjectToRoom(PrototypeDungeonRoom room, Vector2 position, DungeonPlaceable PlacableContents = null, DungeonPlaceableBehaviour NonEnemyBehaviour = null, string EnemyBehaviourGuid = null, float SpawnChance = 1f, int xOffset = 0, int yOffset = 0, int layer = 0, int PathID = -1, int PathStartNode = 0) {
        if (room == null) { return; }
        if (room.placedObjects == null) { room.placedObjects = new List<PrototypePlacedObjectData>(); }
        if (room.placedObjectPositions == null) { room.placedObjectPositions = new List<Vector2>(); }

        PrototypePlacedObjectData m_NewObjectData = new PrototypePlacedObjectData() {
            placeableContents = null,
            nonenemyBehaviour = null,
            spawnChance = SpawnChance,
            unspecifiedContents = null,
            enemyBehaviourGuid = string.Empty,
            contentsBasePosition = position,
            layer = layer,
            xMPxOffset = xOffset,
            yMPxOffset = yOffset,
            fieldData = new List<PrototypePlacedObjectFieldData>(0),
            instancePrerequisites = new DungeonPrerequisite[0],
            linkedTriggerAreaIDs = new List<int>(0),
            assignedPathIDx = PathID,
            assignedPathStartNode = PathStartNode
        };

        if (PlacableContents != null) {
            m_NewObjectData.placeableContents = PlacableContents;
        } else if (NonEnemyBehaviour != null) {
            m_NewObjectData.nonenemyBehaviour = NonEnemyBehaviour;
        } else if (EnemyBehaviourGuid != null) {
            m_NewObjectData.enemyBehaviourGuid = EnemyBehaviourGuid;
        } else {
            // All possible object fields were left null? Do nothing and return if this is the case.
            return;
        }

        room.placedObjects.Add(m_NewObjectData);
        room.placedObjectPositions.Add(position);
        return;
    }

    //Also stolen from Apache
    public static WeightedRoom GenerateWeightedRoom(PrototypeDungeonRoom Room, float Weight = 1, bool LimitedCopies = true, int MaxCopies = 1, DungeonPrerequisite[] AdditionalPrerequisites = null) {
        if (Room == null) { return null; }
        if (AdditionalPrerequisites == null) { AdditionalPrerequisites = new DungeonPrerequisite[0]; }
        return new WeightedRoom() { room = Room, weight = Weight, limitedCopies = LimitedCopies, maxCopies = MaxCopies, additionalPrerequisites = AdditionalPrerequisites };
    }

    private static PrototypeDungeonRoom genericBossRoomPrefab = null;

    public static PrototypeDungeonRoom GetGenericBossRoom()
    {
      // Load gatling gull's boss room as a prototype
      if (genericBossRoomPrefab == null)
      {
        AssetBundle sharedAssets = ResourceManager.LoadAssetBundle("shared_auto_001");
        GenericRoomTable bosstable_01_gatlinggull = sharedAssets.LoadAsset<GenericRoomTable>("bosstable_01_gatlinggull");
        foreach (WeightedRoom wRoom in bosstable_01_gatlinggull.includedRooms.elements)
        {
          genericBossRoomPrefab = wRoom.room;
          break;
          // genericBossRoomPrefab.gameObject.SetActive(false);
          // FakePrefab.MarkAsFakePrefab(genericBossRoomPrefab.gameObject);
          // UnityEngine.Object.DontDestroyOnLoad(genericBossRoomPrefab);
          // break;
        }
        sharedAssets = null;
      }
      // Instantiate and clear out the room for our personal use
      PrototypeDungeonRoom p = UnityEngine.Object.Instantiate(genericBossRoomPrefab);
        p.placedObjects.Clear();
        p.placedObjectPositions.Clear();
        p.ClearAllObjectData();
        p.additionalObjectLayers = new List<PrototypeRoomObjectLayer>();
        p.eventTriggerAreas = new List<PrototypeEventTriggerArea>();
        p.roomEvents = new List<RoomEventDefinition>();
        p.paths = new List<SerializedPath>();
        p.prerequisites = new List<DungeonPrerequisite>();
        p.rectangularFeatures = new List<PrototypeRectangularFeature>();
      return p;
    }

    public static void AddBossToFirstFloorPool(this GameObject self)
    {
        if (theBossMan == null)
          theBossMan = GameManager.Instance.BossManager;

        PrototypeDungeonRoom p = GetGenericBossRoom();
          Vector2 roomCenter = new Vector2(p.Width/2.0f, p.Height/2.0f);
          Vector2 spriteCenter = self.GetComponent<tk2dSpriteAnimator>().GetAnySprite().WorldCenter;
        AddObjectToRoom(p, roomCenter - spriteCenter, EnemyBehaviourGuid: RoomMimic.guid);

        GenericRoomTable theRoomTable = ScriptableObject.CreateInstance<GenericRoomTable>();
          theRoomTable.name = self.name+" Boss Table";
          theRoomTable.includedRooms = new WeightedRoomCollection();
          theRoomTable.includedRooms.elements = new List<WeightedRoom>(){GenerateWeightedRoom(p)};
          theRoomTable.includedRoomTables = new List<GenericRoomTable>(0);

        IndividualBossFloorEntry entry = new IndividualBossFloorEntry()
        {
          BossWeight = 99f,
          TargetRoomTable = theRoomTable,
          GlobalBossPrerequisites = new DungeonPrerequisite[] {
              new DungeonPrerequisite() {
                  prerequisiteOperation = DungeonPrerequisite.PrerequisiteOperation.EQUAL_TO,
                  prerequisiteType = DungeonPrerequisite.PrerequisiteType.TILESET,
                  requiredTileset = GlobalDungeonData.ValidTilesets.CASTLEGEON,
                  requireTileset = true,
                  comparisonValue = 1,
                  encounteredObjectGuid = string.Empty,
                  maxToCheck = TrackedMaximums.MOST_KEYS_HELD,
                  requireDemoMode = false,
                  requireCharacter = false,
                  requiredCharacter = PlayableCharacters.Pilot,
                  requireFlag = false,
                  useSessionStatValue = false,
                  encounteredRoom = null,
                  requiredNumberOfEncounters = -1,
                  saveFlagToCheck = GungeonFlags.TUTORIAL_COMPLETED,
                  statToCheck = TrackedStats.GUNBERS_MUNCHED
              }
          }
        };

        foreach (BossFloorEntry b in theBossMan.BossFloorData)
        {
          if ((b.AssociatedTilesets & GlobalDungeonData.ValidTilesets.CASTLEGEON) > 0)
          {
            foreach (IndividualBossFloorEntry i in b.Bosses)
            {
              i.BossWeight = 0f;
              ETGModConsole.Log($"    {i.TargetRoomTable.name} -> {i.BossWeight}");
            }
            b.Bosses.Add(entry);
            ETGModConsole.Log($"  {b.Annotation}: {b.AssociatedTilesets}");
          }
        }

        //Add rooms to vanilla room tables
        // StaticReferences.RoomTables["triggertwins"].includedRooms = StaticReferences.RoomTables["gull"].includedRooms;
        // StaticReferences.RoomTables["bulletking"].includedRooms = StaticReferences.RoomTables["gull"].includedRooms;
        // ETGModConsole.Log(StaticReferences.RoomTables["boss1"]);//.includedRooms.Add(wRoom);
        // ETGModConsole.Log(StaticReferences.RoomTables["boss2"]);//.includedRooms.Add(wRoom);
        // ETGModConsole.Log(StaticReferences.RoomTables["boss3"]);//.includedRooms.Add(wRoom);

        // if (selectBossHook == null)
        // {
        //   selectBossHook = new Hook(
        //     typeof(BossFloorEntry).GetMethod("SelectBoss", BindingFlags.Public | BindingFlags.Instance),
        //     typeof(BH).GetMethod("SelectBossHook", BindingFlags.Public | BindingFlags.Static));
        // }
    }

    private static BossManager theBossMan = null;

    private static Hook selectBossHook = null;
    private static List<IndividualBossFloorEntry> castleGeonBosses =
      new List<IndividualBossFloorEntry>();
    private static List<IndividualBossFloorEntry> sewerGeonBosses =
      new List<IndividualBossFloorEntry>();
    private static List<IndividualBossFloorEntry> gunGeonBosses =
      new List<IndividualBossFloorEntry>();
    private static List<IndividualBossFloorEntry> cathedralGeonBosses =
      new List<IndividualBossFloorEntry>();
    private static List<IndividualBossFloorEntry> mineGeonBosses =
      new List<IndividualBossFloorEntry>();
    private static List<IndividualBossFloorEntry> catacombGeonBosses =
      new List<IndividualBossFloorEntry>();

    public static IndividualBossFloorEntry SelectBossHook(Func<BossFloorEntry, IndividualBossFloorEntry> orig, BossFloorEntry self)
    {
      // if ((self.AssociatedTilesets & GlobalDungeonData.ValidTilesets.CASTLEGEON) > 0)
      //   self.Bosses.AddRange(castleGeonBosses);
      // if ((self.AssociatedTilesets & GlobalDungeonData.ValidTilesets.SEWERGEON) > 0)
      //   self.Bosses.AddRange(sewerGeonBosses);
      // if ((self.AssociatedTilesets & GlobalDungeonData.ValidTilesets.GUNGEON) > 0)
      //   self.Bosses.AddRange(gunGeonBosses);
      // if ((self.AssociatedTilesets & GlobalDungeonData.ValidTilesets.CATHEDRALGEON) > 0)
      //   self.Bosses.AddRange(cathedralGeonBosses);
      // if ((self.AssociatedTilesets & GlobalDungeonData.ValidTilesets.MINEGEON) > 0)
      //   self.Bosses.AddRange(mineGeonBosses);
      // if ((self.AssociatedTilesets & GlobalDungeonData.ValidTilesets.CATACOMBGEON) > 0)
      //   self.Bosses.AddRange(catacombGeonBosses);

      // foreach (BossFloorEntry b in GameManager.Instance.BossManager.BossFloorData)
      // {
      //   if ((b.AssociatedTilesets & GlobalDungeonData.ValidTilesets.CASTLEGEON) > 0)
      //   {
      //     foreach (IndividualBossFloorEntry i in b.Bosses)
      //     {
      //       i.BossWeight = 0f;
      //       ETGModConsole.Log($"    {i.TargetRoomTable.name} -> {i.BossWeight}");
      //     }
      //     b.Bosses.Add(testIndividualBossFloorEntry);
      //     ETGModConsole.Log($"  {b.Annotation}: {b.AssociatedTilesets}");
      //   }
      // }

      foreach (IndividualBossFloorEntry i in self.Bosses)
        ETGModConsole.Log($"    {i.TargetRoomTable.name} -> {i.BossWeight}");
      // if (!testIndividualBossFloorEntry.GlobalPrereqsValid())
      //   ETGModConsole.Log("failed prerequisite check");
      // if (testIndividualBossFloorEntry != null)
      //   return testIndividualBossFloorEntry;
      return orig(self);
    }
  }
}
