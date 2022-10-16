using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    public class Glock42 : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Glock 42", "glock42");
            Game.Items.Rename("outdated_gun_mods:glock_42", "nn:glock_42");
            gun.gameObject.AddComponent<Glock42>();
            gun.SetShortDescription("Discrete Sidearm");
            gun.SetLongDescription("A simple firearm designed for easy concealment.");

            gun.SetupSprite(null, "glock42_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 10);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(79) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.5f;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.angleVariance = 16;
            gun.DefaultModule.numberOfShotsInClip = 5;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(79) as Gun).muzzleFlashEffects;
            gun.barrelOffset.transform.localPosition = new Vector3(1.0f, 0.5f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.ammo = 200;
            gun.gunClass = GunClass.SHITTY;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage *= 1.4f;
            projectile.baseData.force *= 2f;
            projectile.baseData.speed *= 0.6f;

            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.D;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            Glock42ID = gun.PickupObjectId;
        }
        public static int Glock42ID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile.Owner && projectile.Owner is PlayerController)
            {
                if (UnityEngine.Random.value <= 0.33f && (projectile.Owner as PlayerController).PlayerHasActiveSynergy("Song of my people"))
                {
                    projectile.AdjustPlayerProjectileTint(ExtendedColours.charmPink, 2);
                    projectile.statusEffectsToApply.Add(StaticStatusEffects.charmingRoundsEffect);
                }
                if ((projectile.Owner as PlayerController).PlayerHasActiveSynergy("Life, The Universe, and Everything"))
                {
                    projectile.baseData.damage *= 2f;
                    HomingModifier homing = projectile.gameObject.GetOrAddComponent<HomingModifier>();
                    homing.HomingRadius = 1000f;
                }
            }
            base.PostProcessProjectile(projectile);
        }
        public override void OnReloadSafe(PlayerController player, Gun gun)
        {
            if (gun.ClipShotsRemaining == 0 && UnityEngine.Random.value <= 0.5f && player.PlayerHasActiveSynergy("Concealed Carry")) StealthEffect();
            base.OnReloadSafe(player, gun);
        }
        private void StealthEffect()
        {
            PlayerController owner = gun.CurrentOwner as PlayerController;
            this.BreakStealth(owner);
            owner.OnItemStolen += this.BreakStealthOnSteal;
            owner.ChangeSpecialShaderFlag(1, 1f);
            owner.healthHaver.OnDamaged += this.OnDamaged;
            owner.SetIsStealthed(true, "glock42");
            owner.SetCapableOfStealing(true, "glock42", null);
            GameManager.Instance.StartCoroutine(this.Unstealthy());
        }
        private IEnumerator Unstealthy()
        {
            yield return new WaitForSeconds(0.15f);
            (gun.CurrentOwner as PlayerController).OnDidUnstealthyAction += this.BreakStealth;
            yield break;
        }      
        private void OnDamaged(float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection)
        {
            this.BreakStealth(gun.CurrentOwner as PlayerController);
        }
        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            if (player.IsStealthed)
            {
            BreakStealth(player);
            }
            base.OnPostDroppedByPlayer(player);
        }

        public override void OnDestroy()
        {
            if (gun && gun.CurrentOwner && gun.CurrentOwner is PlayerController)
            {
                BreakStealth(gun.CurrentOwner as PlayerController);
            }
            base.OnDestroy();
        }
        private void BreakStealthOnSteal(PlayerController arg1, ShopItemController arg2) { this.BreakStealth(arg1); }
        private void BreakStealth(PlayerController player)
        {
            player.ChangeSpecialShaderFlag(1, 0f);
            player.OnItemStolen -= this.BreakStealthOnSteal;
            player.SetIsStealthed(false, "glock42");
            player.healthHaver.OnDamaged -= this.OnDamaged;
            player.SetCapableOfStealing(false, "glock42", null);
            player.OnDidUnstealthyAction -= this.BreakStealth;
            AkSoundEngine.PostEvent("Play_ENM_wizardred_appear_01", base.gameObject);
        }
        public Glock42()
        {

        }
    }
}
