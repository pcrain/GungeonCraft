namespace CwaffingTheGungy;

using static BilliardBall.State;

public class English : CwaffGun
{
    public static string ItemName         = "English";
    public static string ShortDescription = "Racking Up Frags";
    public static string LongDescription  = "Fires a cue ball towards a rack of billiard balls that grows as the gun is charged. Billiard balls bounce off of walls, objects, and each other, dealing damage proportional to their velocity. Grounded billiard balls can be reactivated by other billiard balls. Increases curse by 1 while in inventory.";
    public static string Lore             = "This weapon appears to be an ordinary pool cue at first glance. The second, third, and fourth glances are much the same. The fifth glance, however, reveals a tiny spark at the tip of the cue, ready to materialize a cue ball with unforetold power and disregard for the conservation of momentum at the wielder's first intention to strike. The six glance reveals you have gone partially insane from staring at the tip of an ordinary pool cue for so long, and that it is, in fact, just an ordinary pool cue.";

    private const float _CHARGE_PER_LEVEL = 0.4f;
    private const int _MAX_CHARGE_LEVEL   = 5;
    private const int _MAX_PHANTOMS       = 1 + (_MAX_CHARGE_LEVEL * (_MAX_CHARGE_LEVEL + 1)) / 2;
    private const float _MAX_GLOW         = 5f;
    private const float _H_SPACE          = 0.625f;
    private const float _V_SPACE          = 0.625f;

    private static readonly int[] _BALL_ORDER = [8, 6, 11, 14, 7, 0, 5, 9, 2, 13, 10, 1, 12, 3, 4, 15];

    private static GameObject _BilliardBallPhantom           = null;
    private static GameObject _BilliardBallPlaceholder       = null;
    private static Projectile _BilliardBall                  = null;
    private static LinkedList<BilliardBall> _ExtantBilliards = new();

    private RoomHandler _lastRoom          = null;
    private List<GameObject> _phantoms     = null;
    private List<GameObject> _placeholders = null;
    private bool _wasCharging              = false;
    private float _chargeTime              = 0.0f;
    private int _chargeLevel               = 0;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<English>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.CHARGE, reloadTime: 0.9f, ammo: 960, shootFps: 40, chargeFps: 10,
                curse: 1f, muzzleFrom: Items.Mailbox, fireAudio: "billiard_first_strike_sound");
            gun.LoopAnimation(gun.chargeAnimation, loopStart: 5);
            gun.AddToSubShop(ItemBuilder.ShopType.Cursula);

        _BilliardBall = gun.InitProjectile(GunData.New(clipSize: -1, cooldown: 0.25f, angleVariance: 5.0f, chargeTime: 0f,
          shootStyle: ShootStyle.Charged, damage: 2.5f, speed: 81.0f, range: 9999f,
          sprite: "billiard_ball_projectile_small", fps: 12, scale: 1.5f, anchor: Anchor.MiddleCenter)).Attach<BilliardBall>();
        _BilliardBall.collidesWithProjectiles = true;
        _BilliardBall.collidesOnlyWithPlayerProjectiles = true;
        _BilliardBall.hitEffects.alwaysUseMidair = true;
        _BilliardBall.hitEffects.overrideMidairDeathVFX =
            ((ItemHelper.Get(Items.Winchester) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);

        _BilliardBallPhantom = VFX.Create("billiard_ball_small_vfx", fps: 2, loops: true, anchor: Anchor.MiddleCenter, scale: 1.5f, emissivePower: 10f);
        _BilliardBallPlaceholder = VFX.Create("billiard_ball_placeholder_vfx", fps: 10, loops: true, anchor: Anchor.MiddleCenter);
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        CwaffEvents.OnChangedRooms -= OnChangedRooms;
        CwaffEvents.OnChangedRooms += OnChangedRooms;
        this._lastRoom = player.CurrentRoom;
        UpdateBilliardCollisionStatuses(this._lastRoom);
        EnsurePhantoms();
    }

    private static void OnChangedRooms(PlayerController player, RoomHandler oldRoom, RoomHandler newRoom)
    {
        UpdateBilliardCollisionStatuses(newRoom);
    }

    private static void UpdateBilliardCollisionStatuses(RoomHandler newRoom)
    {
        LinkedListNode<BilliardBall> next = _ExtantBilliards.First;
        while (next != null)
        {
            if (!next.Value)
            {
                LinkedListNode<BilliardBall> dead = next;
                next = next.Next;
                _ExtantBilliards.Remove(dead);
                continue;
            }
            BilliardBall bball = next.Value;
            next = next.Next;
            bball.UpdateCollisionStatus(newRoom);
        }
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        DismissPhantoms();
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        EnsurePhantoms();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        DismissPhantoms();
    }

    public override void OnDestroy()
    {
        if (this._phantoms != null)
            for (int i = this._phantoms.Count - 1; i >= 0; --i)
                this._phantoms[i].SafeDestroy();
        if (this._placeholders != null)
            for (int i = this._placeholders.Count - 1; i >= 0; --i)
                this._placeholders[i].SafeDestroy();
        base.OnDestroy();
    }

    private void EnsurePhantoms()
    {
        this._phantoms     ??= Lazy.DefaultList<GameObject>(_MAX_PHANTOMS);
        this._placeholders ??= Lazy.DefaultList<GameObject>(_MAX_PHANTOMS);
        for (int i = 0; i < _MAX_PHANTOMS; ++i)
        {
            if (!this._phantoms[i])
            {
                this._phantoms[i] = SpawnManager.SpawnVFX(_BilliardBallPhantom, base.transform.position, Quaternion.identity, ignoresPools: true);
                this._phantoms[i].GetComponent<tk2dSpriteAnimator>().PickFrame(_BALL_ORDER[i]);
                this._phantoms[i].SetActive(false);
            }
            if (!this._placeholders[i])
            {
                this._placeholders[i] = SpawnManager.SpawnVFX(_BilliardBallPlaceholder, base.transform.position, Quaternion.identity, ignoresPools: true);
                this._placeholders[i].SetActive(false);
            }
        }
    }

    private void DismissPhantoms()
    {
        if (this._phantoms != null)
            for (int i = 0; i < this._phantoms.Count; ++i)
                if (this._phantoms[i])
                    this._phantoms[i].SetActive(false);
        if (this._placeholders != null)
            for (int i = 0; i < this._placeholders.Count; ++i)
                if (this._placeholders[i])
                    this._placeholders[i].SetActive(false);
    }

    public override void Update()
    {
        base.Update();
        if (this.PlayerOwner is not PlayerController player)
            return;
        if (this.gun.IsCharging)
        {
            if ((this._chargeTime += BraveTime.DeltaTime) > _CHARGE_PER_LEVEL)
            {
                this._chargeTime -= _CHARGE_PER_LEVEL;
                if (this._chargeLevel < _MAX_CHARGE_LEVEL)
                {
                    ++this._chargeLevel;
                    base.gameObject.Play("billiard_materialize_sound");
                }
            }
            UpdatePhantomBilliards();
        }
        else
        {
            if (this._wasCharging)
                DismissPhantoms();
            this._chargeLevel = 0;
            this._chargeTime = 0.0f;
        }
        this._wasCharging = this.gun.IsCharging;
    }

    private Vector2 _BasePhantomOffset => this.gun.CurrentAngle.ToVector(_V_SPACE * 3f);

    private void UpdatePhantomBilliards()
    {
        if (this.PlayerOwner is not PlayerController player)
            return;
        Vector2 basePos = this.gun.barrelOffset.position;
        bool canMaterialize = basePos.HasLineOfSight(basePos + _BasePhantomOffset);
        float baseAngle = this.gun.CurrentAngle;
        float perpAngle = this.gun.CurrentAngle + 90f;
        // arrange in a triangle
        Vector2 firstPos = _BasePhantomOffset;
        Vector2 baseVec;
        int d;
        int i = 0;
        float glow = canMaterialize ? 0.5f + Mathf.Max(-0.5f, Mathf.Sin(2f * Mathf.PI * (this._chargeTime / _CHARGE_PER_LEVEL))) : 0f;
        int lastRow = Mathf.Min(this._chargeLevel + 1, _MAX_CHARGE_LEVEL);
        for (d = 0; d < lastRow; ++d)
        {
            baseVec = firstPos + baseAngle.ToVector(_V_SPACE * d);
            for (int w = 0; w <= d; ++w)
            {
                Vector2 perpVec = perpAngle.ToVector(_H_SPACE * ((0.5f * d) - w));
                Vector2 pos = basePos + baseVec + perpVec;
                if (canMaterialize && d < this._chargeLevel)
                {
                    this._placeholders[i].SetActive(false);
                    this._phantoms[i].SetActive(true);
                    this._phantoms[i].transform.position = pos;
                    this._phantoms[i].GetComponent<tk2dSpriteAnimator>().PickFrame(canMaterialize ? _BALL_ORDER[i] : 7); // black eight ball if invalid
                    tk2dSprite sprite = this._phantoms[i].GetComponent<tk2dSprite>();
                    sprite.PlaceAtScaledPositionByAnchor(pos, Anchor.MiddleCenter);
                    sprite.renderer.material.SetFloat("_EmissivePower", _MAX_GLOW * glow);
                }
                else
                {
                    this._phantoms[i].SetActive(false);
                    this._placeholders[i].SetActive(true);
                    this._placeholders[i].transform.position = pos;
                    this._placeholders[i].GetComponent<tk2dSprite>().PlaceAtScaledPositionByAnchor(pos, Anchor.MiddleCenter);
                }
                ++i;
            }
        }
        // cue ball
        {
            i = _MAX_PHANTOMS - 1;
            this._placeholders[i].SetActive(false);
            this._phantoms[i].SetActive(true);
            this._phantoms[i].transform.position = basePos;
            this._phantoms[i].GetComponent<tk2dSpriteAnimator>().PickFrame(canMaterialize ? _BALL_ORDER[i] : 7); // black eight ball if invalid
            tk2dSprite sprite = this._phantoms[i].GetComponent<tk2dSprite>();
            sprite.PlaceAtScaledPositionByAnchor(basePos, Anchor.MiddleCenter);
            sprite.renderer.material.SetFloat("_EmissivePower", _MAX_GLOW * glow);
        }
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(projectile.specRigidbody);
        projectile.pierceMinorBreakables = true;
        projectile.SetFrame(15); // 15 == cue ball (16th ball, 0-indexed)
        if (!this._wasCharging)
            return;

        this._wasCharging = false;
        Vector2 barrelPos = this.gun.barrelOffset.position.XY();
        if (barrelPos.HasLineOfSight(barrelPos + _BasePhantomOffset))
            MaterializePhantoms();
        DismissPhantoms();
    }

    private void MaterializePhantoms()
    {
        UpdatePhantomBilliards();
        int numBalls = (this._chargeLevel * (this._chargeLevel + 1)) / 2;
        for (int i = 0; i < numBalls; ++i)
        {
            Vector2 pos = this._phantoms[i].GetComponent<tk2dSprite>().WorldCenter;
            GameObject projObj = SpawnManager.SpawnProjectile(_BilliardBall.gameObject, pos, Quaternion.identity, true);
            Projectile proj = projObj.GetComponent<Projectile>();
                proj.Owner = this.PlayerOwner;
                proj.collidesWithEnemies = true;
                proj.collidesWithPlayer = false;
                proj.SetFrame(_BALL_ORDER[i]);
            projObj.GetComponent<BilliardBall>().Setup(fired: false, failsafeLaunchAngle: i == 0 ? this.gun.CurrentAngle : null);
        }
        this.gun.LoseAmmo(numBalls);
    }

    internal static void AddExtantBilliard(BilliardBall billiardBall)
    {
        _ExtantBilliards.AddLast(billiardBall);
    }
}

public class BilliardBall : MonoBehaviour
{
    internal enum State
    {
        UNSET,
        FIRED,
        DEACTIVATED,
        ACTIVATED,
    }

    private const float _FRICTION         = 0.965f; // friction to apply when moving around
    private const float _COLLISON_DAMPING = 0.9f;   // percent speed to keep on colliding with anything other than another ball
    private const float _MIN_SPEED        = 1.0f;   // minimum speed before being considered at rest
    private const int _MAX_COLLISIONS     = 10;     // number of times we can collide before disintegrating
    private const int _RESTORE_COLLISIONS = 5;      // if a billiard is at rest and activated, resets _remainingCollisions to this
    private const float _MERCY_TIME       = 0.5f;   // seconds before _MAX_COLLISIONS starts being counted
    private const float _FAILSAFE_TIME    = 0.05f;  // seconds before the lead ball launches itself regardless of cue ball collision
    private const float _ROT_RATE         = 90f;    // animation speed for rotations
    private const float _DAMAGE_SCALE     = 0.5f;   // base damage scaling relative to original basedata.damage
    private const float _MAX_ADJUST       = 45f;    // adjust our angle by at most 45 degress to an actual enemy

    private tk2dBaseSprite _sprite             = null;
    private Projectile _projectile              = null;
    private State _state                        = UNSET;
    private PlayerController _owner             = null;
    private List<BilliardBall> _frameCollisions = new();
    private float _baseDamage                   = 0.0f;
    private float _baseSpeed                    = 0.0f;
    private SpeculativeRigidbody _body          = null;
    private bool _setup                         = false;
    private int _remainingCollisions            = 0;
    private float _spawnTime                    = 0.0f;
    private float _rotation                     = 0.0f;
    private bool _useFailsafeLaunch             = false;
    private float _failsafeLaunchAngle          = 0;

    private void Start()
    {
        Setup(fired: true);
        if (this._body.InsideWall())
            this._projectile.DieInAir(suppressInAirEffects: true, allowActorSpawns: false, allowProjectileSpawns: false, killedEarly: true);
    }

    public void Setup(bool fired, float? failsafeLaunchAngle = null)
    {
        if (this._setup)
            return;

        this._projectile          = base.GetComponent<Projectile>();
        this._owner               = this._projectile.Owner as PlayerController;
        this._baseDamage          = _DAMAGE_SCALE * this._projectile.baseData.damage;
        this._baseSpeed           = this._projectile.baseData.speed;
        this._body                = this._projectile.specRigidbody;
        this._sprite              = this._projectile.sprite;
        this._remainingCollisions = _MAX_COLLISIONS;
        this._spawnTime           = BraveTime.ScaledTimeSinceStartup;
        this._rotation            = 360f * UnityEngine.Random.value;

        this._projectile.BulletScriptSettings.surviveRigidbodyCollisions = true;
        this._projectile.BulletScriptSettings.surviveTileCollisions      = true;

        this._body.OnRigidbodyCollision    += this.OnRigidbodyCollision;
        this._body.OnRigidbodyCollision    += this.OnAnyCollision;
        this._body.OnTileCollision         += this.OnTileCollision;
        this._body.OnTileCollision         += this.OnAnyCollision;
        this._body.OnPreRigidbodyCollision += this.OnPreCollision;

        if (fired)
            this.Activate(FIRED);
        else
            this.Deactivate();
        if (failsafeLaunchAngle.HasValue)
        {
            this._failsafeLaunchAngle = failsafeLaunchAngle.Value;
            this._useFailsafeLaunch = true;
        }
        English.AddExtantBilliard(this);

        this._setup = true;
    }

    internal void UpdateCollisionStatus(RoomHandler newRoom)
    {
        if (!this._body || this._state != DEACTIVATED)
            return;

        Vector2 pos = this._projectile.SafeCenter;
        this._body.enabled = (newRoom == pos.GetAbsoluteRoom());
        if (!this._body.enabled && !pos.OnScreen(leeway: 1f))
            UnityEngine.Object.Destroy(base.gameObject);
    }

    // Skip collisions if we were just fired and haven't hit another ball yet
    private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (this._state != FIRED || (BraveTime.ScaledTimeSinceStartup - this._spawnTime) > _MERCY_TIME)
        {
            this._body.OnPreRigidbodyCollision -= this.OnPreCollision;
            return;
        }
        GameObject other = otherRigidbody.gameObject;
        if (!other.GetComponent<BilliardBall>() && !other.GetComponent<AIActor>())
            PhysicsEngine.SkipCollision = true;
    }

    private void OnAnyCollision(CollisionData tileCollision)
    {
        if (BraveTime.ScaledTimeSinceStartup - _spawnTime < _MERCY_TIME)
            return;
        if ((this._remainingCollisions--) > 0)
            return;
        this._projectile.DieInAir();
    }

    private static Vector2 HandleSimpleBounce(CollisionData collision, float damping = 1.0f)
    {
        Vector2 newVel = collision.MyRigidbody.Velocity;
        if (collision.CollidedX)
          newVel = newVel.WithX(-newVel.x);
        if (collision.CollidedY)
          newVel = newVel.WithY(-newVel.y);
        return newVel * damping;
    }

    private void OnTileCollision(CollisionData collision)
    {
        Vector2 newVel = HandleSimpleBounce(collision, damping: _COLLISON_DAMPING);
        this._projectile.m_currentSpeed = this._projectile.baseData.speed = newVel.magnitude;
        newVel = AdjustTowardsNearbyEnemiesOrBalls(this, this, newVel);
        this._projectile.SendInDirection(newVel, true);
        PhysicsEngine.PostSliceVelocity = newVel;
    }

    private void OnRigidbodyCollision(CollisionData collision)
    {
        if (collision.OtherRigidbody is SpeculativeRigidbody other && other.gameObject.GetComponent<BilliardBall>() is BilliardBall bb)
        {
            DoElasticCollision(collision, bb);
            return;
        }
        if (this._state == DEACTIVATED)
            return;

        Vector2 newVel = HandleSimpleBounce(collision, damping: _COLLISON_DAMPING);
        this._projectile.m_currentSpeed = this._projectile.baseData.speed = newVel.magnitude;
        this._projectile.SendInDirection(newVel, true);
        PhysicsEngine.PostSliceVelocity = newVel;

        base.gameObject.PlayUnique("billiard_collide_sound");
    }

    private void DoElasticCollision(CollisionData collision, BilliardBall other)
    {
        if (this._frameCollisions.Contains(other))
            return;

        Vector2 posDiff = collision.MyRigidbody.UnitCenter - collision.OtherRigidbody.UnitCenter;
        Vector2 v1 = collision.MyRigidbody.Velocity;
        Vector2 v2 = collision.OtherRigidbody.Velocity;
        float invDistNorm = 1f / Mathf.Max(0.1f, posDiff.sqrMagnitude);
        Vector2 newv1 = v1 - (Vector2.Dot(v1 - v2, posDiff) * invDistNorm) * posDiff;
        Vector2 newv2 = v2 - (Vector2.Dot(v2 - v1, -posDiff) * invDistNorm) * (-posDiff);

        float newSpeed = Mathf.Sqrt(Mathf.Max(v1.sqrMagnitude, v2.sqrMagnitude/*, newv1.sqrMagnitude, newv2.sqrMagnitude*/));

        PhysicsEngine.PostSliceVelocity = newSpeed * newv1.normalized;
        this.Activate();
        this._projectile.baseData.speed = newSpeed;
        this._projectile.UpdateSpeed();
        this._projectile.SendInDirection(newv1, true);

        other.Activate();
        Projectile otherProj = other.GetComponent<Projectile>();
        otherProj.baseData.speed = newSpeed;
        otherProj.UpdateSpeed();
        otherProj.SendInDirection(AdjustTowardsNearbyEnemiesOrBalls(this, other, newv2), true);

        this._frameCollisions.Add(other);
        other._frameCollisions.Add(this);

        base.gameObject.Play("billiard_collide_sound");
    }

    private static Vector2 AdjustTowardsNearbyEnemiesOrBalls(BilliardBall self, BilliardBall other, Vector2 origDir)
    {
        const float _MAX_RADIUS = 20f;
        Vector2 pos = self._sprite.WorldCenter;
        float origAngle = origDir.ToAngle();
        Vector2? nearest = Lazy.NearestEnemyPosWithinConeOfVision(start: pos, coneAngle: origAngle, maxDeviation: _MAX_ADJUST, maxDistance: _MAX_RADIUS);
        if (nearest.HasValue)
            return (nearest.Value - pos);
        return origDir;
    }

    private void Update()
    {
        this._frameCollisions.Clear();
        if (this._useFailsafeLaunch && (BraveTime.ScaledTimeSinceStartup - this._spawnTime) > _FAILSAFE_TIME)
        {
            this.Activate();
            this._projectile.baseData.speed = this._baseSpeed;
            this._projectile.UpdateSpeed();
            this._projectile.SendInDirection(this._failsafeLaunchAngle.ToVector(), true);
            ETGModConsole.Log($"did failsafe launch at angle {this._failsafeLaunchAngle} with speed {this._baseSpeed}");
            return;
        }

        if (this._state != FIRED && this._state != ACTIVATED)
            return;

        this._projectile.ApplyFriction(_FRICTION);
        float speed = this._projectile.baseData.speed;
        if (speed < _MIN_SPEED)
        {
            this.Deactivate();
            return;
        }

        this._rotation += Mathf.Sign(this._projectile.specRigidbody.Velocity.x) * _ROT_RATE * speed * BraveTime.DeltaTime;
        this._projectile.transform.localRotation = this._rotation.EulerZ();

        if (this._state == ACTIVATED)
            this._projectile.baseData.damage = this._baseDamage * Mathf.Log(speed, 3);
        else if (this._state == FIRED)
            this._projectile.baseData.damage = this._baseDamage;
    }

    private void AddTrailIfMissing()
    {
        if (this._projectile.GetComponent<EasyTrailBullet>())
            return;

        EasyTrailBullet trail = this._projectile.gameObject.AddComponent<EasyTrailBullet>();
            trail.StartWidth = 0.15f;
            trail.EndWidth   = 0.025f;
            trail.LifeTime   = 0.1f;
            trail.BaseColor  = Color.Lerp(Color.white, Color.clear, 0.2f);
            trail.StartColor = Color.Lerp(Color.white, Color.clear, 0.2f);
            trail.EndColor   = Color.Lerp(Color.white, Color.clear, 0.5f);
    }

    private void Activate(State state = ACTIVATED)
    {
        if (this._state == state)
            return;

        this._projectile.enabled = true;
        this._body.CollideWithTileMap = true;
        this._projectile.collidesWithEnemies = true;
        this._projectile.collidesWithProjectiles = true;
        this._projectile.UpdateCollisionMask();
        if (this._remainingCollisions < _RESTORE_COLLISIONS)
            this._remainingCollisions = _RESTORE_COLLISIONS;
        AddTrailIfMissing();
        this._useFailsafeLaunch = false;
        this._state = state;
    }

    private void Deactivate()
    {
        if (this._state == DEACTIVATED)
            return;

        this._body.Velocity = Vector2.zero;
        this._body.CollideWithTileMap = false;
        this._projectile.collidesWithEnemies = false;
        this._projectile.collidesWithProjectiles = false;
        this._projectile.UpdateCollisionMask();
        this._projectile.enabled = false;
        this._state = DEACTIVATED;
    }
}
