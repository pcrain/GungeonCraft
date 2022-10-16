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

    public class Bleech : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Bleech", "bleech");
            Game.Items.Rename("outdated_gun_mods:bleech", "nn:bleech");
            var comp = gun.gameObject.AddComponent<Bleech>();

            gun.SetShortDescription("");
            gun.SetLongDescription("");

            gun.SetupSprite(null, "bleech_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(198) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(80) as Gun, true, false);
            gun.barrelOffset.transform.localPosition = new Vector3(1.5f, 0.81f, 0f);
            gun.reloadTime = 1.3f;

            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 7;

            gun.gunClass = GunClass.SILLY;
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.RedLaserCircleVFX;
            projectile.hitEffects.alwaysUseMidair = true;

            projectile.SetProjectileSpriteRight("eldermagnum_projectile", 5, 5, true, tk2dBaseSprite.Anchor.MiddleCenter, 4, 4);
            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

        }
    }
}