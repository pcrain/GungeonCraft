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

    public class WheelLock : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Wheellock", "wheellock");
            Game.Items.Rename("outdated_gun_mods:wheellock", "nn:wheellock");
            gun.gameObject.AddComponent<WheelLock>();
            gun.SetShortDescription("Wheeler Dealer");
            gun.SetLongDescription("A classical precursor to the flintlock."+"\n\nUses a spring-powered spinning wheel to ignite the powder within.");
            gun.SetupSprite(null, "wheellock_idle_001", 8);
            gun.SetAnimationFPS(gun.shootAnimation, 12);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(9) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.angleVariance = 5f;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.5f;
            gun.DefaultModule.numberOfShotsInClip = 1;
            gun.barrelOffset.transform.localPosition = new Vector3(1.81f, 0.81f, 0f);
            gun.SetBaseMaxAmmo(50);
            gun.gunClass = GunClass.SHITTY;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.speed *= 0.5f;
            projectile.baseData.damage = 15f;
            projectile.transform.parent = gun.barrelOffset;

            gun.quality = PickupObject.ItemQuality.D;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            WheellockID = gun.PickupObjectId;
        }
        public static int WheellockID;
        public WheelLock()
        {

        }
    }
}