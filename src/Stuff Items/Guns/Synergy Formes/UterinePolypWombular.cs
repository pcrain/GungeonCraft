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

    public class UterinePolypWombular : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Wombular Polyp", "wombularpolyp");
            Game.Items.Rename("outdated_gun_mods:wombular_polyp", "nn:uterine_polyp+wombular");
            var obj = gun.gameObject.AddComponent<UterinePolypWombular>();
            obj.preventNormalFireAudio = true;
            obj.preventNormalReloadAudio = true;
            obj.overrideNormalReloadAudio = "Play_OBJ_cauldron_splash_01";
            obj.overrideNormalFireAudio = "Play_ENM_cult_spew_01";
            gun.SetShortDescription("");
            gun.SetLongDescription("");

            gun.SetupSprite(null, "wombularpolyp_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 20);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);


            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.5f;
            gun.DefaultModule.cooldownTime = 0.25f;
            gun.DefaultModule.angleVariance = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = -1;
            gun.barrelOffset.transform.localPosition = new Vector3(2.0f, 0.75f, 0f);
            gun.SetBaseMaxAmmo(300);

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.transform.parent = gun.barrelOffset;
            projectile.baseData.speed *= 1.1f;
            projectile.pierceMinorBreakables = true;
            PierceProjModifier piercing = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            piercing.penetration++;
            projectile.baseData.damage = 15;
            projectile.SetProjectileSpriteRight("wombularpolyp_projectile", 7, 7, true, tk2dBaseSprite.Anchor.MiddleCenter, 6, 6);

            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            WombularPolypID = gun.PickupObjectId;
        }
        public static int WombularPolypID;
        public UterinePolypWombular()
        {

        }
    }
}