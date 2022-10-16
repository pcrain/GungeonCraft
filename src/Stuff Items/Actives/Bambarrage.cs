using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Dungeonator;
using Alexandria.ItemAPI;
using Alexandria.Misc;
using UnityEngine;

namespace NevernamedsItems
{
    class Bambarrage : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Bambarrage";
            string resourceName = "NevernamedsItems/Resources/NeoActiveSprites/bambarrage_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Bambarrage>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Xiao Zhu Tong Jian";
            string longDesc = "An ancient bamboo tube, hung at the hip- and capable of launching a devastating barrage of poisoned rocket arrows!";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 600);

            projPrefab = ((Gun)PickupObjectDatabase.GetById(56)).DefaultModule.projectiles[0].gameObject.InstantiateAndFakeprefab().GetComponent<Projectile>();
            projPrefab.SetProjectileSpriteRight("bambarrage_proj", 17, 3, false, tk2dBaseSprite.Anchor.MiddleCenter, 17, 3);
            projPrefab.baseData.damage = 7f;
            projPrefab.baseData.AccelerationCurve = ((Gun)PickupObjectDatabase.GetById(39)).DefaultModule.projectiles[0].baseData.AccelerationCurve;
            projPrefab.AppliesPoison = true;
            projPrefab.PoisonApplyChance = 0.8f;
            projPrefab.healthEffect = StaticStatusEffects.irradiatedLeadEffect;
            projPrefab.hitEffects.overrideMidairDeathVFX = ((Gun)PickupObjectDatabase.GetById(543)).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX;
            projPrefab.hitEffects.alwaysUseMidair = true;

            item.quality = ItemQuality.D; 
        }
        public static Projectile projPrefab;
        public override void DoEffect(PlayerController user)
        {
            StartCoroutine(SpawnBarrage(user));
        }     
        private IEnumerator SpawnBarrage(PlayerController user)
        {
            for (int i = 0; i <6; i++)
            {
                AkSoundEngine.PostEvent("Play_WPN_stickycrossbow_shot_01", base.gameObject);
                Projectile rocketArrow = projPrefab.InstantiateAndFireInDirection(user.CurrentGun.barrelOffset.position, user.CurrentGun.CurrentAngle, 40, user).GetComponent<Projectile>();
                rocketArrow.Owner = user;
                rocketArrow.Shooter = user.specRigidbody;
                rocketArrow.baseData.damage *= user.stats.GetStatValue(PlayerStats.StatType.Damage);
                rocketArrow.baseData.speed *= user.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                rocketArrow.baseData.force *= user.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                user.DoPostProcessProjectile(rocketArrow);
                yield return new WaitForSeconds(0.1f);
            }
            yield break;
        }
        public override bool CanBeUsed(PlayerController user)
        {
            if (user.CurrentGun != null) return true;
            return base.CanBeUsed(user);
        }
    }
}
