using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Gungeon;
using Dungeonator;
using SaveAPI;
using System.Collections;

namespace CwaffingTheGungy
{
    public class FakeProjectileComponent : MonoBehaviour
    {
        // dummy compponent
        private void Start()
        {
            Projectile p = base.GetComponent<Projectile>();
            p.sprite.renderer.enabled = false;
            p.damageTypes &= (~CoreDamageTypes.Electric);
        }
    }

    public class BulletLifeTimer : MonoBehaviour
    {
        public BulletLifeTimer()
        {
            this.secondsTillDeath = 1;
            this.eraseInsteadOfDie = false;
        }
        private void Start()
        {
            timer = secondsTillDeath;
            this.m_projectile = base.GetComponent<Projectile>();

        }
        private void FixedUpdate()
        {
            if (this.m_projectile != null)
            {
                if (timer > 0)
                {
                    timer -= BraveTime.DeltaTime;
                }
                if (timer <= 0)
                {
                    if (eraseInsteadOfDie) UnityEngine.Object.Destroy(this.m_projectile.gameObject);
                    else this.m_projectile.DieInAir();
                }
            }
        }
        public float secondsTillDeath;
        public bool eraseInsteadOfDie;
        private float timer;
        private Projectile m_projectile;
    }

    public class Raycast
    {
        private static bool ExcludeAllButWallsAndEnemiesFromRaycasting(SpeculativeRigidbody s)
        {
            if (s.GetComponent<PlayerController>() != null)
                return true; //true == exclude players
            if (s.GetComponent<Projectile>() != null)
                return true; //true == exclude projectiles
            if (s.GetComponent<MinorBreakable>() != null)
                return true; //true == exclude minor breakables
            if (s.GetComponent<MajorBreakable>() != null)
                return true; //true == exclude major breakables
            if (s.GetComponent<FlippableCover>() != null)
                return true; //true == exclude tables
            return false; //false == don't exclude
        }

        private static bool ExcludeAllButWallsFromRaycasting(SpeculativeRigidbody s)
        {
            // if (s.GetComponent<AIActor>() != null)
            //     return true; //true == exclude enemies
            // return ExcludeAllButWallsAndEnemiesFromRaycasting(s);

            // TODO: fails to collide with some unexpected things, including statue in starting rom
            if (s.PrimaryPixelCollider.IsTileCollider)
                return false;
            return true;
        }

        public static Vector2 ToNearestWallOrEnemyOrObject(Vector2 pos, float angle, float minDistance = 1)
        {
            RaycastResult hit;
            if (PhysicsEngine.Instance.Raycast(
              pos+Lazy.AngleToVector(angle,minDistance), Lazy.AngleToVector(angle), 200, out hit,
              rigidbodyExcluder: ExcludeAllButWallsAndEnemiesFromRaycasting))
                return hit.Contact;
            return pos+Lazy.AngleToVector(angle,minDistance);
        }

        public static Vector2 ToNearestWallOrObject(Vector2 pos, float angle, float minDistance = 1)
        {
            RaycastResult hit;
            if (PhysicsEngine.Instance.Raycast(
              pos+Lazy.AngleToVector(angle,minDistance), Lazy.AngleToVector(angle), 200, out hit,
              rigidbodyExcluder: ExcludeAllButWallsFromRaycasting))
                return hit.Contact;
            return pos+Lazy.AngleToVector(angle,minDistance);
        }
    }
}

