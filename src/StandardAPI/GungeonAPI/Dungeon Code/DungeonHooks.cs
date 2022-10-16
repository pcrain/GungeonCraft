using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using Dungeonator;
using MonoMod.RuntimeDetour;
using ItemAPI;

namespace GungeonAPI
{
    public static class DungeonHooks
    {
        public static event Action<LoopDungeonGenerator, Dungeon, DungeonFlow, int> OnPreDungeonGeneration;
        public static event Action OnPostDungeonGeneration, OnFoyerAwake;
        private static GameManager targetInstance;
        public static FieldInfo m_assignedFlow =
            typeof(LoopDungeonGenerator).GetField("m_assignedFlow", BindingFlags.Instance | BindingFlags.NonPublic);

        private static Hook preDungeonGenHook = new Hook(
           typeof(LoopDungeonGenerator).GetConstructor(new Type[] { typeof(Dungeon), typeof(int) }),
           typeof(DungeonHooks).GetMethod("LoopGenConstructor")
        );

        private static Hook foyerAwakeHook = new Hook(
            typeof(MainMenuFoyerController).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance),
            typeof(DungeonHooks).GetMethod("FoyerAwake") //this no longer exists
        );

        //private static Hook roomEventsHook = new Hook(
        //    typeof(RoomHandler).GetMethod("ProcessRoomEvents", BindingFlags.Instance | BindingFlags.NonPublic),
        //    typeof(DungeonHooks).GetMethod("ProcessRoomEvents")
        //);

        //private static Hook flowHook = new Hook(
        //    typeof(DungeonFlow).GetMethod("NewGetNodeChildrenToBuild", BindingFlags.Instance | BindingFlags.Public),
        //    typeof(DungeonHooks).GetMethod("NewGetNodeChildrenToBuild")
        //);

        //private static Hook acquirePrototypeRoomHook = new Hook(
        //    typeof(LoopFlowBuilder).GetMethod("AcquirePrototypeRoom", BindingFlags.Instance | BindingFlags.NonPublic),
        //    typeof(DungeonHooks).GetMethod("AcquirePrototypeRoom")
        //);

        //private static Hook processSingleNodeInjectionHook = new Hook(
        //    typeof(LoopFlowBuilder).GetMethod("ProcessSingleNodeInjection", BindingFlags.Instance | BindingFlags.NonPublic),
        //    typeof(DungeonHooks).GetMethod("ProcessSingleNodeInjection")
        //);

        //private static Hook sanityCheck
        //Hook = new Hook(
        //    typeof(LoopFlowBuilder).GetMethod("SanityCheckRooms", BindingFlags.Instance | BindingFlags.NonPublic),
        //    typeof(DungeonHooks).GetMethod("SanityCheckRooms")
        //);

        //private static Hook roomTableHook = new Hook(
        //    typeof(WeightedRoomCollection).GetMethod("SelectByWeight", BindingFlags.Instance | BindingFlags.Public),
        //    typeof(DungeonHooks).GetMethod("SelectByWeight")
        //);

        public static void FoyerAwake(Action<MainMenuFoyerController> orig, MainMenuFoyerController self)
        {
            orig(self);
            OnFoyerAwake?.Invoke();
        }

        public static void LoopGenConstructor(Action<LoopDungeonGenerator, Dungeon, int> orig, LoopDungeonGenerator self, Dungeon dungeon, int dungeonSeed)
        {

            orig(self, dungeon, dungeonSeed);

            if (GameManager.Instance != null && GameManager.Instance != targetInstance)
            {
                targetInstance = GameManager.Instance;
                targetInstance.OnNewLevelFullyLoaded += OnLevelLoad;
            }

            var flow = (DungeonFlow)m_assignedFlow.GetValue(self);
            OnPreDungeonGeneration?.Invoke(self, dungeon, flow, dungeonSeed);
            dungeon = null;
        }

        public static void OnLevelLoad()
        {
            OnPostDungeonGeneration?.Invoke();
        }

        public static void ProcessRoomEvents(Action<RoomHandler, RoomEventTriggerCondition> orig, RoomHandler self, RoomEventTriggerCondition eventCondition)
        {
            orig(self, eventCondition);
        }

        public static List<WeightedRoomCollection> seenTables = new List<WeightedRoomCollection>();
        public static WeightedRoom SelectByWeight(Func<WeightedRoomCollection, WeightedRoom> orig, WeightedRoomCollection self)
        {
            try
            {
                if (!seenTables.Contains(self))
                {
                    //Tools.Log(self.name, "RoomTables/" + self.name + ".txt");
                    foreach (var wroom in self.elements)
                    {
                        Tools.Log(wroom.room.name, "RoomTables/" + seenTables.Count + ".txt");
                    }
                    seenTables.Add(self);
                }
            }
            catch (Exception e)
            {
                Tools.PrintException(e);
            }
            return orig(self);
        }

        public static void AcquirePrototypeRoom(Action<LoopFlowBuilder, BuilderFlowNode> orig, LoopFlowBuilder self, BuilderFlowNode buildData)
        {
            orig(self, buildData);
            if (buildData.assignedPrototypeRoom)
                if (buildData.assignedPrototypeRoom.category != PrototypeDungeonRoom.RoomCategory.NORMAL)
                    Tools.LogPropertiesAndFields(buildData.assignedPrototypeRoom, "\n" + buildData.assignedPrototypeRoom.name);
                else
                    Tools.Log("======================= NULL =======================\n");
        }


        public static void SanityCheckRooms(Action<LoopFlowBuilder, SemioticLayoutManager> orig, LoopFlowBuilder self, SemioticLayoutManager layout)
        {
            orig(self, layout);

            FieldInfo m_allBuilderNodes = typeof(LoopFlowBuilder).GetField("allBuilderNodes", BindingFlags.Instance | BindingFlags.NonPublic);
            var allBuilderNodes = (List<BuilderFlowNode>)m_allBuilderNodes.GetValue(self);
            for (int j = 0; j < allBuilderNodes.Count; j++)
            {
                BuilderFlowNode builderFlowNode = allBuilderNodes[j];
                if (builderFlowNode != null && builderFlowNode.assignedPrototypeRoom)
                {
                    string name = builderFlowNode.assignedPrototypeRoom.name.ToLower();
                    if (name.Contains("shrine") || name.Contains("glass"))
                    {
                        Tools.LogPropertiesAndFields(builderFlowNode, "Builder Flow Node");
                        Tools.LogPropertiesAndFields(builderFlowNode.assignedPrototypeRoom, "Proto Room");
                        Tools.LogPropertiesAndFields(builderFlowNode.assignedPrototypeRoom.requiredInjectionData, "InjectionData");
                    }
                }
            }
        }

        public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
        public static bool ProcessSingleNodeInjection(Func<LoopFlowBuilder, ProceduralFlowModifierData, BuilderFlowNode, RuntimeInjectionFlags, FlowCompositeMetastructure, RuntimeInjectionMetadata, bool> orig,
            LoopFlowBuilder self, ProceduralFlowModifierData pfmd, BuilderFlowNode root, RuntimeInjectionFlags flags, FlowCompositeMetastructure fcm, RuntimeInjectionMetadata rim = null)
        {
            //Tools.LogPropertiesAndFields(pfmd);
            //if (pfmd.exactRoom)
            //    Tools.Log("Exact Room: "+pfmd.exactRoom.name);
            //if (pfmd.exactSecondaryRoom)
            //    Tools.Log("Exact Secondary Room: " + pfmd.exactSecondaryRoom.name);
            //if (pfmd.roomTable)
            //    Tools.Log("Room Table: " + pfmd.roomTable.name);

            return orig(self, pfmd, root, flags, fcm, rim);
        }

        public static List<BuilderFlowNode> NewGetNodeChildrenToBuild(Func<DungeonFlow, BuilderFlowNode, LoopFlowBuilder, List<BuilderFlowNode>> orig, DungeonFlow self, BuilderFlowNode parentBuilderNode, LoopFlowBuilder builder)
        {
            var list = orig(self, parentBuilderNode, builder);
            try
            {
                foreach (var node in list)
                {

                }
            }
            catch (Exception e)
            {
                Tools.PrintException(e);
            }


            return list;
        }
    }
}