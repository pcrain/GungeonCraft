using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{

    public class BulletBladeGhostForme : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Ghost Blade", "ghostblade");
            Game.Items.Rename("outdated_gun_mods:ghost_blade", "nn:bullet_blade+ghost_sword");
            var behav = gun.gameObject.AddComponent<BulletBladeGhostForme>();
            behav.preventNormalFireAudio = true;
            behav.preventNormalReloadAudio = true;
            //behav.overrideNormalFireAudio = "Play_OBJ_gate_slam_01";//"Play_ENM_gunnut_swing_01";

            gun.SetShortDescription("Forged of Pure Bullet");
            gun.SetLongDescription("The hefty blade of the fearsome armoured sentinels that tread the Gungeon's Halls." + "\n\nHas claimed the life of many a careless gungeoneer with it's wide spread.");
            gun.SetupSprite(null, "ghostblade_idle_001", 8);
            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.SetAnimationFPS(gun.chargeAnimation, 6);

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].eventAudio = "Play_ENM_gunnut_shockwave_01";
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].triggerEvent = true;

            //BURST SHOT
            BurstShot = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            BurstShot.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(BurstShot.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(BurstShot);
            BurstShot.baseData.damage *= 1.6f;
            BurstShot.baseData.speed *= 0.8f;
            BurstShot.SetProjectileSpriteRight("green_enemystyle_projectile", 10, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 8, 8);



            for (int i = 0; i < 46; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            }

            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.angleVariance = 1;
            gun.DefaultModule.cooldownTime = 2.5f;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Charged;
            gun.DefaultModule.numberOfShotsInClip = 1;
            Projectile bigProjectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            gun.DefaultModule.projectiles[0] = bigProjectile;
            bigProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(bigProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(bigProjectile);
            bigProjectile.baseData.damage *= 4;
            SpawnProjModifier radialBurst = bigProjectile.gameObject.AddComponent<SpawnProjModifier>();
            radialBurst.spawnProjectilesInFlight = false;
            radialBurst.spawnProjectilesOnCollision = true;
            radialBurst.spawnProjecitlesOnDieInAir = true;
            radialBurst.spawnOnObjectCollisions = true;
            radialBurst.collisionSpawnStyle = SpawnProjModifier.CollisionSpawnStyle.RADIAL;
            radialBurst.numberToSpawnOnCollison = 30;
            radialBurst.randomRadialStartAngle = true;
            radialBurst.PostprocessSpawnedProjectiles = true;
            radialBurst.projectileToSpawnOnCollision = BurstShot;

            bigProjectile.SetProjectileSpriteRight("large_green_enemystyle_projectile", 18, 18, true, tk2dBaseSprite.Anchor.MiddleCenter, 16, 16);

            ProjectileModule.ChargeProjectile bigchargeProj = new ProjectileModule.ChargeProjectile
            {
                Projectile = bigProjectile,
                ChargeTime = 1f,
            };
            gun.DefaultModule.chargeProjectiles = new List<ProjectileModule.ChargeProjectile> { bigchargeProj };


            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                if (mod != gun.DefaultModule)
                {
                    mod.ammoCost = 1;
                    mod.shootStyle = ProjectileModule.ShootStyle.Charged;
                    mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                    mod.cooldownTime = 2.5f;
                    mod.angleVariance = 70f;
                    mod.numberOfShotsInClip = 1;
                    Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                    mod.projectiles[0] = projectile;
                    projectile.gameObject.SetActive(false);
                    FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                    UnityEngine.Object.DontDestroyOnLoad(projectile);
                    projectile.baseData.damage *= 1.6f;
                    projectile.baseData.speed *= 0.6f;
                    projectile.baseData.range *= 1f;
                    projectile.SetProjectileSpriteRight("green_enemystyle_projectile", 10, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 8, 8);
                    if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                    projectile.transform.parent = gun.barrelOffset;

                    ProjectileModule.ChargeProjectile chargeProj = new ProjectileModule.ChargeProjectile
                    {
                        Projectile = projectile,
                        ChargeTime = 1f,
                    };
                    mod.chargeProjectiles = new List<ProjectileModule.ChargeProjectile> { chargeProj };
                }
            }
            gun.reloadTime = 1f;
            gun.SetBaseMaxAmmo(50);
            gun.quality = PickupObject.ItemQuality.EXCLUDED;

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).loopStart = 4;


            ETGMod.Databases.Items.Add(gun, null, "ANY");
            gun.barrelOffset.transform.localPosition = new Vector3(3.18f, 2.0f, 0f);
            GhostBladeID = gun.PickupObjectId;
        }
        public static int GhostBladeID;
        public static Projectile BurstShot;
        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
        }
        public BulletBladeGhostForme()
        {

        }
    }
}
