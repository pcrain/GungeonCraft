namespace CwaffingTheGungy;

public partial class ArmisticeBoss : AIActor
{
  public  const  string BOSS_GUID              = "Armistice";
  private const  string BOSS_NAME              = "Armistice";
  private const  string SUBTITLE               = "Trapped Gungeoneer";
  private const  string SPRITE_PATH            = $"{C.MOD_INT_NAME}/Resources/Bosses/armistice";

  private static GameObject _NapalmReticle      = null;
  private static AIBulletBank.Entry _MainBullet = null;
  private static AIBulletBank.Entry _TurretBullet = null;
  private static AIBulletBank.Entry _WarheadBullet = null;

  internal static GameObject _BulletSpawnVFX = null;
  internal static GameObject _MuzzleVFXBullet = null;
  internal static GameObject _MuzzleVFXElectro = null;
  internal static GameObject _MuzzleVFXTurret = null;
  internal static GameObject _ExplosionVFX = null;
  internal static GameObject _SmokeVFX = null;
  internal static CwaffTrailController _LaserTrailPrefab;
  internal static CwaffTrailController _WarheadTrailPrefab;

  private const  int _SANS_HP = 60;

  public static PrototypeDungeonRoom ArmisticeBossRoom = null;

  public static void Init()
  {
    BuildABoss bb = BuildABoss.LetsMakeABoss<BossBehavior>(bossname: BOSS_NAME, guid: BOSS_GUID, defaultSprite: $"{SPRITE_PATH}/armistice_idle_001",
      hitboxSize: new IntVector2(8, 9), subtitle: SUBTITLE, bossCardPath: $"{C.MOD_INT_NAME}/Resources/armistice_bosscard.png"); // Create our build-a-boss
    bb.SetStats(health: _SANS_HP, weight: 200f, speed: 0.4f, collisionDamage: 0f, hitReactChance: 0.05f, collisionKnockbackStrength: 0f,
      healthIsNumberOfHits: true, invulnerabilityPeriod: 1.0f, shareCooldowns: false, spriteAnchor: Anchor.LowerCenter); // Set our stats
    bb.InitSpritesFromResourcePath(spritePath: SPRITE_PATH); // Set up our animations
      bb.AdjustAnimation(name: "attack_basic", fps:    16f, loop: true);
      bb.AdjustAnimation(name: "attack_snipe", fps:    16f, loop: false);
      bb.AdjustAnimation(name: "calm",         fps:     6f, loop: true);
      bb.AdjustAnimation(name: "crouch",       fps:    16f, loop: false);
      bb.AdjustAnimation(name: "idle",         fps:    16f, loop: true);
      bb.AdjustAnimation(name: "ready",        fps:    16f, loop: false);
      bb.AdjustAnimation(name: "reload",       fps:    16f, loop: false, eventFrames: [6, 12],
        eventAudio: ["armistice_reload_sound_a", "armistice_reload_sound_b"]);
      bb.AdjustAnimation(name: "run",          fps:    16f, loop: true);
      bb.AdjustAnimation(name: "skyshot",      fps:    30f, loop: false, eventFrames: [5]);
      bb.AdjustAnimation(name: "teleport_in",  fps:    16f, loop: false);
      bb.AdjustAnimation(name: "teleport_out", fps:    16f, loop: false);
      bb.SetIntroAnimations(introAnim: "idle", preIntroAnim: "idle"); // Set up our intro animations (TODO: pre-intro not working???)
      // bb.SetIntroAnimations(introAnim: "decloak", preIntroAnim: "idle_cloak"); // Set up our intro animations (TODO: pre-intro not working???)
    bb.SetDefaultColliders(width: 30, height: 40, xoff: -15, yoff: 2);          // Set our default pixel colliders
    bb.AddCustomIntro<ArmisticeIntro>();                                       // Add custom animation to the generic intro doer
    // bb.MakeInteractible<ArmisticeNPC>(preFight: true, postFight: true) ;       // Add some pre-fight and post-fight dialogue
    bb.TargetPlayer();                                                         // Set up the boss's targeting scripts
    bb.AddCustomMusic(name: "collapse", loopAt: 320576, rewind: 225251);       // Add custom music for our boss
    // bb.AddNamedVFX(pool: VFX.vfxpool["Tornado"], name: "mytornado");           // Add some named vfx pools to our bank of VFX
    // bb.CreateTeleportAttack<CustomTeleportBehavior>(                           // Add some attacks
    //   goneTime: 0.25f, outAnim: "teleport_out", inAnim: "teleport_in", cooldown: 4.26f, attackCooldown: 0.15f, probability: 3f);
    // bb.CreateBulletAttack<CrossBulletsScript, ArmisticeMoveAndShootBehavior>  (fireAnim: "idle", cooldown: 2.0f, attackCooldown: 2.0f, interruptible: true);
    // bb.CreateBulletAttack<ClocksTickingScript, ArmisticeMoveAndShootBehavior> (fireAnim: "calm", cooldown: 2.0f, attackCooldown: 2.0f, interruptible: true);
    // bb.CreateBulletAttack<WalledInScript, ArmisticeMoveAndShootBehavior>      (fireAnim: "idle", cooldown: 2.0f, attackCooldown: 2.0f, interruptible: true);
    // bb.CreateBulletAttack<BoneTunnelScript, ArmisticeMoveAndShootBehavior>    (fireAnim: "idle", cooldown: 2.0f, attackCooldown: 2.0f, interruptible: true);
    // bb.CreateBulletAttack<DanceMonkeyScript, ArmisticeMoveAndShootBehavior>   (fireAnim: "idle", cooldown: 2.0f, attackCooldown: 2.0f, interruptible: true);
    // bb.CreateBulletAttack<PendulumScript, ArmisticeMoveAndShootBehavior>      (fireAnim: "idle", cooldown: 2.0f, attackCooldown: 2.0f, interruptible: true);
    // bb.CreateBulletAttack<BoxTrotScript, ArmisticeMoveAndShootBehavior>       (tellAnim: "ready", fireAnim: "attack_snipe", cooldown: 2.0f, attackCooldown: 2.0f, interruptible: true);
    // bb.CreateBulletAttack<LaserBarrageScript, ArmisticeMoveAndShootBehavior>  (tellAnim: "ready", fireAnim: "attack_basic", cooldown: 2.0f, attackCooldown: 2.0f, interruptible: true);
    bb.CreateBulletAttack<MeteorShowerScript, ArmisticeMoveAndShootBehavior>       (tellAnim: "reload", fireAnim: "skyshot", cooldown: 2.0f, attackCooldown: 2.0f, interruptible: true);
    bb.AddBossToGameEnemies(name: $"{C.MOD_PREFIX}:armisticeboss");               // Add our boss to the enemy database
    ArmisticeBossRoom = bb.CreateStandaloneBossRoom(width: 40, height: 30, exitOnBottom: true);
    InitPrefabs();                                                             // Do miscellaneous prefab loading
  }

  private static void InitPrefabs()
  {
    // VFX
    _BulletSpawnVFX = VFX.Create("armistice_bulet_spawn_vfx", fps: 60, loops: false);
    _MuzzleVFXBullet = VFX.Create("muzzle_armistice_bullet", fps: 60, loops: false, anchor: Anchor.MiddleLeft);
    _MuzzleVFXElectro = VFX.Create("muzzle_armistice_electro", fps: 60, loops: false, anchor: Anchor.MiddleLeft);
    _MuzzleVFXTurret = VFX.Create("muzzle_armistice_turret", fps: 60, loops: false, anchor: Anchor.MiddleLeft);
    _ExplosionVFX = VFX.Create("armistice_warhead_explosion_vfx", fps: 30, loops: false);
    _SmokeVFX = VFX.Create("armistice_warhead_smoke", fps: 16, loops: false, scale: 0.5f);
    _LaserTrailPrefab = VFX.CreateSpriteTrailObject("armistice_laser_trail", fps: 60, softMaxLength: 1f, cascadeTimer: C.FRAME, destroyOnEmpty: true);
    _WarheadTrailPrefab = VFX.CreateSpriteTrailObject("armistice_warhead_smoke_trail", fps: 60, softMaxLength: 1f, cascadeTimer: C.FRAME, destroyOnEmpty: true);

    // Targeting reticle
    _NapalmReticle = ResourceManager.LoadAssetBundle("shared_auto_002").LoadAsset<GameObject>("NapalmStrikeReticle").ClonePrefab();
      _NapalmReticle.GetComponent<tk2dSlicedSprite>().SetSprite(VFX.Collection, VFX.Collection.GetSpriteIdByName("reticle_white"));
      UnityEngine.Object.Destroy(_NapalmReticle.GetComponent<ReticleRiserEffect>());  // delete risers for use with DoomZoneGrowth component later
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
    warheadProj.gameObject.name = "armistice warheadProj projectile";
    warheadProj.AddDefaultAnimation(AnimatedBullet.Create(name: "armistice_warhead"), overwriteExisting: true);
    _WarheadBullet = new AIBulletBank.Entry(baseBullet) {
      Name               = "warhead",
      PlayAudio          = false,
      BulletObject       = warheadProj.gameObject,
      MuzzleFlashEffects = VFX.CreatePoolFromVFXGameObject(Lazy.GunDefaultProjectile(29).hitEffects.overrideMidairDeathVFX),
    };
  }

  private static void SpawnDust(Vector2 where)
    { SpawnManager.SpawnVFX(GameManager.Instance.Dungeon.dungeonDustups.rollLandDustup, where, Lazy.RandomEulerZ()); }

  private static IEnumerator Lengthen(tk2dSlicedSprite quad, float targetLength, int numFrames)
  {
    float scaleFactor = C.PIXELS_PER_TILE * targetLength / numFrames;
    for (int i = 1 ; i <= numFrames; ++i)
    {
      quad.dimensions = quad.dimensions.WithX(scaleFactor * i);
      quad.UpdateZDepth();
      yield return null;
    }
    quad.gameObject.AddComponent<ReticleRiserEffect>().NumRisers = 3; // restore reticle riser settings
  }

  // Creates a napalm-strike-esque danger zone
  private static GameObject DoomZone(Vector2 start, Vector2 target, float width, float lifetime = -1f, int growthTime = 1, string sprite = null)
  {
    Vector2 delta         = target - start;
    GameObject reticle    = UnityEngine.Object.Instantiate(_NapalmReticle);
    tk2dSlicedSprite quad = reticle.GetComponent<tk2dSlicedSprite>();
      if (sprite != null)
        quad.SetSprite(VFX.Collection, VFX.Collection.GetSpriteIdByName(sprite));
      quad.dimensions              = C.PIXELS_PER_TILE * (new Vector2(delta.magnitude / growthTime, width));
      quad.transform.localRotation = delta.EulerZ();
      quad.transform.position      = start + (0.5f * width * delta.normalized.Rotate(-90f));
      quad.StartCoroutine(Lengthen(quad, delta.magnitude,growthTime));
    if (lifetime > 0)
      reticle.ExpireIn(lifetime);
    return reticle;
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

    private void Start()
    {
      base.aiActor.bulletBank.Bullets.Add(_MainBullet);
      base.aiActor.bulletBank.Bullets.Add(_TurretBullet);
      base.aiActor.bulletBank.Bullets.Add(_WarheadBullet);
      base.aiActor.healthHaver.forcePreventVictoryMusic = true; // prevent default floor theme from playing on death
      base.aiActor.healthHaver.OnPreDeath += OnPreDeath;

      if (_ParticleSystem == null)
        _ParticleSystem = MakeParticleSystem(Color.Lerp(Color.red, Color.white, 0.15f));
      GameObject psObj = UnityEngine.Object.Instantiate(_ParticleSystem);
      psObj.transform.position = base.aiActor.sprite.WorldBottomCenter;
      psObj.transform.parent   = base.gameObject.transform;
      psObj.transform.localRotation = Quaternion.identity;
      this._ps = psObj.GetComponent<ParticleSystem>();
      this._ps.Stop();

      // tk2dBaseSprite sprite = base.aiActor.sprite;
      // sprite.usesOverrideMaterial = true;
      // Material mat = sprite.renderer.material;
      // mat.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
      // mat.SetFloat(CwaffVFX._EmissivePowerId, 100f);
      // mat.SetColor(CwaffVFX._EmissiveColorId, new Color(0f, 227f / 255f, 1f));
      // mat.SetFloat(CwaffVFX._EmissiveColorPowerId, 15.0f);
    }

    private void OnPreDeath(Vector2 _)
    {
      GameManager.Instance.DungeonMusicController.LoopMusic(musicName: "sans", loopPoint: 48800, rewindAmount: 48800);

      Lazy.SpawnChestWithSpecificItem(
        pickup: Lazy.Pickup<GasterBlaster>(),
        position: GameManager.Instance.PrimaryPlayer.CurrentRoom.GetCenteredVisibleClearSpot(2, 2, out bool success),
        overrideChestQuality: ItemQuality.S);
    }

    private Geometry _debugHitbox = null;

    private void LateUpdate() // movement is buggy if we use the regular Update() method
    {
      if (BraveTime.DeltaTime == 0)
        return; // don't do anything if we're paused

      #if DEBUG
      // base.specRigidbody.DrawDebugHitbox();
      // DebugDraw.DrawDebugCircle(base.gameObject, base.transform.position, 0.5f, Color.green.WithAlpha(0.5f));
      // DebugDraw.DrawDebugCircle(GameManager.Instance.gameObject, base.transform.position.GetAbsoluteRoom().area.Center, 0.5f, Color.cyan.WithAlpha(0.5f));
      #endif


      if (this._ps.isPlaying && !base.spriteAnimator.IsPlaying("calm"))
        this._ps.Stop();
      else if (!this._ps.isPlaying && base.spriteAnimator.IsPlaying("calm"))
        this._ps.Play();

      // base.aiActor.PathfindToPosition(GameManager.Instance.PrimaryPlayer.specRigidbody.UnitCenter); // drift around
      // if (Lazy.CoinFlip())
      //   SpawnDust(base.specRigidbody.UnitCenter + Lazy.RandomVector(UnityEngine.Random.Range(0.3f,1.25f))); // spawn dust particles
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

    // public override void EndIntro()
    //   { base.aiActor.GetComponent<BossBehavior>().FinishedIntro(); }
  }
}
