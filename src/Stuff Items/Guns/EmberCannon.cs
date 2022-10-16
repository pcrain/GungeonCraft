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
    public class EmberCannon : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Ember Cannon", "embercannon");
            Game.Items.Rename("outdated_gun_mods:ember_cannon", "nn:ember_cannon");
            var behav = gun.gameObject.AddComponent<EmberCannon>();
            behav.preventNormalFireAudio = true;
            behav.preventNormalReloadAudio = true;
            behav.overrideNormalReloadAudio = "Play_ENM_flame_veil_01";
            gun.SetShortDescription("Burns Eternal");
            gun.SetLongDescription("This mighty furnace was created to warm a group of hopeless souls trapped in the freezing hollow."+"\n\nThough it no longer burns with it's ancient ferocity, it has not yet run cold in a thousand years.");
            gun.SetupSprite(null, "embercannon_idle_001", 8);


            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].eventAudio = "Play_WPN_seriouscannon_shot_01";
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].triggerEvent = true;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(37) as Gun).muzzleFlashEffects;
            gun.barrelOffset.transform.localPosition = new Vector3(1.93f, 0.87f, 0f);
            gun.SetAnimationFPS(gun.shootAnimation, 10);
            gun.SetAnimationFPS(gun.chargeAnimation, 8);
            gun.gunClass = GunClass.CHARGE;
            for (int i = 0; i < 10; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(83) as Gun, true, false);
            }

            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.Charged;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.70f;
                mod.angleVariance = 20f;
                mod.numberOfShotsInClip = 3;
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);

                RandomProjectileStatsComponent stats = projectile.gameObject.AddComponent<RandomProjectileStatsComponent>();
                stats.randomDamage = true;
                stats.randomSpeed = true;
                stats.randomRange = true;
                stats.randomKnockback = true;
                stats.scaleBasedOnDamage = true;

                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                ProjectileModule.ChargeProjectile chargeProj = new ProjectileModule.ChargeProjectile
                {
                    Projectile = projectile,
                    ChargeTime = 0.5f,
                };
                mod.chargeProjectiles = new List<ProjectileModule.ChargeProjectile> { chargeProj };
            }
            gun.reloadTime = 1f;
            gun.SetBaseMaxAmmo(100);

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).loopStart = 2;

            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            EmberCannonID = gun.PickupObjectId;
        }
        public static int EmberCannonID;
        protected override void Update()
        {
            base.Update();
        }
        public EmberCannon()
        {

        }
    }
}
