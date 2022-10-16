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
    public class StarterPistol : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Starter Pistol", "starterpistol");
            Game.Items.Rename("outdated_gun_mods:starter_pistol", "nn:starter_pistol");
            gun.gameObject.AddComponent<StarterPistol>();
            gun.SetShortDescription("Ready... Set...");
            gun.SetLongDescription("Designed to signal the start of races, this flimsy plastic gun is cheap to fire and at least delivers a sonic payload.");

            gun.SetupSprite(null, "starterpistol_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 16);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(51) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.4f;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.angleVariance = 5;
            gun.DefaultModule.numberOfShotsInClip = 1;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(37) as Gun).muzzleFlashEffects;
            gun.barrelOffset.transform.localPosition = new Vector3(1.12f, 0.68f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.InfiniteAmmo = true;
            gun.ammo = 200;
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.SetProjectileSpriteRight("starterpistol_proj", 6, 17, true, tk2dBaseSprite.Anchor.MiddleCenter, 2, 13);
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.YellowLaserCircleVFX;
            projectile.baseData.damage = 5;
            projectile.baseData.force = 100;
            projectile.baseData.range = 100;
            projectile.baseData.speed = 40;
            BounceProjModifier bounce = projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            bounce.damageMultiplierOnBounce = 2;
            bounce.numberOfBounces++;

            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.D;
            ETGMod.Databases.Items.Add(gun, false   , "ANY");

            StarterPistolID = gun.PickupObjectId;
        }
        public static int StarterPistolID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile.ProjectilePlayerOwner() && projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Mixed Signals"))
            {
                projectile.statusEffectsToApply.Add(StaticStatusEffects.hotLeadEffect);
            }
            base.PostProcessProjectile(projectile);
        }
        public StarterPistol()
        {

        }
    }
}

