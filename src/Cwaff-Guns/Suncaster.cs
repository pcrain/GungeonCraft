namespace CwaffingTheGungy;

public class Suncaster : AdvancedGunBehavior
{
    public static string ItemName         = "Suncaster";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Reflaktive";
    public static string LongDescription  = "Fires weak piercing beams of sunlight. Reload to toss a refractive prism. Uncharged shots that hit a prism will refract sunlight to all other prisms. Charged shots continuously bounce between and refract off of all placed prisms for a short period. Prisms can be reclaimed by interacting with them or by entering a new room. Cannot gaim ammo normally, but passively restores ammo over time.";
    public static string Lore             = "TBD";

    internal const  string          _PrismUI                 = $"{C.MOD_PREFIX}:_PrismUI";  // need the string immediately for preloading in Main()
    internal const  int             _BASE_MAX_PRISMS         = 6;
    internal const  int             _PRISM_AMMO_COST         = 5;
    internal const  float           _PRISM_LAUNCH_SPEED      = 20f;
    internal const  float           _CHARGE_RATE             = 0.3f;
    internal const  float           _CHARGE_TIME             = 0.65f;
    internal const  int             _CHARGE_AMMO_COST        = 15;

    internal static GameObject      _PrismPrefab             = null;
    internal static GameObject      _TraceVFX                = null;
    internal static GameObject      _NewTraceVFX             = null;
    internal static TrailController _SunTrailPrefab          = null;
    internal static TrailController _SunTrailRefractedPrefab = null;
    internal static TrailController _SunTrailFinalPrefab     = null;
    internal static Projectile      _SuncasterProjectile     = null;

    [SerializeField] // make sure we keep this when the gun is dropped and picked back ups
    private float _lastChargeTime            = 0.0f;

    public List<SuncasterPrism> extantPrisms = new();
    public int maxPrisms                     = _BASE_MAX_PRISMS;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Suncaster>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.S, gunClass: GunClass.FIRE, reloadTime: 0.0f, ammo: 30,
              canReloadNoMatterAmmo: true, canGainAmmo: false, doesScreenShake: false);
            gun.SetAnimationFPS(gun.shootAnimation, 30);
            gun.SetAnimationFPS(gun.reloadAnimation, 40);

        _SuncasterProjectile = gun.InitProjectile(new(clipSize: -1, cooldown: 0.1f, shootStyle: ShootStyle.Charged,
          damage: 2f, speed: 100f, range: 999999f, fps: 12, anchor: Anchor.MiddleLeft)
        ).Attach<PierceProjModifier>(pierce => pierce.penetration = 999
        ).Attach<SuncasterProjectile>();
        _SuncasterProjectile.pierceMinorBreakables = true;
        _SuncasterProjectile.PenetratesInternalWalls = true;

        gun.DefaultModule.chargeProjectiles = new(){
          new ProjectileModule.ChargeProjectile {
            Projectile = _SuncasterProjectile.Clone().Attach<SuncasterProjectile>(s => s.charged = false),
            ChargeTime = 0.0f,
          },
          new ProjectileModule.ChargeProjectile {
            Projectile = _SuncasterProjectile.Clone().Attach<SuncasterProjectile>(s => s.charged = true),
            ChargeTime = _CHARGE_TIME,
            AmmoCost   = _CHARGE_AMMO_COST,
            UsedProperties = ChargeProjectileProperties.ammo,
          },
        };

        _SunTrailPrefab = VFX.CreateTrailObject(ResMap.Get("suncaster_beam_mid")[0], new Vector2(20, 4), new Vector2(0, 0),
            ResMap.Get("suncaster_beam_mid"), 60, cascadeTimer: C.FRAME,  softMaxLength: 1, destroyOnEmpty: false);
        _SunTrailRefractedPrefab = VFX.CreateTrailObject(ResMap.Get("suncaster_beam_refracted_mid")[0], new Vector2(20, 4), new Vector2(0, 0),
            ResMap.Get("suncaster_beam_refracted_mid"), 60, cascadeTimer: C.FRAME,  softMaxLength: 1, destroyOnEmpty: false);
        _SunTrailFinalPrefab = VFX.CreateTrailObject(ResMap.Get("suncaster_beam_final_mid")[0], new Vector2(20, 4), new Vector2(0, 0),
            ResMap.Get("suncaster_beam_final_mid"), 60, cascadeTimer: C.FRAME,  softMaxLength: 1, destroyOnEmpty: false);

        _TraceVFX = VFX.Create("basic_square", fps: 7, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 10f);
        _NewTraceVFX = VFX.Create("basic_green_square", fps: 7, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 10f);

        _PrismPrefab = VFX.Create("prism_vfx", fps: 7, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 5f);
        SpeculativeRigidbody body = _PrismPrefab.GetOrAddComponent<SpeculativeRigidbody>();
        body.CanBePushed          = true;
        body.PixelColliders       = new List<PixelCollider>(){new(){
          ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
          ManualOffsetX          = -6,
          ManualOffsetY          = -18,
          ManualWidth            = 13,
          ManualHeight           = 24,
          CollisionLayer         = CollisionLayer.HighObstacle,
          Enabled                = true,
          IsTrigger              = false,
        }};

        gun.gameObject.AddComponent<SuncasterAmmoDisplay>();
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        if (player.IsDodgeRolling || !player.AcceptingNonMotionInput || this.extantPrisms.Count >= this.maxPrisms)
          return;
        if (player.CurrentRoom is not RoomHandler room)
          return;
        GameObject prism = _PrismPrefab.Instantiate(position: gun.barrelOffset.position);
        prism.AddComponent<SuncasterPrism>().Setup(player, this, room, gun.CurrentAngle.ToVector(_PRISM_LAUNCH_SPEED));
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        projectile.GetComponent<SuncasterProjectile>().FiredFromGun(this);
        // projectile.gameObject.Play("prism_refract_sound");
        base.gameObject.PlayOnce("suncaster_fire_sound");
    }

    protected override void OnPostDroppedByPlayer(PlayerController player)
    {
        base.OnPostDroppedByPlayer(player);
        foreach (SuncasterPrism prism in this.extantPrisms)
          prism.Selfdestruct();
        this.extantPrisms.Clear();
    }

    protected override void Update()
    {
        base.Update();
        gun.sprite.gameObject.SetGlowiness(10f + 40f * Mathf.Abs(Mathf.Sin(10f * BraveTime.ScaledTimeSinceStartup)));
        if (!this.Player)
            return;

        float now = BraveTime.ScaledTimeSinceStartup;
        float elapsed = (now - this._lastChargeTime);
        if (elapsed >= _CHARGE_RATE)
        {
          int ammoToRestore = Mathf.FloorToInt(elapsed / _CHARGE_RATE);  // account for ammo gained / lost while inactive
          this._lastChargeTime += _CHARGE_RATE * ammoToRestore;
          if (this.gun.CurrentAmmo < this.gun.AdjustedMaxAmmo)
            this.gun.ammo = Math.Min(this.gun.ammo + ammoToRestore, this.gun.AdjustedMaxAmmo);
        }
        // if we have less than the ammo required to shoot a charge shot, make the charge time obscenely long
        this.gun.DefaultModule.chargeProjectiles[1].ChargeTime = (this.gun.ammo >= _CHARGE_AMMO_COST) ? _CHARGE_TIME : 3600f;
    }

    public void AddPrism(SuncasterPrism prism)
    {
      this.extantPrisms.Add(prism);
    }

    public void RemovePrism(SuncasterPrism prism, bool adjustTargets = true)
    {
      this.extantPrisms.Remove(prism);
      if (!adjustTargets)
        return;

      // adjust autotargets for all remaining prisms
      foreach (SuncasterPrism p in this.extantPrisms)
      {
        if (p.target != prism)
          continue;
        p.target = prism.target;
        if (p.target == p)
          p.target = null;  // don't target ourselfs
      }
    }

    public override void MidGameSerialize(List<object> data, int i)
    {
        base.MidGameSerialize(data, i);
        data.Add(this.maxPrisms);
    }

    public override void MidGameDeserialize(List<object> data, ref int i)
    {
        base.MidGameDeserialize(data, ref i);
        this.maxPrisms = (int)data[i++];
    }
}

public class SuncasterAmmoDisplay : CustomAmmoDisplay
{
    private Gun              _gun       = null;
    private Suncaster        _suncaster = null;
    private PlayerController _owner     = null;

    private void Start()
    {
        this._gun       = base.GetComponent<Gun>();
        this._suncaster = this._gun.GetComponent<Suncaster>();
        this._owner     = this._gun.CurrentOwner as PlayerController;
    }

    public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
    {
        if (!this._owner)
            return false;

        uic.SetAmmoCountLabelColor(Color.white);
        Vector3 relVec = Vector3.zero;
        uic.GunAmmoCountLabel.AutoHeight = true; // enable multiline text
        uic.GunAmmoCountLabel.ProcessMarkup = true; // enable multicolor text
        int prismsLeft = this._suncaster.maxPrisms - this._suncaster.extantPrisms.Count;
        uic.GunAmmoCountLabel.Text = $"[sprite \"{Suncaster._PrismUI}\"][color #6666dd]x{prismsLeft}[/color]\n{this._gun.CurrentAmmo}";
        return true;
    }
}

public class SuncasterProjectile : MonoBehaviour
{
    private const float            _DAMAGE_SCALING = 1f;
    private const int              _MAX_LOOPS      = 8;

    private Projectile             _proj           = null;
    private PlayerController       _owner          = null;
    private Vector2                _lastPos        = Vector2.zero;
    private TrailController        _trail          = null;
    private Suncaster              _gun            = null;
    private int                    _prismsLeft     = 0;

    protected List<SuncasterPrism> _hitPrisms      = new();
    protected SuncasterPrism       _lastPrism      = null;
    protected bool                 _canRefract     = true;

    public bool                    charged         = false;

    private void Start()
    {
        this._proj  = base.GetComponent<Projectile>();
        this._owner = this._proj.ProjectilePlayerOwner();
        this._trail = this._proj.AddTrailToProjectileInstance(
          this.charged        ? Suncaster._SunTrailRefractedPrefab :
          (this._gun != null) ? Suncaster._SunTrailPrefab :
                                Suncaster._SunTrailFinalPrefab);
        this._trail.gameObject.SetGlowiness(100f);

        if (this._gun)
          this._prismsLeft = this._gun.extantPrisms.Count * (this.charged ? _MAX_LOOPS : 1);

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

    private Projectile Refract(SuncasterPrism prism, Vector2 newDirection)
    {
      Projectile p = SpawnManager.SpawnProjectile(
          prefab   : Suncaster._SuncasterProjectile.gameObject,
          position : prism.transform.position,
          rotation : Quaternion.identity).GetComponent<Projectile>();
      p.Owner           = this._owner;
      p.baseData.speed  = this._proj.baseData.speed;
      p.baseData.damage = this._proj.baseData.damage;
      p.SendInDirection(newDirection, true);

      SuncasterProjectile s = p.GetComponent<SuncasterProjectile>();
      s._lastPrism = prism;
      s._canRefract = false;
      return p;
    }

    private const float _REFRACT_SOUND_RATE = 0.16f;
    private static float _LastRefractSound  = 0.0f;
    private void Amplify(SuncasterPrism prism)
    {
      if (this.charged)
      {
        if ((--this._prismsLeft) <= 0)
          return;
        this._proj.SendInDirection(prism.Angle(), true);
      }

      this._lastPrism = prism;
      if (this._hitPrisms.Contains(prism))
      {
        if (!this._gun)
          return; // refracted projectiles can only pass through each prism once, so we're done here
      }
      else
      {
        this._hitPrisms.Add(prism);
        this._proj.baseData.damage += _DAMAGE_SCALING;
      }

      if (this._canRefract)
        foreach (SuncasterPrism other in this._gun.extantPrisms)
          if (other != prism)
            Refract(prism, newDirection: other.transform.position - prism.transform.position);

      this._proj.sprite.SetGlowiness(this._proj.baseData.damage);
      this._proj.specRigidbody.Position = new Position(prism.BasePosition());
      this._proj.specRigidbody.UpdateColliderPositions();

      // stop audio events locally only on the prism object
      // prism.gameObject.Play("prism_refract_sound_stop");
      // ...on second thought it's noisy, so stop them everywhere
      if ((_LastRefractSound + _REFRACT_SOUND_RATE) < BraveTime.ScaledTimeSinceStartup)
      {
        _LastRefractSound = BraveTime.ScaledTimeSinceStartup;
        prism.gameObject.PlayUnique("suncaster_fire_sound_high");
      }
    }

    public void FiredFromGun(Suncaster gun) => this._gun = gun;
}

public class SuncasterPrism : MonoBehaviour, IPlayerInteractable
{
    private const float _FRICTION   = 0.9f;
    private const float _BOB_SPEED  = 4f;
    private const float _BOB_HEIGHT = 0.20f;
    private const float _MAX_LIFE   = 6000.0f;
    private const float _TRACE_RATE = 0.15f;

    private PlayerController _owner    = null;
    private tk2dSprite _sprite         = null;
    private bool    _setup             = false;
    private Vector2 _velocity          = Vector2.zero;
    private Vector2 _angle             = Vector2.zero;
    private Vector2 _newAngle          = Vector2.zero;
    private float   _lifetime          = 0.0f;
    private SpeculativeRigidbody _body = null;
    private bool    _trace             = false;
    private float   _last_trace        = 0.0f;
    private Suncaster _gun             = null;
    private bool _autotarget           = true;
    private RoomHandler _room          = null;

    public SuncasterPrism target       = null;

    public void Setup(PlayerController owner, Suncaster gun, RoomHandler room, Vector2 velocity)
    {
      base.gameObject.Play("fire_coin_sound");
      this._owner    = owner;
      this._velocity = velocity;
      this._angle    = velocity.normalized;
      this._room     = room;
      this._sprite   = base.GetComponent<tk2dSprite>();
      this._room.RegisterInteractable(this);

      this._body = gameObject.GetComponent<SpeculativeRigidbody>();
      this._body.OnPreRigidbodyCollision += this.OnPreCollision;
      // this._body.OnCollision        += this.OnCollision;  //TODO: figure out why this doesn't properly bounce off walls
      this._body.Velocity = this._velocity;
      this._body.RegisterTemporaryCollisionException(owner.specRigidbody);
      if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
        this._body.RegisterTemporaryCollisionException(GameManager.Instance.GetOtherPlayer(owner)?.specRigidbody);

      this._gun = gun;
      if (this._gun)
      {
        if (this._gun.extantPrisms.Count > 0)
        {
          this.target = this._gun.extantPrisms.Last();
          this._gun.extantPrisms.First().target = this;
        }
        this._gun.AddPrism(this);
      }

      this._setup    = true;
    }

    private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
      if (otherRigidbody.GetComponent<Projectile>() && !otherRigidbody.GetComponent<SuncasterProjectile>())
        PhysicsEngine.SkipCollision = true;  // don't block projectiles other than SuncasterProjectiles
      if (otherRigidbody.GetComponent<AIActor>())
        PhysicsEngine.SkipCollision = true;  // don't block AIActors
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
      this._room.DeregisterInteractable(this);
      if (this._gun)
        this._gun.RemovePrism(this);
    }

    private void Update()
    {
        if (!this._setup)
          return;

        if (this._room != this._owner.CurrentRoom)
        {
          Selfdestruct();
          return;
        }

        if (this.target && this._autotarget)
          this._angle = ((this.target.transform.position - base.transform.position).normalized);

        if ((this._lifetime += BraveTime.DeltaTime) > _MAX_LIFE)
        {
          Selfdestruct();
          return;
        }

        if (this._body.Velocity.sqrMagnitude < 1f)
          this._body.Velocity = Vector2.zero;
        else
          this._body.Velocity *= (float)Lazy.FastPow(_FRICTION, C.FPS * BraveTime.DeltaTime);

        // old trace targeting code, targeting is unused outside of circumstances where there's only one prism so we don't really need this anymore

        // if (((this._last_trace + _TRACE_RATE) < BraveTime.ScaledTimeSinceStartup))
        // {
        //   this._last_trace = BraveTime.ScaledTimeSinceStartup;
        //   FancyVFX.Spawn(Suncaster._TraceVFX, base.transform.position, velocity: 12f * this._angle, lifetime: 0.5f, fadeOutTime: 0.5f);
        //   // if (this._trace && this._owner)
        //   // {
        //   //   this._newAngle = this._owner.m_currentGunAngle.ToVector().normalized;
        //   //   FancyVFX.Spawn(Suncaster._NewTraceVFX, base.transform.position, velocity: 12f * this._newAngle, lifetime: 0.5f, fadeOutTime: 0.5f);
        //   // }
        // }
    }

    public void SetTarget(SuncasterPrism prism) => this.target = prism;

    public Vector2 Angle() => this._angle;
    public Vector2 BasePosition() => base.transform.position;

    public void Selfdestruct()
    {
      Lazy.DoSmokeAt(base.transform.position);
      GameManager.Instance.gameObject.PlayUnique("prism_destroy_sound");
      UnityEngine.Object.Destroy(base.gameObject);
    }

    public void Interact(PlayerController interactor)
    {
      if (interactor == this._owner)
        Selfdestruct();

      // this._target = null; // disable auto-targeting
      // this._autotarget = false;
      // this._angle = this._newAngle;
      // base.gameObject.Play("prism_interact_sound");
    }

    public void OnEnteredRange(PlayerController interactor)
    {
        if (interactor != this._owner)
          return;
        // this._trace = true;
        SpriteOutlineManager.AddOutlineToSprite(this._sprite, Color.white, 1f, 0f, SpriteOutlineManager.OutlineType.NORMAL);
        this._sprite.UpdateZDepth();
    }

    public void OnExitRange(PlayerController interactor)
    {
        if (interactor != this._owner)
          return;
        // this._trace = false;
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
