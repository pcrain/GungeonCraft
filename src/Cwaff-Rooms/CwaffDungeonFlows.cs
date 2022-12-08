// using ExpandUtilities;
// using ExpandPrefab;
using Dungeonator;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using MonoMod.RuntimeDetour;

// Stealing from ApacheThunder this time .o.
namespace CwaffingTheGungy {
    
    public class CwaffDungeonFlow {

        public static Hook loadCustomFlowHook; //Hook for making sure we can load custom flows okay

        public static DungeonFlow LoadCustomFlow(Func<string, DungeonFlow>orig, string target) {
            string flowName = target;
            if (flowName.Contains("/")) { flowName = target.Substring(target.LastIndexOf("/") + 1); }

            if (KnownFlows != null && KnownFlows.Count > 0) {
                foreach (DungeonFlow flow in KnownFlows) {
                    if (flow.name != null && flow.name != string.Empty) {                                                
                        if (flowName.ToLower() == flow.name.ToLower()) {
                            DebugTime.RecordStartTime();
                            DebugTime.Log("AssetBundle.LoadAsset<DungeonFlow>({0})", new object[] { flowName });
                            return flow;
                        }
                    }
                }
            }
            return orig(target);
        }

        public static DungeonFlow LoadOfficialFlow(string target) {
            string flowName = target;
            if (flowName.Contains("/")) { flowName = target.Substring(target.LastIndexOf("/") + 1); }
            AssetBundle m_assetBundle_orig = ResourceManager.LoadAssetBundle("flows_base_001");
            DebugTime.RecordStartTime();
            DungeonFlow result = m_assetBundle_orig.LoadAsset<DungeonFlow>(flowName);
            DebugTime.Log("AssetBundle.LoadAsset<DungeonFlow>({0})", new object[] { flowName });
            if (result == null) {
                Debug.Log("ERROR: Requested DungeonFlow not found!\nCheck that you provided correct DungeonFlow name and that it actually exists!");
                m_assetBundle_orig = null;
                return null;
            } else {
                m_assetBundle_orig = null;
                return result;
            }
        }
        
        public static List<DungeonFlow> KnownFlows;
        
        // Default stuff to use with custom Flows
        // public static SharedInjectionData BaseSharedInjectionData;
        // public static SharedInjectionData GungeonInjectionData;
        // public static SharedInjectionData SewersInjectionData;
        // public static SharedInjectionData HollowsInjectionData;
        // public static SharedInjectionData CastleInjectionData;

        public static DungeonFlowSubtypeRestriction BaseSubTypeRestrictions = new DungeonFlowSubtypeRestriction() {
            baseCategoryRestriction = PrototypeDungeonRoom.RoomCategory.NORMAL,
            normalSubcategoryRestriction = PrototypeDungeonRoom.RoomNormalSubCategory.TRAP,
            bossSubcategoryRestriction = PrototypeDungeonRoom.RoomBossSubCategory.FLOOR_BOSS,
            specialSubcategoryRestriction = PrototypeDungeonRoom.RoomSpecialSubCategory.UNSPECIFIED_SPECIAL,
            secretSubcategoryRestriction = PrototypeDungeonRoom.RoomSecretSubCategory.UNSPECIFIED_SECRET,
            maximumRoomsOfSubtype = 1
        };

        // Generate a DungeonFlowNode with a default configuration
        public static DungeonFlowNode GenerateDefaultNode(DungeonFlow targetflow, PrototypeDungeonRoom.RoomCategory roomType, PrototypeDungeonRoom overrideRoom = null, GenericRoomTable overrideTable = null, bool oneWayLoopTarget = false, bool isWarpWingNode = false, string nodeGUID = null, DungeonFlowNode.NodePriority priority = DungeonFlowNode.NodePriority.MANDATORY, float percentChance = 1, bool handlesOwnWarping = true) {

            if (string.IsNullOrEmpty(nodeGUID)) { nodeGUID = Guid.NewGuid().ToString(); }

            DungeonFlowNode m_CachedNode = new DungeonFlowNode(targetflow) {
                isSubchainStandin = false,
                nodeType = DungeonFlowNode.ControlNodeType.ROOM,
                roomCategory = roomType,
                percentChance = percentChance,
                priority = priority,
                overrideExactRoom = overrideRoom,
                overrideRoomTable = overrideTable,
                capSubchain = false,
                subchainIdentifier = string.Empty,
                limitedCopiesOfSubchain = false,
                maxCopiesOfSubchain = 1,
                subchainIdentifiers = new List<string>(0),
                receivesCaps = false,
                isWarpWingEntrance = isWarpWingNode,
                handlesOwnWarping = handlesOwnWarping,
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
                guidAsString = nodeGUID,
                parentNodeGuid = string.Empty,
                childNodeGuids = new List<string>(0),
                loopTargetNodeGuid = string.Empty,
                loopTargetIsOneWay = oneWayLoopTarget,
                flow = targetflow
            };

            return m_CachedNode;
        }        
        
        // Initialize KnownFlows array with custom + official flows.
        public static void InitDungeonFlowsAndHooks(AssetBundle sharedAssets2, bool refreshFlows = false) {

            // Stolen fromm elsewhere, but we need a hook for loading flows
            // TODO: this should be relocated
            loadCustomFlowHook = new Hook(
                typeof(FlowDatabase).GetMethod("GetOrLoadByName", BindingFlags.Public | BindingFlags.Static),
                typeof(CwaffDungeonFlow).GetMethod("LoadCustomFlow", BindingFlags.Public | BindingFlags.Static)
            );

            Dungeon TutorialPrefab = DungeonDatabase.GetOrLoadByName("Base_Tutorial");
            Dungeon CastlePrefab = DungeonDatabase.GetOrLoadByName("Base_Castle");
            Dungeon SewerPrefab = DungeonDatabase.GetOrLoadByName("Base_Sewer");
            Dungeon GungeonPrefab = DungeonDatabase.GetOrLoadByName("Base_Gungeon");
            Dungeon CathedralPrefab = DungeonDatabase.GetOrLoadByName("Base_Cathedral");
            Dungeon MinesPrefab = DungeonDatabase.GetOrLoadByName("Base_Mines");
            Dungeon ResourcefulRatPrefab = DungeonDatabase.GetOrLoadByName("Base_ResourcefulRat");
            Dungeon CatacombsPrefab = DungeonDatabase.GetOrLoadByName("Base_Catacombs");
            Dungeon NakatomiPrefab = DungeonDatabase.GetOrLoadByName("Base_Nakatomi");
            Dungeon ForgePrefab = DungeonDatabase.GetOrLoadByName("Base_Forge");
            Dungeon BulletHellPrefab = DungeonDatabase.GetOrLoadByName("Base_BulletHell");

            // BaseSharedInjectionData = sharedAssets2.LoadAsset<SharedInjectionData>("Base Shared Injection Data");
            // GungeonInjectionData = GungeonPrefab.PatternSettings.flows[0].sharedInjectionData[1];
            // SewersInjectionData = SewerPrefab.PatternSettings.flows[0].sharedInjectionData[1];
            // HollowsInjectionData = CatacombsPrefab.PatternSettings.flows[0].sharedInjectionData[1];
            // CastleInjectionData = CastlePrefab.PatternSettings.flows[0].sharedInjectionData[0];

            KnownFlows = new List<DungeonFlow>();

            KnownFlows.Add(FlowSimpleAsItGets.Init());

            KnownFlows.Add(FlowHelpers.DuplicateDungeonFlow(LoadOfficialFlow("npcparadise")));

            // Add official flows to list (flows found in Dungeon asset bundles after AG&D)
            foreach (DungeonFlow flow in TutorialPrefab.PatternSettings.flows) { KnownFlows.Add(FlowHelpers.DuplicateDungeonFlow(flow)); }
            foreach (DungeonFlow flow in CastlePrefab.PatternSettings.flows) { KnownFlows.Add(FlowHelpers.DuplicateDungeonFlow(flow)); }
            foreach (DungeonFlow flow in SewerPrefab.PatternSettings.flows) { KnownFlows.Add(FlowHelpers.DuplicateDungeonFlow(flow)); }
            foreach (DungeonFlow flow in GungeonPrefab.PatternSettings.flows) { KnownFlows.Add(FlowHelpers.DuplicateDungeonFlow(flow)); }
            foreach (DungeonFlow flow in CathedralPrefab.PatternSettings.flows) { KnownFlows.Add(FlowHelpers.DuplicateDungeonFlow(flow)); }
            foreach (DungeonFlow flow in MinesPrefab.PatternSettings.flows) { KnownFlows.Add(FlowHelpers.DuplicateDungeonFlow(flow)); }
            foreach (DungeonFlow flow in ResourcefulRatPrefab.PatternSettings.flows) { KnownFlows.Add(FlowHelpers.DuplicateDungeonFlow(flow)); }
            foreach (DungeonFlow flow in CatacombsPrefab.PatternSettings.flows) { KnownFlows.Add(FlowHelpers.DuplicateDungeonFlow(flow)); }
            foreach (DungeonFlow flow in NakatomiPrefab.PatternSettings.flows) { KnownFlows.Add(FlowHelpers.DuplicateDungeonFlow(flow)); }
            foreach (DungeonFlow flow in ForgePrefab.PatternSettings.flows) { KnownFlows.Add(FlowHelpers.DuplicateDungeonFlow(flow)); }
            foreach (DungeonFlow flow in BulletHellPrefab.PatternSettings.flows) { KnownFlows.Add(FlowHelpers.DuplicateDungeonFlow(flow)); }

            TutorialPrefab = null;
            CastlePrefab = null;
            SewerPrefab = null;
            GungeonPrefab = null;
            CathedralPrefab = null;
            MinesPrefab = null;
            ResourcefulRatPrefab = null;
            CatacombsPrefab = null;
            NakatomiPrefab = null;
            ForgePrefab = null;
            BulletHellPrefab = null;
        }
    }
}

