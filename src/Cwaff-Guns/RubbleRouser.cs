namespace CwaffingTheGungy;

public class RubbleRouser : CwaffGun
{
    public static string ItemName         = "Rubble Rouser";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const float _MIN_CHARGE_TIME     = 0.8f;
    private const float _FIXED_RANGE         = 10f;
    private const float _SOUND_TIMER         = 0.16f;
    private const float _SOUND_TIMER_CHARGED = 0.07f;

    internal static GameObject _RockDebris = null;

    private float _nextSoundTime = 0.0f;

    public static void Init()
    {
        Lazy.SetupGun<RubbleRouser>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.CHARGE, reloadTime: 0.0f, ammo: 90, shootFps: 16, chargeFps: 30,
            attacksThroughWalls: true, carryOffset: new IntVector2(6, 4), fireAudio: "rubble_rouser_fire_sound", loopChargeAt: 2, dynamicBarrelOffsets: true,
            muzzleVFX: "rubble_rouser_muzzle", muzzleFps: 20, muzzleScale: 0.75f, muzzleAnchor: Anchor.MiddleCenter, muzzleEmission: 0.1f,
            muzzleEmissionColor: Color.white, muzzleOrphaned: true)
          .AddReticle<CwaffReticle>(
            reticleVFX : VFX.Create("rubble_rouser_target_reticle", fps: 12, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 10, emissiveColour: Color.cyan),
            reticleAlpha: 0.25f, fadeInTime : _MIN_CHARGE_TIME, fadeOutTime : 0.25f, minDistance : _FIXED_RANGE, maxDistance : _FIXED_RANGE,
            controllerScale : 1f + _FIXED_RANGE, visibility : CwaffReticle.Visibility.CHARGING, background: true, rotateSpeed: 90f)
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(sprite: "rubble_rouser_projectile", clipSize: 1, shootStyle: ShootStyle.Charged, customClip: true,
            damage: 200f, chargeTime: _MIN_CHARGE_TIME, force: 200f, speed: 45.0f, angleVariance: 3f, shouldRotate: false, preventOrbiting: true))
          .Attach<RubbleRouserProjectile>()
          .Assign(out Projectile proj);

        _RockDebris = BreakableAPIToolbox.GenerateDebrisObject(
            shardSpritePath         : $"groundhog_rock_vfx1",
            debrisObjectsCanRotate  : true,
            LifeSpanMin             : 1,
            LifeSpanMax             : 1,
            AngularVelocity         : 0,
            AngularVelocityVariance : 90,
            DebrisBounceCount       : 5).gameObject;
    }

    public static float GetStaticLaunchDistance() => _FIXED_RANGE;

    public float GetLaunchDistance() => _FIXED_RANGE;

    public override void Update()
    {
        base.Update();
        if (this.PlayerOwner is not PlayerController player)
            return;
        if (BraveTime.DeltaTime == 0.0f)
            return;
        if (!this.gun.IsCharging)
            return;
        float now = BraveTime.ScaledTimeSinceStartup;
        if (now > this._nextSoundTime)
        {
          this._nextSoundTime = now + (this.gun.HasChargedProjectileReady ? _SOUND_TIMER_CHARGED : _SOUND_TIMER);
          base.gameObject.Play("rubble_rouser_charge_sound");
        }
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        projectile.GetComponent<RubbleRouserProjectile>().Setup(this);
    }
}

public class RubbleRouserProjectile : MonoBehaviour
{
    private const float _ARC_HEIGHT          = 6.0f;
    private const float _CRASH_RADIUS        = 3.0f;
    private const float _NUM_FLIPS           = 2.0f;
    private const float _SPECIAL_ROCK_CHANCE = 0.05f;

    private Projectile _projectile     = null;
    private bool _naturalSpawn         = false;
    private RubbleRouser _rr           = null;
    private Vector2 _startPos          = default;
    private Vector2 _halfwayPos        = default;
    private Vector2 _landingPos        = default;
    private float _travelTime          = 0.0f;
    private float _lifetime            = 0.0f;
    private SpeculativeRigidbody _body = null;

    public void Setup(RubbleRouser rr)
    {
        this._naturalSpawn = true;
        this._rr           = rr;
    }

    private void Start()
    {
        // set up sprites
        this._projectile = base.GetComponent<Projectile>();
        if (UnityEngine.Random.value < _SPECIAL_ROCK_CHANCE)
          this._projectile.spriteAnimator.PickFrame();
        else
          this._projectile.spriteAnimator.PickFrame(0);
        // roughly estimate some initial positions
        float launchAngle = this._projectile.Direction.ToAngle();
        float launchDistance = (this._naturalSpawn && this._rr) ? this._rr.GetLaunchDistance() : RubbleRouser.GetStaticLaunchDistance();
        this._startPos = this._projectile.transform.position;
        this._landingPos = this._startPos + launchAngle.ToVector(launchDistance);
        // bounds-check our trajectory so we aren't going out of bounds
        PerformBoundsChecks(launchAngle);
        // determine our overall travel time
        this._halfwayPos = 0.5f * (this._startPos + this._landingPos);
        this._travelTime = Mathf.Clamp(30.0f / Mathf.Max(1f, this._projectile.baseData.speed), 0.2f, 0.8f);

        this._projectile.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Unoccluded"));
        this._projectile.damageTypes &= (~CoreDamageTypes.Electric); // remove robot's electric damage type from the projectile
        this._body = this._projectile.specRigidbody;
        this._projectile.collidesWithEnemies = false;
        this._body.CollideWithOthers = false;
        this._body.CollideWithTileMap = false;
        this._projectile.OnDestruction += OnProjectileDestruction;
    }

    private void PerformBoundsChecks(float launchAngle)
    {
      RoomHandler startRoom = this._startPos.GetAbsoluteRoom();
      // if our target is in bounds, we're good
      if (this._landingPos.InBounds() && startRoom == this._landingPos.GetAbsoluteRoom())
        return;
      // if our midpoint is in bounds, raycast from there to our landing pos until we hit a wall; otherwise, start from the starting point
      Vector2 midpoint = 0.5f * (this._startPos + this._landingPos);
      Vector2 castpoint = (midpoint.InBounds() && startRoom == midpoint.GetAbsoluteRoom()) ? midpoint : this._startPos;
      this._landingPos = castpoint.ToNearestWall(out Vector2 _, launchAngle, minDistance: 0.1f);
    }

    private void Update()
    {
      float dtime = BraveTime.DeltaTime;
      this._lifetime += dtime;
      float travelPercent = Mathf.Min(this._lifetime / this._travelTime, 1.0f);
      float height = _ARC_HEIGHT * Mathf.Sin(Mathf.PI * travelPercent); // TODO: use an actual parabola here
      Vector2 curPos = Vector2.Lerp(this._startPos, this._landingPos, travelPercent) + new Vector2(0.0f, height);
      this._projectile.m_transform.rotation = (_NUM_FLIPS * 360f * travelPercent).EulerZ();
      float scale = 1f - Mathf.Min(travelPercent, 1f - travelPercent);
      this._projectile.m_transform.localScale = new(scale, scale, 1.0f);
      this._body.Position = new Position(curPos);
      this._body.UpdateColliderPositions();
      if (travelPercent >= 1.0f)
        this._projectile.DieInAir();
    }

    private void OnProjectileDestruction(Projectile projectile)
    {
        projectile.OnDestruction -= OnProjectileDestruction;
        CrashLanding();
    }

    private void CrashLanding()
    {
        // collide with nearby enemies
        this._projectile.CollideWithAllEnemiesInRadius(_CRASH_RADIUS + 0.5f);
        // land and scorch
        Vector2 landingPos = this._projectile.SafeCenter;
        Exploder.DoRadialMinorBreakableBreak(landingPos, _CRASH_RADIUS);
        Exploder.DoRadialMajorBreakableDamage(this._projectile.baseData.damage, landingPos, _CRASH_RADIUS);
        Exploder.DoRadialPush(landingPos, 100f, _CRASH_RADIUS);
        Lazy.ScorchGroundAt(landingPos);
        GameManager.Instance.MainCameraController.DoScreenShake(new ScreenShakeSettings(0.5f, 6f, 0.5f, 0f), landingPos);
        CwaffVFX.SpawnBurst(
          prefab           : Groundhog._EarthClod,
          numToSpawn       : 16,
          basePosition     : landingPos,
          positionVariance : _CRASH_RADIUS,
          velType          : CwaffVFX.Vel.AwayRadial,
          velocityVariance : 4f,
          rotType          : CwaffVFX.Rot.Random,
          lifetime         : 0.5f,
          fadeOutTime      : 0.5f,
          startScale       : 1.0f,
          endScale         : 0.1f,
          uniform          : true,
          randomFrame      : true
          );
        // persist on ground
        Rubble rubble = Rubble.Create(this._projectile.sprite);
        // play a sound
        rubble.gameObject.Play("rubble_crash_sound");
    }
}

public class Rubble : MonoBehaviour
{
    private const float _DAMAGE_SHAKE_TIME = 0.25f;
    private const int _NUM_DEBRIS = 20;
    private const int _RUBBLE_HP = 30;

    private OccupiedCells _occupiedCells = null;
    private RoomHandler _room            = null;
    private bool _destroyed              = false;
    private MajorBreakable _breakable    = null;
    private Vector2 _pos                 = default;
    private tk2dSprite _sprite           = null;
    private float _shakeTimer            = 0.0f;
    private SpeculativeRigidbody _body   = null;

    public static Rubble Create(tk2dBaseSprite sprite)
    {
      // persist on ground
      GameObject rubbleObject = sprite.DuplicateInWorld(copyShader: true).gameObject;
      Rubble rubble = rubbleObject.AddComponent<Rubble>();
      rubble._body = rubbleObject.AutoRigidBody(height: 0.5f, useUntrimmedBounds: true);
      rubble._body.Reinitialize();
      rubble._body.UpdateColliderPositions();
      // prevent pathing over the space
      if (sprite.WorldCenter.GetAbsoluteRoom() is RoomHandler room)
      {
        rubble._room = room;
        rubble._occupiedCells = new OccupiedCells(rubble._body, room); // NOTE: constructor automatically updates Pathfinder
      }
      PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(rubble._body);
      // make breakable
      rubble._breakable = rubbleObject.AddComponent<MajorBreakable>();
      rubble._breakable.spawnShards = false;
      rubble._breakable.HitPoints = _RUBBLE_HP;
      rubble._breakable.destroyedOnBreak = true;
      rubble._breakable.OnBreak += rubble.OnBreak;
      rubble._breakable.OnDamaged += rubble.OnDamaged;
      // decouple sprite and body
      rubble._sprite = rubble._body.DecoupleSpriteFromCollider();
      rubble._sprite.HeightOffGround = -3f;
      rubble._pos = rubble._sprite.WorldCenter.ToVector3ZUp(rubble.transform.position.z);
      return rubble;
    }

    private void Update()
    {
      if (this._shakeTimer <= 0.0f)
        return;

      this._shakeTimer -= BraveTime.DeltaTime;
      Vector3 newPos = (this._shakeTimer <= 0.0f) ? this._pos : this._pos + new Vector2(0.125f * Mathf.Sin(100f * this._shakeTimer), 0.0f);
      this._sprite.PlaceAtPositionByAnchor(newPos, Anchor.MiddleCenter);
      this._sprite.UpdateZDepth();
    }

    private void OnDamaged(float damage)
    {
      base.gameObject.Play("rubble_damage_sound");
      this._shakeTimer = _DAMAGE_SHAKE_TIME;
    }

    private void OnBreak()
    {
      if (this._destroyed)
        return;
      this._destroyed = true;

      // PhysicsEngine.Instance.Deregister(this._body);
      this._occupiedCells.Clear();
      for (int i = 0; i < _NUM_DEBRIS; ++i)
        UnityEngine.Object.Instantiate(RubbleRouser._RockDebris, this._pos, Lazy.RandomEulerZ())
          .GetComponent<DebrisObject>().Trigger(Lazy.RandomVector(3f + 2f * UnityEngine.Random.value).ToVector3ZUp(2f), 0.25f);
      GameManager.Instance.MainCameraController.DoScreenShake(new ScreenShakeSettings(0.2f, 5f, 0.2f, 0f), this._pos);
      base.gameObject.Play("rubble_break_sound");
    }

    private void OnDestroy()
    {
      OnBreak();
    }
}

