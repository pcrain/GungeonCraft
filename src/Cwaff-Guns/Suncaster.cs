namespace CwaffingTheGungy;

public class Suncaster : AdvancedGunBehavior
{
    public static string ItemName         = "Suncaster";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Kaleido-noscope";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal const int _PRISM_AMMO_COST = 5;
    internal const float _PRISM_LAUNCH_SPEED = 20f;

    internal static GameObject _PrismVFX = null;
    internal static GameObject _TraceVFX = null;
    internal static GameObject _NewTraceVFX = null;
    internal static TrailController _SunTrailPrefab = null;
    internal static List<SuncasterPrism> _ExtantPrisms = new();

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Suncaster>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.FIRE, reloadTime: 1.2f, ammo: 500, canReloadNoMatterAmmo: true);
            gun.SetAnimationFPS(gun.shootAnimation, 30);
            gun.SetAnimationFPS(gun.reloadAnimation, 40);

        gun.InitProjectile(new(clipSize: 6, cooldown: 0.5f, shootStyle: ShootStyle.SemiAutomatic, damage: 0f, speed: 100f,
          /*sprite: "suncaster_projectile", */fps: 12, anchor: Anchor.MiddleLeft)).Attach<SuncasterProjectile>();

        _SunTrailPrefab = VFX.CreateTrailObject(ResMap.Get("subtractor_beam_mid")[0], new Vector2(20, 4), new Vector2(0, 0), //TODO: get our own trail here
            ResMap.Get("subtractor_beam_mid"), 60, ResMap.Get("subtractor_beam_start"), 60/*, cascadeTimer: C.FRAME*/, softMaxLength: 1, destroyOnEmpty: false);

        _PrismVFX = VFX.Create("prism_vfx", fps: 7, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 5f);
        _TraceVFX = VFX.Create("basic_square", fps: 7, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 1f);
        _NewTraceVFX = VFX.Create("basic_green_square", fps: 7, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 1f);
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        if (player.IsDodgeRolling || !player.AcceptingNonMotionInput || (gun.ClipShotsRemaining < gun.ClipCapacity) || gun.CurrentAmmo <= _PRISM_AMMO_COST)
          return;
        GameObject prism = _PrismVFX.Instantiate(position: gun.barrelOffset.position);
        prism.AddComponent<SuncasterPrism>().Setup(player, gun.CurrentAngle.ToVector(_PRISM_LAUNCH_SPEED));
        gun.LoseAmmo(_PRISM_AMMO_COST);
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        AkSoundEngine.PostEvent("subtractor_beam_fire_sound_stop_all", projectile.gameObject);
        AkSoundEngine.PostEvent("subtractor_beam_fire_sound", projectile.gameObject);
    }
}

public class SuncasterProjectile : MonoBehaviour
{
    private Projectile           _proj        = null;
    private PlayerController     _owner       = null;
    private Vector2              _lastPos     = Vector2.zero;
    private List<SuncasterPrism> _hitPrisms   = new();
    private int                  _amps        = 0;
    private TrailController      _trail       = null;

    private void Start()
    {
        this._proj  = base.GetComponent<Projectile>();
        this._owner = this._proj.ProjectilePlayerOwner();
        this._trail = this._proj.AddTrailToProjectileInstance(Suncaster._SunTrailPrefab);
        this._trail.gameObject.SetGlowiness(100f);
    }

    private void OnDestroy()
    {
        this._trail.gameObject.SafeDestroy();
    }

    private void Update()
    {
      if (!this._owner)
        return;

      Vector2 curPos = base.transform.PositionVector2();
      foreach (SuncasterPrism prism in Suncaster._ExtantPrisms)
      {
        tk2dSprite sprite = prism.GetComponent<tk2dSprite>();
        // if (!BraveUtility.LineIntersectsAABB(this._lastPos, curPos, sprite.WorldBottomLeft, sprite.WorldTopRight - sprite.WorldBottomLeft, out Vector2 _))
        if (!Lazy.LineIntersectsCircle(this._lastPos, curPos, prism.BasePosition(), 1.5f))
          continue; //TODO: double check the intersection logic for sprites
        if (this._hitPrisms.Contains(prism))
          continue;
        this._hitPrisms.Clear();
        this._hitPrisms.Add(prism);
        Amplify(prism);
      }
      this._lastPos = curPos;
    }

    private void Amplify(SuncasterPrism prism)
    {
      ++this._amps;
      this._proj.baseData.damage *= 2;
      this._proj.sprite.SetGlowiness(this._proj.baseData.damage);
      this._proj.specRigidbody.Position = new Position(prism.BasePosition());
      this._proj.specRigidbody.UpdateColliderPositions();
      this._proj.SendInDirection(prism.Angle(), true);
      this._proj.UpdateSpeed();
      AkSoundEngine.PostEvent("aimu_focus_sound", base.gameObject);
    }
}

public class SuncasterPrism : MonoBehaviour, IPlayerInteractable
{
    private const float _FRICTION   = 0.9f;
    private const float _BOB_SPEED  = 4f;
    private const float _BOB_HEIGHT = 0.20f;
    private const float _MAX_LIFE   = 6000.0f;
    private const float _TRACE_RATE = 0.25f;

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
      Suncaster._ExtantPrisms.Add(this);
      AkSoundEngine.PostEvent("fire_coin_sound", base.gameObject);
      this._owner    = owner;
      this._velocity = velocity;
      this._angle    = velocity.normalized;
      this._sprite   = base.GetComponent<tk2dSprite>();
      base.transform.position.GetAbsoluteRoom()?.RegisterInteractable(this);

      this._body = gameObject.AddComponent<SpeculativeRigidbody>();
      this._body.transform.position = base.transform.position;
      this._body.PixelColliders = new List<PixelCollider>();
      PixelCollider pixelCollider = new PixelCollider();
      pixelCollider.ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual;
      pixelCollider.ManualOffsetX = -6;
      pixelCollider.ManualOffsetY = -18;
      pixelCollider.ManualWidth = 13;
      pixelCollider.ManualHeight = 24;
      pixelCollider.CollisionLayer = CollisionLayer.PlayerBlocker;
      pixelCollider.Enabled = true;
      pixelCollider.IsTrigger = false;
      this._body.CanBePushed = true;
      // this._body.CanPush = true;
      this._body.PixelColliders.Add(pixelCollider);
      this._body.Reinitialize();
      // PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(speculativeRigidbody);

      this._setup    = true;
    }

    private void OnDestroy()
    {
      Suncaster._ExtantPrisms.Remove(this);
      base.transform.position.GetAbsoluteRoom()?.DeregisterInteractable(this);
    }

    private void Update()
    {
        if (!this._setup)
          return;

        this._lifetime += BraveTime.DeltaTime;
        if (this._lifetime > _MAX_LIFE)
        {
          Lazy.DoSmokeAt(base.transform.position);
          UnityEngine.Object.Destroy(base.gameObject);
          return;
        }

        if (this._velocity.sqrMagnitude > 1f)
        {
            this._velocity *= (float)Lazy.FastPow(_FRICTION, C.FPS * BraveTime.DeltaTime);
            base.transform.position = (base.transform.position.XY() + (this._velocity * BraveTime.DeltaTime)).ToVector3ZUp();
            this._body.UpdateColliderPositions();
            this._body.Reinitialize();
        }
        // this._sprite.transform.position = new Vector2(base.transform.position.x, base.transform.position.y + _BOB_HEIGHT * Mathf.Sin(_BOB_SPEED * BraveTime.ScaledTimeSinceStartup)).ToVector3ZisY();
        // this._sprite.transform.localPosition = new Vector2(base.transform.position.x, base.transform.position.y + _BOB_HEIGHT * Mathf.Sin(_BOB_SPEED * BraveTime.ScaledTimeSinceStartup)).ToVector3ZisY();
        // this._sprite.HeightOffGround = _BOB_HEIGHT * Mathf.Sin(_BOB_SPEED * BraveTime.ScaledTimeSinceStartup);
        // this._sprite.UpdateZDepth();

        if (this._trace && ((this._last_trace + _TRACE_RATE) < BraveTime.ScaledTimeSinceStartup))
        {
          this._last_trace = BraveTime.ScaledTimeSinceStartup;
          if (this._owner)
            this._newAngle = (base.transform.position.XY() - this._owner.CenterPosition).normalized;
          FancyVFX.Spawn(Suncaster._TraceVFX, base.transform.position, velocity: 6f * this._angle, lifetime: 1.0f, fadeOutTime: 1.0f);
          FancyVFX.Spawn(Suncaster._NewTraceVFX, base.transform.position, velocity: 6f * this._newAngle, lifetime: 1.0f, fadeOutTime: 1.0f);
        }
    }

    public Vector2 Angle() => this._angle;
    public Vector2 BasePosition() => base.transform.position;

    public void Interact(PlayerController interactor)
    {
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

    public float GetDistanceToPoint(Vector2 point) => Vector2.Distance(point, base.transform.position) / 1.5f;
    public float GetOverrideMaxDistance() => -1f;
}
