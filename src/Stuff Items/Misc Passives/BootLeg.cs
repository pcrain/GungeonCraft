using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    public class BootLeg : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Legboot";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/legboot_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<BootLeg>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Knock It Off";
            string longDesc = "From a peculiar and less graphically appealing alternate dimension where dodge rolling through bullets or into enemies restores ammo." + "\n\nThis boot is as long as your entire leg!";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.A;

            //Synergy with the Balloon Gun --> Double Radius.
            //Synergy with Armour of Thorns --> Deal damage to all enemies pushed. dam = dodgerolldam * 3.
        }


        private void onDodgeRolledOverBullet(Projectile bullet)
        {
            if (Owner.CurrentGun != null) Owner.CurrentGun.GainAmmo(1);
        }
        private void onDodgeRolledIntoEnemy(PlayerController player, AIActor enemy)
        {
            if (Owner.CurrentGun != null) Owner.CurrentGun.GainAmmo(5);
        }

        public override void Pickup(PlayerController player)
        {
            player.OnDodgedProjectile += this.onDodgeRolledOverBullet;
            player.OnRolledIntoEnemy += this.onDodgeRolledIntoEnemy;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject result = base.Drop(player);
            player.OnDodgedProjectile -= this.onDodgeRolledOverBullet;
            player.OnRolledIntoEnemy -= this.onDodgeRolledIntoEnemy;
            return result;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.OnDodgedProjectile -= this.onDodgeRolledOverBullet;
                Owner.OnRolledIntoEnemy -= this.onDodgeRolledIntoEnemy;
            }
            base.OnDestroy();
        }
    }
}