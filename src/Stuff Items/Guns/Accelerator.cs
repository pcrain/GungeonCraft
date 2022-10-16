using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public class Accelerator : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Accelerator", "accelerator");
            Game.Items.Rename("outdated_gun_mods:accelerator", "nn:accelerator");
            var behav = gun.gameObject.AddComponent<Accelerator>();

            gun.SetShortDescription("Sapper");
            gun.SetLongDescription("The projectiles of this remarkable weapon suck the speed right out of enemy bullets!"+"\n\nReverse engineered from vampires.");

            gun.SetupSprite(null, "accelerator_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.2f;
            gun.DefaultModule.cooldownTime = 0.1f;
            //gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 7;
            gun.barrelOffset.transform.localPosition = new Vector3(1.75f, 0.56f, 0f);
            gun.SetBaseMaxAmmo(210);
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 7f;
            projectile.baseData.speed *= 1.1f;
            projectile.baseData.range *= 10f;

            projectile.gameObject.AddComponent<EnemyBulletSpeedSapperMod>();

            projectile.pierceMinorBreakables = true;
            //projectile.shouldRotate = true;
            PierceProjModifier orAddComponent = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            orAddComponent.penetratesBreakables = true;
            orAddComponent.penetration = 1;



            projectile.SetProjectileSpriteRight("accelerator_projectile", 11, 9, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 8);
            
            projectile.transform.parent = gun.barrelOffset;


            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            ID = gun.PickupObjectId;
        }
        public static int ID;
        public Accelerator()
        {

        }
    }
}
