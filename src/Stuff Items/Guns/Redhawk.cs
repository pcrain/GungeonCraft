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
using SaveAPI;

namespace NevernamedsItems
{

    public class Redhawk : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Redhawk", "redhawk");
            Game.Items.Rename("outdated_gun_mods:redhawk", "nn:redhawk");
            gun.gameObject.AddComponent<Redhawk>();
            gun.SetShortDescription("Rugged");
            gun.SetLongDescription("An ancient masterwork, rumoured to be an unreleased Edwin classical original." + "\n\nThe Redhawk's bullets seem to rip and rend the exact right strands of sinew and muscle to cripple any foe.");

            gun.SetupSprite(null, "redhawk_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);
            gun.SetAnimationFPS(gun.reloadAnimation, 0);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(198) as Gun).gunSwitchGroup;

            //GUN STATS
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(58) as Gun).muzzleFlashEffects;
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.2f;
            gun.DefaultModule.numberOfShotsInClip = 6;
            gun.barrelOffset.transform.localPosition = new Vector3(26f / 16f, 11f / 16f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.gunClass = GunClass.PISTOL;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.MEDIUM_BLASTER;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 8;
            projectile.baseData.force = 4f;
            projectile.baseData.speed = 45f;     
            projectile.hitEffects = (PickupObjectDatabase.GetById(32) as Gun).DefaultModule.projectiles[0].hitEffects;

            projectile.SetProjectileSpriteRight("laserwelder_proj", 10, 3, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 3);
            projectile.hitEffects.CenterDeathVFXOnProjectile = true;

            ProjWeaknessModifier weakness = projectile.gameObject.AddComponent<ProjWeaknessModifier>();
            weakness.chanceToApply = 0.33f;
            

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

            projectile.AddTrailToProjectile(
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

            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            ID = gun.PickupObjectId;
            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.ADVDRAGUN_KILLED_SHADE, true);


        }

        public static int ID;
    }
}
