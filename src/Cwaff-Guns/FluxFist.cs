namespace CwaffingTheGungy;

// NOTES:
//   - attract = south = blue = confers negative polarization
//   - repel   = north = red  = confers positive polarization

public class FluxFist : CwaffGun
{
    public static string ItemName         = "Flux Fist";
    public static string ShortDescription = "Pole to Pole";
    public static string LongDescription  = "Alternates between firing north and south polarity beams that magnetize enemies. Reload to toss or dismiss a north-polarized ball that damages and repels / attracts nearby magnetized enemies.";
    public static string Lore             = "A repurposed greave retrofitted with a proprietary piezomagnetic mechanism, capable of generating strong magnetic fields with a clench of the fist. It was originally conceived as a consumer product for retrieving one's dropped keys from underneath the fridge. Sadly, it was recalled hours after hitting store shelves due to a common issue with entire fridges crashing into users.";

    private const float _BALL_LAUNCH_SPEED = 20f;
    private const float _SOUND_TIMER       = 0.25f;

    private static readonly string[] _ExtraAnims = [
      "north_fire",
      "south_fire",
      "north_idle",
      "south_idle",
    ];

    internal static GameObject _MagnetBallPrefab = null;
    internal static GameObject _RepelParticle    = null;
    internal static GameObject _AttractParticle  = null;

    private MagnetBall _magnetBall               = null;
    private bool _attract                        = false;
    private bool _toggleWhenDoneFiring           = false;
    private PlayerController _lastOwner          = null;

    private static Projectile NorthBeam          = null;
    private static Projectile SouthBeam          = null;

    public static void Init()
    {
        Lazy.SetupGun<FluxFist>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.D, gunClass: GunClass.BEAM, reloadTime: 0.0f, ammo: 600, shootFps: 14, reloadFps: 4,
            fireAudio: null, reloadAudio: null, handedness: GunHandedness.HiddenOneHanded, modulesAreTiers: true)
          .AssignGun(out Gun gun);

        for (int i = 0; i < 2; ++i)
        {
          string beamSprite = (i == 0) ? "flux_fist_beam_north" : "flux_fist_beam_south";
          GunData beamData = GunData.New(gun: gun, baseProjectile: Items.Moonscraper.Projectile(), //NOTE: inherit from Moonscraper for hitscan
            clipSize: -1, cooldown: 0.25f, shootStyle: ShootStyle.Beam, damage: 4f, force: 0f, speed: -1f, ammoCost: 4,
            angleVariance: 0f, beamSprite: beamSprite, beamFps: 36, beamChargeFps: 8, beamImpactFps: 30,
            beamLoopCharge: false, beamReflections: 0, beamChargeDelay: 0f, beamEmission: 250f, beamImpactEmission: 50f,
            beamEmissionColor: Color.Lerp(Color.black, (i == 0) ? Color.red : Color.blue, 0.5f), beamEmissionColorPower: 1.5f);
          if (i == 0)
          {
            NorthBeam = gun.InitProjectile(beamData);
            NorthBeam.gameObject.AddComponent<MagnetBeamBehavior>().attract = false;
            gun.DefaultModule.SetupCustomAmmoClip("flux_fist_north");
          }
          else
          {
            ProjectileModule mod2 = new ProjectileModule().InitSingleProjectileModule(beamData);
            gun.Volley.projectiles.Add(mod2);
            SouthBeam = mod2.projectiles[0];
            SouthBeam.gameObject.AddComponent<MagnetBeamBehavior>().attract = true;
            mod2.SetupCustomAmmoClip("flux_fist_south");
          }
        }

        foreach (string anim in _ExtraAnims)
          gun.QuickUpdateGunAnimation(anim);

        _MagnetBallPrefab = VFX.Create("magnet_ball");
        _MagnetBallPrefab.AutoRigidBody(canBePushed: true);
        KnockbackDoer kbd = _MagnetBallPrefab.AddComponent<KnockbackDoer>();
        kbd.weight = 10f;

        _RepelParticle = VFX.Create("flux_fist_repel_vfx", fps: 10, loops: false);
        _AttractParticle = VFX.Create("flux_fist_attract_vfx", fps: 10, loops: false);
    }

    private void CheckFlight(bool newEnabled)
    {
      if (!this._lastOwner)
        return;
      if (newEnabled == this._lastOwner.m_isFlying.HasOverride(ItemName))
        return;
      this._lastOwner.SetIsFlying(newEnabled, ItemName);
      if (newEnabled)
        this._lastOwner.AdditionalCanDodgeRollWhileFlying.AddOverride(ItemName);
      else
        this._lastOwner.AdditionalCanDodgeRollWhileFlying.RemoveOverride(ItemName);
    }

    public override void OnMasteryStatusChanged()
    {
        base.OnMasteryStatusChanged();
        CheckFlight(true);
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        this._lastOwner = this.PlayerOwner;
        CheckFlight(this.Mastered);
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        CheckFlight(false);
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        if (this._lastOwner == this.PlayerOwner)
          CheckFlight(false);
        base.OnDestroy();
    }

    public override void OverrideShootAnimation(ref string overrideAnimation)
    {
        overrideAnimation = this._attract ? "flux_fist_south_fire" : "flux_fist_north_fire";
    }

    public override void PostProcessBeam(BeamController beam)
    {
        base.PostProcessBeam(beam);
        this._toggleWhenDoneFiring = true;
        beam.projectile.OnHitEnemy += this.PolarizeEnemy;
    }

    private void PolarizeEnemy(Projectile projectile, SpeculativeRigidbody rigidbody, bool arg3)
    {
        const float POLARIZE_SECONDS = 2.0f;
        const float POLARIZE_STRENGTH = 1f / POLARIZE_SECONDS;
        if (projectile.gameObject.GetComponent<MagnetBeamBehavior>() is not MagnetBeamBehavior mbb)
          return;
        float strength = this.Mastered ? 3.0f : 1.0f;
        rigidbody.gameObject.GetOrAddComponent<MagnetizedEnemyBehavior>()
          .Polarize(BraveTime.DeltaTime * POLARIZE_STRENGTH * (mbb.attract ? -strength : strength));
    }

    private void TogglePolarity()
    {
        this._attract = !this._attract;
        this.gun.CurrentStrengthTier = this._attract ? 1 : 0;
        gun.idleAnimation = this._attract ? "flux_fist_south_idle" : "flux_fist_north_idle";
        gun.PlayIfExists(gun.idleAnimation, restartIfPlaying: true);
    }

    private void LateUpdate()
    {
        if (this.gun.IsFiring)
          base.gameObject.Play(this._attract ? "flux_fist_attract_sound" : "flux_fist_repel_sound", soundRate: _SOUND_TIMER);
        else if (this._toggleWhenDoneFiring)
        {
          this._toggleWhenDoneFiring = false;
          TogglePolarity();
        }
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        if (this._magnetBall)
        {
          this._magnetBall.Selfdestruct();
          this._magnetBall = null;
          return;
        }
        if (player.CurrentRoom is not RoomHandler room)
          return;
        this._magnetBall = _MagnetBallPrefab.Instantiate(position: gun.barrelOffset.position).AddComponent<MagnetBall>();
        this._magnetBall.Setup(room, player, gun.CurrentAngle.ToVector(_BALL_LAUNCH_SPEED));
        this._magnetBall.gameObject.GetComponent<SpeculativeRigidbody>().CorrectForWalls(andRigidBodies: true);
    }
}

public class MagnetBeamBehavior : MonoBehaviour
{
    public bool attract = false;
}

public class MagnetizedEnemyBehavior : MonoBehaviour
{
  private const float _PARTICLE_TIME = 0.5f;

  private static readonly Color _RepelColor = new Color(242f/255f, 81f/255f, 81f/255f);
  private static readonly Color _AttractColor = new Color(81f/255f, 178f/255f, 242f/255f);

  public float polarity = 0.0f;

  private bool _setup = false;
  private AIActor _enemy = null;
  private KnockbackDoer _kbd = null;
  private HealthHaver _hh = null;
  private float _lastParticles = 0.0f;

  private void Start()
  {
    this._setup  = true;
    this._enemy  = base.gameObject.GetComponent<AIActor>();
    this._kbd    = base.gameObject.GetComponent<KnockbackDoer>();
    this._hh     = base.gameObject.GetComponent<HealthHaver>();
  }

  public void Polarize(float amt)
  {
    this.polarity = Mathf.Clamp(this.polarity + amt, -1.0f, 1.0f);
  }

  private void Update()
  {
    if (!this._setup || !this._enemy || this._enemy.IsGone || !this._hh || this._hh.IsDead || !this._hh.IsVulnerable)
      return;

    int strength = Mathf.CeilToInt(Mathf.Abs(this.polarity) * 10.0f);
    if (strength == 0)
      return;

    float now = BraveTime.ScaledTimeSinceStartup;
    if (this._lastParticles + (_PARTICLE_TIME / strength) > now)
      return;

    this._lastParticles = now;
    CwaffVFX.SpawnBurst(
      prefab           : this.polarity > 0 ? FluxFist._RepelParticle : FluxFist._AttractParticle,
      numToSpawn       : 2,
      basePosition     : this._enemy.CenterPosition,
      positionVariance : 0.5f,
      velocityVariance : 0.5f + 0.5f * strength,
      velType          : CwaffVFX.Vel.AwayRadial,
      rotType          : CwaffVFX.Rot.Random,
      lifetime         : 0.5f,
      emissivePower    : (this.polarity > 0) ? 50f : 100f, // NOTE: blue doesn't emit at well, so make it more emissive
      emissiveColor    : this.polarity > 0 ? _RepelColor : _AttractColor,
      height           : 8f,
      anchorTransform  : this._enemy.sprite.transform
      );
  }

  private const float _MIN_INFLUENCE_TIME    = 0.1f;
  private const float _INFLUENCE_TIME_FACTOR = 0.01f;
  private const float _MAX_INFLUENCE_TIME    = 1.0f;
  private float _lastInfluenceTime = 0.0f;

  public void HandleInfluenceParticles(Vector2 sourcePos, float influence)
  {
    const float LIFETIME = 0.5f;

    if (influence <= 0 || this.polarity == 0.0f || !this._enemy)
      return;

    float now = BraveTime.ScaledTimeSinceStartup;
    float nextInfluenceTime = this._lastInfluenceTime + Mathf.Clamp(_INFLUENCE_TIME_FACTOR / influence, _MIN_INFLUENCE_TIME, _MAX_INFLUENCE_TIME);
    if (now < nextInfluenceTime)
      return;

    this._lastInfluenceTime = now;
    bool repel = (this.polarity > 0);
    Vector2 pos = this._enemy.CenterPosition;
    Vector2 delta = repel ? (pos - sourcePos) : (sourcePos - pos);
    if (delta.sqrMagnitude < 1f)
      return; // don't spawn particles when enemies are right on top of the ball

    CwaffVFX.Spawn(
      prefab        : repel ? FluxFist._RepelParticle : FluxFist._AttractParticle,
      position      : repel ? sourcePos : pos,
      rotation      : Quaternion.identity,
      velocity      : (1f / LIFETIME) * delta,
      lifetime      : LIFETIME,
      emissivePower : 50f,
      startScale    : 3.0f,
      endScale      : 0.5f
      );
  }
}

public class MagnetBall : MonoBehaviour
{
    private const float _FRICTION      = 0.9f;
    private const float _SOUND_RATE    = 0.3f;

    private bool _setup                = false;
    private RoomHandler _room          = null;
    private PlayerController _owner    = null;
    private tk2dBaseSprite _sprite     = null;
    private SpeculativeRigidbody _body = null;
    private KnockbackDoer _kbd         = null;
    private Dictionary<AIActor, ActiveKnockbackData> _KnockbackDict = new();

    public void Setup(RoomHandler room, PlayerController owner, Vector2 velocity)
    {
      this._setup = true;
      this._owner = owner;
      this._room = room;

      tk2dBaseSprite sprite = base.gameObject.GetComponent<tk2dBaseSprite>();
      sprite.usesOverrideMaterial = true;
      // sprite.renderer.material.shader = ShaderCache.Acquire("Brave/PlayerShader");

      this._body = gameObject.GetComponent<SpeculativeRigidbody>();
      this._body.Velocity = velocity;
      this._body.RegisterTemporaryCollisionException(owner.specRigidbody);
      if (GameManager.Instance.GetOtherPlayer(owner) is PlayerController otherPlayer)
        this._body.RegisterTemporaryCollisionException(otherPlayer.specRigidbody);

      this._kbd = gameObject.GetComponent<KnockbackDoer>();

      this._body.OnBeamCollision += this.OnBeamCollision;

      this._sprite = this._body.DecoupleSprite(); // NOTE: lets us move the sprite around independent of the SRB

      base.gameObject.Play("magnet_ball_attract_sound");
    }

    private void OnBeamCollision(BeamController beam)
    {
      const float PUSH_FORCE = 75f;
      if (this._kbd.m_isImmobile.Value || !beam || beam.gameObject.GetComponent<MagnetBeamBehavior>() is not MagnetBeamBehavior mbb)
        return;
      Vector2 velocity = BraveTime.DeltaTime * beam.Direction.ToAngle().ToVector(mbb.attract ? -PUSH_FORCE : PUSH_FORCE);
      this._body.Velocity += velocity;
    }

    public void Selfdestruct()
    {
        Lazy.DoSmokeAt(base.transform.position);
        UnityEngine.Object.Destroy(base.gameObject);
    }

    private void Update()
    {
      const float _MAX_RADIUS = 30f;
      const float _FORCE = 50f;
      const float _DAMAGE = 10f;

      if (!this._setup)
        return;

      if (!this._owner || this._owner.IsGhost || this._owner.CurrentRoom != this._room)
      {
          Selfdestruct();
          return;
      }

      float now = BraveTime.ScaledTimeSinceStartup;
      this._sprite.PlaceAtPositionByAnchor(base.transform.position.HoverAt(0.25f, 10f), Anchor.MiddleCenter);
      this._sprite.UpdateZDepth();
      this._sprite.SetGlowiness(30f + 50f * Mathf.Abs(Mathf.Sin(8f * now)));

      float dtime = BraveTime.DeltaTime;

      if (this._body.Velocity.sqrMagnitude < 0.00001f)
        this._body.Velocity = Vector2.zero;
      else
        this._body.Velocity *= Mathf.Pow(_FRICTION, C.FPS * dtime);

      bool didAnything = false;
      Vector2 basePos = this._body.UnitCenter;
      foreach (AIActor enemy in Lazy.GetAllNearbyEnemies(basePos, _MAX_RADIUS, ignoreWalls: true))
      {
        if (enemy.gameObject.GetComponent<MagnetizedEnemyBehavior>() is not MagnetizedEnemyBehavior meb)
          continue;
        if (meb.polarity == 0.0f)
          continue;
        float maxReach = Mathf.Abs(meb.polarity) * _MAX_RADIUS;
        float maxSquareReach = maxReach * maxReach;
        Vector2 towardsMagnetBall = basePos - enemy.CenterPosition;
        float sqrMagnitude = towardsMagnetBall.sqrMagnitude;
        if (sqrMagnitude >= maxSquareReach)
          continue;
        float influence = (1f - sqrMagnitude / maxSquareReach);
        float kbstrength = ((meb.polarity > 0) ? -_FORCE : _FORCE) * influence;
        float damageStrength = _DAMAGE * influence * dtime;
        enemy.ApplyContinuousSourcedKnockback(base.gameObject, _KnockbackDict, kbstrength * towardsMagnetBall.normalized);
        enemy.healthHaver.ApplyDamage(damageStrength, Vector2.zero, "Flux Field", CoreDamageTypes.None, DamageCategory.Environment);
        meb.HandleInfluenceParticles(basePos, 1f - Mathf.Sqrt(1f - influence));
        didAnything = true;
      }
      if (didAnything)
        base.gameObject.Play("magnet_ball_attract_sound", soundRate: _SOUND_RATE);
    }
}
