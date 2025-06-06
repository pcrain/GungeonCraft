﻿namespace CwaffingTheGungy;

public class Suncaster : CwaffGun
{
    public static string ItemName         = "Suncaster";
    public static string ShortDescription = "Reflaktive";
    public static string LongDescription  = "Fires weak piercing beams of sunlight. Reload to toss a refractive prism. Uncharged shots that hit a prism will refract towards all other prisms. Charged shots continuously bounce between and refract off of all placed prisms for a short period. Prisms can be reclaimed by interacting with them or by entering a new room. Cannot gain ammo normally, but passively restores ammo over time.";
    public static string Lore             = "An exotic firearm feared throughout the galaxy for its potent solar projectiles. The absence of sunlight in the Gungeon dramatically reduces its ability to gather energy, yet it remains a force to be reckoned with once its energy stores are sufficient.";

    internal const  int             _BASE_MAX_PRISMS         = 6;
    internal const  int             _PRISM_AMMO_COST         = 5;
    internal const  float           _PRISM_LAUNCH_SPEED      = 20f;
    internal const  float           _CHARGE_RATE             = 0.3f;
    internal const  float           _CHARGE_TIME             = 0.65f;
    internal const  int             _CHARGE_AMMO_COST        = 15;

    internal static GameObject      _PrismPrefab             = null;
    internal static GameObject      _TraceVFX                = null;
    internal static CwaffTrailController _SunTrailPrefab          = null;
    internal static CwaffTrailController _SunTrailRefractedPrefab = null;
    internal static CwaffTrailController _SunTrailFinalPrefab     = null;
    internal static Projectile      _SuncasterProjectile     = null;

    [SerializeField] // make sure we keep this when the gun is dropped and picked back up
    private float _lastChargeTime            = 0.0f;
    private bool _cachedSolarFlairSynergy    = false;

    public List<SuncasterPrism> extantPrisms = new();
    public int maxPrisms                     = _BASE_MAX_PRISMS;

    public static void Init()
    {
        Lazy.SetupGun<Suncaster>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.A, gunClass: GunClass.FIRE, reloadTime: 0.0f, ammo: 30,
            canReloadNoMatterAmmo: true, canGainAmmo: false, doesScreenShake: false, shootFps: 30, reloadFps: 40)
          .Attach<SuncasterAmmoDisplay>()
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(clipSize: -1, cooldown: 0.1f, shootStyle: ShootStyle.Charged, pierceBreakables: true, pierceInternalWalls: true,
            damage: 2f, speed: 100f, range: 999999f, fps: 12, anchor: Anchor.MiddleLeft, customClip: true, spawnSound: "suncaster_fire_sound", uniqueSounds: true,
            lightStrength: 10f, lightRange: 3f, lightColor: ExtendedColours.vibrantOrange))
          .Attach<PierceProjModifier>(pierce => pierce.penetration = 999)
          .Attach<SuncasterProjectile>()
          .Assign(out _SuncasterProjectile);

        gun.DefaultModule.chargeProjectiles = new(){
          new ProjectileModule.ChargeProjectile {
            Projectile     = _SuncasterProjectile.Clone().Attach<SuncasterProjectile>(s => s.charged = false),
            ChargeTime     = 0.0f,
            AmmoCost       = 1,
            UsedProperties = ChargeProjectileProperties.ammo,
          },
          new ProjectileModule.ChargeProjectile {
            Projectile     = _SuncasterProjectile.Clone().Attach<SuncasterProjectile>(s => s.charged = true),
            ChargeTime     = _CHARGE_TIME,
            AmmoCost       = _CHARGE_AMMO_COST,
            UsedProperties = ChargeProjectileProperties.ammo,
          },
        };

        _SunTrailPrefab = VFX.CreateSpriteTrailObject("suncaster_beam_mid", fps: 60, cascadeTimer: C.FRAME, softMaxLength: 1, destroyOnEmpty: false);
        _SunTrailRefractedPrefab = VFX.CreateSpriteTrailObject("suncaster_beam_refracted_mid", fps: 60, cascadeTimer: C.FRAME, softMaxLength: 1, destroyOnEmpty: false);
        _SunTrailFinalPrefab = VFX.CreateSpriteTrailObject("suncaster_beam_final_mid", fps: 60, cascadeTimer: C.FRAME, softMaxLength: 1, destroyOnEmpty: false);

        _TraceVFX = VFX.Create("basic_square", fps: 7, emissivePower: 10f);

        _PrismPrefab = VFX.Create("prism_vfx", fps: 7, emissivePower: 5f);
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
        prism.GetComponent<SpeculativeRigidbody>().CorrectForWalls(andRigidBodies: true);
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        SuncasterProjectile sp = projectile.GetComponent<SuncasterProjectile>();
        sp.FiredFromGun(this);
        this._lastChargeTime = BraveTime.ScaledTimeSinceStartup; // reset charge timer after firing
        if (!this.Mastered)
          return;
        foreach (SuncasterPrism prism in this.extantPrisms)
          sp.Refract(prism, projectile.transform.right);
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        foreach (SuncasterPrism prism in this.extantPrisms)
          prism.Selfdestruct();
        this.extantPrisms.Clear();
    }

    public override void OnDestroy()
    {
        foreach (SuncasterPrism prism in this.extantPrisms)
          prism.Selfdestruct();
        this.extantPrisms.Clear();
        base.OnDestroy();
    }

    public override void OnMasteryStatusChanged()
    {
        base.OnMasteryStatusChanged();
        this.gun.DefaultModule.chargeProjectiles[0].AmmoCost = 0;
    }

    public override void Update()
    {
        base.Update();
        gun.sprite.gameObject.SetGlowiness(10f + 40f * Mathf.Abs(Mathf.Sin(10f * BraveTime.ScaledTimeSinceStartup)));
        if (!this.PlayerOwner)
            return;

        float now = BraveTime.ScaledTimeSinceStartup;
        float elapsed = (now - this._lastChargeTime);
        float effectiveChargeRate = _CHARGE_RATE * (this._cachedSolarFlairSynergy ? 0.5f : 1.0f);
        if (elapsed >= effectiveChargeRate)
        {
          int ammoToRestore = Mathf.FloorToInt(elapsed / effectiveChargeRate);  // account for ammo gained / lost while inactive
          this._lastChargeTime += effectiveChargeRate * ammoToRestore;
          if (this.gun.CurrentAmmo < this.gun.AdjustedMaxAmmo)
            this.gun.ammo = Math.Min(this.gun.ammo + ammoToRestore, this.gun.AdjustedMaxAmmo);
          this._cachedSolarFlairSynergy = this.PlayerOwner.HasSynergy(Synergy.SOLAR_FLAIR);
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

        int prismsLeft = this._suncaster.maxPrisms - this._suncaster.extantPrisms.Count;
        uic.GunAmmoCountLabel.Text = $"[sprite \"prism_ui_icon\"][color #6666dd]x{prismsLeft}[/color]\n{this._gun.CurrentAmmo}";
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
    private CwaffTrailController  _trail          = null;
    private Suncaster              _gun            = null;
    private int                    _prismsLeft     = 0;
    private bool                   _setup          = false;

    protected List<SuncasterPrism> _hitPrisms      = new();
    protected SuncasterPrism       _lastPrism      = null;
    protected bool                 _canRefract     = true;

    public bool                    charged         = false;

    private void Start()
    {
      if (!this._setup)
        Setup();
    }

    private void Setup()
    {
      this._proj  = base.GetComponent<Projectile>();
      this._owner = this._proj.ProjectilePlayerOwner();
      this._trail = this._proj.AddTrail(
        this.charged        ? Suncaster._SunTrailRefractedPrefab :
        (this._gun != null) ? Suncaster._SunTrailPrefab :
                              Suncaster._SunTrailFinalPrefab);
      this._trail.gameObject.SetGlowiness(100f);

      if (this._gun)
        this._prismsLeft = this._gun.extantPrisms.Count * (this.charged ? _MAX_LOOPS : 1);

      this._proj.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;

      this._setup = true;
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

    public void Refract(SuncasterPrism prism, Vector2 newDirection)
    {
      if (!prism)
        return;
      if (!this._setup)
        Setup();

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
      {
        if (!this._gun && this._owner)
          this._gun = this._owner.GetGun<Suncaster>();
        if (this._gun)
          foreach (SuncasterPrism other in this._gun.extantPrisms)
            if (other != prism)
              Refract(prism, newDirection: other.transform.position - prism.transform.position);
      }

      this._proj.sprite.SetGlowiness(this._proj.baseData.damage);
      this._proj.specRigidbody.Position = new Position(prism.BasePosition());
      this._proj.specRigidbody.UpdateColliderPositions();

      // stop audio events locally only on the prism object
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
      this._body.OnCollision += this.OnCollision;
      this._body.Velocity = this._velocity;
      this._body.RegisterTemporaryCollisionException(owner.specRigidbody);
      if (GameManager.Instance.GetOtherPlayer(owner) is PlayerController otherPlayer)
        this._body.RegisterTemporaryCollisionException(otherPlayer.specRigidbody);

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

    private void OnCollision(CollisionData rigidbodyCollision)
    {
      if (rigidbodyCollision.CollidedX && rigidbodyCollision.CollidedY)
      {
        Selfdestruct(); // stuck inside something
        return;
      }
      if (rigidbodyCollision.collisionType != CollisionData.CollisionType.TileMap)
        return; // nothing else to do unless we have a tile collision (need to be pushable by player)

      SpeculativeRigidbody body = rigidbodyCollision.MyRigidbody;
      Vector2 newVel            = body.Velocity;
      if (rigidbodyCollision.CollidedX)
        newVel = newVel.WithX(-body.Velocity.x);
      else
        newVel = newVel.WithY(-body.Velocity.y);
      PhysicsEngine.PostSliceVelocity = newVel;
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
    }

    public void OnEnteredRange(PlayerController interactor)
    {
        if (interactor != this._owner)
          return;
        SpriteOutlineManager.AddOutlineToSprite(this._sprite, Color.white, 1f, 0f, SpriteOutlineManager.OutlineType.NORMAL);
        this._sprite.UpdateZDepth();
    }

    public void OnExitRange(PlayerController interactor)
    {
        if (interactor != this._owner)
          return;
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
