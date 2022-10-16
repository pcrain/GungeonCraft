using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class WitheringChamber : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Withering Chamber";
            string resourceName = "NevernamedsItems/Resources/AnimatedItemSprites/witheringchamber_icon_001";
            GameObject obj = new GameObject(itemName);

            var item = obj.AddComponent<WitheringChamber>();

            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            /* ItemBuilder.AddAnimatedSpriteToObject(
                 itemName,
                 new List<string>
                 {
                 "NevernamedsItems/Resources/AnimatedItemSprites/witheringchamber_icon_001",
                 "NevernamedsItems/Resources/AnimatedItemSprites/witheringchamber_icon_002",
                 "NevernamedsItems/Resources/AnimatedItemSprites/witheringchamber_icon_003",
                 "NevernamedsItems/Resources/AnimatedItemSprites/witheringchamber_icon_004",
                 },
                 10,
                 obj
                 );*/

            string shortDesc = "Decay";
            string longDesc = "Guns with higher max ammo deal more damage, but suffering damage withers away your ammo capacity." + "\n\nThe spiteful creation of a cruel Chamberlord.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.quality = PickupObject.ItemQuality.B;
            ID = item.PickupObjectId;
        }
        public static int ID;
        public override void Pickup(PlayerController player)
        {
            player.OnReceivedDamage += this.OnHit;
            player.PostProcessBeam += this.PostProcessBeam;
            player.PostProcessProjectile += this.PostProcessProjectile;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.OnReceivedDamage -= this.OnHit;
            player.PostProcessBeam -= this.PostProcessBeam;

            player.PostProcessProjectile -= this.PostProcessProjectile;

            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.OnReceivedDamage -= this.OnHit;
                Owner.PostProcessBeam -= this.PostProcessBeam;

                Owner.PostProcessProjectile -= this.PostProcessProjectile;
            }
            base.OnDestroy();
        }
        private void OnHit(PlayerController player)
        {
            if (player)
            {
                if (player.CurrentGun)
                {
                    float chance = 1;
                    if (player.PlayerHasActiveSynergy("Chamrock")) chance = 0.5f;

                    if (!player.CurrentGun.InfiniteAmmo && player.CurrentGun.GetBaseMaxAmmo() > 0)
                    {
                        if (UnityEngine.Random.value <= chance)
                        {
                            int currentAmmo = player.CurrentGun.CurrentAmmo;
                            player.CurrentGun.SetBaseMaxAmmo(currentAmmo);
                        }
                    }

                }
            }
        }
        private void PostProcessBeam(BeamController beam)
        {
            if (beam && beam.GetComponent<Projectile>() != null)
            {
                PostProcessProjectile(beam.GetComponent<Projectile>(), 1);
            }
        }
        private void PostProcessProjectile(Projectile sourceBullet, float thing)
        {
            if (sourceBullet && sourceBullet.ProjectilePlayerOwner())
            {
                if (sourceBullet.ProjectilePlayerOwner().CurrentGun != null)
                {
                    if (sourceBullet.ProjectilePlayerOwner().CurrentGun.InfiniteAmmo)
                    {
                        sourceBullet.baseData.damage *= 1.25f;
                    }
                    else
                    {
                        int currentgunmax = sourceBullet.ProjectilePlayerOwner().CurrentGun.AdjustedMaxAmmo;
                        if (currentgunmax > 0)
                        {
                            float multiplier = (currentgunmax / 200) + 1;
                            sourceBullet.baseData.damage *= multiplier;
                        }
                    }
                }
            }
        }
    }
}

