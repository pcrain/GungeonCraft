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
    public class StasisRifle : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Stasis Rifle", "stasisrifle");
            Game.Items.Rename("outdated_gun_mods:stasis_rifle", "nn:stasis_rifle");
            var behav = gun.gameObject.AddComponent<StasisRifle>();
            gun.SetShortDescription("Hold Position");
            gun.SetLongDescription("Fires dangerous particle bolts that freeze time in a small radius around the impact site."+"\n\nWorks underwater.");

            gun.SetupSprite(null, "stasisrifle_idle_001", 8);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.doesScreenShake = true;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Charged;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 2f;
            gun.DefaultModule.angleVariance = 0;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(153) as Gun).muzzleFlashEffects;

            gun.DefaultModule.numberOfShotsInClip = 1;
            gun.barrelOffset.transform.localPosition = new Vector3(2.68f, 0.56f, 0f);
            gun.SetBaseMaxAmmo(35);
            gun.ammo = 35;
            gun.gunClass = GunClass.CHARGE;
            gun.SetAnimationFPS(gun.chargeAnimation, 8);
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).loopStart = 22;

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].eventAudio = "Play_WPN_energycannon_shot";
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].triggerEvent = true;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 17f;
            projectile.baseData.force *= 0.5f;
            projectile.baseData.speed *= 2f;
            projectile.baseData.range *= 2f;
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.SmoothLightBlueLaserCircleVFX;
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.SetProjectileSpriteRight("highvelocityrifle_projectile", 13, 5, true, tk2dBaseSprite.Anchor.MiddleCenter, 11, 4);
            FreezeTimeOnHitModifier freezy = projectile.gameObject.GetOrAddComponent<FreezeTimeOnHitModifier>();
            ProjectileModule.ChargeProjectile chargeProj = new ProjectileModule.ChargeProjectile
            {
                Projectile = projectile,
                ChargeTime = 3,
            };
            gun.DefaultModule.chargeProjectiles = new List<ProjectileModule.ChargeProjectile> { chargeProj };

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "Toolgun Bullets";

            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
        }
        public StasisRifle()
        {

        }
        
    }
    public class FreezeTimeOnHitModifier : MonoBehaviour
    {
        public FreezeTimeOnHitModifier()
        {
            this.radius = 4;
            this.lengthOfEffect = 7;
            this.timeMultiplier = 0.00001f;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_projectile.OnDestruction += this.Destruction;
        }
        private void Destruction(Projectile self)
        {
            var timeSlow = new RadialSlowInterface();
            timeSlow.DoesSepia = false;
            timeSlow.EffectRadius = this.radius;
            timeSlow.RadialSlowHoldTime = this.lengthOfEffect;
            timeSlow.RadialSlowTimeModifier = this.timeMultiplier;
            timeSlow.UpdatesForNewEnemies = true;
            timeSlow.DoRadialSlow(self.specRigidbody.UnitCenter, self.transform.position.GetAbsoluteRoom());
            MagicCircleDoer.DoMagicCircle(self.specRigidbody.UnitCenter, this.radius, lengthOfEffect, ExtendedColours.freezeBlue, false);
        }
        private Projectile m_projectile;
        public float radius;
        public float timeMultiplier;
        public float lengthOfEffect;
    }
}
