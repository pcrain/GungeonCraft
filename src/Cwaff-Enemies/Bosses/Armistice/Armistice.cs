namespace CwaffingTheGungy;

public partial class ArmisticeBoss : AIActor
{
  public const   string BOSS_GUID              = "Armistice";
  internal const string BOSS_NAME              = "Armistice";
  private const  string SUBTITLE               = "Tranquil Gungeoneer";
  private const  string SPRITE_PATH            = $"{C.MOD_INT_NAME}/Resources/Bosses/armistice";

  internal const float TIRED_THRES = 0.67f;
  internal const float EXHAUSTED_THRES = 0.34f;

  private static GameObject _NapalmReticle      = null;
  private static AIBulletBank.Entry _MainBullet = null;
  private static AIBulletBank.Entry _TurretBullet = null;
  private static AIBulletBank.Entry _WarheadBullet = null;
  private static AIBulletBank.Entry _MagicMissileBullet = null;

  internal static GameObject _BulletSpawnVFX = null;
  internal static GameObject _MuzzleVFXBullet = null;
  internal static GameObject _MuzzleVFXElectro = null;
  internal static GameObject _MuzzleVFXTurret = null;
  internal static GameObject _MuzzleVFXSnipe = null;
  internal static GameObject _ExplosionVFX = null;
  internal static GameObject _SmokeVFX = null;
  internal static GameObject _MissileSmokeVFX = null;
  internal static GameObject _MissileFlak = null;
  internal static GameObject _BasicFlak = null;
  internal static GameObject _LaserFlakVFX = null;
  internal static CwaffTrailController _LaserTrailPrefab;
  internal static CwaffTrailController _TrickshotTrailPrefab;
  internal static CwaffTrailController _WarheadTrailPrefab;

  // #if DEBUG
  // private const  int _ARMISTICE_HP = 3;
  // #else
  // private const  int _ARMISTICE_HP = 150;
  // #endif

  private const  int _ARMISTICE_HP = 150;

  public static PrototypeDungeonRoom ArmisticeBossRoom = null;

  public static void Init()
  {
    BuildABoss bb = BuildABoss.LetsMakeABoss<BossBehavior>(bossname: BOSS_NAME, guid: BOSS_GUID, defaultSprite: $"{SPRITE_PATH}/armistice_idle_001",
      hitboxSize: new IntVector2(8, 9), subtitle: SUBTITLE, bossCardPath: $"{C.MOD_INT_NAME}/Resources/armistice_bosscard.png"); // Create our build-a-boss
    bb.SetStats(health: _ARMISTICE_HP, weight: 200f, speed: 0.4f, collisionDamage: 0f, hitReactChance: 0.05f, collisionKnockbackStrength: 0f,
      healthIsNumberOfHits: true, invulnerabilityPeriod: 1.0f, shareCooldowns: false, spriteAnchor: Anchor.LowerCenter); // Set our stats
    bb.InitSpritesFromResourcePath(spritePath: SPRITE_PATH); // Set up our animations
      bb.AdjustAnimation(name: "attack_basic", fps:    16f, loop: true);
      bb.AdjustAnimation(name: "attack_snipe", fps:    16f, loop: false);
      bb.AdjustAnimation(name: "breathe",      fps:     2f, loop: true);
      bb.AdjustAnimation(name: "calm",         fps:     8f, loop: true);
      bb.AdjustAnimation(name: "crouch",       fps:    16f, loop: false, eventFrames: [4],
        eventAudio: ["armistice_missile_launch_sound"]);
      bb.AdjustAnimation(name: "death",        fps:    15f, loop: false);
      bb.AdjustAnimation(name: "defeat",       fps:     3f, loop: true, loopFrame: 4);
      bb.AdjustAnimation(name: "exhausted",    fps:     4f, loop: true);
      bb.AdjustAnimation(name: "idle",         fps:    16f, loop: true);
      bb.AdjustAnimation(name: "ready",        fps:    16f, loop: false);
      bb.AdjustAnimation(name: "reload",       fps:    16f, loop: false, eventFrames: [6, 12],
        eventAudio: ["armistice_reload_sound_a", "armistice_reload_sound_b"]);
      bb.AdjustAnimation(name: "run",          fps:    40f, loop: true,  eventFrames: [4, 8],
        eventAudio: ["armistice_step_sound", "armistice_step_sound"]);
      bb.AdjustAnimation(name: "skyshot",      fps:    30f, loop: false, eventFrames: [5]);
      bb.AdjustAnimation(name: "talk",         fps:     8f, loop: true);
      bb.AdjustAnimation(name: "teleport_in",  fps:     9f, loop: false);
      bb.AdjustAnimation(name: "teleport_out", fps:     9f, loop: false);
      bb.AdjustAnimation(name: "tired",        fps:     6f, loop: true);
      bb.SetIntroAnimations(introAnim: "idle", preIntroAnim: "idle"); // Set up our intro animations (TODO: pre-intro not working???)
    bb.SetDefaultColliders(width: 30, height: 40, xoff: -15, yoff: 2);          // Set our default pixel colliders
    bb.AddCustomIntro<ArmisticeIntro>();                                       // Add custom animation to the generic intro doer
    //BUG: without a postFight script, the boss "dies" and spawns a synergy chest + reward pedestal due to technically being in the Abbey. we manually handle this in the post-fight script for now
    bb.MakeInteractible<ArmisticeNPC>(preFight: true, postFight: true, noOutlines: true, talkPointOffset: new Vector2(-0.375f, 0.25f)); // Add some pre-fight and post-fight dialogue
    bb.TargetPlayer();                                                         // Set up the boss's targeting scripts
    bb.AddCustomMusic(name: "collapse", loopAt: 320576, rewind: 225251);       // Add custom music for our boss

    const float CD = 3.0f;
    const float ACD = 0.5f;
    bb.CreateBulletAttack<BoneTunnelScript, ArmisticeMoveAndShootBehavior>    (tellAnim: "teleport_out", fireAnim: "breathe", finishAnim: "teleport_in", cooldown: CD, attackCooldown: ACD, interruptible: true);
    bb.CreateBulletAttack<ClocksTickingScript, ArmisticeMoveAndShootBehavior> (fireAnim: "calm", cooldown: CD, attackCooldown: ACD, interruptible: true, initialCooldown: 5.0f);
    bb.CreateBulletAttack<BoxTrotScript, ArmisticeMoveAndShootBehavior>       (tellAnim: "ready", fireAnim: "attack_snipe", cooldown: CD, attackCooldown: ACD, interruptible: true);
    bb.CreateBulletAttack<LaserBarrageScript, ArmisticeMoveAndShootBehavior>  (tellAnim: "ready", fireAnim: "attack_basic", cooldown: CD, attackCooldown: ACD, interruptible: true);
    bb.CreateBulletAttack<MeteorShowerScript, ArmisticeMoveAndShootBehavior>  (tellAnim: "reload", fireAnim: "skyshot", cooldown: CD, attackCooldown: ACD, interruptible: true);
    bb.CreateBulletAttack<TrickshotScript, ArmisticeMoveAndShootBehavior>     (fireAnim: "idle", cooldown: CD, attackCooldown: ACD, interruptible: true);
    bb.CreateBulletAttack<MagicMissileScript, ArmisticeMoveAndShootBehavior>  (tellAnim: "reload", fireAnim: "crouch", cooldown: CD, attackCooldown: ACD, interruptible: true);
    bb.CreateBulletAttack<SniperScript, ArmisticeMoveAndShootBehavior>        (tellAnim: "reload", cooldown: CD, attackCooldown: ACD, interruptible: true);

    bb.AddBossToGameEnemies(name: $"{C.MOD_PREFIX}:armisticeboss");               // Add our boss to the enemy database
    ArmisticeBossRoom = bb.CreateStandaloneBossRoom(width: 40, height: 30, exitOnBottom: true);

    // fix up the lights a bit
    ArmisticeBossRoom.usesProceduralDecoration = false;
    ArmisticeBossRoom.usesProceduralLighting = false;
    foreach (int y in (int[])[0, 10, 19, 29])
      foreach (int x in (int[])[4, 14, 25, 35])
        ArmisticeBossRoom.FullCellData[x + y * 40].containsManuallyPlacedLight = true;

    InitPrefabs();                                                             // Do miscellaneous prefab loading
  }

  private static void InitPrefabs()
  {
    // VFX
    _BulletSpawnVFX = VFX.Create("armistice_bulet_spawn_vfx", fps: 60, loops: false);
    _MuzzleVFXBullet = VFX.Create("muzzle_armistice_bullet", fps: 60, loops: false, anchor: Anchor.MiddleLeft);
    _MuzzleVFXElectro = VFX.Create("muzzle_armistice_electro", fps: 60, loops: false, anchor: Anchor.MiddleLeft);
    _MuzzleVFXTurret = VFX.Create("muzzle_armistice_turret", fps: 60, loops: false, anchor: Anchor.MiddleLeft);
    _MuzzleVFXSnipe = VFX.Create("muzzle_armistice_snipe", fps: 60, loops: false);
    _ExplosionVFX = VFX.Create("armistice_warhead_explosion_vfx", fps: 30, loops: false);
    _SmokeVFX = VFX.Create("armistice_warhead_smoke", fps: 16, loops: false, scale: 0.5f);
    _LaserFlakVFX = VFX.Create("armistice_laser_flak", fps: 30, loops: false);
    _MissileSmokeVFX = VFX.Create("armistice_missile_smoke_vfx", fps: 60, loops: false);
    _LaserTrailPrefab = VFX.CreateSpriteTrailObject("armistice_laser_trail", fps: 60, softMaxLength: 1f, cascadeTimer: C.FRAME, destroyOnEmpty: true);
    _TrickshotTrailPrefab = VFX.CreateSpriteTrailObject("armistice_trickshot_trail_a", fps: 60, softMaxLength: 1f, cascadeTimer: C.FRAME, destroyOnEmpty: true);
    _WarheadTrailPrefab = VFX.CreateSpriteTrailObject("armistice_warhead_smoke_trail", fps: 60, softMaxLength: 1f, cascadeTimer: C.FRAME, destroyOnEmpty: true);

    // Main bullet
    AIBulletBank.Entry baseBullet = EnemyDatabase.GetOrLoadByGuid(Enemies.Chancebulon).bulletBank.GetBullet("reversible");
    Projectile baseProj = baseBullet.BulletObject.ClonePrefab().GetComponent<Projectile>();
    baseProj.gameObject.name = "armistice base projectile";
    // baseProj.ClearAllImpactVFX();
    _MainBullet = new AIBulletBank.Entry(baseBullet) {
      Name               = "getboned",
      PlayAudio          = false,
      BulletObject       = baseProj.gameObject,
      MuzzleFlashEffects = VFX.CreatePoolFromVFXGameObject(Lazy.GunDefaultProjectile(29).hitEffects.overrideMidairDeathVFX),
      // MuzzleFlashEffects = null,
    };
    _MainBullet.MuzzleFlashEffects.type = VFXPoolType.None;

    //WARNING: enemy projectiles use bagel colliders, which don't work well with new sprites. clone a player projectile as a base instead
    // Projectile turretProj = baseBullet.BulletObject.ClonePrefab().GetComponent<Projectile>();
    Projectile turretProj = Items._38Special.CloneProjectile();
    turretProj.gameObject.name = "armistice turret projectile";
    turretProj.AddDefaultAnimation(AnimatedBullet.Create(name: "armistice_turret", fps: 30), overwriteExisting: true);
    // turretProj.specRigidbody.DebugColliders();
    _TurretBullet = new AIBulletBank.Entry(baseBullet) {
      Name               = "turret",
      PlayAudio          = false,
      BulletObject       = turretProj.gameObject,
      MuzzleFlashEffects = VFX.CreatePoolFromVFXGameObject(Lazy.GunDefaultProjectile(29).hitEffects.overrideMidairDeathVFX),
    };

    Projectile warheadProj = Items._38Special.CloneProjectile();
    warheadProj.gameObject.name = "armistice warhead projectile";
    warheadProj.AddDefaultAnimation(AnimatedBullet.Create(name: "armistice_warhead"), overwriteExisting: true);
    _WarheadBullet = new AIBulletBank.Entry(baseBullet) {
      Name               = "warhead",
      PlayAudio          = false,
      BulletObject       = warheadProj.gameObject,
      MuzzleFlashEffects = VFX.CreatePoolFromVFXGameObject(Lazy.GunDefaultProjectile(29).hitEffects.overrideMidairDeathVFX),
    };

    Projectile magicMissileProj = Items._38Special.CloneProjectile();
    magicMissileProj.gameObject.name = "armistice magic missile projectile";
    magicMissileProj.shouldFlipHorizontally = false;
    magicMissileProj.shouldFlipVertically = false;
    SpeculativeRigidbody mmBody = magicMissileProj.specRigidbody;
    mmBody.UpdateCollidersOnRotation = true;
    mmBody.UpdateCollidersOnScale = true;
    magicMissileProj.AddDefaultAnimation(AnimatedBullet.Create(
        name: "armistice_magic_missile_b", fps: 30, firstFrameIsReference: true,
        overrideColliderPixelSizes: new IntVector2(16, 7), overrideColliderOffsets: new IntVector2(8, 0), anchorsChangeColliders: false, fixesScales: false),
      overwriteExisting: true);
    _MagicMissileBullet = new AIBulletBank.Entry(baseBullet) {
      Name               = "magicmissile",
      PlayAudio          = false,
      BulletObject       = magicMissileProj.gameObject,
      MuzzleFlashEffects = VFX.CreatePoolFromVFXGameObject(Lazy.GunDefaultProjectile(29).hitEffects.overrideMidairDeathVFX),
    };
    _MissileFlak = Lazy.EasyDebris("armistice_missile_flak");
    _BasicFlak = Lazy.EasyDebris("armistice_basic_flak");
  }

  private static void SpawnDust(Vector2 where)
    { SpawnManager.SpawnVFX(GameManager.Instance.Dungeon.dungeonDustups.rollLandDustup, where, Lazy.RandomEulerZ()); }

  private class BirthShaderHandler : MonoBehaviour
  {
    private Material mat;
    private int coordsId;

    private void Start()
    {
      mat = new Material(CwaffShaders.BirthShader);
      mat.SetTexture("_NoiseTex", CwaffShaders.StarNoiseTexture);
      mat.SetFloat("_Emission", 300f);
      mat.SetFloat("_FlashSpeed", 0.02f);
      mat.SetFloat("_Density", 0.02f);
      coordsId = Shader.PropertyToID("_CamXYWH");
      Update();
      TurnOn();
    }

    private void Update()
    {
      CameraController cam = GameManager.Instance.MainCameraController;
      Vector2 camMin = cam.MinVisiblePoint;
      Vector2 camMax = cam.MaxVisiblePoint;
      Vector2 camSize = camMax - camMin;
      mat.SetVector(coordsId, new Vector4(0.0625f * camMin.x, 0.0625f * camMin.y, camSize.x, camSize.y));
    }

    internal void TurnOn() => Pixelator.Instance.RegisterAdditionalRenderPass(mat);
    internal void TurnOff() => Pixelator.Instance.DeregisterAdditionalRenderPass(mat);
  }

  private class BossBehavior : BraveBehaviour
  {
    private static GameObject _ParticleSystem = null;

    internal ParticleSystem _ps = null;

    private static GameObject MakeParticleSystem(Color particleColor)
    {
        GameObject psBasePrefab = Items.CombinedRifle.AsGun().alternateVolley.projectiles[0].projectiles[0].GetComponent<CombineEvaporateEffect>().ParticleSystemToSpawn;
        GameObject psnewPrefab = UnityEngine.Object.Instantiate(psBasePrefab).RegisterPrefab();
        //NOTE: look at CombineSparks.prefab for reference
        //NOTE: uses shader https://github.com/googlearchive/soundstagevr/blob/master/Assets/third_party/Sonic%20Ether/Shaders/SEParticlesAdditive.shader
        ParticleSystem ps = psnewPrefab.GetComponent<ParticleSystem>();
        // ETGModConsole.Log($"was using shader {psObj.GetComponent<ParticleSystemRenderer>().material.shader.name}");

        float arcSpeed = 2f;

        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake             = false;
        main.duration                = 3600f;
        main.startLifetime           = 1.0f; // slightly higher than one rotation
        // main.startSpeed              = 6.0f;
        main.startSize               = 0.0625f;
        main.scalingMode             = ParticleSystemScalingMode.Local;
        main.startRotation           = 0f;
        main.startRotation3D         = false;
        main.startRotationMultiplier = 0f;
        main.maxParticles            = 200;
        main.startColor              = particleColor;
        main.emitterVelocityMode     = ParticleSystemEmitterVelocityMode.Transform;

        ParticleSystem.ForceOverLifetimeModule force = ps.forceOverLifetime;
        force.enabled = false;

        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        AnimationCurve vcurve = new AnimationCurve();
        vcurve.AddKey(0.0f, 5.0f);
        // vcurve.AddKey(0.5f, 6.0f);
        // vcurve.AddKey(0.8f, 1.0f);
        vcurve.AddKey(1.0f, 5.0f);
        vel.x = vel.y = vel.z = new ParticleSystem.MinMaxCurve(1.0f, vcurve);
        vel.xMultiplier = vel.yMultiplier = vel.zMultiplier = 1.0f;
        vel.xMultiplier = 0.0f;

        ParticleSystem.RotationOverLifetimeModule rotl = ps.rotationOverLifetime;
        rotl.enabled = false;

        ParticleSystem.RotationBySpeedModule rots = ps.rotationBySpeed;
        rots.enabled = false;

        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(particleColor, 0.0f), new GradientColorKey(particleColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0.0f), new GradientAlphaKey(1.0f, 0.9f), new GradientAlphaKey(0.01f, 1.0f) }
            // new GradientAlphaKey[] { new GradientAlphaKey(1f, 0.0f), new GradientAlphaKey(0.5f, 0.25f), new GradientAlphaKey(0.15f, 0.5f),  new GradientAlphaKey(0.01f, 0.75f), new GradientAlphaKey(1.0f, 1.0f) }
        );
        ParticleSystem.ColorOverLifetimeModule colm = ps.colorOverLifetime;
        colm.color = new ParticleSystem.MinMaxGradient(g); // looks jank

        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 50f;

        ParticleSystemRenderer psr = psnewPrefab.GetComponent<ParticleSystemRenderer>();
        psr.material.SetFloat("_InvFade", 3.0f);
        psr.material.SetFloat("_EmissionGain", 0.5f);
        psr.material.SetColor("_EmissionColor", particleColor);
        psr.material.SetColor("_DiffuseColor", particleColor);
        psr.sortingLayerName = "Foreground";

        ParticleSystem.SizeOverLifetimeModule psz = ps.sizeOverLifetime;
        psz.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0.0f, 1.0f);
        sizeCurve.AddKey(0.5f, 1.0f);
        sizeCurve.AddKey(1.0f, 0.0f);
        psz.size = new ParticleSystem.MinMaxCurve(1.5f, sizeCurve);

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
        shape.randomDirectionAmount = 0f;
        shape.alignToDirection = false;
        shape.scale           = Vector3.one;
        shape.radiusThickness = 1.0f;
        shape.radiusMode      = ParticleSystemShapeMultiModeValue.Random;
        shape.length          = 2f;
        shape.position        = new Vector3(-0.5f, 0.0f, 0.0f);
        shape.radius          = 1.0f;
        shape.rotation        = Vector3.up;
        shape.arc             = 360f;
        shape.arcMode         = ParticleSystemShapeMultiModeValue.Random;
        shape.arcSpeed        = arcSpeed;
        shape.meshShapeType   = ParticleSystemMeshShapeType.Vertex;

        ParticleSystem.InheritVelocityModule iv = ps.inheritVelocity;
        iv.enabled = true;
        iv.mode = ParticleSystemInheritVelocityMode.Current;
        iv.curveMultiplier = 1f;
        AnimationCurve ivcurve = new AnimationCurve();
        ivcurve.AddKey(0.0f, 1.0f);
        ivcurve.AddKey(1.0f, 1.0f);
        iv.curve = new ParticleSystem.MinMaxCurve(1.0f, ivcurve);

        return psnewPrefab;
    }

    private static readonly Color _CalmRed = Color.Lerp(Color.red, Color.white, 0.15f);
    private static readonly Color _CalmBlue = Color.Lerp(Color.cyan, Color.white, 0.15f);

    private Color? _lastParticleColor = null;
    private int _lastDustFrame = -1;
    private HealthHaver _hh = null;
    private bool _inCombat = false;

    private void Start()
    {
      base.aiActor.bulletBank.Bullets.Add(_MainBullet);
      base.aiActor.bulletBank.Bullets.Add(_TurretBullet);
      base.aiActor.bulletBank.Bullets.Add(_WarheadBullet);
      base.aiActor.bulletBank.Bullets.Add(_MagicMissileBullet);
      base.aiActor.healthHaver.forcePreventVictoryMusic = true; // prevent default floor theme from playing on death
      this._hh = base.aiActor.healthHaver;
      base.aiActor.healthHaver.OnPreDeath += OnPreDeath;

      if (_ParticleSystem == null)
        _ParticleSystem = MakeParticleSystem(_CalmRed);
      GameObject psObj = UnityEngine.Object.Instantiate(_ParticleSystem);
      psObj.transform.position = base.aiActor.sprite.WorldBottomCenter;
      psObj.transform.parent   = base.gameObject.transform;
      psObj.transform.localRotation = Quaternion.identity;
      this._ps = psObj.GetComponent<ParticleSystem>();
      this._ps.Stop();
      this._ps.Clear();

      base.aiActor.aiAnimator.PlayUntilCancelled("calm");

      CwaffRunData.Instance.scrambledBulletHell = true; // going to bullet hell this run scrambles it

      new GameObject("birth shader handler", typeof(BirthShaderHandler));
      CwaffDungeons.PlayCustomFloorMusicDelayed(0.5f); //HACK: temporary hack to get past Modular bug
    }

    internal void FinishedIntro()
    {
      this._inCombat = true;
    }

    private void OnPreDeath(Vector2 _)
    {
      this._inCombat = false;

      GameManager.Instance.DungeonMusicController.LoopMusic(musicName: "clocktowers", loopPoint: 210239, rewindAmount: 182080);
      base.aiActor.aiAnimator.PlayUntilCancelled("defeat");

      CameraController mainCameraController = GameManager.Instance.MainCameraController;
      mainCameraController.OverrideZoomScale = 1.0f;
      mainCameraController.LockToRoom = false;
      mainCameraController.UseOverridePlayerOnePosition = false;
      mainCameraController.UseOverridePlayerTwoPosition = false;

      CustomTrackedStats.DEFEATED_ARMI.Increment();
      CustomDungeonFlags.HAS_DEFEATED_ARMI.Set();
    }

    private Geometry _debugHitbox = null;

    private void SetParticleColor(Color? colorMaybe = null)
    {
      if (!colorMaybe.HasValue)
      {
        if (this._ps.isPlaying)
        {
          this._ps.Stop();
          if (!this._lastParticleColor.HasValue)
            this._ps.Clear(); //HACK: fixes a bug where a singular particle plays right after the fight starts
        }
        return;
      }

      Color color = colorMaybe.Value;
      if (this._lastParticleColor != color)
      {
        var main = this._ps.main;
        main.startColor = color;
        GradientColorKey[] keys = this._ps.colorOverLifetime.color.gradient.colorKeys;
        for (int i = 0; i < keys.Length; ++i)
          keys[i].color = color;

        ParticleSystemRenderer psr = this._ps.gameObject.GetComponent<ParticleSystemRenderer>();
        psr.material.SetColor("_EmissionColor", color);
        psr.material.SetColor("_DiffuseColor", color);

        this._lastParticleColor = color;
      }

      if (!this._ps.isPlaying)
        this._ps.Play();
    }

    private void LateUpdate() // movement is buggy if we use the regular Update() method
    {
      if (BraveTime.DeltaTime == 0 || !this._inCombat)
        return; // don't do anything if we're paused or pre-intro

      #if DEBUG
      // base.specRigidbody.DrawDebugHitbox();
      // DebugDraw.DrawDebugCircle(base.gameObject, base.transform.position, 0.5f, Color.green.WithAlpha(0.5f));
      // DebugDraw.DrawDebugCircle(GameManager.Instance.gameObject, base.transform.position.GetAbsoluteRoom().area.Center, 0.5f, Color.cyan.WithAlpha(0.5f));
      #endif

      float healthRatio = this._hh.currentHealth / this._hh.maximumHealth;
      if (healthRatio < EXHAUSTED_THRES)
      {
        if (base.aiActor.aiAnimator.OverrideIdleAnimation != "exhausted")
          base.aiActor.aiAnimator.OverrideIdleAnimation = "exhausted";
      }
      else if (healthRatio < TIRED_THRES)
      {
        if (base.aiActor.aiAnimator.OverrideIdleAnimation != "tired")
          base.aiActor.aiAnimator.OverrideIdleAnimation = "tired";
      }

      tk2dSpriteAnimator anim = base.spriteAnimator;
      tk2dSpriteAnimationClip clip = anim.CurrentClip;
      if (clip.name == "run" && anim.CurrentFrame != this._lastDustFrame)
      {
        SpawnDust(base.transform.position);
        this._lastDustFrame = anim.CurrentFrame;
      }
      if (clip.name == "defeat" && anim.CurrentFrame < 4)
        anim.ClipFps = 24; // first part of defeat animation should have a higher fps
      else
        anim.ClipFps = 0;
      SetParticleColor(clip.name switch {
        "calm"    => _CalmRed,
        "breathe" => _CalmBlue,
        _         => null
      });
    }
  }

  private class ArmisticeIntro : SpecificIntroDoer
  {
    public override void PlayerWalkedIn(PlayerController player, List<tk2dSpriteAnimator> animators)
    {
      // Set up room specific attacks
      Rect roomTeleportBounds = base.aiActor.GetAbsoluteParentRoom().GetBoundingRect().Inset(8f);
      foreach (AttackBehaviorGroup.AttackGroupItem attack in base.aiActor.GetComponent<BehaviorSpeculator>().AttackBehaviorGroup.AttackBehaviors)
      {
        if (attack.Behavior is not TeleportBehavior)
          continue;
        TeleportBehavior tb = attack.Behavior as TeleportBehavior;
          tb.ManuallyDefineRoom = true;
          tb.roomMin            = roomTeleportBounds.min;
          tb.roomMax            = roomTeleportBounds.max;
      }

      // Set up camera
      CameraController mainCameraController = GameManager.Instance.MainCameraController;
      mainCameraController.OverrideZoomScale = 0.5f;
      mainCameraController.LockToRoom = true;
      mainCameraController.UseOverridePlayerOnePosition = true;
      mainCameraController.OverridePlayerOnePosition = roomTeleportBounds.center;
      mainCameraController.UseOverridePlayerTwoPosition = true;
      mainCameraController.OverridePlayerTwoPosition = roomTeleportBounds.center;
      // mainCameraController.AddFocusPoint(head.gameObject);
    }

    public override void EndIntro()
    {
      base.aiActor.GetComponent<BossBehavior>().FinishedIntro();
    }
  }
}
