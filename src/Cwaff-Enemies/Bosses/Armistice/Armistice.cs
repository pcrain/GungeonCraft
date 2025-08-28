namespace CwaffingTheGungy;

public partial class ArmisticeBoss : AIActor
{
  public  const  string BOSS_GUID              = "Armistice";
  private const  string BOSS_NAME              = "Armistice";
  private const  string SUBTITLE               = "";
  private const  string SPRITE_PATH            = $"{C.MOD_INT_NAME}/Resources/Bosses/armistice";

  private static GameObject _NapalmReticle      = null;
  private static AIBulletBank.Entry _BoneBullet = null;

  private const  int _SANS_HP = 60;

  public static PrototypeDungeonRoom ArmisticeBossRoom = null;

  public static void Init()
  {
    BuildABoss bb = BuildABoss.LetsMakeABoss<BossBehavior>(bossname: BOSS_NAME, guid: BOSS_GUID, defaultSprite: $"{SPRITE_PATH}/armistice_idle_001",
      hitboxSize: new IntVector2(8, 9), subtitle: SUBTITLE, bossCardPath: $"{C.MOD_INT_NAME}/Resources/armistice_bosscard.png"); // Create our build-a-boss
    bb.SetStats(health: _SANS_HP, weight: 200f, speed: 0.4f, collisionDamage: 0f, hitReactChance: 0.05f, collisionKnockbackStrength: 0f,
      healthIsNumberOfHits: true, invulnerabilityPeriod: 1.0f, shareCooldowns: false); // Set our stats
    bb.InitSpritesFromResourcePath(spritePath: SPRITE_PATH);                   // Set up our animations
      bb.AdjustAnimation(name: "idle",         fps:    8f, loop: true);        // Adjust some specific animations as needed
      // bb.AdjustAnimation(name: "idle_glance",  fps:    8f, loop: true);
      // bb.AdjustAnimation(name: "idle_empty",   fps:    8f, loop: true);
      // bb.AdjustAnimation(name: "shrug",        fps:    8f, loop: true);
      // bb.AdjustAnimation(name: "shrug_calm",   fps:    8f, loop: true);
      // bb.AdjustAnimation(name: "shrug_glance", fps:    8f, loop: true);
      // bb.AdjustAnimation(name: "idle_cloak",   fps:   12f, loop: true);
      // bb.AdjustAnimation(name: "decloak",      fps:    6f, loop: false);
      // bb.AdjustAnimation(name: "teleport_in",  fps:   60f, loop: false);
      // bb.AdjustAnimation(name: "teleport_out", fps:   60f, loop: false);
      bb.SetIntroAnimations(introAnim: "idle", preIntroAnim: "idle"); // Set up our intro animations (TODO: pre-intro not working???)
      // bb.SetIntroAnimations(introAnim: "decloak", preIntroAnim: "idle_cloak"); // Set up our intro animations (TODO: pre-intro not working???)
    bb.SetDefaultColliders(width: 15, height: 30, xoff: 24, yoff: 2);          // Set our default pixel colliders
    bb.AddCustomIntro<ArmisticeIntro>();                                       // Add custom animation to the generic intro doer
    // bb.MakeInteractible<ArmisticeNPC>(preFight: true, postFight: true) ;       // Add some pre-fight and post-fight dialogue
    bb.TargetPlayer();                                                         // Set up the boss's targeting scripts
    bb.AddCustomMusic(name: "collapse", loopAt: 320576, rewind: 225251);       // Add custom music for our boss
    // bb.AddNamedVFX(pool: VFX.vfxpool["Tornado"], name: "mytornado");           // Add some named vfx pools to our bank of VFX
    // bb.CreateTeleportAttack<CustomTeleportBehavior>(                           // Add some attacks
    //   goneTime: 0.25f, outAnim: "teleport_out", inAnim: "teleport_in", cooldown: 4.26f, attackCooldown: 0.15f, probability: 3f);
    // bb.CreateBulletAttack<CrossBulletsScript>    (fireAnim: "idle",       cooldown: 2.0f, attackCooldown: 2.0f);
    // bb.CreateBulletAttack<ClocksTickingScript>   (fireAnim: "idle",       cooldown: 2.0f, attackCooldown: 2.0f);
    // bb.CreateBulletAttack<WalledInScript>   (fireAnim: "idle",       cooldown: 2.0f, attackCooldown: 2.0f);
    // bb.CreateBulletAttack<BoneTunnelScript>   (fireAnim: "idle",       cooldown: 2.0f, attackCooldown: 2.0f);
    // bb.CreateBulletAttack<DanceMonkeyScript>   (fireAnim: "idle",       cooldown: 2.0f, attackCooldown: 2.0f);
    bb.CreateBulletAttack<PendulumScript>   (fireAnim: "idle",       cooldown: 2.0f, attackCooldown: 2.0f);
    bb.AddBossToGameEnemies(name: $"{C.MOD_PREFIX}:armisticeboss");               // Add our boss to the enemy database
    ArmisticeBossRoom = bb.CreateStandaloneBossRoom(width: 40, height: 30, exitOnBottom: true);
    InitPrefabs();                                                             // Do miscellaneous prefab loading
  }

  private static void InitPrefabs()
  {
    // Targeting reticle
    _NapalmReticle = ResourceManager.LoadAssetBundle("shared_auto_002").LoadAsset<GameObject>("NapalmStrikeReticle").ClonePrefab();
      _NapalmReticle.GetComponent<tk2dSlicedSprite>().SetSprite(VFX.Collection, VFX.Collection.GetSpriteIdByName("reticle_white"));
      UnityEngine.Object.Destroy(_NapalmReticle.GetComponent<ReticleRiserEffect>());  // delete risers for use with DoomZoneGrowth component later
    // Bone bullet
    _BoneBullet = new AIBulletBank.Entry(EnemyDatabase.GetOrLoadByGuid(Enemies.Chancebulon).bulletBank.GetBullet("reversible")) {
      Name               = "getboned",
      PlayAudio          = false,
      // BulletObject       = boneBulletProjectile.gameObject.ClonePrefab(),
      MuzzleFlashEffects = VFX.CreatePoolFromVFXGameObject(Lazy.GunDefaultProjectile(29).hitEffects.overrideMidairDeathVFX),
      // MuzzleFlashEffects = null,
    };
    _BoneBullet.MuzzleFlashEffects.type = VFXPoolType.None;
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
    private HeatIndicatorController aura = null;

    private void Start()
    {
      this.aiActor.bulletBank.Bullets.Add(_BoneBullet);
      base.aiActor.healthHaver.forcePreventVictoryMusic = true; // prevent default floor theme from playing on death
      base.aiActor.healthHaver.OnPreDeath += OnPreDeath;

      // base.aiActor.sprite.SetGlowiness(30f, glowColor: Color.cyan);
      tk2dBaseSprite sprite = base.aiActor.sprite;
      sprite.usesOverrideMaterial = true;
      Material mat = sprite.renderer.material;
      mat.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
      mat.SetFloat(CwaffVFX._EmissivePowerId, 100f);
      mat.SetColor(CwaffVFX._EmissiveColorId, new Color(0f, 227f / 255f, 1f));
      mat.SetFloat(CwaffVFX._EmissiveColorPowerId, 15.0f);
    }

    private void OnPreDeath(Vector2 _)
    {
      UpdateSpriteIfNecessary();
      GameManager.Instance.DungeonMusicController.LoopMusic(musicName: "sans", loopPoint: 48800, rewindAmount: 48800);
      if (aura && aura.gameObject)
        UnityEngine.Object.Destroy(aura.gameObject);
      aura = null;

      Lazy.SpawnChestWithSpecificItem(
        pickup: Lazy.Pickup<GasterBlaster>(),
        position: GameManager.Instance.PrimaryPlayer.CurrentRoom.GetCenteredVisibleClearSpot(2, 2, out bool success),
        overrideChestQuality: ItemQuality.S);
    }

    private void LateUpdate() // movement is buggy if we use the regular Update() method
    {
      if (BraveTime.DeltaTime == 0)
        return; // don't do anything if we're paused

      UpdateSpriteIfNecessary();

      float yOff = Mathf.CeilToInt(4f*Mathf.Sin(4f*BraveTime.ScaledTimeSinceStartup)) / C.PIXELS_PER_TILE;
      base.sprite.transform.localPosition += Vector3.zero.WithY(yOff);
      base.sprite.transform.position = base.sprite.transform.position.Quantize(C.PIXEL_SIZE);
      // base.aiActor.PathfindToPosition(GameManager.Instance.PrimaryPlayer.specRigidbody.UnitCenter); // drift around
      // if (Lazy.CoinFlip())
      //   SpawnDust(base.specRigidbody.UnitCenter + Lazy.RandomVector(UnityEngine.Random.Range(0.3f,1.25f))); // spawn dust particles
    }

    private void UpdateSpriteIfNecessary()
    {
      if (!base.sprite || !base.specRigidbody || GameManager.Instance.BestActivePlayer is not PlayerController pc)
        return;
      // base.sprite.FlipX  = overrideFlip ?? (pc.CenterPosition.x < base.specRigidbody.UnitBottomCenter.x);
      Vector3 spriteSize = base.sprite.GetUntrimmedBounds().size;
      Vector2 offset     = new Vector2(spriteSize.x / (base.sprite.FlipX ? 2f : -2f), 0f);
      base.sprite.transform.localPosition = (base.specRigidbody.UnitBottomCenter.Quantize(C.PIXEL_SIZE) + offset).ToVector3ZisY(0f);
      base.sprite.UpdateZDepth();
      if (aura != null)
        aura.transform.localPosition = new Vector3(-offset.x, spriteSize.y / 2, 0);
    }

    public void FinishedIntro()
    {
      if (aura != null)
        return; // fix vanilla bug where SpecificIntroDoer.EndIntro() is called twice
      aura = ((GameObject)UnityEngine.Object.Instantiate(ResourceCache.Acquire("Global VFX/HeatIndicator"), base.aiActor.CenterPosition.ToVector3ZisY(), Quaternion.identity, base.aiActor.sprite.transform)).GetComponent<HeatIndicatorController>();
        aura.CurrentColor  = Color.white;
        aura.IsFire        = true;
        aura.CurrentRadius = 2f; // activate aura (from basegame AuraOnReloadModifier)
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
      { base.aiActor.GetComponent<BossBehavior>().FinishedIntro(); }
  }
}
