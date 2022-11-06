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
    public class HeadCannon : AdvancedGunBehavior
    {
        public static string gunName          = "Head Cannon";
        public static string spriteName       = "multiplicator";
        public static string projectileName   = "ak-47";
        public static string shortDescription = "Better Than One";
        public static string longDescription  = "(launches you :D)";

        public static void Add()
        {
            Gun gun = Lazy.InitGunFromStrings(gunName, spriteName, projectileName, shortDescription, longDescription);
            var comp = gun.gameObject.AddComponent<HeadCannon>();

            gun.gunSwitchGroup                    = (PickupObjectDatabase.GetById(198) as Gun).gunSwitchGroup;
            gun.DefaultModule.ammoCost            = 1;
            gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                        = 1.1f;
            gun.DefaultModule.angleVariance       = 15.0f;
            gun.DefaultModule.cooldownTime        = 0.75f;
            gun.DefaultModule.numberOfShotsInClip = 1000;
            gun.quality                           = PickupObject.ItemQuality.D;
            gun.SetBaseMaxAmmo(1000);
            gun.SetAnimationFPS(gun.shootAnimation, 24);

            Projectile projectile       = Lazy.PrefabProjectileFromGun(gun);
            projectile.baseData.damage  = 3f;
            projectile.baseData.speed   = 20.0f;
            projectile.transform.parent = gun.barrelOffset;

            projectile.gameObject.AddComponent<HeadCannonBullets>();
        }

    }

    public class HeadCannonBullets : MonoBehaviour
    {
        private Projectile m_projectile;
        private PlayerController m_owner;

        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (this.m_projectile.Owner && this.m_projectile.Owner is PlayerController)
            {
                this.m_owner      = this.m_projectile.Owner as PlayerController;
            }

            SpeculativeRigidbody specRigidBody = this.m_projectile.specRigidbody;
            // this.m_projectile.BulletScriptSettings.surviveTileCollisions = true;
            specRigidBody.OnCollision += this.OnCollision;
        }

        private void OnCollision(CollisionData tileCollision)
        {
            this.m_projectile.baseData.speed *= 0f;
            this.m_projectile.UpdateSpeed();
            float hitNormal = tileCollision.Normal.ToAngle();
            PhysicsEngine.PostSliceVelocity = new Vector2?(default(Vector2));
            SpeculativeRigidbody specRigidbody = this.m_projectile.specRigidbody;
            specRigidbody.OnCollision -= this.OnCollision;

            // Vector2 spawnPoint = this.m_projectile.specRigidbody.Position.GetPixelVector2();
            Vector2 spawnPoint = tileCollision.PostCollisionUnitCenter;
                // tileCollision.PostCollisionUnitCenter + Lazy.AngleToVector(hitNormal,2f);

            this.TeleportPlayerToPosition(this.m_owner, spawnPoint, hitNormal);

            this.m_projectile.DieInAir();
        }

        private void TeleportPlayerToPosition(PlayerController player, Vector2 position, float normal)
        {
            Vector2 playerPos = player.transform.position;
            // ETGModConsole.Log("Player at "+playerPos.x+","+playerPos.y);
            Vector2 targetPos = position;
            // ETGModConsole.Log("Target at "+targetPos.x+","+targetPos.y);
            Vector2 deltaPos = (targetPos - playerPos)/100.0f;
            // ETGModConsole.Log("Delta/100 is "+deltaPos.x+","+deltaPos.y);
            for (int i = 100; i > 0; --i)
            {
                player.transform.position = playerPos + i * deltaPos;
                player.specRigidbody.Reinitialize();
                if (!PhysicsEngine.Instance.OverlapCast(player.specRigidbody, null, true, false, null, null, false, null, null))
                    break;
            }

            // player.TeleportToPoint(position,false);
        }
    }
}
