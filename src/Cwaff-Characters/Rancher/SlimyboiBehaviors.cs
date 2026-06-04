namespace CwaffingTheGungy;

/// <summary>Main controller class for Slimes</summary>
public class SlimyboiController : BraveBehaviour
{
  private const string VACPACK_FLYING_REASON = "Vacpack";
  private const float VFX_GAP = 0.1f;

  public SlimyboiFlags attributes;
  public SlimyboiType slimeType;

  private bool _activelyBeingLaunched;
  private float _vfxTimer = 0.0f;
  private float _jumpDuration = 1.0f;
  private float _jumpTimer = 0.0f;
  private tk2dSprite _trueSprite = null;
  private tk2dSprite _renderSprite = null;

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
    actor.IsHarmlessEnemy = true;
    actor.IsNormalEnemy = false; // TODO: setting this might make setting some other things redundant
    actor.PreventAutoKillOnBossDeath = true;

    SpeculativeRigidbody body = base.specRigidbody;
    body.AddCollisionLayerOverride(CollisionMask.LayerToMask(CollisionLayer.EnemyHitBox));
    body.AddCollisionLayerOverride(CollisionMask.LayerToMask(CollisionLayer.Projectile)); // NOTE: HitByEnemyBullets does this already in theory, but old start was already called
    body.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;

    this._vfxTimer = VFX_GAP;
    this._trueSprite = body.sprite as tk2dSprite;
  }

  public void HandleFiredFromVacpack()
  {
    this._activelyBeingLaunched = true;
    base.gameObject.Play("slime_launch_sound");
    base.aiActor.SetIsFlying(true, VACPACK_FLYING_REASON, adjustShadow: false, modifyPathing: false);
    base.aiActor.CollisionDamage = 1.0f; // TODO: don't hardcode this, should be double the value from the charge behavior
    base.specRigidbody.OnCollision += this.OnCollisionAfterBeingShot;
  }

  private void HandleNoLongerFiredFromVacpack()
  {
    this._activelyBeingLaunched = false;
    if (base.gameObject.AddComponent<KnockbackUnleasher>() is KnockbackUnleasher kbu)
      UnityEngine.Object.Destroy(kbu);
    base.aiActor.SetIsFlying(false, VACPACK_FLYING_REASON, adjustShadow: false, modifyPathing: false);
    ResetCollisionDamage();
    base.specRigidbody.OnCollision -= this.OnCollisionAfterBeingShot;
    PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(base.specRigidbody);
  }

  public void HandleVacuumedByVacpack()
  {
    base.aiActor.SetIsFlying(true, VACPACK_FLYING_REASON, adjustShadow: false, modifyPathing: false);
    base.specRigidbody.CollideWithOthers = false;
  }

  public void HandleNoLongerVacuumedByVacpack()
  {
    base.aiActor.SetIsFlying(false, VACPACK_FLYING_REASON, adjustShadow: false, modifyPathing: false);
    base.specRigidbody.CollideWithOthers = true;
    PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(base.specRigidbody);
  }

  public void Jump()
  {
    this._jumpDuration = this._jumpTimer = Mathf.Max(0.6f * (this.slimeType.Data().overrideAttackCooldown ?? Slimybois._DEFAULT_COOLDOWN), 0.15f);
  }

  private void ResetCollisionDamage()
  {
    base.aiActor.CollisionDamage = 0.0f;
    foreach (AttackBehaviorBase attack in base.behaviorSpeculator.AttackBehaviors)
      if (attack is SlimyboiChargeBehavior charge)
        charge.m_cachedDamage = 0.0f;
  }

  private void Update()
  {
    const float JUMP_HEIGHT = 1.25f;
    float dtime = BraveTime.DeltaTime;

    SlimyboiChargeBehavior charge = base.behaviorSpeculator.AttackBehaviors[0] as SlimyboiChargeBehavior;
    // base.aiActor.DebugNametag($"{(base.aiActor.PlayerTarget is AIActor a ? a.AmmonomiconName() : "null")}\n{charge.State}\n{base.aiActor.CollisionDamage}\n{charge.m_cachedDamage}");

    if (!this._renderSprite)
      this._renderSprite = base.specRigidbody.DecoupleSpriteFromCollider();
    this._renderSprite.collection = this._trueSprite.collection;
    this._renderSprite.spriteId = this._trueSprite.spriteId;
    Vector3 renderPos = this._trueSprite.transform.position;

    if (this._jumpTimer > 0)
    {
      this._jumpTimer = Mathf.Max(0.0f, this._jumpTimer - dtime);
      Vector3 spritePos = this._trueSprite.transform.position;
      float jumpY = JUMP_HEIGHT * Mathf.Sin(Mathf.PI * (1f - this._jumpTimer / this._jumpDuration));
      this._trueSprite.spriteAnimator.UpdateAnimation(dtime);
      renderPos.y += jumpY;
    }

    this._renderSprite.transform.position = renderPos.Quantize(0.0625f);

    if (this._activelyBeingLaunched)
    {
      KnockbackDoer kbd = base.knockbackDoer;
      if (!kbd.CheckSourceInKnockbacks(base.gameObject))
      {
        HandleNoLongerFiredFromVacpack();
        return;
      }
      if ((this._vfxTimer -= dtime) <= 0.0f)
      {
        this._vfxTimer = VFX_GAP;
        CwaffVFX.SpawnBurst(
          prefab           : Slimybois._SlimeDeathVFX,
          numToSpawn       : 2,
          basePosition     : base.aiActor.CenterPosition,
          positionVariance : 0.5f,
          baseVelocity     : null,
          velocityVariance : 2f,
          velType          : CwaffVFX.Vel.Away,
          rotType          : CwaffVFX.Rot.Random,
          lifetime         : 0.5f,
          startScale       : 1.0f,
          endScale         : 0.1f,
          copyShaders      : true
          );
      }
    }
  }

  private void OnCollisionAfterBeingShot(CollisionData data)
  {
    base.specRigidbody.OnCollision -= this.OnCollisionAfterBeingShot;
    HandleNoLongerFiredFromVacpack();
    base.knockbackDoer.m_activeKnockbacks.Clear();
    Jump();
    base.knockbackDoer.ApplyKnockback(data.Normal, 2f * base.specRigidbody.Velocity.magnitude, time: this._jumpDuration);
    base.specRigidbody.Velocity = Vector2.zero;
    base.gameObject.Play("slime_attack_sound");
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
    if (this._activelyBeingLaunched)
    {
      PhysicsEngine.SkipCollision = true; // don't collide with anything other than the tilemap while being launched
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
    specRigidbody.OnCollision -= base.OnCollision; // remove ChargeBehavior.OnCollision() because we call it manually with OnOverrideCollision()
    specRigidbody.OnCollision -= this.OnOverrideCollision;
    specRigidbody.OnCollision += this.OnOverrideCollision;
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

      // NOTE: attempt to prevent getting stuck inside enemies and dealing ridiculous damage
      m_aiActor.specRigidbody.RegisterTemporaryCollisionException(body, 0.01f);
      m_aiActor.gameObject.Play("slime_attack_sound");
      m_aiActor.knockbackDoer.ApplyKnockback(data.Normal, 25f);
      if (m_aiActor.gameObject.GetComponent<SlimyboiController>() is SlimyboiController sloim)
        sloim.Jump();
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

// same as TargetPlayerBehavior, but cannot target other slimes or things we can't reach
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

  private bool ShouldResetTarget(out bool skipRemainingBehaviors)
  {
    skipRemainingBehaviors = false;

    if (!m_aiActor)
      return true;
    if (m_behaviorSpeculator.PlayerTarget is not GameActor target)
      return true;

    if (target.IsFalling || (target.healthHaver is HealthHaver thh && (thh.IsDead || thh.PreventAllDamage)))
    {
      if (m_aiActor)
        m_aiActor.ClearPath();
      skipRemainingBehaviors = true;
      return true;
    }

    if (!ObjectPermanence)
      return true;
    if (m_aiActor.Path != null && !m_aiActor.Path.WillReachFinalGoal)
      return true; // NOTE: repath if we can't reach our target
    if (target.IsStealthed)
      return true;
    if (GameManager.Instance.AllPlayers.Length > 1 && m_coopRefreshSearchTimer <= 0f)
      return true;
    if (target is not AIActor)
      return false;

    float distance = Vector2.Distance(m_specRigidbody.UnitCenter, target.specRigidbody.UnitCenter);
    bool targetMovedFarAway = m_prevDistToTarget + 3f < distance;
    m_prevDistToTarget = distance;
    if (targetMovedFarAway)
      return true;
    if (!m_aiActor.IsNormalEnemy && m_aiActor.CompanionOwner && target.GetAbsoluteParentRoom() != m_aiActor.CompanionOwner.CurrentRoom)
      return true;
    return false;
  }

  public override BehaviorResult Update()
  {
    BehaviorResult behaviorResult = base.Update();
    if (behaviorResult != 0)
      return behaviorResult;
    if (m_losTimer > 0f)
      return BehaviorResult.Continue;
    m_losTimer = SearchInterval;

    if (ShouldResetTarget(out bool skipRemainingBehaviors))
    {
      m_behaviorSpeculator.PlayerTarget = null;
      if (skipRemainingBehaviors)
        return BehaviorResult.SkipRemainingClassBehaviors;
    }

    if (m_behaviorSpeculator.PlayerTarget)
      return BehaviorResult.Continue;

    PlayerController playerController = GameManager.Instance.GetActivePlayerClosestToPoint(m_specRigidbody.UnitCenter);
    if (m_aiActor && m_aiActor.SuppressTargetSwitch)
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
        AIActor closestTarget = null;
        float closest = -1f;
        if (!m_aiActor || m_aiActor.IsNormalEnemy || !m_aiActor.CompanionOwner || !m_aiActor.CompanionOwner.IsStealthed)
        {
          for (int i = 0; i < activeEnemies.Count; i++)
          {
            AIActor candidate = activeEnemies[i];
            // NOTE: allow attacking harmless enemies like key bullet kin and chance kin
            if (!candidate || !candidate.IsNormalEnemy || candidate.gameObject.GetComponent<SlimyboiController>() || candidate.IsGone || candidate == m_aiActor || candidate.healthHaver && candidate.healthHaver.PreventAllDamage)
                continue;
            float sqrdist = (m_specRigidbody.UnitCenter - candidate.specRigidbody.UnitCenter).sqrMagnitude;
            if (closestTarget != null && sqrdist >= closest)
              continue;
            closestTarget = candidate;
            closest = sqrdist;
          }
        }
        if (closestTarget)
        {
          m_behaviorSpeculator.PlayerTarget = closestTarget;
          m_prevDistToTarget = closest;
        }
      }
    }
    if (m_aiShooter != null && m_behaviorSpeculator.PlayerTarget != null)
      m_aiShooter.AimAtPoint(m_behaviorSpeculator.PlayerTarget.CenterPosition);
    if (m_aiActor && PauseOnTargetSwitch && m_aiActor.HasBeenEngaged && m_previousPlayer && playerController && m_previousPlayer != playerController)
    {
      m_aiActor.behaviorSpeculator.AttackCooldown = Mathf.Max(m_aiActor.behaviorSpeculator.AttackCooldown, PauseTime);
      return BehaviorResult.SkipAllRemainingBehaviors;
    }
    m_previousPlayer = playerController;
    if (m_aiActor && !m_aiActor.HasBeenEngaged)
    {
      m_aiActor.HasBeenEngaged = true;
      return BehaviorResult.SkipAllRemainingBehaviors;
    }
    return BehaviorResult.SkipRemainingClassBehaviors;
  }
}

[HarmonyPatch]
internal static class SlimyboiPatches
{
  /// <summary>Patches to make slime collision damage ignore boss damage caps</summary>
  private static bool _NextAttackIgnoresDamageCaps = false;
  private static void SlimyboiControllerIgnoreDamageCaps(AIActor actor)
  {
    if (actor && actor.gameObject.GetComponent<SlimyboiController>())
      _NextAttackIgnoresDamageCaps = true;
  }
  [HarmonyPatch(typeof(HealthHaver), nameof(HealthHaver.ApplyDamage))]
  [HarmonyPrefix]
  private static void HealthHaverApplyDamagePatch(HealthHaver __instance, float damage, Vector2 direction, string sourceName, CoreDamageTypes damageTypes, DamageCategory damageCategory, bool ignoreInvulnerabilityFrames, PixelCollider hitPixelCollider, ref bool ignoreDamageCaps)
  {
    if (_NextAttackIgnoresDamageCaps)
      ignoreDamageCaps = true;
    _NextAttackIgnoresDamageCaps = false;
  }
  [HarmonyPatch(typeof(AIActor), nameof(AIActor.OnCollision))]
  [HarmonyILManipulator]
  private static void AIActorOnCollisionPatchIL(ILContext il)
  {
      ILCursor cursor = new ILCursor(il);
      if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<AIActor>("CollisionDamageTypes")))
        return;
      if (!cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCallvirt<HealthHaver>(nameof(HealthHaver.ApplyDamage))))
        return;

      cursor.Emit(OpCodes.Ldarg_0);
      cursor.CallPrivate(typeof(SlimyboiPatches), nameof(SlimyboiControllerIgnoreDamageCaps));
  }
}
