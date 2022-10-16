using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using UnityEngine;
using ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    class AmberDie : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Amber Die";
            string resourceName = "NevernamedsItems/Resources/NeoItemSprites/amberdie_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<AmberDie>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Tastes Lucky";
            string longDesc = "Chance for enemy projectiles to be friendly instead!\n\nThe remains of a leprechaun are immaculately preserved in it's center.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;
        }
        private void NewBulletAppeared(Projectile proj)
        {
            if (proj.Owner == null || !(proj.Owner is PlayerController))
            {
                if (UnityEngine.Random.value <= 0.1f)
                {
                    ConvertBullet(proj);
                }
            }
        }
        private void ConvertBullet(Projectile proj)
        {
            Vector2 dir = proj.Direction;
            //proj.RemoveBulletScriptControl();
            if (proj.Owner && proj.Owner.specRigidbody) proj.specRigidbody.DeregisterSpecificCollisionException(proj.Owner.specRigidbody);

            proj.Owner = Owner;
            proj.SetNewShooter(Owner.specRigidbody);
            proj.allowSelfShooting = false;
            proj.collidesWithPlayer = false;
            proj.collidesWithEnemies = true;
            proj.baseData.damage = 15;
            if (proj.IsBlackBullet) proj.baseData.damage *= 2;
            PlayerController player = (Owner as PlayerController);
            if (player != null)
            {
                proj.baseData.damage *= player.stats.GetStatValue(PlayerStats.StatType.Damage);
                proj.baseData.speed *= player.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                proj.UpdateSpeed();
                proj.baseData.force *= player.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                proj.baseData.range *= player.stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
                proj.BossDamageMultiplier *= player.stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
                proj.RuntimeUpdateScale(player.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale));
                if (player.stats.GetStatValue(PlayerStats.StatType.AdditionalShotBounces) > 0)
                {
                    bool hadComp = proj.gameObject.GetComponent<BounceProjModifier>();
                    BounceProjModifier bounce = proj.gameObject.GetOrAddComponent<BounceProjModifier>();

                    if (hadComp) bounce.numberOfBounces += (int)player.stats.GetStatValue(PlayerStats.StatType.AdditionalShotBounces);
                    else bounce.numberOfBounces = (int)player.stats.GetStatValue(PlayerStats.StatType.AdditionalShotBounces);
                }
                player.DoPostProcessProjectile(proj);
            }
            if (proj.GetComponent<BeamController>() != null)
            {
                proj.GetComponent<BeamController>().HitsPlayers = false;
                proj.GetComponent<BeamController>().HitsEnemies = true;
            }
            else if (proj.GetComponent<BasicBeamController>() != null)
            {
                proj.GetComponent<BasicBeamController>().HitsPlayers = false;
                proj.GetComponent<BasicBeamController>().HitsEnemies = true;
            }
            proj.AdjustPlayerProjectileTint(ExtendedColours.honeyYellow, 1);
            proj.UpdateCollisionMask();
            proj.RemoveFromPool();
            proj.Reflected();
            proj.SendInDirection(dir, false);
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            StaticReferenceManager.ProjectileAdded += NewBulletAppeared;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            StaticReferenceManager.ProjectileAdded -= NewBulletAppeared;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                StaticReferenceManager.ProjectileAdded -= NewBulletAppeared;
            }
            base.OnDestroy();
        }
    }
}