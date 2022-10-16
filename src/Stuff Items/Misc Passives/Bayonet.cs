using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Dungeonator;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class Bayonet : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Bayonet";
            string resourceName = "NevernamedsItems/Resources/bayonet_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Bayonet>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "They Don't Like The Cold Steel";
            string longDesc = "Cuts at your enemies when you reload your weapon."+"\n\nAn old fashioned blade attached to the end of rifles to add melee proficiency. Angers the Jammed.";

            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.CanBeDropped = true;
            item.quality = PickupObject.ItemQuality.C;

        }
        private void OnReloadPressed(PlayerController player, Gun gun)
        {
                Slash(player);
        }
        private void PostProcessProjectile(Projectile proj, float thingy)
        {
            if (!proj.TreatedAsNonProjectileForChallenge && proj.ProjectilePlayerOwner() && proj.ProjectilePlayerOwner().PlayerHasActiveSynergy("Hack n' Slash"))
            {
                float chance = 0.2f;
                chance *= thingy;
                if (UnityEngine.Random.value<= chance)
                {
                    Slash(proj.ProjectilePlayerOwner()) ;
                }
            }
        }
        private void Slash(PlayerController player)
        {
            Vector2 vector = player.CenterPosition;
            Vector2 normalized = (player.unadjustedAimPoint.XY() - vector).normalized;

            Vector2 dir = (player.CenterPosition + normalized);
            float angleToUse = player.CurrentGun.CurrentAngle;

            SlashData slashParams = new SlashData();
            slashParams.damage = 20f * player.stats.GetStatValue(PlayerStats.StatType.Damage);
            slashParams.enemyKnockbackForce = 10f * player.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);

            SlashDoer.DoSwordSlash(dir, angleToUse, player, slashParams, null);
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnReloadedGun += OnReloadPressed;
            player.PostProcessProjectile += PostProcessProjectile;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.PostProcessProjectile -= PostProcessProjectile;
            player.OnReloadedGun -= OnReloadPressed;
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessProjectile -= PostProcessProjectile;
                Owner.OnReloadedGun -= OnReloadPressed;
            }
            base.OnDestroy();
        }
    }
}