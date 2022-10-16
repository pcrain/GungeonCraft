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

    public class OBrienFist : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("O'Brien Fist", "obrienfist");
            Game.Items.Rename("outdated_gun_mods:o'brien_fist", "nn:obrien_fist");
            gun.gameObject.AddComponent<OBrienFist>();
            gun.SetShortDescription("Bathe In the Fire");
            gun.SetLongDescription("The limb of a lumbering tortured golem who sought the Gungeon to sate his bloodlust." + "\n\nYour enemies will run, or face your rage.");

            gun.SetupSprite(null, "obrienfist_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 16);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(23) as Gun).gunSwitchGroup;

            //GUN STATS
            gun.gunHandedness = GunHandedness.HiddenOneHanded;
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.08f;
            gun.DefaultModule.angleVariance = 7;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(23) as Gun).muzzleFlashEffects;
            gun.DefaultModule.numberOfShotsInClip = 30;
            gun.barrelOffset.transform.localPosition = new Vector3(17f / 16f, 2f / 16f, 0f);
            gun.SetBaseMaxAmmo(500);
            gun.ammo = 500;
            gun.gunClass = GunClass.FULLAUTO;

            Projectile flame = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(336) as Gun).DefaultModule.projectiles[0]);
            flame.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(flame.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(flame);

            Projectile flame2 = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(722) as Gun).DefaultModule.projectiles[0]);
            flame2.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(flame2.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(flame2);

            Projectile splitter = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            splitter.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(splitter.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(splitter);
            splitter.sprite.renderer.enabled = false;
            SneakyShotgunComponent sneakyShot = splitter.gameObject.GetOrAddComponent<SneakyShotgunComponent>();
            sneakyShot.eraseSource = true;
            sneakyShot.doVelocityRandomiser = true;
            sneakyShot.postProcess = true;
            sneakyShot.projPrefabToFire = flame;
            sneakyShot.scaleOffOwnerAccuracy = true;
            sneakyShot.numToFire = 6;
            sneakyShot.angleVariance = 45f;
            sneakyShot.overrideProjectileSynergy = "The Green Room Pale";
            sneakyShot.synergyProjectilePrefab = flame2;

            gun.DefaultModule.finalProjectile = splitter;
            gun.DefaultModule.usesOptionalFinalProjectile = true;
            gun.DefaultModule.numberOfFinalProjectiles = 1;

            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            ID = gun.PickupObjectId;
        }
        public static int ID;
        public OBrienFist()
        {

        }
    }
}