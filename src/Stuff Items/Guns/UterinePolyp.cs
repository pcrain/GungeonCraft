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

    public class UterinePolyp : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Uterine Polyp", "uterinepolyp");
            Game.Items.Rename("outdated_gun_mods:uterine_polyp", "nn:uterine_polyp");
        var obj =    gun.gameObject.AddComponent<UterinePolyp>();
            obj.preventNormalFireAudio = true;
            obj.preventNormalReloadAudio = true;
            obj.overrideNormalReloadAudio = "Play_OBJ_cauldron_splash_01";
            obj.overrideNormalFireAudio = "Play_ENM_cult_spew_01";
            gun.SetShortDescription("Endometrial");
            gun.SetLongDescription("A disgusting teratoma-esque growth cut from a demon's womb."+"\n\nGenuinely unpleasant to look at, touch, and think about.");

            gun.SetupSprite(null, "uterinepolyp_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);


            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.5f;
            gun.DefaultModule.cooldownTime = 0.2f;
            gun.DefaultModule.numberOfShotsInClip = 3;
            gun.barrelOffset.transform.localPosition = new Vector3(1.25f, 0.43f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.transform.parent = gun.barrelOffset;
            projectile.baseData.speed *= 0.9f;
            projectile.baseData.damage = 15;
            projectile.SetProjectileSpriteRight("uterinepolyp_projectile", 7, 7, true, tk2dBaseSprite.Anchor.MiddleCenter, 6, 6);
           GoopModifier gooper = projectile.gameObject.AddComponent<GoopModifier>();
            gooper.SpawnGoopInFlight = false;
            gooper.SpawnGoopOnCollision = true;
            gooper.CollisionSpawnRadius = 1;
            gooper.goopDefinition = EasyGoopDefinitions.GenerateBloodGoop(10, ExtendedColours.orange, 10);

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("UterinePolyp Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/uterinepolyp_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/uterinepolyp_clipempty");

            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            UterinePolypID = gun.PickupObjectId;
        }        
        public static int UterinePolypID;
        public UterinePolyp()
        {

        }
    }
}