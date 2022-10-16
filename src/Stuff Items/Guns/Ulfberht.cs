using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{

    public class Ulfberht : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Ulfberht", "ulfbehrt");
            Game.Items.Rename("outdated_gun_mods:ulfberht", "nn:ulfberht");
            var behav = gun.gameObject.AddComponent<Ulfberht>();
            gun.SetShortDescription("+VLFBEHT+");
            gun.SetLongDescription("Part of an ancient series of guns from a widespread and respected family of Gunsmiths, now lost to time."+"\n\nCrusty, rusty firearms such as this one are the only evidence of their existence...");

            gun.SetupSprite(null, "ulfbehrt_idle_001", 8);

            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(38) as Gun).muzzleFlashEffects;

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.8f;
            gun.DefaultModule.cooldownTime = 0.25f;
            gun.DefaultModule.numberOfShotsInClip = 7;
            gun.DefaultModule.angleVariance = 10;
            gun.barrelOffset.transform.localPosition = new Vector3(24f/16f, 13f / 16f, 0f);
            gun.SetBaseMaxAmmo(150);
            gun.ammo = 150;
            gun.gunClass = GunClass.PISTOL;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 7f;
            projectile.baseData.range = 5f;
            projectile.baseData.speed *= 0.9f;
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.hitEffects.overrideMidairDeathVFX = (PickupObjectDatabase.GetById(761) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX;

            SpawnProjModifier spawner = projectile.gameObject.AddComponent<SpawnProjModifier>();
            spawner.spawnProjecitlesOnDieInAir = true;
            spawner.spawnProjectilesInFlight = false;
            spawner.spawnProjectilesOnCollision = true;
            spawner.spawnOnObjectCollisions = true;
            spawner.spawnCollisionProjectilesOnBounce = true;
            spawner.randomRadialStartAngle = true;

            GameObject proj2 =  (PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0].gameObject.InstantiateAndFakeprefab();
            proj2.GetComponent<Projectile>().AdditionalScaleMultiplier = 0.8f;
            proj2.GetComponent<Projectile>().baseData.damage = 3;
            spawner.projectileToSpawnOnCollision = proj2.GetComponent<Projectile>();
            spawner.collisionSpawnStyle = SpawnProjModifier.CollisionSpawnStyle.RADIAL;
            spawner.fireRandomlyInAngle = true;
            spawner.PostprocessSpawnedProjectiles = true;
            spawner.numberToSpawnOnCollison = 5;

            projectile.SetProjectileSpriteRight("ulfbehrt_proj", 11, 9, true, tk2dBaseSprite.Anchor.MiddleCenter, 9, 7);
            gun.DefaultModule.projectiles[0] = projectile;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Ulfbehrt Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/ulfbehrt_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/ulfbehrt_clipempty");

            gun.quality = PickupObject.ItemQuality.D;
            ETGMod.Databases.Items.Add(gun, false, "ANY");
            ID = gun.PickupObjectId;

        }
        public static int ID;
    }
}
