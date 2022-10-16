using System.Text;
using UnityEngine;
using Alexandria.ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;
using System.Collections.Generic;
using Dungeonator;

namespace NevernamedsItems
{
    public class LongswordShot : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Longsword shot";
            string resourceName = "NevernamedsItems/Resources/longswordshot_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<LongswordShot>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Foreign Technology";
            string longDesc = "Your bullets cut through the air!" + "\n\nPhased into our dimension through a tear in the curtain from a terrible and heretical place known as the 'Swordtress'.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);
            item.CanBeDropped = true;
            item.quality = PickupObject.ItemQuality.B;
            item.SetTag("bullet_modifier");
        }

        public void PostProcess(Projectile bullet, float chanceScaler)
        {
            if (bullet && bullet.Owner && bullet.Owner is PlayerController)
            {
                float procChance = 0.35f;
                procChance *= chanceScaler;
                if (UnityEngine.Random.value <= procChance)
                {
                    SlashData slashParams = new SlashData();


                    PlayerController player = bullet.Owner as PlayerController;
                    bullet.baseData.speed *= 0.5f;
                    bullet.UpdateSpeed();
                    PierceProjModifier piercing = bullet.gameObject.GetOrAddComponent<PierceProjModifier>();
                    piercing.penetratesBreakables = true;
                    piercing.penetration += 2;
                    ProjectileSlashingBehaviour slashing = bullet.gameObject.GetOrAddComponent<ProjectileSlashingBehaviour>();
                    slashing.DestroyBaseAfterFirstSlash = false;
                    slashing.timeBetweenSlashes = 0.30f;
                    if (player.PlayerHasActiveSynergy("Sword Mage")) slashing.timeBetweenSlashes = 0.15f;

                    slashing.SlashDamageUsesBaseProjectileDamage = true;
                    if (player.PlayerHasActiveSynergy("Sabre Throw")) slashParams.projInteractMode = SlashDoer.ProjInteractMode.REFLECT;
                    if (player.PlayerHasActiveSynergy("Whirling Blade")) slashing.doSpinAttack = true;
                    if (player.PlayerHasActiveSynergy("Live By The Sword")) bullet.OnDestruction += this.OnDestruction;

                    slashParams.playerKnockbackForce = 0;
                    slashing.slashParameters = slashParams;
                }
            }

        }
        private void OnDestruction(Projectile bullet)
        {
            if (Owner && Owner.specRigidbody)
            {
                ExplosionData LinearChainExplosionData = Gungeon.Game.Items["katana_bullets"].GetComponent<ComplexProjectileModifier>().LinearChainExplosionData.CopyExplosionData();
                LinearChainExplosionData.ignoreList.Add(Owner.specRigidbody);
                Exploder.Explode(bullet.specRigidbody.UnitCenter, LinearChainExplosionData, Vector2.zero);
            }
        }
        public override void Pickup(PlayerController player)
        {
            player.PostProcessProjectile += this.PostProcess;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {

            player.PostProcessProjectile -= this.PostProcess;

            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessProjectile -= this.PostProcess;
            }
            base.OnDestroy();
        }
    }

}