using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Alexandria.ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public class SnailBullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Snail Bullets";
            string resourceName = "NevernamedsItems/Resources/snailbullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<SnailBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Slow as Slugs";
            string longDesc = "It looks like a colony of snails has made it’s home in this empty shell to hide from predatory birds. \n\n" + "While the shell itself cannot be fired, the slime it oozes from the generations of snails within has interesting properties when paired with other ammunition.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.ProjectileSpeed, 0.6f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            item.quality = PickupObject.ItemQuality.A;
            item.SetTag("bullet_modifier");
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_SNAILBULLETS, true);
            item.AddItemToGooptonMetaShop(30);
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProjectile;
        }
        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            float activationChance = 0.8f;
            float num = activationChance * effectChanceScalar;
            if (UnityEngine.Random.value < num)
            {
                sourceProjectile.OnHitEnemy = (Action<Projectile, SpeculativeRigidbody, bool>)Delegate.Combine(sourceProjectile.OnHitEnemy, new Action<Projectile, SpeculativeRigidbody, bool>(this.AddSlowEffect));
            }
        }
        private void AddSlowEffect(Projectile arg1, SpeculativeRigidbody arg2, bool arg3)
        {
            if (arg2 != null && arg2.aiActor != null && Owner != null)
            {
                if (arg2 != null && arg2.healthHaver.IsAlive)
                {
                    if (arg2.aiActor.EnemyGuid != "465da2bb086a4a88a803f79fe3a27677" && arg2.aiActor.EnemyGuid != "05b8afe0b6cc4fffa9dc6036fa24c8ec")
                    {
                        Gun gun = ETGMod.Databases.Items["triple_crossbow"] as Gun;
                        GameActorSpeedEffect gameActorSpeedEffect = gun.DefaultModule.projectiles[0].speedEffect;
                        ApplyDirectStatusEffects.ApplyDirectSlow(arg2.gameActor, 20f, gameActorSpeedEffect.SpeedMultiplier, Color.white, Color.white, EffectResistanceType.None, "Snail Bullets", false, false);
                    }
                }
            }
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.PostProcessProjectile -= this.PostProcessProjectile;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner) Owner.PostProcessProjectile -= this.PostProcessProjectile;
            base.OnDestroy();
        }
    }
}
