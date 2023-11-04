namespace CwaffingTheGungy {

    public class FlowSimpleAsItGets {

        public static DungeonFlow Init() {
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
                overrideExactRoom = SansBoss.SansBossRoom,
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

            m_CachedFlow.name = "Simplest";
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

            // ETGModConsole.Log("loaded flow "+m_CachedFlow.name);

            // FlowHelpers.PrintFlow(m_CachedFlow);

            return m_CachedFlow;
        }
    }
}

