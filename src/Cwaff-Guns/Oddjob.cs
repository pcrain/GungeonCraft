namespace CwaffingTheGungy;

public class Oddjob : CwaffGun
{
    public static string ItemName         = "Oddjob";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Oddjob>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 0.9f, ammo: 600, shootFps: 14, reloadFps: 4,
                muzzleFrom: Items.Mailbox, fireAudio: "paintball_shoot_sound", reloadAudio: "paintball_reload_sound");

        gun.InitProjectile(GunData.New(sprite: "oddjob_projectile", clipSize: 12, cooldown: 0.18f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 9.0f, speed: 40f, range: 9999f, force: 12f, hitEnemySound: "paintball_impact_enemy_sound", hitWallSound: "paintball_impact_wall_sound",
            shouldRotate: false))
          .Attach<PierceProjModifier>(pierce => { pierce.penetration = 100; pierce.penetratesBreakables = true; })
          .Attach<OddjobProjectile>();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        //
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        //
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        //
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        //
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        //
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        //
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
        {
            //
        }
        base.OnDestroy();
    }

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner || !this.PlayerOwner.AcceptingNonMotionInput)
            return;

        //
    }
}

public class OddjobProjectile : MonoBehaviour
{
    private void Start()
    {
        Projectile p = base.GetComponent<Projectile>();
        p.specRigidbody.CollideWithTileMap = false;
        p.OverrideMotionModule = new OddjobProjectileMotionModule();
    }
}

// modified from HelixProjectileMotionModule
public class OddjobProjectileMotionModule : ProjectileMotionModule
{
    public bool ForceInvert;

    private bool _initialized;
    private Vector2 _initialRightVector;
    private Vector2 _initialUpVector;
    private Vector2 _privateLastPosition;
    private float _xDisplacement;
    private float _yDisplacement;

    private AIActor _nextEnemyInPath = null;
    private Vector2 _circleCenter;
    private float _circleRadius;
    private float _circleSqrRadius;
    private float _circleCircum;
    private float _startAngleFromCenter;
    private float _curAngleFromCenter;
    private float _circleTraveled;
    private RoomHandler _startRoom;
    private float _returnTime;

    private void ResetAngle(float angleDiff)
    {
        if (float.IsNaN(angleDiff))
            return;

        Quaternion q        = Quaternion.Euler(0f, 0f, angleDiff);
        _initialUpVector    = q * _initialUpVector;
        _initialRightVector = q * _initialRightVector;
    }

    public override void UpdateDataOnBounce(float angleDiff) => ResetAngle(angleDiff);
    public override void AdjustRightVector(float angleDiff) => ResetAngle(angleDiff);

    private const float _MIN_TOSS_RADIUS       = 3f;    // min radius of circle hat orbits around
    private const float _MAX_TOSS_RADIUS       = 10f;   // max radius of circle hat orbits around
    private const float _NO_TARGET_TOSS_RADIUS = 5f;    // default radius of circle hat orbits around
    private const float _SCAN_DELTA            = 45f;   // distance from facing angle the projectile will scan for enemies when created
    private const float _MAX_SQR_DEVIATION     = 16f;   // square of max amount projectile will deviate from circular path to home in on enemies
    private const float _RADIAL_SHIFT_RATE     = 8f;    // rate at which projectile shifts along radial path lines, in units per second
    private const float _RETURN_PERCENT        = 0.75f; // percent of path to complete before trying to return to the player
    private const float _MAX_RETURN_TIME       = 1.0f;  // time to wait before teleporting to the player after traveling
    private const float _MAX_RETURN_SQR_RADIUS = 1.0f;  // distance from player the projectile needs to be to vanish and restore ammo

    private void Initialize(Vector2 lastPosition, Transform projectileTransform, float m_timeElapsed, Vector2 m_currentDirection, bool shouldRotate)
    {
        // ETGModConsole.Log($"thrown from {lastPosition} at angle {m_currentDirection.ToAngle()}");
        _privateLastPosition = lastPosition;
        _initialRightVector  = ((!shouldRotate) ? m_currentDirection : projectileTransform.right.XY());
        _initialUpVector     = ((!shouldRotate) ? (Quaternion.Euler(0f, 0f, 90f) * m_currentDirection) : projectileTransform.up);
        _initialized         = true;
        _xDisplacement       = 0f;
        _yDisplacement       = 0f;

        float aimAngle = _initialRightVector.ToAngle();
        // TODO: figure out if distance or angle is better for target tracking; both have their pros and cons
        AIActor firstEnemy = Lazy.NearestEnemyWithinConeOfVision(lastPosition, aimAngle, _SCAN_DELTA,
          useNearestAngleInsteadOfDistance: false, maxDistance: 2f * _MAX_TOSS_RADIUS, ignoreWalls: true);
        if (firstEnemy)
            _circleCenter = lastPosition + aimAngle.ToVector(0.5f * (firstEnemy.CenterPosition - lastPosition).magnitude);
        else
            _circleCenter = lastPosition + _NO_TARGET_TOSS_RADIUS * _initialRightVector.normalized;

        // Lazy.DebugLog($"  center at {_circleCenter}");
        Vector2 centerDelta = lastPosition - _circleCenter;
        _circleRadius = centerDelta.magnitude;
        _circleSqrRadius = _circleRadius * _circleRadius;
        // Lazy.DebugLog($"  radius is {_circleRadius}");
        _circleCircum = 2f * Mathf.PI * _circleRadius;
        // Lazy.DebugLog($"  circum is {_circleCircum}");
        _startAngleFromCenter = centerDelta.ToAngle();
        // Lazy.DebugLog($"  startAngle is {_startAngleFromCenter}");
        _curAngleFromCenter = _startAngleFromCenter;
        _circleTraveled = 0f;
        _startRoom = lastPosition.GetAbsoluteRoom();

        /* THE PLAN:
            - figure out position of enemy directly in front of where player is aiming
            - get midpoint between player and that enemy, and make that the center of a circle with radius equal to distance to player / target
            - calculate angle to center of circle
            - calculate circumference of circle
            - calculate time to travel intended path based on projectile speed and circumference of circle
            - at each time step while traveling:
              - figure out where the hat *should* be at any given point in time if it were following its path, and make that our target position
              - figure out the nearest enemy along the current path that is 1) within x units of the circumference, and 2) hasn't been passed yet
              - adjust our target position closer / farther from the center to match the radius of the target enemy, if any
              - move the projectile towards its new target
        */
    }

    private bool NextEnemyInPathIsReachable(float targetAngle, bool clockwise)
    {
        if (!_nextEnemyInPath || !_nextEnemyInPath.healthHaver || !_nextEnemyInPath.healthHaver.IsAlive)
            return false;
        return EnemyIsReachable(_nextEnemyInPath, targetAngle, clockwise, out _);
    }

    private bool EnemyIsReachable(AIActor actor, float targetAngle, bool clockwise, out float angularDistance)
    {
        angularDistance = 0f;
        Vector2 deltaFromCenter = actor.CenterPosition - _circleCenter;
        float sqrRadius = deltaFromCenter.sqrMagnitude;
        if (Mathf.Abs(_circleSqrRadius - sqrRadius) > _MAX_SQR_DEVIATION)
            return false;
        angularDistance = (clockwise ? 1 : -1) * (deltaFromCenter.ToAngle() - targetAngle).Clamp180();
        return angularDistance > 0f && angularDistance < 90f;
    }

    private AIActor FindUpcomingTargets(float targetAngle, bool clockwise)
    {
        if (_startRoom == null)
            return null;
        if (NextEnemyInPathIsReachable(targetAngle, clockwise))
            return _nextEnemyInPath;

        _nextEnemyInPath = null;
        float bestDistance = 360f;
        foreach (AIActor enemy in _startRoom.SafeGetEnemiesInRoom())
        {
            if (!EnemyIsReachable(enemy, targetAngle, clockwise, out float angleDelta))
                continue;
            if (angleDelta > bestDistance)
                continue;
            bestDistance = angleDelta;
            _nextEnemyInPath = enemy;
        }
        return _nextEnemyInPath;
    }

    public override void Move(Projectile source, Transform projectileTransform, tk2dBaseSprite projectileSprite, SpeculativeRigidbody specRigidbody, ref float m_timeElapsed, ref Vector2 m_currentDirection, bool Inverted, bool shouldRotate)
    {
        ProjectileData baseData = source.baseData;
        Vector2 curPos = ((!projectileSprite) ? projectileTransform.position.XY() : projectileSprite.WorldCenter);
        if (!_initialized)
            Initialize(curPos, projectileTransform, m_timeElapsed, m_currentDirection, shouldRotate);

        m_timeElapsed   += BraveTime.DeltaTime;
        _circleTraveled += BraveTime.DeltaTime * baseData.speed;
        float percentDone = _circleTraveled / _circleCircum;
        if (percentDone > _RETURN_PERCENT)
        {
            if (!source.Owner || ((_returnTime += BraveTime.DeltaTime) > _MAX_RETURN_TIME))
            {
                source.DieInAir();
                return;
            }
            Vector2 ownerDelta = (source.Owner.CenterPosition - curPos);
            if (ownerDelta.sqrMagnitude < _MAX_RETURN_SQR_RADIUS)
            {
                source.DieInAir();
                return;
            }
            // Accelerate more aggressively towards player as time goes on
            specRigidbody.Velocity = Lazy.SmoothestLerp(
                specRigidbody.Velocity,
                baseData.speed * ownerDelta.normalized,
                5f + 10f * (_returnTime / _MAX_RETURN_TIME));
            return;
        }


        // find enemies that are close to our path and coming up ahead
        float targetAngle = _startAngleFromCenter + 360f * percentDone;
        FindUpcomingTargets(targetAngle: targetAngle, clockwise: !Inverted);

        // determine whether we need to adjust our radius from the center based on the enemy's distance
        float targetRadius = _circleRadius;
        if (_nextEnemyInPath)
            targetRadius = (_nextEnemyInPath.CenterPosition - _circleCenter).magnitude;
        float curRadius = (curPos - _circleCenter).magnitude;
        float maxShift = _RADIAL_SHIFT_RATE * BraveTime.DeltaTime;
        float nextRadius = curRadius + Mathf.Clamp(targetRadius - curRadius, -maxShift, maxShift);
        // if (Mathf.Abs(nextRadius - _circleRadius) > 0.001f)
        //     System.Console.WriteLine($"adjusting radius to {nextRadius - _circleRadius}");

        // actually determine our target position based on target angle and radius, and update velocity
        Vector2 targetPos = _circleCenter + (Inverted ? -targetAngle : targetAngle).ToVector(nextRadius);
        Vector2 velocity = (targetPos - curPos) / BraveTime.DeltaTime;
        specRigidbody.Velocity = velocity;
    }

    public override void SentInDirection(ProjectileData baseData, Transform projectileTransform, tk2dBaseSprite projectileSprite, SpeculativeRigidbody specRigidbody, ref float m_timeElapsed, ref Vector2 m_currentDirection, bool shouldRotate, Vector2 dirVec, bool resetDistance, bool updateRotation)
    {
        Initialize(((!projectileSprite) ? projectileTransform.position.XY() : projectileSprite.WorldCenter), projectileTransform, m_timeElapsed, m_currentDirection, shouldRotate);
    }
}
