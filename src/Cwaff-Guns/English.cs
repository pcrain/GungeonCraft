namespace CwaffingTheGungy;

using System;
using static BilliardBall.State;

/* TODO:
    - more ball colors
    - better sounds for charging
    - better sounds for firing

    - gun sprites

    - fix occasional no-launch glitch
    ? balls falling in pits
*/

public class English : CwaffGun
{
    public static string ItemName         = "English";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const float _CHARGE_PER_LEVEL = 0.5f;
    private const int _MAX_CHARGE_LEVEL = 4;
    private const float _MAX_GLOW = 50f;

    internal static GameObject _BilliardBallPhantom = null;
    internal static GameObject _BilliardBallPlaceholder = null;
    internal static LinkedList<BilliardBall> _ExtantBilliards = new();
    internal static Projectile _BilliardBall = null;

    private RoomHandler _lastRoom = null;
    private List<GameObject> _phantoms = null;
    private List<GameObject> _placeholders = null;
    private bool _wasCharging = false;
    private float _chargeTime = 0.0f;
    private int _chargeLevel = 0;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<English>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.CHARGE, reloadTime: 0.9f, ammo: 600, shootFps: 14, reloadFps: 4,
                muzzleFrom: Items.Mailbox, fireAudio: "paintball_shoot_sound", reloadAudio: "paintball_reload_sound");

        _BilliardBall = gun.InitProjectile(GunData.New(clipSize: -1, cooldown: 0.25f, angleVariance: 5.0f, chargeTime: 0f,
          shootStyle: ShootStyle.Charged, damage: 2.5f, speed: 81.0f, range: 9999f, spawnSound: "tomislav_shoot",
          sprite: "billiard_ball", fps: 12, scale: 1.5f, anchor: Anchor.MiddleCenter)).Attach<BilliardBall>();
        _BilliardBall.collidesWithProjectiles = true;
        _BilliardBall.collidesOnlyWithPlayerProjectiles = true;
        _BilliardBall.hitEffects.alwaysUseMidair = true;
        _BilliardBall.hitEffects.overrideMidairDeathVFX =
            ((ItemHelper.Get(Items.Winchester) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);

        _BilliardBallPhantom = VFX.Create("billiard_ball_vfx", fps: 2, loops: true, anchor: Anchor.MiddleCenter, scale: 1.5f, emissivePower: 10f);
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
        UpdateBilliardCollisionStatuses(oldRoom);
    }

    private static void UpdateBilliardCollisionStatuses(RoomHandler room)
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
            bball.UpdateCollisionStatus(room);
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
        if (this._phantoms == null)
            this._phantoms = new(10);
        if (this._placeholders == null)
            this._placeholders = new(10);
        for (int i = this._phantoms.Count; i < 10; ++i)
            this._phantoms.Add(null);
        for (int i = this._placeholders.Count; i < 10; ++i)
            this._placeholders.Add(null);
        for (int i = 0; i < 10; ++i)
        {
            if (!this._phantoms[i])
            {
                this._phantoms[i] = SpawnManager.SpawnVFX(_BilliardBallPhantom, base.transform.position, Quaternion.identity, ignoresPools: true);
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
                    ++this._chargeLevel;
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

    private const float _H_SPACE = 0.9375f;
    private const float _V_SPACE = 0.9375f;
    private Vector2 _BasePhantomOffset => this.gun.CurrentAngle.ToVector(_V_SPACE * 3f);
    private void UpdatePhantomBilliards()
    {
        if (this.PlayerOwner is not PlayerController player)
            return;
        Vector2 basePos = this.gun.barrelOffset.position;
        float baseAngle = this.gun.CurrentAngle;
        int i = 0;
        // arrange in a triangle
        Vector2 firstPos = _BasePhantomOffset;
        Vector2 baseVec;
        int d;
        float glow = 0.5f + Mathf.Max(-0.5f, Mathf.Sin(2f * Mathf.PI * (this._chargeTime / _CHARGE_PER_LEVEL)));
        for (d = 0; d < this._chargeLevel; ++d)
        {
            baseVec = firstPos + baseAngle.ToVector(_V_SPACE * d);
            for (int w = 0; w <= d; ++w)
            {
                Vector2 perpVec = (baseAngle + 90f).ToVector(_H_SPACE * ((0.5f * d) - w));
                Vector2 pos = basePos + baseVec + perpVec;
                this._placeholders[i].SetActive(false);
                this._phantoms[i].SetActive(true);
                this._phantoms[i].transform.position = pos;
                tk2dSprite sprite = this._phantoms[i].GetComponent<tk2dSprite>();
                sprite.PlaceAtScaledPositionByAnchor(pos, Anchor.MiddleCenter);
                sprite.renderer.material.SetFloat("_EmissivePower", _MAX_GLOW * glow);
                ++i;
            }
        }
        if (this._chargeLevel == _MAX_CHARGE_LEVEL)
            return;
        d = this._chargeLevel;
        baseVec = firstPos + baseAngle.ToVector(_V_SPACE * d);
        for (int w = 0; w <= d; ++w)
        {
            Vector2 perpVec = (baseAngle + 90f).ToVector(_H_SPACE * ((0.5f * d) - w));
            Vector2 pos = basePos + baseVec + perpVec;
            this._placeholders[i].SetActive(true);
            this._placeholders[i].transform.position = pos;
            this._placeholders[i].GetComponent<tk2dSprite>().PlaceAtScaledPositionByAnchor(pos, Anchor.MiddleCenter);
            ++i;
        }
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
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
                proj.baseData.speed = 0.01f;
            projObj.GetComponent<BilliardBall>().Setup(firedManually: false);
        }
    }

    public override void OnFullClipReload(PlayerController player, Gun gun)
    {
        base.OnFullClipReload(player, gun);
        // Cycle through 1, 3, 6, and 10 balls variations
    }

    public static BilliardBall NearestBilliardWithinRadiusAndAngle(BilliardBall me, BilliardBall other, float radius, float angle)
    {
        if (_ExtantBilliards.Count > 50)
        {
            // we have like a zillion balls on screen, so let's not pretend we're actually aiming
            // System.Console.WriteLine($"lolno");
            return null;
        }
        float sqrRadius = radius * radius;
        Vector2 pos = me._sprite.WorldCenter;
        BilliardBall best = null;
        float bestAngle = BilliardBall._MAX_ADJUST;
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

            if (bball == me || bball == other)
                continue; // don't adjust towards ourselves or the ball we just collided with
            if (bball._state != DEACTIVATED)
                continue; // don't adjust towards moving balls
            Vector2 bpos = bball._sprite.WorldCenter; // might not have a valid projectile, so use sprite instead
            Vector2 delta = (bpos - pos);
            if (delta.sqrMagnitude > sqrRadius)
                continue; // don't adjust towards balls that are too far
            float deltaAngle = delta.ToAngle();
            float relAngle = deltaAngle.AbsAngleTo(angle);
            if (relAngle > bestAngle)
                continue; // don't adjust at weird angles
            if (!pos.HasLineOfSight(bpos))
                continue; // don't adjust to balls we can't see
            bestAngle = relAngle;
            best = bball;
        }
        return best;
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

    private const float _FRICTION         = 0.975f; // friction to apply when moving around
    private const float _COLLISON_DAMPING = 0.9f;   // percent speed to keep on colliding with anything other than another ball
    private const float _MIN_SPEED        = 1.0f;   // minimum speed before being considered at rest
    private const int _MAX_COLLISIONS     = 10;     // number of times we can collide before disintegrating
    private const int _RESTORE_COLLISIONS = 3;      // if a billiard is at rest and activated, resets _remainingCollisions to this
    private const float _MERCY_TIME       = 0.5f;   // seconds before _MAX_COLLISIONS starts being counted
    private const float _ROT_RATE         = 90f;
    private const float _DAMAGE_SCALE     = 0.5f;
    internal const float _MAX_ADJUST      = 45f;    // adjust our angle by at most 45 degress to an actual enemy

    internal Projectile _projectile             = null;
    internal tk2dBaseSprite _sprite             = null;
    private PlayerController _owner             = null;
    private List<BilliardBall> _frameCollisions = new();
    internal State _state                       = UNSET;
    private float _baseDamage                   = 0.0f;
    private SpeculativeRigidbody _body          = null;
    private bool _setup                         = false;
    private int _remainingCollisions            = 0;
    private float _spawnTime                    = 0.0f;
    private float _rotation                     = 0.0f;

    private void Start()
    {
        Setup(firedManually: true);
        if (this._body.InsideWall())
            this._projectile.DieInAir(suppressInAirEffects: true, allowActorSpawns: false, allowProjectileSpawns: false, killedEarly: true);
    }

    public void Setup(bool firedManually)
    {
        if (this._setup)
            return;

        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        this._baseDamage = _DAMAGE_SCALE * this._projectile.baseData.damage;
        this._body = this._projectile.specRigidbody;
        this._sprite = this._projectile.sprite;
        this._remainingCollisions = _MAX_COLLISIONS;
        this._spawnTime = BraveTime.ScaledTimeSinceStartup;
        this._rotation = 360f * UnityEngine.Random.value;

        this._projectile.BulletScriptSettings.surviveRigidbodyCollisions = true;
        this._projectile.BulletScriptSettings.surviveTileCollisions = true;

        this._body.OnRigidbodyCollision += this.OnRigidbodyCollision;
        this._body.OnTileCollision += this.OnTileCollision;
        this._body.OnRigidbodyCollision += this.OnAnyCollision;
        this._body.OnTileCollision += this.OnAnyCollision;
        this._body.OnPreRigidbodyCollision += this.OnPreCollision;

        if (firedManually)
        {
            this.Activate();
            this._state = FIRED;
        }
        else
            this.Deactivate();
        English.AddExtantBilliard(this);

        this._setup = true;
    }

    // Skip collisions if we were just fired and haven't hit another ball yet
    private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (this._state != FIRED || (BraveTime.ScaledTimeSinceStartup - this._spawnTime) > _MERCY_TIME)
        {
            this._body.OnPreRigidbodyCollision -= this.OnPreCollision;
            return;
        }
        if (otherRigidbody.gameObject.GetComponent<BilliardBall>() is not BilliardBall)
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

    private void OnTileCollision(CollisionData collision)
    {
        // TODO: refactor since this is used by a lot of projectiles at this point
        SpeculativeRigidbody body = collision.MyRigidbody;
        Vector2 newVel            = body.Velocity;
        if (collision.CollidedX)
          newVel = newVel.WithX(-newVel.x);
        if (collision.CollidedY)
          newVel = newVel.WithY(-newVel.y);
        newVel *= _COLLISON_DAMPING;
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

        // TODO: refactor since this is used by a lot of projectiles at this point
        SpeculativeRigidbody body = collision.MyRigidbody;
        Vector2 newVel            = body.Velocity;
        if (collision.CollidedX)
          newVel = newVel.WithX(-newVel.x);
        if (collision.CollidedY)
          newVel = newVel.WithY(-newVel.y);
        newVel *= _COLLISON_DAMPING;
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

        float newSpeed = Mathf.Sqrt(Mathf.Max(v1.sqrMagnitude, v2.sqrMagnitude, newv1.sqrMagnitude, newv2.sqrMagnitude));

        PhysicsEngine.PostSliceVelocity = newv1;
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
        //NOTE: too slow, and barely helpful
        // if (English.NearestBilliardWithinRadiusAndAngle(self, other, _MAX_RADIUS, origAngle) is BilliardBall bb)
        //     return (bb._sprite.WorldCenter - pos);
        return origDir;
    }

    private void Update()
    {
        this._frameCollisions.Clear();
        if (this._state != FIRED && this._state != ACTIVATED)
            return;

        this._projectile.ApplyFriction(_FRICTION);
        float speed = this._projectile.baseData.speed;
        if (speed < _MIN_SPEED)
        {
            this.Deactivate();
            return;
        }

        this._rotation += Mathf.Sign(this._projectile.specRigidbody.Velocity.x) * _ROT_RATE * this._projectile.baseData.speed * BraveTime.DeltaTime;
        this._projectile.transform.localRotation = this._rotation.EulerZ();

        if (this._state == ACTIVATED)
            this._projectile.baseData.damage = this._baseDamage * Mathf.Log(speed, 3);
        else if (this._state == FIRED)
            this._projectile.baseData.damage = this._baseDamage;
    }

    internal void UpdateCollisionStatus(RoomHandler room)
    {
        if (!this._body)
            return;

        Vector2 pos = this._projectile.SafeCenter;
        bool inSameRoom = room == pos.GetAbsoluteRoom();
        if (!inSameRoom && !pos.OnScreen(leeway: 2f))
        {
            UnityEngine.Object.Destroy(base.gameObject);
            return;
        }
        this._body.enabled = this._projectile ? inSameRoom : false;
    }

    private void AddTrailIfMissing()
    {
        if (this._projectile.GetComponent<EasyTrailBullet>())
            return;

        EasyTrailBullet trail = this._projectile.gameObject.AddComponent<EasyTrailBullet>();
            // trail.TrailPos   = p.transform.position.XY() + new Vector2(5f / C.PIXELS_PER_TILE, 5f / C.PIXELS_PER_TILE); // offset by middle of the sprite
            trail.StartWidth = 0.25f;
            trail.EndWidth   = 0.05f;
            trail.LifeTime   = 0.1f;
            trail.BaseColor  = Color.white;
            trail.StartColor = Color.white;
            trail.EndColor   = Color.Lerp(Color.white, Color.clear, 0.5f);
    }

    private void Activate()
    {
        if (this._state == ACTIVATED)
            return;

        this._projectile.enabled = true;
        this._body.CollideWithTileMap = true;
        this._projectile.collidesWithEnemies = true;
        this._projectile.UpdateCollisionMask();
        if (this._remainingCollisions < _RESTORE_COLLISIONS)
            this._remainingCollisions = _RESTORE_COLLISIONS;
        AddTrailIfMissing();
        this._state = ACTIVATED;
    }

    private void Deactivate()
    {
        if (this._state == DEACTIVATED)
            return;

        this._body.Velocity = Vector2.zero;
        this._body.CollideWithTileMap = false;
        this._projectile.collidesWithEnemies = false;
        this._projectile.UpdateCollisionMask();
        this._projectile.enabled = false;
        this._state = DEACTIVATED;
    }
}
