using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using Dungeonator;
using Random = UnityEngine.Random;
using ItemAPI;
namespace GungeonAPI
{
    public static class SampleFlow
    {
        public static DungeonFlow CreateDebugFlow(Dungeon dungeon)
        {
            var flow = SampleFlow.CreateEntranceExitFlow(dungeon);
            flow.name = "debug_flow";
            DungeonFlowNode
                customRoom,
                hub = new DungeonFlowNode(flow) { overrideExactRoom = RoomFactory.CreateEmptyRoom() },
                lastNode = hub;
            flow.AddNodeToFlow(hub, flow.FirstNode);
            foreach (var room in RoomFactory.rooms.Values)
            {
                Tools.Log("Adding room to flow: " + room.room);
                customRoom = new DungeonFlowNode(flow) { overrideExactRoom = room.room };
                flow.AddNodeToFlow(customRoom, lastNode);
                hub = new DungeonFlowNode(flow) { overrideExactRoom = RoomFactory.CreateEmptyRoom() };
                flow.AddNodeToFlow(hub, customRoom);
                lastNode = hub;
            }
            dungeon = null;
            return flow;
        }

        public static DungeonFlow CreateRoomTypeSampleFlow(Dungeon dungeon)
        {
            var flow = CreateNewFlow(dungeon);
            flow.name = "type_sample_flow";

            var entrance = NodeFromAssetName(flow, "elevator entrance");
            flow.FirstNode = entrance;
            flow.AddNodeToFlow(entrance, null);

            //one room for each visual definition
            DungeonFlowNode lastNode = flow.FirstNode;
            //ItemAPI.Tools.Print(dungeon.roomMaterialDefinitions?.Length);
            for (int i = 0; i < dungeon.roomMaterialDefinitions.Length; i++)
            {
                if (dungeon.name == OfficialFlows.dungeonPrefabNames[3] && i == 5) continue;
                var room = RoomFactory.CreateEmptyRoom(14, 14);
                room.overrideRoomVisualType = i;
                var shrineNode = new DungeonFlowNode(flow)
                {
                    overrideExactRoom = room
                };
                flow.AddNodeToFlow(shrineNode, lastNode);
                lastNode = shrineNode;
            }
            //exit
            flow.AddNodeToFlow(NodeFromAssetName(flow, "exit_room_basic"), lastNode);
            dungeon = null;
            return flow;
        }

        public static DungeonFlow CreateEntranceExitFlow(Dungeon dungeon)
        {
            var flow = CreateNewFlow(dungeon);
            flow.name = "entrance_exit_flow";

            var entrance = NodeFromAssetName(flow, "elevator entrance");
            flow.FirstNode = entrance;
            flow.AddNodeToFlow(entrance, null);
            flow.AddNodeToFlow(NodeFromAssetName(flow, "exit_room_basic"), entrance);
            dungeon = null;
            return flow;
        }

        public static DungeonFlow CreateMazeFlow(Dungeon dungeon)
        {
            var flow = CreateNewFlow(dungeon);
            flow.name = "maze_flow";

            var entrance = NodeFromAssetName(flow, "elevator entrance");
            flow.FirstNode = entrance;
            flow.AddNodeToFlow(entrance, null);
            var maze = new DungeonFlowNode(flow) { overrideExactRoom = RoomFactory.BuildFromResource("resource/rooms/maze.room").room };
            flow.AddNodeToFlow(maze, entrance);
            flow.AddNodeToFlow(NodeFromAssetName(flow, "exit_room_basic"), maze);
            dungeon = null;
            return flow;
        }

        public static DungeonFlow CreateNewFlow(Dungeon dungeon)
        {
            var flow = ScriptableObject.CreateInstance<DungeonFlow>();
            flow.subtypeRestrictions = new List<DungeonFlowSubtypeRestriction>() { new DungeonFlowSubtypeRestriction() };
            flow.flowInjectionData = new List<ProceduralFlowModifierData>();
            flow.sharedInjectionData = new List<SharedInjectionData>();

            var roomTable = dungeon.PatternSettings.flows[0].fallbackRoomTable;
            flow.fallbackRoomTable = roomTable;
            flow.evolvedRoomTable = roomTable;
            flow.phantomRoomTable = roomTable;
            flow.Initialize();

            dungeon = null;
            return flow;
        }

        public static DungeonFlowNode NodeFromAssetName(DungeonFlow flow, string name)
        {
            DungeonFlowNode node = new DungeonFlowNode(flow);
            string asset = name;
            var room = RoomFromAssetName(name);
            if (room == null)
            {
                Tools.Print("Error loading room " + name, "FF0000");
            };
            node.overrideExactRoom = room;
            return node;
        }

        public static PrototypeDungeonRoom RoomFromAssetName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            string asset = name;
            if (name.Contains('/'))
                asset = name.Substring(name.LastIndexOf('/') + 1).Replace(".asset", "").Trim();

            var room = StaticReferences.GetAsset<PrototypeDungeonRoom>(asset); //check both assetbundles
            return room;
        }

        public static void ListNodes(this DungeonFlow flow)
        {
            Tools.Print(flow.name + " node:");
            Tools.Print("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            foreach (var node in flow.AllNodes)
            {
                if (node != null && node.overrideExactRoom)
                    Tools.Print(node.overrideExactRoom);
            }
        }
    }
}