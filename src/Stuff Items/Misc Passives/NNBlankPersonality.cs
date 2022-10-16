using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    public class NNBlankPersonality : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Blank Stare";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/blankpersonality_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<NNBlankPersonality>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Unreadable";
            string longDesc = "Mysterious, boring, and a bit creepy, your newfound personality will sometimes make shopkeepers give you blanks just to make you go away and stop staring.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Coolness, 1, StatModifier.ModifyMethod.ADDITIVE);

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.C;
            item.AddToSubShop(ItemBuilder.ShopType.OldRed);
        }

        private void OnItemPurchased(PlayerController player, ShopItemController obj)
        {
            if (Owner.HasPickupID(Gungeon.Game.Items["nn:spare_blank"].PickupObjectId))
            {
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(224).gameObject, player);
            }
           else if (UnityEngine.Random.value < .50f)
            {
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(224).gameObject, player);
            }

            if (Owner.HasPickupID(Gungeon.Game.Items["nn:spare_key"].PickupObjectId) && UnityEngine.Random.value <= 0.1f)
            {
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(67).gameObject, player);
            }
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnItemPurchased += this.OnItemPurchased;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject result = base.Drop(player);
            player.OnItemPurchased -= this.OnItemPurchased;
            return result;
        }
        public override void OnDestroy()
        {
            if (Owner) Owner.OnItemPurchased -= this.OnItemPurchased;
            base.OnDestroy();
        }
    }
}
