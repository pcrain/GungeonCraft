using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{

    public class SoapGun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Soap Gun", "soapgun");
            Game.Items.Rename("outdated_gun_mods:soap_gun", "nn:soap_gun");
            gun.gameObject.AddComponent<SoapGun>();
            gun.SetShortDescription("Rub a dub dub");
            gun.SetLongDescription("Launches a spray of light, airy, and delicate soap bubbles." + "\n\nUsed for class five cleaning emergencies, when approach is not an option.");

            gun.SetupSprite(null, "soapgun_idle_001", 8);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(404) as Gun).gunSwitchGroup;
            gun.SetAnimationFPS(gun.shootAnimation, 15);
            gun.SetAnimationFPS(gun.reloadAnimation, 13);
            gun.SetAnimationFPS(gun.idleAnimation, 5);

            for (int i = 0; i < 4; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(599) as Gun, true, false);
            }
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(404) as Gun).muzzleFlashEffects;
            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {

                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.4f;
                mod.angleVariance = 30f;
                mod.numberOfShotsInClip = 3;

                for (int i = 0; i < mod.projectiles.Count(); i++)
                {
                    Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[i]);
                    mod.projectiles[i] = projectile;
                    projectile.gameObject.MakeFakePrefab();
                    projectile.baseData.speed *= 3f;
                    projectile.baseData.range *= 2f;
                }
                if (mod != gun.DefaultModule)
                {
                    mod.ammoCost = 0;
                }
            }

            gun.reloadTime = 0.9f;
            gun.barrelOffset.transform.localPosition = new Vector3(23f / 16f, 5f / 16f, 0f);
            gun.SetBaseMaxAmmo(100);
            gun.gunClass = GunClass.SHOTGUN;
            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            gun.Volley.UsesShotgunStyleVelocityRandomizer = true;

            ID = gun.PickupObjectId;
        }
        public static int ID;
    }
}