namespace CwaffingTheGungy;

public partial class SansBoss : AIActor
{
  public  const  string BOSS_GUID              = "Sans Boss";
  private const  string BOSS_NAME              = "Sans Gundertale";
  private const  string SUBTITLE               = "Introducing...";
  private const  string SPRITE_PATH            = $"{C.MOD_INT_NAME}/Resources/Bosses/sans";

  private static AIBulletBank.Entry _BoneBullet = null;

  private const  int _SANS_HP = 60;

  public static PrototypeDungeonRoom SansBossRoom = null;

  public static void Init()
  {
    BuildABoss bb = BuildABoss.LetsMakeABoss<BossBehavior>(bossname: BOSS_NAME, guid: BOSS_GUID, defaultSprite: $"{SPRITE_PATH}/sans_idle_1",
      hitboxSize: new IntVector2(8, 9), subtitle: SUBTITLE, bossCardPath: $"{C.MOD_INT_NAME}/Resources/sans_bosscard.png"); // Create our build-a-boss
    bb.SetStats(health: _SANS_HP, weight: 200f, speed: 0.4f, collisionDamage: 0f, hitReactChance: 0.05f, collisionKnockbackStrength: 0f,
      healthIsNumberOfHits: true, invulnerabilityPeriod: 1.0f, spriteAnchor: Anchor.LowerCenter);                // Set our stats
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
    bb.SetDefaultColliders(width: 15, height: 30, xoff: -7, yoff: 2);          // Set our default pixel colliders
    bb.AddCustomIntro<SansIntro>();                                            // Add custom animation to the generic intro doer
    bb.MakeInteractible<SansNPC>(preFight: true, postFight: true) ;            // Add some pre-fight and post-fight dialogue
    bb.TargetPlayer();                                                         // Set up the boss's targeting scripts
    bb.AddCustomMusic(name: "electromegalo", loopAt: 152512, rewind: 137141);  // Add custom music for our boss
    // bb.AddNamedVFX(pool: VFX.vfxpool["Tornado"], name: "mytornado");           // Add some named vfx pools to our bank of VFX
    bb.CreateTeleportAttack<CustomTeleportBehavior>(                           // Add some attacks
      goneTime: 0.25f, outAnim: "teleport_out", inAnim: "teleport_in", cooldown: 0.26f, attackCooldown: 0.15f, probability: 3f);
    bb.CreateBulletAttack<CeilingBulletsScript>    (fireAnim: "laugh",       cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<OrbitBulletScript>       (fireAnim: "throw_up",    cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<HesitantBulletWallScript>(fireAnim: "throw_down",  cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<SquareBulletScript>      (fireAnim: "throw_left",  cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<ChainBulletScript>       (fireAnim: "throw_right", cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<WallSlamScript>          (fireAnim: "laugh",       cooldown: 0.25f, attackCooldown: 0.15f);  //TODO: refactor to use CreateSequentialAttack and CustomTeleportBehavior
    bb.CreateBulletAttack<SineWaveScript>          (fireAnim: "throw_right", cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<OrangeAndBlueScript>     (fireAnim: "throw_right", cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<WiggleWaveScript>        (fireAnim: "throw_right", cooldown: 0.25f, attackCooldown: 0.15f);
    bb.AddBossToGameEnemies(name: $"{C.MOD_PREFIX}:sansboss");                    // Add our boss to the enemy database
    // bb.AddBossToFloorPool(floors: Floors.CASTLEGEON, weight: 9999f);           // Add our boss to the first floor's boss pool
    // bb.AddBossToFloorPool(floors: Floors.MINEGEON, weight: 1f);                // Add our boss to the first floor's boss pool
    SansBossRoom = bb.CreateStandaloneBossRoom(width: 38, height: 27, exitOnBottom: false);
    InitPrefabs();                                                             // Do miscellaneous prefab loading
  }

  private static void InitPrefabs()
  {
    // Bone bullet
    Projectile boneBulletProjectile = Items.HegemonyRifle.CloneProjectile();
      // boneBulletProjectile.BulletScriptSettings.preventPooling = true; // prevents shenanigans with OrangeAndBlue Bullet script causing permanent collision skips
    _BoneBullet = new AIBulletBank.Entry(EnemyDatabase.GetOrLoadByGuid(Enemies.Chancebulon).bulletBank.GetBullet("reversible")) {
      Name               = "getboned",
      PlayAudio          = false,
      BulletObject       = boneBulletProjectile.gameObject.ClonePrefab(),
      MuzzleFlashEffects = VFX.CreatePoolFromVFXGameObject(Lazy.GunDefaultProjectile(29).hitEffects.overrideMidairDeathVFX),
    };
  }

  private static void SpawnDust(Vector2 where)
    { SpawnManager.SpawnVFX(GameManager.Instance.Dungeon.dungeonDustups.rollLandDustup, where, Lazy.RandomEulerZ()); }

  private class BossBehavior : BraveBehaviour
  {
    private HeatIndicatorController aura = null;

    private void Start()
    {
      this.aiActor.bulletBank.Bullets.Add(_BoneBullet);
      base.aiActor.healthHaver.forcePreventVictoryMusic = true; // prevent default floor theme from playing on death
      base.aiActor.healthHaver.OnPreDeath += OnPreDeath;
    }

    private void OnPreDeath(Vector2 _)
    {
      FlipSpriteIfNecessary(overrideFlip: false);
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
      if (!base.sprite || !base.specRigidbody || GameManager.Instance.BestActivePlayer is not PlayerController pc)
        return;
      base.sprite.FlipX  = overrideFlip ?? (pc.CenterPosition.x < base.specRigidbody.UnitBottomCenter.x);
      base.sprite.transform.localPosition = (base.specRigidbody.UnitBottomCenter.Quantize(C.PIXEL_SIZE)).ToVector3ZisY(0f);
      base.sprite.UpdateZDepth();
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
