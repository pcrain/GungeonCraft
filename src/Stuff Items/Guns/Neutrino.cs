using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class Neutrino : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Neutrino", "neutrino");
            Game.Items.Rename("outdated_gun_mods:neutrino", "nn:neutrino");
            gun.gameObject.AddComponent<Neutrino>();
            gun.SetShortDescription("D'arvit");
            gun.SetLongDescription("Made for tiny hands. A micro-nuclear battery will keep this blaster burning longer than the lifetime of Gunymede's star!" + "\n\nReload a full clip to swap between 'stun' and 'fry' modes.");

            gun.SetupSprite(null, "neutrino_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 10);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(32) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            gun.InfiniteAmmo = true;

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Ordered;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(809) as Gun).muzzleFlashEffects;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.15f;
            gun.DefaultModule.numberOfShotsInClip = 12;
            gun.barrelOffset.transform.localPosition = new Vector3(0.87f, 0.31f, 0f);
            gun.SetBaseMaxAmmo(100);
            gun.ammo = 100;
            gun.gunClass = GunClass.FULLAUTO;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 2.5f;
            projectile.baseData.speed *= 1.5f;
            projectile.AppliesStun = true;
            projectile.AppliedStunDuration = 4f;
            projectile.StunApplyChance = 0.2f;
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.SmoothLightBlueLaserCircleVFX;
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.SetProjectileSpriteRight("neutrino_stun_proj", 10, 3, true, tk2dBaseSprite.Anchor.MiddleCenter, 8, 3);

            //FIREBULLETS
            Projectile projectile2 = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile2.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile2.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile2);
            projectile2.baseData.damage = 2.5f;
            projectile2.baseData.speed *= 1.5f;
            projectile2.AppliesFire = true;
            projectile2.fireEffect = StaticStatusEffects.hotLeadEffect;
            projectile2.FireApplyChance = 0.07f;
            projectile2.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.YellowLaserCircleVFX;
            projectile2.hitEffects.alwaysUseMidair = true;
            projectile2.SetProjectileSpriteRight("neutrino_burn_proj", 10, 3, true, tk2dBaseSprite.Anchor.MiddleCenter, 8, 3);

            burnModeProj = projectile2;

            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
        }
        public static Projectile burnModeProj;
        private bool isInBurnMode = false;

        public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
        {
            if (isInBurnMode) return burnModeProj;
            return base.OnPreFireProjectileModifier(gun, projectile, mod);
        }
        public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
        {
            if (gun.ClipShotsRemaining == gun.ClipCapacity)
            {
                if (isInBurnMode) isInBurnMode = false;
                else isInBurnMode = true;
            }
            base.OnReloadPressed(player, gun, manualReload);
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile && projectile.ProjectilePlayerOwner())
            {
                if (projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Softnose"))
                {
                    projectile.baseData.damage *= 2;
                    projectile.baseData.speed *= 0.5f;
                }
            }
            base.PostProcessProjectile(projectile);
        }
        public Neutrino()
        {

        }
    }
}
