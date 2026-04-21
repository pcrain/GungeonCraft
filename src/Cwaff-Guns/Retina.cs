namespace CwaffingTheGungy;

/* TODO:
    - make entire HUD
      - orange visor
      - enemy diorama
      - health bar readout
      - range readout
      - enemy type readout
      - enemy count readout
      - obstruction readout

    - better dispersal particles
    - fix memory leaks on floor transitions

    - targeting sightlines
    - dissipate non-enemy game objects
    - gun animations
    - impact splash damage / explosion / goop
    - custom clip
*/

public class Retina : CwaffGun
{
    public static string ItemName         = "Retina";
    public static string ShortDescription = "Breach of Covenant";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private RetinaHUD _hud = null;

    public static void Init()
    {
        Lazy.SetupGun<Retina>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.S, gunClass: GunClass.RIFLE, reloadTime: 2.50f, ammo: 50, idleFps: 10, shootFps: 24, reloadFps: 30,
            smoothReload: 0.1f, reloadAudio: "retina_reload_sound", fireAudio: "retina_fire_sound")
          .InitProjectile(GunData.New(sprite: "retina_projectile", clipSize: 4, cooldown: 1.25f, angleVariance: 1.0f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 300.0f, speed: 300.0f, force: 10.0f, range: 1000.0f, pierceBreakables: true, hitSound: "retina_impact_sound", bossDamageMult: 0.6f))
          .SetAllImpactVFX(VFX.CreatePool("retina_impact_vfx", fps: 30, loops: false, emissivePower: 1f/*, lightColor: ExtendedColours.vibrantOrange, lightRange: 7.0f, lightStrength: 20.0f*/))
          .AttachTrail("retina_beam", fps: 60, timeTillAnimStart: 0.00f,
            destroyOnEmpty: true, dispersalPrefab: Lazy.DispersalParticles(ExtendedColours.vibrantOrange))
          .Attach<PierceProjModifier>(pierce => {
            pierce.penetration          = 999;
            pierce.penetratesBreakables = true; })
          .Attach<RetinaProjectile>();
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        CreateHUDIfNecessary();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        DismissHUD();
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        DismissHUD();
        base.OnDestroy();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        DismissHUD();
        base.OnSwitchedAwayFromThisGun();
    }

    public override void OnFullClipReload(PlayerController player, Gun gun)
    {
        CreateHUDIfNecessary();
        this._hud.Toggle();
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
      if (!this._hud)
        return;
      this._hud.Dismiss();
      this._hud = null;
    }
}

public class RetinaHUD : MonoBehaviour
{
  private const float _SHWOOP_TIME = 0.25f;
  private const int MAX_TARGETS = 5;

  private bool _setup              = false;   // whether we're set up
  private bool _active             = false;   // whether we're active
  private Retina _gun              = null;    // gun we're attache dto
  private Vector2 _center          = default; // center of the HUD (corresponds to view center)
  private Vector2 _topLeft         = default; // top left of the HUD, as determined by _center and _scale
  private float _scale             = 0.0f;    // percent of HUD that's visible
  private List<Geometry> _geometry = new();   // all shapes rendered by the HUD
  private List<dfLabel> _labels    = new();   // all labels rendered by the HUD

  private CameraController _camera;
  private Vector2 _worldBottomLeft;
  private Vector2 _worldTopRight;

  private Geometry _base;                     // backdrop of the HUD
  private Geometry _targetInfoRectangle;      // rectangular area where target information is drawn
  private Geometry _targetSpriteRectangle;    // subarea where target sprite is drawn and animated
  private Geometry _healthbarBack;            // backdrop for health bar
  private Geometry _healthbarHurt;            // foreground for health bar, projected health after shooting
  private Geometry _healthbarFore;            // foreground for health bar, current health

  private dfLabel _nameHeader;
  private dfLabel _nameLabel;
  private dfLabel _obstructionHeader;
  private dfLabel _obstructionLabel;
  private dfLabel _rangeHeader;
  private dfLabel _rangeLabel;
  private dfLabel _angleHeader;
  private dfLabel _angleLabel;
  private dfLabel _vulnHeader;
  private dfLabel _vulnLabel;
  private dfLabel _counterLabel;              // counter for additional enemies that are in line of sight

  private tk2dSprite _targetSprite;           // copy of the sprite for the current target
  private List<tk2dSprite> _extraTargetSprites; // copy of the sprite for the current extra targets

  public void Setup()
  {
    this._gun = this.gameObject.GetComponent<Retina>();

    this._base                  = Geom().Setup(shape: Geometry.Shape.RECTANGLE, color: ExtendedColours.vibrantOrange.WithAlpha(0.25f));
    this._targetInfoRectangle   = Geom().Setup(shape: Geometry.Shape.RECTANGLE, color: Color.yellow.WithAlpha(0.5f));
    this._targetSpriteRectangle = Geom().Setup(shape: Geometry.Shape.RECTANGLE, color: Color.black.WithAlpha(0.85f));
    this._healthbarBack         = Geom().Setup(shape: Geometry.Shape.RECTANGLE, color: Color.black);
    this._healthbarHurt         = Geom().Setup(shape: Geometry.Shape.RECTANGLE, color: Color.green.WithAlpha(0.85f));
    this._healthbarFore         = Geom().Setup(shape: Geometry.Shape.RECTANGLE, color: Color.red.WithAlpha(0.85f));

    this._nameHeader            = Lab(color: Color.cyan);
    this._nameLabel             = Lab(color: Color.cyan);
    this._obstructionHeader     = Lab(color: Color.cyan);
    this._obstructionLabel      = Lab(color: Color.cyan);
    this._rangeHeader           = Lab(color: Color.cyan);
    this._rangeLabel            = Lab(color: Color.cyan);
    this._angleHeader           = Lab(color: Color.cyan);
    this._angleLabel            = Lab(color: Color.cyan);
    this._vulnHeader            = Lab(color: Color.cyan);
    this._vulnLabel             = Lab(color: Color.cyan);
    this._counterLabel          = Lab(color: Color.cyan);

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
      extraTarget.renderer.material.shader = ShaderCache.Acquire("Brave/PlayerShader");
      extraTarget.renderer.enabled = false;
      return extraTarget;
  }

  private Geometry Geom()
  {
      Geometry g = new GameObject().AddComponent<Geometry>();
      g.gameObject.SetLayerRecursively(LayerMask.NameToLayer("GUI"));
      this._geometry.Add(g);
      return g;
  }

  private dfLabel Lab(Color? color = null, TextAlignment align = TextAlignment.Left)
  {
    dfLabel label = CwaffLabel.MakeNewLabel(unicode: false, outline: false);
    label.Color = color ?? Color.white;
    // label.OutlineColor = Color.black;
    label.TextAlignment = align; // TODO: make this actuall works
    this._labels.Add(label);
    return label;
  }

  private void Update()
  {
    if (!this._setup)
      return;
    if (!this._camera)
    {
      Dismiss(deactivate: true);
      UnityEngine.Object.Destroy(this);
    }
    else if (GameManager.Instance.IsPaused)
      Dismiss(deactivate: false);
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

    if (this._active && this._scale < 1.0f)
      // this._scale = Mathf.Min(this._scale + (BraveTime.DeltaTime / _SHWOOP_TIME), 1f);
      this._scale = 1.0f;
    else if (!this._active && this._scale > 0.0f)
      this._scale = Mathf.Max(this._scale - (BraveTime.DeltaTime / _SHWOOP_TIME), 0f);

    if (this._active)
      PlaceHUDElements();
  }

  private void Place(Geometry g, Vector2 topLeft, Vector2 bottomRight, Color? newColor = null)
  {
    Vector2 pos = new Vector2(
      Mathf.Lerp(this._worldBottomLeft.x, this._worldTopRight.x, topLeft.x),
      Mathf.Lerp(this._worldBottomLeft.y, this._worldTopRight.y, topLeft.y));
    Vector2 pos2 = new Vector2(
      Mathf.Lerp(this._worldBottomLeft.x, this._worldTopRight.x, bottomRight.x),
      Mathf.Lerp(this._worldBottomLeft.y, this._worldTopRight.y, bottomRight.y));
    g.Setup(pos: pos, pos2: pos2, color: newColor);
  }

  private void Place(dfLabel d, string text, Vector2 screenPos, Color? newColor = null)
  {
    Vector2 pos = new Vector2(
      Mathf.Lerp(this._worldBottomLeft.x, this._worldTopRight.x, screenPos.x),
      Mathf.Lerp(this._worldBottomLeft.y, this._worldTopRight.y, screenPos.y));
    if (newColor is Color c)
    {
      d.Color   = c.WithAlpha(1f);
      d.Opacity = c.a;
    }
    d.Text = text;
    d.Place(pos);
  }

  private void Place(tk2dSprite sprite, Vector2 screenPos, float scale = 1.0f, bool outline = false)
  {
    Vector2 pos = new Vector2(
      Mathf.Lerp(this._worldBottomLeft.x, this._worldTopRight.x, screenPos.x),
      Mathf.Lerp(this._worldBottomLeft.y, this._worldTopRight.y, screenPos.y));
    Vector3 extents = sprite.GetBounds().extents;
    float maxSpriteSide = 2f * C.PIXELS_PER_TILE * Mathf.Max(extents.x, extents.y);
    sprite.scale = new Vector3(scale, scale, 1f);
    sprite.PlaceAtScaledPositionByAnchor(pos, Anchor.LowerCenter);
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
    string enemyName = enemy.AmmonomiconName();
    if (_EnemyScales.TryGetValue(enemyName, out float scale))
      return scale;

    scale = 1f;
    if (enemy.sprite.collection.spriteDefinitions != null)
    {
      tk2dSpriteDefinition idleDef = enemy.sprite.collection.spriteDefinitions[Lazy.GetIdForBestIdleAnimation(enemy)];
      Vector3 extents = idleDef.boundsDataExtents;
      float maxSpriteSide = C.PIXELS_PER_TILE * Mathf.Max(extents.x, extents.y);
      scale = (maxSpriteSide < 30f) ? 2f : (maxSpriteSide < 60f) ? 1f : 0.5f;
    }
    return _EnemyScales[enemyName] = scale;
  }

  private static float GetNextLine(ref float y, float skip = 1.0f)
  {
    const float lineSpacing = -0.03f;
    float oldY = y;
    y += (lineSpacing * skip);
    return oldY;
  }

  private static string Status(AIActor target)
  {
    if (!target)
      return "------";
    if (target.healthHaver is not HealthHaver hh)
      return "No";
    if (target.IsGone)
      return "No";
    if (!hh.IsVulnerable)
      return "No";
    return "Yes";
  }

  private void SetUpTargetSprite(int ti)
  {
    AIActor target = (_TargetedEnemies.Count > ti) ? _TargetedEnemies[ti] : null;
    if (target && target.sprite is tk2dSprite enemySprite)
    {
      this._extraTargetSprites[ti].renderer.enabled = true;
      this._extraTargetSprites[ti].SetSprite(enemySprite.collection, enemySprite.spriteId);
      if (ti == 0)
        Place(this._extraTargetSprites[ti], new Vector2(0.85f, 0.55f), scale: GetScaleForEnemy(target), outline: true);
      else
      {
        float y = 0.45f + 0.10f * ti;
        Place(this._extraTargetSprites[ti], new Vector2(0.95f, y), scale: 0.25f * GetScaleForEnemy(target), outline: true);
      }
    }
    else
    {
      if (SpriteOutlineManager.HasOutline(this._extraTargetSprites[ti]))
        SpriteOutlineManager.RemoveOutlineFromSprite(this._extraTargetSprites[ti]);
      this._extraTargetSprites[ti].renderer.enabled = false;
    }
  }

  private List<AIActor> _TargetedEnemies = new();
  private void PlaceHUDElements()
  {
    // recompute camera pos
    this._worldBottomLeft = this._camera.MinVisiblePoint;
    this._worldTopRight = this._camera.MaxVisiblePoint;

    // determine target
    PlayerController player = this._gun.PlayerOwner;
    AIActor target = null;
    Vector2 bpos;
    float gunAngle;
    if (player && player.CurrentGun == this._gun.gun)
    {
      bpos = this._gun.gun.barrelOffset.position.XY();
      gunAngle = this._gun.gun.CurrentAngle;
      // TODO: this should return all enemies in direct sight unobstructed by a wall, sorted by closes first
      this._gun.gun.AllEnemiesInLineOfSight(ref _TargetedEnemies, accountForWalls: true, sort: true);
      target = (_TargetedEnemies.Count > 0) ? _TargetedEnemies[0] : null;
    }
    else
    {
      bpos = Vector2.zero;
      gunAngle = 0f;
    }

    // if (target && target.sprite is tk2dSprite enemySprite)
    // {
    //   this._targetSprite.renderer.enabled = true;
    //   this._targetSprite.SetSprite(enemySprite.collection, enemySprite.spriteId);
    //   Place(this._targetSprite, new Vector2(0.9f, 0.55f), scale: GetScaleForEnemy(target), outline: true);
    // }
    // else
    // {
    //   if (SpriteOutlineManager.HasOutline(this._targetSprite))
    //     SpriteOutlineManager.RemoveOutlineFromSprite(this._targetSprite);
    //   this._targetSprite.renderer.enabled = false;
    // }

    // compute some HUD parameters
    float now = BraveTime.ScaledTimeSinceStartup;
    float linePos     = 0.91f;
    string targetName = target ? target.AmmonomiconName() : "<none>";
    Vector2 delta     = target ? target.CenterPosition - bpos : Vector2.right;
    int dAngle        = Mathf.RoundToInt(delta.ToAngle().AbsAngleTo(gunAngle));
    string range      = target ? $"{Mathf.RoundToInt(16f * delta.magnitude)} cm" : "------";
    string angle      = target ? $"{dAngle}deg" : "------";
    Color healthColor = Color.Lerp(Color.green, Color.red, Mathf.Abs(Mathf.Sin(9f * now))).WithAlpha(0.85f);
    bool shotWillKill = false;

    // place indivual elements in screen space
    Place(this._base, new Vector2(0.0f, 0.0f), new Vector2(1.0f, 1.0f));
    Place(this._targetInfoRectangle, new Vector2(0.5f, 0.5f), new Vector2(1.0f, 1.0f));
    Place(this._targetSpriteRectangle, new Vector2(0.8f, 0.5f), new Vector2(1.0f, 1.0f));

    Place(this._nameHeader,        "Specimen",         new Vector2(0.6f, GetNextLine(ref linePos, 1.0f)), newColor: Color.cyan);
    Place(this._nameLabel,         targetName,         new Vector2(0.6f, GetNextLine(ref linePos, 1.0f)), newColor: ExtendedColours.pink);
    float healthTopY = GetNextLine(ref linePos, 1.0f);
    float healthBotY = GetNextLine(ref linePos, 1.5f);
    const float healthLeft = 0.55f;
    const float healthW = 0.10f;
    if (target && target.healthHaver is HealthHaver hh)
    {
      float projectedDamage = this._gun.gun.DefaultModule.projectiles[0].projectile.baseData.damage * player.DamageMult();
      float hpPre  = hh.GetCurrentHealthPercentage();
      float hpPost = Mathf.Max(hh.GetCurrentHealth() - projectedDamage, 0f) / hh.GetMaxHealth();
      shotWillKill = hpPost <= 0.0f;
      Place(this._healthbarHurt, new Vector2(healthLeft, healthBotY), new Vector2(healthLeft + hpPost * healthW, healthTopY));
      Place(this._healthbarFore, new Vector2(healthLeft + hpPost * healthW, healthBotY), new Vector2(healthLeft + hpPre * healthW, healthTopY), newColor: healthColor);
      Place(this._healthbarBack, new Vector2(healthLeft + hpPre * healthW, healthBotY), new Vector2(healthLeft + healthW, healthTopY));
    }
    else
    {
      this._healthbarBack._meshRenderer.enabled = false;
      this._healthbarHurt._meshRenderer.enabled = false;
      this._healthbarFore._meshRenderer.enabled = false;
    }

    Place(this._obstructionHeader, "Obstruction", new Vector2(0.6f, GetNextLine(ref linePos, 1.0f)), newColor: Color.cyan);
    Place(this._obstructionLabel,  "No",          new Vector2(0.6f, GetNextLine(ref linePos, 1.5f)), newColor: ExtendedColours.pink);
    Place(this._rangeHeader,       "Range",       new Vector2(0.6f, GetNextLine(ref linePos, 1.0f)), newColor: Color.cyan);
    Place(this._rangeLabel,        range,         new Vector2(0.6f, GetNextLine(ref linePos, 1.5f)), newColor: ExtendedColours.pink);
    Place(this._angleHeader,       "Angle",       new Vector2(0.6f, GetNextLine(ref linePos, 1.0f)), newColor: Color.cyan);
    Place(this._angleLabel,        angle,         new Vector2(0.6f, GetNextLine(ref linePos, 1.5f)), newColor: ExtendedColours.pink);
    Place(this._vulnHeader,        "Status",      new Vector2(0.6f, GetNextLine(ref linePos, 1.0f)), newColor: Color.cyan);
    Place(this._vulnLabel,         Status(target),new Vector2(0.6f, GetNextLine(ref linePos, 1.5f)), newColor: ExtendedColours.pink);
    Place(this._counterLabel,      "",            new Vector2(0.6f, GetNextLine(ref linePos, 1.0f)), newColor: Color.cyan);

    // handle additional targets
    for (int i = 0; i < MAX_TARGETS; ++i)
      SetUpTargetSprite(i);
  }

  private void OnDestroy()
  {
    GameManager.Instance.MainCameraController.OnFinishedFrame -= this.OnFinishedFrame;
    if (!this._setup)
      return;

    for (int i = this._geometry.Count - 1; i >= 0; --i)
      if (this._geometry[i])
        UnityEngine.Object.Destroy(this._geometry[i].gameObject);

    for (int i = this._labels.Count - 1; i >= 0; --i)
      if (this._labels[i])
        UnityEngine.Object.Destroy(this._labels[i].gameObject);

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

    this._scale = 0.0f;
    this._active = true;
  }

  public void Dismiss(bool force = false, bool deactivate = true)
  {
    if (!this._active && !force)
      return;

    foreach (Geometry g in this._geometry)
      if (g)
          g._meshRenderer.enabled = false;

    foreach (dfLabel label in this._labels)
    {
      if (label == null)
        continue;
      label.Opacity = 0.0f;
      label.IsVisible = false;
    }

    if (deactivate)
      this._active = false;
  }
}

// [HarmonyPatch]
public class RetinaProjectile : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private bool _killedEnemy;
    private Vector2? collisionPoint;

    // [HarmonyPatch(typeof(VFXPool), nameof(VFXPool.SpawnAtPosition), typeof(Vector3), typeof(float), typeof(Transform), typeof(Vector2?), typeof(Vector2?), typeof(float?), typeof(bool), typeof(VFXComplex.SpawnMethod), typeof(tk2dBaseSprite), typeof(bool))]
    // [HarmonyPrefix]
    // private static void ProjectileHandleHitEffectsEnemyPatch(VFXPool __instance)
    // {
    //   if (__instance.effects.Length > 0 && __instance.effects[0].effects.Length > 0)
    //     System.Console.WriteLine($"spawning {__instance.effects[0].effects[0].effect.name}");
    // }

    // [HarmonyPatch(typeof(SpawnManager), nameof(SpawnManager.Spawn), typeof(GameObject), typeof(Vector3), typeof(Quaternion), typeof(Transform), typeof(bool))]
    // [HarmonyPrefix]
    // private static void SpawnManagerSpawnPatch(SpawnManager __instance, GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, bool ignoresPools)
    // {
    //     if (!prefab || !prefab.name.Contains("retina"))
    //       return;
    //     System.Console.WriteLine($"spawning object {prefab.name} at {position.x},{position.y},{position.z} with ignorePools {ignoresPools} and parent {(parent == null ? "null" : parent.gameObject.name)}");
    // }

    private void Start()
    {
      this._projectile = base.GetComponent<Projectile>();
      this._projectile.OnWillKillEnemy += this.OnWillKillEnemy;
      this._projectile.specRigidbody.OnRigidbodyCollision += this.OnRigidbodyCollision;
      this._projectile.specRigidbody.OnTileCollision += this.OnTileCollision;
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
    }

    private void OnRigidbodyCollision(CollisionData collision)
    {
      this._projectile.ResetPiercing();
      collisionPoint = collision.Contact;
      if (this._killedEnemy) //NOTE: need to spawn hit effects manually due to EraseFromExistenceWithRewards
      {
        this._killedEnemy = false;
        SpawnManager.SpawnVFX(this._projectile.hitEffects.enemy.effects[0].effects[0].effect, collision.Contact, Quaternion.identity, ignoresPools: true);
      }
    }

    private void OnTileCollision(CollisionData tileCollision)
    {
      collisionPoint = tileCollision.Contact;
    }

    private static void DoLightBurst(Vector2 lightPoint)
    {
      EasyLight.Create(pos: lightPoint, color: ExtendedColours.vibrantOrange, radius: 4f, grownIn: true, brightness: 10.0f, fadeInTime: 0.2f, fadeOutTime: 0.2f, maxLifeTime: 0.5f);
    }

    private void OnDestroy()
    {
      Vector2 lightPoint = collisionPoint ?? (this._projectile ? this._projectile.SafeCenter : base.transform.position);
      DoLightBurst(lightPoint);
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
