namespace CwaffingTheGungy;

public class Oddjob : CwaffGun
{
    public static string ItemName         = "Oddjob";
    public static string ShortDescription = "Hat Tricks";
    public static string LongDescription  = "Travels in a circular arc towards the enemy closest to the player's line of sight, sawing through anything in its path before returning to the player. Cannot be switched out or dropped while in flight. Increases curse by 1 while in inventory.";
    public static string Lore             = "A hat that once belonged to an aggravatingly short gunman. One would think from his profession that he lost his life either in a gun fight or from accidentally decapitating himself with his hat, but the reality is he was done in by another gungeoneer that left their Battery Bullets lying around near a water barrel.";

    internal static GameObject _Sparks = null;
    internal static Hat _OddjobHat = null;
    internal static Projectile _OddjobFlakProjectile = null;

    private Projectile _extantOddjobProj = null;
    private string _capiHatName = null;

    public static void Init()
    {
        Lazy.SetupGun<Oddjob>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.A, gunClass: GunClass.SILLY, reloadTime: 0.0f, ammo: 1, shootFps: 60, reloadFps: 4,
            muzzleFrom: Items.Mailbox, canGainAmmo: false, suppressReloadLabel: true, curse: 1f, carryOffset: new IntVector2(3, 11),
            onlyUsesIdleInWeaponBox: true)
          .Attach<Unthrowable>()
          .InitProjectile(GunData.New(sprite: "oddjob_projectile", clipSize: 1, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 25.0f, speed: 40f, range: 9999f, force: 12f, hitEnemySound: "paintball_impact_enemy_sound",
            hitWallSound: "paintball_impact_wall_sound", shouldRotate: false, fps: 30, preventOrbiting: true))
          .Attach<PierceProjModifier>(pierce => { pierce.penetration = 100; pierce.penetratesBreakables = true; })
          .Attach<OddjobProjectile>();

      _Sparks = VFX.Create("oddjob_sparks");
      _OddjobHat = CwaffHats.EasyHat(name: "oddjob_hat", offset: new IntVector2(0, -3), excluded: true);

      _OddjobFlakProjectile = Items.Ak47.CloneProjectile(GunData.New(sprite: "oddjob_flak_projectile", angleVariance: 0.0f,
          speed: 20f, damage: 6f, shouldRotate: false, spinRate: 2160f, glowAmount: 10f))
        .SetAllImpactVFX(VFX.CreatePool("oddjob_flak_impact", fps: 20, loops: false));
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (!projectile.FiredForFree())
        {
            this._extantOddjobProj = projectile;
            RemoveHatFromHead();
        }
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        PutHatOnHead();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        RemoveHatFromHead();
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        player.OnTriedToInitiateAttack -= this.OnTriedToInitiateAttack;
        player.OnTriedToInitiateAttack += this.OnTriedToInitiateAttack;
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        RemoveHatFromHead();
        player.OnTriedToInitiateAttack -= this.OnTriedToInitiateAttack;
    }

    private void ReturnHatProjectile()
    {
        if (this._extantOddjobProj)
            this._extantOddjobProj.DieInAir();
        this.gun.CurrentAmmo = 1;
        this.gun.MoveBulletsIntoClip(1);
    }

    private void PutHatOnHead()
    {
        if (this.PlayerOwner is not PlayerController player)
            return;
        if (player.gameObject.GetComponent<HatController>() is not HatController hc)
            return;
        if (player.CurrentHat() is Hat hat)
        {
            if (hat.hatName == _OddjobHat.hatName)
                return;
            this._capiHatName = hat.hatName.GetDatabaseFriendlyHatName();
        }
        hc.SetHat(_OddjobHat);
    }

    private void RemoveHatFromHead()
    {
        if (this.PlayerOwner is not PlayerController player)
            return;
        if (player.gameObject.GetComponent<HatController>() is not HatController hc)
            return;
        if (hc.CurrentHat is Hat curHat && curHat.hatName == _OddjobHat.hatName)
            hc.RemoveCurrentHat();
        if (string.IsNullOrEmpty(this._capiHatName))
            return;
        if (Hatabase.Hats.TryGetValue(this._capiHatName, out Hat origHat))
            hc.SetHat(origHat);
        this._capiHatName = null;
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
        {
            this.PlayerOwner.OnTriedToInitiateAttack -= this.OnTriedToInitiateAttack;
            RemoveHatFromHead();
        }
        base.OnDestroy();
    }

    public override void Update()
    {
        base.Update();
        bool gunSpriteEnabled = (!this.PlayerOwner);
        if (gunSpriteEnabled != this.gun.sprite.renderer.enabled)
        {
            this.gun.sprite.renderer.enabled = gunSpriteEnabled;
            if (gunSpriteEnabled)
                SpriteOutlineManager.AddOutlineToSprite(this.gun.sprite, Color.black, 0.2f, 0.05f);
            else if (SpriteOutlineManager.HasOutline(this.gun.sprite))
                SpriteOutlineManager.RemoveOutlineFromSprite(this.gun.sprite);
        }
        if (!this.PlayerOwner)
            return;

        bool holdingHat = !this._extantOddjobProj;
        this.gun.CanBeDropped = holdingHat;
        this.gun.CanBeSold = holdingHat;
        this.PlayerOwner.inventory.GunLocked.SetOverride(ItemName, !holdingHat);
        if (holdingHat && this.gun.CurrentAmmo == 0)
        {
            ReturnHatProjectile();
            PutHatOnHead();
            SpawnManager.SpawnVFX(Breegull._TalonDust, this.PlayerOwner.sprite.WorldTopCenter, Quaternion.identity);
            this.gun.PlayIdleAnimation();
        }
    }

    private void OnTriedToInitiateAttack(PlayerController player)
    {
        if (!player || player.CurrentGun != this.gun)
            return; // inactive, do normal firing stuff
        if (this._extantOddjobProj)
            player.SuppressThisClick = true; // can't fire more than one hat at once
    }
}

public class OddjobProjectile : MonoBehaviour
{
    private const float _SPARK_RATE = 0.05f;
    private const float _FLAK_TIMER = 0.15f;
    private const int _FLAK_COUNT = 5;
    private const float _FLAK_SPACING = 360f / _FLAK_COUNT;

    internal bool _collidedLastFrame = false;
    internal bool _returning = false;

    private Projectile _proj;
    private PlayerController _owner = null;
    private float _lastSparkTime = 0;
    private bool _mastered = false;
    private float _nextFlakTime = 0;
    private List<HealthHaver> _hitThisFrame = new();
    private List<HealthHaver> _hitLastFrame = new();

    private void Start()
    {
        this._proj = base.GetComponent<Projectile>();
        this._proj.specRigidbody.CollideWithTileMap = false;
        this._proj.specRigidbody.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
        OddjobProjectileMotionModule motionModule = new OddjobProjectileMotionModule();
        this._proj.OverrideMotionModule = motionModule;
        if (this._proj.Owner is PlayerController player)
        {
            this._owner = player;
            motionModule.ForceInvert = player.SpriteFlipped;
            this._mastered = player.HasSynergy(Synergy.MASTERY_ODDJOB);
            this._nextFlakTime = BraveTime.ScaledTimeSinceStartup + _FLAK_TIMER;
        }
        else
            motionModule.ForceInvert = Lazy.CoinFlip();

        EasyTrailBullet trail = base.gameObject.AddComponent<EasyTrailBullet>();
            trail.StartWidth = 0.5f;
            trail.EndWidth   = 0.05f;
            trail.LifeTime   = 0.1f;
            trail.BaseColor  = new Color(0.5f, 0.5f, 0.5f);
            trail.StartColor = Color.Lerp(trail.BaseColor, Color.white, 0.5f);
            trail.EndColor   = trail.BaseColor;

        this._proj.sprite.HeightOffGround = 4f;
        this._proj.sprite.UpdateZDepth();
    }

    private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (!this._proj || otherRigidbody.gameActor is not AIActor enemy)
            return;
        PhysicsEngine.SkipCollision = true;
        if (this._returning || !enemy.healthHaver || enemy.healthHaver.IsDead)
            return;

        this._hitThisFrame.Add(enemy.healthHaver);
        if (!enemy.healthHaver.IsBoss && !enemy.healthHaver.IsSubboss)
        {
            enemy.ClearPath();
            if (enemy.behaviorSpeculator.IsInterruptable)
                enemy.behaviorSpeculator.Interrupt();
            enemy.behaviorSpeculator.Stun(1f);
        }
        this._collidedLastFrame = true;
        float now = BraveTime.ScaledTimeSinceStartup;
        if ((now - this._lastSparkTime) >= _SPARK_RATE)
        {
            this._lastSparkTime = now;
            CwaffVFX.SpawnBurst(prefab: Oddjob._Sparks, numToSpawn: 10, basePosition: myRigidbody.UnitBottomCenter, height: 4f,
              minVelocity: 10f, velocityVariance: 5f, rotType: CwaffVFX.Rot.Random, lifetime: 0.2f, fadeOutTime: 0.05f);
        }
    }

    private void OnDestroy()
    {
        this.LoopSoundIf(false, "oddjob_saw_sound");
        this._hitThisFrame.Clear();
        UpdateCollisionData();
    }

    private void UpdateCollisionData()
    {
        foreach (HealthHaver hh in this._hitLastFrame)
            if (hh && !hh.IsDead && hh.IsVulnerable && !this._hitThisFrame.Contains(hh))
            {
                hh.ApplyDamage(this._proj.baseData.damage, Vector2.zero, this._proj.OwnerName);
                if (hh.specRigidbody)
                    this._proj.specRigidbody.RegisterTemporaryCollisionException(hh.specRigidbody, 0.1f);
            }
        BraveUtility.Swap(ref this._hitThisFrame, ref this._hitLastFrame);
        this._hitThisFrame.Clear();
        this._collidedLastFrame = false;
    }

    private void Update()
    {
        if (BraveTime.DeltaTime == 0.0f)
            return;

        this.LoopSoundIf(this._collidedLastFrame, "oddjob_saw_sound");
        this.LoopSoundIf(!this._collidedLastFrame, "oddjob_spin_sound");
        UpdateCollisionData();

        if (!this._mastered)
            return;

        float now = BraveTime.ScaledTimeSinceStartup;
        if (now < this._nextFlakTime)
            return;

        this._nextFlakTime = now + _FLAK_TIMER;
        float offset = 360f * UnityEngine.Random.value;
        for (int i = 0; i < _FLAK_COUNT; ++i)
        {
            Projectile proj = SpawnManager.SpawnProjectile(
                prefab   : Oddjob._OddjobFlakProjectile.gameObject,
                position : this._proj.SafeCenter,
                rotation : (offset + i * _FLAK_SPACING).EulerZ()).GetComponent<Projectile>();
            //REFACTOR: combine the next few lines into the base function throughout the code base
            proj.collidesWithPlayer  = false;
            proj.collidesWithEnemies = true;
            proj.SetOwnerAndStats(this._owner);
            //REFACTOR: end of refactor
            foreach (HealthHaver hh in this._hitLastFrame)
                if (hh.specRigidbody is SpeculativeRigidbody body)
                    proj.specRigidbody.RegisterSpecificCollisionException(body);
        }
        base.gameObject.Play("oddjob_flak_sound");
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
    private OddjobProjectile _oddProj;

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
    private const float _MAX_RETURN_SQR_RADIUS = 0.2f;  // square distance from player the projectile needs to be to vanish and restore ammo
    private const float _SAW_SPEED             = 0.1f;  // fractional speed the projectile moves when actively colliding with an enemy

    private void Initialize(Vector2 lastPosition, Transform projectileTransform, float m_timeElapsed, Vector2 m_currentDirection, bool shouldRotate)
    {
        _privateLastPosition = lastPosition;
        _initialRightVector  = ((!shouldRotate) ? m_currentDirection : projectileTransform.right.XY());
        _initialUpVector     = ((!shouldRotate) ? (Quaternion.Euler(0f, 0f, 90f) * m_currentDirection) : projectileTransform.up);
        _initialized         = true;
        _xDisplacement       = 0f;
        _yDisplacement       = 0f;

        float aimAngle = _initialRightVector.ToAngle();
        AIActor firstEnemy = Lazy.NearestEnemyWithinConeOfVision(lastPosition, aimAngle, _SCAN_DELTA,
          useNearestAngleInsteadOfDistance: false, maxDistance: 2f * _MAX_TOSS_RADIUS, ignoreWalls: true);
        if (firstEnemy)
            _circleCenter = lastPosition + aimAngle.ToVector(0.5f * (firstEnemy.CenterPosition - lastPosition).magnitude);
        else
            _circleCenter = lastPosition + _NO_TARGET_TOSS_RADIUS * _initialRightVector.normalized;

        Vector2 centerDelta = lastPosition - _circleCenter;
        _circleRadius = centerDelta.magnitude;
        _circleSqrRadius = _circleRadius * _circleRadius;
        _circleCircum = 2f * Mathf.PI * _circleRadius;
        _startAngleFromCenter = centerDelta.ToAngle();
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
        if (!this._oddProj)
        {
            this._oddProj = source.gameObject.GetComponent<OddjobProjectile>();
            if (!this._oddProj)
            {
                Lazy.RuntimeWarn("Moving Oddjob projectile without a projectile!");
                return;
            }
        }

        ProjectileData baseData = source.baseData;
        Vector2 curPos = ((!projectileSprite) ? projectileTransform.position.XY() : projectileSprite.WorldCenter);
        if (!_initialized)
            Initialize(curPos, projectileTransform, m_timeElapsed, m_currentDirection, shouldRotate);

        m_timeElapsed   += BraveTime.DeltaTime;
        float travelRate = this._oddProj._collidedLastFrame ? _SAW_SPEED : 1.0f;
        _circleTraveled += BraveTime.DeltaTime * baseData.speed * travelRate;
        float percentDone = _circleTraveled / _circleCircum;
        if (percentDone > _RETURN_PERCENT && !this._oddProj._collidedLastFrame)
        {
            this._oddProj._returning = true;
            if (!source.Owner || ((_returnTime += BraveTime.DeltaTime) > _MAX_RETURN_TIME))
            {
                source.DieInAir();
                return;
            }
            Vector2 ownerDelta = (source.Owner.sprite.WorldTopCenter - curPos);
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
        bool clockwise = Inverted == ForceInvert;
        float targetAngle = _startAngleFromCenter + (clockwise ? 360f : -360f) * percentDone;
        FindUpcomingTargets(targetAngle: targetAngle, clockwise: clockwise);

        // determine whether we need to adjust our radius from the center based on the enemy's distance
        float targetRadius = _circleRadius;
        if (_nextEnemyInPath)
            targetRadius = (_nextEnemyInPath.CenterPosition - _circleCenter).magnitude;
        float curRadius = (curPos - _circleCenter).magnitude;
        float maxShift = _RADIAL_SHIFT_RATE * BraveTime.DeltaTime;
        float nextRadius = curRadius + Mathf.Clamp(targetRadius - curRadius, -maxShift, maxShift);

        // actually determine our target position based on target angle and radius, and update velocity
        Vector2 targetPos = _circleCenter + targetAngle.ToVector(nextRadius);
        Vector2 velocity = (targetPos - curPos) / BraveTime.DeltaTime;
        specRigidbody.Velocity = velocity;
    }

    public override void SentInDirection(ProjectileData baseData, Transform projectileTransform, tk2dBaseSprite projectileSprite, SpeculativeRigidbody specRigidbody, ref float m_timeElapsed, ref Vector2 m_currentDirection, bool shouldRotate, Vector2 dirVec, bool resetDistance, bool updateRotation)
    {
        Initialize(((!projectileSprite) ? projectileTransform.position.XY() : projectileSprite.WorldCenter), projectileTransform, m_timeElapsed, m_currentDirection, shouldRotate);
    }
}
