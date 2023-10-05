using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace CwaffingTheGungy
{
    public class TennisRocket : AdvancedGunBehavior
    {
        public static string ItemName         = "Tennis Rocket";
        public static string SpriteName       = "paddle";
        public static string ProjectileName   = "86"; //marine sidearm
        public static string ShortDescription = "TBD";
        public static string LongDescription  = "TBD";

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
            var comp = gun.gameObject.AddComponent<TennisRocket>();
                comp.SetFireAudio("racket_hit");

            gun.DefaultModule.ammoCost               = 1;
            gun.DefaultModule.shootStyle             = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle          = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                           = 0;
            gun.DefaultModule.cooldownTime           = 0.3f;
            gun.muzzleFlashEffects.type              = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip    = 5;
            gun.barrelOffset.transform.localPosition = new Vector3(1.0f, 1.875f, 0);
            gun.quality                              = PickupObject.ItemQuality.D;
            gun.gunClass                             = GunClass.SILLY;
            gun.gunSwitchGroup                       = (ItemHelper.Get(Items.Blasphemy) as Gun).gunSwitchGroup;
            gun.InfiniteAmmo                         = true;
            gun.SetAnimationFPS(gun.shootAnimation, 60);
            // gun.LoopAnimation(gun.shootAnimation, 9);
            gun.SetAnimationFPS(gun.idleAnimation, 24);
            gun.LoopAnimation(gun.idleAnimation, 0);

            Projectile projectile              = Lazy.PrefabProjectileFromGun(gun);
            projectile.baseData.damage         = 4f;
            projectile.baseData.speed          *= 1.2f;
            projectile.sprite.renderer.enabled = false;

            ProjectileSlashingBehaviour slash          = projectile.gameObject.AddComponent<ProjectileSlashingBehaviour>();
            slash.DestroyBaseAfterFirstSlash           = true;
            slash.slashParameters                      = new SlashData();
            slash.slashParameters.soundEvent           = null;
            slash.slashParameters.projInteractMode     = SlashDoer.ProjInteractMode.IGNORE;
            slash.slashParameters.playerKnockbackForce = 0;
            slash.SlashDamageUsesBaseProjectileDamage  = true;
            slash.slashParameters.enemyKnockbackForce  = 10;
            slash.slashParameters.doVFX                = false;
            slash.slashParameters.doHitVFX             = true;
            slash.slashParameters.slashRange           = 2f;

            // tk2dSpriteAnimationClip reloadClip = gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.reloadAnimation);
            // foreach (tk2dSpriteAnimationFrame frame in reloadClip.frames)
            // {
            //     tk2dSpriteDefinition def = frame.spriteCollection.spriteDefinitions[frame.spriteId];
            //     def?.MakeOffset(new Vector2(-0.81f, -2.18f));
            // }
            // tk2dSpriteAnimationClip fireClip = gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation);
            // foreach (tk2dSpriteAnimationFrame frame in fireClip.frames)
            // {
            //     tk2dSpriteDefinition def = frame.spriteCollection.spriteDefinitions[frame.spriteId];
            //     def?.MakeOffset(new Vector2(-0.81f, -2.18f));
            // }
        }
    }
}
