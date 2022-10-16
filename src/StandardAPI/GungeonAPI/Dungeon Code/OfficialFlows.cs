using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;

namespace GungeonAPI
{
    class OfficialFlows
    {
        public enum FLOORS {
            KEEP,
            PROPER,
            MINES,
            HOLLOW,
            FORGE,
            HELL
        }

        public static string[] dungeonPrefabNames =
        {
            "Base_Castle",
            "Base_Gungeon",
            "Base_Mines",
            "Base_Catacombs",
            "Base_Forge",
            "Base_Sewer",
            "Base_Cathedral ",
            "Base_ResourcefulRat",
            "Base_Nakatomi",
            "Base_BulletHell",
        };

        public static string[] dungeonPrefabNamesInOrder =
        {
            "Foyer",
            "Base_Castle",
            "Base_Sewer",
            "Base_Gungeon",
            "Base_Cathedral",
            "Base_Mines",
            "Base_Catacombs",
            "Base_Forge",
            "Base_BulletHell",
        };

        public static string[] dungeonSceneNamesInOrder =
        {
            "tt_foyer",
            "tt_castle",
            "tt_sewer",
            "tt5",
            "tt_cathedral",
            "tt_mines",
            "tt_catacombs",
            "tt_forge",
            "tt_bullethell",
        };

        public static int GetLevelIndex(string dungeonName)
        {
            for (int i = 0; i < dungeonPrefabNames.Length; i++)
            {
                if (dungeonPrefabNames[i].ToLower().Contains(dungeonName.ToLower()))
                    return i;
            }
            return -1;
        }

        public static Dungeon GetDungeonPrefab(string floor)
        {
            return DungeonDatabase.GetOrLoadByName(floor);
        }

        public static Dungeon GetDungeonPrefab(int floor)
        { return DungeonDatabase.GetOrLoadByName(dungeonPrefabNames[floor]); }

        public static List<PrototypeDungeonRoom> GetRoomsFromRoomTables(string floor)
        {
            var dungeon = GetDungeonPrefab(floor);
            var rooms = new List<PrototypeDungeonRoom>();
            for (int i = 0; i < dungeon.PatternSettings.flows.Count; i++)
            {
                foreach (var elem in dungeon.PatternSettings.flows[i].fallbackRoomTable.includedRooms.elements)
                {
                    rooms.Add(elem.room);
                }
            }
            dungeon = null;
            return rooms;
        }

        public static List<PrototypeDungeonRoom> GetRoomsFromRoomTables(int floor)
        { return GetRoomsFromRoomTables(dungeonPrefabNames[floor]); }

        public static PrototypeDungeonRoom GetRoomFromDungeon(string roomName, string floor)
        {
            roomName = roomName.ToLower();
            var rooms = GetRoomsFromRoomTables(floor);
            foreach (var room in rooms)
            {
                Tools.Log(room.name, "roomnames.txt");
                if (room.name.ToLower().Equals(roomName))
                {
                    return room;
                }
            }
            var nodes = OfficialFlows.GetAllFlowNodes(floor);
            if (nodes == null) return null;
            foreach (var node in nodes)
            {
                var overrideRoom = node.overrideExactRoom;

                if(overrideRoom != null)
                    Tools.Log(overrideRoom.name, "roomnames.txt");
                if (overrideRoom != null && overrideRoom.name.ToLower().Equals(roomName))
                {
                    return overrideRoom;
                }
            }
            return null;
        }

        public static PrototypeDungeonRoom GetRoomFromDungeon(string roomName, int floor)
        { return GetRoomFromDungeon(roomName, dungeonPrefabNames[floor]); }

        public static DungeonFlowNode GetNodeFromDungeon(string roomName, string floor)
        {
            roomName = roomName.ToLower();
            var nodes = OfficialFlows.GetAllFlowNodes(floor);
            if (nodes == null) return null;
            foreach (var node in nodes)
            {
                var overrideRoom = node.overrideExactRoom;
                if (overrideRoom != null && overrideRoom.name.ToLower().Equals(roomName))
                {
                    return node;
                }
            }
            return null;
        }

        public static List<DungeonFlowNode> GetAllFlowNodes(string floor)
        {
            var dungeon = GetDungeonPrefab(floor);
            var nodes = dungeon.PatternSettings.flows[0].AllNodes;
            for (int i = 1; i < dungeon.PatternSettings.flows.Count; i++)
            {
                nodes.Concat(dungeon.PatternSettings.flows[i].AllNodes);
            }
            dungeon = null;
            return nodes;
        }

        public static List<DungeonFlowNode> GetAllFlowNodes(int floor)
        { return GetAllFlowNodes(dungeonPrefabNames[floor]); }
    }
}
