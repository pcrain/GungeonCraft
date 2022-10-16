using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Alexandria.ItemAPI;
using Dungeonator;
using SaveAPI;

namespace NevernamedsItems
{
    public class HallowedBullets : IntersectEnemyBulletsItem
    {
        public static void Init()
        {
            string itemName = "Hallowed Bullets";
            string resourceName = "NevernamedsItems/Resources/hallowedrounds_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<HallowedBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Bullet Holeys";
            string longDesc = "These bullets are so blessed that they will preach to other projectiles mid-air. Usually this does nothing, but it does give Jammed projectiles a chance at redemption.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.D;
            item.SetTag("bullet_modifier");
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.ALLJAMMED_BEATEN_KEEP, true);
        }
        public void onFiredGun(Projectile bullet, float eventchancescaler)
        {           
            bullet.BlackPhantomDamageMultiplier *= 1.1f;              
        }        
        public override void Pickup(PlayerController player)
        {
            player.PostProcessProjectile += this.onFiredGun;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject result = base.Drop(player);
            player.PostProcessProjectile -= this.onFiredGun;
            return result;
        }
        public override void OnDestroy()
        {
            Owner.PostProcessProjectile -= this.onFiredGun;
            base.OnDestroy();
        }
        public override void DoIntersectionEffect(Projectile playerBullet, Projectile enemyBullet)
        {
            if (enemyBullet.IsBlackBullet)
            {
                enemyBullet.ReturnFromBlackBullet();
            }
            base.DoIntersectionEffect(playerBullet, enemyBullet);
        }

    }
}