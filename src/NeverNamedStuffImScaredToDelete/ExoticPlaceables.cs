using Alexandria.ItemAPI;
using Dungeonator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CwaffingTheGungy
{
    class ExoticPlaceables
    {
        public static void Init()
        {
            #region SpecialLoot
            NonRatStealableArmor = PickupObjectDatabase.GetById(120).gameObject.InstantiateAndFakeprefab();
            NonRatStealableArmor.GetComponent<PickupObject>().IgnoredByRat = true;

            NonRatStealableAmmo = PickupObjectDatabase.GetById(78).gameObject.InstantiateAndFakeprefab();
            NonRatStealableAmmo.GetComponent<PickupObject>().IgnoredByRat = true;

            NonRatStealableSpreadAmmo = PickupObjectDatabase.GetById(600).gameObject.InstantiateAndFakeprefab();
            NonRatStealableSpreadAmmo.GetComponent<PickupObject>().IgnoredByRat = true;

            MapPlaceable = PickupObjectDatabase.GetById(137).gameObject.InstantiateAndFakeprefab();
            MapPlaceable.GetComponent<PickupObject>().IgnoredByRat = true;
            MapPlaceable.GetOrAddComponent<SquishyBounceWiggler>();

            GlassGuonPlaceable = PickupObjectDatabase.GetById(565).gameObject.InstantiateAndFakeprefab();
            GlassGuonPlaceable.GetComponent<PickupObject>().IgnoredByRat = true;
            GlassGuonPlaceable.GetOrAddComponent<SquishyBounceWiggler>();

            FiftyCasingPlaceable = PickupObjectDatabase.GetById(74).gameObject.InstantiateAndFakeprefab();
            if (FiftyCasingPlaceable.GetComponent<PickupMover>()) UnityEngine.Object.Destroy(FiftyCasingPlaceable.GetComponent<PickupMover>());

            SingleCasingPlaceable = PickupObjectDatabase.GetById(68).gameObject.InstantiateAndFakeprefab();
            if (SingleCasingPlaceable.GetComponent<PickupMover>()) UnityEngine.Object.Destroy(SingleCasingPlaceable.GetComponent<PickupMover>());

            FiveCasingPlaceable = PickupObjectDatabase.GetById(70).gameObject.InstantiateAndFakeprefab();
            if (FiveCasingPlaceable.GetComponent<PickupMover>()) UnityEngine.Object.Destroy(FiveCasingPlaceable.GetComponent<PickupMover>());
            #endregion
            #region NonFloorStolen
            BulletKingThrone = EnemyDatabase.GetOrLoadByGuid("ffca09398635467da3b1f4a54bcfda80").GetComponent<BulletKingDeathController>().thronePrefab.InstantiateAndFakeprefab();
            OldKingThrone = EnemyDatabase.GetOrLoadByGuid("5729c8b5ffa7415bb3d01205663a33ef").GetComponent<BulletKingDeathController>().thronePrefab.InstantiateAndFakeprefab();
            GameObject skeletonNote = LoadHelper.LoadAssetFromAnywhere<GameObject>("assets/data/prefabs/interactable objects/notes/skeleton_note_001.prefab");
            SecretRoomSkeleton = skeletonNote.transform.Find("skleleton").gameObject.InstantiateAndFakeprefab();
            #endregion
            #region Oubliette
            Dungeon sewerDungeon = DungeonDatabase.GetOrLoadByName("base_sewer");
            foreach (WeightedRoom wRoom in sewerDungeon.PatternSettings.flows[0].fallbackRoomTable.includedRooms.elements)
            {
                if (wRoom.room != null && !string.IsNullOrEmpty(wRoom.room.name))
                {
                    if (wRoom.room.name.ToLower().StartsWith("sewer_trash_compactor_001"))
                    {
                        HorizontalCrusher = wRoom.room.placedObjects[0].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                    }
                }
            }
            sewerDungeon = null;
            #endregion
            #region Abbey
            Dungeon abbeyDungeon = DungeonDatabase.GetOrLoadByName("base_cathedral");
            foreach (WeightedRoom wRoom in abbeyDungeon.PatternSettings.flows[0].fallbackRoomTable.includedRooms.elements)
            {
                if (wRoom.room != null && !string.IsNullOrEmpty(wRoom.room.name))
                {
                    if (wRoom.room.name.ToLower().StartsWith("cathedral_brent_standard_02"))
                    {
                        Pew = wRoom.room.placedObjects[0].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                    }
                }
            }
            abbeyDungeon = null;
            #endregion
            #region R&GDept
            Dungeon rngDungeon = DungeonDatabase.GetOrLoadByName("base_nakatomi");
            if (rngDungeon)
            {
                if (rngDungeon.PatternSettings.flows[0].name == "FS4_Nakatomi_Flow")
                {
                    if (rngDungeon.PatternSettings.flows[0].AllNodes.Count == 14)
                    {               
                        MopAndBucket =  rngDungeon.PatternSettings.flows[0].AllNodes[0].overrideExactRoom.placedObjects[0].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        CardboardBox3 = rngDungeon.PatternSettings.flows[0].AllNodes[0].overrideExactRoom.placedObjects[2].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        ACUnit = rngDungeon.PatternSettings.flows[0].AllNodes[1].overrideExactRoom.placedObjects[1].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        ACVent = rngDungeon.PatternSettings.flows[0].AllNodes[1].overrideExactRoom.placedObjects[2].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        KitchenChairFront = rngDungeon.PatternSettings.flows[0].AllNodes[4].overrideExactRoom.placedObjects[1].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        KitchenChairLeft = rngDungeon.PatternSettings.flows[0].AllNodes[4].overrideExactRoom.placedObjects[8].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        KitchenChairRight = rngDungeon.PatternSettings.flows[0].AllNodes[4].overrideExactRoom.placedObjects[12].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        KitchenCounter = rngDungeon.PatternSettings.flows[0].AllNodes[4].overrideExactRoom.placedObjects[16].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        SteelTableHorizontal = rngDungeon.PatternSettings.flows[0].AllNodes[4].overrideExactRoom.placedObjects[6].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        SteelTableVertical = rngDungeon.PatternSettings.flows[0].AllNodes[4].overrideExactRoom.placedObjects[3].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        BathroomStallDividerNorth = rngDungeon.PatternSettings.flows[0].AllNodes[6].overrideExactRoom.placedObjects[0].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        BathroomStallDividerEast = rngDungeon.PatternSettings.flows[0].AllNodes[6].overrideExactRoom.placedObjects[6].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        BathroomStallDividerWest = rngDungeon.PatternSettings.flows[0].AllNodes[6].overrideExactRoom.placedObjects[9].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        ToiletNorth = rngDungeon.PatternSettings.flows[0].AllNodes[6].overrideExactRoom.placedObjects[2].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        ToiletEast = rngDungeon.PatternSettings.flows[0].AllNodes[6].overrideExactRoom.placedObjects[7].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        ToiletWest = rngDungeon.PatternSettings.flows[0].AllNodes[6].overrideExactRoom.placedObjects[10].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        GlassWallVertical = rngDungeon.PatternSettings.flows[0].AllNodes[7].overrideExactRoom.placedObjects[0].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        GlassWallHorizontal = rngDungeon.PatternSettings.flows[0].AllNodes[7].overrideExactRoom.placedObjects[6].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        LargeDesk = rngDungeon.PatternSettings.flows[0].AllNodes[8].overrideExactRoom.placedObjects[0].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        TechnoFloorCellEmpty = rngDungeon.PatternSettings.flows[0].AllNodes[10].overrideExactRoom.placedObjects[0].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        TechnoFloorCellCaterpillar = rngDungeon.PatternSettings.flows[0].AllNodes[10].overrideExactRoom.placedObjects[4].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        TechnoFloorCellLeever = rngDungeon.PatternSettings.flows[0].AllNodes[10].overrideExactRoom.placedObjects[13].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        TechnoFloorCellSpider = rngDungeon.PatternSettings.flows[0].AllNodes[10].overrideExactRoom.placedObjects[14].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        WideComputerBreakable = rngDungeon.PatternSettings.flows[0].AllNodes[10].overrideExactRoom.placedObjects[6].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        MetalCrate = rngDungeon.PatternSettings.flows[0].AllNodes[10].overrideExactRoom.placedObjects[10].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        HologramWallHorizontal = rngDungeon.PatternSettings.flows[0].AllNodes[11].overrideExactRoom.placedObjects[0].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        HologramWallVertical = rngDungeon.PatternSettings.flows[0].AllNodes[11].overrideExactRoom.placedObjects[7].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        VentilationTube = rngDungeon.PatternSettings.flows[0].AllNodes[11].overrideExactRoom.placedObjects[8].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        TallComputerBreakable = rngDungeon.PatternSettings.flows[0].AllNodes[11].overrideExactRoom.placedObjects[13].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        AgunimBossMatt = rngDungeon.PatternSettings.flows[0].AllNodes[12].overrideExactRoom.placedObjects[1].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        AlienTank = rngDungeon.PatternSettings.flows[0].AllNodes[13].overrideExactRoom.placedObjects[9].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        DecorativeElectricFloor = rngDungeon.PatternSettings.flows[0].AllNodes[13].overrideExactRoom.placedObjects[29].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                    }
                    else { ETGModConsole.Log("<color=#ff0000ff>ERROR: R&G DEPARTMENT FLOW 0 HAS AN INCORRECT AMOUNT OF NODES</color>"); }
                }
                else { ETGModConsole.Log("<color=#ff0000ff>ERROR: R&G DEPARTMENT FLOW 0 HAS AN INCORRECT NAME, AND HAS BEEN ALTERED</color>"); }
            }
            else { ETGModConsole.Log("<color=#ff0000ff>ERROR: R&G DEPARTMENT DUNGEON PREFAB WAS NULL</color>"); }
            rngDungeon = null;
            #endregion
            #region Forge
            Dungeon forgeDungeon = DungeonDatabase.GetOrLoadByName("base_forge");
            foreach (WeightedRoom wRoom in forgeDungeon.PatternSettings.flows[0].fallbackRoomTable.includedRooms.elements)
            {
                if (wRoom.room != null && !string.IsNullOrEmpty(wRoom.room.name))
                {
                    if (wRoom.room.name.ToLower().StartsWith("forge_normal_cubulead_03"))
                    {
                        VerticalCrusher = wRoom.room.placedObjects[0].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        FireBarTrap = wRoom.room.placedObjects[7].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                    }
                    if (wRoom.room.name.ToLower().StartsWith("forge_connector_flamepipes_01"))
                    {
                        FlamePipeNorth = wRoom.room.placedObjects[1].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        FlamePipeWest = wRoom.room.placedObjects[3].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                        FlamePipeEast = wRoom.room.placedObjects[2].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                    }

                }
            }
            forgeDungeon = null;
            #endregion
            #region BulletHell
            Dungeon hellDungeon = DungeonDatabase.GetOrLoadByName("base_bullethell");
            foreach (WeightedRoom wRoom in hellDungeon.PatternSettings.flows[0].fallbackRoomTable.includedRooms.elements)
            {
                if (wRoom.room != null && !string.IsNullOrEmpty(wRoom.room.name))
                {
                    if (wRoom.room.name.ToLower().StartsWith("hell_connector_pathburst_01"))
                    {
                        FlameburstTrap = wRoom.room.placedObjects[3].nonenemyBehaviour.gameObject.InstantiateAndFakeprefab();
                    }
                }
            }
            hellDungeon = null;
            #endregion
        }

        #region Objects
        //Loot
        public static GameObject NonRatStealableArmor;
        public static GameObject NonRatStealableAmmo;
        public static GameObject NonRatStealableSpreadAmmo;
        public static GameObject MapPlaceable;
        public static GameObject GlassGuonPlaceable;
        public static GameObject FiftyCasingPlaceable;
        public static GameObject SingleCasingPlaceable;
        public static GameObject FiveCasingPlaceable;
        //Traps
        public static GameObject HorizontalCrusher;
        public static GameObject VerticalCrusher;
        public static GameObject FireBarTrap;
        public static GameObject FlamePipeNorth;
        public static GameObject FlamePipeEast;
        public static GameObject FlamePipeWest;
        public static GameObject FlameburstTrap;
        //Decoration
        public static GameObject MopAndBucket;
        public static GameObject SecretRoomSkeleton;
        public static GameObject Pew;
        public static GameObject OldKingThrone;
        public static GameObject BulletKingThrone;
        public static GameObject AlienTank;
        public static GameObject DecorativeElectricFloor;
        public static GameObject AgunimBossMatt;
        public static GameObject TallComputerBreakable;
        public static GameObject VentilationTube;
        public static GameObject HologramWallVertical;
        public static GameObject HologramWallHorizontal;
        public static GameObject MetalCrate;
        public static GameObject WideComputerBreakable;
        public static GameObject TechnoFloorCellSpider;
        public static GameObject TechnoFloorCellLeever;
        public static GameObject TechnoFloorCellCaterpillar;
        public static GameObject TechnoFloorCellEmpty;
        public static GameObject LargeDesk;
        public static GameObject GlassWallHorizontal;
        public static GameObject GlassWallVertical;
        public static GameObject ToiletWest;
        public static GameObject ToiletEast;
        public static GameObject ToiletNorth;
        public static GameObject BathroomStallDividerWest;
        public static GameObject BathroomStallDividerEast;
        public static GameObject BathroomStallDividerNorth;
        public static GameObject SteelTableVertical;
        public static GameObject SteelTableHorizontal;
        public static GameObject KitchenCounter;
        public static GameObject KitchenChairRight;
        public static GameObject KitchenChairLeft;
        public static GameObject KitchenChairFront;
        public static GameObject ACVent;
        public static GameObject ACUnit;
        public static GameObject CardboardBox3;
        #endregion
    }
}
