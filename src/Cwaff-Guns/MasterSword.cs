using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;

using Gungeon;
using ItemAPI;

namespace CwaffingTheGungy
{
    public class MasterSword : AdvancedGunBehavior
    {
        public static string gunName          = "Master Sword";
        public static string spriteName       = "carnwennan";
        public static string projectileName   = "86"; //marine sidearm
        public static string shortDescription = "Dangerous Alone";
        public static string longDescription  = "(shoots beams until you get hit, stylish hat)";

        public static void Add()
        {
            Gun gun = Lazy.InitGunFromStrings(gunName, spriteName, projectileName, shortDescription, longDescription);
            var comp = gun.gameObject.AddComponent<MasterSword>();

            gun.DefaultModule.ammoCost               = 1;
            gun.DefaultModule.shootStyle             = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle          = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                           = 1.05f;
            gun.DefaultModule.cooldownTime           = 0.3f;
            gun.muzzleFlashEffects.type              = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip    = 5;
            gun.barrelOffset.transform.localPosition = new Vector3(9f / 16f, 4f / 16f, 0f);
            gun.quality                              = PickupObject.ItemQuality.D;
            gun.gunClass                             = GunClass.SILLY;
            gun.DefaultModule.ammoType               = GameUIAmmoType.AmmoType.BEAM;
            gun.gunSwitchGroup                       = (PickupObjectDatabase.GetById(417) as Gun).gunSwitchGroup;
            gun.InfiniteAmmo                         = true;
            gun.SetAnimationFPS(gun.shootAnimation, 12);

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

            tk2dSpriteAnimationClip reloadClip = gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.reloadAnimation);
            foreach (tk2dSpriteAnimationFrame frame in reloadClip.frames)
            {
                tk2dSpriteDefinition def = frame.spriteCollection.spriteDefinitions[frame.spriteId];
                def?.MakeOffset(new Vector2(-0.81f, -2.18f));
            }
            tk2dSpriteAnimationClip fireClip = gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation);
            foreach (tk2dSpriteAnimationFrame frame in fireClip.frames)
            {
                tk2dSpriteDefinition def = frame.spriteCollection.spriteDefinitions[frame.spriteId];
                def?.MakeOffset(new Vector2(-0.81f, -2.18f));
            }
        }
    }
}
