namespace CwaffingTheGungy;

//TODO: disable auto aim
//TODO: fix rare hitscan issues

public class Deadline : CwaffGun
{
    public static string ItemName         = "Deadline";
    public static string ShortDescription = "Mission Improbable";
    public static string LongDescription  = "Upon colliding with walls, projectiles create laser beams perpendicular to the wall at their point of collision. If two such lasers intersect, a large explosion is created at the point of intersection.";
    public static string Lore             = "Not intended to be a weapon at all, this gun was used primarily as a tool for setting up dodge roll training rooms for newbie Gungeoneers. After an accidental crossing of the beams (an act generally known not to be a great idea) left seven injured, the engineer responsible for designing the tool publicly apologized for the incident. Immediately afterwards, he returned to a private meeting room with his colleagues, who unanimously agreed the explosion was freakin' awesome. High fives and fist bumps were promptly exchanged all around.";

    private const float _SIGHT_WIDTH = 2.0f;
    private const float _EXPLOSION_DELAY = 1.0f;

    internal static ExplosionData _DeadlineExplosion = null;
    internal static GameObject _SplodeVFX;

    private static List <DeadlineLaser> _MyLasers = new();
    private float _myTimer = 0;
    private GameObject _myLaserSight = null;
    private GameObject _debugLaserSight = null;

    public static void Init()
    {
        Lazy.SetupGun<Deadline>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 0.9f, ammo: 200, idleFps: 10, shootFps: 20,
            reloadFps: 30, muzzleVFX: "muzzle_deadline", muzzleFps: 20, muzzleScale: 0.4f, muzzleAnchor: Anchor.MiddleCenter,
            fireAudio: "deadline_fire_sound", reloadAudio: "deadline_reload_sound")
          .AddToShop(ModdedShopType.TimeTrader)
          .AddToShop(ModdedShopType.Boomhildr)
          .InitProjectile(GunData.New(clipSize: 8, cooldown: 0.4f, angleVariance: 0.0f, shootStyle: ShootStyle.SemiAutomatic, customClip: true,
            speed: 60.0f, range: 30.0f, sprite: "deadline_projectile", fps: 2, anchor: Anchor.MiddleLeft, collidesWithEnemies: false))
          .Attach<DeadlineProjectile>()
          .Attach<EasyTrailBullet>(trail => {
            trail.TrailPos   = trail.transform.position;
            trail.StartWidth = 0.2f;
            trail.EndWidth   = 0f;
            trail.LifeTime   = 0.1f;
            trail.BaseColor  = Color.green;
            trail.EndColor   = Color.green;
          });

        _DeadlineExplosion = Explosions.DefaultLarge.With(damage: 100f, force: 100f, debrisForce: 30f, radius: 3f, preventPlayerForce: false);
        _DeadlineExplosion.ss = new ScreenShakeSettings {
            magnitude               = 0.5f,
            speed                   = 1.5f,
            time                    = 1f,
            falloff                 = 0,
            direction               = Vector2.zero,
            vibrationType           = ScreenShakeSettings.VibrationType.Auto,
            simpleVibrationStrength = Vibration.Strength.Light,
            simpleVibrationTime     = Vibration.Time.Instant };

        _SplodeVFX = VFX.Create("splode", fps: 18, emissivePower: 100, emissiveColour: Color.cyan);
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        EnableLaserSight();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        DisableLaserSights();
        base.OnSwitchedAwayFromThisGun();
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        EnableLaserSight();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        DisableLaserSights();
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        DisableLaserSights();
        base.OnDestroy();
    }

    private void EnableLaserSight()
    {
        if (this._myLaserSight)
            return;
        this._myLaserSight = VFX.CreateLaserSight(this.gun.barrelOffset.transform.position, 1f, _SIGHT_WIDTH, this.gun.CurrentAngle, colour: Color.cyan, power: 50f);
        this._myLaserSight.transform.parent = this.gun.barrelOffset.transform;
        UpdateLaserSight();
    }

    private void DisableLaserSights()
    {
        this._myLaserSight.SafeDestroy();
        this._myLaserSight = null;
        this._debugLaserSight.SafeDestroy();
        this._debugLaserSight = null;
    }

    private void UpdateLaserSight()
    {
        if (!this._myLaserSight)
            return;

        int rayMask = CollisionMask.LayerToMask(CollisionLayer.HighObstacle/*, CollisionLayer.BulletBlocker, CollisionLayer.BulletBreakable*/);
        RaycastResult result;
        if (!PhysicsEngine.Instance.Raycast(this.gun.barrelOffset.transform.position.XY(), this.gun.CurrentAngle.ToVector(), 999f, out result, true, false, rayMask, null, false))
        {
            RaycastResult.Pool.Free(ref result);
            return;
        }
        float length = C.PIXELS_PER_TILE * result.Distance;
        Vector2 target = result.Contact;
        RaycastResult.Pool.Free(ref result);

        gun.sprite.ForceRotationRebuild();
        tk2dTiledSprite sprite = this._myLaserSight.GetComponent<tk2dTiledSprite>();
        sprite.renderer.enabled = true;
        sprite.dimensions = new Vector2(length, _SIGHT_WIDTH);
        this._myLaserSight.transform.rotation = this.gun.CurrentAngle.EulerZ();
        this._myLaserSight.transform.parent = this.gun.barrelOffset;
        this._myLaserSight.transform.localPosition = Vector2.zero;
        sprite.ForceRotationRebuild();
        sprite.UpdateZDepth();
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
        Color c = Color.Lerp(_Green, _Red, t);
        sprite.renderer.material.SetColor(CwaffVFX._OverrideColorId, c);
        sprite.renderer.material.SetColor(CwaffVFX._EmissiveColorId, c);
    }

    // Using LateUpdate() here instead of Update() so laser sight is updated correctly without jittering
    private void LateUpdate()
    {
        if (!this.PlayerOwner)
            return;

        if (this.PlayerOwner.m_hideGunRenderers.Value)
        {
            if (this._debugLaserSight)
                this._debugLaserSight.GetComponent<tk2dTiledSprite>().renderer.enabled = false;
            if (this._myLaserSight)
                this._myLaserSight.GetComponent<tk2dTiledSprite>().renderer.enabled = false;
        }
        else
        {
            EnableLaserSight();
            UpdateLaserSight();
            // DrawSpeculativeLaser();
        }
    }

    public void GetSpeculativeLaserEndpoints(out Vector2? start, out Vector2? end)
    {
        GetSpeculativeLaserEndpoints(this.gun.barrelOffset.transform.position.XY(), this.gun.CurrentAngle, out start, out end);
    }

    private const int _RAYCAST_SPREAD = 1;
    public static void GetSpeculativeLaserEndpoints(Vector2 from, float towardsAngle, out Vector2? start, out Vector2? end)
    {
        Vector2 normal1, normal2, normal3, trueNormal, normalb;

        // our speculative laser can hit the corner / seam of two tiles and produce the wrong normal, so do multiple raycasts to try to find the true normal
        int rayMask = CollisionMask.LayerToMask(CollisionLayer.HighObstacle/*, CollisionLayer.BulletBlocker, CollisionLayer.BulletBreakable*/);
        RaycastResult result;
        bool success = PhysicsEngine.Instance.Raycast(from, towardsAngle.ToVector(), 999f, out result, true, false, rayMask, null, false);
        if (!success)
        {
            start = null;
            end = null;
            RaycastResult.Pool.Free(ref result);
            return;
        }
        start   = result.Contact;
        normal1 = result.Normal;
        success = PhysicsEngine.Instance.Raycast(from, (towardsAngle + _RAYCAST_SPREAD).ToVector(), 999f, out result, true, false, rayMask, null, false);
        normal2 = success ? result.Normal : Vector2.zero;
        success = PhysicsEngine.Instance.Raycast(from, (towardsAngle - _RAYCAST_SPREAD).ToVector(), 999f, out result, true, false, rayMask, null, false);
        normal3 = success ? result.Normal : Vector2.zero;
        RaycastResult.Pool.Free(ref result);

        // If our first raycast agrees with either of the others, if all 3 disagree, or if either of the next 2 are a zero vector, use the first raycast normal
        if (normal1 == normal2 || normal1 == normal3 || normal2 != normal3 || normal2 == Vector2.zero)
            trueNormal = normal1;
        else // otherwise, use the second raycast normal
            trueNormal = normal2;

        start -= towardsAngle.ToVector(C.PIXEL_SIZE); // move ever so slightly out of the wall
        float normangle = trueNormal.ToAngle().Clamp360();
        end = (start.Value + trueNormal).ToNearestWall(out normalb, normangle, minDistance: 0.01f);
        if (normalb != -trueNormal)
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
        Vector2? start, end;
        GetSpeculativeLaserEndpoints(out start, out end);
        if (!start.HasValue)
        {
            if (this._debugLaserSight)
                this._debugLaserSight.GetComponent<tk2dTiledSprite>().renderer.enabled = false;
            return; // no normal, nothing to do
        }

        Vector2 delta = (end.Value - start.Value);
        if (this._debugLaserSight)
        {
            tk2dTiledSprite sprite = this._debugLaserSight.GetComponent<tk2dTiledSprite>();
            sprite.renderer.enabled = true;
            sprite.dimensions = new Vector2(C.PIXELS_PER_TILE * delta.magnitude, _SIGHT_WIDTH);
            this._debugLaserSight.transform.rotation = delta.EulerZ();
            this._debugLaserSight.transform.position = start.Value;
            sprite.ForceRotationRebuild();
            sprite.UpdateZDepth();
        }
        else
        {
            this._debugLaserSight = VFX.CreateLaserSight(start.Value, C.PIXELS_PER_TILE * delta.magnitude, _SIGHT_WIDTH, delta.ToAngle(), colour: Color.magenta/*, power: 50f*/);
            this._debugLaserSight.SetAlpha(0.3f);
        }

    }

    public static void CreateALaser(Vector2 start, Vector2 end, bool mastered)
    {
        _MyLasers.Add(new GameObject().AddComponent<DeadlineLaser>().Setup(start, end, (end - start).ToAngle(), mastered));
        CheckForLaserIntersections();
    }

    public static void CheckForLaserIntersections()
    {
        if (_MyLasers.Count < 2)
            return;

        float closest = 9999f;
        int closestIndex = -1;
        Vector2 closestPosition = Vector2.zero;

        // find the nearest laser we'd collide with
        DeadlineLaser newest = _MyLasers[_MyLasers.Count-1];
        for (int i = 0; i < _MyLasers.Count-1; ++i)
        {
            if (_MyLasers[i].markedForDestruction)
                continue; //if we're already trying to explode, don't
            Vector2? ipoint = newest.Intersects(_MyLasers[i]);
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
            newest.InitiateDeathSequenceAt(Vector2.zero,false, _EXPLOSION_DELAY);
            _MyLasers[closestIndex].InitiateDeathSequenceAt(closestPosition.ToVector3ZisY(-1f),true, _EXPLOSION_DELAY);
            ETGModMainBehaviour.Instance.gameObject.Play("gaster_blaster_sound_effect");
            new FakeExplosion(UnityEngine.Object.Instantiate(_SplodeVFX, closestPosition, Quaternion.identity), maxLifetime: _EXPLOSION_DELAY);
        }

        for (int i = _MyLasers.Count - 1; i >= 0; i--)
        {
            if (_MyLasers[i].dead)
                _MyLasers.RemoveAt(i);
        }
    }

    private class FakeExplosion
    {
        private const float _END_SCALE    = 1.5f;
        private const float _RPS          = 1080.0f;

        private GameObject _theExplosionVFX;
        private float _lifetime = 0.0f;
        private float _maxLifetime = 0.0f;

        public FakeExplosion(GameObject go, float maxLifetime)
        {
            this._theExplosionVFX = go;
            this._maxLifetime = maxLifetime;
            GameManager.Instance.StartCoroutine(Explode());
        }

        private IEnumerator Explode()
        {
            while(this._lifetime < this._maxLifetime)
            {
                this._lifetime += BraveTime.DeltaTime;
                float curScale = _END_SCALE * (this._lifetime / this._maxLifetime);
                this._theExplosionVFX.transform.localScale = new Vector3(curScale, curScale, curScale);
                this._theExplosionVFX.transform.rotation = Quaternion.Euler(0,0,_RPS*this._lifetime);
                yield return null;
            }
            UnityEngine.Object.Destroy(this._theExplosionVFX);
            yield return null;
        }

    }

    private class DeadlineLaser : MonoBehaviour
    {
        private const float _GROWTH_TIME = 0.15f;
        private const float _EXPLOSION_DELAY_MASTERED = 0.4f;

        private float _length;
        private float _angle;
        private GameObject _laserVfx = null;
        private Vector3 _ipoint;
        private Color _color;
        private float _power = 0;
        private float _lifeTime = 0.0f;
        private bool _mastered = false;

        public Vector2 start;
        public Vector2 end;
        public tk2dTiledSprite laserComp = null;
        public Material laserMat = null;
        public bool markedForDestruction = false;
        public bool dead = false;

        public DeadlineLaser Setup(Vector2 p1, Vector2 p2, float angle, bool mastered)
        {
            this.start        = p1;
            this.end          = p2;
            this._length      = C.PIXELS_PER_TILE*Vector2.Distance(this.start,this.end);
            this._angle       = angle;
            this._color       = Color.red;
            this._power       = 0;
            this._mastered    = mastered;
            UpdateLaser();
            return this;
        }

        public void UpdateEndPoint(Vector2 newEnd, bool andUpdate = true)
        {
            this.end     = newEnd;
            this._length = C.PIXELS_PER_TILE*Vector2.Distance(this.start,this.end);
            if (andUpdate)
                UpdateLaser();
        }

        public void InitiateDeathSequenceAt(Vector3 ipoint, bool explode, float timer)
        {
            if (markedForDestruction)
                return;
            this.markedForDestruction = true;
            this._ipoint = ipoint;
            GameManager.Instance.StartCoroutine(ExplodeViolentlyAt(explode, timer));
        }

        private void Update()
        {
            float power = 200.0f + 400.0f * Mathf.Abs(Mathf.Sin(16 * BraveTime.ScaledTimeSinceStartup));
            UpdateLaser(emissivePower : power);
        }

        public void UpdateLaser(Color? color = null, float? emissivePower = null)
        {
            if (this.dead)
                return;

            this._lifeTime += BraveTime.DeltaTime;
            float curLength = this._length * Mathf.Min(1,this._lifeTime/_GROWTH_TIME);

            bool needToRecreate = false;
            if (color.HasValue)
            {
                this._color = color.Value;
                needToRecreate = true;
            }
            if (emissivePower.HasValue)
                this._power = emissivePower.Value;

            if (needToRecreate || this._laserVfx == null)
            {
                this._laserVfx.SafeDestroy();
                this._laserVfx = VFX.CreateLaserSight(this.start,curLength,1,this._angle,this._color,this._power);
                this.laserComp = _laserVfx.GetComponent<tk2dTiledSprite>();
                this.laserMat  = this.laserComp.sprite.renderer.material;
            }
            else
            {
                this.laserComp.dimensions = new Vector2(curLength, 1);
                this.laserMat.SetFloat(CwaffVFX._EmissivePowerId, this._power);
            }

            if (this.markedForDestruction || !this._mastered)
                return;
            if (Lazy.NearestEnemyInLineOfSight(out Vector2 ipoint, this.start, this.start + this._angle.ToVector(curLength)) is not AIActor target)
                return;

            UpdateEndPoint(ipoint, andUpdate: false); // avoid infinite recursion
            InitiateDeathSequenceAt(ipoint.ToVector3ZisY(-1f), true, _EXPLOSION_DELAY_MASTERED);
            ETGModMainBehaviour.Instance.gameObject.Play("gaster_blaster_sound_effect_short");
            new FakeExplosion(UnityEngine.Object.Instantiate(_SplodeVFX, ipoint, Quaternion.identity), maxLifetime: _EXPLOSION_DELAY_MASTERED);
        }

        private IEnumerator ExplodeViolentlyAt(bool explode, float timer)
        {
            UpdateLaser(color : Color.cyan);
            yield return new WaitForSeconds(timer);

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
            return BraveUtility.LineIntersectsLine(start, end, other.start, other.end, out Vector2 ipoint) ? ipoint : null;
        }
    }
}

public class DeadlineProjectile : MonoBehaviour
{
    private void Start()
    {
        Projectile p = base.GetComponent<Projectile>();
        if (p.Owner is not PlayerController pc)
            return;

        Deadline.GetSpeculativeLaserEndpoints(p.SafeCenter, p.Direction.ToAngle(), out Vector2? start, out Vector2? end);
        if (!start.HasValue)
            return;

        Deadline.CreateALaser(start.Value, end.Value, pc.HasSynergy(Synergy.MASTERY_DEADLINE));
        p.DieInAir();
    }
}
