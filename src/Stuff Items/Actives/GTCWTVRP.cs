using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    class GTCWTVRP : PlayerItem
    {
        //Call this method from the Start() method of your ETGModule extension class
        public static void Init()
        {
            //The name of the item
            string itemName = "GTCWTVRP";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it.
            string resourceName = "NevernamedsItems/Resources/gtcwtvrp_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<GTCWTVRP>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "'Tis but a copy";
            string longDesc = "'Gun That Can Wound The Very Recent Past'" + "\n\nReloads the current floor. A cheap plastic knockoff of The Gun That Can Kill The Past.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //"kts" here is the item pool. In the console you'd type kts:sweating_bullets
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.None, 1000);

            //Adds a passive modifier, like curse, coolness, damage, etc. to the item. Works for passives and actives.

            //Set some other fields
            item.consumable = true;
            item.quality = ItemQuality.A;

            item.AddToSubShop(ItemBuilder.ShopType.Cursula);
            item.AddToSubShop(ItemBuilder.ShopType.Trorc);

        }
        public override void DoEffect(PlayerController user)
        {
            if (GameManager.Instance.Dungeon.IsGlitchDungeon)
            {
                GameManager.Instance.InjectedFlowPath = "Core Game Flows/Secret_DoubleBeholster_Flow";
            }
            if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.CASTLEGEON) GameManager.Instance.LoadCustomLevel("tt_castle");
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.JUNGLEGEON) GameManager.Instance.LoadCustomLevel("tt_jungle");
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.BELLYGEON) GameManager.Instance.LoadCustomLevel("tt_belly");
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.SEWERGEON) GameManager.Instance.LoadCustomLevel("tt_sewer");
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.GUNGEON) GameManager.Instance.LoadCustomLevel("tt5");
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.CATHEDRALGEON) GameManager.Instance.LoadCustomLevel("tt_cathedral");
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.MINEGEON) GameManager.Instance.LoadCustomLevel("tt_mines");
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.RATGEON) GameManager.Instance.LoadCustomLevel("ss_resourcefulrat");
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.CATACOMBGEON) GameManager.Instance.LoadCustomLevel("tt_catacombs");
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.OFFICEGEON) GameManager.Instance.LoadCustomLevel("tt_nakatomi");
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.FORGEGEON) GameManager.Instance.LoadCustomLevel("tt_forge");
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.WESTGEON) GameManager.Instance.LoadCustomLevel("tt_canyon");//Apache's glitch floor
            else if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.HELLGEON) GameManager.Instance.LoadCustomLevel("tt_bullethell");
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
