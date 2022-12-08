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
                            // Allows glitch chest floors to have things like the Old Crest room drop off if on Gungeon tileset, etc.
                            // if (GlitchChestFlows.Contains(flow.name.ToLower())) {
                            //     flow.sharedInjectionData = RetrieveSharedInjectionDataListFromCurrentFloor();
                            // }
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
        
        public static DungeonFlow Foyer_Flow;

        // Default stuff to use with custom Flows
        public static SharedInjectionData BaseSharedInjectionData;
        public static SharedInjectionData GungeonInjectionData;
        public static SharedInjectionData SewersInjectionData;
        public static SharedInjectionData HollowsInjectionData;
        public static SharedInjectionData CastleInjectionData;
        public static SharedInjectionData JungleInjectionData;
        public static SharedInjectionData BellyInjectionData;
        public static SharedInjectionData PhobosInjectionData;
        public static SharedInjectionData OfficeInjectionData;

        // public static ProceduralFlowModifierData JunkSecretRoomInjector;
        // public static ProceduralFlowModifierData SecretFloorEntranceInjector;
        // public static ProceduralFlowModifierData SecretMiniElevatorInjector;
        // public static ProceduralFlowModifierData SecretJungleEntranceInjector;
        // public static ProceduralFlowModifierData BellySpecialEntranceRoomInjector;
        // public static ProceduralFlowModifierData BellySpecialMonsterRoomInjector;

        public static DungeonFlowSubtypeRestriction BaseSubTypeRestrictions = new DungeonFlowSubtypeRestriction() {
            baseCategoryRestriction = PrototypeDungeonRoom.RoomCategory.NORMAL,
            normalSubcategoryRestriction = PrototypeDungeonRoom.RoomNormalSubCategory.TRAP,
            bossSubcategoryRestriction = PrototypeDungeonRoom.RoomBossSubCategory.FLOOR_BOSS,
            specialSubcategoryRestriction = PrototypeDungeonRoom.RoomSpecialSubCategory.UNSPECIFIED_SPECIAL,
            secretSubcategoryRestriction = PrototypeDungeonRoom.RoomSecretSubCategory.UNSPECIFIED_SECRET,
            maximumRoomsOfSubtype = 1
        };

        // Custom Room Table for Keep Shared Injection Data (for Jungle Entrance room injection data)
        public static GenericRoomTable m_KeepJungleEntranceRooms;

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
        
        
        // Retrieve sharedInjectionData from a specific floor if one is available
        public static List<SharedInjectionData> RetrieveSharedInjectionDataListFromCurrentFloor() {
            Dungeon dungeon = GameManager.Instance.CurrentlyGeneratingDungeonPrefab;
            
            if (dungeon == null) {
                dungeon = GameManager.Instance.Dungeon;
                if (dungeon == null) { return new List<SharedInjectionData>(0); }
                
            }

            if (dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.FORGEGEON | 
                dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.WESTGEON |
                dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.FINALGEON |
                dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.HELLGEON |
                dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.OFFICEGEON |
                dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.RATGEON)
            {
                return new List<SharedInjectionData>(0);
            }

            List<SharedInjectionData> m_CachedInjectionDataList = new List<SharedInjectionData>(0);

            if (dungeon.PatternSettings != null && dungeon.PatternSettings.flows != null && dungeon.PatternSettings.flows.Count > 0) {
                if (dungeon.PatternSettings.flows[0].sharedInjectionData != null && dungeon.PatternSettings.flows[0].sharedInjectionData.Count > 0) {
                    m_CachedInjectionDataList = dungeon.PatternSettings.flows[0].sharedInjectionData;
                }
            }
            
            return m_CachedInjectionDataList;
        }

        // public static ProceduralFlowModifierData RickRollSecretRoomInjector;

        // public static SharedInjectionData CustomSecretFloorSharedInjectionData;


        // Initialize KnownFlows array with custom + official flows.
        public static void InitDungeonFlows(AssetBundle sharedAssets2, bool refreshFlows = false) {

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

            BaseSharedInjectionData = sharedAssets2.LoadAsset<SharedInjectionData>("Base Shared Injection Data");
            GungeonInjectionData = GungeonPrefab.PatternSettings.flows[0].sharedInjectionData[1];
            SewersInjectionData = SewerPrefab.PatternSettings.flows[0].sharedInjectionData[1];
            HollowsInjectionData = CatacombsPrefab.PatternSettings.flows[0].sharedInjectionData[1];
            CastleInjectionData = CastlePrefab.PatternSettings.flows[0].sharedInjectionData[0];

            // Don't build/add flows until injection data is created!
            Foyer_Flow = FlowHelpers.DuplicateDungeonFlow(sharedAssets2.LoadAsset<DungeonFlow>("Foyer Flow"));

            // List<DungeonFlow> m_knownFlows = new List<DungeonFlow>();
            KnownFlows = new List<DungeonFlow>();

            // Build and add custom flows to list.
            // BossrushFlows.InitBossrushFlows();

            // KnownFlows.Add(custom_glitchchest_flow.Custom_GlitchChest_Flow());
            // KnownFlows.Add(test_west_floor_03a_flow.TEST_West_Floor_03a_Flow());
            // KnownFlows.Add(demo_stage_flow.DEMO_STAGE_FLOW());
            KnownFlows.Add(FlowSimpleAsItGets.Init());
            // KnownFlows.Add(custom_glitch_flow.Custom_Glitch_Flow());
            // KnownFlows.Add(really_big_flow.Really_Big_Flow());
            // KnownFlows.Add(fruit_loops.Fruit_Loops());
            // KnownFlows.Add(custom_glitchchestalt_flow.Custom_GlitchChestAlt_Flow());
            // KnownFlows.Add(test_traproom_flow.Test_TrapRoom_Flow());
            // KnownFlows.Add(test_customroom_flow.Test_CustomRoom_Flow());
            // KnownFlows.Add(apache_fucking_around_flow.Apache_Fucking_Around_Flow());
            // KnownFlows.Add(f1b_jungle_flow_01.F1b_Jungle_Flow_01());
            // KnownFlows.Add(f1b_jungle_flow_02.F1b_Jungle_Flow_02());
            // KnownFlows.Add(f2b_belly_flow_01.F2b_Belly_Flow_01());
            // KnownFlows.Add(f4c_west_flow_01.F4c_West_Flow_01());
            // KnownFlows.Add(f0b_phobos_flows.F0b_Phobos_Flow_01(FlowHelpers.DuplicateDungeonFlow(SewerPrefab.PatternSettings.flows[0])));
            // KnownFlows.Add(f0b_phobos_flows.F0b_Phobos_Flow_02(FlowHelpers.DuplicateDungeonFlow(SewerPrefab.PatternSettings.flows[1])));
            // KnownFlows.Add(f0b_office_flows.F0b_Office_Flow_01(FlowHelpers.DuplicateDungeonFlow(CathedralPrefab.PatternSettings.flows[0])));


            // Fix issues with nodes so that things other then MainMenu can load Foyer flow
            Foyer_Flow.name = "Foyer_Flow";
            Foyer_Flow.AllNodes[1].handlesOwnWarping = true;
            Foyer_Flow.AllNodes[2].handlesOwnWarping = true;
            Foyer_Flow.AllNodes[3].handlesOwnWarping = true;

            KnownFlows.Add(Foyer_Flow);
            KnownFlows.Add(FlowHelpers.DuplicateDungeonFlow(LoadOfficialFlow("npcparadise")));
            // KnownFlows.Add(FlowHelpers.DuplicateDungeonFlow(LoadOfficialFlow("secret_doublebeholster_flow")));

            // KnownFlows.Add(BossrushFlows.Bossrush_01_Castle);
            // KnownFlows.Add(BossrushFlows.Bossrush_01a_Sewer);
            // KnownFlows.Add(BossrushFlows.Bossrush_02_Gungeon);
            // KnownFlows.Add(BossrushFlows.Bossrush_02a_Cathedral);
            // KnownFlows.Add(BossrushFlows.Bossrush_03_Mines);
            // KnownFlows.Add(BossrushFlows.Bossrush_04_Catacombs);
            // KnownFlows.Add(BossrushFlows.Bossrush_05_Forge);
            // KnownFlows.Add(BossrushFlows.Bossrush_06_BulletHell);
            // KnownFlows.Add(BossrushFlows.MiniBossrush_01);

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
            

            // // Let's make things look cool and give all boss rush flows my new tiny exit room. :D
            // BossrushFlows.Bossrush_01a_Sewer.AllNodes[2].overrideExactRoom = ExpandPrefabs.tiny_exit;
            // BossrushFlows.Bossrush_02_Gungeon.AllNodes[6].overrideExactRoom = ExpandPrefabs.tiny_exit;
            // BossrushFlows.Bossrush_02a_Cathedral.AllNodes[2].overrideExactRoom = ExpandPrefabs.oldbulletking_room_01;
            // BossrushFlows.Bossrush_02a_Cathedral.AllNodes[3].overrideExactRoom = ExpandPrefabs.tiny_exit;
            // BossrushFlows.Bossrush_03_Mines.AllNodes[6].overrideExactRoom = ExpandPrefabs.tiny_exit;
            // BossrushFlows.Bossrush_04_Catacombs.AllNodes[6].overrideExactRoom = ExpandPrefabs.tiny_exit;
            // // Fix Forge Bossrush so it uses the correct boss foyer room for Dragun.
            // // Using the same foyer room for previous floors looks odd so I fixed it. :P
            // BossrushFlows.Bossrush_05_Forge.AllNodes[1].overrideExactRoom = ExpandPrefabs.DragunBossFoyerRoom;
            // BossrushFlows.Bossrush_05_Forge.AllNodes[3].overrideExactRoom = ExpandPrefabs.tiny_exit;

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

