namespace CwaffingTheGungy;

public class Retina : CwaffGun
{
    public static string ItemName         = "Retina";
    public static string ShortDescription = "Breach of Covenant";
    public static string LongDescription  = "Shoots lethal piercing directed energy blasts. Press fire once to scope in, and again to shoot. Scope out by shooting, dodging, reloading, or taking damage.";
    public static string Lore             = "An engineering marvel developed by three tech-savvy track athletes and their coach under contract by the Department of Defense. Featuring a state-of-the-art digital scope, mach 4 directed energy projectiles, and enough power to wipe out a Gun Nut, this four-runner-designed weapon is perfect for fighting from an extremely comfortable distance.";

    private RetinaHUD _hud = null;

    public static void Init()
    {
        Lazy.SetupGun<Retina>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.S, gunClass: GunClass.RIFLE, reloadTime: 2.50f, ammo: 50, idleFps: 10, shootFps: 24, reloadFps: 30,
            smoothReload: 0.1f, reloadAudio: "retina_reload_sound", fireAudio: "retina_fire_sound")
          .InitProjectile(GunData.New(sprite: "retina_projectile", clipSize: 4, cooldown: 1.25f, angleVariance: 1.0f, shootStyle: ShootStyle.SemiAutomatic, customClip: true,
            damage: 300.0f, speed: 900.0f, force: 10.0f, range: 1000.0f, pierceBreakables: true, hitSound: "retina_impact_sound", bossDamageMult: 0.6f))
          .AttachTrail("retina_beam", fps: 60, timeTillAnimStart: 0.00f,
            destroyOnEmpty: true, dispersalPrefab: Lazy.DispersalParticles(ExtendedColours.vibrantOrange))
          .Attach<PierceProjModifier>(pierce => {
            pierce.penetration          = 999;
            pierce.penetratesBreakables = true; })
          .Attach<RetinaProjectile>()
          .Assign(out Projectile proj);

        VFXPool impactPool = VFX.CreatePool("retina_impact_vfx", fps: 30, loops: false, emissivePower: 2f,
          emissiveColorPower: 10.0f, emissiveColour: new Color(1.0f, 0.85f, 0.5f), emissiveSensitivity: 0.6f);
        impactPool.effects[0].effects[0].effect.AddComponent<RetinaLightburstDoer>();
        proj.SetAllImpactVFX(impactPool);
    }

    private void RegisterEvents(PlayerController player)
    {
        if (!player)
          return;
        player.OnReceivedDamage -= this.OnReceivedDamage;
        player.OnReceivedDamage += this.OnReceivedDamage;
    }

    private void OnReceivedDamage(PlayerController player)
    {
      DismissHUD();
    }

    private void DeregisterEvents(PlayerController player)
    {
        if (!player)
          return;
        player.OnReceivedDamage -= this.OnReceivedDamage;
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        CreateHUDIfNecessary();
        RegisterEvents(this.PlayerOwner);
    }

    public override void OnTriedToInitiateAttack(PlayerController player)
    {
        base.OnTriedToInitiateAttack(player);
        if (player.IsDodgeRolling || player.CurrentInputState != PlayerInputState.AllInput)
            return; // inactive, do normal firing stuff
        if (this.gun.IsReloading || this.gun.ClipShotsRemaining == 0 || this.gun.CurrentAmmo == 0)
            return; // inactive, do normal firing stuff
        CreateHUDIfNecessary();
        if (!this._hud || this._hud.Active)
          return; // no HUD or HUD already active

        player.SuppressThisClick = true;
        if (!this.gun.m_moduleData[this.gun.DefaultModule].onCooldown) // don't toggle HUD while the weapon is on cooldown
          this._hud.Toggle();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        DismissHUD();
        DeregisterEvents(player);
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        DismissHUD();
        DeregisterEvents(this.PlayerOwner);
        base.OnDestroy();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        DismissHUD();
        DeregisterEvents(this.PlayerOwner);
        base.OnSwitchedAwayFromThisGun();
    }

    public override bool OnManualReloadAttempted(PlayerController player)
    {
        if (!this._hud || !this._hud.Active)
            return true;
        this._hud.Dismiss();
        return false;
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        if (this._hud)
          this._hud.Dismiss();
    }

    private void CreateHUDIfNecessary()
    {
      if (this._hud)
        return;
      this._hud = base.gameObject.AddComponent<RetinaHUD>();
      this._hud.Setup();
    }

    private void DismissHUD()
    {
      if (this._hud)
        this._hud.Dismiss(deactivate: true);
    }
}

public class RetinaHUD : MonoBehaviour
{
  private const float _SHWOOP_TIME = 0.25f;
  private const float _FADE_TIME   = 0.125f;
  private const float _ENGAGE_TIME = _SHWOOP_TIME + _FADE_TIME;
  private const int MAX_TARGETS = 4;
  private const float _FIRST_TEXT_LINE       = 0.05f;
  private const float _TARGET_SPRITE_MAIN_X  = 0.175f;
  private const float _TARGET_SPRITE_EXTRA_X = 0.3f;
  private const float _TARGET_SPRITE_MIN_Y   = 0.01f;
  private const float _TARGET_SPACING        = 0.13f;
  private const float _INFO_LABEL_X          = 0.075f;
  private const float _HEALTH_W              = 0.075f;
  private const float _HEALTH_LEFT           = _INFO_LABEL_X - 0.5f * _HEALTH_W;
  private const float _PANEL_SIZE_X          = 0.35f;
  private const float _PANEL_SIZE_Y          = 0.4f;
  private const float MIN_TIMESCALE = 0.25f;

  private static readonly Color _HeaderColor         = Color.Lerp(Color.green, Color.black, 0.60f);
  private static readonly Color _LabelColor          = Color.Lerp(Color.green, Color.black, 0.35f);
  private static readonly string[] _CollateralLabels = ["Low", "Medium", "High", "Extreme"];

  private bool _setup              = false;   // whether we're set up
  private bool _active             = false;   // whether we're active
  private Retina _gun              = null;    // gun we're attached to
  private float _shwoop             = 0.0f;    // percent of HUD that's visible
  private List<Geometry> _geometry = new();   // all shapes rendered by the HUD
  private List<dfLabel> _labels    = new();   // all labels rendered by the HUD

  private CameraController _camera;
  private Vector2 _worldBottomLeft;
  private Vector2 _worldTopRight;
  private Vector2 _basePos;

  private Geometry _base;                     // backdrop of the HUD
  private Geometry _targetInfoRectangle;      // rectangular area where target information is drawn
  private Geometry _healthbarBack;            // backdrop for health bar
  private Geometry _healthbarHurt;            // foreground for health bar, projected health after shooting
  private Geometry _healthbarFore;            // foreground for health bar, current health

  private dfLabel _nameHeader;
  private dfLabel _nameLabel;
  private dfLabel _collateralHeader;
  private dfLabel _collateralLabel;
  private dfLabel _rangeHeader;
  private dfLabel _rangeLabel;
  private dfLabel _vulnHeader;
  private dfLabel _vulnLabel;

  private tk2dSprite _targetSprite;           // copy of the sprite for the current target
  private List<tk2dSprite> _extraTargetSprites; // copy of the sprite for the current extra targets

  public bool Active => this._active;

  public void Setup()
  {
    this._gun = this.gameObject.GetComponent<Retina>();

    this._base                  = Geom(Geometry.Shape.RECTANGLE).Place(color: ExtendedColours.vibrantOrange.WithAlpha(0.1f));
    this._targetInfoRectangle   = Geom(Geometry.Shape.RECTANGLE).Place(color: ExtendedColours.vibrantOrange.WithAlpha(0.2f));
    this._healthbarBack         = Geom(Geometry.Shape.RECTANGLE).Place(color: Color.black);
    this._healthbarHurt         = Geom(Geometry.Shape.RECTANGLE).Place(color: Color.green.WithAlpha(0.85f));
    this._healthbarFore         = Geom(Geometry.Shape.RECTANGLE).Place(color: Color.red.WithAlpha(0.85f));

    this._nameHeader            = Lab(color: Color.cyan);
    this._nameLabel             = Lab(color: Color.cyan);
    this._collateralHeader      = Lab(color: Color.cyan);
    this._collateralLabel       = Lab(color: Color.cyan);
    this._rangeHeader           = Lab(color: Color.cyan);
    this._rangeLabel            = Lab(color: Color.cyan);
    this._vulnHeader            = Lab(color: Color.cyan);
    this._vulnLabel             = Lab(color: Color.cyan);

    this._nameLabel.AutoHeight = true;
    this._nameLabel.WordWrap = true;
    this._nameLabel.VerticalAlignment = dfVerticalAlignment.Top;
    this._nameLabel.Pivot = dfPivotPoint.TopCenter;
    this._nameLabel.Size = new Vector2(160f, 48f); // split long names onto multiple lines

    this._extraTargetSprites = new();
    for (int i = 0; i < MAX_TARGETS; ++i)
    {
      tk2dSprite extraTarget = CreateNewTargetSprite(i);
      this._extraTargetSprites.Add(extraTarget);
    }
    this._targetSprite = this._extraTargetSprites[0];

    Dismiss(force: true);
    this._setup = true;

    this._camera = GameManager.Instance.MainCameraController;
    if (this._camera)
    {
      this._camera.OnFinishedFrame -= this.OnFinishedFrame;
      this._camera.OnFinishedFrame += this.OnFinishedFrame; // synchronize HUD elements with camera for pseudo-overlay effect
    }
  }

  private static tk2dSprite CreateNewTargetSprite(int i)
  {
      tk2dSprite extraTarget = new GameObject($"retina target preview {(i + 1)}").AddComponent<tk2dSprite>();
      extraTarget.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Unoccluded"));
      extraTarget.renderer.enabled = false;
      return extraTarget;
  }

  private Geometry Geom(Geometry.Shape shape)
  {
      Geometry g = Geometry.Create(shape).UseGUILayer();
      this._geometry.Add(g);
      return g;
  }

  private dfLabel Lab(Color? color = null, TextAlignment align = TextAlignment.Center)
  {
    dfLabel label = EasyLabel.Create(unicode: false, outline: false, align: align);
    label.Color = color ?? Color.white;
    this._labels.Add(label);
    return label;
  }

  private void Update()
  {
    if (!this._setup)
      return;
    if (!this._camera || !this._gun || this._gun.gun is not Gun gun || this._gun.PlayerOwner is not PlayerController player)
    {
      Dismiss(deactivate: true);
      UnityEngine.Object.Destroy(this);
    }
    else if (GameManager.Instance.IsPaused)
      Dismiss(deactivate: false);
    else if (player.IsDodgeRolling || gun.IsReloading || player.CurrentInputState != PlayerInputState.AllInput)
      Dismiss(deactivate: true);
  }

  // TODO: actually check if UI size has changed
  private void UpdateLabelsForUISize()
  {
    // foreach (dfLabel label in this._labels)
    // {
    //   if (label == null)
    //     continue;
    //   label.TextScale = label.TextScale; // TODO: do something useful here
    // }
  }

  // private void LateUpdate()
  private void OnFinishedFrame()
  {
    if (!this._setup || !this._active || !this._camera || GameManager.Instance.IsPaused)
      return;

    Engage();
    UpdateLabelsForUISize();
    if (this._active)
      PlaceHUDElements();
  }

  private void Place(Geometry g, Vector2 topLeft, Vector2 bottomRight, Color? newColor = null)
  {
    Vector2 pos = new Vector2(
      Mathf.Lerp(this._worldBottomLeft.x, this._worldTopRight.x, this._basePos.x + topLeft.x),
      Mathf.Lerp(this._worldBottomLeft.y, this._worldTopRight.y, this._basePos.y + topLeft.y));
    Vector2 pos2 = new Vector2(
      Mathf.Lerp(this._worldBottomLeft.x, this._worldTopRight.x, this._basePos.x + bottomRight.x),
      Mathf.Lerp(this._worldBottomLeft.y, this._worldTopRight.y, this._basePos.y + bottomRight.y));
    g.Place(pos: pos, pos2: pos2, color: newColor);
  }

  private void Place(dfLabel d, string text, Vector2 screenPos, Color? newColor = null)
  {
    Vector2 pos = new Vector2(
      Mathf.Lerp(this._worldBottomLeft.x, this._worldTopRight.x, this._basePos.x + screenPos.x),
      Mathf.Lerp(this._worldBottomLeft.y, this._worldTopRight.y, this._basePos.y + screenPos.y));
    if (newColor is Color c)
      d.Color   = c.WithAlpha(1f);
    d.Opacity = Mathf.Clamp01((this._shwoop - _SHWOOP_TIME) / _FADE_TIME);
    d.Text = text;
    d.Place(pos);
  }

  private void Place(tk2dSprite sprite, Vector2 screenPos, float scale = 1.0f, bool outline = false, Anchor anchor = Anchor.LowerCenter, Texture2D palette = null)
  {
    Vector2 pos = new Vector2(
      Mathf.Lerp(this._worldBottomLeft.x, this._worldTopRight.x, this._basePos.x + screenPos.x),
      Mathf.Lerp(this._worldBottomLeft.y, this._worldTopRight.y, this._basePos.y + screenPos.y));

    Material mat = sprite.renderer.material;
    mat.shader = ShaderCache.Acquire((palette == null) ? "Brave/PlayerShader" : "Brave/LitCutoutUber");
    mat.SetFloat("_UsePalette", (palette == null) ? 0f : 1f);
    mat.SetFloat("_EmissivePower", 10f);
    mat.SetTexture("_PaletteTex", palette);

    sprite.scale = new Vector3(scale, scale, 1f);
    sprite.PlaceAtScaledPositionByAnchor(pos, anchor);
    sprite.UpdateZDepth();
    if (outline)
    {
      if (!SpriteOutlineManager.HasOutline(sprite))
        SpriteOutlineManager.AddOutlineToSprite(sprite, Color.white, 0.1f);
      foreach (tk2dSprite outlineSprite in SpriteOutlineManager.GetOutlineSprites(sprite))
        outlineSprite.UpdateZDepth();
    }
  }

  private static Dictionary<string, float> _EnemyScales = new();
  private static float GetScaleForEnemy(AIActor enemy)
  {
    string enemyGuid = enemy.EnemyGuid;
    if (_EnemyScales.TryGetValue(enemyGuid, out float scale))
      return scale;

    scale = 1f;
    if (enemy.sprite.collection.spriteDefinitions != null)
    {
      Lazy.GetCollectionAndIdForBestIdleAnimation(enemy, out tk2dSpriteCollectionData collection, out int spriteId);
      tk2dSpriteDefinition idleDef = collection.spriteDefinitions[spriteId];
      Vector3 extents = idleDef.boundsDataExtents;
      float maxSpriteSide = C.PIXELS_PER_TILE * Mathf.Max(extents.x, extents.y);
      scale = (maxSpriteSide < 40f) ? 2f : (maxSpriteSide < 80f) ? 1f : 0.5f;
    }
    if (enemy.healthHaver is HealthHaver hh && (hh.IsBoss || hh.IsSubboss))
      scale = Mathf.Min(scale, 1f);
    return _EnemyScales[enemyGuid] = scale;
  }

  private static float GetNextLine(ref float y, float skip = 1.0f)
  {
    const float lineSpacing = -0.03f;
    float oldY = y;
    y += (lineSpacing * skip);
    return oldY;
  }

  private string Status(AIActor target, out float projectedDamage)
  {
    projectedDamage = 0f;
    if (!target)
      return string.Empty;
    if (target.healthHaver is not HealthHaver hh)
      return "Immortal";
    if (hh.IsDead)
      return "Deceased";
    if (target.IsGone || !hh.IsVulnerable || hh.PreventAllDamage || hh.OnlyAllowSpecialBossDamage)
      return "Invulnerable";
    if (hh.healthIsNumberOfHits)
    {
      if (hh.GetCurrentHealth() <= 1f)
        return "Faltering";
      return "Steeled";
    }

    Projectile projectedProjectile = this._gun.gun.DefaultModule.projectiles[0].projectile;
    projectedDamage = projectedProjectile.baseData.damage;
    if (this._gun.PlayerOwner is PlayerController owner)
    {
      projectedDamage *= this._gun.PlayerOwner.DamageMult();
      if (hh.IsBoss)
        projectedDamage *= this._gun.PlayerOwner.BossDamageMult();
    }
    projectedDamage *= hh.AllDamageMultiplier;
    if (hh.IsBoss)
      projectedDamage *= projectedProjectile.BossDamageMultiplier;
    bool capped = false;
    if (projectedDamage <= 999f && !projectedProjectile.ignoreDamageCaps)
    {
      float uncappedDamage = projectedDamage;
      if (hh.m_damageCap > 0f)
        projectedDamage = Mathf.Min(hh.m_damageCap, projectedDamage);
      if (hh.m_bossDpsCap > 0f)
        projectedDamage = Mathf.Min(projectedDamage, hh.m_bossDpsCap * 3f - hh.m_recentBossDps);
      capped = projectedDamage < uncappedDamage;
    }
    if (projectedDamage >= hh.GetCurrentHealth())
      return "Threatened";
    if (capped)
      return "Resistant";
    return "Vulnerable";
  }

  private void PlaceHUDElements()
  {
    PlayerController player = this._gun.PlayerOwner;
    if (!player || player.CurrentGun != this._gun.gun)
    {
      Dismiss();
      return;
    }

    // recompute camera and HUD pos
    float dtime      = Time.deltaTime;
    this._shwoop     = this._shwoop + dtime;
    float ease       = Ease.OutQuad(Mathf.Clamp01(this._shwoop / _SHWOOP_TIME));
    bool panelOnLeft = Mathf.Abs(player.m_currentGunAngle.Clamp180()) < 90f;
    float panelX     = panelOnLeft ? (_PANEL_SIZE_X * (ease - 1.0f)) : (1.0f - ease * _PANEL_SIZE_X);
    this._basePos    = new Vector2(panelX, 0.5f - (0.5f * _PANEL_SIZE_Y));
    this._worldBottomLeft = this._camera.MinVisiblePoint;
    this._worldTopRight   = this._camera.MaxVisiblePoint;
    float linePos         = _PANEL_SIZE_Y - _FIRST_TEXT_LINE;
    float infoPos         = panelOnLeft ? _INFO_LABEL_X : (_PANEL_SIZE_X - _INFO_LABEL_X);
    float healthPos       = panelOnLeft ? _HEALTH_LEFT : (_PANEL_SIZE_X - _HEALTH_LEFT);
    float healthW         = panelOnLeft ? _HEALTH_W : -_HEALTH_W;
    bool fullyShwooped = this._shwoop > _SHWOOP_TIME;

    // set time scale
    BraveTime.SetTimeScaleMultiplier(Mathf.Lerp(1.0f, MIN_TIMESCALE, ease), base.gameObject);

    // determine target and compute some HUD parameters
    ReadOnlyCollection<AIActor> targetedEnemies = this._gun.gun.AllEnemiesInLineOfSight(accountForWalls: true, sort: true);
    float now         = Time.realtimeSinceStartup;
    Vector2 bpos      = this._gun.gun.barrelOffset.position.XY();
    float gunAngle    = this._gun.gun.CurrentAngle;
    AIActor target    = (targetedEnemies.Count > 0) ? targetedEnemies[0] : null;
    string targetName = target ? target.AmmonomiconName() : string.Empty;
    Vector2 delta     = target ? target.CenterPosition - bpos : Vector2.right;
    int dAngle        = Mathf.RoundToInt(delta.ToAngle().AbsAngleTo(gunAngle));
    string range      = target ? $"{(Mathf.RoundToInt(10f * delta.magnitude) / 10.0f):0.0}m" : string.Empty;
    // string angle      = target ? $"{dAngle}deg" : "------";

    float healthAlpha = 0.85f * Mathf.Clamp01((this._shwoop - _SHWOOP_TIME) / _FADE_TIME);
    Color healthColor = Color.Lerp(Color.green, Color.red, Mathf.Abs(Mathf.Sin(9f * now))).WithAlpha(healthAlpha);
    string status     = Status(target, out float projectedDamage);
    string collateral = target ? _CollateralLabels[Mathf.Clamp(targetedEnemies.Count - 1, 0, _CollateralLabels.Length - 1)] : string.Empty;

    // place individual elements in screen space
    // Place(this._base, Vector2.zero, Vector2.one);
    Place(this._targetInfoRectangle, Vector2.zero, new Vector2(_PANEL_SIZE_X, _PANEL_SIZE_Y), newColor: ExtendedColours.vibrantOrange.WithAlpha(0.2f * ease));
    Place(this._nameHeader,        "Species",     new Vector2(infoPos, GetNextLine(ref linePos, 0.0f)),  newColor: _HeaderColor);
    Place(this._nameLabel,         targetName,    new Vector2(infoPos, GetNextLine(ref linePos, 3.25f)), newColor: _LabelColor);
    Place(this._rangeHeader,       "Range",       new Vector2(infoPos, GetNextLine(ref linePos, 1.0f)),  newColor: _HeaderColor);
    Place(this._rangeLabel,        range,         new Vector2(infoPos, GetNextLine(ref linePos, 1.25f)), newColor: _LabelColor);
    Place(this._collateralHeader,  "Collateral",  new Vector2(infoPos, GetNextLine(ref linePos, 1.0f)),  newColor: _HeaderColor);
    Place(this._collateralLabel,   collateral,    new Vector2(infoPos, GetNextLine(ref linePos, 1.25f)), newColor: _LabelColor);
    Place(this._vulnHeader,        "Status",      new Vector2(infoPos, GetNextLine(ref linePos, 1.0f)),  newColor: _HeaderColor);
    Place(this._vulnLabel,         status,        new Vector2(infoPos, GetNextLine(ref linePos, 1.0f)),  newColor: _LabelColor);
    float healthTopY = GetNextLine(ref linePos, 1.0f);
    float healthBotY = GetNextLine(ref linePos, 1.0f);
    if (target && target.healthHaver is HealthHaver hh)
    {
      float hpPre  = hh.GetCurrentHealthPercentage();
      float hpPost = Mathf.Max(hh.GetCurrentHealth() - projectedDamage, 0f) / hh.GetMaxHealth();
      Place(this._healthbarHurt, new Vector2(healthPos, healthBotY), new Vector2(healthPos + hpPost * healthW, healthTopY), newColor: Color.green.WithAlpha(healthAlpha));
      Place(this._healthbarFore, new Vector2(healthPos + hpPost * healthW, healthBotY), new Vector2(healthPos + hpPre * healthW, healthTopY), newColor: healthColor);
      Place(this._healthbarBack, new Vector2(healthPos + hpPre * healthW, healthBotY), new Vector2(healthPos + healthW, healthTopY), newColor: Color.black.WithAlpha(healthAlpha));
    }
    else
    {
      this._healthbarBack.Disable();
      this._healthbarHurt.Disable();
      this._healthbarFore.Disable();
    }

    // handle additional target renders
    for (int ti = 0; ti < MAX_TARGETS; ++ti)
    {
      AIActor extraTarget = (targetedEnemies.Count > ti) ? targetedEnemies[ti] : null;
      if (fullyShwooped && extraTarget && extraTarget.sprite is tk2dSprite enemySprite)
      {
        this._extraTargetSprites[ti].renderer.enabled = true;
        this._extraTargetSprites[ti].SetSprite(enemySprite.collection, enemySprite.spriteId);
        if (ti == 0)
        {
          float spriteX = panelOnLeft ? _TARGET_SPRITE_MAIN_X : (_PANEL_SIZE_X - _TARGET_SPRITE_MAIN_X);
          Anchor anchor = panelOnLeft ? Anchor.LowerLeft : Anchor.LowerRight;
          Place(this._extraTargetSprites[ti], new Vector2(spriteX, _TARGET_SPRITE_MIN_Y), scale: GetScaleForEnemy(extraTarget), outline: true, palette: extraTarget.optionalPalette);
        }
        else
        {
          float spriteX = panelOnLeft ? _TARGET_SPRITE_EXTRA_X : (_PANEL_SIZE_X - _TARGET_SPRITE_EXTRA_X);
          float y = _TARGET_SPRITE_MIN_Y + _TARGET_SPACING * (ti - 1);
          Place(this._extraTargetSprites[ti], new Vector2(spriteX, y), scale: 0.5f * GetScaleForEnemy(extraTarget), outline: true, anchor: Anchor.LowerCenter, palette: extraTarget.optionalPalette);
        }
      }
      else
      {
        if (SpriteOutlineManager.HasOutline(this._extraTargetSprites[ti]))
          SpriteOutlineManager.RemoveOutlineFromSprite(this._extraTargetSprites[ti]);
        this._extraTargetSprites[ti].renderer.enabled = false;
      }
    }
  }

  private void OnDestroy()
  {
    if (GameManager.HasInstance && GameManager.Instance.MainCameraController)
      GameManager.Instance.MainCameraController.OnFinishedFrame -= this.OnFinishedFrame;

    if (!this._setup)
      return;

    if (this._geometry != null)
      for (int i = this._geometry.Count - 1; i >= 0; --i)
        if (this._geometry[i])
          UnityEngine.Object.Destroy(this._geometry[i].gameObject);

    if (this._labels != null)
      for (int i = this._labels.Count - 1; i >= 0; --i)
        if (this._labels[i])
          UnityEngine.Object.Destroy(this._labels[i].gameObject);

    if (this._extraTargetSprites != null)
      for (int i = 0; i < this._extraTargetSprites.Count; ++i)
        if (this._extraTargetSprites[i])
          UnityEngine.Object.Destroy(this._extraTargetSprites[i].gameObject);
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

    this._shwoop = 0.0f;
    this._active = true;
    base.gameObject.Play("retina_hud_engage_sound");
  }

  public void Dismiss(bool force = false, bool deactivate = true)
  {
    if (!this._active && !force)
      return;

    foreach (Geometry g in this._geometry)
      if (g)
          g.Disable();

    foreach (dfLabel label in this._labels)
    {
      if (label == null)
        continue;
      label.Opacity = 0.0f;
      label.IsVisible = false;
    }

    foreach (tk2dSprite sprite in this._extraTargetSprites)
      if (sprite)
      {
        if (SpriteOutlineManager.HasOutline(sprite))
          SpriteOutlineManager.RemoveOutlineFromSprite(sprite);
        sprite.renderer.enabled = false;
      }

    if (deactivate)
    {
      // base.gameObject.Play("retina_hud_dismiss_sound");
      this._active = false;
      BraveTime.ClearMultiplier(base.gameObject);
    }
  }
}

public class RetinaLightburstDoer : MonoBehaviour
{
  private void OnSpawned()
  {
    EasyLight.Create(pos: base.transform.position, color: ExtendedColours.vibrantOrange, radius: 4f, grownIn: true, brightness: 10.0f, fadeInTime: 0.2f, fadeOutTime: 0.2f, maxLifeTime: 0.5f);
  }
}

public class RetinaProjectile : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private bool _killedEnemy;
    private int _killTracker = 0;

    private void Start()
    {
      this._projectile = base.GetComponent<Projectile>();
      this._projectile.OnWillKillEnemy += this.OnWillKillEnemy;
      this._projectile.specRigidbody.OnRigidbodyCollision += this.OnRigidbodyCollision;
      this._owner = this._projectile.Owner as PlayerController;
      if (base.GetComponentInChildren<CwaffTrailController>() is CwaffTrailController tc)
        tc.gameObject.GetComponent<tk2dBaseSprite>().SetGlowiness(100f);
    }

    private void OnWillKillEnemy(Projectile projectile, SpeculativeRigidbody enemy)
    {
      if (enemy.healthHaver is not HealthHaver hh || hh.IsBoss || hh.IsSubboss)
        return;
      if (enemy.aiActor is not AIActor actor || actor.sprite is not tk2dBaseSprite sprite)
        return;
      sprite.DuplicateInWorldAsMesh(optionalPalette: actor.optionalPalette)
        .Dissipate(time: 1.5f, amplitudeStart: 0.0625f, amplitudeEnd: 4f, emissionEnd: 50f,
          easeEmit: RetinaEmit, easeFade: RetinaFade, easeAmp: RetinaAmp, sound: "retina_impact_burst_sound", soundTime: 0.4f);
      actor.EraseFromExistenceWithRewards(true); // NOTE: this suppresses hit effects, so we need to spawn them manually
      this._killedEnemy = true;
      ++this._killTracker;
    }

    private void OnRigidbodyCollision(CollisionData collision)
    {
      this._projectile.ResetPiercing();
      if (this._killedEnemy) //NOTE: need to spawn hit effects manually due to EraseFromExistenceWithRewards
      {
        this._killedEnemy = false;
        SpawnManager.SpawnVFX(this._projectile.hitEffects.enemy.effects[0].effects[0].effect, collision.Contact, Quaternion.identity, ignoresPools: true);
      }
    }

    private void OnDestroy()
    {
      if (this._killTracker < 2 || !this._owner || !this._owner.HasSynergy(Synergy.MASTERY_RETINA))
        return;
      if (this._owner.CurrentGun is not Gun gun || gun.gameObject.GetComponent<Retina>() is not Retina retina)
        return;

      // double kill -> refund shot
      gun.MoveBulletsIntoClip(1);
      gun.GainAmmo(1);
      if (this._killTracker < 3)
      {
        gun.gameObject.Play("halo_double_kill_sound");
        return;
      }

      // triple kill -> spawn armor piece
      Vector2 armorSpot = this._owner.CenterPosition;
      if (this._owner.CurrentRoom is RoomHandler room)
      {
        Vector2 betterSpot = room.GetCenteredVisibleClearSpot(2, 2, out bool success).ToVector2();
        if (success)
          armorSpot = betterSpot;
      }
      LootEngine.SpawnItem(ItemHelper.Get(Items.Armor).gameObject, armorSpot, Vector2.zero, 0f, true, true, false);
      if (this._killTracker < 4)
      {
        gun.gameObject.Play("halo_triple_kill_sound");
        return;
      }

      // quad kill -> fully restore ammo
      gun.GainAmmo(gun.AdjustedMaxAmmo);
      gun.MoveBulletsIntoClip(gun.DefaultModule.numberOfShotsInClip);
      gun.gameObject.Play("halo_overkill_sound");
    }

    private static float RetinaEmit(float t)
    {
      if (t < 0.4f)
        return 0f;
      if (t < 0.5f)
        return 10f * (t - 0.4f);
      return 1f;
    }

    private static float RetinaFade(float t)
    {
      if (t < 0.5f)
        return t * 0.1f;
      return 0.05f + 2.0f * (t - 0.5f);
    }

    private static float RetinaAmp(float t)
    {
      if (t < 0.5f)
        return t * 0.05f;
      return 0.025f + 2.0f * (t - 0.5f);
    }
}
