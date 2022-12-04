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
    public class Deadline : AdvancedGunBehavior
    {
        public static string gunName          = "Deadline";
        public static string spriteName       = "bullatterer";
        public static string projectileName   = "38_special";
        public static string shortDescription = "Pythagoras Would be Proud";
        public static string longDescription  = "(intersecting lines instakil)";

        public List<GameObject> myLasers;
        public List<Vector2> laserEndpoints;

        public static void Add()
        {
            Gun gun = Lazy.InitGunFromStrings(gunName, spriteName, projectileName, shortDescription, longDescription);
            var comp = gun.gameObject.AddComponent<Deadline>();
            comp.preventNormalReloadAudio = true;
            comp.preventNormalFireAudio = true;
            comp.overrideNormalFireAudio = "Play_WPN_Vorpal_Shot_Critical_01";

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
            projectile.gameObject.AddComponent<DeadlineProjectile>();
        }

        public Deadline()
        {
            laserEndpoints = new List<Vector2>();
            myLasers = new List<GameObject>();
        }

        private GameObject myLaser = null;

        protected override void Update()
        {
            base.Update();

            if (!(this.gun && this.gun.GunPlayerOwner()))
                return;
            PlayerController p = this.gun.GunPlayerOwner();

            if (myLaser != null)
                UnityEngine.Object.Destroy(myLaser);

            Vector2 target = Raycast.ToNearestWallOrEnemyOrObject(p.sprite.WorldCenter, this.gun.CurrentAngle, 1);
            float length = C.PIXELS_PER_TILE*Vector2.Distance(p.sprite.WorldCenter,target);
            myLaser = VFX.RenderLaserSight(p.sprite.WorldCenter,length,2,this.gun.CurrentAngle);
            myLaser.transform.parent = this.gun.transform;
        }

        public void CheckForLaserIntersections()
        {
            ETGModConsole.Log("checking for laser intersections");
            if (laserEndpoints.Count < 4)
            {
                ETGModConsole.Log("not enough lasers");
                return;
            }
            if (laserEndpoints.Count % 2 != 0)
            {
                ETGModConsole.Log("odd number of laser endpoints, you messed up o.o");
                return;
            }
            Vector2 ipoint = Vector2.zero;
            BraveUtility.LineIntersectsLine(laserEndpoints[0],laserEndpoints[1],laserEndpoints[2],laserEndpoints[3],out ipoint);
            if (ipoint != Vector2.zero)
            {
                ETGModConsole.Log("found intersection at"+ipoint.x+","+ipoint.y);
                Exploder.Explode(ipoint, DerailGun.bigTrainExplosion, Vector2.zero);
            }
            else
            {
                ETGModConsole.Log("no intersection found");
            }
            for (int i = 0; i < myLasers.Count; i++)
            {
                UnityEngine.Object.Destroy(myLasers[i]);
            }
            myLasers.Clear();
            laserEndpoints.Clear();
        }
    }

    public class DeadlineProjectile : MonoBehaviour
    {
        private Projectile m_projectile;
        private PlayerController m_owner;
        private Deadline m_gun = null;

        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (this.m_projectile.Owner && this.m_projectile.Owner is PlayerController)
            {
                this.m_owner = this.m_projectile.Owner as PlayerController;
                m_gun = this.m_owner.CurrentGun.GetComponent<Deadline>();
                if (m_gun == null)
                    ETGModConsole.Log("this is a problem, Deadline is null o.o");
            }
            else{
                ETGModConsole.Log("our owner is not a player O_O");
            }

            SpeculativeRigidbody specRigidBody = this.m_projectile.specRigidbody;
            this.m_projectile.BulletScriptSettings.surviveTileCollisions = true;
            specRigidBody.OnCollision += this.OnCollision;
            // this.m_projectile.AdjustPlayerProjectileTint(Color.green, 2);
        }

        private void OnCollision(CollisionData tileCollision)
        {
            this.m_projectile.baseData.speed *= 0f;
            this.m_projectile.UpdateSpeed();
            float m_hitNormal = tileCollision.Normal.ToAngle();
            PhysicsEngine.PostSliceVelocity = new Vector2?(default(Vector2));
            SpeculativeRigidbody specRigidbody = this.m_projectile.specRigidbody;
            specRigidbody.OnCollision -= this.OnCollision;

            // Vector2 spawnPoint = this.m_projectile.sprite.WorldCenter;
            Vector2 spawnPoint = tileCollision.PostCollisionUnitCenter;

            CreateALaser(spawnPoint,m_hitNormal);

            this.m_projectile.DieInAir();
        }

        private void CreateALaser(Vector2 position, float angle, float width = 1)
        {
            Vector2 target = Raycast.ToNearestWallOrEnemyOrObject(position, angle, 1);
            float length = C.PIXELS_PER_TILE*Vector2.Distance(position,target);
            GameObject theLaser = VFX.RenderLaserSight(position,length,width,angle);

            if (m_gun == null)
            {
                ETGModConsole.Log("this is a problem, Deadline is null o.o");
                return;
            }

            m_gun.laserEndpoints.Add(position);
            m_gun.laserEndpoints.Add(target);
            m_gun.myLasers.Add(theLaser);
            m_gun.CheckForLaserIntersections();
        }
    }
}
