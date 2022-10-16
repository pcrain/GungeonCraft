using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public class Welrod : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Welrod", "welrod");
            Game.Items.Rename("outdated_gun_mods:welrod", "nn:welrod");
          var behav =  gun.gameObject.AddComponent<Welrod>();
            behav.overrideNormalFireAudio = "Play_WPN_SAA_impact_01";
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("Quiet, Quiet");
            gun.SetLongDescription("Designed for stealth assasination missions behind enemy lines, the Welrod is quiet and efficient.");

            gun.SetupSprite(null, "welrod_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.2f;
            //gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 4;
            gun.barrelOffset.transform.localPosition = new Vector3(1.18f, 0.56f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 15f;
            projectile.baseData.speed *= 0.5f;
            projectile.baseData.range *= 10f;

            projectile.pierceMinorBreakables = true;
            //projectile.shouldRotate = true;
            PierceProjModifier orAddComponent = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            orAddComponent.penetratesBreakables = true;
            orAddComponent.penetration = 1;

            projectile.SetProjectileSpriteRight("welrod_projectile", 7, 5, true, tk2dBaseSprite.Anchor.MiddleCenter, 6, 4);

            projectile.transform.parent = gun.barrelOffset;


            gun.quality = PickupObject.ItemQuality.D;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            WelrodID = gun.PickupObjectId;
        }
        public static int WelrodID;       
        public Welrod()
        {

        }
    }
}
