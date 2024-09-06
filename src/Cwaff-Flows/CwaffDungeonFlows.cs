namespace CwaffingTheGungy;

public class CwaffDungeonFlow {

    internal static List<DungeonFlow> _KnownFlows;

    public static void InitDungeonFlows(AssetBundle sharedAssets2, bool refreshFlows = false) {
        _KnownFlows = new List<DungeonFlow>(){
            SansDungeonFlow.Init(),
        };
    }

    [HarmonyPatch(typeof(FlowDatabase), nameof(FlowDatabase.GetOrLoadByName))]
    private class LoadCustomFlowPatch // Patch for making sure we can load custom flows okay
    {
        static bool Prefix(string name, ref DungeonFlow __result)
        {
            if (_KnownFlows == null || _KnownFlows.Count <= 0)
                return true;

            string flowName = name.ToLower();
            if (flowName.Contains("/"))
                flowName = flowName.Substring(flowName.LastIndexOf("/") + 1);

            foreach (DungeonFlow flow in _KnownFlows)
            {
                if (!string.IsNullOrEmpty(flow.name) && flowName == flow.name.ToLower())
                {
                    __result = flow;
                    return false;
                }
            }
            return true;
        }
    }

    // stolen from Apache
    public static void PrintBossManager()
    {
        ETGModConsole.Log(string.Format("{0}.PrintBossManager(): Currently loaded BossManager data below (incomplete output).", typeof(CwaffDungeonFlow)));
        ETGModConsole.Log(string.Format("Prior floor selected boss room: {0}", BossManager.PriorFloorSelectedBossRoom));
        foreach (BossFloorEntry bossFloorEntry in GameManager.Instance.BossManager.BossFloorData) {
            ETGModConsole.Log(string.Format("BossFloorEntry: {0} '{1}' '{2}'", bossFloorEntry.AssociatedTilesets, bossFloorEntry.Annotation, bossFloorEntry.ToString()));
            foreach (IndividualBossFloorEntry individualBossFloorEntry in bossFloorEntry.Bosses) {
                ETGModConsole.Log(string.Format(" Individual: '{0}' ({1}) (#prerequisites={2})", individualBossFloorEntry.ToString(), individualBossFloorEntry.BossWeight, individualBossFloorEntry.GlobalBossPrerequisites.Length));
                foreach (WeightedRoom weightedRoom in individualBossFloorEntry.TargetRoomTable.GetCompiledList()) {
                    ETGModConsole.Log(string.Format("  Room: '{0}' (#prerequisites={1})", weightedRoom.room, weightedRoom.additionalPrerequisites.Length));
                }
            }
        }
    }

    // stolen from Apache
    public static void PrintFlow(DungeonFlow flow)
    {
        bool flag = flow == null;
        if (!flag) {
            try {
                ETGModConsole.Log(string.Format("DungeonFlow: '{0}' '{1}'", flow, flow.name));
                // bool flag2 = flow.flowInjectionData != null;
                // if (flag2) {
                //  foreach (ProceduralFlowModifierData arg in flow.flowInjectionData) {
                //      ETGModConsole.Log(string.Format(" ProceduralFlowModifierData: {0}", arg));
                //  }
                // }
                // bool flag3 = flow.sharedInjectionData != null;
                // if (flag3) {
                //  foreach (SharedInjectionData arg2 in flow.sharedInjectionData) {
                //      ETGModConsole.Log(string.Format(" SharedInjectionData: {0}", arg2));
                //  }
                // }
                bool flag4 = flow.phantomRoomTable != null;
                if (flag4) {
                    foreach (WeightedRoom weightedRoom in flow.phantomRoomTable.GetCompiledList()) {
                        string format = "  PhantomRoom: '{0}' '{1}'";
                        object arg3 = weightedRoom;
                        PrototypeDungeonRoom room = weightedRoom.room;
                        ETGModConsole.Log(string.Format(format, arg3, (room != null) ? room.name : null));
                    }
                }
                bool flag5 = flow.fallbackRoomTable != null;
                if (flag5) {
                    foreach (WeightedRoom weightedRoom2 in flow.fallbackRoomTable.GetCompiledList()) {
                        string format2 = "  FallbackRoom: '{0}' '{1}'";
                        object arg4 = weightedRoom2;
                        PrototypeDungeonRoom room2 = weightedRoom2.room;
                        ETGModConsole.Log(string.Format(format2, arg4, (room2 != null) ? room2.name : null));
                    }
                }
                foreach (DungeonFlowNode dungeonFlowNode in flow.AllNodes) {
                    ETGModConsole.Log(string.Format(" Flow Node: {0} {1} (iswarpwingentrance={2}) ({3}) (globalboss={4}) (roomcategory={5}) (override={6})", new object[] {
                        dungeonFlowNode.priority,
                        dungeonFlowNode.guidAsString,
                        dungeonFlowNode.isWarpWingEntrance,
                        dungeonFlowNode.handlesOwnWarping,
                        dungeonFlowNode.UsesGlobalBossData,
                        dungeonFlowNode.roomCategory,
                        dungeonFlowNode.overrideExactRoom
                    }));
                    bool flag6 = dungeonFlowNode.overrideRoomTable != null;
                    if (flag6) {
                        foreach (WeightedRoom weightedRoom3 in dungeonFlowNode.overrideRoomTable.GetCompiledList()) {
                            string str = "  Possible Room Table: ";
                            PrototypeDungeonRoom room3 = weightedRoom3.room;
                            ETGModConsole.Log(str + ((room3 != null) ? room3.name : null));
                        }
                    }
                    ETGModConsole.Log("  Parent: " + dungeonFlowNode.parentNodeGuid);
                    foreach (string str2 in dungeonFlowNode.childNodeGuids) {
                        ETGModConsole.Log("  Child: " + str2);
                    }
                    ETGModConsole.Log(string.Format("  Loop: {0} {1} {2}", dungeonFlowNode.loopForcedDoorType, dungeonFlowNode.loopTargetIsOneWay, dungeonFlowNode.loopTargetNodeGuid));
                    ETGModConsole.Log(string.Format("  Subchain Identifiers: '{0}' #{1} ('{2}')", dungeonFlowNode.subchainIdentifier, dungeonFlowNode.subchainIdentifiers.Count, string.Join("','", dungeonFlowNode.subchainIdentifiers.ToArray())));
                    ETGModConsole.Log(string.Format("  Door Types: {0} {1}", dungeonFlowNode.forcedDoorType, dungeonFlowNode.loopForcedDoorType));
                    foreach (ChainRule chainRule in dungeonFlowNode.chainRules) {
                        ETGModConsole.Log(string.Format("  Chain Rule: {0} '{1}' '{2}' {3}", new object[] {
                            chainRule.mandatory,
                            chainRule.form,
                            chainRule.target,
                            chainRule.weight
                        }));
                    }
                }
            } catch (Exception ex) {
                ETGModConsole.Log("Exception caught and will not cause errors. Just fix the printing code instead!");
                ETGModConsole.Log(ex.ToString());
            }
        }
    }
}
