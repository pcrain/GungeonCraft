using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{

    public class BackWarder : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Back Warder", "backwarder");
            Game.Items.Rename("outdated_gun_mods:back_warder", "nn:back_warder");
            gun.gameObject.AddComponent<BackWarder>();
            gun.SetShortDescription("Backup");
            gun.SetLongDescription("Fires backwards."+ "\n\nThe condition of shooting backwards (known medically as disgyrismata) affects as many as one in five Gungeoneers.");

            gun.SetupSprite(null, "backwarder_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 14);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(38) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.15f;
            gun.DefaultModule.angleFromAim = 180;
            gun.DefaultModule.angleVariance = 7;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 6;
            gun.barrelOffset.transform.localPosition = new Vector3(0.18f, 0.87f, 0f);
            gun.SetBaseMaxAmmo(140);
            gun.ammo = 140;
            gun.gunClass = GunClass.SHITTY;

            gun.quality = PickupObject.ItemQuality.D;
            ETGMod.Databases.Items.Add(gun, false, "ANY");
            BackWarderID = gun.PickupObjectId;

        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile.ProjectilePlayerOwner() && projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Backstabber!"))
            {
              ProjectileSlashingBehaviour slash =  projectile.gameObject.AddComponent<ProjectileSlashingBehaviour>();
                slash.DestroyBaseAfterFirstSlash = false;
                slash.timeBetweenSlashes = 1;
                slash.SlashDamageUsesBaseProjectileDamage = true;
                slash.slashParameters.playerKnockbackForce = 0;
            }
            base.PostProcessProjectile(projectile);
        }
        public static int BackWarderID;      
        public BackWarder()
        {

        }
    }
}