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
    public class Gravitron : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Gravitron", "gravitron");
            Game.Items.Rename("outdated_gun_mods:gravitron", "nn:gravitron");
            gun.gameObject.AddComponent<Gravitron>();
            gun.SetShortDescription("Outstanding In It's Field");
            gun.SetLongDescription("Bullets orbit enemies."+"\n\nMilitary adaptation of orbtial projectile technology, these bullets are clever enough to latch onto the target instead of their owner.");

            gun.SetupSprite(null, "gravitron_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 10);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(13) as Gun).gunSwitchGroup;

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.1f;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(334) as Gun).muzzleFlashEffects;

            gun.DefaultModule.cooldownTime = 0.2f;
            gun.DefaultModule.numberOfShotsInClip = 25;
            gun.barrelOffset.transform.localPosition = new Vector3(2.12f, 0.56f, 0f);
            gun.SetBaseMaxAmmo(360);
            gun.ammo = 360;
            gun.gunClass = GunClass.FULLAUTO;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.range *= 2f;
            GravitronBulletsBehaviour behav = projectile.gameObject.GetOrAddComponent<GravitronBulletsBehaviour>();
            behav.maxOrbitalRadius = 6;
            behav.minOrbitalRadius = 3.5f;
            projectile.SetProjectileSpriteRight("gravitron_projectile",7,7, true, tk2dBaseSprite.Anchor.MiddleCenter, 6, 6);

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Gravitron Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/gravitron_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/gravitron_clipempty");

            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            GravitronID = gun.PickupObjectId;
        }
        public static int GravitronID;
        public Gravitron()
        {

        }
    }
}
