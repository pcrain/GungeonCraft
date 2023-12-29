namespace CwaffingTheGungy;

public static class OneOffDebugDungeonFlow {

    public static DungeonFlow CreateAndWarp(string shopName) {
        PrototypeDungeonRoom theRoom = FancyShopBuilder.FancyShopRooms[shopName];
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
