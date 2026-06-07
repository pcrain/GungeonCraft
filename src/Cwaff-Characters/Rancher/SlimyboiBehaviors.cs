namespace CwaffingTheGungy;

/* TODO:
    - fix targeting invulnerable chancellor (check for BulletKingToadieController and m_isCrazed)
*/

/// <summary>Main controller class for Slimes</summary>
public class SlimyboiController : BraveBehaviour
{
  private const string VACPACK_FLYING_REASON = "Vacpack";
  private const float IFRAME_LENGTH = 0.25f;

  public SlimyboiFlags attributes;
  public SlimyboiType slimeType;

  private SlimeData _slimeData;
  private bool _activelyBeingLaunched;
  private float _jumpDuration = 1.0f;
  private float _jumpTimer = 0.0f;
  private float _growDuration = 1.0f;
  private float _growTimer = 0.0f;
  private float _projectileIframeTimer = 0.0f;
  private tk2dSprite _trueSprite = null;
  private tk2dSprite _renderSprite = null;
  private ParticleSystem _ps = null;
  private bool _appearOutOfNowhere = false;
  private bool _setup = false;

  private void Start()
  {
    Setup();
  }

  private void Setup()
  {
    if (this._setup)
      return;

    this._slimeData = this.slimeType.Data(); // NOTE: can't serialize this because broken ):

    HealthHaver hh = base.healthHaver;
    hh.SuppressDeathSounds = true;
    hh.OnDeath += this.OnDeath;
    hh.OnDamaged += this.OnDamaged;

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

    this._trueSprite = body.sprite as tk2dSprite;

    SlimyboiManager.RegisterSlime(this);

    this._setup = true;
  }

  private void OnDamaged(float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection)
  {
    this._projectileIframeTimer = IFRAME_LENGTH;
  }

  public void HandleRoomSpawn()
  {
    base.gameObject.Play("slime_vacuum_sound");
    this._appearOutOfNowhere = true;
    Setup();
  }

  public void HandleFiredFromVacpack(Vector2 dir)
  {
    this._activelyBeingLaunched = true;
    base.gameObject.Play("slime_launch_sound");
    base.aiActor.SetIsFlying(true, VACPACK_FLYING_REASON, adjustShadow: false, modifyPathing: false);
    base.aiActor.CollisionDamage = 1.0f; // TODO: don't hardcode this, should be double the value from the charge behavior
    base.specRigidbody.OnCollision += this.OnCollisionAfterBeingShot;
    RecreateParticleSystem(dir);
  }

  private void RecreateParticleSystem(Vector2 dir)
  {
    if (this._ps)
      DestroyParticleSystem();
    GameObject psObj = UnityEngine.Object.Instantiate(Slimybois.SlimeParticleSystem);
    psObj.transform.position = base.aiActor.sprite.WorldCenter;
    psObj.transform.parent   = base.gameObject.transform;
    psObj.transform.localRotation = dir.EulerZ();
    this._ps = psObj.GetComponent<ParticleSystem>();
  }

  private void DestroyParticleSystem(bool immediate = false)
  {
    if (!this._ps)
      return;

    this._ps.Stop(true, immediate ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
    this._ps.gameObject.transform.parent = null;
    if (immediate)
      UnityEngine.Object.Destroy(this._ps.gameObject);
    else
      UnityEngine.Object.Destroy(this._ps.gameObject, Mathf.Min(3.0f, this._ps.main.duration + this._ps.main.startLifetime.constantMax));
    this._ps = null;
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
    DestroyParticleSystem();
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

  public void Jump(float? overideDuration = null, bool growIn = false)
  {
    this._jumpTimer = overideDuration ?? Mathf.Clamp(0.6f * (this.slimeType.Data().overrideAttackCooldown ?? Slimybois._DEFAULT_COOLDOWN), 0.15f, 0.5f);
    this._jumpDuration = this._jumpTimer;
    if (growIn)
    {
      this._growTimer = 0.5f * this._jumpTimer;
      this._growDuration = this._growTimer;
    }
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

    // SlimyboiChargeBehavior charge = base.behaviorSpeculator.AttackBehaviors[0] as SlimyboiChargeBehavior;
    // base.aiActor.DebugNametag($"target: {(base.aiActor.PlayerTarget is AIActor a ? a.AmmonomiconName() : "null")}\nstate: {charge.State}\nready: {charge.IsReady()}\nlastcharge: {Time.frameCount - charge._LastUpdateFrame}\nattackcooldown: {base.aiActor.behaviorSpeculator.m_attackCooldownTimer}");

    if (!this._renderSprite)
    {
      this._renderSprite = base.specRigidbody.DecoupleSpriteFromCollider();
      if (this._appearOutOfNowhere)
      {
        this._appearOutOfNowhere = false;
        Vector2 pos = this._trueSprite.WorldCenter;
        for (int i = 0; i < 10; ++i)
        {
          DebrisObject debris = UnityEngine.Object.Instantiate(
            this._slimeData.debris, pos, Quaternion.identity).GetComponent<DebrisObject>();
          debris.GravityOverride = 30.0f;
          debris.Trigger(Lazy.RandomVector(3f * UnityEngine.Random.value).ToVector3ZUp(4f), 0.25f);
          debris.sprite.MakeGlowyBetter(glowAmount: 10.0f, glowColor: new Color(1.0f, 0.75f, 0.9f), glowColorPower: 20.0f, sensitivity: 0.3f);
        }
        Jump(0.5f, growIn: true);
        this._renderSprite.scale = new Vector3(0.0f, 0.0f, 1.0f);
      }
    }

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
    if (this._growTimer > 0)
    {
      this._growTimer = Mathf.Max(0.0f, this._growTimer - dtime);
      float scale = 1f - this._growTimer / this._growDuration;
      this._renderSprite.scale = new Vector3(scale, scale, 1.0f);
      renderPos -= this._renderSprite.GetRelativePositionFromAnchor(Anchor.LowerCenter).ToVector3ZUp();
      renderPos += new Vector3(0.5f * this._renderSprite.GetCurrentSpriteDef().untrimmedBoundsDataExtents.x, 0.0f, 0.0f); // add half width of sprite
    }

    this._renderSprite.transform.position = renderPos.Quantize(0.0625f);

    if (this._activelyBeingLaunched && !base.knockbackDoer.CheckSourceInKnockbacks(base.gameObject))
      HandleNoLongerFiredFromVacpack();

    // invulnerability flicker
    // TODO: this doesn't work quite right if other things disable our renderer
    this._projectileIframeTimer = Mathf.Max(this._projectileIframeTimer - dtime, 0.0f);
    if (Mathf.FloorToInt(this._projectileIframeTimer * 20.0f) % 2 == 1)
      this._renderSprite.renderer.enabled = false;
    else
      this._renderSprite.renderer.enabled = true;
  }

  private void OnCollisionAfterBeingShot(CollisionData data)
  {
    base.specRigidbody.OnCollision -= this.OnCollisionAfterBeingShot;
    HandleNoLongerFiredFromVacpack();
    base.knockbackDoer.m_activeKnockbacks.Clear();
    base.specRigidbody.Velocity = Vector2.zero;
    OnAttackCollision(data, knockback: 2f * base.specRigidbody.Velocity.magnitude);
  }

  public void OnAttackCollision(CollisionData data, float knockback)
  {
      float vfxAngle = (-base.specRigidbody.Velocity).ToAngle().AddRandomSpread(10f);
      CwaffVFX.Spawn(
        prefab           : Slimybois._SlimeImpactVFX, // TODO: better particle
        position         : data.Contact + Lazy.RandomVector(0.25f),
        velocity         : vfxAngle.ToVector(UnityEngine.Random.Range(5.0f, 8.0f)),
        rotation         : vfxAngle.EulerZ(),
        height           : 8.0f,
        copyShaders      : true
        );

      // NOTE: attempt to prevent getting stuck inside enemies and dealing ridiculous damage
      base.specRigidbody.RegisterTemporaryCollisionException(data.OtherRigidbody, 0.01f);
      base.gameObject.Play("slime_attack_sound");
      base.knockbackDoer.ApplyKnockback(data.Normal, knockback, time: this._jumpDuration);
      Jump();
  }

  private void OnDeath(Vector2 vector)
  {
    DestroyParticleSystem(true);

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

  public override void OnDestroy()
  {
    DestroyParticleSystem(true);
    SlimyboiManager.DeregisterSlime(this);
    base.OnDestroy();
  }

  private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
  {
    if (this._projectileIframeTimer > 0 && otherRigidbody.projectile)
    {
      PhysicsEngine.SkipCollision = true; // don't collide with projectiles while iframes are active
      return;
    }
    GameActor actor = otherRigidbody.gameActor;
    if (actor is AIActor enemy)
    {
      if (enemy.gameObject.GetComponent<SlimyboiController>())
        PhysicsEngine.SkipCollision = true; // don't collide with other slimybois
      return;
    }
    if (this._activelyBeingLaunched)
    {
      PhysicsEngine.SkipCollision = true; // don't collide with anything other than the tilemap and enemies while being launched
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

// same as ChargeBehavior, but with a max range constraint and better range detection to detect large bosses
public class SlimyboiChargeBehavior : ChargeBehavior
{
  public float maxRange = 3f;

  private SlimyboiController _slime = null;

  // internal float _LastUpdateFrame = 0;

  public override void Init(GameObject gameObject, AIActor aiActor, AIShooter aiShooter)
  {
    base.Init(gameObject, aiActor, aiShooter);
    SpeculativeRigidbody specRigidbody = m_aiActor.specRigidbody;
    this._slime = base.m_aiActor.gameObject.GetComponent<SlimyboiController>();
    specRigidbody.OnCollision -= base.OnCollision; // remove ChargeBehavior.OnCollision() because we call it manually with OnOverrideCollision()
    specRigidbody.OnCollision -= this.OnOverrideCollision;
    specRigidbody.OnCollision += this.OnOverrideCollision;
    m_initialized = true;
  }

  private void OnOverrideCollision(CollisionData data)
  {
    //REFACTOR: use hitVfx
    if (this.State == FireState.Charging && data.OtherRigidbody is SpeculativeRigidbody body && body.aiActor && m_aiActor && !m_aiActor.healthHaver.IsDead && this._slime)
    {
      // NOTE: this makes enemies with guns almost unable to fire them due to being too close
      // if (!body.aiActor.OverrideTarget)
      //   body.aiActor.OverrideTarget = m_aiActor.specRigidbody;
      this._slime.OnAttackCollision(data, 25f); // TODO: figure out better way to set knockback
    }
    base.OnCollision(data); // run ChargeBehavior.OnCollision() as normal
  }

  /// <summary>Changed to use distance to hitbox rectangel</summary>
  public override BehaviorResult Update()
  {
    // _LastUpdateFrame = Time.frameCount;
    if (!m_initialized)
    {
      SpeculativeRigidbody specRigidbody = m_aiActor.specRigidbody;
      specRigidbody.OnCollision += base.OnCollision;
      m_initialized = true;
    }
    if (!IsReady() || !m_aiActor.TargetRigidbody)
      return BehaviorResult.Continue;

    Vector2 myCenter = m_aiActor.specRigidbody.UnitCenter;
    PixelCollider targetCollider = m_aiActor.TargetRigidbody.specRigidbody.GetPixelCollider(ColliderType.HitBox);
    if (targetCollider == null)
      return BehaviorResult.Continue;

    // NOTE: better tracking using closest point on collider rather than center (fixes tracking issues with large enemies like Bullet King)
    Vector2 targetCenter = myCenter.ClosestPointOnCollider(targetCollider);
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

    HealthHaver thh = target.healthHaver;
    if (target.IsFalling || (thh && (thh.IsDead || thh.PreventAllDamage)))
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
    if (thh && !thh.IsVulnerable)
      return true; // NOTE: don't attack invulnerable targets
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
      RoomHandler myRoom = GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(m_aiActor.GridPosition);
      List<AIActor> activeEnemies = (myRoom != null) ? myRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All) : null;
      if (activeEnemies != null && activeEnemies.Count > 0)
      {
        AIActor closestTarget = null;
        float closest = -1f;
        Vector2 myPos = m_specRigidbody.UnitCenter;
        if (!m_aiActor || m_aiActor.IsNormalEnemy || !m_aiActor.CompanionOwner || !m_aiActor.CompanionOwner.IsStealthed)
        {
          for (int i = 0; i < activeEnemies.Count; i++)
          {
            AIActor candidate = activeEnemies[i];
            // NOTE: allow attacking harmless enemies like key bullet kin and chance kin
            if (!candidate || !candidate.IsNormalEnemy || candidate.gameObject.GetComponent<SlimyboiController>() || candidate.IsGone || candidate == m_aiActor || candidate.healthHaver && candidate.healthHaver.PreventAllDamage)
                continue;
            Vector2 epos = candidate.specRigidbody.UnitCenter;
            float sqrdist = (myPos - epos).sqrMagnitude;
            if (closestTarget != null && sqrdist >= closest)
              continue;
            if (!m_aiActor.IsFlying && myRoom.SeparatedByPit(myPos, epos))
            {
              // Lazy.DebugConsoleLog($"can't target across pit");
              continue;
            }
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

// REFACTOR: relocate to Slimyboi.cs
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

  /// <summary>Patch to make player-owned beams not hit slimes.</summary>
  private static SpeculativeRigidbody[] _IgnoredBodiesPlusSlimes = new SpeculativeRigidbody[0];
  [HarmonyPatch(typeof(BeamController), nameof(BeamController.GetIgnoreRigidbodies))]
  [HarmonyPostfix]
  private static void BeamControllerGetIgnoreRigidbodiesPatch(BeamController __instance, ref SpeculativeRigidbody[] __result)
  {
      if (!SlimyboiManager.AnyActiveSlimes() || __instance.Owner is not PlayerController)
        return; // shortcut if no slimes are active or if the beam is not player-owned

      int numSlimes = SlimyboiManager.NumActiveSlimes();
      int numOtherBodies = __result.Length; // get the older number of ignored bodies
      int totalIgnoredBodies = numSlimes + numOtherBodies; // total ignored bodies is the old number + the number of slimes
      if (totalIgnoredBodies != _IgnoredBodiesPlusSlimes.Length)
        _IgnoredBodiesPlusSlimes = new SpeculativeRigidbody[totalIgnoredBodies]; // don't reallocate unless we absolutely need to
      int i;
      for (i = 0; i < numOtherBodies; ++i)
        _IgnoredBodiesPlusSlimes[i] = __result[i]; // copy the result array over
      foreach (SlimyboiController sloim in SlimyboiManager.ActiveSlimes)
        _IgnoredBodiesPlusSlimes[i++] = sloim ? sloim.specRigidbody : null; // add rigidbodies for the slimes in as necessary
      __result = _IgnoredBodiesPlusSlimes; // replace the result with the slimes
  }

  /// <summary>Patches to detect rooms that spawn with traps</summary>
  [HarmonyPatch(typeof(PathingTrapController), nameof(PathingTrapController.Start))]
  [HarmonyPostfix]
  private static void PathingTrapControllerStartPatch(PathingTrapController __instance)
  {
    SlimyboiManager.RegisterTrap(__instance, __instance.m_parentRoom);
  }
  [HarmonyPatch(typeof(BasicTrapController), nameof(BasicTrapController.Start))]
  [HarmonyPostfix]
  private static void BasicTrapControllerStartPatch(BasicTrapController __instance)
  {
    SlimyboiManager.RegisterTrap(__instance, __instance.m_parentRoom);
  }

  /// <summary>Patch to detect flipped tables</summary>
  [HarmonyPatch(typeof(FlippableCover), nameof(FlippableCover.Flip), typeof(DungeonData.Direction))]
  [HarmonyPostfix]
  private static void FlippableCoverFlipPatch(FlippableCover __instance, DungeonData.Direction flipDirection)
  {
    SlimyboiManager.HandleTableFlip(__instance);
  }
}
