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
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class Robogun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Robogun", "robogun");
            Game.Items.Rename("outdated_gun_mods:robogun", "nn:robogun");
            var behav = gun.gameObject.AddComponent<Robogun>();

            gun.SetShortDescription("Sapper");
            gun.SetLongDescription("The projectiles of this remarkable weapon suck the speed right out of enemy bullets!" + "\n\nReverse engineered from vampires.");

            gun.SetupSprite(null, "robogun_idle_001", 8);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(58) as Gun).gunSwitchGroup;

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.SetAnimationFPS(gun.reloadAnimation, 0);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.15f;
            gun.DefaultModule.cooldownTime = 0.15f;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(58) as Gun).muzzleFlashEffects;
            gun.DefaultModule.numberOfShotsInClip = 5;
            gun.barrelOffset.transform.localPosition = new Vector3(26f / 16f, 9f  / 16f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.gunClass = GunClass.PISTOL;

            //BULLET STATS
            Projectile projectile = gun.DefaultModule.projectiles[0].InstantiateAndFakeprefab();
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 8f;
            projectile.baseData.speed *= 1.1f;
            projectile.baseData.range *= 10f;
            SelfReAimBehaviour reaim = projectile.gameObject.GetOrAddComponent<SelfReAimBehaviour>();
            reaim.trigger = SelfReAimBehaviour.ReAimTrigger.IMMEDIATE;

            projectile.pierceMinorBreakables = true;

            PierceProjModifier orAddComponent = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            orAddComponent.penetratesBreakables = true;
            orAddComponent.penetration = 1;

            projectile.SetProjectileSpriteRight("robogun_proj", 17, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 17, 7);
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.RedLaserCircleVFX;

            gun.quality = PickupObject.ItemQuality.B;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.MEDIUM_BLASTER;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            ID = gun.PickupObjectId;
        }
        public static int ID;
    }
}
