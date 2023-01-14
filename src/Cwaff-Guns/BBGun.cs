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

        private static Projectile fakeProjectile;

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

            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Charged;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Ordered;

            //GUN STATS
            ProjectileModule mod = gun.DefaultModule;
                mod.ammoCost            = 1;
                mod.shootStyle          = ProjectileModule.ShootStyle.Charged;
                mod.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Ordered;
                mod.cooldownTime        = 0.70f;
                mod.angleVariance       = 20f;
                mod.numberOfShotsInClip = 3;

            List<ProjectileModule.ChargeProjectile> tempChargeProjectiles =
                new List<ProjectileModule.ChargeProjectile>();

            for (int i = 0; i < CHARGE_LEVELS.Length; i++)
            {
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                if (i < mod.projectiles.Count)
                    mod.projectiles[i] = projectile;
                else
                    mod.projectiles.Add(projectile);
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);

                TheBB bb = projectile.gameObject.AddComponent<TheBB>();
                bb.chargeLevel = i+1;

                // if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                ProjectileModule.ChargeProjectile chargeProj = new ProjectileModule.ChargeProjectile
                {
                    Projectile = projectile,
                    ChargeTime = CHARGE_LEVELS[i],
                };
                tempChargeProjectiles.Add(chargeProj);
            }
            mod.chargeProjectiles = tempChargeProjectiles;
            gun.reloadTime = 1f;
            gun.SetBaseMaxAmmo(100);

            gun.quality = PickupObject.ItemQuality.B;

            fakeProjectile = Lazy.PrefabProjectileFromGun(gun);
            fakeProjectile.gameObject.AddComponent<FakeProjectileComponent>();
        }

        public override void PostProcessProjectile(Projectile projectile)
        {
            ETGModConsole.Log("for speed, last charge was "+lastCharge);
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
            ETGModConsole.Log("created projectile with charge level "+chargeLevel);
            // Projectile self = base.GetComponent<Projectile>();
            // if (self?.Owner is PlayerController)
            // {
            //     PlayerController owner = self.Owner as PlayerController;
            //     self.RuntimeUpdateScale(NATASHA_PROJECTILE_SCALE * owner.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale));
            // }
        }
    }
}
