using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    public class IngressBullets : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Ingress Bullets";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/workinprogress_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<IngressBullets>();

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
            item.quality = PickupObject.ItemQuality.EXCLUDED; //D

            //Synergy with the Balloon Gun --> Double Radius.
            //Synergy with Armour of Thorns --> Deal damage to all enemies pushed. dam = dodgerolldam * 3.
        }
        public bool firstBulletReady = true;
        public void onFired(Projectile bullet, float eventchancescaler)
        {
            if (firstBulletReady == true)
            {
                if (bullet.Owner == Owner.gameActor)
                {
                    bullet.AdjustPlayerProjectileTint(Color.yellow, 1, 0f);
                    bullet.baseData.damage += 10000f;
                    firstBulletReady = false;
                }
            }
            else return;
        }
        private void onFiredBeam(BeamController sourceBeam)
        {

        }
        private void OnNewFloor()
        {
            firstBulletReady = true;
        }
        public override void Pickup(PlayerController player)
        {
            player.PostProcessProjectile += this.onFired;
            player.PostProcessBeam += this.onFiredBeam;
            GameManager.Instance.OnNewLevelFullyLoaded += this.OnNewFloor;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject result = base.Drop(player);
            player.PostProcessProjectile -= this.onFired;
            player.PostProcessBeam -= this.onFiredBeam;
            GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
            return result;
        }
    }
}