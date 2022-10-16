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

    public class Glazerbeam : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Glazerbeam", "glazerbeam");
            Game.Items.Rename("outdated_gun_mods:glazerbeam", "nn:glazerbeam");
            gun.gameObject.AddComponent<Glazerbeam>();
            gun.SetShortDescription("Static Lasers");
            gun.SetLongDescription("Fires lasers that hang in the air. Repurposed from arbitrarily menacing 'laser walls' salvaged from an abandoned supervillain hideout.");

            gun.SetupSprite(null, "glazerbeam_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 14);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(153) as Gun).gunSwitchGroup;

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(38) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.25f;
            gun.DefaultModule.angleFromAim = 0;
            gun.DefaultModule.angleVariance = 10;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.barrelOffset.transform.localPosition = new Vector3(2.0f, 0.56f, 0f);
            gun.SetBaseMaxAmmo(300);
            gun.ammo = 300;
            gun.gunClass = GunClass.BEAM;

            Projectile beamtofire = UnityEngine.Object.Instantiate<Projectile>(LaserBullets.SimpleRedBeam);
            beamtofire.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(beamtofire.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(beamtofire);
            beamtofire.baseData.damage = 10;

            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 20f;
            projectile.baseData.speed = 1.7f;
            projectile.baseData.range = 3f;
            projectile.SetProjectileSpriteRight("enemystyle_projectile", 10, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 8, 8);
            projectile.transform.parent = gun.barrelOffset;
            BeamBulletsBehaviour beam = projectile.gameObject.AddComponent<BeamBulletsBehaviour>();
            beam.firetype = BeamBulletsBehaviour.FireType.FORWARDS;
            beam.beamToFire = beamtofire;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "Punishment Ray Lasers";
            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            GlazerbeamID = gun.PickupObjectId;

        }
        public static int GlazerbeamID;
        public Glazerbeam()
        {

        }
    }
}