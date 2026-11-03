namespace CwaffingTheGungy;

public class AutumnsFury : CwaffGun
{
    public static string ItemName         = "Autumn's Fury";
    public static string ShortDescription = "Mysterious Wind";
    public static string LongDescription  = "Builds a vortex of leaves around the player while fire is held, pushing away nearby enemies and objects. Leaves home in on nearby enemies when fire is released, passing through most obstacles.";
    public static string Lore             = "Drinking apple cider, carving pumpkins, and frantically hiding from raging tornadoes inflicting wanton destruction upon everything in their path: these are just a few signs that the beautiful season of autumn has arrived. For a weapon embodying the destructiveness of autumn, one of these things is more intrinsically dangerous than the others. Sadly, due to a global pumpkin shortage, the second most dangerous one will have to do.";

    internal const float MIN_RADIUS = 2f;
    internal const float MAX_RADIUS = 7f;
    internal const float DLT_RADIUS = MAX_RADIUS - MIN_RADIUS;
    internal const float CHARGE_TIME = 5f;

    internal static CwaffTrailController _AutumnTrailPrefab = null;

    private float _chargeTime = 0.0f;
    private int _nextIndex = 0;
    private int _curBatch  = 0;
    private List<AutumnsFuryProjectile> _projectiles = new();
    private float _nextSoundTime = 0.0f;

    public float radius = MIN_RADIUS;

    public static void Init()
    {
        Lazy.SetupGun<AutumnsFury>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.S, gunClass: GunClass.FULLAUTO, reloadTime: 0.0f, ammo: 2000, shootFps: 24, chargeFps: 12,
            reloadFps: 30, attacksThroughWalls: true, doesScreenShake: false, suppressFireAnim: true, suppressReloadAnim: true)
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(clipSize: 100, cooldown: 0.05f, angleVariance: 5.0f, customClip: true, preventSparks: true,
            shootStyle: ShootStyle.Automatic, damage: 7.0f, speed: 50.0f, range: 999999f, hitSound: "autumn_impact_sound",
            sprite: "autumns_fury_bullet", fps: 12, anchor: Anchor.MiddleCenter, useDummyChargeModule: true, pierceBreakables: true))
          .SetAllImpactVFX(VFX.CreatePool("leaf_impact", fps: 30, loops: false, emissivePower: 1.0f,
            emissiveColorPower: 10.0f, emissiveColour: ExtendedColours.vibrantOrange, emissiveSensitivity: 0.5f))
          .Attach<AutumnsFuryProjectile>();

        _AutumnTrailPrefab = VFX.CreateSpriteTrailObject("autumn_trail", fps: 60, timeTillAnimStart: 0.00f,
            destroyOnEmpty: true, dispersalPrefab: Lazy.DispersalParticles(ExtendedColours.vibrantOrange));
    }

    public override void Update()
    {
        base.Update();
        float dtime = BraveTime.DeltaTime;
        if (dtime == 0.0f)
            return;
        if (this.PlayerOwner is not PlayerController player)
            return;
        if (this.gun.IsCharging)
        {
          float now = BraveTime.ScaledTimeSinceStartup;
          if (this._nextSoundTime <= now)
          {
            this._nextSoundTime = now + 0.2f;
            base.gameObject.Play("autumn_wind_sound");
          }
          this._chargeTime += dtime * player.ChargeMult();
          this.radius = MIN_RADIUS + DLT_RADIUS * Mathf.Clamp01(this._chargeTime / CHARGE_TIME);
          float phase = 1f + Mathf.Sin(10f * now);
          float curRadius = this.radius + phase;
          if (this.Mastered)
            AssimilateNearbyDebris(player, curRadius);
          else
            Exploder.DoRadialPush(player.CenterPosition, 0.35f * curRadius, curRadius);
          Exploder.DoRadialKnockback(player.CenterPosition, 0.35f * curRadius, curRadius);
          return;
        }

        if (!this.gun.IsReloading && this.gun.ClipShotsRemaining < Mathf.Min(this.gun.ClipCapacity, this.gun.CurrentAmmo))
            this.gun.Reload(); // force reload while we're not at max clip capacity

        Reset();
        // Synchronize ammo clips between projectile modules as necessary
        // (don't do while charging or bullets will all be forcibly released)
        this.gun.SynchronizeReloadAcrossAllModules();
    }

    private void AssimilateNearbyDebris(PlayerController player, float radius)
    {
        foreach(DebrisObject debris in player.CenterPosition.DebrisWithinCone(this.radius * this.radius, 0, 360f, limit: 100, allowJunk: false))
        {
            // Make sure our debris doesn't glitch out with existing additional movement modifiers
            if (!debris.DeactivateDebris(disable: false))
              continue;
            tk2dBaseSprite sprite = debris.sprite;
            if (!sprite)
              sprite = debris.gameObject.GetComponent<tk2dSprite>();
            if (sprite)
            {
              Material mat = sprite.renderer.material;
              Texture2D palette = mat.HasProperty("_PaletteTex") ? (mat.GetTexture("_PaletteTex") as Texture2D) : null;
              GameObject newDebrisObject = sprite.DuplicateInWorld(optionalPalette: palette, copyShader: true).gameObject;
              newDebrisObject.AddComponent<AutumnsFuryProjectile>().Setup(true, player);
            }
            UnityEngine.Object.Destroy(debris.gameObject);
        }
    }

    public void AddProjectile(AutumnsFuryProjectile proj)
    {
        this._projectiles.Add(proj);
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        Reset();
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        Reset();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        Reset();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        Reset();
    }

    private void Reset()
    {
        if (this._nextIndex == 0)
            return;

        this._nextIndex = 0;
        ++this._curBatch;
        foreach (AutumnsFuryProjectile p in this._projectiles)
          if (p)
            p.ScatterLikeTheWind();
        this._projectiles.Clear();
        this.radius = MIN_RADIUS;
        if (this._chargeTime > 0)
        {
          gun.spriteAnimator.StopAndResetFrameToDefault();
          gun.spriteAnimator.Play(gun.shootAnimation);
        }
        this._chargeTime = 0.0f;
    }

    public int GetNextIndex() => this._nextIndex++;
    public int GetBatch() => this._curBatch;
}

public class AutumnsFuryProjectile : MonoBehaviour
{
    private const float ROT_PER_SEC = 2f;    // rotation speed of leaf sprites
    private const float REV_PER_SEC = 2.5f; // revolution speed of leaves around player
    private const float TIME_TIL_ORBIT = 0.125f;
    private const float SLOW_TIME = 0.5f;
    private const float ACCEL_TIME = 0.35f;

    private PlayerController _owner                 = null;
    private Projectile       _projectile            = null;
    private int              _index                 = 0;
    private int              _batch                 = 0;
    private bool             _naturalSpawn          = false;
    private float            _spawnTime             = 0f;
    private AutumnsFury      _af                    = null;
    private float            _stateTimer            = 0.0f;
    private float            _radius                = 0.0f;
    private float            _radiusScale           = 0.0f;
    private float            _angle                 = 0.0f;
    private float            _rotSpeed              = 0.0f;
    private float            _orbitTime             = 0.0f;
    private float            _targetSpeed           = 0.0f;
    private RoomHandler      _room                  = null;
    private float            _tilOrbitTimer         = 0.0f;
    private tk2dBaseSprite   _sprite                = null;
    private State            _state                 = State.NEUTRAL;
    private SpeculativeRigidbody _body              = null;
    private float            _slowTime              = 0.0f;
    private float            _originalRotSpeed      = 0.0f;
    private float            _accelTime             = 0.0f;
    private bool             _addedTrail            = false;
    private bool             _atFinalSpeed          = false;
    private Vector2          _velocity              = default;
    private bool             _didSetup              = false;

    private enum State {
        NEUTRAL,
        ORBITING,
        SLOWING,
        SCATTERING,
    }

    private void Start()
    {
        Setup(false, null);
    }

    public void Setup(bool fromGroundedDebris, PlayerController player)
    {
        if (this._didSetup)
          return;

        this._didSetup    = true;
        if (fromGroundedDebris)
        {
          this._projectile = base.gameObject.AddComponent<Projectile>();
          this._projectile.specRigidbody = base.gameObject.GetComponent<SpeculativeRigidbody>();
          if (!this._projectile.specRigidbody)
            this._projectile.specRigidbody = base.gameObject.AutoRigidBody(CollisionLayer.Projectile);
          this._projectile.baseData = new ProjectileData() {
            damage = 7.0f,
            speed  = 50.0f,
            range  = 999999f,
            force  = 5.0f,
          };
          this._projectile.Awake(); // HACK: force tile collision logic to be set up
          this._owner = player;
          this._projectile.SetOwnerAndStats(this._owner);
          // this._owner.DoPostProcessProjectile(this._projectile); // NOTE: intentionally not doing this due to lag + sparks + generally not useful
        }
        else
        {
          this._projectile  = base.GetComponent<Projectile>();
          this._owner       = this._projectile.Owner as PlayerController;
        }
        this._spawnTime   = BraveTime.ScaledTimeSinceStartup;
        this._room        = base.transform.position.GetAbsoluteRoom();
        this._targetSpeed = this._projectile.baseData.speed;
        this._sprite      = this._projectile.sprite;
        this._body        = this._projectile.specRigidbody;

        // grounded debris gets destroyed once flung
        // (NOTE: mostly because shotgun kin palettes get weird and i can't be bothered to find out why)
        if (!fromGroundedDebris)
        {
          this._projectile.DestroyMode = Projectile.ProjectileDestroyMode.BecomeDebris;
          this._projectile.OnBecameDebris += OnBecameDebris;
        }

        if (!this._owner)
            return; // shouldn't happen, but just be safe

        this._projectile.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Unoccluded"));
        if (this._owner.CurrentGun.GetComponent<AutumnsFury>() is AutumnsFury af)
        {
            this._index = af.GetNextIndex();
            this._batch = af.GetBatch();
            this._naturalSpawn = true;
            this._af = af;
            this._radius = af.radius;
            this._projectile.SetSpeed(this._radius / TIME_TIL_ORBIT);
            af.AddProjectile(this);
        }
        if (!fromGroundedDebris)
          this._projectile.sprite.MakeGlowyBetter(glowAmount: 1f, glowColor: ExtendedColours.vibrantOrange, glowColorPower: 10f, sensitivity: 0.5f);
        this._body.CollideWithTileMap = false;
        this._body.CollideWithOthers = false;
        this._body.Reinitialize();

        this._state = State.NEUTRAL;
        this._tilOrbitTimer = fromGroundedDebris ? 0.0f : TIME_TIL_ORBIT;

        if (!this._naturalSpawn)
        {
          ScatterLikeTheWind();
          return;
        }

        PhysicsEngine.Instance.Deregister(this._body);
        this._state = State.ORBITING;
        this._projectile.ManualControl = true;
        this._projectile.SetSpeed(0.01f);
        this._radiusScale = UnityEngine.Random.Range(0.92f, 1.08f);
        this._angle = this._projectile.AngleFromOwner();
        this._rotSpeed = REV_PER_SEC * UnityEngine.Random.Range(1.0f, 2.0f);
        this._orbitTime = 100.0f * UnityEngine.Random.value;
    }

    private void Update()
    {
        if (!this._projectile)
            return;

        float dtime = BraveTime.DeltaTime;
        this._projectile.transform.localRotation = (-ROT_PER_SEC * 360f * (BraveTime.ScaledTimeSinceStartup - this._spawnTime)).EulerZ();
        this._stateTimer -= dtime;
        switch (this._state)
        {
          case State.ORBITING:
            this._orbitTime += dtime;
            if (!this._owner || !this._af || this._af.GetBatch() != this._batch)
            {
                ScatterLikeTheWind();
                break;
            }
            Swirl(dtime);
            break;
          case State.SLOWING:
            if (this._stateTimer > 0)
            {
              this._rotSpeed = this._originalRotSpeed * (1f - Ease.InCubic(1f - this._stateTimer / this._slowTime));
              Swirl(dtime);
              break;
            }
            // transition to scattering state
            this._state = State.SCATTERING;
            base.gameObject.Play("autumn_shoot_sound");
            this._projectile.PenetratesInternalWalls = true;
            this._body.OnPreTileCollision += this._projectile.OnPreTileCollision;
            this._body.CollideWithOthers = true;
            this._body.CollideWithTileMap = true;
            this._body.m_initialized = false;
            this._body.Reinitialize();
            AIActor closestEnemy = Lazy.NearestEnemyWithinConeOfVision(start: this._projectile.SafeCenter, coneAngle: this._velocity.ToAngle(),
              maxDeviation: 360f, maxDistance: 100f, ignoreWalls: true, useNearestAngleInsteadOfDistance: true);
            if (closestEnemy)
              this._projectile.SendInDirection(closestEnemy.CenterPosition - this._projectile.SafeCenter, resetDistance: true);
            else if (this._velocity.sqrMagnitude > 0.1f)
              this._projectile.SendInDirection(this._velocity, resetDistance: true);
            else
              this._projectile.SendInDirection(this._projectile.OriginalDirection().ToVector(), resetDistance: true);
            this._body.Reinitialize();
            this._projectile.SetSpeed(0.01f);
            this._accelTime = ACCEL_TIME * UnityEngine.Random.value;
            this._stateTimer = this._accelTime;
            break;
          case State.SCATTERING:
            if (this._stateTimer > 0)
            {
              float newSpeed = this._targetSpeed * Ease.InCubic(1f - this._stateTimer / this._accelTime);
              this._projectile.SetSpeed(newSpeed);
              if (!this._addedTrail && newSpeed > 20f)
              {
                this._addedTrail = true;
                this._projectile.AddTrail(AutumnsFury._AutumnTrailPrefab);
              }
            }
            else if (!this._atFinalSpeed)
            {
              this._atFinalSpeed = true;
              this._projectile.SetSpeed(this._targetSpeed);
            }
            Vector2 pos = this._projectile.SafeCenter;
            if (GameManager.Instance.MainCameraController.PointIsVisible(pos, 0.4f))
              break;
            if (this._room != null && this._room == pos.GetAbsoluteRoom())
              break;
            this._projectile.OnBecameDebris -= OnBecameDebris;
            this._projectile.DestroyMode = Projectile.ProjectileDestroyMode.Destroy;
            this._projectile.DieInAir();
            break;
        }
    }

    public void ScatterLikeTheWind()
    {
      if (!this._projectile || !this._body || this._state == State.SCATTERING || this._state == State.SLOWING)
        return;

      this._body.Reinitialize();
      this._projectile.ManualControl = false;
      this._state = State.SLOWING;
      this._slowTime = SLOW_TIME * UnityEngine.Random.value;
      this._originalRotSpeed = this._rotSpeed;
      this._stateTimer = this._slowTime;
    }

    private void Swirl(float dtime)
    {
        this._tilOrbitTimer -= dtime;
        this._angle = (this._angle + 360f * this._rotSpeed * (0.5f * Mathf.Abs(Mathf.Sin(2f * this._orbitTime))) * dtime).Clamp360();
        Vector2 ownerPos = this._owner.CenterPosition;
        float orbitOffset = 3f * this._orbitTime;
        if (this._sprite)
          this._sprite.scale = this._sprite.scale.WithY(Mathf.Sin(orbitOffset));
        Vector2 newPos = ownerPos + this._angle.ToVector((this._radius * this._radiusScale) + Mathf.Cos(orbitOffset));
        if (this._tilOrbitTimer > 0)
          newPos = Vector2.Lerp(ownerPos, newPos, Ease.OutQuad(1f - this._tilOrbitTimer / TIME_TIL_ORBIT));
        this._velocity = (newPos - base.transform.position.XY());
        base.transform.position = newPos;
      }

    private void OnBecameDebris(DebrisObject debris)
    {
      // REFACTOR: make this a BecomeWaftingDebris() helper
      if (this._sprite)
        this._sprite.scale = this._sprite.scale.WithY(1f);
      WaftingDebrisObject wafter = base.gameObject.AddComponent<WaftingDebrisObject>();
      wafter.angularVelocity = 45f;
      wafter.angularVelocityVariance = 20f;
      wafter.decayOnBounce = 0.5f;
      wafter.bounceCount = 1;
      wafter.canRotate = true;
      wafter.shouldUseSRBMotion = true;
      wafter.AssignFinalWorldDepth(-0.5f);
      wafter.sprite = this._projectile.sprite;
      wafter.animatePitFall = true;
      wafter.inertialMass = 0.5f;
      wafter.Trigger(wafter.m_velocity * (1.0f + 4.0f * UnityEngine.Random.value), 0.5f);
      if (this._projectile)
      {
          this._projectile.OnBecameDebris -= OnBecameDebris;
          this._projectile = null;
      }
      if (debris)
        UnityEngine.Object.Destroy(debris); // destroy the original DebrisObject since we now have a WaftingDebrisObject
      UnityEngine.Object.Destroy(this); // destroy the AutumnsFuryProjectile component
    }
}
