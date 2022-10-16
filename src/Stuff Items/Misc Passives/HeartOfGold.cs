using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    class HeartOfGold : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Heart of Gold";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/NeoItemSprites/heartofgold_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<HeartOfGold>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Red Gold";
            string longDesc = "Squirts out some cash upon it's bearer taking damage."+"\n\nA small statue of a much larger and far more hostile golden heart discovered deep underground...";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.C;

            HeartOfGoldID = item.PickupObjectId;
        }

        public static int HeartOfGoldID;
        private void giveCash(PlayerController user)
        {
            int cashMoney = 10;
            if (Owner.PlayerHasActiveSynergy("Do-Gooder")) cashMoney *= 2;
            LootEngine.SpawnCurrency(user.sprite.WorldCenter, cashMoney);
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnReceivedDamage += this.giveCash;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnReceivedDamage -= this.giveCash;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner) Owner.OnReceivedDamage -= this.giveCash;
            base.OnDestroy();
        }
    }
}
