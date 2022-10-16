using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Alexandria.ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Dungeonator;
using SaveAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class MagnetItem : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Magnet";
            string resourceName = "NevernamedsItems/Resources/NeoItemSprites/magnet_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<MagnetItem>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Mysterious Magic";
            string longDesc = "A mysterious artifact. Nobody is quite sure how it works."+"\n\nBullets draw in enemies. Don't take the fight to them, take them to the fight.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.D;
            item.SetTag("bullet_modifier");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.KnockbackMultiplier, 3, StatModifier.ModifyMethod.ADDITIVE);
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.BOSSRUSH_ROBOT, true);
        }

        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            MagnetBehaviour SUCK = sourceProjectile.gameObject.GetOrAddComponent<MagnetBehaviour>();
            SUCK.gravitationalForce = 166f;
            SUCK.statMult = sourceProjectile.ProjectilePlayerOwner().stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
            SUCK.debugMode = false;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.PostProcessProjectile -= this.PostProcessProjectile;
            return debrisObject;
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProjectile;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessProjectile -= this.PostProcessProjectile;
            }
            base.OnDestroy();
        }
    }
}
