using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{

    public class DecretionCarbine : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Decretion Carbine", "decretioncarbine");
            Game.Items.Rename("outdated_gun_mods:decretion_carbine", "nn:decretion_carbine");
            gun.gameObject.AddComponent<DecretionCarbine>();
            gun.SetShortDescription("Shrinky Shrinky");
            gun.SetLongDescription("The bullets of this rapid firing bronze marvel were originally indended to grow in size and damage as they travelled. Unfortunately, the blueprint was read upside down, and the effect was reversed.");

            gun.SetupSprite(null, "decretioncarbine_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);
            gun.SetAnimationFPS(gun.reloadAnimation, 0);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(15) as Gun).gunSwitchGroup;

            //GUN STATS
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(38) as Gun).muzzleFlashEffects;
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.barrelOffset.transform.localPosition = new Vector3(28f / 16f, 15f / 16f, 0f);
            gun.SetBaseMaxAmmo(450);
            gun.gunClass = GunClass.FULLAUTO;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("DecretionCarbine Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/decretioncarbine_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/decretioncarbine_clipempty");

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.SetProjectileSpriteRight("decretioncarbine_proj", 15, 15, true, tk2dBaseSprite.Anchor.MiddleCenter, 15, 15);
            projectile.baseData.damage = 10f;
            ScaleChangeOverTimeModifier scaler = projectile.gameObject.GetOrAddComponent<ScaleChangeOverTimeModifier>();
            scaler.destroyAfterChange = true;
            scaler.timeToChangeOver = 0.4f;
            scaler.ScaleToChangeTo = 0.001f;
            scaler.suppressDeathFXIfdestroyed = true;
            scaler.scaleMultAffectsDamage = true;


            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");


            ID = gun.PickupObjectId;
        }
        public static int ID;
    }
}