using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using Alexandria.ItemAPI;
using UnityEngine;
using SaveAPI;
namespace NevernamedsItems
{

    public class Boltcaster : AdvancedGunBehavior
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Boltcaster", "nnboltcaster");

            Game.Items.Rename("outdated_gun_mods:boltcaster", "nn:boltcaster");
        var behav =    gun.gameObject.AddComponent<Boltcaster>();
            gun.SetShortDescription("It Belongs In A Museum");
            gun.SetLongDescription("An old relic of one of the many rebel groups that have risen up against- and inevitably broken against the unshakeable Hegemony of Man."+"\n\nThe magnetic mass driver orbs give it the appearance of a crossbow.");
            behav.preventNormalFireAudio = true;
            behav.overrideNormalFireAudio = "Play_WPN_plasmarifle_shot_01";

            gun.SetupSprite(null, "nnboltcaster_idle_001", 8);
            gun.SetAnimationFPS(gun.shootAnimation, 16);
            gun.SetAnimationFPS(gun.reloadAnimation, 0);

            gun.muzzleFlashEffects = VFXToolbox.CreateVFXPool("Boltcaster Muzzleflash",
                new List<string>()
                {
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/Boltcaster/boltcaster_muzzleflash_001",
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/Boltcaster/boltcaster_muzzleflash_002",
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/Boltcaster/boltcaster_muzzleflash_003",
                },
                10, //FPS
                new IntVector2(49, 26), //Dimensions
                tk2dBaseSprite.Anchor.MiddleLeft, //Anchor
                false, //Uses a Z height off the ground
                0, //The Z height, if used
                false,
               VFXAlignment.Fixed
                  );



            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.1f;
            gun.DefaultModule.cooldownTime = 0.5f;
            gun.DefaultModule.numberOfShotsInClip = 1;
            gun.DefaultModule.angleVariance = 0f;
            gun.SetBaseMaxAmmo(100);
            gun.gunClass = GunClass.RIFLE;
            gun.barrelOffset.transform.localPosition = new Vector3(27f/16f, 7f / 16f, 0f);

            //BULLET STATS
            Projectile projectile = gun.DefaultModule.projectiles[0].gameObject.InstantiateAndFakeprefab().GetComponent<Projectile>();
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 30f;
            projectile.baseData.speed *= 3f;
            projectile.baseData.range *= 3f;
            projectile.SetProjectileSpriteRight("boltcaster_proj", 23, 3, true, tk2dBaseSprite.Anchor.MiddleCenter, 13, 3);
            projectile.hitEffects = (PickupObjectDatabase.GetById(543) as Gun).DefaultModule.projectiles[0].hitEffects;
            projectile.pierceMinorBreakables = true;
            projectile.baseData.force *= 4f;

            gun.quality = PickupObject.ItemQuality.B;

            ETGMod.Databases.Items.Add(gun, false, "ANY");

            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.BOSSRUSH_HUNTER, true);
            gun.SetTag("arrow_bolt_weapon");
        }
    }
}
