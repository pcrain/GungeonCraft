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

    public class ElderMagnum : AdvancedGunBehavior
    {


        public static void Add()
        {
            
            Gun gun = ETGMod.Databases.Items.NewGun("Elder Magnum", "eldermagnum2");
            
            Game.Items.Rename("outdated_gun_mods:elder_magnum", "nn:elder_magnum");
       var comp =     gun.gameObject.AddComponent<ElderMagnum>();

            gun.SetShortDescription("Guncestral");
            gun.SetLongDescription("An ancient firearm, left to age in some safe over hundreds of years."+"\n\nWhoever owned this gun has probably been slinging since before your great grandpappy was born.");
            
            gun.SetupSprite(null, "eldermagnum2_idle_001", 8);
            
            gun.SetAnimationFPS(gun.shootAnimation, 14);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(198) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(80) as Gun, true, false);

            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.3f;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 7;
            gun.barrelOffset.transform.localPosition = new Vector3(1.5f, 0.81f, 0f);
            gun.InfiniteAmmo = true;
            gun.gunClass = GunClass.SHITTY;
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
        public ElderMagnum()
        {

        }
    }
}