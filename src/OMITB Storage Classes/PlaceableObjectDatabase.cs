using Dungeonator;
//using GungeonAPI;
using ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{    
    public class EasyPlaceableObjects
    {
        public static GameObject CoffinVert = LoadHelper.LoadAssetFromAnywhere<GameObject>("Coffin_Vertical");
        public static GameObject CoffinHoriz = LoadHelper.LoadAssetFromAnywhere<GameObject>("Coffin_Horizontal");
        public static GameObject Brazier = LoadHelper.LoadAssetFromAnywhere<DungeonPlaceable>("brazier").variantTiers[0].GetOrLoadPlaceableObject;
        public static GameObject CursedPot = LoadHelper.LoadAssetFromAnywhere<DungeonPlaceable>("Curse Pot").variantTiers[0].GetOrLoadPlaceableObject;
        public static GameObject GenericBarrel = LoadHelper.LoadAssetFromAnywhere<DungeonPlaceable>("Barrel_collection").variantTiers[0].nonDatabasePlaceable;
        public static DungeonPlaceable GenericBarrelDungeonPlaceable = LoadHelper.LoadAssetFromAnywhere<DungeonPlaceable>("Barrel_collection");
        public static GameObject PoisonBarrel = LoadHelper.LoadAssetFromAnywhere<GameObject>("Yellow Drum");
        public static GameObject MetalExplosiveBarrel = LoadHelper.LoadAssetFromAnywhere<GameObject>("Red Drum");
        public static GameObject ExplosiveBarrel = LoadHelper.LoadAssetFromAnywhere<GameObject>("Red Barrel");
        public static GameObject WaterBarrel = LoadHelper.LoadAssetFromAnywhere<GameObject>("Blue Drum");
        public static GameObject OilBarrel = LoadHelper.LoadAssetFromAnywhere<GameObject>("Purple Drum");
        public static GameObject IceBomb = LoadHelper.LoadAssetFromAnywhere<GameObject>("Ice Cube Bomb");
        public static GameObject TableHorizontal = LoadHelper.LoadAssetFromAnywhere<GameObject>("Table_Horizontal");
        public static GameObject TableVertical = LoadHelper.LoadAssetFromAnywhere<GameObject>("Table_Vertical");
        public static GameObject TableHorizontalStone = LoadHelper.LoadAssetFromAnywhere<GameObject>("Table_Horizontal_Stone");
        public static GameObject TableVerticalStone = LoadHelper.LoadAssetFromAnywhere<GameObject>("Table_Vertical_Stone");
        public static GameObject SpikeTrap = LoadHelper.LoadAssetFromAnywhere<GameObject>("trap_spike_gungeon_2x2");
        public static GameObject FlameTrap = LoadHelper.LoadAssetFromAnywhere<GameObject>("trap_flame_poofy_gungeon_1x1");
        public static GameObject HangingPot = LoadHelper.LoadAssetFromAnywhere<GameObject>("Hanging_Pot");
        public static GameObject DeadBlow = LoadHelper.LoadAssetFromAnywhere<GameObject>("Forge_Hammer");
        public static GameObject ChestTruth = LoadHelper.LoadAssetFromAnywhere<GameObject>("TruthChest");
        public static GameObject ChestRat = LoadHelper.LoadAssetFromAnywhere<GameObject>("Chest_Rat");
        public static GameObject Mirror = LoadHelper.LoadAssetFromAnywhere<GameObject>("Shrine_Mirror");
        public static GameObject FoldingTable = PickupObjectDatabase.GetById(644).GetComponent<FoldingTableItem>().TableToSpawn.gameObject;
        public static GameObject BabyDragunNPC = LoadHelper.LoadAssetFromAnywhere<GameObject>("BabyDragunJail");
    } 
}
