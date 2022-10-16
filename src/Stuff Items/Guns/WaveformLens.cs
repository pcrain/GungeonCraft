using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{

    public class WaveformLens : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Waveform Lens", "waveformlens");
            Game.Items.Rename("outdated_gun_mods:waveform_lens", "nn:waveform_lens");
            gun.gameObject.AddComponent<WaveformLens>();
            gun.SetShortDescription("No Man's Gun");
            gun.SetLongDescription("A scientific waveform multitool designed for deep space exploration."+"\n\nInitial releases of the technology were highly flawed, but years of R&D have managed to develop the device into something resembling a useful tool.");

            //Whole Gun Stats
            gun.SetupSprite(null, "waveformlens_idle_001", 8);
            gun.SetAnimationFPS(gun.shootAnimation, 15);
            gun.SetAnimationFPS(gun.alternateShootAnimation, 15);
            gun.SetAnimationFPS(gun.reloadAnimation, 5);
            gun.SetAnimationFPS(gun.alternateReloadAnimation, 5);
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(32) as Gun).gunSwitchGroup;
            gun.alternateSwitchGroup = (PickupObjectDatabase.GetById(383) as Gun).gunSwitchGroup;
            gun.barrelOffset.transform.localPosition = new Vector3(16f / 16f, 12f / 16f, 0f);
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(59) as Gun).muzzleFlashEffects;
            gun.IsTrickGun = true;
            gun.reloadTime = 1f;
            gun.SetBaseMaxAmmo(300);
            gun.gunClass = GunClass.FULLAUTO;

            //First Module 
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            //First Module Bullet
            Projectile projectile = ProjectileUtility.SetupProjectile(86);           
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 5f;
            projectile.SetProjectileSpriteRight("waveformlens_proj", 5, 5, false, tk2dBaseSprite.Anchor.MiddleCenter, 4, 4);
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.SmoothLightBlueLaserCircleVFX;
            projectile.hitEffects.alwaysUseMidair = true;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "evo3";

            //Second Module
            ProjectileModule altProjModule = ProjectileModule.CreateClone(((Gun)PickupObjectDatabase.GetById(56)).DefaultModule, false, -1);
            gun.alternateVolley = new ProjectileVolleyData
            {
                projectiles = new List<ProjectileModule> { altProjModule },
                UsesShotgunStyleVelocityRandomizer = false,
                ModulesAreTiers = false,
                BeamRotationDegreesPerSecond = 30.0f,
                DecreaseFinalSpeedPercentMin = -5.0f,
                IncreaseFinalSpeedPercentMax = 5.0f,
                UsesBeamRotationLimiter = false,
            };
            gun.alternateVolley.projectiles[0].ammoType = GameUIAmmoType.AmmoType.MEDIUM_BLASTER;
            gun.alternateVolley.projectiles[0].ammoCost = 1;
            gun.alternateVolley.projectiles[0].shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.alternateVolley.projectiles[0].cooldownTime = 0.5f;
            gun.alternateVolley.projectiles[0].numberOfShotsInClip = 5;
            gun.alternateVolley.projectiles[0].angleVariance = 0;

            Projectile RayProjectile = ProjectileUtility.SetupProjectile(56);
            altProjModule.projectiles[0] = RayProjectile;
            RayProjectile.hitEffects = (PickupObjectDatabase.GetById(32) as Gun).DefaultModule.projectiles[0].hitEffects;
            RayProjectile.baseData.damage = 10;
            RayProjectile.baseData.speed = 600f;
            RayProjectile.SetProjectileSpriteRight("laserwelder_proj", 10, 3, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 3);
            RayProjectile.hitEffects.CenterDeathVFXOnProjectile = true;
            List<string> BeamAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/TrailSprites/laserwelder_trail_001",
                "NevernamedsItems/Resources/TrailSprites/laserwelder_trail_002",
                "NevernamedsItems/Resources/TrailSprites/laserwelder_trail_003",
                "NevernamedsItems/Resources/TrailSprites/laserwelder_trail_004",
                "NevernamedsItems/Resources/TrailSprites/laserwelder_trail_005",
            };
            List<string> ImpactAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/TrailSprites/laserwelder_trailstart_001",
                "NevernamedsItems/Resources/TrailSprites/laserwelder_trailstart_002",
                "NevernamedsItems/Resources/TrailSprites/laserwelder_trailstart_003",
                "NevernamedsItems/Resources/TrailSprites/laserwelder_trailstart_004",
                "NevernamedsItems/Resources/TrailSprites/laserwelder_trailstart_005",
            };
            RayProjectile.AddTrailToProjectile(
                "NevernamedsItems/Resources/TrailSprites/laserwelder_trail_001",
                new Vector2(17, 5),
                new Vector2(0, 6),
                BeamAnimPaths, 20,
                ImpactAnimPaths, 20,
                0.1f,
                -1,
                -1,
                true
                );

            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, false, "ANY");


            ID = gun.PickupObjectId;
        }
        public static int ID;
    }
}