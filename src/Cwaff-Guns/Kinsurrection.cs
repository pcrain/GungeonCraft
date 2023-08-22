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
    public class Kinsurrection : AdvancedGunBehavior
    {
        public static string GunName          = "Kinsurrection";
        public static string SpriteName       = "redblaster";
        public static string ProjectileName   = "ak-47";
        public static string ShortDescription = "Friendliest Fire";
        public static string LongDescription  = "(shoots bullet kin as projectiles, surviving with 1hp)";

        public static void Add()
        {
            Gun gun = Lazy.InitGunFromStrings(GunName, SpriteName, ProjectileName, ShortDescription, LongDescription);
            var comp = gun.gameObject.AddComponent<Kinsurrection>();

            gun.gunSwitchGroup                    = (PickupObjectDatabase.GetById(198) as Gun).gunSwitchGroup;
            gun.DefaultModule.ammoCost            = 1;
            gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                        = 1.1f;
            gun.DefaultModule.angleVariance       = 15.0f;
            gun.DefaultModule.cooldownTime        = 0.4f;
            gun.DefaultModule.numberOfShotsInClip = 1000;
            gun.quality                           = PickupObject.ItemQuality.D;
            gun.SetBaseMaxAmmo(1000);
            gun.SetAnimationFPS(gun.shootAnimation, 24);

            Projectile projectile       = Lazy.PrefabProjectileFromGun(gun);
            projectile.baseData.damage  = 30f;
            projectile.baseData.force   = 50f;
            projectile.baseData.speed   = 0.01f;
            projectile.transform.parent = gun.barrelOffset;

            projectile.PenetratesInternalWalls    = false;
            projectile.pierceMinorBreakables      = true;
            PierceProjModifier pierce             = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            pierce.penetration                    = 3;
            pierce.penetratesBreakables           = true;

            projectile.gameObject.AddComponent<FakeProjectileComponent>();
            projectile.gameObject.AddComponent<BulletKinLauncher>();
            projectile.gameObject.AddComponent<EnemyIsTheProjectileBehavior>();
        }

    }

    public class BulletKinLauncher : MonoBehaviour
    {
        private Projectile m_projectile;
        private PlayerController m_owner;
        private AIActor m_bullet_kin;
        private float m_angle;

        private static AIActor bulletkin =
            EnemyDatabase.GetOrLoadByGuid(EnemyGuidDatabase.Entries["bullet_kin"]);

        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (this.m_projectile.Owner && this.m_projectile.Owner is PlayerController)
            {
                this.m_owner = this.m_projectile.Owner as PlayerController;
                this.m_angle = this.m_owner.CurrentGun.CurrentAngle;
            }

            SpeculativeRigidbody specRigidBody = this.m_projectile.specRigidbody;

            Vector2 position = this.m_projectile.sprite.WorldCenter;

            this.m_bullet_kin = AIActor.Spawn(
                bulletkin, position, GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(
                    position.ToIntVector2()), true, AIActor.AwakenAnimationType.Spawn, true);
            this.m_bullet_kin.CollisionDamage            = 10f;
            this.m_bullet_kin.CollisionKnockbackStrength = 100f;
            this.m_bullet_kin.specRigidbody.RegisterTemporaryCollisionException(
                this.m_owner.specRigidbody, 0.05f);
            this.m_bullet_kin.specRigidbody.RegisterSpecificCollisionException(
                this.m_projectile.specRigidbody);
            this.m_bullet_kin.healthHaver.knockbackDoer.ApplyKnockback(
                BraveMathCollege.DegreesToVector(this.m_angle), 100f);

            EnemyIsTheProjectileBehavior comp =
                this.m_projectile.gameObject.GetComponent<EnemyIsTheProjectileBehavior>();
            comp.Initialize(this.m_bullet_kin);
        }

    }

    public class EnemyIsTheProjectileBehavior : MonoBehaviour
    {
        private AIActor target_enemy = null;

        private Projectile m_projectile;
        private PlayerController m_owner;
        private float m_angle;

        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (this.m_projectile.Owner && this.m_projectile.Owner is PlayerController)
            {
                this.m_owner = this.m_projectile.Owner as PlayerController;
                this.m_angle = this.m_owner.CurrentGun.CurrentAngle;
            }
        }

        private void Update()
        {
            if (this.target_enemy == null)
            {
                this.m_projectile.DieInAir();
                return;
            }
            this.m_projectile.specRigidbody.Position =
                new Position(this.target_enemy.sprite.WorldCenter);
        }

        public void Initialize(AIActor target)
        {
            this.target_enemy   = target;
        }
    }
}
