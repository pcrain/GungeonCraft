namespace CwaffingTheGungy;

public enum Floors // Matches GlobalDungeonData.ValidTilesets
{
  GUNGEON       = 0x0001,
  CASTLEGEON    = 0x0002,
  SEWERGEON     = 0x0004,
  CATHEDRALGEON = 0x0008,
  MINEGEON      = 0x0010,
  CATACOMBGEON  = 0x0020,
  FORGEGEON     = 0x0040,
  HELLGEON      = 0x0080,
  SPACEGEON     = 0x0100,
  PHOBOSGEON    = 0x0200,
  WESTGEON      = 0x0400,
  OFFICEGEON    = 0x0800,
  BELLYGEON     = 0x1000,
  JUNGLEGEON    = 0x2000,
  FINALGEON     = 0x4000,
  RATGEON       = 0x8000,
}

// Helper class for loading runtime boss information
public class BossController : DungeonPlaceableBehaviour, IPlaceConfigurable
{
  public string enemyGuid = null;
  public string musicId = null;
  public int loopPoint  = -1;
  public int loopRewind = -1;

  private bool bossFightStarted = false;

  private BossController() {} // default constructor is private

  internal static BossController NewPrefab(string guid) // should not be instantiated outside this class
    {
      return new GameObject("BossController").RegisterPrefab().AddComponent<BossController>(
        new BossController() {
          enemyGuid = guid,
        }
      );
    }

  public void ConfigureOnPlacement(RoomHandler room)
  {
    AIActor theBoss = null;
    foreach (AIActor enemy in room.SafeGetEnemiesInRoom())
      if (enemy.EnemyGuid == this.enemyGuid)
      {
        theBoss = enemy;
        break;
      }
    if (theBoss == null)
    {
      ETGModConsole.Log($"Something went horrendously wrong setting up the Boss o.o");
      return;
    }
    SetUpBossRoom(theBoss);
    room.Entered += (_) => {
      SetUpBossFight(theBoss);
    };
  }

  public void SetMusic(string musicName, int loopPoint = -1, int rewindAmount = -1)
  {
    this.musicId    = musicName;
    this.loopPoint  = loopPoint;
    this.loopRewind = rewindAmount;
  }

  private void RegisterAnyInteractables(AIActor enemy)
  {
    UnityEngine.Component[] componentsInChildren = enemy.GetComponentsInChildren(typeof(IPlayerInteractable));
    for (int i = 0; i < componentsInChildren.Length; i++)
    {
        if (componentsInChildren[i] is IPlayerInteractable)
          enemy.transform.position.GetAbsoluteRoom().RegisterInteractable(componentsInChildren[i] as IPlayerInteractable);
    }
  }

  private void SetUpBossRoom(AIActor enemy)
  {
    // ETGModConsole.Log($"Setting up Boss Room");
    RegisterAnyInteractables(enemy);
    GenericIntroDoer gid = enemy.GetComponent<GenericIntroDoer>();
    if (!string.IsNullOrEmpty(gid.preIntroAnim))
      enemy.aiAnimator.PlayUntilCancelled(gid.preIntroAnim); //HACK: forcibly play the pre-intro animation before room entry
    enemy.healthHaver.ManualDeathHandling = true; // make sure we manually handle our death as necessary
    if (gid.triggerType == GenericIntroDoer.TriggerType.BossTriggerZone)
      enemy.healthHaver.IsVulnerable = false;
  }

  private void SetUpBossFight(AIActor enemy)
  {
    // ETGModConsole.Log($"Setting up Boss Fight");
    BossNPC npc = enemy.GetComponent<BossNPC>();
    if (npc != null)
    {
      npc.SetBossController(this);
      enemy.healthHaver.OnPreDeath += (_) => {
        npc.FinishBossFight();
      };
    }
    else
      enemy.healthHaver.OnPreDeath += (_) => {
        enemy.transform.position.GetAbsoluteRoom().UnsealRoom();
      };
    if (enemy.GetComponent<GenericIntroDoer>().triggerType == GenericIntroDoer.TriggerType.PlayerEnteredRoom)
      StartBossFight(enemy);
  }

  public void StartBossFight(AIActor enemy)
  {
    if (bossFightStarted)
      return;
    bossFightStarted = true;
    // ETGModConsole.Log($"Starting Boss Fight");
    enemy.aiAnimator.EndAnimation();  // make sure our base idle animation plays after our preIntro animation finishes
    BossNPC npc = enemy.GetComponent<BossNPC>();
    if (npc != null)
    {
      enemy.aiAnimator.StartCoroutine(RemoveOutlines(enemy)); //HACK: because trying to remove outlines instantaneously doesn't work for some reason
      npc.startedFight = true;
      enemy.gameObject.transform.position.GetAbsoluteRoom().DeregisterInteractable(npc);
    }
    if (this.musicId != null)
      enemy.PlayBossMusic(this.musicId, this.loopPoint, this.loopRewind);
    if (enemy.GetComponent<GenericIntroDoer>().triggerType == GenericIntroDoer.TriggerType.BossTriggerZone)
      enemy.healthHaver.IsVulnerable = true;
    enemy.transform.position.GetAbsoluteRoom().SealRoom();
  }

  private IEnumerator RemoveOutlines(AIActor enemy)
  {
    yield return new WaitForSecondsRealtime(1.0f/60.0f);
    SpriteOutlineManager.RemoveOutlineFromSprite(enemy.sprite);
  }

}

// Class for adding a custom dialogue with a boss leading up to a boss fight
public class BossNPC : FancyNPC
{
  public bool hasPreFightDialogue = false;
  public bool hasPostFightDialogue = false;
  public bool startedFight = false;
  public bool finishedFight = false;

  private BossController bossController = null;

  protected void StartBossFight()
  {
    AIActor enemy = this.gameObject.GetComponent<AIActor>();

    if (this.bossController != null)
      this.bossController.StartBossFight(enemy);
    else
      ETGModConsole.Log($"BOSS CONTROLLER SHOULD NEVER BE NULL");
    this.gameObject.GetComponent<GenericIntroDoer>().TriggerSequence(GameManager.Instance.BestActivePlayer);

    this.startedFight = true;
  }

  public void SetBossController(BossController bc)
  {
    this.bossController ??= bc;
  }

  public void FinishBossFight()
  {
    StartCoroutine(FinishBossFight_CR());

    IEnumerator FinishBossFight_CR()
    {
      AIActor enemy = this.gameObject.GetComponent<AIActor>();
      enemy.transform.position.GetAbsoluteRoom().RegisterInteractable(this);

      IEnumerator script = DefeatedScript();
      while(script.MoveNext())
        yield return script.Current;

      // if (hasPreFightDialogue)
        enemy.transform.position.GetAbsoluteRoom().UnsealRoom();
      if (!hasPostFightDialogue)
        enemy.healthHaver.DeathAnimationComplete(null, null);

      this.finishedFight = true;
    }
  }

  protected override IEnumerator NPCTalkingScript()
  {
    IEnumerator script = (startedFight ? PostFightScript() : PreFightScript());
    while(script.MoveNext())
      yield return script.Current;
  }

  protected virtual IEnumerator PreFightScript()
  {
    GameManager.Instance.MainCameraController.OverridePosition = this.sprite.transform.localPosition;
    GameManager.Instance.MainCameraController.SetManualControl(true, true);
    yield return Prompt("fight this guy", "don't fight this guy");
    if (PromptResult() == 0) // accept
      StartBossFight();
  }
  protected virtual IEnumerator DefeatedScript() // called by BossController()
    { yield break; }
  protected virtual IEnumerator PostFightScript()
    { yield break; }
}

// The big boi itself
public class BuildABoss
{
  public   GameObject     prefab         { get; private set; } = null;
  public   string         guid           { get; private set; } = null;
  internal BossController bossController { get; private set; } = null;
  private  GameObject     defaultGunAttachPoint  = null;
  private  BraveBehaviour enemyBehavior          = null;
  internal Anchor         spriteAnchor           = default;

  // Private constructor
  private BuildABoss() {}

  public static BuildABoss LetsMakeABoss<T>(string bossname, string guid, string defaultSprite, IntVector2 hitboxSize, string subtitle, string bossCardPath)
    where T : BraveBehaviour
  {
    // Do some basic boss prefab / guid setup
    BuildABoss bb = new BuildABoss();
    bb.guid = guid;
    bb.prefab = BossBuilder.BuildPrefab(bossname, bb.guid, defaultSprite,
      IntVector2.Zero, hitboxSize, false, true);

    // Add sane default behavior
    bb.enemyBehavior  = BH.AddSaneDefaultBossBehavior<T>(bb.prefab,bossname,subtitle,bossCardPath);

    // Add a BossController
    bb.bossController = BossController.NewPrefab(guid);

    // Set up default colliders from the default sprite
    var sprite = bb.prefab.GetComponent<HealthHaver>().GetAnySprite();
    Vector2 spriteSize = (16f * sprite.GetBounds().size);
    bb.SetDefaultColliders((int)spriteSize.x,(int)spriteSize.y,0,0);

    // Set up a default shoot point from the center of our sprite
    GameObject shootpoint = new GameObject("attach");
      shootpoint.transform.parent = bb.enemyBehavior.specRigidbody.transform;
      shootpoint.transform.position = bb.enemyBehavior.specRigidbody.UnitCenter;
      shootpoint.transform.localPosition = bb.enemyBehavior.specRigidbody.UnitCenter;
      shootpoint.transform.localScale = new Vector3(-1f, 1f, 1f);
    bb.defaultGunAttachPoint = bb.enemyBehavior.transform.Find("attach").gameObject;

    // Set up a default shadow so teleportation doesn't throw exceptions
    if (bb.enemyBehavior.aiActor.ShadowObject == null)
      bb.enemyBehavior.aiActor.ShadowObject = ((GameObject)UnityEngine.Object.Instantiate(ResourceCache.Acquire("DefaultShadowSprite"))).RegisterPrefab();

    return bb;
  }

  public void SetStats(float? health = null, float? weight = null, float? speed = null, float? collisionDamage = null,
    float? collisionKnockbackStrength = null, float? hitReactChance = null, bool? healthIsNumberOfHits = null,
    float? invulnerabilityPeriod = null, bool? shareCooldowns = null, Anchor? spriteAnchor = null)
  {
    if (health.HasValue)
    {
      this.enemyBehavior.aiActor.healthHaver.SetHealthMaximum(health.Value);
      this.enemyBehavior.aiActor.healthHaver.FullHeal();
      // this.enemyBehavior.aiActor.healthHaver.ForceSetCurrentHealth(health.Value);
    }
    if (weight.HasValue)
      this.enemyBehavior.aiActor.knockbackDoer.weight = weight.Value;
    if (speed.HasValue)
      this.enemyBehavior.aiActor.MovementSpeed = speed.Value;
    if (collisionDamage.HasValue)
      this.enemyBehavior.aiActor.CollisionDamage = collisionDamage.Value;
    if (collisionKnockbackStrength.HasValue)
      this.enemyBehavior.aiActor.CollisionKnockbackStrength = collisionKnockbackStrength.Value;
    if (hitReactChance.HasValue)
      this.enemyBehavior.aiActor.aiAnimator.HitReactChance = hitReactChance.Value;
    if (healthIsNumberOfHits.HasValue)
      this.enemyBehavior.aiActor.healthHaver.healthIsNumberOfHits = healthIsNumberOfHits.Value;
    if (invulnerabilityPeriod.HasValue)
    {
      this.enemyBehavior.aiActor.healthHaver.invulnerabilityPeriod = invulnerabilityPeriod.Value;
      this.enemyBehavior.aiActor.healthHaver.usesInvulnerabilityPeriod = invulnerabilityPeriod.Value > 0.0f;
    }
    if (shareCooldowns.HasValue)
      this.enemyBehavior.behaviorSpeculator.AttackBehaviorGroup.ShareCooldowns = shareCooldowns.Value;
    this.spriteAnchor = spriteAnchor ?? Anchor.LowerLeft;
  }

  /// <summary>Adds custom music for a custom boss.</summary>
  /// <param name="name">The name of the music track (WWise event name) to play.</param>
  /// <param name="loopAt">Offset in milliseconds where a song should loop.</param>
  /// <param name="rewind">How many milliseconds a song should rewind after reaching the loop point.</param>
  public void AddCustomMusic(string name, int loopAt = -1, int rewind = -1)
    { this.bossController.SetMusic(name, loopAt, rewind); }

  /// <summary>Adds a new ShootBehavior attack with a custom Brave.BraveBulletScript.Script to a custom boss.</summary>
  /// <typeparam name="T">A Brave Bullet Script or derived class thereof.</typeparam>
  /// <param name="add">Whether to add this to our base attack pool. Set to false for attacks nested in simultaneous / sequential attacks.</param>
  /// <param name="cooldown">Time before THIS behavior may be run again.</param>
  /// <param name="cooldownVariance">Time variance added to the base cooldown.</param>
  /// <param name="attackCooldown">Time before ATTACK behaviors may be run again.</param>
  /// <param name="globalCooldown">Time before ANY behavior may be run again.</param>
  /// <param name="initialCooldown">Time after the enemy becomes active before this attack can be used for the first time.</param>
  /// <param name="initialCooldownVariance">Time variance added to the initial cooldown.</param>
  /// <param name="probability">The probability of using this attack relative to other attacks.</param>
  /// <param name="maxUsages">This attack can only be used this number of times.</param>
  /// <param name="requiresLineOfSight">Require line of sight to target. Expensive! Use for companions.</param>
  /// <param name="minHealth">The minimum amount of health an enemy can have and still use this attack.\n(Raising this means the enemy wont use this attack at low health)</param>
  /// <param name="maxHealth">The maximum amount of health an enemy can have and still use this attack.\n(Lowering this means the enemy wont use this attack until they lose health)</param>
  /// <param name="healthThresholds">The attack can only be used once each time a new health threshold is met</param>
  /// <param name="accumulateHealthThresholds">If true, the attack can build up multiple uses by passing multiple thresholds in quick succession</param>
  /// <param name="minRange">Minimum range</param>
  /// <param name="range">Range</param>
  /// <param name="minWallDist">Minimum distance from a wall</param>
  /// <param name="shootPoint">Object to use for the relative transform of where the attack is fired from.</param>
  /// <param name="fireAnim">Named animation used when the attack is in progress.</param>
  /// <param name="tellAnim">Named animation used when the attack is being forecasted.</param>
  /// <param name="chargeAnim">Named animation used when the attack is being charged.</param>
  /// <param name="finishAnim">Named animation used when the attack is complete.</param>
  /// <param name="lead">(Unknown) How far in front of the target the attack will be aimed / predicted.</param>
  /// <param name="chargeTime">Amount of time the attack takes to charge.</param>
  /// <param name="interruptible">Whether this attack is interruptible.</param>
  /// <param name="clearGoop">Whether this attack clears goop.</param>
  /// <param name="clearRadius">Radius around the bullets for which this attack clears goops.</param>
  /// <param name="vfx">VFX to spawn when using this attack.</param>
  /// <param name="fireVfx">VFX to spawn when firing this attack.</param>
  /// <param name="tellVfx">VFX to spawn when forecasting this attack.</param>
  /// <param name="chargeVfx">VFX to spawn when charging this attack.</param>
  /// <returns>A ShootBehavior with sane defaults initalized according to the parameters</returns>
  public ShootBehavior CreateBulletAttack<T>(
    bool add = true, float cooldown = 0f, float cooldownVariance = 0f, float attackCooldown = 0f, float globalCooldown = 0f,
    float initialCooldown = 0.5f, float initialCooldownVariance = 0f,
    float probability = 1f, int maxUsages = -1, bool requiresLineOfSight = false,
    float minHealth = 0f, float maxHealth = 1f, float[] healthThresholds = null, bool accumulateHealthThresholds = true,
    float minRange = 0f, float range = 0f, float minWallDist = 0f,
    GameObject shootPoint = null, string fireAnim = null, string tellAnim = null,
    string chargeAnim = null, string finishAnim = null, float lead = 0f, float chargeTime = 0f,
    bool interruptible = false, bool clearGoop = false, float clearRadius = 2f,
    string vfx = null, string fireVfx = null, string tellVfx = null, string chargeVfx = null)
    where T : Brave.BulletScript.Script
  {
    return CreateBulletAttack<T, ShootBehavior>(add: add, cooldown: cooldown, cooldownVariance: cooldownVariance,
        attackCooldown: attackCooldown, globalCooldown: globalCooldown, initialCooldown: initialCooldown,
        initialCooldownVariance: initialCooldownVariance, probability: probability, maxUsages: maxUsages,
        requiresLineOfSight: requiresLineOfSight, minHealth: minHealth, maxHealth: maxHealth,
        healthThresholds: healthThresholds, accumulateHealthThresholds: accumulateHealthThresholds,
        minRange: minRange, range: range, minWallDist: minWallDist, shootPoint: shootPoint, fireAnim: fireAnim,
        tellAnim: tellAnim, chargeAnim: chargeAnim, finishAnim: finishAnim, lead: lead, chargeTime: chargeTime,
        interruptible: interruptible, clearGoop: clearGoop, clearRadius: clearRadius, vfx: vfx, fireVfx: fireVfx,
        tellVfx: tellVfx, chargeVfx: chargeVfx);
  }

/// <summary>Adds a new ShootBehavior attack with a custom Brave.BraveBulletScript.Script to a custom boss.</summary>
  /// <typeparam name="T">A Brave Bullet Script or derived class thereof.</typeparam>
  /// <typeparam name="M">A ShootBehavior or derived class thereof.</typeparam>
  /// <param name="add">Whether to add this to our base attack pool. Set to false for attacks nested in simultaneous / sequential attacks.</param>
  /// <param name="cooldown">Time before THIS behavior may be run again.</param>
  /// <param name="cooldownVariance">Time variance added to the base cooldown.</param>
  /// <param name="attackCooldown">Time before ATTACK behaviors may be run again.</param>
  /// <param name="globalCooldown">Time before ANY behavior may be run again.</param>
  /// <param name="initialCooldown">Time after the enemy becomes active before this attack can be used for the first time.</param>
  /// <param name="initialCooldownVariance">Time variance added to the initial cooldown.</param>
  /// <param name="probability">The probability of using this attack relative to other attacks.</param>
  /// <param name="maxUsages">This attack can only be used this number of times.</param>
  /// <param name="requiresLineOfSight">Require line of sight to target. Expensive! Use for companions.</param>
  /// <param name="minHealth">The minimum amount of health an enemy can have and still use this attack.\n(Raising this means the enemy wont use this attack at low health)</param>
  /// <param name="maxHealth">The maximum amount of health an enemy can have and still use this attack.\n(Lowering this means the enemy wont use this attack until they lose health)</param>
  /// <param name="healthThresholds">The attack can only be used once each time a new health threshold is met</param>
  /// <param name="accumulateHealthThresholds">If true, the attack can build up multiple uses by passing multiple thresholds in quick succession</param>
  /// <param name="minRange">Minimum range</param>
  /// <param name="range">Range</param>
  /// <param name="minWallDist">Minimum distance from a wall</param>
  /// <param name="shootPoint">Object to use for the relative transform of where the attack is fired from.</param>
  /// <param name="fireAnim">Named animation used when the attack is in progress.</param>
  /// <param name="tellAnim">Named animation used when the attack is being forecasted.</param>
  /// <param name="chargeAnim">Named animation used when the attack is being charged.</param>
  /// <param name="finishAnim">Named animation used when the attack is complete.</param>
  /// <param name="lead">(Unknown) How far in front of the target the attack will be aimed / predicted.</param>
  /// <param name="chargeTime">Amount of time the attack takes to charge.</param>
  /// <param name="interruptible">Whether this attack is interruptible.</param>
  /// <param name="clearGoop">Whether this attack clears goop.</param>
  /// <param name="clearRadius">Radius around the bullets for which this attack clears goops.</param>
  /// <param name="vfx">VFX to spawn when using this attack.</param>
  /// <param name="fireVfx">VFX to spawn when firing this attack.</param>
  /// <param name="tellVfx">VFX to spawn when forecasting this attack.</param>
  /// <param name="chargeVfx">VFX to spawn when charging this attack.</param>
  /// <returns>A ShootBehavior (of type M) with sane defaults initalized according to the parameters</returns>
  public M CreateBulletAttack<T, M>(
    bool add = true, float cooldown = 0f, float cooldownVariance = 0f, float attackCooldown = 0f, float globalCooldown = 0f,
    float initialCooldown = 0.5f, float initialCooldownVariance = 0f,
    float probability = 1f, int maxUsages = -1, bool requiresLineOfSight = false,
    float minHealth = 0f, float maxHealth = 1f, float[] healthThresholds = null, bool accumulateHealthThresholds = true,
    float minRange = 0f, float range = 0f, float minWallDist = 0f,
    GameObject shootPoint = null, string fireAnim = null, string tellAnim = null,
    string chargeAnim = null, string finishAnim = null, float lead = 0f, float chargeTime = 0f,
    bool interruptible = false, bool clearGoop = false, float clearRadius = 2f,
    string vfx = null, string fireVfx = null, string tellVfx = null, string chargeVfx = null)
    where T : Brave.BulletScript.Script
    where M : ShootBehavior, new()
  {
    if (shootPoint == null)
      shootPoint = this.defaultGunAttachPoint;
    if (healthThresholds == null)
      healthThresholds = new float[0];
    bool anyVFx = (!(
      String.IsNullOrEmpty(vfx)     &&
      String.IsNullOrEmpty(fireVfx) &&
      String.IsNullOrEmpty(tellVfx) &&
      String.IsNullOrEmpty(chargeVfx)));
    M bangbang = new M {
        Cooldown                   = cooldown,
        CooldownVariance           = cooldownVariance,
        AttackCooldown             = attackCooldown,
        GlobalCooldown             = globalCooldown,
        InitialCooldown            = initialCooldown,
        InitialCooldownVariance    = initialCooldownVariance,
        MaxUsages                  = maxUsages,
        RequiresLineOfSight        = requiresLineOfSight,
        MinHealthThreshold         = minHealth,
        MaxHealthThreshold         = maxHealth,
        HealthThresholds           = healthThresholds,
        AccumulateHealthThresholds = accumulateHealthThresholds,
        MinRange                   = minRange,
        Range                      = range,
        MinWallDistance            = minWallDist,
        targetAreaStyle            = null,

        ShootPoint                 = shootPoint,
        BulletScript               = new CustomBulletScriptSelector(typeof(T)),
        LeadAmount                 = lead,
        ChargeTime                 = chargeTime,
        FireAnimation              = fireAnim,
        TellAnimation              = tellAnim,
        ChargeAnimation            = chargeAnim,
        PostFireAnimation          = finishAnim,
        StopDuring                 = ShootBehavior.StopType.Attack,
        Uninterruptible            = !interruptible,
        ClearGoop                  = clearGoop,
        ClearGoopRadius            = clearRadius,
        UseVfx                     = anyVFx,
        Vfx                        = vfx,
        FireVfx                    = fireVfx,
        TellVfx                    = tellVfx,
        ChargeVfx                  = chargeVfx,
      };
    if (add)
    {
      AttackBehaviorGroup.AttackGroupItem theAttack = new AttackBehaviorGroup.AttackGroupItem()
      {
        Probability = probability,
        NickName    = typeof(T).AssemblyQualifiedName,
        Behavior    = bangbang
      };
      this.prefab.GetComponent<BehaviorSpeculator>().AttackBehaviorGroup.AttackBehaviors.Add(theAttack);
      // this.prefab.GetComponent<BehaviorSpeculator>().AttackBehaviors.Add(bangbang); // TODO: could also just do this
    }
    return bangbang;
  }

  /// <summary>Adds a new TeleportBehavior attack to a custom boss.</summary>
  /// <typeparam name="T">A TeleportBehavior or derived class thereof.</typeparam>
  /// <param name="add">Whether to add this to our base attack pool. Set to false for attacks nested in simultaneous / sequential attacks.</param>
  /// <param name="cooldown">Time before THIS behavior may be run again.</param>
  /// <param name="cooldownVariance">Time variance added to the base cooldown.</param>
  /// <param name="attackCooldown">Time before ATTACK behaviors may be run again.</param>
  /// <param name="globalCooldown">Time before ANY behavior may be run again.</param>
  /// <param name="initialCooldown">Time after the enemy becomes active before this attack can be used for the first time.</param>
  /// <param name="initialCooldownVariance">Time variance added to the initial cooldown.</param>
  /// <param name="probability">The probability of using this attack relative to other attacks.</param>
  /// <param name="maxUsages">This attack can only be used this number of times.</param>
  /// <param name="requiresLineOfSight">Require line of sight to target. Expensive! Use for companions.</param>
  /// <param name="minHealth">The minimum amount of health an enemy can have and still use this attack.\n(Raising this means the enemy wont use this attack at low health)</param>
  /// <param name="maxHealth">The maximum amount of health an enemy can have and still use this attack.\n(Lowering this means the enemy wont use this attack until they lose health)</param>
  /// <param name="healthThresholds">The attack can only be used once each time a new health threshold is met</param>
  /// <param name="accumulateHealthThresholds">If true, the attack can build up multiple uses by passing multiple thresholds in quick succession</param>
  /// <param name="minRange">Minimum range</param>
  /// <param name="range">Range</param>
  /// <param name="minWallDist">Minimum distance from a wall</param>

  /// <param name="vulnerable">Whether we're vulnerable during teleportation.</param>
  /// <param name="avoidWalls">Whether teleportation avoids walls.</param>
  /// <param name="stayOnScreen">If false, we're allowed to teleport off screen</param>
  /// <param name="minDist">Minimum distance from player we must be before teleporting.</param>
  /// <param name="maxDist">Maximum distance from player we must be before teleporting.</param>
  /// <param name="goneTime">Amont of time we're teleporting for.</param>
  /// <param name="onlyIfUnreachable">If true, only teleport if we can't pathfind our way to the player.</param>
  /// <param name="outAnim">Named animation to play when teleporting out.</param>
  /// <param name="inAnim">Named animation to play when teleporting back in. (WARNING: cannot be looped)</param>
  /// <param name="outScript">Bullet scripts to run when initiating teleport. (WARNING: cannot be looped)</param>
  /// <param name="inScript">Bullet scripts to run when finishing teleport.</param>
  /// <param name="roomBounds">Bounding rectangle for the room (if null, use the default bounding rectangle; can lead to glitches).</param>
  /// <returns>A TeleportBehavior with sane defaults initalized according to the parameters</returns>
  public TeleportBehavior CreateTeleportAttack<T>(
    bool add = true, float cooldown = 0f, float cooldownVariance = 0f, float attackCooldown = 0f, float globalCooldown = 0f,
    float initialCooldown = 0f, float initialCooldownVariance = 0f,
    float probability = 1f, int maxUsages = -1, bool requiresLineOfSight = false,
    float minHealth = 0f, float maxHealth = 1f, float[] healthThresholds = null, bool accumulateHealthThresholds = true,
    float minRange = 0f, float range = 0f, float minWallDist = 0f,
    bool vulnerable = false, bool avoidWalls = false, bool stayOnScreen = false, float minDist = 0f, float maxDist = 0f,
    float goneTime = 1f, bool onlyIfUnreachable = false,  string outAnim = null, string inAnim = null,
    Type outScript = null, Type inScript = null, Rect? roomBounds = null, GameObject who = null
    )
    where T : TeleportBehavior, new()
  {
    if (healthThresholds == null)
      healthThresholds = new float[0];
    CustomBulletScriptSelector outScript_ =
      (outScript != null && outScript.IsSubclassOf(typeof(Bullet)))
      ? new CustomBulletScriptSelector(outScript)
      : null;
    CustomBulletScriptSelector inScript_ =
      (inScript != null && inScript.IsSubclassOf(typeof(Bullet)))
      ? new CustomBulletScriptSelector(inScript)
      : null;
    TeleportBehavior blipblip = new T() {
        Cooldown                        = cooldown,
        CooldownVariance                = cooldownVariance,
        AttackCooldown                  = attackCooldown,
        GlobalCooldown                  = globalCooldown,
        InitialCooldown                 = initialCooldown,
        InitialCooldownVariance         = initialCooldownVariance,
        MaxUsages                       = maxUsages,
        RequiresLineOfSight             = requiresLineOfSight,
        MinHealthThreshold              = minHealth,
        MaxHealthThreshold              = maxHealth,
        HealthThresholds                = healthThresholds,
        AccumulateHealthThresholds      = accumulateHealthThresholds,
        MinRange                        = minRange,
        Range                           = range,
        MinWallDistance                 = minWallDist,
        targetAreaStyle                 = null,

        AllowCrossRoomTeleportation     = false,
        ManuallyDefineRoom              = roomBounds.HasValue,
        roomMin                         = roomBounds.HasValue ? roomBounds.Value.min : Vector2.zero,
        roomMax                         = roomBounds.HasValue ? roomBounds.Value.max : Vector2.zero,
        MaxEnemiesInRoom                = 0,
        AttackableDuringAnimation       = vulnerable,
        AvoidWalls                      = avoidWalls,
        StayOnScreen                    = stayOnScreen,
        MinDistanceFromPlayer           = minDist,
        MaxDistanceFromPlayer           = maxDist,
        GoneTime                        = goneTime,
        OnlyTeleportIfPlayerUnreachable = onlyIfUnreachable,
        teleportOutAnim                 = outAnim,
        teleportInAnim                  = inAnim,
        teleportOutBulletScript         = outScript_,
        teleportInBulletScript          = inScript_,
      };
    if (add)
    {
      AttackBehaviorGroup.AttackGroupItem theAttack = new AttackBehaviorGroup.AttackGroupItem()
      {
        Probability = probability,
        NickName = typeof(T).AssemblyQualifiedName,
        Behavior = blipblip
      };
      who ??= this.prefab;
      who.GetComponent<BehaviorSpeculator>().AttackBehaviorGroup.AttackBehaviors.Add(theAttack);
      // this.prefab.GetComponent<BehaviorSpeculator>().AttackBehaviors.Add(blipblip); // TODO: could also just do this
    }
    return blipblip;
  }

  /// <summary>Adds a new BasicAttackBehavior attack to a custom boss.</summary>
  /// <typeparam name="T">A BasicAttackBehavior or derived class thereof.</typeparam>
  /// <param name="add">Whether to add this to our base attack pool. Set to false for attacks nested in simultaneous / sequential attacks.</param>
  /// <param name="cooldown">Time before THIS behavior may be run again.</param>
  /// <param name="cooldownVariance">Time variance added to the base cooldown.</param>
  /// <param name="attackCooldown">Time before ATTACK behaviors may be run again.</param>
  /// <param name="globalCooldown">Time before ANY behavior may be run again.</param>
  /// <param name="initialCooldown">Time after the enemy becomes active before this attack can be used for the first time.</param>
  /// <param name="initialCooldownVariance">Time variance added to the initial cooldown.</param>
  /// <param name="probability">The probability of using this attack relative to other attacks.</param>
  /// <param name="maxUsages">This attack can only be used this number of times.</param>
  /// <param name="requiresLineOfSight">Require line of sight to target. Expensive! Use for companions.</param>
  /// <param name="minHealth">The minimum amount of health an enemy can have and still use this attack.\n(Raising this means the enemy wont use this attack at low health)</param>
  /// <param name="maxHealth">The maximum amount of health an enemy can have and still use this attack.\n(Lowering this means the enemy wont use this attack until they lose health)</param>
  /// <param name="healthThresholds">The attack can only be used once each time a new health threshold is met</param>
  /// <param name="accumulateHealthThresholds">If true, the attack can build up multiple uses by passing multiple thresholds in quick succession</param>
  /// <param name="minRange">Minimum range</param>
  /// <param name="range">Range</param>
  /// <param name="minWallDist">Minimum distance from a wall</param>
  /// <returns>A BasicAttackBehavior with sane defaults initalized according to the parameters</returns>
  public BasicAttackBehavior CreateBasicAttack<T>(
    bool add = true, float cooldown = 0f, float cooldownVariance = 0f, float attackCooldown = 0f, float globalCooldown = 0f,
    float initialCooldown = 0.5f, float initialCooldownVariance = 0f,
    float probability = 1f, int maxUsages = -1, bool requiresLineOfSight = false,
    float minHealth = 0f, float maxHealth = 1f, float[] healthThresholds = null, bool accumulateHealthThresholds = true,
    float minRange = 0f, float range = 0f, float minWallDist = 0f)
    where T : BasicAttackBehavior, new()
  {
    if (healthThresholds == null)
      healthThresholds = new float[0];
    BasicAttackBehavior basicAttack = new T() {
        Cooldown                   = cooldown,
        CooldownVariance           = cooldownVariance,
        AttackCooldown             = attackCooldown,
        GlobalCooldown             = globalCooldown,
        InitialCooldown            = initialCooldown,
        InitialCooldownVariance    = initialCooldownVariance,
        MaxUsages                  = maxUsages,
        RequiresLineOfSight        = requiresLineOfSight,
        MinHealthThreshold         = minHealth,
        MaxHealthThreshold         = maxHealth,
        HealthThresholds           = healthThresholds,
        AccumulateHealthThresholds = accumulateHealthThresholds,
        MinRange                   = minRange,
        Range                      = range,
        MinWallDistance            = minWallDist,
        targetAreaStyle            = null,
      };
    if (add)
    {
      AttackBehaviorGroup.AttackGroupItem theAttack = new AttackBehaviorGroup.AttackGroupItem()
      {
        Probability = probability,
        NickName = typeof(T).AssemblyQualifiedName,
        Behavior = basicAttack
      };
      this.prefab.GetComponent<BehaviorSpeculator>().AttackBehaviorGroup.AttackBehaviors.Add(theAttack);
      // this.prefab.GetComponent<BehaviorSpeculator>().AttackBehaviors.Add(basicAttack); // TODO: could also just do this
    }
    return basicAttack;
  }

  public SimultaneousAttackBehaviorGroup CreateSimultaneousAttack(List<AttackBehaviorBase> attacks, bool add = true, float probability = 1f)
  {
    SimultaneousAttackBehaviorGroup theGroup = new SimultaneousAttackBehaviorGroup(){AttackBehaviors = attacks};
    if (add)
    {
      AttackBehaviorGroup.AttackGroupItem theAttack = new AttackBehaviorGroup.AttackGroupItem()
      {
        Probability = probability,
        // NickName = typeof(T).AssemblyQualifiedName,
        Behavior = theGroup
      };
      this.prefab.GetComponent<BehaviorSpeculator>().AttackBehaviorGroup.AttackBehaviors.Add(theAttack);
      // this.prefab.GetComponent<BehaviorSpeculator>().AttackBehaviors.Add(theGroup); // TODO: could also just do this
    }
    return theGroup;
  }

  public SequentialAttackBehaviorGroup CreateSequentialAttack(List<AttackBehaviorBase> attacks, List<float> cooldownOverrides = null, bool add = true, float probability = 1f)
  {
    if (cooldownOverrides != null && cooldownOverrides.Count != attacks.Count)
      cooldownOverrides = null;
    SequentialAttackBehaviorGroup theGroup = new SequentialAttackBehaviorGroup(){
      AttackBehaviors = attacks,
      OverrideCooldowns = cooldownOverrides,
      };
    if (add)
    {
      AttackBehaviorGroup.AttackGroupItem theAttack = new AttackBehaviorGroup.AttackGroupItem()
      {
        Probability = probability,
        // NickName = typeof(T).AssemblyQualifiedName,
        Behavior = theGroup
      };
      this.prefab.GetComponent<BehaviorSpeculator>().AttackBehaviorGroup.AttackBehaviors.Add(theAttack);
      // this.prefab.GetComponent<BehaviorSpeculator>().AttackBehaviors.Add(theGroup); // TODO: could also just do this
    }
    return theGroup;
  }

  public TargetPlayerBehavior TargetPlayer(float radius = 35f, float searchInterval = 0.25f, float pauseTime = 0.25f, bool pauseOnTargetSwitch = false, bool lineOfSight = false, bool objectPermanence = true)
  {
    TargetPlayerBehavior t = new TargetPlayerBehavior
    {
      Radius = radius,
      LineOfSight = lineOfSight,
      ObjectPermanence = objectPermanence,
      SearchInterval = searchInterval,
      PauseOnTargetSwitch = pauseOnTargetSwitch,
      PauseTime = pauseTime
    };
    this.prefab.GetComponent<BehaviorSpeculator>().TargetBehaviors.Add(t);
    return t;
  }

  public void MakeInteractible<T>(bool preFight = true, bool postFight = false) where T : BossNPC
  {
    // WARNING: if this were ever actually attached to a proper BossTriggerZone, this could cause null dereferences
    // in the vanilla BossTriggerZone.cs -> OnTriggerCollision() method. See discussion on Gungeon Modding Discord 2023-11-05
    if (preFight)
      this.prefab.GetComponent<GenericIntroDoer>().triggerType = GenericIntroDoer.TriggerType.BossTriggerZone;
    T npc = this.prefab.AddComponent<T>();
      npc.hasPreFightDialogue  = preFight;
      npc.hasPostFightDialogue = postFight;
      npc.autoFlipSprite       = false;
  }

  public void AddNamedVFX(VFXObject vfxobj, string name, Transform transformAnchor = null)
  {
      VFXComplex complex = new VFXComplex();
      complex.effects    = new VFXObject[] { vfxobj };
      AddNamedVFX(complex, name, transformAnchor);
  }

  public void AddNamedVFX(VFXComplex complex, string name, Transform transformAnchor = null)
  {
      VFXPool pool = new VFXPool();
      pool.type    = VFXPoolType.All;
      pool.effects = new VFXComplex[] { complex };
      AddNamedVFX(pool, name, transformAnchor);
  }

  public void AddNamedVFX(VFXPool pool, string name, Transform transformAnchor = null)
  {
    if (this.enemyBehavior.aiAnimator.OtherVFX == null)
      this.enemyBehavior.aiAnimator.OtherVFX = new List<AIAnimator.NamedVFXPool>();
    this.enemyBehavior.aiAnimator.OtherVFX.Add(new AIAnimator.NamedVFXPool(){name = name, vfxPool = pool, anchorTransform = transformAnchor});
  }

  public void SetDefaultColliders(int width, int height, int xoff = 0, int yoff = 0)
  {
    this.enemyBehavior.aiActor.specRigidbody.SetDefaultColliders(width,height,xoff,yoff); //TODO: should be automatically set from sprite
  }

  public void InitSpritesFromResourcePath(string spritePath)
  {
    this.enemyBehavior.InitSpritesFromResourcePath(spritePath);
    if (this.spriteAnchor is Anchor anchor)
      foreach (tk2dSpriteDefinition def in this.enemyBehavior.GetComponent<tk2dSpriteCollectionData>().spriteDefinitions)
        def.BetterConstructOffsetsFromAnchor(anchor);
  }

  public void AdjustAnimation(string name, float? fps = null, bool? loop = null)
  {
    this.enemyBehavior.AdjustAnimation(name, fps, loop);
  }

  public void SetIntroAnimations(string introAnim = null, string preIntroAnim = null)
  {
    if (introAnim != null)
      this.prefab.GetComponent<GenericIntroDoer>().introAnim = introAnim;
    if (preIntroAnim != null)
      this.prefab.GetComponent<GenericIntroDoer>().preIntroAnim = preIntroAnim;
  }

  public void AddBossToGameEnemies(string name)
  {
    Game.Enemies.Add(name, this.enemyBehavior.aiActor);
  }

  public void AddCustomIntro<T>() where T: SpecificIntroDoer
  {
    this.prefab.AddComponent<T>();
  }

  public void AddBossToFloorPool(Floors floors, float weight = 1f)
  {
    this.prefab.AddBossToFloorPool(bb: this, guid: this.guid, floors: floors, weight: weight);
  }

  public PrototypeDungeonRoom CreateStandaloneBossRoom(int width, int height, bool exitOnBottom)
  {
    return this.prefab.CreateStandaloneBossRoom(bb: this, width: width, height: height, exitOnBottom: exitOnBottom);
  }
}

// BuildABoss helper extension methods
public static class BH
{
  // Used for loading a sane default behavior speculator
  public const string BULLET_KIN_GUID = "01972dee89fc4404a5c408d50007dad5";

  // Per Apache, need reference to BossManager or Unity will muck with the prefab
  private static BossManager theBossMan = null;

  // Regular expression for teasing apart animation names in a folder
  public static Regex rx_anim = new Regex(@"^(?:([^_]*?)_)?(.*)_([0-9]+)\.png$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Regular expression for teasing apart animation names from packed textures
  public static Regex rx_anim_no_ext = new Regex(@"^(?:([^_]*?)_)?(.*)_([0-9]+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

  public static List<int> Range(int start, int end)
  {
    return Enumerable.Range(start, end-start+1).ToList();
  }

  public static IEnumerator WaitForSecondsInvariant(float time)
  {
    for (float elapsed = 0f; elapsed < time; elapsed += GameManager.INVARIANT_DELTA_TIME) { yield return null; }
    yield break;
  }

  public static void CopySaneDefaultBehavior(this BehaviorSpeculator self, BehaviorSpeculator other)
  {
    self.OverrideBehaviors               = other.OverrideBehaviors;
    self.OtherBehaviors                  = other.OtherBehaviors;
    self.InstantFirstTick                = other.InstantFirstTick;
    self.TickInterval                    = other.TickInterval;
    self.PostAwakenDelay                 = other.PostAwakenDelay;
    self.RemoveDelayOnReinforce          = other.RemoveDelayOnReinforce;
    self.OverrideStartingFacingDirection = other.OverrideStartingFacingDirection;
    self.StartingFacingDirection         = other.StartingFacingDirection;
    self.SkipTimingDifferentiator        = other.SkipTimingDifferentiator;
  }

  public static void SetDefaultColliders(this SpeculativeRigidbody self, int width, int height, int xoff = 0, int yoff = 0)
  {
    self.PixelColliders.Clear();
    for (int i = 0; i < 2; ++i)
      self.PixelColliders.Add(new PixelCollider
        {
          ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
          CollisionLayer = (i == 0) ? CollisionLayer.EnemyCollider : CollisionLayer.EnemyHitBox,
          ManualOffsetX = xoff,
          ManualOffsetY = yoff,
          ManualWidth = width,
          ManualHeight = height,
        });
  }

  public static T AddSaneDefaultBossBehavior<T>(GameObject prefab, string name, string subtitle, string bossCardPath = "")
    where T : BraveBehaviour
  {
    BraveBehaviour companion = prefab.AddComponent<T>();
      companion.aiActor.healthHaver.PreventAllDamage = false;
      companion.aiActor.HasShadow = false;
      companion.aiActor.IgnoreForRoomClear = false;
      companion.aiActor.specRigidbody.CollideWithOthers = true;
      companion.aiActor.specRigidbody.CollideWithTileMap = true;
      companion.aiActor.PreventFallingInPitsEver = true;
      companion.aiActor.procedurallyOutlined = false;
      companion.aiActor.CanTargetPlayers = true;
      companion.aiActor.PreventBlackPhantom = false;
      companion.aiActor.CorpseObject = EnemyDatabase.GetOrLoadByGuid(BULLET_KIN_GUID).CorpseObject;
      // set some sane stats
      companion.aiActor.healthHaver.SetHealthMaximum(1000f);
      companion.aiActor.healthHaver.FullHeal();
      // companion.aiActor.healthHaver.ForceSetCurrentHealth(1000f);
      companion.aiActor.knockbackDoer.weight = 100;
      companion.aiActor.MovementSpeed = 1f;
      companion.aiActor.CollisionDamage = 1f;
      companion.aiActor.aiAnimator.HitReactChance = 0.05f;
      companion.aiActor.CollisionKnockbackStrength = 5f;
      // companion.aiActor.ShadowObject = (GameObject)UnityEngine.Object.Instantiate(ResourceCache.Acquire("DefaultShadowSprite"));

    // prefab.name = tableId+"_NAME";
    prefab.name = name;
    string tableId = "#"+name.Replace(" ","_").ToUpper();
    ETGMod.Databases.Strings.Enemies.Set(tableId+"_NAME", name);
    ETGMod.Databases.Strings.Enemies.Set(tableId+"_SUBTITLE", subtitle);
    ETGMod.Databases.Strings.Enemies.Set(tableId+"_QUOTE", string.Empty);
    companion.aiActor.healthHaver.overrideBossName = tableId+"_NAME";
    companion.aiActor.OverrideDisplayName = tableId+"_NAME";
    companion.aiActor.ActorName = tableId+"_NAME";
    companion.aiActor.name = tableId+"_NAME";
    GenericIntroDoer miniBossIntroDoer = BH.AddSaneDefaultIntroDoer(prefab);
      if (!String.IsNullOrEmpty(bossCardPath))
      {
        Texture2D bossCardTexture = ResourceExtractor.GetTextureFromResource(bossCardPath);
        miniBossIntroDoer.portraitSlideSettings = new PortraitSlideSettings()
        {
          bossNameString = tableId+"_NAME",
          bossSubtitleString = tableId+"_SUBTITLE",
          bossQuoteString = tableId+"_QUOTE",
          bossSpritePxOffset = IntVector2.Zero,
          topLeftTextPxOffset = IntVector2.Zero,
          bottomRightTextPxOffset = IntVector2.Zero,
          bgColor = Color.cyan
        };
        miniBossIntroDoer.portraitSlideSettings.bossArtSprite = bossCardTexture;
        miniBossIntroDoer.SkipBossCard = false;
        prefab.GetComponent<BraveBehaviour>().aiActor.healthHaver.bossHealthBar = HealthHaver.BossBarType.MainBar;
      }
      else
      {
        miniBossIntroDoer.SkipBossCard = true;
        prefab.GetComponent<BraveBehaviour>().aiActor.healthHaver.bossHealthBar = HealthHaver.BossBarType.SubbossBar;
      }
      miniBossIntroDoer.SkipFinalizeAnimation = true;
      miniBossIntroDoer.RegenerateCache();

    BehaviorSpeculator bs = prefab.GetComponent<BehaviorSpeculator>();
      bs.CopySaneDefaultBehavior(EnemyDatabase.GetOrLoadByGuid(BULLET_KIN_GUID).behaviorSpeculator);
      bs.AttackBehaviorGroup.ShareCooldowns = true; //NOTE: this defaults to false
      bs.AttackBehaviorGroup.AttackBehaviors = new List<AttackBehaviorGroup.AttackGroupItem>();
      bs.TargetBehaviors = new List<TargetBehaviorBase>();
    return companion as T;
  }

  public static GenericIntroDoer AddSaneDefaultIntroDoer(GameObject prefab)
  {
    GenericIntroDoer miniBossIntroDoer = prefab.AddComponent<GenericIntroDoer>();
      miniBossIntroDoer.triggerType = GenericIntroDoer.TriggerType.PlayerEnteredRoom;
      miniBossIntroDoer.specifyIntroAiAnimator = null;
      miniBossIntroDoer.initialDelay = 0.15f;
      miniBossIntroDoer.cameraMoveSpeed = 14;
      miniBossIntroDoer.introAnim = string.Empty;
      // miniBossIntroDoer.introAnim = "intro"; //TODO: check if this actually exists
      miniBossIntroDoer.introDirectionalAnim = string.Empty;
      miniBossIntroDoer.continueAnimDuringOutro = false;
      // miniBossIntroDoer.BossMusicEvent = "Play_MUS_Boss_Theme_Beholster";
      miniBossIntroDoer.BossMusicEvent = "Play_Nothing";
      miniBossIntroDoer.PreventBossMusic = false;
      miniBossIntroDoer.InvisibleBeforeIntroAnim = false;
      miniBossIntroDoer.preIntroAnim = string.Empty;
      miniBossIntroDoer.preIntroDirectionalAnim = string.Empty;
      miniBossIntroDoer.cameraFocus = null;
      miniBossIntroDoer.roomPositionCameraFocus = Vector2.zero;
      miniBossIntroDoer.restrictPlayerMotionToRoom = false;
      miniBossIntroDoer.fusebombLock = false;
      miniBossIntroDoer.AdditionalHeightOffset = 0;
    return miniBossIntroDoer;
  }

  public static void AddAnimation(this BraveBehaviour self, tk2dSpriteCollectionData collection, List<int> ids, string name, float fps, bool loop, DirectionalAnimation.DirectionType direction = DirectionalAnimation.DirectionType.None)
  {
    tk2dSpriteAnimationClip.WrapMode loopMode = loop
      ? tk2dSpriteAnimationClip.WrapMode.Loop
      : tk2dSpriteAnimationClip.WrapMode.Once;
    SpriteBuilder.AddAnimation(self.spriteAnimator, collection, ids, name, loopMode).fps = fps;
    if (direction != DirectionalAnimation.DirectionType.None)
    {
      if (name == "idle")
      {
        self.aiAnimator.IdleAnimation = new DirectionalAnimation
        {
          Type = direction,
          Prefix = name,
          AnimNames = new string[1], // TODO: this might not be one if our directional type is not single
          Flipped = new DirectionalAnimation.FlipType[1]
        };
      }
    }
  }

  public static void AdjustAnimation(this BraveBehaviour self, string name, float? fps = null, bool? loop = null)
  {
    tk2dSpriteAnimationClip clip = self.spriteAnimator.GetClipByName(name);
    if (clip == null)
    {
      ETGModConsole.Log($"tried to modify sprite {name} which does not exist");
      return;
    }
    if (fps.HasValue)
      clip.fps = fps.Value;
    if (loop.HasValue)
    {
      clip.wrapMode = loop.Value
        ? tk2dSpriteAnimationClip.WrapMode.Loop
        : tk2dSpriteAnimationClip.WrapMode.Once;
    }
  }

  public static void InitSpritesFromResourcePath(this BraveBehaviour self, string resourcePath, int defaultFps = 15)
  {
    // TODO: maybe add warning if a path isn't added as a resource?
    string realPath = resourcePath.Replace('/', '.') + ".";

    // Load all of our sprites into a dictionary of ordered lists of names
    #if DEBUG
      ETGModConsole.Log($"loading sprites from {resourcePath}");
    #endif
    Dictionary<string,string[]> spriteMaps = new Dictionary<string,string[]>();
    // foreach (string s in ResourceExtractor.GetResourceNames())
    string bossName = resourcePath.Split('/').Last();  //HACK: assuming boss sprites are in their own folder, probably not the best
    foreach (string s in AtlasHelper._PackedTextures.Keys)
    {
      if (!s.StartsWith(bossName))
        continue;
      // string name = s.Substring(realPath.Length);  // get name of resource relative to the path
      string name = s; // new method now that we're loading from a packed texture
      // MatchCollection matches = rx_anim.Matches(name);
      MatchCollection matches = rx_anim_no_ext.Matches(name);
      foreach (Match match in matches)
      {
        string spriteName = match.Groups[1].Value;  //TODO: verification?
        string animName   = match.Groups[2].Value;
        string animIndex  = match.Groups[3].Value;
        if (!spriteMaps.ContainsKey(animName))
        {
          // ETGModConsole.Log($"  found animation {animName}");
          spriteMaps[animName] = new string[0];
        }
        int index = Int32.Parse(animIndex);
        if (index >= spriteMaps[animName].Length)
        {
          string[] sa = spriteMaps[animName];
          Array.Resize(ref sa, index+1);
          spriteMaps[animName] = sa;
        }
        spriteMaps[animName][index] = name;
      }
    }

    // create the sprite collection itself
    tk2dSpriteCollectionData bossSprites = SpriteBuilder.ConstructCollection(
      self.gameObject, (self.gameObject.name+" Collection").Replace(" ","_"));
    UnityEngine.Object.DontDestroyOnLoad(bossSprites);
    int lastAnim = 0;
    foreach(KeyValuePair<string, string[]> entry in spriteMaps)
    {
      int firstAnim = lastAnim;
      // ETGModConsole.Log($"Showing sprites for {entry.Key}");
      List<string> newSprites = new();
      foreach(string v in entry.Value)
      {
        if (String.IsNullOrEmpty(v))
          continue;
        newSprites.Add(v);
      }
      AtlasHelper.AddSpritesToCollection(newSprites, bossSprites); //TODO: this could feasibly be hoisted to the outer loop
      lastAnim += newSprites.Count;
      DirectionalAnimation.DirectionType dir;
      if (entry.Key == "idle")
        dir = DirectionalAnimation.DirectionType.Single;
      else
        dir = DirectionalAnimation.DirectionType.None;
      // ETGModConsole.Log($"calling self.AddAnimation(bossSprites, BH.Range({firstAnim}, {lastAnim-1}), \"{entry.Key}\", {defaultFps}, {true}, {dir});");
      self.AddAnimation(bossSprites, BH.Range(firstAnim, lastAnim-1), entry.Key, defaultFps, true, dir);
    }
  }

  // public static tk2dSpriteCollectionData LoadSpriteCollection(GameObject prefab, string[] spritePaths)
  // {
  //   tk2dSpriteCollectionData bossSprites = SpriteBuilder.ConstructCollection(prefab, (prefab.name+" Collection").Replace(" ","_"));
  //   UnityEngine.Object.DontDestroyOnLoad(bossSprites);
  //   AtlasHelper.AddSpritesToCollection(spritePaths, bossSprites);
  //   return bossSprites;
  // }

  //Stolen from Apache
  public static void AddObjectToRoom(PrototypeDungeonRoom room, Vector2 position, DungeonPlaceable PlacableContents = null, DungeonPlaceableBehaviour NonEnemyBehaviour = null, string EnemyBehaviourGuid = null, float SpawnChance = 1f, int xOffset = 0, int yOffset = 0, int layer = 0, int PathID = -1, int PathStartNode = 0) {
      if (room == null) { return; }
      if (room.placedObjects == null) { room.placedObjects = new List<PrototypePlacedObjectData>(); }
      if (room.placedObjectPositions == null) { room.placedObjectPositions = new List<Vector2>(); }

      PrototypePlacedObjectData m_NewObjectData = new PrototypePlacedObjectData() {
          placeableContents = null,
          nonenemyBehaviour = null,
          spawnChance = SpawnChance,
          unspecifiedContents = null,
          enemyBehaviourGuid = string.Empty,
          contentsBasePosition = position,
          layer = layer,
          xMPxOffset = xOffset,
          yMPxOffset = yOffset,
          fieldData = new List<PrototypePlacedObjectFieldData>(0),
          instancePrerequisites = new DungeonPrerequisite[0],
          linkedTriggerAreaIDs = new List<int>(0),
          assignedPathIDx = PathID,
          assignedPathStartNode = PathStartNode
      };

      if (PlacableContents != null) {
          m_NewObjectData.placeableContents = PlacableContents;
      } else if (NonEnemyBehaviour != null) {
          m_NewObjectData.nonenemyBehaviour = NonEnemyBehaviour;
      } else if (EnemyBehaviourGuid != null) {
          m_NewObjectData.enemyBehaviourGuid = EnemyBehaviourGuid;
      } else {
          // All possible object fields were left null? Do nothing and return if this is the case.
          return;
      }

      room.placedObjects.Add(m_NewObjectData);
      room.placedObjectPositions.Add(position);
      return;
  }

  //Also stolen from Apache
  public static WeightedRoom GenerateWeightedRoom(PrototypeDungeonRoom Room, float Weight = 1, bool LimitedCopies = true, int MaxCopies = 1, DungeonPrerequisite[] AdditionalPrerequisites = null) {
      if (Room == null) { return null; }
      if (AdditionalPrerequisites == null) { AdditionalPrerequisites = new DungeonPrerequisite[0]; }
      return new WeightedRoom() { room = Room, weight = Weight, limitedCopies = LimitedCopies, maxCopies = MaxCopies, additionalPrerequisites = AdditionalPrerequisites };
  }

  public static PrototypeDungeonRoom GetGenericBossRoom(int width, int height, bool exitOnBottom)
  {
    // Instantiate a new boss room
    PrototypeDungeonRoom p = Alexandria.DungeonAPI.RoomFactory.CreateEmptyRoom(width, height);
      p.category = PrototypeDungeonRoom.RoomCategory.BOSS;
      if (exitOnBottom)
      {
        p.exitData.exits.Clear();
        Alexandria.DungeonAPI.RoomFactory.AddExit(p, new Vector2(p.Width / 2, 0), DungeonData.Direction.SOUTH, PrototypeRoomExit.ExitType.ENTRANCE_ONLY);
        Alexandria.DungeonAPI.RoomFactory.AddExit(p, new Vector2(p.Width / 2, p.Height), DungeonData.Direction.NORTH, PrototypeRoomExit.ExitType.EXIT_ONLY);
      }

    // Make sure we don't treat the room as a room with normal enemies upon entry (mostly useful for interaction-based bosses)
      p.UseCustomMusicState = true;
      p.OverrideMusicState = DungeonFloorMusicController.DungeonMusicState.CALM;


    // Make sure it still seals when we enter and exit (entrance now handled in StartBossFight())
      // p.roomEvents.Add(new RoomEventDefinition(RoomEventTriggerCondition.ON_ENTER_WITH_ENEMIES, RoomEventTriggerAction.SEAL_ROOM));
      p.roomEvents.Add(new RoomEventDefinition(RoomEventTriggerCondition.ON_ENEMIES_CLEARED, RoomEventTriggerAction.UNSEAL_ROOM));

    // TODO: add custom music
      // p.UseCustomMusic = true;
      // p.CustomMusicEvent = "";
    return p;
  }

  public static PrototypeDungeonRoom CreateStandaloneBossRoom(this GameObject self, BuildABoss bb, int width, int height, bool exitOnBottom)
  {
      PrototypeDungeonRoom p = GetGenericBossRoom(width: width, height: height, exitOnBottom: exitOnBottom);
      Vector2 roomCenter = new Vector2(0.5f*p.Width, 0.5f*p.Height);
      tk2dBaseSprite anySprite = self.GetComponent<tk2dSpriteAnimator>().GetAnySprite();
      Vector2 spritePos = roomCenter - 2f * anySprite.GetRelativePositionFromAnchor(bb.spriteAnchor);
      AddObjectToRoom(p, spritePos.Quantize(C.PIXEL_SIZE), EnemyBehaviourGuid: bb.guid);
      AddObjectToRoom(p, roomCenter, NonEnemyBehaviour: bb.bossController);
      return p;
  }

  /*
    Legal tilesets:
      CASTLEGEON, SEWERGEON, GUNGEON, CATHEDRALGEON, MINEGEON, CATACOMBGEON, FORGEGEON, HELLGEON
    Illegal tilesets:
      SPACEGEON, PHOBOSGEON, WESTGEON, OFFICEGEON, BELLYGEON, JUNGLEGEON, FINALGEON, RATGEON
  */
  public static void AddBossToFloorPool(this GameObject self, BuildABoss bb, string guid, Floors floors = Floors.CASTLEGEON, float weight = 1f)
  {
      // Load our boss manager if it's not loaded already
      if (theBossMan == null)
        theBossMan = GameManager.Instance.BossManager;

      // Convert our Floors enum to a GlobalDungeonData.ValidTilesets enum
      GlobalDungeonData.ValidTilesets allowedFloors =
        (GlobalDungeonData.ValidTilesets)Enum.Parse(typeof(GlobalDungeonData.ValidTilesets), floors.ToString());

      // Get a generic boss room and add it to the center of the room
      PrototypeDungeonRoom p = self.CreateStandaloneBossRoom(bb, width: 38, height: 27, exitOnBottom: false);

      // Create a new table and add our new boss room
      GenericRoomTable theRoomTable = ScriptableObject.CreateInstance<GenericRoomTable>();
        theRoomTable.name = self.name+" Boss Table";
        theRoomTable.includedRooms = new WeightedRoomCollection();
        theRoomTable.includedRooms.elements = new List<WeightedRoom>(){GenerateWeightedRoom(p)};
        theRoomTable.includedRoomTables = new List<GenericRoomTable>(0);

      // Make a new floor entry for our boss
      IndividualBossFloorEntry entry = new IndividualBossFloorEntry() {
        BossWeight              = weight,
        TargetRoomTable         = theRoomTable,
        GlobalBossPrerequisites = new DungeonPrerequisite[] {
          new DungeonPrerequisite() {
            prerequisiteOperation = DungeonPrerequisite.PrerequisiteOperation.EQUAL_TO,
            prerequisiteType = DungeonPrerequisite.PrerequisiteType.TILESET,
            requiredTileset = allowedFloors,
            requireTileset = true,
            comparisonValue = 1,
            encounteredObjectGuid = string.Empty,
            maxToCheck = TrackedMaximums.MOST_KEYS_HELD,
            requireDemoMode = false,
            requireCharacter = false,
            requiredCharacter = PlayableCharacters.Pilot,
            requireFlag = false,
            useSessionStatValue = false,
            encounteredRoom = null,
            requiredNumberOfEncounters = -1,
            saveFlagToCheck = GungeonFlags.TUTORIAL_COMPLETED,
            statToCheck = TrackedStats.GUNBERS_MUNCHED
          }
        }
      };

      // Add the new floor entry to all allowed floors
      foreach (BossFloorEntry b in theBossMan.BossFloorData)
      {
        if ((b.AssociatedTilesets & allowedFloors) > 0)
          b.Bosses.Add(entry);
      }
  }

  // SpecificIntroDoer extension method for playing boss music
  public static uint PlayBossMusic(this AIActor aiActor, string musicName, int loopPoint = -1, int rewindAmount = -1)
  {
    uint musicEventId = GameManager.Instance.DungeonMusicController.LoopMusic(musicName, loopPoint, rewindAmount);
    aiActor.healthHaver.OnPreDeath += (_) =>
      { AkSoundEngine.StopPlayingID(musicEventId, 0, AkCurveInterpolation.AkCurveInterpolation_Constant); };
    return musicEventId;
  }

  public static uint LoopMusic(this DungeonFloorMusicController musicController, string musicName, int loopPoint, int rewindAmount)
  {
    if (musicController.m_coreMusicEventID > 0)
      AkSoundEngine.StopPlayingID(musicController.m_coreMusicEventID, 0, AkCurveInterpolation.AkCurveInterpolation_Constant);
    uint musicEventId =  AkSoundEngine.PostEvent(musicName, musicController.gameObject, in_uFlags: (uint)AkCallbackType.AK_EnableGetSourcePlayPosition);
    musicController.m_coreMusicEventID = musicEventId;
    if (loopPoint > 0 && rewindAmount > 0)
      musicController.LoopMusic(musicEventId, musicName, loopPoint, rewindAmount);
    return musicEventId;
  }

  public static void LoopMusic(this DungeonFloorMusicController musicController, uint musicPlayingEventId, string musicName, int loopPoint, int rewindAmount)
  {
    musicController.StartCoroutine(LoopMusic_CR(musicPlayingEventId, musicName, loopPoint, rewindAmount));

    static IEnumerator LoopMusic_CR(uint musicPlayingEventId, string musicName, int loopPoint, int rewindAmount)
    {
      yield return new WaitForSeconds(1f);  // GetSourcePlayPosition() will fail if we don't wait a bit
      while (true)
      {
        int pos;
        AKRESULT status = AkSoundEngine.GetSourcePlayPosition(musicPlayingEventId, out pos);
        if (status != AKRESULT.AK_Success)
          break;
        if (pos >= loopPoint)
          AkSoundEngine.SeekOnEvent(musicName, GameManager.Instance.DungeonMusicController.gameObject, pos - rewindAmount);
        yield return null;
      }
      yield break;
    }
  }

}
