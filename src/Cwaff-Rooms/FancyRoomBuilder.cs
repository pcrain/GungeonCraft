using SaveAPI;
using Alexandria.DungeonAPI;

namespace CwaffingTheGungy;

public static class FancyRoomBuilder
{
  public delegate bool SpawnCondition();

  public static Dictionary<string, PrototypeDungeonRoom> FancyShopRooms = new();
  // public static Vector3[] defaultItemPositions = new Vector3[] {
  //   new Vector3(1.125f, 2.125f, 1),
  //   new Vector3(2.625f, 1f, 1),
  //   new Vector3(4.125f, 2.125f, 1) };

  private static List<string> _DefaultLine = new(){"Buy somethin', will ya!"};

  public static PrototypeDungeonRoom MakeFancyShop(string npcName, List<int> shopItems, float spawnChance = 1f, Floors? spawnFloors = null,
    CwaffPrerequisite spawnPrerequisite = CwaffPrerequisite.NONE, SpawnCondition spawnPrequisiteChecker = null, string voice = null,
    List<String> genericDialog = null, List<String> stopperDialog = null, List<String> purchaseDialog = null,
    List<String> noSaleDialog = null, List<String> introDialog = null, List<String> attackedDialog = null, bool allowDupes = false,
    float mainPoolChance = 0.0f, Vector3? talkPointOffset = null, Vector3? npcPosition = null, List<Vector3> itemPositions = null,
    bool oncePerRun = true)
  {
    if (C.DEBUG_BUILD)
      ETGModConsole.Log($"Setting up shop for {npcName}");

    string npcNameUpper = npcName.ToUpper();

    $"#{npcNameUpper}_GENERIC_TALK".SetupDBStrings(genericDialog ?? _DefaultLine);
    $"#{npcNameUpper}_STOPPER_TALK".SetupDBStrings(stopperDialog ?? _DefaultLine);
    $"#{npcNameUpper}_PURCHASE_TALK".SetupDBStrings(purchaseDialog ?? _DefaultLine);
    $"#{npcNameUpper}_NOSALE_TALK".SetupDBStrings(noSaleDialog ?? _DefaultLine);
    $"#{npcNameUpper}_INTRO_TALK".SetupDBStrings(introDialog ?? _DefaultLine);
    $"#{npcNameUpper}_ATTACKED_TALK".SetupDBStrings(attackedDialog ?? _DefaultLine);

    List<DungeonPrerequisite> dungeonPrerequisites = new(){
      new CwaffDungeonPrerequisite { prerequisite = spawnPrerequisite.SetupCheck(spawnPrequisiteChecker)
      }};

    string currencyIcon = ResMap.Get($"{npcName}_icon", quietFailure: true)?[0];
    if (!string.IsNullOrEmpty(currencyIcon))
      currencyIcon += ".png";

    GameObject shop = ShopAPI.SetUpShop(
      name                              : npcName,
      prefix                            : C.MOD_PREFIX,
      idleSpritePaths                   : ResMap.Get($"{npcName}_idle"),
      idleFps                           : 2,
      talkSpritePaths                   : ResMap.Get($"{npcName}_talk"),
      talkFps                           : 2,
      lootTable                         : shopItems.ToLootTable(),
      // currency                          : CustomShopItemController.ShopCurrencyType.CUSTOM,
      currency                          : CustomShopItemController.ShopCurrencyType.COINS,
      runBasedMultilineGenericStringKey : $"#{npcNameUpper}_GENERIC_TALK",
      runBasedMultilineStopperStringKey : $"#{npcNameUpper}_STOPPER_TALK",
      purchaseItemStringKey             : $"#{npcNameUpper}_PURCHASE_TALK",
      purchaseItemFailedStringKey       : $"#{npcNameUpper}_NOSALE_TALK",
      introStringKey                    : $"#{npcNameUpper}_INTRO_TALK",
      attackedStringKey                 : $"#{npcNameUpper}_ATTACKED_TALK",
      stolenFromStringKey               : $"#{npcNameUpper}_STOLEN_TALK",
      talkPointOffset                   : talkPointOffset ?? ShopAPI.defaultTalkPointOffset,
      npcPosition                       : npcPosition ?? ShopAPI.defaultNpcPosition,
      voiceBox                          : ShopAPI.VoiceBoxes.OLD_MAN,
      itemPositions                     : itemPositions?.ToArray() ?? ShopAPI.defaultItemPositions,
      costModifier                      : 1f,
      giveStatsOnPurchase               : false,
      statsToGiveOnPurchase             : null,
      CustomCanBuy                      : null,
      CustomRemoveCurrency              : null,
      CustomPrice                       : null,
      // CustomPrice                       : HealthTraderCustomPrice,
      OnPurchase                        : null,
      OnSteal                           : null,
      currencyIconPath                  : currencyIcon/*""*/,
      // currencyIconPath                  : "",
      // currencyName                      : "test",
      currencyName                      : "ui_coin",
      canBeRobbed                       : true,
      hasCarpet                         : ResMap.Get($"{npcName}_carpet", quietFailure: true)?[0] != null,
      carpetSpritePath                  : ResMap.Get($"{npcName}_carpet", quietFailure: true)?[0],
      CarpetOffset                      : null,
      hasMinimapIcon                    : ResMap.Get($"{npcName}_icon", quietFailure: true)?[0] != null,
      minimapIconSpritePath             : ResMap.Get($"{npcName}_icon", quietFailure: true)?[0],
      addToMainNpcPool                  : mainPoolChance > 0,
      percentChanceForMainPool          : mainPoolChance,
      prerequisites                     : dungeonPrerequisites.ToArray(), // used by RegisterShopRoom(), but we're manually calling AddInjection() for now
      fortunesFavorRadius               : 2,
      poolType                          : allowDupes ? CustomShopController.ShopItemPoolType.DUPES : CustomShopController.ShopItemPoolType.DEFAULT,
      RainbowModeImmunity               : false,
      hitboxSize                        : null, // must be null, doesn't work properly
      // hitboxSize                        : new IntVector2(0/*13*/, 0/*19*/), // must be null, doesn't work properly
      hitboxOffset                      : null // must be null, doesn't work properly
      );

    TalkDoerLite npc = shop.GetComponentInChildren<TalkDoerLite>();
    npc.gameObject.AddComponent<FlipsToFacePlayer>();
    // npc.GetComponent<AIAnimator>().IdleAnimation.Type = DirectionalAnimation.DirectionType.TwoWayHorizontal;
    // npc.GetComponent<AIAnimator>().IdleAnimation.Flipped[0] = DirectionalAnimation.FlipType.Mirror;
    if (!string.IsNullOrEmpty(voice))
      npc.audioCharacterSpeechTag = voice;

    PrototypeDungeonRoom shopRoom = GetBasicShopRoom();

    // ShopAPI.RegisterShopRoom(shop: shop, protoroom: shopRoom, vector: new Vector2((float)(shopRoom.Width / 2), (float)(shopRoom.Height / 2)));
    RoomFactory.AddInjection(
      protoroom            : shopRoom/*RoomFactory.BuildFromResource(roomPath: "Planetside/Resources/ShrineRooms/ShopRooms/TimeTraderShop.room").room*/,
      injectionAnnotation  : $"{npcName}'s Shop Room",
      placementRules       : new() { ProceduralFlowModifierData.FlowModifierPlacementType.END_OF_CHAIN },
      chanceToLock         : 0,
      prerequisites        : dungeonPrerequisites,
      injectorName         : $"{npcName}'s Shop Room",
      selectionWeight      : 1,
      chanceToSpawn        : spawnChance,
      addSingularPlaceable : shop,
      XFromCenter          : 0,
      YFromCenter          : 0
      );

    // below doesn't seem to work, figure out later
    if (false && oncePerRun) // small hack for now since Alexandria doesn't let us modify this directly as of right now
    {
      SharedInjectionData baseInjection    = LoadHelper.LoadAssetFromAnywhere<SharedInjectionData>("Base Shared Injection Data");
      SharedInjectionData injector         = baseInjection.AttachedInjectionData[baseInjection.AttachedInjectionData.Count - 1];
      injector.OnlyOne                     = true;
      injector.ChanceToSpawnOne            = 1.0f;
      injector.InjectionData[0].OncePerRun = true;
      // baseInjection                        = null;
    }

    // foreach (Floors floor in Enum.GetValues(typeof(Floors)))
    // {
    //   if ((floor & spawnFloors) == 0)
    //     continue;
    //   // TODO: add shop room to specific floor
    // }

    FancyShopRooms[npcName] = shopRoom;
    return shopRoom;
  }

  public static int HealthTraderCustomPrice(CustomShopController shop, CustomShopItemController itemCont, PickupObject item)
  {
    return 5;
  }

  private static PrototypeDungeonRoom _BasicShopRoom = null;
  public static PrototypeDungeonRoom GetBasicShopRoom()
  {
    PrototypeDungeonRoom room = RoomFactory.BuildNewRoomFromResource(roomPath: "CwaffingTheGungy/Resources/Rooms/BasicShopRoom2.newroom").room; // rebuild every time for now
    return room;
    // if (_BasicShopRoom == null)
    //   _BasicShopRoom = RoomFactory.BuildFromResource(roomPath: "CwaffingTheGungy/Resources/Rooms/BasicShopRoom.room").room;
    // return _BasicShopRoom;
  }

  // Extension for checking shop activation conditions using CwaffPrerequisite and returning CwaffPrerequisite for inline setup
  public static CwaffPrerequisite SetupCheck(this CwaffPrerequisite prereq, FancyRoomBuilder.SpawnCondition check)
  {
    CwaffDungeonPrerequisite.AddPrequisiteCheck(prereq, check);
    return prereq;
  }

  // Stolen from NN for now
  public static GenericLootTable CreateLootTable(List<GenericLootTable> includedLootTables = null, DungeonPrerequisite[] prerequisites = null)
  {
      GenericLootTable lootTable = ScriptableObject.CreateInstance<GenericLootTable>();
      lootTable.defaultItemDrops = new WeightedGameObjectCollection()
      {
          elements = new List<WeightedGameObject>()
      };
      lootTable.tablePrerequisites = prerequisites ?? new DungeonPrerequisite[0];
      lootTable.includedLootTables = includedLootTables ?? new List<GenericLootTable>();
      return lootTable;
  }

  public static void ForceSpawnForDebugPurposes(this PrototypeDungeonRoom room)
  {
      SharedInjectionData injector                   = ScriptableObject.CreateInstance<SharedInjectionData>();
      injector.UseInvalidWeightAsNoInjection         = true;
      injector.PreventInjectionOfFailedPrerequisites = false/*true*/;
      injector.IsNPCCell                             = false;
      injector.IgnoreUnmetPrerequisiteEntries        = false;
      injector.OnlyOne                               = false;
      injector.ChanceToSpawnOne                      = 1f;
      injector.AttachedInjectionData                 = new List<SharedInjectionData>();
      injector.InjectionData                         = new List<ProceduralFlowModifierData>
      {
        GenerateNewProcData(room, GlobalDungeonData.ValidTilesets.CASTLEGEON),
      };
      injector.name = $"Debug Force Spawn Room {Guid.NewGuid()}";
      SharedInjectionData BaseInjection = LoadHelper.LoadAssetFromAnywhere<SharedInjectionData>("Base Shared Injection Data");
      BaseInjection.AttachedInjectionData ??= new List<SharedInjectionData>();
      BaseInjection.AttachedInjectionData.Add(injector);
  }

  public static ProceduralFlowModifierData GenerateNewProcData(PrototypeDungeonRoom RequiredRoom, GlobalDungeonData.ValidTilesets Tileset)
  {
    string name = RequiredRoom.name.ToString()+Tileset.ToString();
    if (RequiredRoom.name.ToString() == null)
      name = "EmergencyAnnotationName";

    Vector2 offset = new Vector2(-2.25f, -1.25f);
    Vector2 vector = new Vector2((float)(RequiredRoom.Width / 2) + offset.x, (float)(RequiredRoom.Height / 2) + offset.y);

    RequiredRoom.placedObjectPositions.Add(vector);

    ProceduralFlowModifierData SpecProcData = new ProceduralFlowModifierData()
    {
      annotation = name,
      DEBUG_FORCE_SPAWN = false/*true*/,
      OncePerRun = false,
      placementRules = new List<ProceduralFlowModifierData.FlowModifierPlacementType>()
      {
        ProceduralFlowModifierData.FlowModifierPlacementType.END_OF_CHAIN
      },
      roomTable = null,
      exactRoom = RequiredRoom,
      IsWarpWing = false,
      RequiresMasteryToken = false,
      chanceToLock = 0,
      selectionWeight = 2,
      chanceToSpawn = 1,
      RequiredValidPlaceable = null,
      prerequisites = new DungeonPrerequisite[]
      {
        new CwaffDungeonPrerequisite { prerequisite = CwaffPrerequisite.TEST_PREREQUISITE.SetupCheck(null) }
      },
      CanBeForcedSecret = false,
      RandomNodeChildMinDistanceFromEntrance = 0,
      exactSecondaryRoom = null,
      framedCombatNodes = 0,
    };
    return SpecProcData;
  }
}

public enum CwaffPrerequisite
{
  NONE,
  INSURANCE_PREREQUISITE,
  TEST_PREREQUISITE,
}

public class CwaffDungeonPrerequisite : CustomDungeonPrerequisite
{
  private static List<FancyRoomBuilder.SpawnCondition> SpawnConditions =
    new(Enumerable.Repeat<FancyRoomBuilder.SpawnCondition>(null, Enum.GetNames(typeof(CwaffPrerequisite)).Length).ToList());

  public CwaffPrerequisite prerequisite = CwaffPrerequisite.NONE;

  public static void AddPrequisiteCheck(CwaffPrerequisite prereq, FancyRoomBuilder.SpawnCondition check)
  {
    if (SpawnConditions[(int)prereq] != null)
    {
      ETGModConsole.Log($"  Tried to reinitialize a shop spawn prerequisite!");
      return;
    }
    SpawnConditions[(int)prereq] = check;
  }

  public override bool CheckConditionsFulfilled()
  {
    if (prerequisite == CwaffPrerequisite.NONE || SpawnConditions[(int)prerequisite] == null)
      return true;
    return SpawnConditions[(int)prerequisite]();
  }
}

public static class OneOffDebugDungeonFlow {

    public static DungeonFlow CreateAndWarp(string shopName) {
        PrototypeDungeonRoom theRoom = FancyRoomBuilder.FancyShopRooms[shopName];
        DungeonFlow m_CachedFlow = CreateFlowWithSingleTestRoom(theRoom);
        m_CachedFlow.name = shopName;

        ETGModConsole.Log($"Attempting to warp to floor with {shopName} room");
        GameLevelDefinition floorDef = new GameLevelDefinition()
        {
            dungeonSceneName           = shopName, //this is the name we will use whenever we want to load our dungeons scene
            dungeonPrefabPath          = shopName, //this is what we will use when we want to acess our dungeon prefab
            priceMultiplier            = 1f, //multiplies how much things cost in the shop
            secretDoorHealthMultiplier = 1, //multiplies how much health secret room doors have, aka how many shots you will need to expose them
            enemyHealthMultiplier      = 2, //multiplies how much health enemies have
            damageCap                  = 300, // damage cap for regular enemies
            bossDpsCap                 = 78, // damage cap for bosses
            flowEntries                = new List<DungeonFlowLevelEntry>(0),
            predefinedSeeds            = new List<int>(0)
        };

        CwaffDungeons.Register(internalName: shopName, floorName: shopName,
            dungeonGenerator: OneOffDungeonGenerator, gameLevelDefinition: floorDef);

        _CurrentCustomDebugFlow = m_CachedFlow;
        GameManager.Instance.LoadCustomLevel(shopName);

        return m_CachedFlow;
    }

    public static Dungeon OneOffDungeonGenerator(Dungeon dungeon)
    {
        dungeon.PatternSettings = new SemioticDungeonGenSettings()
        {
            flows = new List<DungeonFlow>() {
                OneOffDebugDungeonFlow.GetCurrentCustomDebugFlow(),
            },
            mandatoryExtraRooms = new List<ExtraIncludedRoomData>(0),
            optionalExtraRooms = new List<ExtraIncludedRoomData>(0),
            MAX_GENERATION_ATTEMPTS = 250,
            DEBUG_RENDER_CANVASES_SEPARATELY = false
        };
        return dungeon;
    }

    public static DungeonFlow CreateFlowWithSingleTestRoom(PrototypeDungeonRoom theRoom)
    {
        DungeonFlow m_CachedFlow = ScriptableObject.CreateInstance<DungeonFlow>();

        DungeonFlowNode Node_00 = new DungeonFlowNode(m_CachedFlow) {
            isSubchainStandin = false,
            nodeType = DungeonFlowNode.ControlNodeType.ROOM,
            roomCategory = PrototypeDungeonRoom.RoomCategory.CONNECTOR,
            percentChance = 1f,
            priority = DungeonFlowNode.NodePriority.MANDATORY,
            overrideExactRoom = CwaffDungeonPrefabs.elevator_entrance,
            overrideRoomTable = null,
            capSubchain = false,
            subchainIdentifier = string.Empty,
            limitedCopiesOfSubchain = false,
            maxCopiesOfSubchain = 1,
            subchainIdentifiers = new List<string>(0),
            receivesCaps = false,
            isWarpWingEntrance = false,
            handlesOwnWarping = false,
            forcedDoorType = DungeonFlowNode.ForcedDoorType.NONE,
            loopForcedDoorType = DungeonFlowNode.ForcedDoorType.NONE,
            nodeExpands = false,
            initialChainPrototype = "n",
            chainRules = new List<ChainRule>(0),
            minChainLength = 3,
            maxChainLength = 8,
            minChildrenToBuild = 1,
            maxChildrenToBuild = 1,
            canBuildDuplicateChildren = false,
            parentNodeGuid = string.Empty,
            childNodeGuids = new List<string>(0),
            loopTargetNodeGuid = string.Empty,
            loopTargetIsOneWay = false,
            guidAsString = Guid.NewGuid().ToString(),
            flow = m_CachedFlow,
        };

        DungeonFlowNode CustomShopNode = new DungeonFlowNode(m_CachedFlow) {
            isSubchainStandin = false,
            nodeType = DungeonFlowNode.ControlNodeType.ROOM,
            roomCategory = PrototypeDungeonRoom.RoomCategory.CONNECTOR,
            percentChance = 1f,
            priority = DungeonFlowNode.NodePriority.MANDATORY,
            overrideExactRoom = theRoom,
            overrideRoomTable = null,
            capSubchain = false,
            subchainIdentifier = string.Empty,
            limitedCopiesOfSubchain = false,
            maxCopiesOfSubchain = 1,
            subchainIdentifiers = new List<string>(0),
            receivesCaps = false,
            isWarpWingEntrance = false,
            handlesOwnWarping = false,
            forcedDoorType = DungeonFlowNode.ForcedDoorType.NONE,
            loopForcedDoorType = DungeonFlowNode.ForcedDoorType.NONE,
            nodeExpands = false,
            initialChainPrototype = "n",
            chainRules = new List<ChainRule>(0),
            minChainLength = 3,
            maxChainLength = 8,
            minChildrenToBuild = 1,
            maxChildrenToBuild = 1,
            canBuildDuplicateChildren = false,
            parentNodeGuid = string.Empty,
            childNodeGuids = new List<string>(0),
            loopTargetNodeGuid = string.Empty,
            loopTargetIsOneWay = false,
            guidAsString = Guid.NewGuid().ToString(),
        };

        DungeonFlowNode Node_99 = new DungeonFlowNode(m_CachedFlow) {
            isSubchainStandin = false,
            nodeType = DungeonFlowNode.ControlNodeType.ROOM,
            roomCategory = PrototypeDungeonRoom.RoomCategory.EXIT,
            percentChance = 1f,
            priority = DungeonFlowNode.NodePriority.MANDATORY,
            overrideExactRoom = CwaffDungeonPrefabs.exit_room_basic,
            overrideRoomTable = null,
            capSubchain = false,
            subchainIdentifier = string.Empty,
            limitedCopiesOfSubchain = false,
            maxCopiesOfSubchain = 1,
            subchainIdentifiers = new List<string>(0),
            receivesCaps = false,
            isWarpWingEntrance = false,
            handlesOwnWarping = false,
            forcedDoorType = DungeonFlowNode.ForcedDoorType.NONE,
            loopForcedDoorType = DungeonFlowNode.ForcedDoorType.NONE,
            nodeExpands = false,
            initialChainPrototype = "n",
            chainRules = new List<ChainRule>(0),
            minChainLength = 3,
            maxChainLength = 8,
            minChildrenToBuild = 1,
            maxChildrenToBuild = 1,
            canBuildDuplicateChildren = false,
            parentNodeGuid = string.Empty,
            childNodeGuids = new List<string>(0),
            loopTargetNodeGuid = string.Empty,
            loopTargetIsOneWay = false,
            guidAsString = Guid.NewGuid().ToString(),
            flow = m_CachedFlow,
        };

        // m_CachedFlow.fallbackRoomTable = BossrushFlows.Bossrush_01_Castle.fallbackRoomTable;
        m_CachedFlow.fallbackRoomTable = CwaffDungeonPrefabs.SewersRoomTable;
        m_CachedFlow.subtypeRestrictions = new List<DungeonFlowSubtypeRestriction>(0);
        m_CachedFlow.flowInjectionData = new List<ProceduralFlowModifierData>(0);
        m_CachedFlow.sharedInjectionData = new List<SharedInjectionData>(0);

        m_CachedFlow.Initialize();

        m_CachedFlow.AddNodeToFlow(Node_00, null);
        m_CachedFlow.AddNodeToFlow(CustomShopNode, Node_00);
        m_CachedFlow.AddNodeToFlow(Node_99, CustomShopNode);

        m_CachedFlow.FirstNode = Node_00;

        return m_CachedFlow;
    }

    internal static DungeonFlow _CurrentCustomDebugFlow = null;
    public static DungeonFlow GetCurrentCustomDebugFlow()
    {
      return _CurrentCustomDebugFlow;
    }
}

public class FlipsToFacePlayer : MonoBehaviour
{
  private AIAnimator _animator;
  private Transform _speechPoint;
  private float _flipOffset;
  private float _centerX;
  private float _baseX;
  private Vector3 _baseSpeechPos;
  private bool _cachedFlipped;

  private void Start()
  {
    this._animator      = base.GetComponent<AIAnimator>();
    this._flipOffset    = this._animator.sprite.GetUntrimmedBounds().size.x /** 0.5f*/;
    this._centerX       = this._animator.sprite.WorldBottomCenter.x;
    this._baseX         = this._animator.sprite.transform.localPosition.x;

    this._speechPoint   = base.transform.Find("SpeechPoint");
    this._baseSpeechPos = this._speechPoint.position;

    this._cachedFlipped = false;
  }

  private void Update()
  {
    // this._animator.sprite.FlipX = GameManager.Instance.BestActivePlayer.CenterPosition.x < this._animator.transform.position.x;
    // this._animator.sprite.transform.localScale = this._animator.sprite.transform.localScale.WithX(
    //   (GameManager.Instance.BestActivePlayer.CenterPosition.x < this._animator.transform.position.x) ? -1f : 1f);
    FlipSpriteIfNecessary();
  }

  private void FlipSpriteIfNecessary()
  {
    this._animator.sprite.FlipX = GameManager.Instance.BestActivePlayer.sprite.WorldBottomCenter.x < this._centerX;
    if (this._animator.sprite.FlipX == this._cachedFlipped)
      return;

    this._cachedFlipped = this._animator.sprite.FlipX;
    base.transform.localPosition = base.transform.localPosition.WithX(
      this._baseX + (this._cachedFlipped ? _flipOffset : 0f));
    this._speechPoint.position = this._baseSpeechPos;
  }

  // private void FlipSpriteIfNecessaryClose()
  // {
  //   this._animator.sprite.FlipX = GameManager.Instance.BestActivePlayer.sprite.WorldBottomCenter.x < this._centerX;
  //   if (this._animator.sprite.FlipX == this._cachedFlipped)
  //     return;

  //   this._cachedFlipped = this._animator.sprite.FlipX;
  //   this._animator.sprite.transform.localPosition = this._animator.sprite.transform.localPosition.WithX(
  //     this._baseX + (this._cachedFlipped ? _flipOffset : 0f));
  // }
}
