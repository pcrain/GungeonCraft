namespace CwaffingTheGungy;

public static class Slimybois
{
  public static AIActor PinkSlimeEnemyPrefab = null;

  internal static GameObject _SlimeGoopDebris;
  internal static GameObject _SlimeDeathVFX;

  public static void Init()
  {
    AIActor actor = PinkSlimeEnemyPrefab = "Slime Pink".InitEnemy(health: 20, baseFps: 12, doCorpse: false);
    actor.procedurallyOutlined       = false; // TODO: remove outlines from sprites later
    actor.MovementSpeed              = 4.5f;
    actor.CollisionDamage            = 0.0f; // Overridden by SlimyboiChargeBehavior at charge time
    actor.CollisionKnockbackStrength = 0.0f; // Overridden by SlimyboiChargeBehavior at charge time
    actor.AvoidRadius                = 4.0f;

    BehaviorSpeculator bs = PinkSlimeEnemyPrefab.gameObject.GetComponent<BehaviorSpeculator>();
    bs.TargetBehaviors.Add(new SlimyboiTargetingBehavior(){ Radius = 35.0f, LineOfSight = false });
    bs.MovementBehaviors.Add(new SeekTargetBehavior(){ StopWhenInRange = true, CustomRange = 2.0f, PathInterval = 0.5f });
    bs.MovementBehaviors.Add(new MoveErraticallyBehavior { PathInterval = 0.5f, StayOnScreen = false, UseTargetsRoom = false, AvoidTarget = false });
    bs.AttackBehaviors.Add(new SlimyboiChargeBehavior(){ chargeDamage = 0.5f, chargeKnockback = 5.0f, chargeSpeed = 30.0f,
      minRange = 0.0f, maxRange = 3.0f, Cooldown = 0.5f, maxChargeDistance = 3.5f }); //NOTE: chargeDamage multiplied by 5 on enemies for some reason

    KnockbackDoer kbd = PinkSlimeEnemyPrefab.gameObject.GetComponent<KnockbackDoer>();
    kbd.weight = 40.0f;
    kbd.deathMultiplier = 3.0f;

    AIAnimator aiAnim = PinkSlimeEnemyPrefab.gameObject.GetComponent<AIAnimator>();
    aiAnim.facingType = AIAnimator.FacingType.Movement;

    GoopDoer gd = PinkSlimeEnemyPrefab.gameObject.AddComponent<GoopDoer>();
    gd.goopDefinition     = EasyGoopDefinitions.BlobulonGoopDef;
    gd.positionSource     = GoopDoer.PositionSource.HitBoxCenter;
    gd.updateTiming       = GoopDoer.UpdateTiming.Always;
    gd.updateFrequency    = 0.05f;
    gd.defaultGoopRadius  = 1f;
    gd.radiusMin          = 0.25f;
    gd.radiusMax          = 0.75f;

    actor.gameObject.AddComponent<SlimyboiController>();

    DebrisObject goopDebris = BreakableAPIToolbox.GenerateDebrisObject(
      shardSpritePath         : $"slime_debris",
      debrisObjectsCanRotate  : false,
      LifeSpanMin             : 1.0f,
      LifeSpanMax             : 2.0f,
      AngularVelocity         : 0,
      AngularVelocityVariance : 0,
      DebrisBounceCount       : 1,
      DoesGoopOnRest          : true,
      GoopType                : gd.goopDefinition,
      GoopRadius              : 0.5f,
      Mass                    : 1.0f);
    goopDebris.decayOnBounce = 0.8f;
    goopDebris.usesLifespan = true;
    _SlimeGoopDebris = goopDebris.gameObject;

    _SlimeDeathVFX = VFX.Create("slime_death_vfx");
    _SlimeDeathVFX.GetComponent<tk2dSprite>().MakeGlowyBetter(glowAmount: 100.0f, glowColorPower: 100.0f, glowColor: Color.white);
  }
}

[Flags]
public enum SlimyboiFlags // : ulong
{
  None              = 0,
  Allied            = 1 << 0, // if set, slime is allied and cannot hurt or be hurt by player characters
  CanFly            = 1 << 1, // if set, slime can fly and path over pits and other hazards
  ExplodesOnDeath   = 1 << 2, // if set, slime exploded upon dying
  ExtraCasingOnKill = 1 << 3, // if set, slime spawns an extra casing upon killing an enemy
}

public class SlimeData
{
  // string slimeName;
  // SlimyboiFlags flags;
  // Color goopColor;
  // float? overrideHealth;
  // float? overrideWeight;
  // float? overrideSpeed;
  // float? overrideContactDamage;
  // float? overrideAttackCooldown;
  // float? overrideAttackRange;
  // GoopDefinition goopDefinition;
  // GameObject attackVFX;
}

public class SlimyboiController : BraveBehaviour
{
  public SlimyboiFlags attributes;

  private void Start()
  {
    HealthHaver hh = base.healthHaver;
    hh.SuppressDeathSounds = true;
    hh.OnDeath += this.OnDeath;

    this.attributes |= SlimyboiFlags.Allied;
    AIActor actor = base.aiActor;
    actor.CanTargetEnemies = true;
    actor.CanTargetPlayers = false;
    actor.HitByEnemyBullets = true; // NOTE: no effect once AIActor.Start() is called, so needs to be manually overridden
    actor.IgnoreForRoomClear = true;

    SpeculativeRigidbody body = base.specRigidbody;
    body.AddCollisionLayerOverride(CollisionMask.LayerToMask(CollisionLayer.EnemyHitBox));
    body.AddCollisionLayerOverride(CollisionMask.LayerToMask(CollisionLayer.Projectile)); // NOTE: HitByEnemyBullets does this already in theory, but old start was already called
    body.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
  }

  private void OnDeath(Vector2 vector)
  {
    if (base.aiActor.StealthDeath)
      return;


    base.gameObject.Play(Lazy.CoinFlip() ? "slime_death_sound_a" : "slime_death_sound_b");
    CwaffVFX.SpawnBurst(
      prefab           : Slimybois._SlimeDeathVFX,
      numToSpawn       : 6,
      basePosition     : base.aiActor.CenterPosition,
      positionVariance : 0.5f,
      baseVelocity     : null,
      minVelocity      : 2f,
      velocityVariance : 2f,
      velType          : CwaffVFX.Vel.Away,
      rotType          : CwaffVFX.Rot.Random,
      lifetime         : 0.5f,
      startScale       : 1.0f,
      endScale         : 0.1f,
      copyShaders      : true
      );
  }

  private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
  {
    GameActor actor = otherRigidbody.gameActor;
    if (actor is AIActor enemy)
    {
      if (enemy.gameObject.GetComponent<SlimyboiController>())
        PhysicsEngine.SkipCollision = true; // don't collide with other slimybois
      return;
    }
    if (otherRigidbody.projectile is Projectile proj)
    {
      if (proj.Owner is PlayerController playerOwner && playerOwner && !base.aiActor.CanTargetPlayers)
        PhysicsEngine.SkipCollision = true; // don't collide with player projectiles when allied
      return;
    }
    if (actor is not PlayerController pc)
      return;
    if (!base.aiActor.CanTargetPlayers)
    {
      PhysicsEngine.SkipCollision = true;
      return;
    }
    if (pc.CurrentGun is Gun gun1 && gun1.gameObject.GetComponent<Vacpack>() is Vacpack v1 && v1.IsVacuumingSlime(this))
    {
      PhysicsEngine.SkipCollision = true;
      return;
    }
    if (GameManager.Instance.CurrentGameType != GameManager.GameType.COOP_2_PLAYER || GameManager.Instance.GetOtherPlayer(pc) is not PlayerController p2)
      return;
    if (p2.CurrentGun is Gun gun2 && gun2.gameObject.GetComponent<Vacpack>() is Vacpack v2 && v2.IsVacuumingSlime(this))
    {
      PhysicsEngine.SkipCollision = true;
      return;
    }
  }
}

// same as ChargeBehavior, but with a max range constraint
public class SlimyboiChargeBehavior : ChargeBehavior
{
  // private static GameObject _ContactVFX = null;

  public float maxRange = 3f;

  public override void Init(GameObject gameObject, AIActor aiActor, AIShooter aiShooter)
  {
    base.Init(gameObject, aiActor, aiShooter);
    SpeculativeRigidbody specRigidbody = m_aiActor.specRigidbody;
    specRigidbody.OnCollision -= base.OnCollision; // remove ChargeBehavior.OnCollision() because we call it manually...
    specRigidbody.OnCollision += this.OnOverrideCollision; //...with OnOverrideCollision()
    m_initialized = true;
  }

  private void OnOverrideCollision(CollisionData data)
  {
    //REFACTOR: use hitVfx
    if (this.State == FireState.Charging && data.OtherRigidbody is SpeculativeRigidbody body && body.aiActor && m_aiActor && !m_aiActor.healthHaver.IsDead)
    {

      // if (_ContactVFX == null)
      //   _ContactVFX = Items.Glacier.AsGun().muzzleFlashEffects.effects[0].effects[0].effect;
      // tk2dSprite vfx = SpawnManager.SpawnVFX(_ContactVFX, data.Contact, data.Normal.EulerZ()).GetComponent<tk2dSprite>();
      // vfx.HeightOffGround = 10.0f;
      // vfx.UpdateZDepth();

      CwaffVFX.SpawnBurst(
        prefab           : Slimybois._SlimeDeathVFX, // TODO: better particle
        numToSpawn       : 3,
        basePosition     : data.Contact,
        positionVariance : 0.5f,
        minVelocity      : 4f,
        velocityVariance : 4f,
        velType          : CwaffVFX.Vel.Away,
        rotType          : CwaffVFX.Rot.Random,
        lifetime         : 0.4f,
        // fadeOutTime      : 0.2f,
        startScale       : 1.0f,
        endScale         : 0.1f,
        height           : 8.0f,
        copyShaders      : true
        );

      m_aiActor.gameObject.Play("slime_attack_sound");
      m_aiActor.knockbackDoer.ApplyKnockback(data.Normal, 25f);
    }
    base.OnCollision(data); // run ChargeBehavior.OnCollision() as normal
  }

  public override BehaviorResult Update()
  {
    if (!m_initialized)
    {
      SpeculativeRigidbody specRigidbody = m_aiActor.specRigidbody;
      specRigidbody.OnCollision += base.OnCollision;
      m_initialized = true;
    }
    if (!IsReady() || !m_aiActor.TargetRigidbody)
      return BehaviorResult.Continue;
    Vector2 myCenter = m_aiActor.specRigidbody.UnitCenter;
    Vector2 targetCenter = m_aiActor.TargetRigidbody.specRigidbody.GetUnitCenter(ColliderType.HitBox);
    if (leadAmount > 0f)
    {
      Vector2 vector2 = targetCenter + m_aiActor.TargetRigidbody.specRigidbody.Velocity * 0.75f;
      vector2 = BraveMathCollege.GetPredictedPosition(targetCenter, m_aiActor.TargetVelocity, myCenter, chargeSpeed);
      targetCenter = Vector2.Lerp(targetCenter, vector2, leadAmount);
    }
    float sqrDist = (myCenter - targetCenter).sqrMagnitude;
    if (sqrDist <= (minRange * minRange) || sqrDist >= (maxRange * maxRange))
      return BehaviorResult.Continue;

    if (!string.IsNullOrEmpty(primeAnim) || primeTime > 0f)
      State = FireState.Priming;
    else
      State = FireState.Charging;
    m_updateEveryFrame = true;
    return BehaviorResult.RunContinuous;
  }
}

// same as TargetPlayerBehavior, but cannot target other slimes
public class SlimyboiTargetingBehavior : TargetBehaviorBase
{
  private const float PLAYER_REFRESH_TIMER = 1f;

  public float Radius = 10f;
  public bool LineOfSight = true;
  public bool ObjectPermanence = true;
  public float SearchInterval = 0.25f;
  public bool PauseOnTargetSwitch;
  public float PauseTime = 0.25f;

  private float m_losTimer;
  private float m_coopRefreshSearchTimer;
  private float m_prevDistToTarget;
  private PlayerController m_previousPlayer;
  private SpeculativeRigidbody m_specRigidbody;
  private BehaviorSpeculator m_behaviorSpeculator;

  public override void Init(GameObject gameObject, AIActor aiActor, AIShooter aiShooter)
  {
    base.Init(gameObject, aiActor, aiShooter);
    m_specRigidbody = gameObject.GetComponent<SpeculativeRigidbody>();
    m_behaviorSpeculator = gameObject.GetComponent<BehaviorSpeculator>();
  }

  public override void Upkeep()
  {
    base.Upkeep();
    DecrementTimer(ref m_losTimer);
    DecrementTimer(ref m_coopRefreshSearchTimer);
  }

  public override BehaviorResult Update()
  {
    BehaviorResult behaviorResult = base.Update();
    if (behaviorResult != 0)
      return behaviorResult;
    if (m_losTimer > 0f)
      return BehaviorResult.Continue;
    m_losTimer = SearchInterval;
    if (m_behaviorSpeculator.PlayerTarget)
    {
      if (m_behaviorSpeculator.PlayerTarget.IsFalling || (m_behaviorSpeculator.PlayerTarget.healthHaver && (m_behaviorSpeculator.PlayerTarget.healthHaver.IsDead || m_behaviorSpeculator.PlayerTarget.healthHaver.PreventAllDamage)))
      {
        m_behaviorSpeculator.PlayerTarget = null;
        if ((bool)m_aiActor)
          m_aiActor.ClearPath();
        return BehaviorResult.SkipRemainingClassBehaviors;
      }
    }
    else
      m_behaviorSpeculator.PlayerTarget = null;

    if (!ObjectPermanence)
      m_behaviorSpeculator.PlayerTarget = null;
    if (m_behaviorSpeculator.PlayerTarget != null && m_behaviorSpeculator.PlayerTarget.IsStealthed)
      m_behaviorSpeculator.PlayerTarget = null;
    if (GameManager.Instance.AllPlayers.Length > 1 && m_coopRefreshSearchTimer <= 0f)
      m_behaviorSpeculator.PlayerTarget = null;
    if (m_behaviorSpeculator.PlayerTarget is AIActor)
    {
      float num = Vector2.Distance(m_specRigidbody.UnitCenter, m_behaviorSpeculator.PlayerTarget.specRigidbody.UnitCenter);
      if (m_prevDistToTarget + 3f < num)
        m_behaviorSpeculator.PlayerTarget = null;
      m_prevDistToTarget = num;
      if ((bool)m_aiActor && !m_aiActor.IsNormalEnemy && (bool)m_aiActor.CompanionOwner && m_behaviorSpeculator.PlayerTarget is AIActor && m_behaviorSpeculator.PlayerTarget.GetAbsoluteParentRoom() != m_aiActor.CompanionOwner.CurrentRoom)
        m_behaviorSpeculator.PlayerTarget = null;
    }
    if (m_behaviorSpeculator.PlayerTarget != null)
      return BehaviorResult.Continue;
    PlayerController playerController = GameManager.Instance.GetActivePlayerClosestToPoint(m_specRigidbody.UnitCenter);
    if ((bool)m_aiActor && m_aiActor.SuppressTargetSwitch)
      playerController = m_previousPlayer;
    if (!m_aiActor || (m_aiActor.CanTargetPlayers && !m_aiActor.CanTargetEnemies))
    {
      if (playerController == null)
        return BehaviorResult.Continue;
      m_behaviorSpeculator.PlayerTarget = playerController;
      if (GameManager.Instance.AllPlayers.Length > 1)
        m_coopRefreshSearchTimer = PLAYER_REFRESH_TIMER;
    }
    else if (m_aiActor.CanTargetEnemies && !m_aiActor.CanTargetPlayers)
    {
      List<AIActor> activeEnemies = GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(m_aiActor.GridPosition).GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
      if (activeEnemies != null && activeEnemies.Count > 0)
      {
        AIActor aIActor = null;
        float num2 = -1f;
        if (!m_aiActor || m_aiActor.IsNormalEnemy || !m_aiActor.CompanionOwner || !m_aiActor.CompanionOwner.IsStealthed)
        {
          for (int i = 0; i < activeEnemies.Count; i++)
          {
            AIActor candidate = activeEnemies[i];
            if (candidate && candidate.IsNormalEnemy && !candidate.gameObject.GetComponent<SlimyboiController>() && !candidate.IsGone && !candidate.IsHarmlessEnemy && !(candidate == m_aiActor) && (!candidate.healthHaver || !candidate.healthHaver.PreventAllDamage))
            {
              float num3 = Vector2.Distance(m_specRigidbody.UnitCenter, candidate.specRigidbody.UnitCenter);
              if (aIActor == null || num3 < num2)
              {
                aIActor = candidate;
                num2 = num3;
              }
            }
          }
        }
        if ((bool)aIActor)
        {
          m_behaviorSpeculator.PlayerTarget = aIActor;
          m_prevDistToTarget = num2;
        }
      }
    }
    if (m_aiShooter != null && m_behaviorSpeculator.PlayerTarget != null)
      m_aiShooter.AimAtPoint(m_behaviorSpeculator.PlayerTarget.CenterPosition);
    if ((bool)m_aiActor && PauseOnTargetSwitch && m_aiActor.HasBeenEngaged && (bool)m_previousPlayer && (bool)playerController && m_previousPlayer != playerController)
    {
      m_aiActor.behaviorSpeculator.AttackCooldown = Mathf.Max(m_aiActor.behaviorSpeculator.AttackCooldown, PauseTime);
      return BehaviorResult.SkipAllRemainingBehaviors;
    }
    m_previousPlayer = playerController;
    if ((bool)m_aiActor && !m_aiActor.HasBeenEngaged)
    {
      m_aiActor.HasBeenEngaged = true;
      return BehaviorResult.SkipAllRemainingBehaviors;
    }
    return BehaviorResult.SkipRemainingClassBehaviors;
  }
}
