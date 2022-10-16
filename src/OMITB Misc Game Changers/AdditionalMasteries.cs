using Dungeonator;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Alexandria.DungeonAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{ 
    public class AdditionalMasteries 
    {
        public static void Init()
        {
            MasteryOverrideHandler.RegisterFloorForMasterySpawn(MasteryOverrideHandler.ViableRegisterFloors.OUBLIETTE);
            MasteryOverrideHandler.RegisterFloorForMasterySpawn(MasteryOverrideHandler.ViableRegisterFloors.ABBEY);
            CustomActions.OnRewardPedestalDetermineContents += OnMasteryDetermineContents;
        }
        public static void OnMasteryDetermineContents(RewardPedestal pedestal, PlayerController determiner, CustomActions.ValidPedestalContents valids)
        {
            if (pedestal.ContainsMasteryTokenForCurrentLevel())
            {
                if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.SEWERGEON)
                {
                    valids.overrideItemPool.Add(new Tuple<int, float>(GooeyHeart.GooeyHeartID, 1));
                    valids.overrideItemPool.Add(new Tuple<int, float>(BloodglassGuonStone.BloodGlassGuonStoneID, 1));
                    valids.overrideItemPool.Add(new Tuple<int, float>(BlobulonRage.BlobulonRageID, 1));
                }
                else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.CATHEDRALGEON)
                {
                    valids.overrideItemPool.Add(new Tuple<int, float>(ExaltedHeart.ExaltedHeartID, 1));
                }
            }
        }       
    }
}

