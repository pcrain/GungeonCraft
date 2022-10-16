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

    public class Ranger : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Ranger", "ranger");
            Game.Items.Rename("outdated_gun_mods:ranger", "nn:ranger");
            var behav = gun.gameObject.AddComponent<Ranger>();

            gun.SetShortDescription("Shooting Range");
            gun.SetLongDescription("Fires an even range of bullets, starting high in damage at one end of the spread and incrementally decreasing towards the other.");

            gun.SetupSprite(null, "ranger_idle_001", 8);
            //gun.gunSwitchGroup = (PickupObjectDatabase.GetById(86) as Gun).gunSwitchGroup;
            //gun.muzzleFlashEffects.type = VFXPoolType.None;


            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.barrelOffset.transform.localPosition = new Vector3(1.93f, 0.81f, 0f);

            for (int i = 0; i < 6; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            }
            float AllModCooldown = 0.6f;
            int AllModClipshots = 8;

            //
            Projectile twentyDamageProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            twentyDamageProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(twentyDamageProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(twentyDamageProjectile);
            twentyDamageProjectile.baseData.damage *= 4f;
            twentyDamageProjectile.baseData.speed *= 0.65f;
            //twentyDamageProjectile.RuntimeUpdateScale(2);

            Projectile seventeenDamageProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            seventeenDamageProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(seventeenDamageProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(seventeenDamageProjectile);
            seventeenDamageProjectile.baseData.damage *= 3.4f;
            seventeenDamageProjectile.baseData.speed *= 0.65f;
            //twentyDamageProjectile.RuntimeUpdateScale(1.75f);

            Projectile fourteenDamageProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            fourteenDamageProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(fourteenDamageProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(fourteenDamageProjectile);
            fourteenDamageProjectile.baseData.damage *= 2.8f;
            fourteenDamageProjectile.baseData.speed *= 0.65f;
            //fourteenDamageProjectile.RuntimeUpdateScale(1.25f);

            Projectile elevenDamageProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            elevenDamageProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(elevenDamageProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(elevenDamageProjectile);
            elevenDamageProjectile.baseData.damage *= 2.2f;
            elevenDamageProjectile.baseData.speed *= 0.65f;
            // elevenDamageProjectile.RuntimeUpdateScale(1f);

            Projectile eightDamageProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            eightDamageProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(eightDamageProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(eightDamageProjectile);
            eightDamageProjectile.baseData.damage *= 1.6f;
            eightDamageProjectile.baseData.speed *= 0.65f;
                        //eightDamageProjectile.RuntimeUpdateScale(0.75f);

                        Projectile fiveDamageProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            fiveDamageProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(fiveDamageProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(fiveDamageProjectile);
            fiveDamageProjectile.baseData.damage *= 1f;
            fiveDamageProjectile.baseData.speed *= 0.65f;
            //fiveDamageProjectile.RuntimeUpdateScale(0.5f);

            twentyDamageProjectile.SetProjectileSpriteRight("rangerproj_1", 23, 23, true, tk2dBaseSprite.Anchor.MiddleCenter, 20, 20);
            seventeenDamageProjectile.SetProjectileSpriteRight("rangerproj_2", 20, 20, true, tk2dBaseSprite.Anchor.MiddleCenter, 17, 17);
            fourteenDamageProjectile.SetProjectileSpriteRight("rangerproj_3", 16, 16, true, tk2dBaseSprite.Anchor.MiddleCenter, 13, 13);
            elevenDamageProjectile.SetProjectileSpriteRight("rangerproj_4", 12, 12, true, tk2dBaseSprite.Anchor.MiddleCenter, 11, 11);
            eightDamageProjectile.SetProjectileSpriteRight("rangerproj_5", 9, 9, true, tk2dBaseSprite.Anchor.MiddleCenter,8, 8);
            fiveDamageProjectile.SetProjectileSpriteRight("rangerproj_6", 6,6, true, tk2dBaseSprite.Anchor.MiddleCenter, 6, 6);

            int i2 = 0;
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.numberOfShotsInClip = AllModClipshots;
                mod.cooldownTime = AllModCooldown;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Ordered;
                mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
               

                if (i2 <= 0) //40 degrees
                {
                    mod.ammoCost = 0;
                    mod.angleVariance = 0.01f;
                    mod.angleFromAim = 40f;
                    mod.projectiles[0] = twentyDamageProjectile;
                   mod.projectiles.Add(fiveDamageProjectile);
                    i2++;
                }
                else if (i2 == 1) //24 degrees
                {
                    mod.ammoCost = 1;
                    mod.angleVariance = 0.01f;
                    mod.angleFromAim = 24f;
                    mod.projectiles[0] = seventeenDamageProjectile;
                    mod.projectiles.Add(eightDamageProjectile);
                    i2++;
                }
                else if (i2 == 2) //8 Degrees
                {
                    mod.ammoCost = 0;
                    mod.angleFromAim = 8f;
                    mod.angleVariance = 0.1f;
                    mod.projectiles[0] = fourteenDamageProjectile;
                    mod.projectiles.Add(elevenDamageProjectile);
                    i2++;
                }
                else if (i2 == 3) //-8 Degrees
                {
                    mod.ammoCost = 0;
                    mod.angleFromAim = -8f;
                    mod.angleVariance = 0.1f;
                    mod.projectiles[0] = elevenDamageProjectile;
                    mod.projectiles.Add(fourteenDamageProjectile);
                    i2++;
                }
                else if (i2 == 4) //-24 Degrees
                {
                    mod.ammoCost = 0;
                    mod.angleFromAim = -24f;
                    mod.angleVariance = 0.1f;
                    mod.projectiles[0] = eightDamageProjectile;
                    mod.projectiles.Add(seventeenDamageProjectile);
                    i2++;
                }
                else if (i2 >= 5) //-40 Degrees
                {
                    mod.ammoCost = 0;
                    mod.angleFromAim = -40f;
                    mod.angleVariance = 0.1f;
                    mod.projectiles[0] = fiveDamageProjectile;
                    mod.projectiles.Add(twentyDamageProjectile);
                    i2++;
                }
            }
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "Justice Bullets";
            gun.reloadTime = 1.4f;
            gun.SetBaseMaxAmmo(100);
            gun.gunClass = GunClass.SHOTGUN;
            gun.quality = PickupObject.ItemQuality.A; //A
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            RangerID = gun.PickupObjectId;
        }
        public static int RangerID;
        public Ranger()
        {

        }
    }
    
}