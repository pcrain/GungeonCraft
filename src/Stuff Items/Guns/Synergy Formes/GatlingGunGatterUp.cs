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
    public class GatlingGunGatterUp : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Gat Gun", "gatgun");
            Game.Items.Rename("outdated_gun_mods:gat_gun", "nn:gatling_gun+gatter_up");
            gun.gameObject.AddComponent<GatlingGunGatterUp>();
            gun.SetShortDescription("oh dear");
            gun.SetLongDescription("a literal fucking gat");

            gun.SetupSprite(null, "gatgun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 16);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(84) as Gun).gunSwitchGroup;

            //GUN STATS
            gun.reloadTime = 1.5f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.barrelOffset.transform.localPosition = new Vector3(0.87f, 0.43f, 0f);
            gun.SetBaseMaxAmmo(240);
            gun.ammo = 240;

            for (int i = 0; i < 3; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(84) as Gun, true, false);
            }

            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.Automatic;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.25f;
                mod.angleVariance = 5f;
                if (mod != gun.DefaultModule) { mod.angleVariance = 25; }
                mod.numberOfShotsInClip = 16;

                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);

                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                projectile.transform.parent = gun.barrelOffset;
            }

            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            GatGunID = gun.PickupObjectId;
        }
        public static int GatGunID;
        public GatlingGunGatterUp()
        {

        }
    }
}