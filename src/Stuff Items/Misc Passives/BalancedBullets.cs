using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    class BalancedBullets : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Balanced Bullets";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/purpleprose_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<BalancedBullets>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "As All Things Should Be";
            string longDesc = "Charms all enemies in the room upon taking damage." + "\n\nA beautiful rose grown in some long lost floral garden deep within the gungeon. As you peel away it's petals, you find more inside, with seemingly no end. \n\nI guess you'll never find out if 'she loves you'.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.EXCLUDED;
        }

        private void charmAll(float healthRemaining, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection)
        {
            if (damageTypes != CoreDamageTypes.Void)
            {

            }
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.healthHaver.OnDamaged += this.charmAll;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.healthHaver.OnDamaged -= this.charmAll;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            Owner.healthHaver.OnDamaged -= this.charmAll;
            base.OnDestroy();
        }
    }
}