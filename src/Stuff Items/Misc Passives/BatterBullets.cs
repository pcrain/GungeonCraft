using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Alexandria.ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;
using SaveAPI;

namespace NevernamedsItems
{
    public class BatterBullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Batter Bullets";
            string resourceName = "NevernamedsItems/Resources/NeoItemSprites/batterbullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<BatterBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Up";
            string longDesc = "Smacks enemies on fatal damage."+"\n\nThere are two types of people in this world; people who enjoy beating others with baseball bats, and the rest of you weirdos.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;
            item.SetTag("bullet_modifier");
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_BATTERBULLETS, true);
            item.AddItemToDougMetaShop(45);
        }

        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            if (sourceProjectile.GetComponent<KilledEnemiesBecomeProjectileModifier>())
            {
                //maybe add a synergy here idfk
            }
            else
            {
                sourceProjectile.baseData.force *= 3f;
                sourceProjectile.baseData.speed *= 1.25f;
                sourceProjectile.UpdateSpeed();
                sourceProjectile.OnHitEnemy += OnHitEnemy;
                KilledEnemiesBecomeProjectileModifier caseyness = sourceProjectile.gameObject.AddComponent<KilledEnemiesBecomeProjectileModifier>();
                caseyness.CompletelyBecomeProjectile = (PickupObjectDatabase.GetById(541) as Gun).DefaultModule.chargeProjectiles[0].Projectile.GetComponent<KilledEnemiesBecomeProjectileModifier>().CompletelyBecomeProjectile;
                caseyness.BaseProjectile = (PickupObjectDatabase.GetById(541) as Gun).DefaultModule.chargeProjectiles[0].Projectile.GetComponent<KilledEnemiesBecomeProjectileModifier>().BaseProjectile;
            }
        }    
        private void OnHitEnemy(Projectile self, SpeculativeRigidbody bodi, bool fatal)
        {
            if (self && bodi && fatal)
            {
                GameObject hitVFX = (PickupObjectDatabase.GetById(541) as Gun).DefaultModule.chargeProjectiles[0].Projectile.hitEffects.enemy.effects[0].effects[0].effect;
                UnityEngine.Object.Instantiate<GameObject>(hitVFX, self.specRigidbody.UnitCenter, Quaternion.identity);
            }
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProjectile;
        }
        public override void DisableEffect(PlayerController player)
        {
            player.PostProcessProjectile -= this.PostProcessProjectile;
            base.DisableEffect(player);
        }
    }
}