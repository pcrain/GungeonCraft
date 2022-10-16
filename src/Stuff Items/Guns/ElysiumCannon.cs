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

    public class ElysiumCannon : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Elysium Cannon", "elysiumcannon");
            Game.Items.Rename("outdated_gun_mods:elysium_cannon", "nn:elysium_cannon");
            var behav = gun.gameObject.AddComponent<ElysiumCannon>();
            behav.overrideNormalFireAudio = "Play_ElectricSound";
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("The Sky Will Crack");
            gun.SetLongDescription("Cracks reality and unleashes a torrent of holey light."+"\n\nInitially created by the Order of the True Gun for righteous inquisitions, this unimaginable flood of radiant power has been the subject of countless wars.");

            gun.SetupSprite(null, "elysiumcannon_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation,16);
            gun.SetAnimationFPS(gun.idleAnimation, 16);
            gun.SetAnimationFPS(gun.reloadAnimation, 16);

            for (int i = 0; i < 2; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            }
            gun.muzzleFlashEffects.type = VFXPoolType.None;

            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {

                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.Automatic;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.01f;
                mod.angleVariance = 20f;
                mod.numberOfShotsInClip = 70;
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                projectile.baseData.range *= 1.5f;
                projectile.baseData.damage = 3f;
                projectile.baseData.force *= 0.5f;
                projectile.baseData.speed *= 0.85f;
                projectile.PenetratesInternalWalls = true;
                projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.BigWhitePoofVFX;
                projectile.hitEffects.alwaysUseMidair = true;

                    PierceProjModifier pierce = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
                pierce.penetration = 100;
                pierce.penetratesBreakables = true;
                projectile.pierceMinorBreakables = true;
                projectile.SetProjectileSpriteRight("elysiumcannon_proj", 56, 56, true, tk2dBaseSprite.Anchor.MiddleCenter, 50, 50);

                BounceProjModifier Bouncing = projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
                    Bouncing.numberOfBounces = 1;

                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                projectile.transform.parent = gun.barrelOffset;
            }
            gun.gunHandedness = GunHandedness.HiddenOneHanded;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.reloadAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "white";

            gun.reloadTime = 2f;
            gun.barrelOffset.transform.localPosition = new Vector3(1.25f, 0.81f, 0f);
            gun.SetBaseMaxAmmo(777);
            gun.gunClass = GunClass.SHOTGUN;
            //BULLET STATS
            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            gun.Volley.UsesShotgunStyleVelocityRandomizer = true;

            ID = gun.PickupObjectId;
        }
        public static int ID;
    }
}