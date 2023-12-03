using SaveAPI;
using Alexandria.DungeonAPI;

namespace CwaffingTheGungy;

public static class FancyRoomBuilder
{
  public delegate bool SpawnCondition();

  public static Dictionary<string, PrototypeDungeonRoom> FancyShopRooms = new();

  public static PrototypeDungeonRoom MakeFancyShop(string npcName, GenericLootTable shopLootTable, float spawnChance = 1f, Floors? spawnFloors = null,
    CwaffPrerequisite spawnPrerequisite = CwaffPrerequisite.NONE, SpawnCondition spawnPrequisiteChecker = null)
  {
    if (C.DEBUG_BUILD)
      ETGModConsole.Log($"Setting up shop for {npcName}");

    string npcNameUpper = npcName.ToUpper();

    ETGMod.Databases.Strings.Core.AddComplex($"#{npcNameUpper}_GENERIC_TALK",  "GENERIC_TALK");
    ETGMod.Databases.Strings.Core.AddComplex($"#{npcNameUpper}_STOPPER_TALK",  "STOPPER_TALK");
    ETGMod.Databases.Strings.Core.AddComplex($"#{npcNameUpper}_PURCHASE_TALK", "PURCHASE_TALK");
    ETGMod.Databases.Strings.Core.AddComplex($"#{npcNameUpper}_NOSALE_TALK",   "NOSALE_TALK");
    ETGMod.Databases.Strings.Core.AddComplex($"#{npcNameUpper}_INTRO_TALK",    "INTRO_TALK");
    ETGMod.Databases.Strings.Core.AddComplex($"#{npcNameUpper}_ATTACKED_TALK", "ATTACKED_TALK");

    List<int> LootTable = new List<int>()
    {
        108, //Bomb
        109, //Ice Bomb
        460, //Chaff Grenade
        66, //Proximity Mine
        308, //Cluster Mine
        136, //C4
        252, //Air Strike
        443, //Big Boy
        567, //Roll Bomb
        234, //iBomb Companion App
        438, //Explosive Decoy
        403, //Melted Rock
        304, //Explosive Rounds
        312, //Blast Helmet
        398, //Table Tech Rocket
        440, //Ruby Bracelet
        601, //Big Shotgun
        4, //Sticky Crossbow
        542, //Strafe Gun
        96, //M16
        6, //Zorgun
        81, //Deck4rd
        274, //Dark Marker
        39, //RPG
        19, //Grenade Launcher
        92, //Stinger
        563, //The Exotic
        129, //Com4nd0
        372, //RC Rocket
        16, //Yari Launcher
        332, //Lil Bomber
        180, //Grasschopper
        593, //Void Core Cannon
        362, //Bullet Bore
        186, //Machine Fist
        28, //Mailbox
        339, //Mahoguny
        478, //Banana
    };

    GenericLootTable loot = CreateLootTable();
    foreach (int i in LootTable)
        loot.AddItemToPool(i);

    List<DungeonPrerequisite> dungeonPrerequisites = new(){
      new CwaffDungeonPrerequisite { prerequisite = spawnPrerequisite.SetupCheck(spawnPrequisiteChecker)
      }};

    GameObject shop = ShopAPI.SetUpShop(
      name                              : npcName,
      prefix                            : C.MOD_PREFIX,
      idleSpritePaths                   : ResMap.Get($"{npcName}_idle"),
      idleFps                           : 2,
      talkSpritePaths                   : ResMap.Get($"{npcName}_talk"),
      talkFps                           : 2,
      lootTable                         : loot/*shopLootTable*/,
      currency                          : CustomShopItemController.ShopCurrencyType.COINS,
      runBasedMultilineGenericStringKey : $"#{npcNameUpper}_GENERIC_TALK",
      runBasedMultilineStopperStringKey : $"#{npcNameUpper}_STOPPER_TALK",
      purchaseItemStringKey             : $"#{npcNameUpper}_PURCHASE_TALK",
      purchaseItemFailedStringKey       : $"#{npcNameUpper}_NOSALE_TALK",
      introStringKey                    : $"#{npcNameUpper}_INTRO_TALK",
      attackedStringKey                 : $"#{npcNameUpper}_ATTACKED_TALK",
      stolenFromStringKey               : $"#{npcNameUpper}_STOLEN_TALK",
      talkPointOffset                   : Vector3.zero,
      npcPosition                       : Vector3.zero,
      voiceBox                          : ShopAPI.VoiceBoxes.OLD_MAN,
      itemPositions                     : ShopAPI.defaultItemPositions,
      costModifier                      : 1f,
      giveStatsOnPurchase               : false,
      statsToGiveOnPurchase             : null,
      CustomCanBuy                      : null,
      CustomRemoveCurrency              : null,
      CustomPrice                       : null,
      OnPurchase                        : null,
      OnSteal                           : null,
      currencyIconPath                  : "",
      currencyName                      : "",
      canBeRobbed                       : true,
      hasCarpet                         : false,
      carpetSpritePath                  : "",
      CarpetOffset                      : null,
      hasMinimapIcon                    : ResMap.Get($"{npcName}_icon")?[0] != null,
      minimapIconSpritePath             : ResMap.Get($"{npcName}_icon")?[0],
      addToMainNpcPool                  : false,
      percentChanceForMainPool          : 0.1f,
      prerequisites                     : null,  // DOESN'T DO ANYTHING AS FAR AS I CAN TELL BASED ON THE SOURCE CODE
      // prerequisites                     : dungeonPrerequisites.ToArray(),
      fortunesFavorRadius               : 2,
      poolType                          : CustomShopController.ShopItemPoolType.DEFAULT,
      RainbowModeImmunity               : false,
      hitboxSize                        : null,
      hitboxOffset                      : null
      );

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

    // foreach (Floors floor in Enum.GetValues(typeof(Floors)))
    // {
    //   if ((floor & spawnFloors) == 0)
    //     continue;
    //   // TODO: add shop room to specific floor
    // }

    FancyShopRooms[npcName] = shopRoom;
    return shopRoom;
  }

  private static PrototypeDungeonRoom _BasicShopRoom = null;
  public static PrototypeDungeonRoom GetBasicShopRoom()
  {
    return RoomFactory.BuildFromResource(roomPath: "CwaffingTheGungy/Resources/Rooms/BasicShopRoom.room").room; // rebuild every time for now
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
