using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{

    public class RC360 : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("RC-360", "rc360");
            Game.Items.Rename("outdated_gun_mods:rc360", "nn:rc_360");
            gun.gameObject.AddComponent<RC360>();
            gun.SetShortDescription("Sentry Gun");
            gun.SetLongDescription("The final bullet in the clip of this strange gun is actually an entirely self-contained sentry-robot.");

            gun.SetupSprite(null, "rc360_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);
            gun.SetAnimationFPS(gun.reloadAnimation, 30);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(80) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.5f;
            gun.DefaultModule.angleVariance = 10;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 20;
            gun.barrelOffset.transform.localPosition = new Vector3(2.18f, 0.93f, 0f);
            gun.SetBaseMaxAmmo(500);
            gun.gunClass = GunClass.FULLAUTO;
            gun.DefaultModule.usesOptionalFinalProjectile = true;
            gun.DefaultModule.numberOfFinalProjectiles = 1;

            //BULLET STATS
            Projectile regProj = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            regProj.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(regProj.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(regProj);
            regProj.baseData.damage = 4.5f;
            gun.DefaultModule.projectiles[0] = regProj;           


            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.speed *= 0.1f;
            projectile.baseData.damage = 25f;
            projectile.SetProjectileSpriteRight("goopyproj_001", 16, 16, true, tk2dBaseSprite.Anchor.MiddleCenter, 14, 14);
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.YellowLaserCircleVFX;
            projectile.gameObject.AddComponent<RC360TurretShot>();
            projectile.pierceMinorBreakables = true;
            gun.DefaultModule.finalProjectile = projectile;

            gun.quality = PickupObject.ItemQuality.B; 
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            RC360ID = gun.PickupObjectId;
        }
        public static int RC360ID;
        
        public RC360()
        {

        }
    }
    public class RC360TurretShot : MonoBehaviour
    {
        public RC360TurretShot()
        {

        }
        private void Start()
        {
            self = base.GetComponent<Projectile>();
            if (self.ProjectilePlayerOwner()) owner = self.ProjectilePlayerOwner();
            if (owner)
            {
                owner.PostProcessProjectile += this.PostProcess;
            }
        }
        private void OnDestroy()
        {
            if (owner)
            {
                owner.PostProcessProjectile -= this.PostProcess;
            }
        }
        private void PostProcess(Projectile bulletToClone, float t)
        {
            self.StartCoroutine(DelayedPostProcess(bulletToClone));
        }
        private IEnumerator DelayedPostProcess(Projectile bulletToClone)
        {
            yield return null;
            if (bulletToClone.GetComponent<RC360TurretShot>() != null) yield break;
            if (self.PossibleSourceGun == null || bulletToClone.PossibleSourceGun == null) yield break;
            if (bulletToClone.PossibleSourceGun == self.PossibleSourceGun)
            {
                float angle = bulletToClone.Direction.ToAngle();
                if (owner.PlayerHasActiveSynergy("I Call Hacks")) angle = self.specRigidbody.UnitCenter.GetVectorToNearestEnemy().ToAngle();
                GameObject newBulletOBJ = FakePrefab.Clone(bulletToClone.gameObject);
                GameObject spawnedBulletOBJ = SpawnManager.SpawnProjectile(newBulletOBJ, self.sprite.WorldCenter, Quaternion.Euler(0f, 0f, angle), true);
                Projectile component = spawnedBulletOBJ.GetComponent<Projectile>();
                if (component != null)
                {
                    component.Owner = owner;
                    component.Shooter = owner.specRigidbody;
                }
            }
            yield break;
        }
        private Projectile self;
        private PlayerController owner;
    }
}
