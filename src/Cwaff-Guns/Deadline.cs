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

    /* TODO:
        - disable auto aim
        - fix rare hitscan issues
    */

    public class Deadline : AdvancedGunBehavior
    {
        public static string ItemName         = "Deadline";
        public static string SpriteName       = "bullatterer";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "Pythagoras Would be Proud";
        public static string LongDescription  = "(intersecting lines create explosions)";

        private static ExplosionData _DeadlineExplosion = null;

        private List <DeadlineLaser> _myLasers;
        private float _myTimer = 0;
        private GameObject _myLaserSight = null;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.SetFireAudio("Play_WPN_stdissuelaser_shot_01");

            var comp = gun.gameObject.AddComponent<Deadline>();
                comp.preventNormalReloadAudio = true;

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.collidesWithEnemies = false;
                projectile.gameObject.AddComponent<DeadlineProjectile>();

            ExplosionData defaultExplosion = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultExplosionData;
            _DeadlineExplosion = new ExplosionData()
            {
                forceUseThisRadius     = true,
                pushRadius             = 3f,
                damageRadius           = 3f,
                damageToPlayer         = 1f,
                doDamage               = true,
                damage                 = 100,
                doDestroyProjectiles   = false,
                doForce                = true,
                debrisForce            = 30f,
                preventPlayerForce     = false,
                explosionDelay         = 0.01f,
                usesComprehensiveDelay = false,
                doScreenShake          = true,
                playDefaultSFX         = true,
                effect                 = defaultExplosion.effect,
                ignoreList             = defaultExplosion.ignoreList,
                ss                     = new ScreenShakeSettings
                {
                    magnitude               = 2.5f,
                    speed                   = 2.5f,
                    time                    = 1f,
                    falloff                 = 0,
                    direction               = Vector2.zero,
                    vibrationType           = ScreenShakeSettings.VibrationType.Auto,
                    simpleVibrationStrength = Vibration.Strength.Light,
                    simpleVibrationTime     = Vibration.Time.Quick
                },
            };
        }

        public Deadline()
        {
            _myLasers = new List<DeadlineLaser>();
        }

        protected override void Update()
        {
            base.Update();

            if (!this.Player)
                return;

            if (_myLaserSight != null)
                UnityEngine.Object.Destroy(_myLaserSight);

            Vector2 target = Raycast.ToNearestWallOrObject(this.Player.sprite.WorldCenter, this.gun.CurrentAngle, minDistance: 1);
            float length = C.PIXELS_PER_TILE*Vector2.Distance(this.Player.sprite.WorldCenter,target);
            _myLaserSight = VFX.RenderLaserSight(this.Player.sprite.WorldCenter,length,2,this.gun.CurrentAngle);
            _myLaserSight.transform.parent = this.gun.transform;

            _myTimer += BraveTime.DeltaTime;
            float power = 200.0f+400.0f*Mathf.Abs(Mathf.Sin(16*_myTimer));

            foreach (DeadlineLaser laser in _myLasers)
                laser.UpdateLaser(emissivePower : power);
        }

        public void CreateALaser(Vector2 position, float angle)
        {
            Vector2 target = Raycast.ToNearestWallOrObject(position, angle, minDistance: C.PIXELS_PER_TILE);
            // raycast backwards to snap to wall
            Vector2 invtarget = Raycast.ToNearestWallOrObject(position, angle + (angle < 180 ? 180 : -180), minDistance: 0);
            this._myLasers.Add(new DeadlineLaser(invtarget,target,angle));
            if (this.Player)
                AkSoundEngine.PostEvent("Play_WPN_moonscraperLaser_shot_01", this.Player.gameObject);
            this.CheckForLaserIntersections();
        }

        public void CheckForLaserIntersections()
        {
            if (_myLasers.Count < 2)
                return;

            float closest = 9999f;
            int closestIndex = -1;
            Vector2 closestPosition = Vector2.zero;

            // find the nearest laser we'd collide with
            DeadlineLaser newest = _myLasers[_myLasers.Count-1];
            for (int i = 0; i < _myLasers.Count-1; ++i)
            {
                if (_myLasers[i].markedForDestruction)
                    continue; //if we're already trying to explode, don't
                Vector2? ipoint = newest.Intersects(_myLasers[i]);
                if (!ipoint.HasValue)
                    continue;
                float distance = Vector2.Distance(newest.start,ipoint.Value);
                if (distance >= closest)
                    continue;
                closest         = distance;
                closestIndex    = i;
                closestPosition = ipoint.Value;
            }

            // collide with the nearest laser
            if (closestIndex >= 0)
            {
                newest.UpdateEndPoint(closestPosition);
                newest.InitiateDeathSequenceAt(Vector2.zero,false);
                // myLasers[closestIndex].UpdateEndPoint(closestPosition);
                _myLasers[closestIndex].InitiateDeathSequenceAt(closestPosition.ToVector3ZisY(-1f),true);
                AkSoundEngine.PostEvent("gaster_blaster_sound_effect", ETGModMainBehaviour.Instance.gameObject);

                new FakeExplosion(Instantiate<GameObject>(VFX.animations["Splode"], closestPosition, Quaternion.identity));
            }

            for (int i = _myLasers.Count - 1; i >= 0; i--)
            {
                if (_myLasers[i].dead)
                    _myLasers.RemoveAt(i);
            }
        }

        private class FakeExplosion
        {
            private const float _START_SCALE  = 0.0f;
            private const float _END_SCALE    = 1.5f;
            private const float _RPS          = 1080.0f;
            private const float _MAX_LIFETIME = 1.0f;

            private GameObject _theExplosionVFX;
            private float _lifetime = 0.0f;

            public FakeExplosion(GameObject go)
            {
                this._theExplosionVFX = go;
                GameManager.Instance.StartCoroutine(Explode());
            }

            private IEnumerator Explode()
            {
                while(this._lifetime < _MAX_LIFETIME)
                {
                    this._lifetime += BraveTime.DeltaTime;
                    float curScale = _START_SCALE + (_END_SCALE - _START_SCALE)*(this._lifetime/_MAX_LIFETIME);
                    this._theExplosionVFX.transform.localScale = new Vector3(curScale,curScale,curScale);
                    this._theExplosionVFX.transform.rotation = Quaternion.Euler(0,0,_RPS*this._lifetime);
                    yield return null;
                }
                UnityEngine.Object.Destroy(this._theExplosionVFX);
                yield return null;
            }

        }

        private class DeadlineLaser
        {
            private static float _GrowthTime = 0.15f;
            private static float _ExplosionDelay = 1.0f;

            private float _length;
            private float _angle;
            private GameObject _laserVfx = null;
            private Vector3 _ipoint;
            private Color _color;
            private float _power = 0;
            private float _lifeTime = 0.0f;

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
                this._length      = C.PIXELS_PER_TILE*Vector2.Distance(this.start,this.end);
                this._angle       = angle;
                this._color       = Color.red;
                this._power       = 0;
                UpdateLaser();
            }

            public void UpdateEndPoint(Vector2 newEnd)
            {
                this.end     = newEnd;
                this._length = C.PIXELS_PER_TILE*Vector2.Distance(this.start,this.end);
                UpdateLaser();
            }

            public void InitiateDeathSequenceAt(Vector3 ipoint, bool explode = false)
            {
                if (markedForDestruction)
                    return;
                this.markedForDestruction = true;
                this._ipoint = ipoint;
                GameManager.Instance.StartCoroutine(ExplodeViolentlyAt(explode));
            }

            public void UpdateLaser(Color? color = null, float? emissivePower = null)
            {
                if (this.dead)
                    return;

                this._lifeTime += BraveTime.DeltaTime;
                float curLength = this._length * Mathf.Min(1,this._lifeTime/_GrowthTime);

                bool needToRecreate = false;
                if (color.HasValue)
                {
                    this._color = color.Value;
                    needToRecreate = true;
                }
                if (emissivePower.HasValue)
                    this._power = emissivePower.Value;

                if(needToRecreate || this._laserVfx == null)
                {
                    if (this._laserVfx != null)
                        UnityEngine.Object.Destroy(this._laserVfx);
                    this._laserVfx = VFX.RenderLaserSight(this.start,curLength,1,this._angle,this._color,this._power);
                    this.laserComp = _laserVfx.GetComponent<tk2dTiledSprite>();
                    this.laserMat  = this.laserComp.sprite.renderer.material;
                }
                else
                {
                    this.laserComp.dimensions = new Vector2(curLength, 1);
                    this.laserMat.SetFloat("_EmissivePower", this._power);
                }
            }

            private IEnumerator ExplodeViolentlyAt(bool explode)
            {
                UpdateLaser(color : Color.cyan);
                yield return new WaitForSeconds(_ExplosionDelay);

                if (explode)
                    Exploder.Explode(this._ipoint, _DeadlineExplosion, Vector2.zero);
                this.DestroyLaser();
                yield return null;
            }

            private void DestroyLaser()
            {
                this.dead = true;
                this.markedForDestruction = true;
                UnityEngine.Object.Destroy(this._laserVfx);
                this._laserVfx = null;
            }

            public Vector2? Intersects(DeadlineLaser other)
            {
                Vector2 ipoint = Vector2.zero;
                BraveUtility.LineIntersectsLine(start,end,other.start,other.end,out ipoint);
                return (ipoint != Vector2.zero) ? ipoint : null;
            }
        }
    }

    public class DeadlineProjectile : MonoBehaviour
    {
        private Projectile _projectile  = null;
        private PlayerController _owner = null;
        private Deadline _gun           = null;

        private void Start()
        {
            this._projectile = base.GetComponent<Projectile>();
            if (this._projectile.Owner is PlayerController pc)
            {
                this._owner = pc;
                this._gun = pc.CurrentGun.GetComponent<Deadline>();
            }

            SpeculativeRigidbody specRigidBody = this._projectile.specRigidbody;
            this._projectile.BulletScriptSettings.surviveTileCollisions = true;
            specRigidBody.OnPreRigidbodyCollision += this.OnPreCollision;
            specRigidBody.OnCollision += this.OnCollision;
        }

        // Only collide with tiles
        private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            if (!(otherRigidbody?.PrimaryPixelCollider?.IsTileCollider ?? false))
                PhysicsEngine.SkipCollision = true;
        }

        private void OnCollision(CollisionData tileCollision)
        {
            this._projectile.baseData.speed *= 0f;
            this._projectile.UpdateSpeed();
            float m_hitNormal = tileCollision.Normal.ToAngle();
            PhysicsEngine.PostSliceVelocity = new Vector2?(default(Vector2));
            SpeculativeRigidbody specRigidbody = this._projectile.specRigidbody;
            specRigidbody.OnCollision -= this.OnCollision;
            Vector2 spawnPoint = tileCollision.PostCollisionUnitCenter;
            _gun?.CreateALaser(spawnPoint,m_hitNormal);
            this._projectile.DieInAir();
        }
    }
}
