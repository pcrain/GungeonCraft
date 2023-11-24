namespace CwaffingTheGungy;

public partial class SansBoss : AIActor
{
  public  const  string BOSS_GUID              = "Sans Boss";
  private const  string BOSS_NAME              = "Sans Gundertale";
  private const  string SUBTITLE               = "Introducing...";
  private const  string SPRITE_PATH            = "CwaffingTheGungy/Resources/sans";

  private static GameObject _NapalmReticle      = null;
  private static AIBulletBank.Entry _BoneBullet = null;

  private const  int _SANS_HP = 60;

  public static PrototypeDungeonRoom SansBossRoom = null;

  public static void Init()
  {
    BuildABoss bb = BuildABoss.LetsMakeABoss<BossBehavior>(bossname: BOSS_NAME, guid: BOSS_GUID, defaultSprite: $"{SPRITE_PATH}/sans_idle_1",
      hitboxSize: new IntVector2(8, 9), subtitle: SUBTITLE, bossCardPath: $"{SPRITE_PATH}_bosscard.png"); // Create our build-a-boss
    bb.SetStats(health: C.DEBUG_BUILD ? 1 : _SANS_HP, weight: 200f, speed: 0.4f, collisionDamage: 0f, hitReactChance: 0.05f, collisionKnockbackStrength: 0f,
    // bb.SetStats(health: _SANS_HP, weight: 200f, speed: 0.4f, collisionDamage: 0f, hitReactChance: 0.05f, collisionKnockbackStrength: 0f,
      healthIsNumberOfHits: true, invulnerabilityPeriod: 1.0f);                // Set our stats
    bb.InitSpritesFromResourcePath(spritePath: SPRITE_PATH);                   // Set up our animations
      bb.AdjustAnimation(name: "idle",         fps:    8f, loop: true);        // Adjust some specific animations as needed
      bb.AdjustAnimation(name: "idle_glance",  fps:    8f, loop: true);
      bb.AdjustAnimation(name: "idle_empty",   fps:    8f, loop: true);
      bb.AdjustAnimation(name: "shrug",        fps:    8f, loop: true);
      bb.AdjustAnimation(name: "shrug_calm",   fps:    8f, loop: true);
      bb.AdjustAnimation(name: "shrug_glance", fps:    8f, loop: true);
      bb.AdjustAnimation(name: "idle_cloak",   fps:   12f, loop: true);
      bb.AdjustAnimation(name: "decloak",      fps:    6f, loop: false);
      bb.AdjustAnimation(name: "teleport_in",  fps:   60f, loop: false);
      bb.AdjustAnimation(name: "teleport_out", fps:   60f, loop: false);
      bb.SetIntroAnimations(introAnim: "idle", preIntroAnim: "idle"); // Set up our intro animations (TODO: pre-intro not working???)
      // bb.SetIntroAnimations(introAnim: "decloak", preIntroAnim: "idle_cloak"); // Set up our intro animations (TODO: pre-intro not working???)
    bb.SetDefaultColliders(width: 15, height: 30, xoff: 24, yoff: 2);          // Set our default pixel colliders
    bb.AddCustomIntro<SansIntro>();                                            // Add custom animation to the generic intro doer
    bb.MakeInteractible<SansNPC>(preFight: true, postFight: true) ;            // Add some pre-fight and post-fight dialogue
    bb.TargetPlayer();                                                         // Set up the boss's targeting scripts
    bb.AddCustomMusic(name: "electromegalo", loopAt: 152512, rewind: 137141);  // Add custom music for our boss
    bb.AddNamedVFX(pool: VFX.vfxpool["Tornado"], name: "mytornado");           // Add some named vfx pools to our bank of VFX
    bb.CreateTeleportAttack<CustomTeleportBehavior>(                           // Add some attacks
      goneTime: 0.25f, outAnim: "teleport_out", inAnim: "teleport_in", cooldown: 0.26f, attackCooldown: 0.15f, probability: 3f);
    bb.CreateBulletAttack<CeilingBulletsScript>    (fireAnim: "laugh",       cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<OrbitBulletScript>       (fireAnim: "throw_up",    cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<HesitantBulletWallScript>(fireAnim: "throw_down",  cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<SquareBulletScript>      (fireAnim: "throw_left",  cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<ChainBulletScript>       (fireAnim: "throw_right", cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<WallSlamScript>          (fireAnim: "laugh",       cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<SineWaveScript>          (fireAnim: "throw_right", cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<OrangeAndBlueScript>     (fireAnim: "throw_right", cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<WiggleWaveScript>        (fireAnim: "throw_right", cooldown: 0.25f, attackCooldown: 0.15f);
    bb.AddBossToGameEnemies(name: "cg:sansboss");                              // Add our boss to the enemy database
    // bb.AddBossToFloorPool(floors: Floors.CASTLEGEON, weight: 9999f);           // Add our boss to the first floor's boss pool
    // bb.AddBossToFloorPool(floors: Floors.MINEGEON, weight: 1f);                // Add our boss to the first floor's boss pool
    SansBossRoom = bb.CreateStandaloneBossRoom();
    InitPrefabs();                                                             // Do miscellaneous prefab loading
  }

  private static void InitPrefabs()
  {
    // Targeting reticle
    _NapalmReticle = ResourceManager.LoadAssetBundle("shared_auto_002").LoadAsset<GameObject>("NapalmStrikeReticle").ClonePrefab();
      _NapalmReticle.GetComponent<tk2dSlicedSprite>().SetSprite(VFX.SpriteCollection, VFX.sprites["reticle_white"]);
      UnityEngine.Object.Destroy(_NapalmReticle.GetComponent<ReticleRiserEffect>());  // delete risers for use with DoomZoneGrowth component later
    // Bone bullet
    Projectile boneBulletProjectile = Items.HegemonyRifle.CloneProjectile();
      // boneBulletProjectile.BulletScriptSettings.preventPooling = true; // prevents shenanigans with OrangeAndBlue Bullet script causing permanent collision skips
    _BoneBullet = new AIBulletBank.Entry(EnemyDatabase.GetOrLoadByGuid("1bc2a07ef87741be90c37096910843ab").bulletBank.GetBullet("reversible")) {
      Name               = "getboned",
      PlayAudio          = false,
      BulletObject       = boneBulletProjectile.gameObject.ClonePrefab(),
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
        quad.SetSprite(VFX.SpriteCollection, VFX.sprites[sprite]);
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
      base.aiActor.healthHaver.OnPreDeath += (_) => {
        FlipSpriteIfNecessary(overrideFlip: false);
        GameManager.Instance.DungeonMusicController.LoopMusic(musicName: "sans", loopPoint: 48800, rewindAmount: 48800);
        UnityEngine.Object.Destroy(aura.gameObject);
        aura = null;

        bool success;
        Lazy.SpawnChestWithSpecificItem(
          pickup: ItemHelper.Get((Items)GasterBlaster.ID),
          position: GameManager.Instance.PrimaryPlayer.CurrentRoom.GetCenteredVisibleClearSpot(2, 2, out success),
          overrideChestQuality: ItemQuality.S);
        // GameManager.Instance.RewardManager.SpawnTotallyRandomChest(GameManager.Instance.PrimaryPlayer.CurrentRoom.GetRandomVisibleClearSpot(1, 1)).IsLocked = false;
      };
    }

    private void LateUpdate() // movement is buggy if we use the regular Update() method
    {
      if (BraveTime.DeltaTime == 0)
        return; // don't do anything if we're paused

      FlipSpriteIfNecessary();
      if (aura == null)
        return; // don't do anything else if we're pre-intro or post-fight

      base.sprite.transform.localPosition += Vector3.zero.WithY(Mathf.CeilToInt(4f*Mathf.Sin(4f*BraveTime.ScaledTimeSinceStartup))/C.PIXELS_PER_TILE);
      base.aiActor.PathfindToPosition(GameManager.Instance.PrimaryPlayer.specRigidbody.UnitCenter); // drift around
      if (Lazy.CoinFlip())
        SpawnDust(base.specRigidbody.UnitCenter + Lazy.RandomVector(UnityEngine.Random.Range(0.3f,1.25f))); // spawn dust particles
    }

    private void FlipSpriteIfNecessary(bool? overrideFlip = null)
    {
      base.sprite.FlipX  = overrideFlip ?? (GameManager.Instance.BestActivePlayer.sprite.WorldBottomCenter.x < base.specRigidbody.UnitBottomCenter.x);
      Vector3 spriteSize = base.sprite.GetUntrimmedBounds().size;
      Vector3 offset     = Vector3.zero.WithX(spriteSize.x / (base.sprite.FlipX ? 2f : -2f));
      base.sprite.transform.localPosition = (Vector3)base.specRigidbody.UnitBottomCenter/*.RoundToInt()*/ + offset;
      if (aura != null)
        aura.transform.localPosition = new Vector3(0,spriteSize.y / 2,0) - offset;
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

  private class SansIntro : SpecificIntroDoer
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
    }

    public override void EndIntro()
      { base.aiActor.GetComponent<BossBehavior>().FinishedIntro(); }
  }
}
