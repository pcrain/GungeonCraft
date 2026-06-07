namespace CwaffingTheGungy;

/* TODO:
    - save serialization for stored slimes
    - fix firing when holding fire while switching modes
    - fix slimes that dodge the vacuum wave and get stuck to walls in their initial velocity direction until retriggering vacuum
    - prevent volley modifications such as Scattershot
*/

public class Vacpack : CwaffGun
{
    public static string ItemName         = "Vacpack";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject _VacpackVFX = null;

    internal const float _REACH            =  9.00f; // how far (in tiles) the gun reaches
    internal const float _ABSORB_RANGE     =  1.50f; // how far (in tiles) the gun absorbs slimes
    internal const float _SPREAD           =    15f; // radius (in degrees) of suction cone at the end of our reach
    internal const float _ACCEL_SEC        =  3.50f; // speed (in tiles per second) at which debris accelerates towards the gun near the end of the gun's reach
    internal const float _SUCK_FORCE       = 100.0f; // force with which slimes are sucked towards the vacuum

    internal const float _SQR_REACH        = _REACH * _REACH;
    internal const float _SQR_ABSORB_RANGE = _ABSORB_RANGE * _ABSORB_RANGE;

    private const float _HUD_HOLD_TIME = 0.25f;

    public List<int> slimeCounts;

    private Dictionary<AIActor, ActiveKnockbackData> _KnockbackDict = new();
    private List<SlimyboiController> _vacSlimes = new();
    private VacpackHUD _hud = null;
    private int _curSlime = -1;
    private int _lastSlime = (int)SlimyboiType.Pink;
    private float _hudHoldTimer = 0.0f;

    public static void Init()
    {
        Lazy.SetupGun<Vacpack>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.EXCLUDED, gunClass: CwaffGunClass.UTILITY, reloadTime: 0.0f, ammo: 999, infiniteAmmo: true, modulesAreTiers: true,
            chargeFps: 16, banFromBlessedRuns: true, isStarterGun: true, doesScreenShake: false, muzzleFrom: Items.Mailbox) // TODO: verify this doesn't break paradox
          .Attach<VacpackAmmoDisplay>()
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(clipSize: -1, shootStyle: ShootStyle.Charged, hideAmmo: true, chargeTime: float.MaxValue)); // absurdly high charge value so we never actually shoot

        ProjectileModule shootMod = new ProjectileModule().InitSpecialSingleProjectileModule<SlimeProjectile>(GunData.New(
          gun: gun, baseProjectile: Items._38Special.Projectile(), clipSize: -1, cooldown: 0.45f, shootStyle: ShootStyle.SemiAutomatic,
          damage: 3.0f, speed: 100f, range: 9999f, force: 12f,  hitSound: "generic_bullet_impact", angleVariance: 10.0f
          ));
        gun.Volley.projectiles.Add(shootMod);

        _VacpackVFX = VFX.Create("vacpack_wind_sprite_a", fps: 30, loopStart: 6, scale: 0.5f);
    }

    private class SlimeProjectile : WeirdProjectile
    {
        private static void BecomeSlime(Projectile proj)
        {
          int slimeType = (int)SlimyboiType.Pink; // fallback in case fired from an unknown source
          if (proj.PossibleSourceGun is Gun gun && gun.gameObject.GetComponent<Vacpack>() is Vacpack vp)
          {
            slimeType = vp._lastSlime;
            if (vp.slimeCounts[slimeType] > 0)
              --vp.slimeCounts[slimeType];
          }
          if (slimeType < 0 && slimeType >= Slimybois.NumSlimes)
          {
            Lazy.DebugWarn("TRIED TO FIRE A NONEXISTENT SLIME TYPE, THIS SHOULD NEVER HAPPEN");
            slimeType = (int)SlimyboiType.Pink;
          }
          SlimeData sd = Slimybois.SlimeData[slimeType];
          Vector2 ppos = proj.SafeCenter;
          AIActor newSlime = AIActor.Spawn(
            prefabActor     : sd.prefab,
            position        : ppos,
            source          : ppos.GetAbsoluteRoom(),
            awakenAnimType  : AIActor.AwakenAnimationType.Spawn,
            correctForWalls : true);
          newSlime.SpawnInInstantly(isReinforcement: true);
          newSlime.gameObject.AddComponent<KnockbackUnleasher>();
          Vector2 dir = proj.transform.right;
          newSlime.knockbackDoer.ApplySourcedKnockback(
            direction: proj.transform.right, time: 1.0f, source: newSlime.gameObject,
            force: proj.baseData.speed * UnityEngine.Random.Range(0.8f, 1.2f));
          newSlime.gameObject.GetComponent<SlimyboiController>().HandleFiredFromVacpack(dir);

          for (int i = 0; i < 10; ++i)
          {
            DebrisObject debris = UnityEngine.Object.Instantiate(sd.debris, ppos, Quaternion.identity).GetComponent<DebrisObject>();
            debris.GravityOverride = 30.0f;
            debris.Trigger((3f * dir + Lazy.RandomVector(2f * UnityEngine.Random.value)).ToVector3ZUp(4f), 0.25f);
            debris.sprite.MakeGlowyBetter(glowAmount: 10.0f, glowColor: new Color(1.0f, 0.75f, 0.9f), glowColorPower: 20.0f, sensitivity: 0.3f);
          }

          proj.gameObject.Play("slime_spawn_sound");
        }

        protected override void OnFiredByAnything()
        {
          BecomeSlime(this);
          DieInAir(suppressInAirEffects: true, allowActorSpawns: false, allowProjectileSpawns: false, killedEarly: false);
        }
    }

    private void Start()
    {
      if (slimeCounts == null || slimeCounts.Count == 0)
      {
        Lazy.DebugConsoleLog($" initializing slime counts");
        int cap = Slimybois.NumSlimes;
        slimeCounts = new(cap);
        for (int i = 0; i < cap; ++i)
          slimeCounts.Add(0);
        slimeCounts[(int)SlimyboiType.Pink] = 8;
      }
    }

    public override void OnTriedToInitiateAttack(PlayerController player)
    {
      base.OnTriedToInitiateAttack(player);
      if (this._hud && this._hud.Active)
      {
        DoSlimeSelection();
        DismissHUD();
        player.SuppressThisClick = true;
      }
      else if (this._curSlime >= 0 && this.slimeCounts[this._curSlime] == 0)
      {
        base.gameObject.Play("vacpack_empty_sound");
        player.SuppressThisClick = true;
      }
    }

    public override bool OnManualReloadAttempted(PlayerController player)
    {
      if (!player.AcceptingNonMotionInput || gun.IsFiring)
          return true;
      CreateHUDIfNecessary();
      if (this._hud.Active)
          return true;

      this._hudHoldTimer = _HUD_HOLD_TIME;
      return false;
    }

    public void DoSlimeSelection(int slimeIndex = -2)
    {
        if (slimeIndex == -2)
          this._lastSlime = slimeIndex = VacpackHUD.SlimeIndexForAngle(this.PlayerOwner.AimAngleFromCenterOfScreen());
        this._curSlime = slimeIndex;
        int newTier = slimeIndex >= 0 ? 1 : 0;
        if (newTier != this.gun.m_currentStrengthTier)
        {
          this.gun.CurrentStrengthTier = newTier;
          ClearCachedShootData(); // reset particle effects
        }
        base.gameObject.Play("vacpack_select_sound");
    }

    public override void Update()
    {
        base.Update();
        float dtime = BraveTime.DeltaTime;
        if (this.PlayerOwner is not PlayerController player || dtime == 0.0f)
        {
          this.gun.LoopSoundIf(false, "vacpack_fire_sound");
          return;
        }

        if (this._hudHoldTimer > 0.0f)
        {
          if (!player.m_activeActions.ReloadAction.IsPressed) // quick select
          {
            this._hudHoldTimer = 0.0f;
            DoSlimeSelection(this._curSlime == -1 ? this._lastSlime : -1);
          }
          else if ((this._hudHoldTimer -= dtime) <= 0.0f)
          {
            this._hudHoldTimer = 0.0f;
            this._hud.Engage();
            SetFocus(true);
          }
        }
        if (this._hud && this._hud.Active && !player.m_activeActions.ReloadAction.IsPressed)
        {
          DoSlimeSelection();
          DismissHUD();
        }

        bool isCharging = this.gun.IsCharging && this.gun.m_currentStrengthTier == 0;
        this.gun.LoopSoundIf(isCharging, "vacpack_fire_sound", loopPointMs: 3898, rewindAmountMs: 3898 - 1855);
        if (!isCharging)
        {
            foreach (SlimyboiController sloim in this._vacSlimes)
              if (sloim)
                sloim.HandleNoLongerVacuumedByVacpack();
            this._vacSlimes.Clear();
            return;
        }

        Vector2 gunpos = this.gun.barrelOffset.position;

        // Particle effect creation logic should not be tied to framerate
        bool isParticleFrame = UnityEngine.Random.value < 0.66f * (BraveTime.DeltaTime * C.FPS);
        if (isParticleFrame)
        {
            float angleFromGun = this.gun.CurrentAngle + UnityEngine.Random.Range(-_SPREAD, _SPREAD);
            //WARNING: verify this doesn't cause pooling issues
            GameObject o = SpawnManager.SpawnVFX(_VacpackVFX, (gunpos + angleFromGun.ToVector(_REACH)).ToVector3ZUp(), Lazy.RandomEulerZ());
            o.AddComponent<VacpackParticle>().Setup(this.gun, _REACH);
        }

        float towardsGunAngle = (180f + this.gun.CurrentAngle).Clamp180();
        foreach (AIActor enemy in Lazy.GetAllNearbyEnemies(gunpos, radius: _REACH, limitToCurrentRoom: false,
          includeInvulnerable: true, includeHarmless: true, ignoreWalls: false))
        {
          if (!enemy || enemy.gameObject.GetComponent<SlimyboiController>() is not SlimyboiController sloim)
            continue;
          if (player.IsInCombat && sloim.healthHaver.GetCurrentHealthPercentage() < 1.0f)
            continue; // no vacuuming injured slimes in combat

          Vector2 towardsGun = gunpos - enemy.CenterPosition;
          float sqrMagnitude = towardsGun.sqrMagnitude;
          if (!this._vacSlimes.Contains(sloim)) // we can skip checks for slimes that have been in range at least once
          {
            if (sqrMagnitude >= _SQR_REACH)
              continue; // don't absorb new slimes outside our reach
            if (towardsGunAngle.AbsAngleTo(towardsGun.ToAngle()) > _SPREAD)
              continue; // don't absorb new slimes outside our cone of influence
            sloim.HandleVacuumedByVacpack();
            this._vacSlimes.Add(sloim);
          }
          if (sqrMagnitude < _SQR_ABSORB_RANGE)
          {
            ProcessSlime(sloim);
            continue;
          }
          enemy.behaviorSpeculator.Stun(0.1f, false);
          float influence = Mathf.Max(0.1f, 1f - sqrMagnitude / _SQR_REACH);
          enemy.ApplyContinuousSourcedKnockback(base.gameObject, _KnockbackDict, (_SUCK_FORCE * influence) * towardsGun.normalized);
          if (isParticleFrame)
            CwaffVFX.SpawnBurst(
              prefab           : _VacpackVFX,
              numToSpawn       : 1,
              basePosition     : enemy.CenterPosition,
              positionVariance : 0.5f,
              baseVelocity     : null,
              minVelocity      : 2f,
              velocityVariance : 2f,
              velType          : CwaffVFX.Vel.Away,
              rotType          : CwaffVFX.Rot.Random,
              lifetime         : 0.5f,
              fadeOutTime      : 0.5f,
              startScale       : 1.0f,
              endScale         : 0.1f
              );
        }
    }

    public bool IsVacuumingSlime(SlimyboiController sloim) => this._vacSlimes.Contains(sloim);

    public void ProcessSlime(SlimyboiController sloim)
    {
        base.gameObject.PlayUnique("slime_vacuum_sound");
        ++this.slimeCounts[(int)sloim.slimeType];
        Vector2 pos = (this.PlayerOwner.CurrentGun is Gun gun) ? gun.barrelOffset.position : sloim.aiActor.CenterPosition;
        sloim.aiActor.EraseFromExistence(suppressDeathSounds: true);
        for (int i = 0; i < 10; ++i)
        {
          DebrisObject debris = UnityEngine.Object.Instantiate(
            SlimyboiType.Pink.Data().debris, pos, Quaternion.identity).GetComponent<DebrisObject>();
          debris.GravityOverride = 30.0f;
          debris.Trigger(Lazy.RandomVector(3f * UnityEngine.Random.value).ToVector3ZUp(4f), 0.25f);
          debris.sprite.MakeGlowyBetter(glowAmount: 10.0f, glowColor: new Color(1.0f, 0.75f, 0.9f), glowColorPower: 20.0f, sensitivity: 0.3f);
        }
    }

    private void OnReceivedDamage(PlayerController player)
    {
      DismissHUD();
    }

    private void DeregisterEvents(PlayerController player)
    {
      player.OnRoomClearEvent -= SlimyboiManager.OnCombatRoomClear;
      CustomActions.OnAnyPlayerCollectedAmmo -= SlimyboiManager.OnAmmoCollected;
      CustomActions.OnAnyPlayerCollectedBlank -= SlimyboiManager.OnBlankCollected;
      CustomActions.OnAnyHealthHaverDie -= SlimyboiManager.OnAnyHealthHaverDie;
      CustomActions.OnAnyPlayerCollectedHealth -= SlimyboiManager.OnAnyPlayerCollectedHealth;
      CustomActions.OnMinorBreakableShattered -= SlimyboiManager.OnMinorBreakableShattered;
      CwaffEvents.OnWillPickUpCurrency -= SlimyboiManager.OnWillPickUpCurrency;
      CwaffEvents.OnWillPickUpAnyPassive -= SlimyboiManager.OnWillPickUpAnyPassive;
    }

    private void RegisterEvents(PlayerController player)
    {
      DeregisterEvents(player);
      player.OnRoomClearEvent += SlimyboiManager.OnCombatRoomClear;
      CustomActions.OnAnyPlayerCollectedAmmo += SlimyboiManager.OnAmmoCollected;
      CustomActions.OnAnyPlayerCollectedBlank += SlimyboiManager.OnBlankCollected;
      CustomActions.OnAnyHealthHaverDie += SlimyboiManager.OnAnyHealthHaverDie;
      CustomActions.OnAnyPlayerCollectedHealth += SlimyboiManager.OnAnyPlayerCollectedHealth;
      CustomActions.OnMinorBreakableShattered += SlimyboiManager.OnMinorBreakableShattered;
      CwaffEvents.OnWillPickUpCurrency += SlimyboiManager.OnWillPickUpCurrency;
      CwaffEvents.OnWillPickUpAnyPassive += SlimyboiManager.OnWillPickUpAnyPassive;
    }

    public override void OnPlayerPickup(PlayerController player)
    {
      base.OnPlayerPickup(player);
      SlimyboiManager.EnsureInstance();
      RegisterEvents(player);
    }

    public override void OnSwitchedToThisGun()
    {
      base.OnSwitchedToThisGun();
      SlimyboiManager.EnsureInstance();
      CreateHUDIfNecessary();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        DismissHUD();
        base.OnSwitchedAwayFromThisGun();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        DeregisterEvents(player);
        base.OnDroppedByPlayer(player);
        DismissHUD();
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner is PlayerController player)
          DeregisterEvents(player);
        DismissHUD();
        base.OnDestroy();
    }

    private void CreateHUDIfNecessary()
    {
      if (this._hud)
        return;
      this._hud = base.gameObject.AddComponent<VacpackHUD>();
      this._hud.Setup();
    }

    private void DismissHUD()
    {
      if (!this._hud || !this._hud.Active)
        return;
      this._hud.Dismiss();
    }

    internal void SetFocus(bool focus)
    {
      BraveTime.SetTimeScaleMultiplier(focus ? 0.1f : 1.0f, base.gameObject); //TODO: use vanilla metalgear time slowdown factor
    }

    private class VacpackAmmoDisplay : CustomAmmoDisplay
    {
        private Gun _gun;
        private Vacpack _vac;
        private PlayerController _owner;

        private int _cachedIndex = -1;
        private int _cachedCount = -1;
        private string _cachedAmmoString = string.Empty;

        private void Start()
        {
            this._gun   = base.GetComponent<Gun>();
            this._vac   = this._gun.GetComponent<Vacpack>();
            this._owner = this._gun.CurrentOwner as PlayerController;
        }

        public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
        {
            if (!this._owner)
                return false;

            int index = this._vac._curSlime;
            if (index < 0)
                uic.GunAmmoCountLabel.Text = "Vac";
            else
            {
                int count = this._vac.slimeCounts[index];
                if (index != this._cachedIndex || count != this._cachedCount)
                {
                  this._cachedIndex = index;
                  this._cachedCount = count;
                  this._cachedAmmoString = $"[sprite \"slime_{Slimybois.SlimeData[index].slimeName}_ui\"]\nx{count}";
                }
                uic.GunAmmoCountLabel.Text = this._cachedAmmoString;
            }
            return true;
        }
    }
}

public class VacpackParticle : MonoBehaviour
{
    private const float _MAX_LIFE           = 1.0f;
    private const float _MIN_DIST_TO_VACUUM = 0.5f;
    private const float _MIN_VAC_DIST_SQR   = _MIN_DIST_TO_VACUUM * _MIN_DIST_TO_VACUUM;
    private const float _MAX_ALPHA          = 0.5f;
    private const float _DLT_ALPHA          = 0.01f;

    private Vacpack  _vac          = null;
    private Gun _gun               = null;
    private tk2dBaseSprite _sprite = null;
    private Vector2 _velocity      = Vector2.zero;
    private float _lifetime        = 0.0f;
    private float _startDistance   = 0.0f;
    private float _startScaleX     = 1.0f;
    private float _startScaleY     = 1.0f;
    private Vector2 _spriteCenter  = Vector2.zero;

    public void Setup(Gun gun, float startDistance = 0.0f)
    {
        this._gun           = gun;
        this._vac           = gun.gameObject.GetComponent<Vacpack>();
        this._startDistance = startDistance;
        this._sprite        = base.gameObject.GetComponent<tk2dSprite>();
        this._startScaleX   = 1.0f;
        this._startScaleY   = 1.0f;
        this._spriteCenter  = this._sprite.WorldCenter;
    }

    // Using LateUpdate() here so alpha is updated correctly
    private void LateUpdate()
    {
        if (BraveTime.DeltaTime == 0.0f)
            return; // nothing to do if time isn't passing

        // handle particle fading logic exclusive to the vacuum particles
        this._lifetime += BraveTime.DeltaTime;
        if (!this._gun || this._lifetime > _MAX_LIFE)
        {
            UnityEngine.GameObject.Destroy(base.gameObject);
            return;
        }
        this._sprite.renderer.SetAlpha(_MAX_ALPHA * (1f - this._lifetime / _MAX_LIFE));

        Vector2 towardsVacuum = (this._gun.barrelOffset.position.XY() - this._sprite.WorldCenter);
        if (towardsVacuum.sqrMagnitude < _MIN_VAC_DIST_SQR)
        {
            UnityEngine.GameObject.Destroy(base.gameObject);
            return;
        }

        // Shrink on our way to the vacuum
        float scale = towardsVacuum.magnitude / this._startDistance;
        this._sprite.scale = new Vector3(this._startScaleX * scale, this._startScaleY * scale, 1f);
        this._velocity = this._sprite.WorldCenter.LerpDirectAndNaturalVelocity(
            target          : this._gun.barrelOffset.position,
            naturalVelocity : this._velocity,
            accel           : VacuumCleaner._ACCEL_SEC * BraveTime.DeltaTime,
            lerpFactor      : 1f);
        this._spriteCenter += (this._velocity * C.FPS * BraveTime.DeltaTime);
        this._sprite.PlaceAtRotatedPositionByAnchor(this._spriteCenter, Anchor.MiddleCenter);
    }
}

public class VacpackHUD : MonoBehaviour
{
  internal const string _NAME_LETTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

  private static float _WedgeArc = 360f / Slimybois.NumSlimes;
  private static float _SHWOOP_TIME = 0.3f;
  private static float _BASE_GEOM_ALPHA = 0.3f;

  private static readonly Color _GeomColor1 = new Color(0.25f, 0.25f, 0.25f);
  private static readonly Color _GeomColor2 = new Color(0.35f, 0.35f, 0.35f);

  private bool _setup                 = false;   // whether we're set up
  private float _shwoop               = 0.0f;    // whether we're shwooped open
  private bool _active                = false;   // whether we're active
  private Vacpack _gun              = null;      // gun we're attached to
  private List<Geometry> _geometry    = new();   // all shapes rendered by the HUD
  private Geometry       _selector    = new();   // extra selector rendered by the HUD
  private List<dfLabel> _labels       = new();   // all letter labels rendered by the HUD
  private dfLabel       _nameLabel    = null;    // extra name line for currently selected slime rendered by the HUD
  private dfLabel       _countLabel   = null;    // extra count line for currently selected slime rendered by the HUD

  private CameraController _camera;
  private Vector2 _worldBottomLeft;
  private Vector2 _worldTopRight;
  // private Vector2 _basePos;

  public bool Active => this._active;

  public void Setup()
  {
    this._gun = this.gameObject.GetComponent<Vacpack>();

    this._selector = Geometry.Create(Geometry.Shape.RING).Place(color: Color.red.WithAlpha(_BASE_GEOM_ALPHA)).UseGUILayer();
    for (int i = 0; i < Slimybois.NumSlimes; ++i)
    {
      this._geometry.Add(Geometry.Create(Geometry.Shape.RING).Place(color: ((i % 2 == 0) ? _GeomColor1 : _GeomColor2).WithAlpha(_BASE_GEOM_ALPHA)).UseGUILayer());
      this._labels.Add(EasyLabel.Create(unicode: false, outline: true, align: TextAlignment.Center));
      this._labels[i].Text = $"[sprite \"slime_{Slimybois.SlimeData[i].slimeName}_ui\"]";
      this._labels[i].Pivot = dfPivotPoint.MiddleCenter;
    }
    this._nameLabel = EasyLabel.Create(unicode: false, outline: true, align: TextAlignment.Center);
    this._countLabel = EasyLabel.Create(unicode: false, outline: true, align: TextAlignment.Center);

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
    if (base.gameObject.RequestCameraControl())
      GameManager.Instance.MainCameraController.OverridePosition = this._gun.PlayerOwner.CenterPosition;
    base.gameObject.Play("vacpack_menu_sound");
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
    if (this._nameLabel)
    {
      this._nameLabel.Opacity = 0.0f;
      this._nameLabel.IsVisible = false;
    }
    if (this._countLabel)
    {
      this._countLabel.Opacity = 0.0f;
      this._countLabel.IsVisible = false;
    }

    if (deactivate)
    {
      this._active = false;
      base.gameObject.RelinquishCameraControl();
      if (this._gun)
        this._gun.SetFocus(false);
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
      base.gameObject.RequestCameraControl();
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

  internal static int SlimeIndexForAngle(float angle)
    => Mathf.FloorToInt((angle.Clamp360() + 0.5f * _WedgeArc) / _WedgeArc) % Slimybois.NumSlimes;

  private void PlaceHUDElements()
  {
    PlayerController player = this._gun.PlayerOwner;
    if (!player || player.CurrentGun != this._gun.gun)
    {
      Dismiss();
      return;
    }

    float gunAngle = player.AimAngleFromCenterOfScreen().Clamp360();
    int curSegment = SlimeIndexForAngle(gunAngle);

    this._shwoop = Mathf.Clamp01(this._shwoop + Time.unscaledDeltaTime / _SHWOOP_TIME);
    float ease = Ease.OutQuad(this._shwoop);

    this._worldBottomLeft = this._camera.MinVisiblePoint;
    this._worldTopRight   = this._camera.MaxVisiblePoint;
    Vector2 screenCenter = 0.5f * (this._worldBottomLeft + this._worldTopRight);
    float screenHeight = this._camera.MaxVisiblePoint.y - this._camera.MinVisiblePoint.y;
    float shwoopHeight = ease * screenHeight;
    float geomAlpha = _BASE_GEOM_ALPHA * ease;
    float innerRadius = 0.3f * screenHeight;
    float outerRadius = innerRadius + 0.1f * shwoopHeight;
    float labelRadius = 0.5f * (innerRadius + outerRadius);

    this._selector.Disable();
    for (int i = 0; i < Slimybois.NumSlimes; ++i)
    {
      bool sel = (i == curSegment);
      bool goodLetter = DeathNoteNameHandler.IsGoodLetter(_NAME_LETTERS[i]);
      this._geometry[i].Place(pos: screenCenter, angle: _WedgeArc * i, arc: _WedgeArc,
        radiusInner: innerRadius, radius: outerRadius * (sel ? 1.125f : 1.0f),
        color: (sel ? Color.white : (i % 2 == 0) ? _GeomColor1 : _GeomColor2).WithAlpha(geomAlpha));
      Vector2 labelPos = screenCenter + (_WedgeArc * i).ToVector(labelRadius);
      if (sel)
      {
        this._nameLabel.Text = Slimybois.SlimeData[i].fullName;
        this._nameLabel.Opacity = ease;
        this._nameLabel.Place(pos: screenCenter);

        this._countLabel.Text = this._gun.slimeCounts[i].ToString();
        this._countLabel.Opacity = ease;
        this._countLabel.Place(pos: screenCenter + new Vector2(0.0f, -this._countLabel.Height / 32f));
      }
      this._labels[i].Color = ((this._gun.slimeCounts[i] == 0) ? Color.black : Color.white);
      this._labels[i].OutlineColor = (sel ? Color.white : Color.black).WithAlpha(Mathf.Clamp01(2f * ease - 1f));
      this._labels[i].Opacity = ease;
      this._labels[i].Place(labelPos);
    }
  }
}
