using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    public class AntimatterBullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Antimatter Bullets";
            string resourceName = "NevernamedsItems/Resources/antimatterbullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<AntimatterBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Prime Antimaterial";
            string longDesc = "Your bullets have a chance to create an explosion upon intersecting with enemy bullets." + "\n\nWhy does't this trigger when you actually hit an enemy?... don't ask questions.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.A;
            item.SetTag("bullet_modifier");
            AntimatterBulletsID = item.PickupObjectId;

        }
        public static int AntimatterBulletsID;

        private void PostProcessProjectile(Projectile proj, float flot)
        {
            AntimatterBulletsModifier mod = proj.gameObject.GetOrAddComponent<AntimatterBulletsModifier>();
            mod.explosionData = smallPlayerSafeExplosion;
        }
        public override void Pickup(PlayerController player)
        {
            player.PostProcessProjectile += PostProcessProjectile;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.PostProcessProjectile -= PostProcessProjectile;
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessProjectile -= PostProcessProjectile;
            }
            base.OnDestroy();
        }

        public static ExplosionData smallPlayerSafeExplosion = new ExplosionData()
        {
            effect = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultSmallExplosionData.effect,
            ss = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultSmallExplosionData.ss,
            ignoreList = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultSmallExplosionData.ignoreList,
            damageRadius = 2.5f,
            damageToPlayer = 0f,
            doDamage = true,
            damage = 25,
            doDestroyProjectiles = false,
            doForce = true,
            debrisForce = 30f,
            preventPlayerForce = true,
            explosionDelay = 0.1f,
            usesComprehensiveDelay = false,
            doScreenShake = true,
            playDefaultSFX = true,
        };
    }
}
