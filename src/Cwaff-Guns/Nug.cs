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
    public class Nug : AdvancedGunBehavior
    {
        public static string GunName          = "Nug";
        public static string SpriteName       = "flayedrevolver";
        public static string ProjectileName   = "magnum"; //38
        public static string ShortDescription = "Noegnug Eht Retne";
        public static string LongDescription  = "(everything's backwards D:)";

        public static Projectile gunprojectile;
        public static Projectile fakeprojectile;

        private int oldammo = 1;

        public static void Add()
        {
            Gun gun = Lazy.InitGunFromStrings(GunName, SpriteName, ProjectileName, ShortDescription, LongDescription);

            var comp = gun.gameObject.AddComponent<Nug>();
            comp.preventNormalFireAudio = true;

            gun.muzzleFlashEffects.type           = VFXPoolType.None;
            gun.gunSwitchGroup                    = (PickupObjectDatabase.GetById(198) as Gun).gunSwitchGroup;
            gun.DefaultModule.ammoCost            = 0;
            gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                        = 1.1f;
            gun.DefaultModule.cooldownTime        = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.quality                           = PickupObject.ItemQuality.D;
            gun.SetBaseMaxAmmo(250);
            gun.SetAnimationFPS(gun.shootAnimation, 24);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
            projectile.baseData.speed   = 200.0f;
            Projectile projectile2 = Lazy.PrefabProjectileFromGun(gun, false);
            projectile2.baseData.speed   = 20.0f;
            Projectile projectile3 = Lazy.PrefabProjectileFromGun(PickupObjectDatabase.GetById(38) as Gun, false);
            projectile3.baseData.speed = 0.0f;

            // Ordering is important here, we don't want the secondary projectile to have the NugBehavior
            projectile.gameObject.AddComponent<NugBehavior>();
            projectile.gameObject.AddComponent<FakeProjectileComponent>();
            projectile2.gameObject.AddComponent<NugRedBehavior>();
            projectile3.gameObject.AddComponent<FakeProjectileComponent>();

            gunprojectile = projectile2;
            fakeprojectile = projectile3;
        }

        public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
        {
            if (gun.ClipShotsRemaining < gun.DefaultModule.numberOfShotsInClip)
            {
              if (gun.CurrentAmmo < gun.AdjustedMaxAmmo)
              {
                gun.CurrentAmmo += 1;
                gun.ClipShotsRemaining += 2;
                return projectile;
              }
            }
            gun.ClipShotsRemaining += 1;
            return fakeprojectile;
        }

        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            base.OnPickedUpByPlayer(player);
            if (!everPickedUpByPlayer)
            {
                if (gun)
                {
                    gun.ClipShotsRemaining = 1;
                    gun.CurrentAmmo = 1;
                }
            }
        }

        public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
        {
            if (!manualReload || (gun.ClipShotsRemaining < gun.DefaultModule.numberOfShotsInClip))
                return;
            gun.ClipShotsRemaining = 1;
            gun.CurrentAmmo = 1;
            AkSoundEngine.PostEvent("Play_WPN_crossbow_reload_01", gameObject);
            base.OnReloadPressed(player, gun, manualReload);
        }
    }

    public class NugRedBehavior : MonoBehaviour
    {
        private Projectile m_projectile;

        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_projectile.AdjustPlayerProjectileTint(Color.red, 2);
            AkSoundEngine.PostEvent("Play_WPN_smileyrevolver_shot_01", this.m_projectile.gameObject);
        }
    }

    public class NugBehavior : MonoBehaviour
    {
        private Projectile m_projectile;
        private PlayerController m_owner;

        public float targetAngle;

        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (this.m_projectile.Owner && this.m_projectile.Owner is PlayerController)
            {
                this.m_owner      = this.m_projectile.Owner as PlayerController;
                this.targetAngle  = this.m_owner.CurrentGun.CurrentAngle + 180;
            }

            SpeculativeRigidbody specRigidBody = this.m_projectile.specRigidbody;
            this.m_projectile.BulletScriptSettings.surviveTileCollisions = true;
            specRigidBody.OnCollision += this.OnCollision;
        }

        private void OnCollision(CollisionData tileCollision)
        {
            this.m_projectile.baseData.speed *= 0f;
            this.m_projectile.UpdateSpeed();
            float m_hitNormal = tileCollision.Normal.ToAngle();
            PhysicsEngine.PostSliceVelocity = new Vector2?(default(Vector2));
            SpeculativeRigidbody specRigidbody = this.m_projectile.specRigidbody;
            specRigidbody.OnCollision -= this.OnCollision;

            Vector2 spawnPoint = tileCollision.PostCollisionUnitCenter;
            GameObject spawn = SpawnManager.SpawnProjectile(
                Nug.gunprojectile.gameObject,
                spawnPoint,
                Quaternion.Euler(0f, 0f, this.targetAngle),
                true);

            // Hack to adjust the tint of a non-owned projectile
            Projectile proj = spawn.GetComponent<Projectile>();
            if (proj)
            {
                proj.Owner = this.m_owner;
                proj.AdjustPlayerProjectileTint(Color.red, 1);
                proj.Owner = null;
                proj.BulletScriptSettings.surviveTileCollisions = true;
                BulletLifeTimer timer = proj.gameObject.GetOrAddComponent<BulletLifeTimer>();
                timer.secondsTillDeath = 3;
            }

            this.m_projectile.DieInAir();
        }
    }
}
