using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using ItemAPI;
using UnityEngine;

namespace CwaffingTheGungy
{
    class BorrowedTime : PlayerItem
    {
        public static string activeName       = "Borrowed Time";
        public static string spritePath       = "CwaffingTheGungy/Resources/NeoItemSprites/88888888_icon";
        public static string shortDescription = "Clock's Ticking";
        public static string longDescription  = "(insta clear any room, but enemies will all respawn in boss room with increased jam chance)";

        //Call this method from the Start() method of your ETGModule extension class
        public static void Init()
        {
            PlayerItem item = Lazy.SetupActive<BorrowedTime>(activeName, spritePath, shortDescription, longDescription, "cg");
            item.quality      = PickupObject.ItemQuality.C;

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Timed, 5);
            item.consumable = false;
            item.quality = ItemQuality.D;
        }
        public override void DoEffect(PlayerController user)
        {
            return;
            // if (user.HasPickupID(Gungeon.Game.Items["space_friend"].PickupObjectId))
            // {
            //     goUp = true;
            // }
            // else
            // {
            //     if (UnityEngine.Random.value > .05) goUp = false;
            //     else goUp = true;
            // }
            // if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.CASTLEGEON)
            // {
            //     GameManager.Instance.LoadCustomLevel("tt5");
            // }
            // else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.SEWERGEON)
            // {
            //     if (goUp == false)
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt5");
            //     }
            //     else
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt_castle");
            //     }
            // }
            // else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.JUNGLEGEON)
            // {
            //     if (goUp == false)
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt5");
            //     }
            //     else
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt_castle");
            //     }
            // }
            // else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.BELLYGEON)
            // {
            //     if (goUp == false)
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt_mines");
            //     }
            //     else
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt5");
            //     }
            // }
            // else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.GUNGEON)
            // {
            //     if (goUp == false)
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt_mines");
            //     }
            //     else
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt_castle");
            //     }
            // }
            // else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.CATHEDRALGEON)
            // {
            //     if (goUp == false)
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt_mines");
            //     }
            //     else
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt5");
            //     }
            // }
            // else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.MINEGEON)
            // {
            //     if (goUp == false)
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt_catacombs");
            //     }
            //     else
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt5");
            //     }
            // }
            // else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.RATGEON)
            // {
            //     if (goUp == false)
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt_catacombs");
            //     }
            //     else
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt_mines");
            //     }
            // }
            // else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.CATACOMBGEON)
            // {
            //     if (goUp == false)
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt_forge");
            //     }
            //     else
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt_mines");
            //     }
            // }
            // else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.OFFICEGEON)
            // {
            //     if (goUp == false)
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt_forge");
            //     }
            //     else
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt_catacombs");
            //     }
            // }
            // else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.FORGEGEON)
            // {
            //     if (goUp == false)
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt_bullethell");
            //     }
            //     else
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt_catacombs");
            //     }
            // }
            // else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.WESTGEON)
            // {
            //     if (goUp == false)
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt_bullethell");
            //     }
            //     else
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt_catacombs");
            //     }
            // }//Apache's glitch floor
            // else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.HELLGEON)
            // {
                
            //     if (goUp == false)
            //     {
            //         Exploder.DoDefaultExplosion(LastOwner.specRigidbody.UnitCenter, new Vector2());
            //     }
            //     else
            //     {
            //         GameManager.Instance.LoadCustomLevel("tt_forge");
            //     }
            // }
            // else
            // {
            //     IntVector2 bestRewardLocation = user.CurrentRoom.GetBestRewardLocation(IntVector2.One * 3, RoomHandler.RewardLocationStyle.PlayerCenter, true);
            //     Chest rainbow_Chest = GameManager.Instance.RewardManager.Rainbow_Chest;
            //     rainbow_Chest.IsLocked = false;
            //     Chest.Spawn(rainbow_Chest, bestRewardLocation);
            // }
        }
    }
}
