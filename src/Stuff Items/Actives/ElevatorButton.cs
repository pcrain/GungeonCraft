using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    class ElevatorButton : PlayerItem
    {
        //Call this method from the Start() method of your ETGModule extension class
        public static void Init()
        {
            //The name of the item
            string itemName = "Elevator Button";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it.
            string resourceName = "NevernamedsItems/Resources/elevatorbutton_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<ElevatorButton>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Going... Down";
            string longDesc = "Transports the user to the next floor. One use." + "\n\nMay malfunction.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //"kts" here is the item pool. In the console you'd type kts:sweating_bullets
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.None, 1000);

            //Adds a passive modifier, like curse, coolness, damage, etc. to the item. Works for passives and actives.

            //Set some other fields
            item.consumable = true;
            item.quality = ItemQuality.D;

            item.AddToSubShop(ItemBuilder.ShopType.Trorc);

        }
        public bool goUp;
        public override void DoEffect(PlayerController user)
        {
            if (user.HasPickupID(Gungeon.Game.Items["space_friend"].PickupObjectId))
            {
                goUp = true;
            }
            else
            {
                if (UnityEngine.Random.value > .05) goUp = false;
                else goUp = true;
            }
            if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.CASTLEGEON)
            {
                GameManager.Instance.LoadCustomLevel("tt5");
            }
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.SEWERGEON)
            {
                if (goUp == false)
                {
                    GameManager.Instance.LoadCustomLevel("tt5");
                }
                else
                {
                    GameManager.Instance.LoadCustomLevel("tt_castle");
                }
            }
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.JUNGLEGEON)
            {
                if (goUp == false)
                {
                    GameManager.Instance.LoadCustomLevel("tt5");
                }
                else
                {
                    GameManager.Instance.LoadCustomLevel("tt_castle");
                }
            }
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.BELLYGEON)
            {
                if (goUp == false)
                {
                    GameManager.Instance.LoadCustomLevel("tt_mines");
                }
                else
                {
                    GameManager.Instance.LoadCustomLevel("tt5");
                }
            }
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.GUNGEON)
            {
                if (goUp == false)
                {
                    GameManager.Instance.LoadCustomLevel("tt_mines");
                }
                else
                {
                    GameManager.Instance.LoadCustomLevel("tt_castle");
                }
            }
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.CATHEDRALGEON)
            {
                if (goUp == false)
                {
                    GameManager.Instance.LoadCustomLevel("tt_mines");
                }
                else
                {
                    GameManager.Instance.LoadCustomLevel("tt5");
                }
            }
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.MINEGEON)
            {
                if (goUp == false)
                {
                    GameManager.Instance.LoadCustomLevel("tt_catacombs");
                }
                else
                {
                    GameManager.Instance.LoadCustomLevel("tt5");
                }
            }
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.RATGEON)
            {
                if (goUp == false)
                {
                    GameManager.Instance.LoadCustomLevel("tt_catacombs");
                }
                else
                {
                    GameManager.Instance.LoadCustomLevel("tt_mines");
                }
            }
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.CATACOMBGEON)
            {
                if (goUp == false)
                {
                    GameManager.Instance.LoadCustomLevel("tt_forge");
                }
                else
                {
                    GameManager.Instance.LoadCustomLevel("tt_mines");
                }
            }
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.OFFICEGEON)
            {
                if (goUp == false)
                {
                    GameManager.Instance.LoadCustomLevel("tt_forge");
                }
                else
                {
                    GameManager.Instance.LoadCustomLevel("tt_catacombs");
                }
            }
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.FORGEGEON)
            {
                if (goUp == false)
                {
                    GameManager.Instance.LoadCustomLevel("tt_bullethell");
                }
                else
                {
                    GameManager.Instance.LoadCustomLevel("tt_catacombs");
                }
            }
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.WESTGEON)
            {
                if (goUp == false)
                {
                    GameManager.Instance.LoadCustomLevel("tt_bullethell");
                }
                else
                {
                    GameManager.Instance.LoadCustomLevel("tt_catacombs");
                }
            }//Apache's glitch floor
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.HELLGEON)
            {
                
                if (goUp == false)
                {
                    Exploder.DoDefaultExplosion(LastOwner.specRigidbody.UnitCenter, new Vector2());
                }
                else
                {
                    GameManager.Instance.LoadCustomLevel("tt_forge");
                }
            }
            else
            {
                IntVector2 bestRewardLocation = user.CurrentRoom.GetBestRewardLocation(IntVector2.One * 3, RoomHandler.RewardLocationStyle.PlayerCenter, true);
                Chest rainbow_Chest = GameManager.Instance.RewardManager.Rainbow_Chest;
                rainbow_Chest.IsLocked = false;
                Chest.Spawn(rainbow_Chest, bestRewardLocation);
            }

        }
    }
}