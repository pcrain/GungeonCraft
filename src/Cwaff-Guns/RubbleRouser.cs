namespace CwaffingTheGungy;

public class RubbleRouser : CwaffGun
{
    public static string ItemName         = "Rubble Rouser";
    public static string ShortDescription = "Rock It, Launcher";
    public static string LongDescription  = "Launches large boulders a fixed distance away from the player. Boulders can be lobbed over walls, can block paths when grounded, and can be destroyed by projectiles or explosions.";
    public static string Lore             = "Seasoned explorers of the Gungeon's mines will often employ tactical blast mining to combat the Gundead. The Rubble Rouser captures all of the fun of dropping unholy amounts of rocks on unsuspecting Gundead and puts it in a convenient, portable form factor.";

    private const float _MIN_CHARGE_TIME     = 0.8f;
    private const float _FIXED_RANGE         = 10f;
    private const float _SOUND_TIMER         = 0.16f;
    private const float _SOUND_TIMER_CHARGED = 0.07f;

    internal static GameObject _RockDebris = null;
    internal static Projectile _RubbleProj = null;

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
          .InitProjectile(GunData.New(sprite: "rubble_rouser_projectile", clipSize: 1, shootStyle: ShootStyle.Charged, customClip: true, preventSparks: true,
            damage: 200f, chargeTime: _MIN_CHARGE_TIME, force: 200f, speed: 45.0f, angleVariance: 3f, shouldRotate: false, preventOrbiting: true))
          .Attach<RubbleRouserProjectile>()
          .Assign(out _RubbleProj);

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
    private bool _mastered             = false;
    private PlayerController _owner    = null;
    private GameObject _shadow         = null;

    public void Setup(RubbleRouser rr)
    {
        this._naturalSpawn = true;
        this._rr           = rr;
    }

    private void Start()
    {
        // set up sprites
        this._projectile = base.GetComponent<Projectile>();
        this._projectile.m_usesNormalMoveRegardless = true; // ignore helix bullets and whatnot
        this._owner      = this._projectile.Owner as PlayerController;
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
        this._travelTime = Mathf.Clamp(30.0f / Mathf.Max(1f, this._projectile.baseData.speed), 0.2f, 0.8f).AddRandomSpread(0.1f);

        this._projectile.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Unoccluded"));
        this._body = this._projectile.specRigidbody;
        this._projectile.collidesWithEnemies = false;
        this._body.CollideWithOthers = false;
        this._body.CollideWithTileMap = false;
        this._projectile.OnDestruction += OnProjectileDestruction;
        this._shadow = this._projectile.sprite.AddShadow(parentToSprite: false);

        if (this._owner && this._owner.GetGun<RubbleRouser>() is RubbleRouser rr && rr.Mastered)
          this._mastered = true;
    }

    private void PerformBoundsChecks(float launchAngle)
    {
      RoomHandler startRoom = this._startPos.GetAbsoluteRoom();
      // if our target is in bounds, we're good
      RoomHandler targetRoom = this._landingPos.GetAbsoluteRoom();
      if (this._landingPos.InBounds() && (targetRoom == null || targetRoom.IsVisible || startRoom == targetRoom))
      {
        // verify we're at least one unit away from the nearest wall
        Vector2 wallInFront = this._landingPos.ToNearestWall(out Vector2 wallInFrontNormal, launchAngle, minDistance: 0.1f);
        if ((wallInFront - this._landingPos).sqrMagnitude < 1f)
          this._landingPos = wallInFront + wallInFrontNormal;
        Vector2 wallBehind = this._landingPos.ToNearestWall(out Vector2 wallBehindNormal, launchAngle + 180f, minDistance: 0.1f);
        if ((wallBehind - this._landingPos).sqrMagnitude < 1f)
          this._landingPos = wallBehind + wallBehindNormal;
        return;
      }
      // if our midpoint is in bounds, raycast from there to our landing pos until we hit a wall; otherwise, start from the starting point
      Vector2 midpoint = 0.5f * (this._startPos + this._landingPos);
      Vector2 castpoint = (midpoint.InBounds() && startRoom == midpoint.GetAbsoluteRoom()) ? midpoint : this._startPos;
      this._landingPos = castpoint.ToNearestWall(out Vector2 normal, launchAngle, minDistance: 0.1f);
      this._landingPos += normal;
    }

    private void Update()
    {
      float dtime = BraveTime.DeltaTime;
      this._lifetime += dtime;
      float travelPercent = Mathf.Min(this._lifetime / this._travelTime, 1.0f);
      float height = _ARC_HEIGHT * Mathf.Sin(Mathf.PI * travelPercent);
      Vector2 shadowPos = Vector2.Lerp(this._startPos, this._landingPos, travelPercent);
      this._shadow.transform.position = shadowPos;
      Vector2 curPos = shadowPos + new Vector2(0.0f, height);
      this._projectile.m_transform.rotation = (_NUM_FLIPS * 360f * travelPercent).EulerZ();
      float scale = 1f - Mathf.Min(travelPercent, 1f - travelPercent);
      this._shadow.transform.localScale = new Vector3(2f * scale, 2f * scale, 1.0f);
      this._projectile.m_transform.localScale = new(scale, scale, 1.0f);
      this._body.Position = new Position(curPos);
      this._body.UpdateColliderPositions();
      if (travelPercent >= 1.0f)
        this._projectile.DieInAir();
    }

    private void OnProjectileDestruction(Projectile projectile)
    {
        projectile.OnDestruction -= OnProjectileDestruction;
        CrashLanding(projectile, createRubble: true, makeSentient: this._mastered);
    }

    private void OnDestroy()
    {
      if (this._shadow)
        UnityEngine.Object.Destroy(this._shadow);
    }

    internal static void CrashLanding(Projectile projectile, bool createRubble, bool makeSentient)
    {
        // collide with nearby enemies
        projectile.CollideWithAllEnemiesInRadius(_CRASH_RADIUS + 0.5f);
        // land and scorch
        Vector2 landingPos = projectile.SafeCenter;
        Exploder.DoRadialMinorBreakableBreak(landingPos, _CRASH_RADIUS);
        Exploder.DoRadialMajorBreakableDamage(projectile.baseData.damage, landingPos, _CRASH_RADIUS);
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
        // play a sound
        projectile.gameObject.PlayUnique("rubble_crash_sound");
        // persist on ground
        if (createRubble)
          Rubble.Create(projectile.sprite, sentient: makeSentient, owner: projectile.Owner as PlayerController);
    }
}

public class Rubble : MonoBehaviour
{
    private const float _DAMAGE_SHAKE_TIME = 0.25f;
    private const int _NUM_DEBRIS          = 20;
    private const int _RUBBLE_HP           = 30;
    private const float _Z_OFFSET          = 0.25f;
    private const float _RISE_TIME         = 0.5f;
    private const float _MOVE_TIME         = 0.4f;
    private const float _FALL_TIME         = 0.2f;
    private const float _RISE_HEIGHT       = 5f;
    private const float _SCAN_RADIUS       = 6.0f;
    private const float _SCAN_DELAY        = 0.5f;

    private OccupiedCells _occupiedCells = null;
    private RoomHandler _room            = null;
    private bool _destroyed              = false;
    private MajorBreakable _breakable    = null;
    private Vector2 _pos                 = default;
    private tk2dSprite _sprite           = null;
    private float _shakeTimer            = 0.0f;
    private SpeculativeRigidbody _body   = null;
    private bool _sentient               = false;
    private bool _stomping               = false;
    private PlayerController _owner      = null;
    private GameObject _shadow           = null;
    private float _scanTime              = 0.0f;

    public static void Create(tk2dBaseSprite sprite, bool sentient, PlayerController owner)
    {
      // persist on ground
      GameObject rubbleObject = sprite.DuplicateInWorld(copyShader: true).gameObject;
      Rubble rubble = rubbleObject.AddComponent<Rubble>();
      rubble._body = rubbleObject.AutoRigidBody(height: 0.5f, useUntrimmedBounds: true);
      rubble._body.Reinitialize();
      if (rubble.CheckForPits())
        return;
      // prevent pathing over the space
      if (sprite.WorldCenter.GetAbsoluteRoom() is RoomHandler room)
      {
        rubble._room = room;
        rubble._occupiedCells = new OccupiedCells(rubble._body, rubble._room); // NOTE: constructor automatically updates Pathfinder
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
      rubble._sprite.HeightOffGround = _Z_OFFSET;
      rubble._sprite.UpdateZDepth();
      rubble._pos = rubble._sprite.WorldCenter.ToVector3ZisY(_Z_OFFSET);
      rubble._owner = owner;
      // set up timers for sentient ruble
      rubble._sentient = sentient;
      if (rubble._sentient)
      {
        rubble.UpdateSpriteOutline();
        rubble._scanTime = BraveTime.ScaledTimeSinceStartup + _SCAN_DELAY;
      }
    }

    private bool CheckForPits()
    {
      DebrisObject debris = base.gameObject.AddComponent<DebrisObject>();
      debris.Priority = EphemeralObject.EphemeralPriority.Important;
      debris.canRotate = false;
      debris.angularVelocity = 0.0f;
      debris.onGround = true;
      debris.Trigger(Vector2.zero, 0.0f);
      debris.isStatic = true;
      debris.animatePitFall = true;
      debris.ForceCheckForPitfall();
      if (!debris.isFalling)
      {
        UnityEngine.Object.Destroy(debris);
        return false;
      }
      return true;
    }

    private void WatchForNearbyEnemies()
    {
      if (this._scanTime > BraveTime.ScaledTimeSinceStartup)
        return;
      if (Lazy.NearestEnemy(this._pos, ignoreWalls: true) is not AIActor actor)
        return;
      if ((actor.CenterPosition - this._pos).sqrMagnitude > (_SCAN_RADIUS * _SCAN_RADIUS))
        return;

      this._sentient = false;
      this._stomping = true;
      StartCoroutine(StompEnemy(actor));
    }

    private void UpdateSpriteOutline()
    {
      SpriteOutlineManager.AddOutlineToSprite(this._sprite, ExtendedColours.vibrantOrange, zOffset: _Z_OFFSET + 0.75f);
    }

    private void MoveSprite(Vector2 newPos, float height)
    {
      this._sprite.PlaceAtPositionByAnchor(new Vector2(newPos.x, newPos.y + height).Quantize(0.0625f).ToVector3ZisY(_Z_OFFSET), Anchor.MiddleCenter);
      if (this._shadow)
      {
        this._shadow.transform.position = newPos;
        float scale = 2f - (height / _RISE_HEIGHT);
        this._shadow.transform.localScale = new Vector3(scale, scale, 1.0f);
      }
      this._sprite.HeightOffGround = _Z_OFFSET;
      this._sprite.UpdateZDepth();
    }

    private IEnumerator StompEnemy(AIActor actor)
    {
      if (!actor || actor.specRigidbody is not SpeculativeRigidbody targetBody || actor.healthHaver is not HealthHaver targetHH || targetHH.IsDead)
        yield break;

      this._shadow = this._sprite.AddShadow(parentToSprite: false);
      this._occupiedCells.Clear();
      this._occupiedCells = null;
      this._breakable.enabled = false; // NOTE: prevent self damage and weird shenanigans with collisions when multiple rocks are on screen
      PhysicsEngine.Instance.Deregister(this._body);
      int oldLayer = base.gameObject.layer;
      base.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Unoccluded"));

      Vector2 startPos = this._pos;

      for (float elapsed = 0f; elapsed < _RISE_TIME; elapsed += BraveTime.DeltaTime)
      {
          float percentDone = elapsed / _RISE_TIME;
          MoveSprite(startPos, _RISE_HEIGHT * percentDone);
          yield return null;
      }

      Vector2 curPos = startPos;
      Vector2 targetPos = targetBody ? targetBody.UnitBottomCenter : startPos;

      for (float elapsed = 0f; elapsed < _MOVE_TIME; elapsed += BraveTime.DeltaTime)
      {
          float percentDone = elapsed / _MOVE_TIME;
          if (targetBody && targetHH && targetHH.IsAlive)
            targetPos = targetBody.UnitBottomCenter;
          curPos = Vector2.Lerp(startPos, targetPos, Ease.OutQuad(percentDone));
          MoveSprite(curPos, _RISE_HEIGHT);
          yield return null;
      }

      for (float elapsed = 0f; elapsed < _FALL_TIME; elapsed += BraveTime.DeltaTime)
      {
          float percentDone = elapsed / _FALL_TIME;
          if (targetBody && targetHH && targetHH.IsAlive)
            targetPos = targetBody.UnitBottomCenter;
          MoveSprite(targetPos, _RISE_HEIGHT * (1f - percentDone));
          yield return null;
      }

      base.gameObject.SetLayerRecursively(oldLayer);
      if (targetBody && targetHH && targetHH.IsAlive)
        targetPos = targetBody.UnitBottomCenter;
      this._pos = targetPos;
      MoveSprite(this._pos, 0.0f);
      this._stomping = false;
      base.transform.position = this._sprite.WorldCenter.ToVector3ZisY();
      SpriteOutlineManager.RemoveOutlineFromSprite(this._sprite);
      this._sprite.transform.position = base.transform.position;
      this._body.Reinitialize();
      this._body.UpdateColliderPositions();
      this._occupiedCells = new OccupiedCells(this._body, this._room);
      this._sprite.UpdateZDepth();
      if (this._shadow)
        UnityEngine.Object.Destroy(this._shadow);
      this._shadow = null;

      Projectile proj = base.gameObject.AddComponent<Projectile>();
      proj.specRigidbody = this._body;
      proj.baseData = new ProjectileData(RubbleRouser._RubbleProj.baseData); // NOTE: clone so this doesn't get overwritten
      if (this._owner)
        proj.SetOwnerAndStats(this._owner, updateCollisions: true);
      RubbleRouserProjectile.CrashLanding(proj, createRubble: false, makeSentient: false);
      proj.DestroyMode = Projectile.ProjectileDestroyMode.DestroyComponent;
      proj.DieInAir(true, false, false, true);
      this._body.PrimaryPixelCollider.IsTrigger = false; // NOTE: Projectile component sets the collider to a trigger, so we need to undo that
      PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(this._body);
      this._breakable.enabled = true;
      this.CheckForPits();

      yield break;
    }

    private void Update()
    {
      if (this._sentient)
        WatchForNearbyEnemies();
      if (this._stomping || this._shakeTimer <= 0.0f)
        return;

      this._shakeTimer -= BraveTime.DeltaTime;
      Vector2 newPos = (this._shakeTimer <= 0.0f) ? this._pos : this._pos + new Vector2(0.125f * Mathf.Sin(100f * this._shakeTimer), 0.0f);
      MoveSprite(newPos, 0.0f);
    }

    private void OnDamaged(float damage)
    {
      base.gameObject.Play("rubble_damage_sound");
      this._shakeTimer = _DAMAGE_SHAKE_TIME;
      if (this._room != null && !this._room.IsSealed && this._breakable && this._breakable.enabled && !this._breakable.IsDestroyed)
        this._breakable.Break(Vector2.zero);
    }

    private void OnBreak()
    {
      if (!this || this._destroyed || GameManager.Instance.IsLoadingLevel)
        return;
      this._destroyed = true;

      if (this._occupiedCells != null)
        this._occupiedCells.Clear();
      for (int i = 0; i < _NUM_DEBRIS; ++i)
        UnityEngine.Object.Instantiate(RubbleRouser._RockDebris, this._pos, Lazy.RandomEulerZ())
          .GetComponent<DebrisObject>().Trigger(Lazy.RandomVector(3f + 2f * UnityEngine.Random.value).ToVector3ZUp(2f), 0.25f);
      GameManager.Instance.MainCameraController.DoScreenShake(new ScreenShakeSettings(0.2f, 5f, 0.2f, 0f), this._pos);
      base.gameObject.Play("rubble_break_sound");
    }

    private void OnDestroy()
    {
      if (this._shadow)
        UnityEngine.Object.Destroy(this._shadow);
      OnBreak();
    }
}
