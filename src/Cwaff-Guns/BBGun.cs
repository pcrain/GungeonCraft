using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod;
using MonoMod.RuntimeDetour;
using Gungeon;
using Alexandria.Misc;
using Alexandria.ItemAPI;

namespace CwaffingTheGungy
{
    public class BBGun : AdvancedGunBehavior
    {
        public static string gunName          = "B. B. Gun";
        public static string spriteName       = "embercannon";
        public static string projectileName   = "83";
        public static string shortDescription = "Spare No One";
        public static string longDescription  = "(Three Strikes)";

        private float lastCharge = 0.0f;

        private static readonly float[] CHARGE_LEVELS = {0.5f,1.0f,2.0f};

        public static void Add()
        {
            Gun gun = Lazy.InitGunFromStrings(gunName, spriteName, projectileName, shortDescription, longDescription);
            var comp = gun.gameObject.AddComponent<BBGun>();

            comp.preventNormalFireAudio = true;
            comp.preventNormalReloadAudio = true;
            comp.overrideNormalReloadAudio = "Play_ENM_flame_veil_01";

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].eventAudio = "Play_WPN_seriouscannon_shot_01";
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].triggerEvent = true;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).loopStart = 2;

            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(37) as Gun).muzzleFlashEffects;
            gun.barrelOffset.transform.localPosition = new Vector3(1.93f, 0.87f, 0f);
            gun.SetAnimationFPS(gun.shootAnimation, 10);
            gun.SetAnimationFPS(gun.chargeAnimation, 8);
            gun.gunClass = GunClass.CHARGE;

            Gun projectileBase = PickupObjectDatabase.GetById(83) as Gun;
            // Lazy.InitGunFromStrings already handles the first one
            for (int i = 1; i < CHARGE_LEVELS.Length; i++)
                gun.AddProjectileModuleFrom(projectileBase, true, false);

            //GUN STATS
            int n = 0;
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                ++n;
                mod.ammoCost            = 1;
                mod.shootStyle          = ProjectileModule.ShootStyle.Charged;
                mod.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Ordered;
                mod.cooldownTime        = 0.70f;
                mod.angleVariance       = 20f;
                mod.numberOfShotsInClip = 3;

                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);

                Expiration expire = projectile.gameObject.AddComponent<Expiration>();
                TheBB bb = projectile.gameObject.AddComponent<TheBB>();
                bb.chargeLevel = n;

                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                ProjectileModule.ChargeProjectile chargeProj = new ProjectileModule.ChargeProjectile
                {
                    Projectile = projectile,
                    ChargeTime = CHARGE_LEVELS[n-1],
                };
                mod.chargeProjectiles = new List<ProjectileModule.ChargeProjectile> { chargeProj };
            }
            gun.reloadTime = 1f;
            gun.SetBaseMaxAmmo(100);

            gun.quality = PickupObject.ItemQuality.B;
        }

        public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
        {
            // ETGModConsole.Log("charge is "+this.gun.GetChargeFraction());
            // ETGModConsole.Log("last charge was "+lastCharge);

            TheBB bb = projectile.gameObject.GetComponent<TheBB>();
            // ETGModConsole.Log("  attempting to create projectile with level "+bb.chargeLevel);

            if (bb.chargeLevel == CHARGE_LEVELS.Length)
            {
                // ETGModConsole.Log("    creating fully charged projectile");
                return base.OnPreFireProjectileModifier(gun,projectile,mod); //if we're the final projectile, there's nothing to do
            }
            // determine how much charge we need to create the next projectile on a scale of 0 to 1
            float nextCharge = CHARGE_LEVELS[bb.chargeLevel] / CHARGE_LEVELS[CHARGE_LEVELS.Length - 1];
            if (nextCharge > lastCharge) // if we're not able to create the next level projectile yet, there's nothing to do
            {
                // ETGModConsole.Log("    creating projectile with level "+bb.chargeLevel+"/"+CHARGE_LEVELS.Length);
                return base.OnPreFireProjectileModifier(gun,projectile,mod);
            }

            // another stronger charged projectile is available, so get rid of this one
            projectile.gameObject.AddComponent<FakeProjectileComponent>();

            return base.OnPreFireProjectileModifier(gun,projectile,mod);
        }

        public override void PostProcessProjectile(Projectile projectile)
        {
            projectile.baseData.speed *= lastCharge;
            base.PostProcessProjectile(projectile);
        }

        protected override void Update()
        {
            base.Update();
            if (!(this.gun.CurrentOwner && this.gun.CurrentOwner is PlayerController))
                return;
            if (this.gun.IsCharging)
                lastCharge = this.gun.GetChargeFraction();
            // p.CurrentGun.charge
            // ETGModConsole.Log("charge is "+this.gun.GetChargeFraction());
        }
    }

    public class TheBB : MonoBehaviour
    {
        public int chargeLevel = 0;
        private void Start()
        {
            // Projectile self = base.GetComponent<Projectile>();
            // if (self?.Owner is PlayerController)
            // {
            //     PlayerController owner = self.Owner as PlayerController;
            //     self.RuntimeUpdateScale(NATASHA_PROJECTILE_SCALE * owner.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale));
            // }
        }
    }
}
