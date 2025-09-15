namespace CwaffingTheGungy;

public static class OneOffDebugDungeonFlow {

    private static DungeonFlow _CurrentCustomDebugFlow = null;
    private static readonly string DEBUG_FLOW_NAME = "cwaff_debug_flow";

    public static void TestSingleRoom(PrototypeDungeonRoom theRoom) {
        _CurrentCustomDebugFlow = CreateFlowWithSingleTestRoom(theRoom);
        _CurrentCustomDebugFlow.name = DEBUG_FLOW_NAME;

        GameLevelDefinition floorDef = new()
        {
            dungeonSceneName  = DEBUG_FLOW_NAME, //this is the name we will use whenever we want to load our dungeons scene
            dungeonPrefabPath = DEBUG_FLOW_NAME, //this is what we will use when we want to acess our dungeon prefab
            flowEntries       = new(),
            predefinedSeeds   = new(),
        };

        CwaffDungeons.Register(internalName: DEBUG_FLOW_NAME, floorName: DEBUG_FLOW_NAME,
            dungeonGenerator: OneOffDungeonGenerator, gameLevelDefinition: floorDef);

        GameManager.Instance.LoadCustomLevel(DEBUG_FLOW_NAME);
    }

    private static DungeonFlowNode Sanitize(this DungeonFlowNode node)
    {
        node.subchainIdentifiers ??= new();
        node.chainRules          ??= new();
        return node;
    }

    private static Dungeon OneOffDungeonGenerator(Dungeon dungeon)
    {
        dungeon.PatternSettings = new SemioticDungeonGenSettings()
        {
            flows               = new() { _CurrentCustomDebugFlow },
            mandatoryExtraRooms = new(),
            optionalExtraRooms  = new(),
        };
        return dungeon;
    }

    private static DungeonFlow CreateFlowWithSingleTestRoom(PrototypeDungeonRoom theRoom)
    {
        DungeonFlow flow = ScriptableObject.CreateInstance<DungeonFlow>();

        DungeonFlowNode entranceNode = new DungeonFlowNode(flow) {
            roomCategory = PrototypeDungeonRoom.RoomCategory.ENTRANCE,
            overrideExactRoom = CwaffDungeonPrefabs.elevator_entrance,
        }.Sanitize();

        DungeonFlowNode roomNode = new DungeonFlowNode(flow) {
            overrideExactRoom = theRoom,
        }.Sanitize();

        DungeonFlowNode exitNode = new DungeonFlowNode(flow) {
            roomCategory = PrototypeDungeonRoom.RoomCategory.EXIT,
            overrideExactRoom = CwaffDungeonPrefabs.exit_room_basic,
        }.Sanitize();

        flow.fallbackRoomTable   = null;
        flow.subtypeRestrictions = new();
        flow.flowInjectionData   = new();
        flow.sharedInjectionData = new();
        flow.FirstNode           = entranceNode;

        flow.Initialize();
        flow.AddNodeToFlow(entranceNode, null);
        flow.AddNodeToFlow(roomNode, entranceNode);
        flow.AddNodeToFlow(exitNode, roomNode);

        return flow;
    }
}
