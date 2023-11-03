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
        public static string SpriteName       = "deadline";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "Pythagoras Would be Proud";
        public static string LongDescription  = "Upon colliding with walls, projectiles create laser beams perpendicular to the wall at their point of collision. If two such lasers intersect, a large explosion is created at the point of intersection.\n\nNot intended to be a weapon at all, this gun was used primarily as a tool for setting up dodge roll training rooms for newbie Gungeoneers. After an accidental crossing of the beams (an act generally known not to be a great idea) left seven injured, the engineer responsible for desigining the tool publicly apologized for the incident. Immediately afterwards, he returned to a private meeting room with his colleagues, who unanimously agreed the explosion was freakin' awesome. High fives and fist bumps were promptly exchanged all around.";

        private const float _SIGHT_WIDTH = 2.0f;

        internal static ExplosionData _DeadlineExplosion = null;
        internal static tk2dSpriteAnimationClip _BulletSprite;
        internal static GameObject _SplodeVFX;

        private List <DeadlineLaser> _myLasers = new();
        private float _myTimer = 0;
        private GameObject _myLaserSight = null;
        private GameObject _debugLaserSight = null;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun<Deadline>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.SetAttributes(quality: PickupObject.ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 0.9f, ammo: 64);
                gun.SetAnimationFPS(gun.shootAnimation, 20);
                gun.SetAnimationFPS(gun.reloadAnimation, 30);
                gun.SetAnimationFPS(gun.idleAnimation, 10);
                gun.SetFireAudio("deadline_fire_sound");
                gun.SetReloadAudio("deadline_fire_sound");
                gun.AddToSubShop(ModdedShopType.TimeTrader);
                gun.AddToSubShop(ModdedShopType.Boomhildr);

            ProjectileModule mod = gun.DefaultModule;
                mod.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.angleVariance       = 0.0f;
                mod.cooldownTime        = 0.4f;
                mod.numberOfShotsInClip = 8;

            _BulletSprite = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("deadline_projectile").Base(),
                2, true, new IntVector2(23, 4),
                false, tk2dBaseSprite.Anchor.MiddleLeft, true, true);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.AddDefaultAnimation(_BulletSprite);
                projectile.collidesWithEnemies = false;
                projectile.baseData.speed      = 60.0f;
                projectile.baseData.range      = 30.0f;
                projectile.gameObject.AddComponent<DeadlineProjectile>();

            EasyTrailBullet trail = projectile.gameObject.AddComponent<EasyTrailBullet>();
                trail.TrailPos   = trail.transform.position;
                trail.StartWidth = 0.2f;
                trail.EndWidth   = 0f;
                trail.LifeTime   = 0.1f;
                trail.BaseColor  = Color.green;
                trail.EndColor   = Color.green;

            ExplosionData defaultExplosion = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultExplosionData;
            _DeadlineExplosion = new ExplosionData()
            {
                forceUseThisRadius     = true,
                pushRadius             = 3f,
                damageRadius           = 3f,
                damageToPlayer         = 0f,
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
                    magnitude               = 0.5f,
                    speed                   = 1.5f,
                    time                    = 1f,
                    falloff                 = 0,
                    direction               = Vector2.zero,
                    vibrationType           = ScreenShakeSettings.VibrationType.Auto,
                    simpleVibrationStrength = Vibration.Strength.Light,
                    simpleVibrationTime     = Vibration.Time.Instant
                },
            };

            _SplodeVFX = VFX.RegisterVFXObject("Splode", ResMap.Get("splode"),
                fps: 18, loops: true, anchor: tk2dBaseSprite.Anchor.MiddleCenter, emissivePower: 100, emissiveColour: Color.cyan);
        }

        public override void OnSwitchedToThisGun()
        {
            base.OnSwitchedToThisGun();
            EnableLaserSight();
        }

        public override void OnSwitchedAwayFromThisGun()
        {
            DisableLaserSight();
            base.OnSwitchedAwayFromThisGun();
        }

        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            base.OnPickedUpByPlayer(player);
            EnableLaserSight();
        }

        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            DisableLaserSight();
            base.OnPostDroppedByPlayer(player);
        }

        private void EnableLaserSight()
        {
            if (this._myLaserSight)
                return;
            this._myLaserSight = VFX.CreateLaserSight(this.gun.barrelOffset.transform.position, 1f, _SIGHT_WIDTH, this.gun.CurrentAngle, colour: Color.cyan, power: 50f);
            this._myLaserSight.transform.parent = this.gun.barrelOffset.transform;
            UpdateLaserSight();
        }

        private void DisableLaserSight()
        {
            if (!this._myLaserSight)
                return;
            UnityEngine.Object.Destroy(this._myLaserSight);
        }

        private void UpdateLaserSight()
        {
            if (!this._myLaserSight)
                return;

            Vector2 target = Raycast.ToNearestWall(this.gun.barrelOffset.transform.position, this.gun.CurrentAngle, minDistance: 0.01f);
            float length = C.PIXELS_PER_TILE*Vector2.Distance(this.gun.barrelOffset.transform.position,target);

            tk2dTiledSprite sprite = this._myLaserSight.GetComponent<tk2dTiledSprite>();
            sprite.dimensions = new Vector2(length, _SIGHT_WIDTH);
            this._myLaserSight.transform.rotation = this.gun.CurrentAngle.EulerZ();
            this._myLaserSight.transform.position = this.gun.barrelOffset.transform.position; // TODO: maybe unnecessary?
            MakeLaserMatchGunSpriteColor(sprite);
        }

        // Logic below is custom-tailored to current specific animation, change as necessary
        internal static Color _Green = Color.green;
        internal static Color _Red   = Color.red;
        private void MakeLaserMatchGunSpriteColor(tk2dTiledSprite sprite)
        {
            int frame = this.gun.spriteAnimator.CurrentFrame;
            string clip = this.gun.spriteAnimator.CurrentClip.name;

            float t = 0.0f;
            if (clip == "deadline_idle") // max green on 0 and 20, max red on 10
                t = 1.0f - 0.1f*Mathf.Abs(10 - frame); // full red
            else if (clip == "deadline_reload") // max green on 0 and 21, max red on 10 and 11
                t = 0.1f * ((frame < 11) ? frame : (21 - frame));
            else if (clip == "deadline_fire") // always green
                t = 0.0f;
            else
                return; // unknown animation, nothing to do
            Color c = Lazy.Blend(_Green, _Red, t);
            sprite.renderer.material.SetColor("_OverrideColor", c);
            sprite.renderer.material.SetColor("_EmissiveColor", c);
        }

        protected override void Update()
        {
            base.Update();
            if (!this.Player)
                return;

            if (this.Player.m_hideGunRenderers.Value)
                DisableLaserSight();
            else
            {
                EnableLaserSight();
                UpdateLaserSight();
                // DrawSpeculativeLaser();
            }

            this._myTimer += BraveTime.DeltaTime;
            float power = 200.0f + 400.0f * Mathf.Abs(Mathf.Sin(16 * this._myTimer));
            foreach (DeadlineLaser laser in _myLasers)
                laser.UpdateLaser(emissivePower : power);
        }

        public void GetSpeculativeLaserEndpoints(out Vector2? start, out Vector2? end)
        {
            Vector2 normal, normalb;
            start = Raycast.ToNearestWall(this.gun.barrelOffset.transform.position, out normal, this.gun.CurrentAngle, minDistance: 0.01f);
            if (normal == Vector2.zero)
            {
                start = null;
                end   = null;
                return; // no normal, nothing to do
            }

            start -= this.gun.CurrentAngle.ToVector(C.PIXEL_SIZE); // move ever so slightly out of the wall
            float normangle = normal.ToAngle().Clamp360();
            end = (start.Value + normal).ToNearestWall(out normalb, normangle, minDistance: 0.01f);
            if (normalb != -normal)
            {
                start = null;
                end   = null;
                return; // other wall's normal isn't the complete inverse of our original wall's normal, so not a good wall for putting out a laser
            }

            end -= normangle.ToVector(C.PIXEL_SIZE); // move ever so slightly out of the wall
            return;
        }

        private void DrawSpeculativeLaser()
        {
            if (this._debugLaserSight)
                UnityEngine.Object.Destroy(this._debugLaserSight);

            Vector2? start, end;
            GetSpeculativeLaserEndpoints(out start, out end);
            if (!start.HasValue)
                return; // no normal, nothing to do

            Vector2 delta = (end.Value - start.Value);
            this._debugLaserSight = VFX.CreateLaserSight(start.Value, C.PIXELS_PER_TILE * delta.magnitude, _SIGHT_WIDTH, delta.ToAngle(), colour: Color.magenta, power: 50f);
        }

        public void CreateALaser(Vector2 start, Vector2 end)
        {
            this._myLasers.Add(new DeadlineLaser(start, end, (end - start).ToAngle()));
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
                _myLasers[closestIndex].InitiateDeathSequenceAt(closestPosition.ToVector3ZisY(-1f),true);
                AkSoundEngine.PostEvent("gaster_blaster_sound_effect", ETGModMainBehaviour.Instance.gameObject);

                new FakeExplosion(Instantiate<GameObject>(_SplodeVFX, closestPosition, Quaternion.identity));
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
                    this._laserVfx = VFX.CreateLaserSight(this.start,curLength,1,this._angle,this._color,this._power);
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

        private Vector2? _start;
        private Vector2? _end;

        private void Start()
        {
            this._projectile = base.GetComponent<Projectile>();
            if (this._projectile.Owner is not PlayerController pc)
                return;

            this._owner = pc;
            this._gun = pc.CurrentGun.GetComponent<Deadline>();
            this._gun.GetSpeculativeLaserEndpoints(out this._start, out this._end);
            if (!this._start.HasValue)
                return;

            SpeculativeRigidbody specRigidBody = this._projectile.specRigidbody;
            this._projectile.BulletScriptSettings.surviveTileCollisions = true;
            specRigidBody.OnPreRigidbodyCollision += this.OnPreCollision;
            specRigidBody.OnCollision += this.OnCollision;
        }

        // private static PrototypeDungeonRoom.RoomCategory[] _BannedRoomTypes = {
        //     PrototypeDungeonRoom.RoomCategory.BOSS,
        //     PrototypeDungeonRoom.RoomCategory.CONNECTOR,
        //     PrototypeDungeonRoom.RoomCategory.ENTRANCE,
        //     PrototypeDungeonRoom.RoomCategory.EXIT,
        // };

        // Only collide with tiles
        private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            if (!(otherRigidbody?.PrimaryPixelCollider?.IsTileCollider ?? false))
                PhysicsEngine.SkipCollision = true;

            // RoomHandler room = myPixelCollider.UnitCenter.GetAbsoluteRoom();
            // if (_BannedRoomTypes.Contains(room.area.PrototypeRoomCategory) || !myPixelCollider.FullyWithin(room.GetBoundingRect()))
            //     PhysicsEngine.SkipCollision = true;
        }

        private void OnCollision(CollisionData tileCollision)
        {
            // this._projectile.baseData.speed = 0.01f;
            // this._projectile.UpdateSpeed();
            // float m_hitNormal = tileCollision.Normal.ToAngle();
            // PhysicsEngine.PostSliceVelocity = new Vector2?(default(Vector2));
            // SpeculativeRigidbody specRigidbody = this._projectile.specRigidbody;
            // specRigidbody.OnCollision -= this.OnCollision;
            // Vector2 spawnPoint = tileCollision.PostCollisionUnitCenter;
            // _gun?.CreateALaser(spawnPoint,m_hitNormal);
            _gun?.CreateALaser(this._start.Value, this._end.Value);
            this._projectile.DieInAir();
        }
    }
}
