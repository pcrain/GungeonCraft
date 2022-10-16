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
    public class Nocturne : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Nocturne", "nocturne");
            Game.Items.Rename("outdated_gun_mods:nocturne", "nn:nocturne");
            gun.gameObject.AddComponent<Nocturne>();
            gun.SetShortDescription("Welcome Back");
            gun.SetLongDescription("Alternates between faster purple blasts, and more damaging green blasts."+"\n\nThis gun was left in the Gungeon as one part of a great puzzle."+"\nUnfortunately, the seekers seem to have given up.");

            gun.SetupSprite(null, "nocturne_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 10);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(357) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Ordered;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(334) as Gun).muzzleFlashEffects;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 6;
            gun.barrelOffset.transform.localPosition = new Vector3(1.75f, 0.87f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.ammo = 200;
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 9f;
            projectile.baseData.speed *= 1.5f;
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.RedLaserCircleVFX;
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.SetProjectileSpriteRight("nocturnepurple_projectile", 10, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 8, 8);

            Projectile projectile2 = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            projectile2.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile2.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile2);
            projectile2.baseData.damage = 15f;
            projectile2.baseData.speed *= 0.5f;
            projectile2.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.GreenLaserCircleVFX;
            projectile2.hitEffects.alwaysUseMidair = true;
            projectile2.SetProjectileSpriteRight("nocturnegreen_projectile", 10, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 8, 8);

            gun.DefaultModule.projectiles[0] = projectile;
            gun.DefaultModule.projectiles.Add(projectile2);
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Nocturne Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/nocturne_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/nocturne_clipempty");
            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
        }
        public Nocturne()
        {

        }
    }
}