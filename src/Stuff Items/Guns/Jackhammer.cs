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

    public class Jackhammer : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Jackhammer", "jackhammer");
            Game.Items.Rename("outdated_gun_mods:jackhammer", "nn:jackhammer");
            gun.gameObject.AddComponent<Jackhammer>();
            gun.SetShortDescription("A Better Way");
            gun.SetLongDescription("A fully automatic shotgun designed by a dissilusioned Gungeoneer."+"\n\nPump action is soooo last decade.");

            gun.SetupSprite(null, "jackhammer_idle_001", 8);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(98) as Gun).gunSwitchGroup;
            gun.SetAnimationFPS(gun.shootAnimation, 13);
            gun.SetAnimationFPS(gun.idleAnimation, 5);
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(23) as Gun).muzzleFlashEffects;

            for (int i = 0; i < 4; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            }

            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {

                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.Automatic;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.15f;
                mod.angleVariance = 10f;
                mod.numberOfShotsInClip = 20;
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);

                projectile.AdditionalScaleMultiplier = 0.8f;
                projectile.baseData.range = 15;
                projectile.baseData.damage = 4;
                projectile.baseData.force *= 0.5f;
                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
            }
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.BLUE_SHOTGUN;
            
            gun.reloadTime = 1f;
            gun.barrelOffset.transform.localPosition = new Vector3(25f/16f, 6f/16f, 0f);
            gun.SetBaseMaxAmmo(350);
            gun.gunClass = GunClass.SHOTGUN;
            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            gun.Volley.UsesShotgunStyleVelocityRandomizer = true;

            ID = gun.PickupObjectId;
        }
        public static int ID;
    }
}
