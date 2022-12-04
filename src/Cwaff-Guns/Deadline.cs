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

    /* TODO:
        - disable auto aim
        - add nicer fx
    */

    public class Deadline : AdvancedGunBehavior
    {
        public static string gunName          = "Deadline";
        public static string spriteName       = "bullatterer";
        public static string projectileName   = "38_special";
        public static string shortDescription = "Pythagoras Would be Proud";
        public static string longDescription  = "(intersecting lines instakil)";

        private List <DeadlineLaser> myLasers;
        // public List<GameObject> myLasers;
        public List<Vector2> laserEndpoints;

        private float myTimer = 0;

        public static void Add()
        {
            Gun gun = Lazy.InitGunFromStrings(gunName, spriteName, projectileName, shortDescription, longDescription);

            var comp = gun.gameObject.AddComponent<Deadline>();
            comp.preventNormalReloadAudio = true;
            comp.preventNormalFireAudio = true;
            comp.overrideNormalFireAudio = "Play_WPN_Vorpal_Shot_Critical_01";

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
            projectile.collidesWithEnemies = false;
            projectile.gameObject.AddComponent<DeadlineProjectile>();
        }

        public Deadline()
        {
            myLasers = new List<DeadlineLaser>();
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

            Vector2 target = Raycast.ToNearestWallOrObject(p.sprite.WorldCenter, this.gun.CurrentAngle, minDistance: 1);
            float length = C.PIXELS_PER_TILE*Vector2.Distance(p.sprite.WorldCenter,target);
            myLaser = VFX.RenderLaserSight(p.sprite.WorldCenter,length,2,this.gun.CurrentAngle);
            myLaser.transform.parent = this.gun.transform;

            myTimer += BraveTime.DeltaTime;
            float power = 200.0f+400.0f*Mathf.Abs(Mathf.Sin(16*myTimer));

            foreach (DeadlineLaser laser in myLasers)
                laser.UpdateLaser(emissivePower : power);
        }

        public void CreateALaser(Vector2 position, float angle)
        {
            Vector2 target = Raycast.ToNearestWallOrObject(position, angle, minDistance: C.PIXELS_PER_TILE);
            this.myLasers.Add(new DeadlineLaser(position,target,angle));
            this.CheckForLaserIntersections();
        }

        public void CheckForLaserIntersections()
        {
            if (myLasers.Count < 2)
                return;

            // if we call this function every time a new laser is created, we only have to check
            //   for intersections with the newest laser
            DeadlineLaser newest = myLasers[myLasers.Count-1];


            float closest = 9999f;
            int closestIndex = -1;
            Vector2 closestPosition = Vector2.zero;

            for (int i = 0; i < myLasers.Count-1; ++i)
            {
                if (myLasers[i].markedForDestruction)
                    continue; //if we're already trying to explode, don't
                Vector2? ipoint = newest.Intersects(myLasers[i]);
                if (!ipoint.HasValue)
                    continue;
                float distance = Vector2.Distance(newest.start,ipoint.Value);
                if (distance < closest)
                {
                    closest         = distance;
                    closestIndex    = i;
                    closestPosition = ipoint.Value;
                }
            }

            // collide with the nearest laser
            if (closestIndex >= 0)
            {
                newest.UpdateEndPoint(closestPosition);
                newest.InitiateDeathSequenceAt(Vector2.zero,false);
                // myLasers[closestIndex].UpdateEndPoint(closestPosition);
                myLasers[closestIndex].InitiateDeathSequenceAt(closestPosition.ToVector3ZisY(-1f),true);
                AkSoundEngine.PostEvent("gaster_blaster_sound_effect", ETGModMainBehaviour.Instance.gameObject);
            }

            for (int i = myLasers.Count - 1; i >= 0; i--)
            {
                if (myLasers[i].dead)
                    myLasers.RemoveAt(i);
            }
        }

        private class DeadlineLaser
        {
            private static float growthTime = 0.15f;

            private float length;
            private float angle;
            private GameObject laserVfx = null;
            private Vector3 ipoint;
            private Color color;
            private float power = 0;
            private float lifeTime = 0.0f;

            public Vector2 start;
            public Vector2 end;
            public tk2dTiledSprite laserComp = null;
            public Material laserMat = null;
            public bool markedForDestruction = false;
            public bool dead = false;

            // TODO: angle technically redundant here
            public DeadlineLaser(Vector2 p1, Vector2 p2, float angle)
            {
                this.start        = p1;
                this.end          = p2;
                this.length       = C.PIXELS_PER_TILE*Vector2.Distance(this.start,this.end);
                this.angle        = angle;
                this.color        = Color.red;
                this.power        = 0;
                UpdateLaser();
            }

            public void UpdateEndPoint(Vector2 newEnd)
            {
                this.end    = newEnd;
                this.length = C.PIXELS_PER_TILE*Vector2.Distance(this.start,this.end);
                UpdateLaser();
            }

            public void InitiateDeathSequenceAt(Vector3 ipoint, bool explode = false)
            {
                if (markedForDestruction)
                    return;
                this.markedForDestruction = true;
                this.ipoint = ipoint;
                GameManager.Instance.StartCoroutine(ExplodeViolentlyAt(explode));
            }

            public void UpdateLaser(Color? color = null, float? emissivePower = null)
            {
                if (this.dead)
                    return;

                this.lifeTime += BraveTime.DeltaTime;
                float curLength = this.length * Mathf.Min(1,this.lifeTime/growthTime);

                bool needToRecreate = false;
                if (color.HasValue)
                {
                    this.color = color.Value;
                    needToRecreate = true;
                }
                if (emissivePower.HasValue)
                    this.power = emissivePower.Value;

                if(needToRecreate || this.laserVfx == null)
                {
                    if (this.laserVfx != null)
                        UnityEngine.Object.Destroy(this.laserVfx);
                    this.laserVfx = VFX.RenderLaserSight(this.start,curLength,1,this.angle,this.color,this.power);
                    this.laserComp = laserVfx.GetComponent<tk2dTiledSprite>();
                    this.laserMat  = this.laserComp.sprite.renderer.material;
                }
                else
                {
                    this.laserComp.dimensions = new Vector2(curLength, 1);
                    this.laserMat.SetFloat("_EmissivePower", this.power);
                }
            }

            private IEnumerator ExplodeViolentlyAt(bool explode)
            {
                UpdateLaser(color : Color.cyan);

                // TODO: could maybe figure out why this doesn't work some day and optimize things
                //   current solution seems rather slow
                // tk2dTiledSprite comp = laserVfx.GetComponent<tk2dTiledSprite>();
                // comp.usesOverrideMaterial = true;
                // comp.sprite.renderer.material.SetColor("_OverrideColor", Color.cyan);
                // comp.sprite.renderer.material.SetColor("_EmissiveColor", Color.cyan);
                // comp.sprite.UpdateMaterial();
                // comp.sprite.ForceUpdateMaterial();
                // comp.dimensions = new Vector2(length, newWidth);
                // comp.sprite.renderer
                // comp.sprite.renderer.material.SetFloat("_EmissivePower", 100);
                // comp.sprite.renderer.material.SetFloat("_EmissiveColorPower", 1.55f);

                yield return new WaitForSeconds(1.0f);

                if (explode)
                    Exploder.Explode(this.ipoint, DerailGun.bigTrainExplosion, Vector2.zero);
                this.DestroyLaser();
                yield return null;
            }

            private void DestroyLaser()
            {
                this.dead = true;
                this.markedForDestruction = true;
                UnityEngine.Object.Destroy(this.laserVfx);
                this.laserVfx = null;
            }

            public Vector2? Intersects(DeadlineLaser other)
            {
                Vector2 ipoint = Vector2.zero;
                BraveUtility.LineIntersectsLine(start,end,other.start,other.end,out ipoint);
                if (ipoint != Vector2.zero)
                    return ipoint;
                return null;
            }
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
            specRigidBody.OnPreRigidbodyCollision += this.OnPreCollision;
            specRigidBody.OnCollision += this.OnCollision;
        }

        // Only collide with tiles
        private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            if (!otherRigidbody || otherRigidbody.PrimaryPixelCollider == null || !otherRigidbody.PrimaryPixelCollider.IsTileCollider)
            {
                PhysicsEngine.SkipCollision = true;
                return;
            }
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

            if (m_gun == null)
            {
                ETGModConsole.Log("this is a problem, Deadline is null o.o");
                return;
            }

            m_gun.CreateALaser(spawnPoint,m_hitNormal);

            this.m_projectile.DieInAir();
        }
    }
}
