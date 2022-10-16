using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using Alexandria.Misc;
using SaveAPI;

namespace NevernamedsItems
{

    public class Schwarzlose : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Schwarzlose", "schwarzlose");
            Game.Items.Rename("outdated_gun_mods:schwarzlose", "nn:schwarzlose");
            gun.gameObject.AddComponent<Schwarzlose>();
            gun.SetShortDescription("I See It's As Big As Mine");
            gun.SetLongDescription("An old fashioned machine gun with a water-cooled barrel."+"\n\nExcercising remarkable minimalism in it's parts- it's bullets seem prone to drift.");

            gun.SetupSprite(null, "schwarzlose_idle_001", 8);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(51) as Gun).gunSwitchGroup;

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(519) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.5f;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 20;
            gun.barrelOffset.transform.localPosition = new Vector3(37f / 16f, 6f / 16f, 0f);
            gun.SetBaseMaxAmmo(350);
            gun.gunClass = GunClass.FULLAUTO;
            //BULLET STATS
            Projectile projectile = ProjectileUtility.SetupProjectile(519); 
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 9f;
            projectile.AdditionalScaleMultiplier = 0.8f;
            projectile.gameObject.AddComponent<ProjectileMotionDrift>();

            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, false, "ANY");
            gun.AddToSubShop(ItemBuilder.ShopType.Trorc);

            ID = gun.PickupObjectId;
        }
        public static int ID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile && projectile.ProjectilePlayerOwner() && projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Water Jacket"))
            {
                if (projectile.GetComponent<BounceProjModifier>()) { projectile.GetComponent<BounceProjModifier>().numberOfBounces++; }
                else { projectile.gameObject.AddComponent<BounceProjModifier>(); }

                projectile.GetComponent<BounceProjModifier>().OnBounceContext += OnBounced;
            }
            base.PostProcessProjectile(projectile);
        }
        public void OnBounced(BounceProjModifier bounce, SpeculativeRigidbody body)
        {
            if (body && body.projectile)
            {
                HomingModifier homing = body.projectile.gameObject.GetOrAddComponent<HomingModifier>();
                homing.HomingRadius += 5;
            }
        }

    }
    
}
