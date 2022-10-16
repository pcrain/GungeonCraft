using Alexandria.ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    class EmptyRounds : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Empty Rounds";
            string resourceName = "NevernamedsItems/Resources/emptyrounds_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<EmptyRounds>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Less is More";
            string longDesc = "Increases damage by how empty your guns are of ammo." + "\n\nBrought to the Gungeon by a dopey gnome who felt it suited his spray-and-pray combat style." + "\nHe lost it within an hour." + "\nTypical.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.CanBeDropped = true;
            item.quality = PickupObject.ItemQuality.A;
            item.SetTag("bullet_modifier");
        }
        private void PostProcessBeam(BeamController beam)
        {
            if (beam)
            {
                Projectile projectile = beam.projectile;
                if (projectile)
                {
                    this.PostProcess(projectile, 1f);
                }
            }
        }
        private void PostProcess(Projectile bullet, float var)
        {
            if (Owner)
            {
                int totalMaxAmmoOfAllGuns = 0;
                int totalRemainingAmmoOfAllGuns = 0;
                foreach (Gun gun in Owner.inventory.AllGuns)
                {
                    if (!gun.InfiniteAmmo)
                    {
                        totalMaxAmmoOfAllGuns += gun.AdjustedMaxAmmo;
                        totalRemainingAmmoOfAllGuns += gun.CurrentAmmo;
                    }
                }
                float amountOfMissingBullets = totalMaxAmmoOfAllGuns - totalRemainingAmmoOfAllGuns;
                float emptinessPercent = (amountOfMissingBullets / totalMaxAmmoOfAllGuns);
                bullet.baseData.damage *= (emptinessPercent) + 1;


            }
        }
        public override void Pickup(PlayerController player)
        {
            player.PostProcessProjectile += this.PostProcess;
            player.PostProcessBeam += this.PostProcessBeam;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.PostProcessProjectile -= this.PostProcess;
            player.PostProcessBeam -= this.PostProcessBeam;
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessProjectile -= this.PostProcess;
                Owner.PostProcessBeam -= this.PostProcessBeam;
            }
            base.OnDestroy();
        }
    }
}
