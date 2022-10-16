using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using Dungeonator;
using System.Collections.Generic;

namespace NevernamedsItems
{

    public class GalaxyCrusher : AdvancedGunBehavior
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Galaxy Crusher", "galaxycrusher");
            Game.Items.Rename("outdated_gun_mods:galaxy_crusher", "nn:galaxy_crusher");

            var behav = gun.gameObject.AddComponent<GalaxyCrusher>();
            behav.preventNormalReloadAudio = true;
            behav.overrideNormalReloadAudio = "Play_ENV_water_splash_01";
            gun.SetShortDescription("Cosmic Crunch");
            gun.SetLongDescription("Tears apart the fabric of space time and good game design, allowing for ridiculous gun effects and fourth wall breaking Ammonomicon descriptions.");

            gun.SetupSprite(null, "galaxycrusher_idle_001", 8);

            gun.SetAnimationFPS(gun.chargeAnimation, 13);

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.SetAnimationFPS(gun.reloadAnimation, 9);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].eventAudio = "Play_WPN_blackhole_shot_01";
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].triggerEvent = true;

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).frames[0].eventAudio = "Play_WPN_blackhole_charge_01";
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).frames[0].triggerEvent = true;

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).frames[22].eventAudio = "Play_WPN_stdissuelaser_shot_01";
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).frames[22].triggerEvent = true;

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).frames[24].eventAudio = "Play_WPN_stdissuelaser_shot_01";
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).frames[24].triggerEvent = true;

            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(228) as Gun).muzzleFlashEffects;
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Charged;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 2f;
            gun.DefaultModule.cooldownTime = 0.5f;
            gun.DefaultModule.numberOfShotsInClip = 1;
            gun.barrelOffset.transform.localPosition = new Vector3(4.18f, 1.56f, 0f);
            gun.SetBaseMaxAmmo(10);
            gun.gunClass = GunClass.CHARGE;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).loopStart = 21;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(169) as Gun).DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 20f;
            projectile.baseData.speed *= 10f;
            projectile.AdditionalScaleMultiplier = 2f;

            //SmallerHole
            Projectile smallhole = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(169) as Gun).DefaultModule.projectiles[0]);
            smallhole.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(smallhole.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(smallhole);
            gun.DefaultModule.projectiles[0] = smallhole;
            smallhole.baseData.damage = 5f;
            smallhole.baseData.speed = 5f;
            smallhole.AdditionalScaleMultiplier = 0.9f;
            BounceProjModifier bounce = smallhole.gameObject.AddComponent<BounceProjModifier>();
            bounce.numberOfBounces = 1;
            EasyTrailBullet trail4 = smallhole.gameObject.AddComponent<EasyTrailBullet>();
            trail4.TrailPos = smallhole.transform.position;
            trail4.StartWidth = 1.56f;
            trail4.EndWidth = 0f;
            trail4.LifeTime = 1.5f;
            trail4.BaseColor = Color.black;
            trail4.EndColor = Color.black;

            SpawnProjModifier spawn = projectile.gameObject.AddComponent<SpawnProjModifier>();
            spawn.spawnProjecitlesOnDieInAir = true;
            spawn.spawnProjectilesOnCollision = true;
            spawn.spawnProjectilesInFlight = false;
            spawn.spawnOnObjectCollisions = true;
            spawn.collisionSpawnStyle = SpawnProjModifier.CollisionSpawnStyle.RADIAL;
            spawn.numberToSpawnOnCollison = 30;
            spawn.PostprocessSpawnedProjectiles = true;
            spawn.projectileToSpawnOnCollision = smallhole;
            spawn.spawnCollisionProjectilesOnBounce = false;


            ProjectileModule.ChargeProjectile chargeProj = new ProjectileModule.ChargeProjectile
            {
                Projectile = projectile,
                ChargeTime = 2f,
            };
            gun.DefaultModule.chargeProjectiles = new List<ProjectileModule.ChargeProjectile> { chargeProj };

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "black_hole";

            // Here we just set the quality of the gun and the "EncounterGuid", which is used by Gungeon to identify the gun.
            gun.quality = PickupObject.ItemQuality.S;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            ID = gun.PickupObjectId;
        }
        public static int ID;
    }
}