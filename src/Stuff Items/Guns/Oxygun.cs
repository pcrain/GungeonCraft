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
    public class Oxygun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Oxygun", "oxygun");
            Game.Items.Rename("outdated_gun_mods:oxygun", "nn:oxygun");
            gun.gameObject.AddComponent<Oxygun>();
            gun.SetShortDescription("Bullets Not Included");
            gun.SetLongDescription("This standard-issue colony multi-tool seems to be stuck on the 'offensive' setting." + "\n\nUpon finding it, there seemed to be no shots left inside.");

            gun.SetupSprite(null, "oxygun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.8f;
            gun.DefaultModule.cooldownTime = 0.65f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 5;
            gun.barrelOffset.transform.localPosition = new Vector3(1.93f, 0.62f, 0f);
            gun.SetBaseMaxAmmo(300);
            gun.ammo = 0;
            gun.gunClass = GunClass.SILLY;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 2f;
            projectile.baseData.speed *= 0.4f;
            projectile.baseData.range *= 10f;

            HomingModifier homing = projectile.gameObject.AddComponent<HomingModifier>();
            homing.AngularVelocity = 120f;
            homing.HomingRadius = 1000f;

            projectile.SetProjectileSpriteRight("oxygun_projectile", 17, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 17, 7);

            projectile.transform.parent = gun.barrelOffset;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "Repetitron Bullets";

            gun.quality = PickupObject.ItemQuality.C;
            gun.encounterTrackable.EncounterGuid = "this is the Oxygun";
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            OxygunID = gun.PickupObjectId;
        }
        public static int OxygunID;
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            gun.PreventNormalFireAudio = true;
            AkSoundEngine.PostEvent("Play_WPN_stdissuelaser_shot_01", gameObject);
        }      
        public Oxygun()
        {

        }
    }
}