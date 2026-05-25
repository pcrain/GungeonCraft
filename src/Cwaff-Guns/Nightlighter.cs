namespace CwaffingTheGungy;

public class Nightlighter : CwaffGun
{
    public static string ItemName         = "Nightlighter";
    public static string ShortDescription = "Energy Efficient";
    public static string LongDescription  = "Fires strings of lights that entangle enemies and weigh them down. Up to 16 strings can be attached to an enemy, with each string reducing an enemy's speed by 10%.";
    public static string Lore             = "The LED light string is an iconic staple of many a festivity and celebration. They're cheap, pretty, and easy to set up...at least the first time. When it comes time to tear them down, they have a propensity for magically tangling themselves up and becoming impossible to untangle the next time you want to use them. At least you can put this magical self-tangling to use in the Gungeon.";

    internal static tk2dSpriteAnimationClip _LightString = null;
    internal static tk2dSpriteAnimationClip _ShinyLightString = null;

    internal static Projectile _NightlighterProjectile = null;

    internal static bool _UseFancyLights = false;

    public static void Init()
    {
        Lazy.SetupGun<Nightlighter>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.SILLY, reloadTime: 1.6f, ammo: 300, shootFps: 12, smoothReload: 0.1f,
            fireAudio: "nightlighter_fire_sound")
          .SetReloadAudio("bulb_unscrew_sound_2", 0, 2)
          .SetReloadAudio("bulb_replace_sound", 4)
          .SetReloadAudio("bulb_unscrew_sound_2", 8, 10)
          .AddDualWieldSynergy(Synergy.CABLE_MANAGEMENT)
          .AssignGun(out Gun gun)
          .LoopAnimation(gun.emptyAnimation, 7)
          .InitProjectile(GunData.New(clipSize: 12, cooldown: 0.4f, shootStyle: ShootStyle.SemiAutomatic, invisibleProjectile: true,
            damage: 11.0f, speed: 75f, range: 30f, force: 12f, hitSound: "nightlighter_impact_sound", customClip: true))
          .Attach<LightStringDoer>()
          .Assign(out _NightlighterProjectile);

        _LightString = VFX.Create("nightlighter_light_string", fps: 12).DefaultAnimation();
        _ShinyLightString = VFX.Create("nightlighter_light_string_shiny", fps: 12).DefaultAnimation();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (projectile.gameObject.GetComponent<LightStringDoer>() is LightStringDoer lsd)
          lsd.Setup(this);
    }
}

public class LightStringDoer : MonoBehaviour
{
  private bool _setup = false;

  public void Setup(Nightlighter gun, AIActor anchorEnemy = null, List<AIActor> enemyChain = null)
  {
    if (base.gameObject.GetComponent<Projectile>() is Projectile proj)
      new GameObject("lightstring").AddComponent<LightString>().Setup(proj, gun, anchorEnemy, enemyChain);
    this._setup = true;
  }

  private void Start()
  {
    if (!this._setup)
      Setup(null);
  }
}

public class LightString : MonoBehaviour
{
    private const int SEGMENTS         = 20;
    private const int _MAX_ATTACHMENTS = 16; // max number of strings attached to a single enemy
    private const float _SPEED_MULT_PER_CHAIN = 0.8f;

    private PlayerController _owner = null;
    private Nightlighter _gun = null;
    private Projectile _projectile = null;
    private AIActor _enemy = null;
    private SpeculativeRigidbody _enemyBody = null;
    private AIActor _anchorEnemy = null;
    private SpeculativeRigidbody _anchorEnemyBody = null;
    private List<AIActor> _enemyChain = null;
    private CwaffRopeMesh _mesh = null;
    private Vector2 _endTransformOffset = default;
    private bool _connectedToGun = false;
    private bool _connectedToProjectile = false;
    private bool _connectedToEnemy = false;
    private bool _anchoredToEnemy = false;
    private bool _mastered = false;
    private bool _setup = false;
    private bool _active = false;
    private Material _mat = null;
    private float _lifetime = 0.0f;
    private bool _fancy = false;

    private static List<LightString> _ExtantChainsOnFloor = new();
    private static ListDictionary _Attachments = new();

    private void Start()
    {
      _ExtantChainsOnFloor.Add(this);
    }

    public bool AttachedToGun => this._active && this._connectedToGun && this._gun != null;

    public void Setup(Projectile proj, Nightlighter gun, AIActor anchorEnemy, List<AIActor> enemyChain)
    {
      if (this._setup)
        return;

      CwaffEvents.OnFloorEnded -= CleanUpExtantChains;
      CwaffEvents.OnFloorEnded += CleanUpExtantChains;
      if (!proj)
      {
        UnityEngine.Object.Destroy(base.gameObject);
        return;
      }
      this._connectedToProjectile = true;
      this._projectile            = proj;
      proj.OnHitEnemy += this.OnHitEnemy;

      this._enemyChain = enemyChain;
      if (this._enemyChain == null)
        this._enemyChain = new();
      this._owner                 = proj.Owner as PlayerController;
      this._gun                   = gun;
      this._anchorEnemy           = anchorEnemy;
      this._anchorEnemyBody       = anchorEnemy ? anchorEnemy.specRigidbody : null;
      this._anchoredToEnemy       = this._anchorEnemy && this._anchorEnemyBody;
      this._connectedToGun        = gun != null && !this._anchoredToEnemy;
      this._mastered              = gun && gun.Mastered;
      if (this._mastered)
        proj.pierceMinorBreakables = true;
      Vector2 endPos              = anchorEnemy ? this._anchorEnemyBody.UnitCenter : (gun ? gun.gun.barrelOffset.transform.position : proj.SafeCenter);
      this._fancy = Nightlighter._UseFancyLights;
      this._mesh                  = CwaffRopeMesh.Create(
        animation: this._fancy ? Nightlighter._ShinyLightString : Nightlighter._LightString, startPos: endPos, endPos: endPos,
        numSegments: SEGMENTS, stretchPolicy: CwaffingTheGungy.RopeSim.StretchPolicy.GROWPERMANENT);
      this._mesh.sprite.HeightOffGround = -10f; // draw behind most things
      if (this._fancy)
        this._mesh.sprite.MakeGlowyBetter(glowAmount: 10.0f, glowColorPower: 10.0f, sensitivity: 0.125f);
      else
        this._mesh.sprite.SetGlowiness(50f, glowColor: Color.white, glowColorPower: 10f);
      this._mat = this._mesh.sprite.renderer.material;
      this._active                = true;
      this._setup                 = true;
    }

    private static void CleanUpExtantChains()
    {
      for (int i = _ExtantChainsOnFloor.Count - 1; i >= 0; --i)
        if (_ExtantChainsOnFloor[i])
          UnityEngine.Object.Destroy(_ExtantChainsOnFloor[i].gameObject);
      _ExtantChainsOnFloor.Clear();
    }

    private void OnHitEnemy(Projectile projectile, SpeculativeRigidbody rigidbody, bool arg3)
    {
        projectile.OnHitEnemy -= this.OnHitEnemy;
        if (!this || this._connectedToEnemy || !rigidbody || !rigidbody.aiActor)
          return;
        if (TryChainLinkEnemy(rigidbody.aiActor))
          projectile.DieInAir();
    }

    private bool TryChainLinkEnemy(AIActor enemy)
    {
      if (!enemy || enemy.healthHaver is not HealthHaver hh)
        return false;
      if (this._enemyChain.Contains(enemy))
        return false;
      if (enemy.specRigidbody is not SpeculativeRigidbody body)
        return false;
      this._connectedToProjectile = false;
      this._projectile = null;
      this._connectedToGun = false;

      this._connectedToEnemy = true;
      this._enemy = enemy;
      this._enemyBody = body;
      this._enemyBody.OnPreMovement += this.ChainDown;
      if (!_Attachments.TryGetValue(enemy, out List<LightString> attachments))
        _Attachments[enemy] = attachments = new();
      if (attachments.Count >= _MAX_ATTACHMENTS)
        attachments.ChooseRandom().Disconnect();
      attachments.Add(this);
      this._enemyChain.Add(enemy);

      Vector3 spriteSize = this._enemy.sprite.GetBounds().size;
          float randomXOffset = 0.25f * spriteSize.x * UnityEngine.Random.value * BraveUtility.RandomSign();
          float randomYOffset = 0.25f * spriteSize.y * UnityEngine.Random.value * BraveUtility.RandomSign();
      this._endTransformOffset += new Vector2(randomXOffset, randomYOffset);

      this._mesh.endPos = enemy.CenterPosition + this._endTransformOffset;
      base.gameObject.Play("light_string_attach_sound");

      if (!this._mastered)
        return true;

      // if mastered, attempt to find another enemy to anchor onto
      ReadOnlyCollection<AIActor> anchorTargets = enemy.CenterPosition.GetAllNearbyEnemies(radius: 12f, ignoreWalls: false);
      foreach (AIActor newTarget in anchorTargets.InRandomOrder())
      {
        if (this._enemyChain.Contains(newTarget) || newTarget.specRigidbody is not SpeculativeRigidbody newBody)
          continue;
        // fire the projectile and register the current enemy as a collision exception
        Vector2 delta = (newTarget.CenterPosition - enemy.CenterPosition);
        Projectile p = VolleyUtility.ShootSingleProjectile(Nightlighter._NightlighterProjectile, enemy.CenterPosition, delta.ToAngle(), false, this._owner);
        p.SetOwnerAndStats(this._owner);
        this._owner.DoPostProcessProjectile(p);
        p.specRigidbody.RegisterSpecificCollisionException(body);
        // set up the new LightString component with the appropriate anchoring information
        LightStringDoer otherString = p.gameObject.GetComponent<LightStringDoer>();
        otherString.Setup(this._gun, anchorEnemy: enemy, enemyChain: this._enemyChain);
        break;
      }
      return true;
    }

    private void ChainDown(SpeculativeRigidbody rigidbody)
    {
        rigidbody.Velocity *= _SPEED_MULT_PER_CHAIN;
    }

    private static readonly List<Color> _ColorsPerFrame = [
      new Color(1.00f, 0.45f, 0.40f),
      new Color(1.00f, 0.50f, 0.25f),
      new Color(1.00f, 0.85f, 0.00f),
      new Color(0.40f, 1.00f, 0.35f),
      new Color(0.60f, 0.85f, 1.00f),
      new Color(1.00f, 0.45f, 1.00f),
    ];

    private void LateUpdate()
    {
      const float DESPAWN_RADIUS = 30.0f;
      const float DESPAWN_RADIUS_SQR = DESPAWN_RADIUS * DESPAWN_RADIUS;
      const float FPS = 5.0f;

      float dtime = BraveTime.DeltaTime;
      if (this._fancy)
      {
        this._lifetime += dtime;
        float frame = FPS * this._lifetime;
        int wholeFrame = (int)frame;
        float brightness = frame - wholeFrame;
        if (brightness > 0.5f)
          brightness = 1.0f - brightness;
        this._mat.SetFloat(CwaffVFX._EmissivePowerId, 50f * brightness);
        this._mat.SetColor(CwaffVFX._EmissiveColorId, _ColorsPerFrame[wholeFrame % 6]);
      }

      if (!this._active)
      {
        // despawn if the player has moved far enough away
        PlayerController nearestPlayer = this._owner;
        if (!nearestPlayer)
          nearestPlayer = GameManager.Instance.BestActivePlayer;
        bool despawn = nearestPlayer == null || this._mesh == null;
        if (!despawn)
        {
          Vector2 ppos = nearestPlayer.CenterPosition;
          if ((ppos - this._mesh.startPos).sqrMagnitude >= DESPAWN_RADIUS_SQR)
            if ((ppos - this._mesh.endPos).sqrMagnitude >= DESPAWN_RADIUS_SQR)
              despawn = true;
        }
        if (despawn)
          UnityEngine.Object.Destroy(base.gameObject);
        return;
      }
      if (!this._setup || dtime == 0.0f || GameManager.Instance.IsPaused)
        return;
      if (this._anchoredToEnemy && (!this._anchorEnemy || !this._anchorEnemyBody || !this._anchorEnemyBody.enabled || this._anchorEnemy.IsGone || (this._anchorEnemy.healthHaver is HealthHaver hha && hha.IsDead)))
      {
        this._anchoredToEnemy = false;
        this._anchorEnemy = null;
        this._anchorEnemyBody = null;
      }
      if (this._connectedToGun && (!this._owner || !this._gun || !this._gun.gun || (this._owner.CurrentGun != this._gun.gun && this._owner.CurrentSecondaryGun != this._gun.gun)))
      {
        Disconnect();
        return;
      }
      if (this._connectedToProjectile && !this._projectile)
      {
        Disconnect();
        return;
      }
      if (this._connectedToEnemy && (!this._enemy || !this._enemyBody || !this._enemyBody.enabled || this._enemy.IsGone || (this._enemy.healthHaver is HealthHaver hh && hh.IsDead)))
      {
        Disconnect();
        return;
      }

      // always update start position if we're connected to a gun or anchor enemy
      if (this._connectedToGun && this._gun)
        this._mesh.startPos = this._gun.gun.barrelOffset.transform.position;
      else if (this._anchoredToEnemy && this._anchorEnemyBody)
        this._mesh.startPos = this._anchorEnemyBody.UnitCenter;

      // if the other end is connected to a projectile, follow it
      if (this._projectile)
      {
        this._mesh.endPos = this._projectile.SafeCenter;
        return;
      }

      // if we have no projectile and we have nothing else to connect to, we're done
      if (!this._enemy || !this._enemyBody)
      {
          Disconnect(); // nothing else to do
          return;
      }

      // update connected enemy
      BraveInput currentInput = BraveInput.GetInstanceForPlayer(this._owner.PlayerIDX);
      Vector2 targetPos = (currentInput == null || this._owner.IsKeyboardAndMouse())
        ? this._owner.unadjustedAimPoint.XY()
        : this._owner.CenterPosition + 20f * currentInput.ActiveActions.Aim.Vector;
      this._mesh.endPos = this._enemyBody.UnitCenter + this._endTransformOffset;
    }

    internal void Disconnect(bool forDestroy = false)
    {
      DeregisterEvents();
      if (this._connectedToEnemy && _Attachments.TryGetValue(this._enemy, out List<LightString> attachments))
      {
        attachments.Remove(this);
        if (attachments.Count == 0)
          _Attachments.Remove(this._enemy);
      }
      if (this._enemyBody)
        this._enemyBody.OnPreMovement -= this.ChainDown;
      this._connectedToEnemy      = false;
      this._enemy                 = null;
      // this._gun                   = null;
      this._connectedToGun        = false;
      this._projectile            = null;
      this._connectedToProjectile = false;
      if (!forDestroy && this._mesh)
        this._mesh.LockWhenStationary(keepAnimating: true); // prevent the chain from updating once it stops moving around much
      this._active = false;
    }

    private void DeregisterEvents()
    {
      if (this._projectile)
        this._projectile.OnHitEnemy -= this.OnHitEnemy;
    }

    private void OnDestroy()
    {
      Disconnect(forDestroy: true);
      if (this._mesh)
        UnityEngine.Object.Destroy(this._mesh.gameObject);
    }
}
