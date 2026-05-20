namespace CwaffingTheGungy;

public class DeathNote : CwaffGun
{
    public static string ItemName         = "Death Note";
    public static string ShortDescription = "You Will Know Their Names";
    public static string LongDescription  = "Can be used to see enemies' names. Writing a name ensures the namebearer's untimely death. Increases Curse by 3.";
    public static string Lore             = ""; // TODO: write lore

    internal static GameObject _ReaperVFX = null;
    internal static GameObject _ScytheVFX = null;

    private DeathNoteHUD _hud = null;

    private static GameObject _Scribbles;

    internal string _ownerName = string.Empty;
    internal int nextLetter = 0;

    public static void Init()
    {
        Lazy.SetupGun<DeathNote>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.A, gunClass: GunClass.SILLY, reloadTime: 0.0f, ammo: 444, shootFps: 14, reloadFps: 4, attacksThroughWalls: true,
            curse: 3f, preventDuctTape: true, dynamicBarrelOffsets: true)
          .AddToShop(ItemBuilder.ShopType.Cursula)
          .AssignGun(out Gun gun)
          // .BanFromCoop() // NOTE: unsure how well this will work in co-op. commenting this out for now, we'll see how it goes
          .InitProjectile(GunData.New(clipSize: -1, shootStyle: ShootStyle.Charged, customClip: true, chargeTime: float.MaxValue)); // absurdly high charge value so we never actually shoot

        gun.spriteAnimator.SetLoopPoint(gun.QuickUpdateGunAnimation("open", fps: 10), 3);
        gun.QuickUpdateGunAnimation("close", fps: 30, returnToIdle: true);

        _ReaperVFX = VFX.Create("reaper_vfx", anchor: Anchor.LowerCenter);
        _ScytheVFX = VFX.Create("death_scythe_swing", fps: 30, loops: false, anchor: Anchor.LowerCenter, zHeightOffset: 10f);
        _Scribbles = VFX.Create("scribbles");

        // read name list
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{C.MOD_INT_NAME}.Resources.listofnames.txt"))
        using (StreamReader reader = new StreamReader(stream))
        for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
        {
          string trimmed = line.Trim();
          if (trimmed.Length > 0)
            DeathNoteNameHandler.AddPossibleName(trimmed);
        }
    }

    public override void Update()
    {
        base.Update();
        this.gun.OverrideAngleSnap = 180f;
      }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        this.nextLetter = 0;
        this._ownerName = Lazy.GetPlayerCharacterName(player).ToUpper();
        Lazy.DebugConsoleLog($"picked up by {this._ownerName}");
    }

    public override void OnTriedToInitiateAttack(PlayerController player)
    {
      base.OnTriedToInitiateAttack(player);
      if (this.gun.CurrentAmmo == 0 && !this.gun.InfiniteAmmo)
      {
        DismissHUD(); // can't fire if out of ammo
        return;
      }

      player.SuppressThisClick = true; // always suppress this gun's attacks, for now
      if (player.IsDodgeRolling || player.CurrentInputState != PlayerInputState.AllInput)
        return; // inactive, do normal firing stuff
      if (this.gun.IsReloading || this.gun.CurrentAmmo == 0)
        return; // inactive, do normal firing stuff
      if (!this._hud || !this._hud.Active)
      {
        CreateHUDIfNecessary();
        if (this._hud && !this.gun.m_moduleData[this.gun.DefaultModule].onCooldown) // don't toggle HUD while the weapon is on cooldown
        {
          gun.PlayIfExists("death_note_open", restartIfPlaying: true);
          this._hud.Toggle();
        }
        return;
      }
      WriteInNotebook(player);
    }

    public void WriteInNotebook(PlayerController player)
    {
      base.gameObject.Play("death_note_write_sound");
      char letter = DeathNoteHUD._NAME_LETTERS[DeathNoteHUD.LetterIndexForAngle(player.AimAngleFromCenterOfScreen())];
      DeathNoteNameHandler.WriteLetter(letter, player, this.Mastered);
      CwaffVFX.SpawnBurst(
        prefab           : _Scribbles,
        numToSpawn       : 4,
        basePosition     : base.gun.barrelOffset.transform.position,
        positionVariance : 0.5f,
        baseVelocity     : new Vector2(0, 12f),
        velocityVariance : 6f,
        velType          : CwaffVFX.Vel.Random,
        lifetime         : 1.5f,
        fadeOutTime      : 0.5f,
        randomFrame      : true,
        emissivePower    : 10f,
        startScale       : 0.5f,
        emissiveColor    : Color.white
        );
      this.gun.LoseAmmo(1);
      if (this.gun.CurrentAmmo == 0 && !this.gun.InfiniteAmmo)
        DismissHUD(); // can't fire if out of ammo

      if (string.IsNullOrEmpty(this._ownerName))
        return;
      if (this._ownerName[this.nextLetter] != letter)
      {
        this.nextLetter = 0;
        return;
      }
      if (++this.nextLetter != this._ownerName.Length)
        return;
      this.nextLetter = 0;
      new GameObject().AddComponent<ShinigamiVisit>().Setup(player.healthHaver);
    }

    private void RegisterEvents(PlayerController player)
    {
      if (!player)
        return;
      player.OnReceivedDamage -= this.OnReceivedDamage;
      player.OnReceivedDamage += this.OnReceivedDamage;
      player.OnEnteredCombat -= this.OnEnteredCombat;
      player.OnEnteredCombat += this.OnEnteredCombat;
    }

    private void DeregisterEvents(PlayerController player)
    {
      if (!player)
        return;
      player.OnReceivedDamage -= this.OnReceivedDamage;
      player.OnEnteredCombat -= this.OnEnteredCombat;
    }

    private void OnEnteredCombat()
    {
      if (this.PlayerOwner is PlayerController player && player.CurrentGun == this.gun && player.HasSynergy(Synergy.ILL_TAKE_A_POTATO_CHIP))
        BecomeInvisible(player);
    }

    // copied and simplified from DoEffect() of CardboardBoxItem.cs
    private void BecomeInvisible(PlayerController player)
    {
      player.OnDidUnstealthyAction += BreakStealth;
      player.SetIsStealthed(true, "PotatoChips");
      // Apply a shadowy shader
      foreach (Material m in player.SetOverrideShader(ShaderCache.Acquire("Brave/Internal/HighPriestAfterImage")))
      {
        m.SetFloat(CwaffVFX._EmissivePowerId, 0f);
        m.SetFloat("_Opacity", 0.5f);
        m.SetColor("_DashColor", Color.gray);
      }
    }

    private void BreakStealth(PlayerController player)
    {
      player.ClearOverrideShader();
      player.SetIsStealthed(false, "PotatoChips");
      player.OnDidUnstealthyAction -= BreakStealth;
    }

    private void OnReceivedDamage(PlayerController player)
    {
      DismissHUD();
    }

    public override bool OnManualReloadAttempted(PlayerController player)
    {
      if (!this._hud || !this._hud.Active)
        return true;
      gun.PlayIfExists("death_note_close", restartIfPlaying: true);
      base.gameObject.Play("death_note_close_sound");
      // SpawnManager.SpawnVFX(GameManager.Instance.Dungeon.dungeonDustups.rollLandDustup, gun.barrelOffset.position, Quaternion.identity);
      this._hud.Dismiss();
      return false;
    }

    public override void OnSwitchedToThisGun()
    {
      base.OnSwitchedToThisGun();
      DeathNoteNameHandler.Instance(); // ensure we have a DeathNoteNameHandler
      CreateHUDIfNecessary();
      RegisterEvents(this.PlayerOwner);
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        DismissHUD();
        base.OnSwitchedAwayFromThisGun();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        DismissHUD();
        DeregisterEvents(player);
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
            DeregisterEvents(this.PlayerOwner);
        DismissHUD();
        base.OnDestroy();
    }

    private void CreateHUDIfNecessary()
    {
      if (this._hud)
        return;
      this._hud = base.gameObject.AddComponent<DeathNoteHUD>();
      this._hud.Setup();
    }

    private void DismissHUD()
    {
      if (!this._hud || !this._hud.Active)
        return;
      gun.PlayIfExists("death_note_close", restartIfPlaying: true);
      this._hud.Dismiss();
    }
}

public class ShinigamiVisit : MonoBehaviour
{
  private const float FADE_DELAY = 1.0f;
  private const float LERP_RATE = 5.0f;
  private static readonly Vector2 _HoverOffset = new Vector2(0.0f, 1.0f);
  private static readonly Vector2 _OffscreenOffset = new Vector2(0.0f, 15.0f);

  private GameActor _actor = null;
  private HealthHaver _hh = null;
  private SpeculativeRigidbody _body = null;
  private tk2dSprite _shinigami = null;
  private float _timer = 0.0f;
  private bool _activated = false;
  private bool _setup = false;
  private bool _destroying = false;
  private Vector2 _lastPosition;

  public void Setup(HealthHaver hh)
  {
    this._hh = hh;
    if (!this._hh)
    {
      UnityEngine.Object.Destroy(base.gameObject);
      return;
    }
    this._actor = this._hh.gameActor;
    if (!this._actor || !this._actor.sprite || !this._actor.specRigidbody)
    {
      UnityEngine.Object.Destroy(base.gameObject);
      return;
    }
    this._body = this._actor.specRigidbody;
    if (!this._body || !this._hh || this._hh.IsDead)
    {
      UnityEngine.Object.Destroy(base.gameObject);
      return;
    }
    this._timer = FADE_DELAY;

    this._shinigami = base.gameObject.AddComponent<tk2dSprite>();
    this._shinigami.SetSprite(VFX.Collection, DeathNote._ReaperVFX.GetComponent<tk2dSprite>().spriteId);
    this._shinigami.MakeGlowyBetter(glowColor: Color.red, glowAmount: 2f, glowColorPower: 10.0f);
    this._lastPosition = this._actor.sprite.WorldTopCenter + _OffscreenOffset;
    this._shinigami.PlaceAtPositionByAnchor(this._lastPosition.HoverAt(amplitude: 0.25f), anchor: Anchor.LowerCenter);

    this._setup = true;
  }

  private void Update()
  {
    if (!this._setup || this._destroying)
      return;
    if (!this._actor || !this._actor.sprite || !this._body || !this._hh)
    {
      UnityEngine.Object.Destroy(base.gameObject);
      return;
    }
    this._timer = Mathf.Max(this._timer - BraveTime.DeltaTime, 0f);
    if (!this._activated)
      this._lastPosition = Lazy.SmoothestLerp(this._lastPosition, this._actor.sprite.WorldTopCenter + _HoverOffset, LERP_RATE);

    this._shinigami.PlaceAtPositionByAnchor(this._lastPosition.HoverAt(amplitude: 0.25f), anchor: Anchor.LowerCenter);
    if (this._activated || this._timer > 0 || this._actor.IsGone || !this._hh.IsVulnerable || GameManager.IsBossIntro)
      return;

    this._activated = true;
    this._destroying = true;
    base.StartCoroutine(GlowTime());
    base.gameObject.Play("death_note_scythe_swing_sound");
    CwaffVFX.Spawn(prefab: DeathNote._ScytheVFX, position: this._actor.sprite.WorldBottomCenter, height: 10f);
    CwaffVFX.Spawn(prefab: DeathNote._ScytheVFX, position: this._actor.sprite.WorldBottomCenter, height: 10f, flipX: true);
    CwaffVFX.Spawn(prefab: DeathNote._ScytheVFX, position: this._actor.sprite.WorldBottomCenter, height: 10f, rotation: 45f.EulerZ());
    CwaffVFX.Spawn(prefab: DeathNote._ScytheVFX, position: this._actor.sprite.WorldBottomCenter, height: 10f, flipX: true, rotation: (-45f).EulerZ());
    if (this._actor is PlayerController player)
      this._hh.NextShotKills = true;
    this._hh.minimumHealth = 0f;
    this._hh.ApplyDamage(9999999f, Vector2.zero, "Shinigami", CoreDamageTypes.None, DamageCategory.Unstoppable,
      ignoreInvulnerabilityFrames: true, ignoreDamageCaps: true);
    this._hh.gameObject.PlayUnique("death_note_scythe_hit_sound");
    if (!this._hh.IsDead && !this._hh.IsBoss && !this._hh.IsSubboss && this._actor is AIActor enemy)
      enemy.EraseFromExistenceWithRewards(); // some troublesome enemies like Bloodbulons don't go down as easily
  }

  private IEnumerator GlowTime()
  {
    const float GLOW_TIME = 0.2f;
    const float FADE_TIME = 1.0f;
    const float LERP_OUT_RATE = 3.0f;
    this._shinigami.MakeGlowyBetter(glowColor: Color.red, glowAmount: 2f, glowColorPower: 10.0f);
    for (float elapsed = 0f; elapsed < GLOW_TIME; elapsed += BraveTime.DeltaTime)
    {
        float percentDone = elapsed / GLOW_TIME;
        this._shinigami.MakeGlowyBetter(glowAmount: Mathf.Lerp(2f, 20f, Ease.OutQuad(percentDone)));
        yield return null;
    }
    for (float elapsed = 0f; elapsed < GLOW_TIME; elapsed += BraveTime.DeltaTime)
    {
        float percentDone = elapsed / GLOW_TIME;
        this._shinigami.MakeGlowyBetter(glowAmount: Mathf.Lerp(20f, 2f, Ease.OutQuad(percentDone)));
        yield return null;
    }
    for (float elapsed = 0f; elapsed < FADE_TIME; elapsed += BraveTime.DeltaTime)
    {
        this._lastPosition = Lazy.SmoothestLerp(this._lastPosition, this._lastPosition.WithY(GameManager.Instance.MainCameraController.MaxVisiblePoint.y + 1.5f), LERP_OUT_RATE);
        this._shinigami.PlaceAtPositionByAnchor(this._lastPosition.HoverAt(amplitude: 0.25f), anchor: Anchor.LowerCenter);
        yield return null;
    }
    UnityEngine.Object.Destroy(base.gameObject);
  }
}

public class DeathNoteNameHandler
{
  private static DeathNoteNameHandler _Instance = null;

  private const int _MAX_NAME_LENGTH = 16;
  private static readonly float[] _NameLengthHealthThresholds = [
    0f,    // starting point
    10f,   // up to this much health gets a name of length 1
    20f,   // up to this much health gets a name of length 2
    40f,   // up to this much health gets a name of length 3
    60f,   // up to this much health gets a name of length 4
    90f,   // up to this much health gets a name of length 5
    120f,  // up to this much health gets a name of length 6
    150f,  // up to this much health gets a name of length 7
    200f,  // up to this much health gets a name of length 8
    300f,  // up to this much health gets a name of length 9
    400f,  // up to this much health gets a name of length 10
    500f,  // up to this much health gets a name of length 11
    750f,  // up to this much health gets a name of length 12
    100f,  // up to this much health gets a name of length 13
    1500f, // up to this much health gets a name of length 14
    2000f, // up to this much health gets a name of length 15
    float.MaxValue, // 16
    ];

  private Dictionary<AIActor,DeathNoteNametag> _nametags = null;
  private char _queuedLetter = '\0';
  private bool _preventReset = false;
  private List<char> _bestLetters = null;
  private bool _needsReset = false;
  private PlayerController _owner = null;

  internal static readonly List<List<string>> _PossibleNames = new();

  static DeathNoteNameHandler()
  {
    for (int i = 0; i <= _MAX_NAME_LENGTH; ++i)
      _PossibleNames.Add(new());
  }

  public static DeathNoteNameHandler Instance()
  {
    if (_Instance == null)
      _Instance = new DeathNoteNameHandler();
    return _Instance;
  }

  // private to ensure no manual construction
  private DeathNoteNameHandler()
  {
    this._nametags = new();
    this._bestLetters = new();
    CwaffEvents.OnFloorEnded += OnFloorEnded;
  }

  public static void AddPossibleName(string name)
  {
    DeathNoteNameHandler.Instance();
    if (name.Length > _MAX_NAME_LENGTH)
    {
      Lazy.DebugWarn($" name {name} is too long!");
      return;
    }
    _PossibleNames[name.Length].Add(name);
  }

  private static void OnFloorEnded()
  {
    _Instance._nametags.Clear();
  }

  public static string GenerateName(AIActor enemy, HealthHaver hh)
  {
    float health = hh.AdjustedMaxHealth;
    int nameLength = _NameLengthHealthThresholds.FirstLT(health);
    if (!hh.IsBoss && !hh.IsSubboss && nameLength < _MAX_NAME_LENGTH && UnityEngine.Random.value < 0.2f)
      ++nameLength; // 20% chance to be one letter longer than normal
    string name = _PossibleNames[nameLength].ChooseRandom();
    Lazy.DebugConsoleLog($"{enemy.AmmonomiconName()} with {health} health gets name of length {nameLength}: {name}");
    return name;
  }

  public static void WriteLetter(char c, PlayerController owner, bool preventReset)
  {
    _Instance._queuedLetter = c;
    _Instance._owner = owner;
    _Instance._preventReset = preventReset;
  }

  public static bool OneGoodLetter() => _Instance._bestLetters.Count == 1;

  public static bool IsGoodLetter(char c) => _Instance._bestLetters.Contains(c);

  public static bool ResetNameProgress() => _Instance._needsReset = true;

  internal static readonly List<string> _ActiveNames = new();

  public IEnumerable GetNameTags()
  {
    // phase 1: determine names that are no longer in use
    this._nametags.RemoveDeadKeys();
    _ActiveNames.Clear();
    foreach (DeathNoteNametag tag in this._nametags.Values)
      _ActiveNames.Add(tag.name);

    // phase 2: actually compute and return nametags
    bool resetNames = this._needsReset;
    this._needsReset = false;
    bool newLetter = (this._queuedLetter != '\0');
    this._bestLetters.Clear();
    int longestName = 0;
    for (int i = 0; i < StaticReferenceManager.AllEnemies.Count; i++)
    {
      AIActor enemy = StaticReferenceManager.AllEnemies[i];
      if (!enemy || enemy.healthHaver is not HealthHaver hh || hh.IsDead)
        continue;
      if (!enemy.isActiveAndEnabled || !enemy.IsWorthShootingAt)
        continue;
      if (enemy.m_spriteDimensions == default) // HACK: what we're actually checking is if the enemy has called Start() yet and, e.g., become a black phantom
      {
        // Lazy.DebugConsoleLog("hasn't called start yet");
        continue;
      }
      if (!this._nametags.TryGetValue(enemy, out DeathNoteNametag tag))
      {
        const int DUPLICATE_PREVENTION_ATTEMPTS = 100;
        string name = null;
        int tries = DUPLICATE_PREVENTION_ATTEMPTS;
        while (tries-- > 0 && (string.IsNullOrEmpty(name) || _ActiveNames.Contains(name)))
          name = DeathNoteNameHandler.GenerateName(enemy, hh);
        this._nametags[enemy] = tag = DeathNoteNametag.Generate(enemy, hh, name);
      }
      if (resetNames)
        tag.ResetName();
      if (newLetter)
        tag.HandleLetter(this._queuedLetter, this._owner, this._preventReset);
      if (tag.nextLetter < tag.nameLength) // if our name isn't completely spelled out
      {
        if (tag.nextLetter > longestName)
        {
          longestName = tag.nextLetter;
          this._bestLetters.Clear();
        }
        if (tag.nextLetter == longestName)
          this._bestLetters.Add(tag.uppername[tag.nextLetter]);
      }
      yield return tag;
    }
    if (newLetter)
      this._queuedLetter = '\0';
    yield break;
  }
}

public class DeathNoteNametag
{
  public string name = null;
  public string uppername = null;
  public string markupName = null;
  public AIActor actor = null;
  public HealthHaver hh = null;
  public int nameLength = -1;
  public int nextLetter = 0;

  private bool _dirty = false;
  private bool _dying = false;

  public static DeathNoteNametag Generate(AIActor enemy, HealthHaver hh, string name)
  {
      DeathNoteNametag tag = new DeathNoteNametag();
      tag.actor = enemy;
      tag.hh = hh;
      tag.nextLetter = 0;
      tag.name = name;
      tag.uppername = tag.name.ToUpper();
      tag.nameLength = tag.name.Length;
      tag._dirty = true;
      return tag;
  }

  public void HandleLetter(char c, PlayerController owner, bool preventReset)
  {
    if (this._dying || !hh || !actor)
      return;

    this._dirty = true;
    if (c != this.uppername[nextLetter])
    {
      if (!preventReset)
        nextLetter = 0;
      return;
    }
    ++nextLetter;
    if (nextLetter != nameLength)
      return;

    if (!preventReset)
      DeathNoteNameHandler.ResetNameProgress(); // once an enemy has been killed, reset progress on all other names

    this._dying = true;
    if (owner)
      owner.DidUnstealthyAction();
    new GameObject().AddComponent<ShinigamiVisit>().Setup(hh);
  }

  public void ResetName()
  {
    if (!this._dying)
    {
      this.nextLetter = 0;
      this._dirty = true;
    }
  }

  public void PlaceNametag(dfLabel label)
  {
    if (this._dirty)
    {
      this.markupName = "[color #dd6666]" + name.Substring(0, nextLetter) + "[/color]" + name.Substring(nextLetter);
      this._dirty = false;
    }
    label.IsVisible = true;
    label.Opacity = 1.0f;
    label.Text = this.markupName;
    Vector2 pos = actor.sprite.WorldTopCenter + new Vector2(0.0f, 1.0f);
    if (this._dying)
      pos += Lazy.RandomVector(0.0625f);
    label.Place(pos);
  }
}

public class DeathNoteHUD : MonoBehaviour
{
  internal const string _NAME_LETTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

  private const int _NUM_LETTERS = 26;
  private const float _WEDGE_ARC = 360f / _NUM_LETTERS;
  private const float _SHWOOP_TIME = 0.3f;
  private const float _BASE_GEOM_ALPHA = 0.3f;

  private static readonly Color _GeomColor1 = new Color(0.25f, 0.25f, 0.25f);
  private static readonly Color _GeomColor2 = new Color(0.35f, 0.35f, 0.35f);

  private bool _setup                 = false;   // whether we're set up
  private float _shwoop               = 0.0f;    // whether we're shwooped open
  private bool _active                = false;   // whether we're active
  private DeathNote _gun              = null;    // gun we're attached to
  private List<Geometry> _geometry    = new();   // all shapes rendered by the HUD
  private Geometry       _selector    = new();   // extra selector rendered by the HUD
  private List<dfLabel> _labels       = new();   // all letter labels rendered by the HUD
  private List<dfLabel> _nametags     = new();   // all nametag labels rendered by the HUD

  private CameraController _camera;
  private Vector2 _worldBottomLeft;
  private Vector2 _worldTopRight;
  // private Vector2 _basePos;

  public bool Active => this._active;

  public void Setup()
  {
    this._gun = this.gameObject.GetComponent<DeathNote>();

    this._selector = Geometry.Create(Geometry.Shape.RING).Place(color: Color.red.WithAlpha(_BASE_GEOM_ALPHA)).UseGUILayer();
    for (int i = 0; i < _NUM_LETTERS; ++i)
    {
      this._geometry.Add(Geometry.Create(Geometry.Shape.RING).Place(color: ((i % 2 == 0) ? _GeomColor1 : _GeomColor2).WithAlpha(_BASE_GEOM_ALPHA)).UseGUILayer());
      this._labels.Add(EasyLabel.Create(unicode: false, outline: true, align: TextAlignment.Center));
      this._labels[i].Text = new string(_NAME_LETTERS[i], 1);
      this._labels[i].Pivot = dfPivotPoint.MiddleCenter;
    }

    Dismiss(force: true);
    this._setup = true;

    this._camera = GameManager.Instance.MainCameraController;
    if (this._camera)
    {
      this._camera.OnFinishedFrame -= this.OnFinishedFrame;
      this._camera.OnFinishedFrame += this.OnFinishedFrame; // synchronize HUD elements with camera for pseudo-overlay effect
    }
  }

  public void Toggle()
  {
    if (this._active)
      Dismiss();
    else
      Engage();
  }

  public void Engage()
  {
    if (this._active)
      return;

    this._active = true;
    this._shwoop = 0.0f;
    if (base.gameObject.RequestCameraControl(relinquishAction: OnCameraRelinquish))
      GameManager.Instance.MainCameraController.OverridePosition = this._gun.PlayerOwner.CenterPosition;
    base.gameObject.Play("death_note_open_sound");
    // BraveTime.SetTimeScaleMultiplier(0.5f, base.gameObject);
  }

  private void OnCameraRelinquish()
  {
    // Lazy.DebugConsoleLog($" death note relinquished camera");
  }

  public void Dismiss(bool force = false, bool deactivate = true)
  {
    if (!this._active && !force)
      return;

    foreach (Geometry g in this._geometry)
      if (g)
          g.Disable();
    if (this._selector)
      this._selector.Disable();

    foreach (dfLabel label in this._labels)
    {
      if (label == null)
        continue;
      label.Opacity = 0.0f;
      label.IsVisible = false;
    }

    foreach (dfLabel label in this._nametags)
    {
      if (label == null)
        continue;
      label.Opacity = 0.0f;
      label.IsVisible = false;
    }

    if (deactivate)
    {
      this._active = false;
      // BraveTime.SetTimeScaleMultiplier(1.0f, base.gameObject);
      base.gameObject.RelinquishCameraControl();
    }
  }

  private void Update()
  {
    if (!this._setup)
      return;
    if (!this._camera || !this._gun || this._gun.gun is not Gun gun || this._gun.PlayerOwner is not PlayerController player)
    {
      Dismiss();
      UnityEngine.Object.Destroy(this);
    }
    else if (GameManager.Instance.IsPaused)
      Dismiss(deactivate: false);
    else if (player.CurrentInputState != PlayerInputState.AllInput || gun.IsReloading || GameManager.IsBossIntro)
      Dismiss();
    else if (this._active)
    {
      base.gameObject.RequestCameraControl(relinquishAction: OnCameraRelinquish);
      if (base.gameObject.HasControlOverCamera())
        GameManager.Instance.MainCameraController.OverridePosition = this._gun.PlayerOwner.CenterPosition;
    }
  }

  private void OnFinishedFrame()
  {
    if (!this._setup || !this._active || !this._camera || GameManager.Instance.IsPaused || !this._gun || !this._gun.PlayerOwner || this._gun.PlayerOwner.CurrentInputState != PlayerInputState.AllInput)
      return;

    Engage();
    // UpdateLabelsForUISize();
    if (this._active)
      PlaceHUDElements();
  }

  internal static int LetterIndexForAngle(float angle)
    => Mathf.FloorToInt((angle.Clamp360() + 0.5f * _WEDGE_ARC) / _WEDGE_ARC) % _NUM_LETTERS;

  private void PlaceHUDElements()
  {
    PlayerController player = this._gun.PlayerOwner;
    if (!player || player.CurrentGun != this._gun.gun)
    {
      Dismiss();
      return;
    }

    float gunAngle = player.AimAngleFromCenterOfScreen().Clamp360();
    int curSegment = LetterIndexForAngle(gunAngle);

    this._shwoop = Mathf.Clamp01(this._shwoop + BraveTime.DeltaTime / _SHWOOP_TIME);
    float ease = Ease.OutQuad(this._shwoop);

    this._worldBottomLeft = this._camera.MinVisiblePoint;
    this._worldTopRight   = this._camera.MaxVisiblePoint;
    Vector2 screenCenter = 0.5f * (this._worldBottomLeft + this._worldTopRight);
    float screenHeight = this._camera.MaxVisiblePoint.y - this._camera.MinVisiblePoint.y;
    float shwoopHeight = ease * screenHeight;
    float geomAlpha = _BASE_GEOM_ALPHA * ease;
    float innerRadius = 0.225f * screenHeight;
    float outerRadius = innerRadius + 0.04f * shwoopHeight;
    float labelRadius = 0.5f * (innerRadius + outerRadius);

    this._selector.Disable();
    for (int i = 0; i < _NUM_LETTERS; ++i)
    {
      bool sel = (i == curSegment);
      bool goodLetter = DeathNoteNameHandler.IsGoodLetter(_NAME_LETTERS[i]);
      this._geometry[i].Place(pos: screenCenter, angle: _WEDGE_ARC * i, arc: _WEDGE_ARC,
        radiusInner: innerRadius, radius: outerRadius * (sel ? 1.125f : 1.0f),
        color: (sel ? Color.white : (i % 2 == 0) ? _GeomColor1 : _GeomColor2).WithAlpha(geomAlpha));
      Vector2 labelPos = screenCenter + (_WEDGE_ARC * i).ToVector(labelRadius);
      if (sel)
      {
        labelPos += Lazy.RandomVector(1/32f);
      }
      if (goodLetter && DeathNoteNameHandler.OneGoodLetter())
        this._selector.Place(pos: screenCenter, angle: _WEDGE_ARC * i, arc: _WEDGE_ARC,
          radiusInner: innerRadius * 0.3f, radius: innerRadius * 0.9f,
          color: (Color.Lerp(Color.red, ExtendedColours.pink, Mathf.Abs(Mathf.Sin(12f * BraveTime.ScaledTimeSinceStartup)))).WithAlpha(geomAlpha));
      this._labels[i].Color = (sel ? Color.black : goodLetter ? Color.red : Color.white).WithAlpha(Mathf.Clamp01(2f * ease - 1f));
      this._labels[i].OutlineColor = (sel ? Color.white : Color.black).WithAlpha(Mathf.Clamp01(2f * ease - 1f));
      this._labels[i].Opacity = ease;
      this._labels[i].IsVisible = true;
      this._labels[i].Place(labelPos);
    }

    int ni = 0;
    foreach (DeathNoteNametag tag in DeathNoteNameHandler.Instance().GetNameTags())
    {
      if (ni >= this._nametags.Count)
      {
        dfLabel label = EasyLabel.Create(unicode: false, outline: true, align: TextAlignment.Center);
        label.ProcessMarkup = true;
        this._nametags.Add(label);
      }
      tag.PlaceNametag(this._nametags[ni]);
      ++ni;
    }
    for (; ni < this._nametags.Count; ++ni)
    {
      this._nametags[ni].IsVisible = false;
      this._nametags[ni].Opacity = 0.0f;
    }
  }
}
