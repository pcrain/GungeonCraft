
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using Dungeonator;
using Random = UnityEngine.Random;
using CustomShrineData = GungeonAPI.ShrineFactory.CustomShrineController;
using RoomData = GungeonAPI.RoomFactory.RoomData;
using RoomCategory = PrototypeDungeonRoom.RoomCategory;
using RoomNormalSubCategory = PrototypeDungeonRoom.RoomNormalSubCategory;
using RoomBossSubCategory = PrototypeDungeonRoom.RoomBossSubCategory;
using RoomSpecialSubCategory = PrototypeDungeonRoom.RoomSpecialSubCategory;
using ItemAPI;
namespace GungeonAPI
{

    public static class DungeonHandler
    {
        //public static float GlobalRoomWeight = 1.5f;
        private static bool initialized = false;
        public static bool debugFlow = false;

        public static void Init()
        {
            if (!initialized)
            {              
                DungeonHooks.OnPreDungeonGeneration += OnPreDungeonGen;
                initialized = true;
            }
        }

        public static void OnPreDungeonGen(LoopDungeonGenerator generator, Dungeon dungeon, DungeonFlow flow, int dungeonSeed)
        {
            // Tools.Print("Attempting to override floor layout...", "5599FF");
            //CollectDataForAnalysis(flow, dungeon);
            if (flow.name != "Foyer Flow" && !GameManager.IsReturningToFoyerWithPlayer)
            {
                if (debugFlow)
                {
                    flow = SampleFlow.CreateDebugFlow(dungeon);
                    generator.AssignFlow(flow);
                }
                //  Tools.Print("Dungeon name: " + dungeon.name);
                // Tools.Print("Override Flow set to: " + flow.name);
            }
            dungeon = null;
        }

        public static void Register(RoomData roomData)
        {
            var room = roomData.room;
            var wRoom = new WeightedRoom()
            {
                room = room,
                additionalPrerequisites = new DungeonPrerequisite[0],
                weight = roomData.weight
            };

            AssetBundle shared_auto_001 = ResourceManager.LoadAssetBundle("shared_auto_001");

            GameObject iconPrefab = RoomFactory.MinimapIconPrefab ?? (shared_auto_001.LoadAsset("assets/data/prefabs/room icons/minimap_boss_icon.prefab") as GameObject);
            //bool success = false;
            switch (room.category)
            {
                case RoomCategory.SPECIAL:
                    switch (room.subCategorySpecial)
                    {
                        case RoomSpecialSubCategory.STANDARD_SHOP:  //shops
                            StaticReferences.RoomTables["shop"].includedRooms.Add(wRoom);
                            // Tools.Print($"Registering {roomData.room.name} with weight {wRoom.weight} as {roomData.category}:{roomData.specialSubCategory}");
                            //   success = true;
                            break;
                        case RoomSpecialSubCategory.WEIRD_SHOP:    //subshops
                            StaticReferences.subShopTable.InjectionData.AddRange(GetFlowModifier(roomData));
                            /// Tools.Print($"Registering {roomData.room.name} with weight {wRoom.weight} as {roomData.category}:{roomData.specialSubCategory}");
                            // success = true;
                            break;
                        default:
                            StaticReferences.RoomTables["special"].includedRooms.Add(wRoom);
                            //Tools.Print($"Registering {roomData.room.name} with weight {wRoom.weight} as {roomData.category}:{roomData.specialSubCategory}");
                            // success = true;
                            break;
                    }
                    break;
                case RoomCategory.SECRET:
                    StaticReferences.RoomTables["secret"].includedRooms.Add(wRoom);
                    //success = true;
                    break;
                //===========================PUTS YOUR BOSS ROOMS IN THE POOLS DEFINED IN STATICREFERENCES ====================
                case RoomCategory.BOSS:
                    switch (room.subCategoryBoss)
                    {
                        case RoomBossSubCategory.FLOOR_BOSS:
                            foreach (var p in room.prerequisites)

                                if (p.requiredTileset == GlobalDungeonData.ValidTilesets.CASTLEGEON)
                                {
                                    StaticReferences.RoomTables["boss1"].includedRooms.Add(wRoom);
                                    StaticReferences.RoomTables["boss2"].includedRooms.Add(wRoom);
                                    StaticReferences.RoomTables["boss3"].includedRooms.Add(wRoom);
                                }
                                else if (p.requiredTileset == GlobalDungeonData.ValidTilesets.SEWERGEON)
                                {
                                    StaticReferences.RoomTables["blobby"].includedRooms.Add(wRoom);
                                }
                                else if (p.requiredTileset == GlobalDungeonData.ValidTilesets.GUNGEON)
                                {
                                    StaticReferences.RoomTables["gorgun"].includedRooms.Add(wRoom);
                                    StaticReferences.RoomTables["beholster"].includedRooms.Add(wRoom);
                                    StaticReferences.RoomTables["ammoconda"].includedRooms.Add(wRoom);
                                }
                                else if (p.requiredTileset == GlobalDungeonData.ValidTilesets.CATHEDRALGEON)
                                {
                                    StaticReferences.RoomTables["oldking"].includedRooms.Add(wRoom);
                                }
                                else if (p.requiredTileset == GlobalDungeonData.ValidTilesets.MINEGEON)
                                {
                                    StaticReferences.RoomTables["tank"].includedRooms.Add(wRoom);
                                    StaticReferences.RoomTables["cannonballrog"].includedRooms.Add(wRoom);
                                    StaticReferences.RoomTables["flayer"].includedRooms.Add(wRoom);
                                }
                                else if (p.requiredTileset == GlobalDungeonData.ValidTilesets.CATHEDRALGEON)
                                {
                                    StaticReferences.RoomTables["pillars"].includedRooms.Add(wRoom);
                                    StaticReferences.RoomTables["priest"].includedRooms.Add(wRoom);
                                    StaticReferences.RoomTables["monger"].includedRooms.Add(wRoom);
                                }
                                else
                                {
                                    //StaticReferences.RoomTables["doorlord"].includedRooms.Add(wRoom);
                                }
                            room.associatedMinimapIcon = iconPrefab;

                            break;
                        case RoomBossSubCategory.MINI_BOSS:
                            StaticReferences.RoomTables["blockner"].includedRooms.Add(wRoom);
                            StaticReferences.RoomTables["shadeagunim"].includedRooms.Add(wRoom);
                            //StaticReferences.RoomTables["fuselier"].includedRooms.Add(wRoom);
                            room.associatedMinimapIcon = iconPrefab;
                            break;
                        default:
                            //StaticReferences.RoomTables["doorlord"].includedRooms.Add(wRoom);
                            // room.associatedMinimapIcon = iconPrefab;
                            break;
                    }
                    break;

                //===============================================
                default:
                    foreach (var p in room.prerequisites)
                        if (p.requireTileset)
                            StaticReferences.GetRoomTable(p.requiredTileset).includedRooms.Add(wRoom);
                    //   success = true;
                    break;
            }

            RemoveTilesetPrereqs(room);


        }
        public static GameObject MinimapShrineIconPrefab;

        public static void RegisterForShrine(RoomData roomData)
        {
            var room = roomData.room;
            var wRoom = new WeightedRoom()
            {
                room = room,
                additionalPrerequisites = new DungeonPrerequisite[0],
                weight = roomData.weight
            };
            //AssetBundle shared_auto_001 = ResourceManager.LoadAssetBundle("shared_auto_001");

            GameObject iconPrefab = (GameObject)BraveResources.Load("Global Prefabs/Minimap_Shrine_Icon", ".prefab");
            room.associatedMinimapIcon = iconPrefab;
            // bool success = false;
            switch (room.category)
            {
                case RoomCategory.SPECIAL:
                    switch (room.subCategorySpecial)
                    {
                        case RoomSpecialSubCategory.STANDARD_SHOP:  //shops
                            StaticReferences.RoomTables["shop"].includedRooms.Add(wRoom);
                            // Tools.Print($"Registering {roomData.room.name} with weight {wRoom.weight} as {roomData.category}:{roomData.specialSubCategory}");
                            //    success = true;
                            break;
                        case RoomSpecialSubCategory.WEIRD_SHOP:    //subshops
                            StaticReferences.subShopTable.InjectionData.AddRange(GetFlowModifier(roomData));
                            /// Tools.Print($"Registering {roomData.room.name} with weight {wRoom.weight} as {roomData.category}:{roomData.specialSubCategory}");
                        //    success = true;
                            break;
                        default:
                            StaticReferences.RoomTables["special"].includedRooms.Add(wRoom);
                            //Tools.Print($"Registering {roomData.room.name} with weight {wRoom.weight} as {roomData.category}:{roomData.specialSubCategory}");
                            //  success = true;
                            break;
                    }
                    break;
                case RoomCategory.SECRET:
                    StaticReferences.RoomTables["secret"].includedRooms.Add(wRoom);
                    //success = true;
                    break;
                case RoomCategory.BOSS:
                    // TODO
                    break;
                default:
                    foreach (var p in room.prerequisites)
                        if (p.requireTileset)
                            StaticReferences.GetRoomTable(p.requiredTileset).includedRooms.Add(wRoom);
                    // success = true;
                    break;
            }
            //success = true;
            RemoveTilesetPrereqs(room);


        }

        public static List<ProceduralFlowModifierData> GetFlowModifier(RoomData roomData)
        {
            var room = roomData.room;
            List<ProceduralFlowModifierData> data = new List<ProceduralFlowModifierData>();
            var tilesetPrereqs = new List<DungeonPrerequisite>();
            foreach (var p in room.prerequisites)
            {
                if (p.requireTileset)
                {
                    data.Add(new ProceduralFlowModifierData()
                    {

                        annotation = room.name,
                        placementRules = new List<ProceduralFlowModifierData.FlowModifierPlacementType>()
                        {
                            ProceduralFlowModifierData.FlowModifierPlacementType.END_OF_CHAIN,
                            ProceduralFlowModifierData.FlowModifierPlacementType.HUB_ADJACENT_NO_LINK,
                        },
                        exactRoom = room,
                        selectionWeight = roomData.weight,
                        chanceToSpawn = 1,
                        prerequisites = new DungeonPrerequisite[] { p }, //doesn't include all the other prereqs, pls fix
                        CanBeForcedSecret = true,
                    });
                }
            }

            RemoveTilesetPrereqs(room);
            if (data.Count == 0)
            {
                data.Add(new ProceduralFlowModifierData()
                {

                    annotation = room.name,
                    placementRules = new List<ProceduralFlowModifierData.FlowModifierPlacementType>()
                        {
                            ProceduralFlowModifierData.FlowModifierPlacementType.END_OF_CHAIN,
                            ProceduralFlowModifierData.FlowModifierPlacementType.HUB_ADJACENT_NO_LINK,
                        },
                    exactRoom = room,
                    selectionWeight = roomData.weight,
                    chanceToSpawn = 1,
                    prerequisites = new DungeonPrerequisite[0], //doesn't include all the other prereqs, pls fix
                    CanBeForcedSecret = true,
                });
            }


            return data;
        }

        public static void RemoveTilesetPrereqs(PrototypeDungeonRoom room)
        {
            var tilesetPrereqs = new List<DungeonPrerequisite>();
            foreach (var p in room.prerequisites)
            {
                if (p.requireTileset)
                    tilesetPrereqs.Add(p);
            }

            foreach (var p in tilesetPrereqs)
                room.prerequisites.Remove(p);
        }

        public static bool BelongsOnThisFloor(RoomData data, string dungeonName)
        {
            if (data.floors == null || data.floors.Length == 0) return true;
            bool onThisFloor = false;
            foreach (var floor in data.floors)
            {
                if (floor.ToLower().Equals(dungeonName.ToLower())) { onThisFloor = true; break; }
            }
            return onThisFloor;
        }

        public static GenericRoomTable GetSpecialRoomTable()
        {
            foreach (var entry in GameManager.Instance.GlobalInjectionData.entries)
                if (entry.injectionData?.InjectionData != null)
                    foreach (var data in entry.injectionData.InjectionData)
                    {
                        if (data.exactRoom != null)
                        {
                            Tools.Log(data.exactRoom.name);

                            if (data.prerequisites != null)
                                foreach (var p in data.prerequisites)
                                    Tools.Log("\t" + p.prerequisiteType);

                            if (data.placementRules != null)
                                foreach (var p in data.placementRules)
                                    Tools.Log("\t" + p);
                        }
                    }
            return null;
        }

        public static void CollectDataForAnalysis(DungeonFlow flow, Dungeon dungeon)
        {
            try
            {
                //GetSpecialRoomTable();
                foreach (var room in flow.fallbackRoomTable.includedRooms.elements)
                {
                    // Tools.Print("Fallback table: " + room?.room?.name);
                }
            }
            catch (Exception e)
            {
                Tools.PrintException(e);
            }
            dungeon = null;
        }

        public static void LogProtoRoomData(PrototypeDungeonRoom room)
        {
            int i = 0;
            Tools.LogPropertiesAndFields(room, "ROOM");
            foreach (var placedObject in room.placedObjects)
            {
                Tools.Log($"\n----------------Object #{i++}----------------");
                Tools.LogPropertiesAndFields(placedObject, "PLACED OBJECT");
                Tools.LogPropertiesAndFields(placedObject?.placeableContents, "PLACEABLE CONTENT");
                Tools.LogPropertiesAndFields(placedObject?.placeableContents?.variantTiers[0], "VARIANT TIERS");
            }

            Tools.Print("==LAYERS==");
            foreach (var layer in room.additionalObjectLayers)
            {
                //Tools.LogPropertiesAndFields(layer);
            }
        }
    }
}