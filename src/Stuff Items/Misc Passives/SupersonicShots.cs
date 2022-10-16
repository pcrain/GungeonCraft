using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Alexandria.ItemAPI;
using System.Collections;
using SaveAPI;

namespace NevernamedsItems
{
    public class SupersonicShots : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Supersonic Shots";
            string resourceName = "NevernamedsItems/Resources/supersonicshot_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<SupersonicShots>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Nyoom";
            string longDesc = "Makes your bullets travel at supersonic speeds." + "\n\nBrought to the Gungeon by the infamous speedster Tonic.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.ProjectileSpeed, 10, StatModifier.ModifyMethod.MULTIPLICATIVE);
            item.quality = PickupObject.ItemQuality.A;
            item.SetTag("bullet_modifier");
            item.AddToSubShop(ItemBuilder.ShopType.Cursula);
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.BEATEN_HOLLOW_BOSS_TURBO_MODE, true);
        }
        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            sourceProjectile.AdjustPlayerProjectileTint(Color.blue, 1, 0f);
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

