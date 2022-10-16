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
    public class RedBlaster : AdvancedGunBehavior
    {
        public static void Add()
        {

            Gun gun = ETGMod.Databases.Items.NewGun("Red Blaster", "redblaster");
            Game.Items.Rename("outdated_gun_mods:red_blaster", "nn:red_blaster");
            var behav = gun.gameObject.AddComponent<RedBlaster>();
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("King of Crimson");
            gun.SetLongDescription("Fires pure, concentrated red."+"\n\nInvented by a mad chromatologist deep in his underground lab...");

            gun.SetupSprite(null, "redblaster_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 8);
            gun.isAudioLoop = true;

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
          

            //GUN STATS
            gun.doesScreenShake = false;
            gun.DefaultModule.ammoCost = 4;
            gun.DefaultModule.angleVariance = 0;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Beam;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.cooldownTime = 0.001f;
            gun.DefaultModule.numberOfShotsInClip = -1;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "red_beam";
            gun.barrelOffset.transform.localPosition = new Vector3((14f / 16f), (6f / 16f), 0f);
            gun.SetBaseMaxAmmo(900);
            gun.ammo = 900;
            gun.gunClass = GunClass.BEAM;

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).loopStart = 1;
         
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(658) as Gun).Volley.projectiles[3].projectiles[0]);           
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 33f;
            TintingBeamModifier tint = projectile.gameObject.AddComponent<TintingBeamModifier>();
            tint.designatedSource = "RedBlaster";

            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.B; //D
            ETGMod.Databases.Items.Add(gun, null, "ANY");

        }
        public RedBlaster()
        {

        }
    }
}

