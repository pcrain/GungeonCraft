using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod;
using MonoMod.RuntimeDetour;
using ItemAPI;

namespace CwaffingTheGungy
{
    public class SoulKaliber : AdvancedGunBehavior
    {
        public static string gunName          = "Soul Kaliber";
        public static string spriteName       = "ringer";
        public static string projectileName   = "ak-47";
        public static string shortDescription = "Gundead or Alive";
        public static string longDescription  = "(hitting an enemy gives them the soul link status effect, making all soul linked enemies take damage when any enemy is hit)";

        public static void Add()
        {
            Gun gun = Lazy.InitGunFromStrings(gunName, spriteName, projectileName, shortDescription, longDescription);

            var comp = gun.gameObject.AddComponent<SoulKaliber>();
            comp.preventNormalFireAudio = true;

            gun.muzzleFlashEffects.type           = VFXPoolType.None;
            gun.gunSwitchGroup                    = (PickupObjectDatabase.GetById(198) as Gun).gunSwitchGroup;
            gun.DefaultModule.ammoCost            = 1;
            gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                        = 1.1f;
            gun.DefaultModule.cooldownTime        = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.quality                           = PickupObject.ItemQuality.D;
            gun.SetBaseMaxAmmo(250);
            gun.SetAnimationFPS(gun.shootAnimation, 24);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
            projectile.baseData.speed   = 30.0f;
            projectile.baseData.damage  = 1f;
            projectile.gameObject.AddComponent<SoulLinkProjectile>();
        }

    }
}
