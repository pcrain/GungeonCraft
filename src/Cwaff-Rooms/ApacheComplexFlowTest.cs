// using Dungeonator;
// using System;
// using System.Collections.Generic;
// using UnityEngine;

// namespace CwaffingTheGungy {

//     public class FlowSimpleAsItGets {

//         public static DungeonFlow Simplest_Flow() {
//             DungeonFlow m_CachedFlow = ScriptableObject.CreateInstance<DungeonFlow>();

//             DungeonFlowNode ComplexFlowNode_00 = new DungeonFlowNode(m_CachedFlow) {
//                 isSubchainStandin = false,
//                 nodeType = DungeonFlowNode.ControlNodeType.ROOM,
//                 roomCategory = PrototypeDungeonRoom.RoomCategory.CONNECTOR,
//                 percentChance = 1f,
//                 priority = DungeonFlowNode.NodePriority.MANDATORY,
//                 overrideExactRoom = ExpandPrefabs.elevator_entrance,
//                 overrideRoomTable = null,
//                 capSubchain = false,
//                 subchainIdentifier = string.Empty,
//                 limitedCopiesOfSubchain = false,
//                 maxCopiesOfSubchain = 1,
//                 subchainIdentifiers = new List<string>(0),
//                 receivesCaps = false,
//                 isWarpWingEntrance = false,
//                 handlesOwnWarping = false,
//                 forcedDoorType = DungeonFlowNode.ForcedDoorType.NONE,
//                 loopForcedDoorType = DungeonFlowNode.ForcedDoorType.NONE,
//                 nodeExpands = false,
//                 initialChainPrototype = "n",
//                 chainRules = new List<ChainRule>(0),
//                 minChainLength = 3,
//                 maxChainLength = 8,
//                 minChildrenToBuild = 1,
//                 maxChildrenToBuild = 1,
//                 canBuildDuplicateChildren = false,
//                 parentNodeGuid = string.Empty,
//                 childNodeGuids = new List<string>(0),
//                 loopTargetNodeGuid = string.Empty,
//                 loopTargetIsOneWay = false,
//                 guidAsString = Guid.NewGuid().ToString(),
//                 flow = m_CachedFlow,
//             };
//             DungeonFlowNode ComplexFlowNode_13 = new DungeonFlowNode(m_CachedFlow) {
//                 isSubchainStandin = false,
//                 nodeType = DungeonFlowNode.ControlNodeType.ROOM,
//                 roomCategory = PrototypeDungeonRoom.RoomCategory.EXIT,
//                 percentChance = 1f,
//                 priority = DungeonFlowNode.NodePriority.MANDATORY,
//                 overrideExactRoom = ExpandPrefabs.exit_room_basic,
//                 overrideRoomTable = null,
//                 capSubchain = false,
//                 subchainIdentifier = string.Empty,
//                 limitedCopiesOfSubchain = false,
//                 maxCopiesOfSubchain = 1,
//                 subchainIdentifiers = new List<string>(0),
//                 receivesCaps = false,
//                 isWarpWingEntrance = false,
//                 handlesOwnWarping = false,
//                 forcedDoorType = DungeonFlowNode.ForcedDoorType.NONE,
//                 loopForcedDoorType = DungeonFlowNode.ForcedDoorType.NONE,
//                 nodeExpands = false,
//                 initialChainPrototype = "n",
//                 chainRules = new List<ChainRule>(0),
//                 minChainLength = 3,
//                 maxChainLength = 8,
//                 minChildrenToBuild = 1,
//                 maxChildrenToBuild = 1,
//                 canBuildDuplicateChildren = false,
//                 parentNodeGuid = string.Empty,
//                 childNodeGuids = new List<string>(0),
//                 loopTargetNodeGuid = string.Empty,
//                 loopTargetIsOneWay = false,
//                 guidAsString = Guid.NewGuid().ToString(),
//                 flow = m_CachedFlow,
//             };


//             m_CachedFlow.name = "Simplest";
//             // m_CachedFlow.fallbackRoomTable = BossrushFlows.Bossrush_01_Castle.fallbackRoomTable;
//             m_CachedFlow.fallbackRoomTable = ExpandPrefabs.SewersRoomTable;
//             m_CachedFlow.subtypeRestrictions = new List<DungeonFlowSubtypeRestriction>(0);
//             m_CachedFlow.flowInjectionData = new List<ProceduralFlowModifierData>(0);
//             m_CachedFlow.sharedInjectionData = new List<SharedInjectionData>(0);

//             m_CachedFlow.Initialize();

//             m_CachedFlow.AddNodeToFlow(ComplexFlowNode_00, null);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_01, ComplexFlowNode_30);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_02, ComplexFlowNode_01);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_03, ComplexFlowNode_02);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_04, ComplexFlowNode_03);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_05, ComplexFlowNode_07);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_06, ComplexFlowNode_04);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_07, ComplexFlowNode_05);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_08, ComplexFlowNode_09);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_09, ComplexFlowNode_04);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_10, ComplexFlowNode_06);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_11, ComplexFlowNode_10);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_12, ComplexFlowNode_11);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_13, ComplexFlowNode_12);
//             m_CachedFlow.AddNodeToFlow(ComplexFlowNode_13, ComplexFlowNode_00);

//             // Orphaned Chain.
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_14, null);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_15, ComplexFlowNode_14);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_16, ComplexFlowNode_15);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_17, ComplexFlowNode_16);

//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_18, ComplexFlowNode_10);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_19, ComplexFlowNode_18);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_20, ComplexFlowNode_00);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_21, ComplexFlowNode_20);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_22, ComplexFlowNode_23);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_23, ComplexFlowNode_24);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_24, ComplexFlowNode_25);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_25, ComplexFlowNode_21);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_26, ComplexFlowNode_21);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_27, ComplexFlowNode_26);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_28, ComplexFlowNode_27);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_29, ComplexFlowNode_28);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_30, ComplexFlowNode_24);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_31, ComplexFlowNode_01);
//             // m_CachedFlow.AddNodeToFlow(ComplexFlowNode_32, ComplexFlowNode_22);

//             // m_CachedFlow.LoopConnectNodes(ComplexFlowNode_05, ComplexFlowNode_09);
//             // m_CachedFlow.LoopConnectNodes(ComplexFlowNode_19, ComplexFlowNode_04);
//             // m_CachedFlow.LoopConnectNodes(ComplexFlowNode_26, ComplexFlowNode_29);

//             m_CachedFlow.FirstNode = ComplexFlowNode_00;

//             ETGModConsole.Log("loaded complex_flow_test");

//             return m_CachedFlow;
//         }
//     }
// }

