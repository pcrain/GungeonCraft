using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using SynergyTools;
using MonoMod.RuntimeDetour;
using System.Reflection;
using SaveAPI;

namespace NevernamedsItems
{
    public class Skorpion : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Skorpion", "skorpion");
            Game.Items.Rename("outdated_gun_mods:skorpion", "nn:skorpion");
            gun.gameObject.AddComponent<Skorpion>();
            gun.SetShortDescription("Model 59");
            gun.SetLongDescription("A military sidearm most notable for not actually incorporating scorpions at any stage during construction.");

            gun.SetupSprite(null, "skorpion_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(23) as Gun).muzzleFlashEffects;
            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.4f;
            gun.DefaultModule.cooldownTime = 0.06f;
            gun.DefaultModule.numberOfShotsInClip = 45;
            gun.DefaultModule.angleVariance = 10f;
            gun.barrelOffset.transform.localPosition = new Vector3(42f/16f, 13f/16f, 0f);
            gun.SetBaseMaxAmmo(660);
            gun.ammo = 660;
            gun.gunClass = GunClass.FULLAUTO;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 4f;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.SMALL_BULLET;

            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            AdvancedFireOnReloadSynergyProcessor reloadfire = gun.gameObject.AddComponent<AdvancedFireOnReloadSynergyProcessor>();
            reloadfire.synergyToCheck = "Skorpion Sting";
            reloadfire.angleVariance = 5f;
            reloadfire.numToFire = 1;
            reloadfire.projToFire = (PickupObjectDatabase.GetById(406) as Gun).Volley.projectiles[1].projectiles[0];

            gun.AddToSubShop(ItemBuilder.ShopType.Trorc);
            ID = gun.PickupObjectId;
        }
        public static int ID;     
    }
}
