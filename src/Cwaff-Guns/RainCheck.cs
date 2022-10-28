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
    public class RainCheck : AdvancedGunBehavior
    {
        public static string gunName          = "Rain Check";
        public static string spriteName       = "eldermagnum2";
        public static string projectileName   = "ak-47";
        public static string shortDescription = "For a Rainy Day";
        public static string longDescription  = "(Upon firing, bullets are delayed from moving until reloading, then move towards player. Switching away from this gun keeps bullets in stasis until switching back to this gun.)";

        public static void Add()
        {
            Gun gun = Lazy.InitGunFromStrings(gunName, spriteName, projectileName, shortDescription, longDescription);
            var comp = gun.gameObject.AddComponent<RainCheck>();

            gun.gunSwitchGroup                    = (PickupObjectDatabase.GetById(198) as Gun).gunSwitchGroup;
            gun.DefaultModule.ammoCost            = 1;
            gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                        = 1.1f;
            gun.DefaultModule.cooldownTime        = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 20;
            gun.quality                           = PickupObject.ItemQuality.D;
            gun.SetBaseMaxAmmo(250);
            gun.SetAnimationFPS(gun.shootAnimation, 24);

            Projectile projectile       = Lazy.PrefabProjectileFromGun(gun);
            projectile.baseData.damage  = 5f;
            projectile.baseData.speed   = 20.0f;
            projectile.transform.parent = gun.barrelOffset;

            projectile.gameObject.AddComponent<RainCheckBullets>();
        }

        public override void OnPostFired(PlayerController player, Gun gun)
        {
            base.OnPostFired(player, gun);
        }

        protected override void Update()
        {
            base.Update();
        }

        public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
        {
            base.OnReloadPressed(player, gun, manualReload);
        }

        public override void OnReload(PlayerController player, Gun gun)
        {
            base.OnReload(player, gun);
            LaunchAllBullets();
        }

        public override void OnSwitchedToThisGun()
        {
            base.OnSwitchedToThisGun();
            LaunchAllBullets();
        }

        public override void OnSwitchedAwayFromThisGun()
        {
            base.OnSwitchedAwayFromThisGun();
            PutAllBulletsInStasis();
        }


        private void LaunchAllBullets()
        {
            int num_found = 0;
            for (int i = 0; i < StaticReferenceManager.AllProjectiles.Count; i++)
            {
                Projectile projectile = StaticReferenceManager.AllProjectiles[i];
                if (projectile && projectile.Owner == gun.CurrentOwner && projectile.GetComponent<RainCheckBullets>())
                {
                    projectile.GetComponent<RainCheckBullets>().ForceMove(++num_found);
                }
            }
        }

        private void PutAllBulletsInStasis()
        {
            for (int i = 0; i < StaticReferenceManager.AllProjectiles.Count; i++)
            {
                Projectile projectile = StaticReferenceManager.AllProjectiles[i];
                if (projectile && projectile.Owner == gun.CurrentOwner && projectile.GetComponent<RainCheckBullets>())
                {
                    projectile.GetComponent<RainCheckBullets>().PutInStasis();
                }
            }
        }
    }

    public class RainCheckBullets : MonoBehaviour
    {
        private const float RAINCHECK_MAX_TIMEOUT  = 10f;
        private const float RAINCHECK_LAUNCH_DELAY = 0.025f;

        private Projectile self;
        private PlayerController owner;
        private float initialSpeed;
        private float moveTimer;
        private bool launchSequenceStarted;
        private bool inStasis;
        private bool wasEverInStasis;
        private void Start()
        {
            this.self                  = base.GetComponent<Projectile>();
            this.owner                 = self.ProjectilePlayerOwner();
            this.initialSpeed          = self.baseData.speed;
            this.moveTimer             = RAINCHECK_MAX_TIMEOUT;
            this.launchSequenceStarted = false;
            this.inStasis              = false;
            this.wasEverInStasis       = false;

            self.baseData.speed = 0.1f;
            self.UpdateSpeed();

            // Reset the timers of all of our other RainCheckBullets, with a small delay
            int numRainProjectiles = 0;
            for (int i = 0; i < StaticReferenceManager.AllProjectiles.Count; i++)
            {
                Projectile projectile = StaticReferenceManager.AllProjectiles[i];
                if (projectile && projectile.Owner == self.Owner)
                {
                    var p = projectile.GetComponent<RainCheckBullets>();
                    if (p && !p.launchSequenceStarted)
                    {
                        p.moveTimer =
                            RAINCHECK_MAX_TIMEOUT - RAINCHECK_LAUNCH_DELAY * numRainProjectiles;
                        ++numRainProjectiles;
                    }
                }
            }

            StartCoroutine(DoSpeedChange());
        }

        private IEnumerator DoSpeedChange()
        {
            while (this.inStasis || this.moveTimer > 0)
            {
                this.moveTimer -= BraveTime.DeltaTime;
                if (!self) break;
                yield return null;
            }
            this.launchSequenceStarted = true;
            self.baseData.speed        = this.initialSpeed;
            if (this.owner)
            {
                Vector2 dirToPlayer = self.sprite.WorldCenter.CalculateVectorBetween(this.owner.sprite.WorldCenter);
                self.SendInDirection(dirToPlayer, true);
            }
            self.UpdateSpeed();
        }

        public void ForceMove(int index)
        {
            if (!this.launchSequenceStarted)
            {  //no resetting our timers after this function has been called once
                this.launchSequenceStarted = true;
                this.moveTimer             = index * RAINCHECK_LAUNCH_DELAY;
                this.inStasis              = false;
            }
        }

        public void PutInStasis()
        {
            if (!this.wasEverInStasis)
            {
                this.inStasis        = true;
                this.wasEverInStasis = true;
            }
        }
    }
}
