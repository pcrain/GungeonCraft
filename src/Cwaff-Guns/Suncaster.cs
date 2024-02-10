namespace CwaffingTheGungy;

public class Suncaster : AdvancedGunBehavior
{
    public static string ItemName         = "Suncaster";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Kaleido-noscope";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal const int _PRISM_AMMO_COST                      = 5;
    internal const float _PRISM_LAUNCH_SPEED                 = 20f;

    internal static GameObject _PrismPrefab                  = null;
    internal static GameObject _TraceVFX                     = null;
    internal static GameObject _NewTraceVFX                  = null;
    internal static TrailController _SunTrailPrefab          = null;
    internal static TrailController _SunTrailRefractedPrefab = null;
    internal static Projectile _SuncasterProjectile          = null;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Suncaster>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.FIRE, reloadTime: 0.0f, ammo: 500, canReloadNoMatterAmmo: true, infiniteAmmo: true);
            gun.SetAnimationFPS(gun.shootAnimation, 30);
            gun.SetAnimationFPS(gun.reloadAnimation, 40);

        _SuncasterProjectile = gun.InitProjectile(new(clipSize: -1, cooldown: 0.5f, shootStyle: ShootStyle.SemiAutomatic, damage: 1f, speed: 100f, range: 999999f,
          /*sprite: "suncaster_projectile", */fps: 12, anchor: Anchor.MiddleLeft)).Attach<SuncasterProjectile>();

        _SunTrailPrefab = VFX.CreateTrailObject(ResMap.Get("suncaster_beam_mid")[0], new Vector2(20, 4), new Vector2(0, 0),
            ResMap.Get("suncaster_beam_mid"), 60, cascadeTimer: C.FRAME,  softMaxLength: 1, destroyOnEmpty: false);
        _SunTrailRefractedPrefab = VFX.CreateTrailObject(ResMap.Get("suncaster_beam_refracted_mid")[0], new Vector2(20, 4), new Vector2(0, 0),
            ResMap.Get("suncaster_beam_refracted_mid"), 60, cascadeTimer: C.FRAME,  softMaxLength: 1, destroyOnEmpty: false);

        _TraceVFX = VFX.Create("basic_square", fps: 7, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 10f);
        _NewTraceVFX = VFX.Create("basic_green_square", fps: 7, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 10f);

        _PrismPrefab = VFX.Create("prism_vfx", fps: 7, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 5f);
        SpeculativeRigidbody body = _PrismPrefab.GetOrAddComponent<SpeculativeRigidbody>();
        body.CanBePushed = true;
        body.PixelColliders = new List<PixelCollider>(){new(){
          ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
          ManualOffsetX          = -6,
          ManualOffsetY          = -18,
          ManualWidth            = 13,
          ManualHeight           = 24,
          CollisionLayer         = CollisionLayer.HighObstacle,
          Enabled                = true,
          IsTrigger              = false,
        }};
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        if (player.IsDodgeRolling || !player.AcceptingNonMotionInput/* || (gun.ClipShotsRemaining < gun.ClipCapacity) || gun.CurrentAmmo <= _PRISM_AMMO_COST*/)
          return;
        GameObject prism = _PrismPrefab.Instantiate(position: gun.barrelOffset.position);
        prism.AddComponent<SuncasterPrism>().Setup(player, gun.CurrentAngle.ToVector(_PRISM_LAUNCH_SPEED));
        // gun.LoseAmmo(_PRISM_AMMO_COST);
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        projectile.GetComponent<SuncasterProjectile>().FiredFromGun();
        AkSoundEngine.PostEvent("subtractor_beam_fire_sound_stop_all", projectile.gameObject);
        AkSoundEngine.PostEvent("subtractor_beam_fire_sound", projectile.gameObject);
    }
}

public class SuncasterProjectile : MonoBehaviour
{
    private readonly bool _ORIGINAL_GOES_STRAIGHT = true;  // false for old version of projectile

    protected List<SuncasterPrism> _hitPrisms = new();
    protected SuncasterPrism       _lastPrism = null;

    private Projectile           _proj        = null;
    private PlayerController     _owner       = null;
    private Vector2              _lastPos     = Vector2.zero;
    private int                  _amps        = 0;
    private TrailController      _trail       = null;
    private bool                 _fromGun     = false;

    private void Start()
    {
        this._proj  = base.GetComponent<Projectile>();
        this._owner = this._proj.ProjectilePlayerOwner();
        this._trail = this._proj.AddTrailToProjectileInstance(this._fromGun ? Suncaster._SunTrailPrefab : Suncaster._SunTrailRefractedPrefab);
        this._trail.gameObject.SetGlowiness(100f);

        this._proj.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
    }

    private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
      if (otherRigidbody.GetComponent<SuncasterPrism>() is not SuncasterPrism prism)
        return;

      PhysicsEngine.SkipCollision = true;
      if (this._lastPrism != prism)
        Amplify(prism);
    }

    private void OnDestroy()
    {
      if (this._trail)
        this._trail.gameObject.SafeDestroy();
    }

    private void Amplify(SuncasterPrism prism)
    {
      ++this._amps;

      this._lastPrism = prism;
      if (!this._hitPrisms.Contains(prism))
      {
        this._hitPrisms.Add(prism);
        this._proj.baseData.damage *= 2;
      }

      if (this._fromGun)
      {
        Projectile p = SpawnManager.SpawnProjectile(
            prefab   : Suncaster._SuncasterProjectile.gameObject,
            position : prism.transform.position,
            rotation : Quaternion.identity).GetComponent<Projectile>();
        p.Owner           = this._owner;
        p.baseData.speed  = this._proj.baseData.speed;
        p.baseData.damage = this._proj.baseData.damage;
        p.GetComponent<SuncasterProjectile>()._lastPrism = prism;
        if (_ORIGINAL_GOES_STRAIGHT)
          p.SendInDirection(prism.Angle(), true);
      }

      this._proj.sprite.SetGlowiness(this._proj.baseData.damage);
      this._proj.specRigidbody.Position = new Position(prism.BasePosition());
      this._proj.specRigidbody.UpdateColliderPositions();
      if (!_ORIGINAL_GOES_STRAIGHT)
        this._proj.SendInDirection(prism.Angle(), true);
      this._proj.UpdateSpeed();
      AkSoundEngine.PostEvent("snd_select", base.gameObject);
    }

    public void FiredFromGun() => this._fromGun = true;
}

public class SuncasterPrism : MonoBehaviour, IPlayerInteractable
{
    private const float _FRICTION   = 0.9f;
    private const float _BOB_SPEED  = 4f;
    private const float _BOB_HEIGHT = 0.20f;
    private const float _MAX_LIFE   = 6000.0f;
    private const float _TRACE_RATE = 0.15f;

    private PlayerController _owner = null;
    private tk2dSprite _sprite      = null;
    private bool    _setup          = false;
    private Vector2 _velocity       = Vector2.zero;
    private Vector2 _angle          = Vector2.zero;
    private Vector2 _newAngle       = Vector2.zero;
    private float   _lifetime       = 0.0f;
    private SpeculativeRigidbody _body = null;
    private bool    _trace          = false;
    private float   _last_trace     = 0.0f;

    public void Setup(PlayerController owner, Vector2 velocity)
    {
      AkSoundEngine.PostEvent("fire_coin_sound", base.gameObject);
      this._owner    = owner;
      this._velocity = velocity;
      this._angle    = velocity.normalized;
      this._sprite   = base.GetComponent<tk2dSprite>();
      base.transform.position.GetAbsoluteRoom()?.RegisterInteractable(this);

      this._body                    = gameObject.GetComponent<SpeculativeRigidbody>();
      // this._body.OnCollision        += this.OnCollision;  //TODO: figure out why this doesn't properly bounce off walls
      this._body.Velocity = this._velocity;
      this._body.RegisterTemporaryCollisionException(owner.specRigidbody);
      if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
        this._body.RegisterTemporaryCollisionException(GameManager.Instance.GetOtherPlayer(owner)?.specRigidbody);

      this._setup    = true;
    }

    private void OnCollision(CollisionData collision)
    {
      if (collision.collisionType != CollisionData.CollisionType.TileMap)
        return;
      // ETGModConsole.Log($"handling normal {collision.Normal}");
      // ETGModConsole.Log($"  old velocity {this._body.Velocity}");
      // this._body.transform.position -= (C.PIXELS_PER_TILE * collision.NewPixelsToMove.ToVector2()).ToVector3ZUp();
      // this._body.Reinitialize();
      this._body.Velocity = new Vector2(
        x: this._body.Velocity.x * (collision.CollidedX ? -1f : 1f),
        y: this._body.Velocity.y * (collision.CollidedY ? -1f : 1f)
        );
      // ETGModConsole.Log($"  new velocity {this._body.Velocity}");
    }

    private void OnDestroy()
    {
      base.transform.position.GetAbsoluteRoom()?.DeregisterInteractable(this);
    }

    private void Update()
    {
        if (!this._setup)
          return;

        if ((this._lifetime += BraveTime.DeltaTime) > _MAX_LIFE)
        {
          Lazy.DoSmokeAt(base.transform.position);
          UnityEngine.Object.Destroy(base.gameObject);
          return;
        }

        if (this._body.Velocity.sqrMagnitude >= 1f)
          this._body.Velocity *= (float)Lazy.FastPow(_FRICTION, C.FPS * BraveTime.DeltaTime);
        else
          this._body.Velocity = Vector2.zero;

        if (((this._last_trace + _TRACE_RATE) < BraveTime.ScaledTimeSinceStartup))
        {
          this._last_trace = BraveTime.ScaledTimeSinceStartup;
          FancyVFX.Spawn(Suncaster._TraceVFX, base.transform.position, velocity: 12f * this._angle, lifetime: 0.5f, fadeOutTime: 0.5f);
          if (this._trace && this._owner)
          {
            this._newAngle = this._owner.m_currentGunAngle.ToVector().normalized;
            FancyVFX.Spawn(Suncaster._NewTraceVFX, base.transform.position, velocity: 12f * this._newAngle, lifetime: 0.5f, fadeOutTime: 0.5f);
          }
        }
    }

    public Vector2 Angle() => this._angle;
    public Vector2 BasePosition() => base.transform.position;

    public void Interact(PlayerController interactor)
    {
      if (interactor != this._owner)
        return;

      this._angle = this._newAngle;
      AkSoundEngine.PostEvent("prism_interact_sound", base.gameObject);
    }

    public void OnEnteredRange(PlayerController interactor)
    {
        if (interactor == this._owner)
          this._trace = true;
        SpriteOutlineManager.AddOutlineToSprite(this._sprite, Color.white, 1f, 0f, SpriteOutlineManager.OutlineType.NORMAL);
        this._sprite.UpdateZDepth();
    }

    public void OnExitRange(PlayerController interactor)
    {
        if (interactor == this._owner)
          this._trace = false;
        SpriteOutlineManager.AddOutlineToSprite(this._sprite, Color.black, 1f, 0f, SpriteOutlineManager.OutlineType.NORMAL);
    }

    public string GetAnimationState(PlayerController interactor, out bool shouldBeFlipped)
    {
      shouldBeFlipped = false;
      return string.Empty;
    }

    public float GetDistanceToPoint(Vector2 point) => Vector2.Distance(point, base.transform.position) * 0.33f; // triple normal interaction range
    public float GetOverrideMaxDistance() => -1f;
}
