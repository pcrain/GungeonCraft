namespace CwaffingTheGungy;

/* TODO:
    - fix targeting invulnerable chancellor (check for BulletKingToadieController and m_isCrazed)
*/

/// <summary>Main controller class for Slimes</summary>
public class SlimyboiController : BraveBehaviour
{
  private const string VACPACK_FLYING_REASON = "Vacpack";
  private const string DERVISH_FLYING_REASON = "Dervish Aura";

  private const float _BASE_IFRAME_LENGTH = 1.0f;
  private const float _DODGE_IFRAME_LENGTH = 0.25f;
  private const float _DODGE_COOLDOWN = 1.0f;
  private const float _DODGE_LENGTH = 2.0f;
  private const float _HEAL_RADIUS = 7.5f;
  private const float _AURA_RADIUS = 6.0f;
  private const float _AURA_THICKNESS = 0.05f;
  private const float _AURA_PERSIST_TIME = 2.0f;
  private const float _REFLECT_GLOW_TIME = 1.0f;
  private const float _REFLECT_GLOW_STRENGTH = 10.0f;
  private const float _HEAL_COOLDOWN_TIME = 0.33f;
  private const float _HEAL_AMOUNT = 1.0f;
  private const float _VINE_ATTACK_TIME = 0.1f;
  private const float _VINE_HOLD_TIME = 0.3f;
  private const float _VINE_RETRACT_TIME = 0.1f;
  private const float _VINE_RADIUS = 5.0f;
  private const float _HEALTH_FROM_BULLETS = 2.5f;
  private const float _HEALTH_DRAIN_RATE = 2.0f;
  private const float _HEALTH_DRAIN_AMOUNT = 1.0f;
  private const float _MAX_GROUP_HEAL = 5.0f;
  private const float _MAX_STUCK_TIME = 0.25f;

  private const float _HEAL_RADIUS_SQR = _HEAL_RADIUS * _HEAL_RADIUS;
  private const float _AURA_RADIUS_SQR = _AURA_RADIUS * _AURA_RADIUS;
  private const float _VINE_RADIUS_SQR = _VINE_RADIUS * _VINE_RADIUS;
  private const float _VINE_TOTAL_TIME = _VINE_ATTACK_TIME + _VINE_HOLD_TIME + _VINE_RETRACT_TIME;

  public SlimyboiFlags attributes;
  public SlimyboiType slimeType;

  internal float _dodgeCooldown = 0.0f;
  internal Vector2 _dodgeVector = default;
  internal bool _queuedDodge = false;
  internal PlayerController _owner = null;
  internal tk2dBaseSprite _renderSprite = null;

  private bool _setup = false;
  private SlimeData _slimeData;
  private bool _activelyBeingLaunched;
  private float _jumpDuration = 1.0f;
  private float _jumpTimer = 0.0f;
  private float _growDuration = 1.0f;
  private float _growTimer = 0.0f;
  private float _reflectGlowTimer = 0.0f;
  private float _projectileIframeTimer = 0.0f;
  private float _buffCooldownTimer = 0.0f;
  private tk2dSprite _trueSprite = null;
  private ParticleSystem _ps = null;
  private Geometry _aura = null;
  private EasyLight _light = null;
  private bool _didGlowSetup = false;
  private bool _appearOutOfNowhere = false;
  private Color? _queuedOutlineColor = null;
  private float _iframeLength = 1.0f;
  private Vector2 _lastPosition = default;
  private float _stuckTime = 0.0f;
  private SlimyboiChargeBehavior _chargeBehavior = null;

  // Tangle specific
  private CwaffRopeMesh _vineMesh = null;
  private Projectile _vineTarget = null;
  private Vector2? _vineTargetPos = null;
  private float _vineTimer = 0.0f;
  private float _vineDirection = 0.0f;

  private DamageTypeModifier _poisonImmunity;
  private float _immuneToPoisonTimer = 0.0f;
  private DamageTypeModifier _fireImmunity;
  private float _immuneToFireTimer = 0.0f;
  private float _flightTimer = 0.0f;
  private float _goldTimer = 0.0f;
  private float _healthDrainTimer = 0.0f;

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

    this._poisonImmunity = new(){damageType = CoreDamageTypes.Poison, damageMultiplier = 0f};
    this._fireImmunity = new(){damageType = CoreDamageTypes.Fire, damageMultiplier = 0f};
    if (hh.damageTypeModifiers == null)
      hh.damageTypeModifiers = new();
    if (actor.EffectResistances == null)
      actor.EffectResistances = new ActorEffectResistance[0];
    if (this.attributes.IsSet(SlimyboiFlags.QuantumInstability))
    {
      hh.PreventAllDamage = true;
      hh.IsVulnerable = false;
    }
    if (this.attributes.IsSet(SlimyboiFlags.PassiveHealthDrain))
      this._healthDrainTimer = _HEALTH_DRAIN_RATE;
    if (this._slimeData.flags.IsSet(SlimyboiFlags.PoisonImmunity))
    {
      hh.damageTypeModifiers.Add(this._poisonImmunity);
      actor.SetResistance(EffectResistanceType.Poison, 1.0f);
    }
    if (this._slimeData.flags.IsSet(SlimyboiFlags.FireImmunity))
    {
      hh.damageTypeModifiers.Add(this._fireImmunity);
      actor.SetResistance(EffectResistanceType.Fire, 1.0f);
    }
    this._iframeLength = GetIframesForFloor();
    int chargeIndex = base.behaviorSpeculator.AttackBehaviors.FindIndex(a => a is SlimyboiChargeBehavior);
    if (chargeIndex >= 0)
      this._chargeBehavior = base.behaviorSpeculator.AttackBehaviors[chargeIndex] as SlimyboiChargeBehavior;

    SpeculativeRigidbody body = base.specRigidbody;
    body.AddCollisionLayerOverride(CollisionMask.LayerToMask(CollisionLayer.EnemyHitBox));
    body.AddCollisionLayerOverride(CollisionMask.LayerToMask(CollisionLayer.Projectile)); // NOTE: HitByEnemyBullets does this already in theory, but old start was already called
    body.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;

    this._trueSprite = body.sprite as tk2dSprite;

    SlimyboiManager.RegisterSlime(this);

    this._setup = true;
  }

  private static float GetIframesForFloor()
  {
    float healthMod = AIActor.BaseLevelHealthModifier;
    return Mathf.Clamp(_BASE_IFRAME_LENGTH / (healthMod * healthMod), 0.2f, 1.0f);
  }

  private void OnDamaged(float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection)
  {
    this._projectileIframeTimer = this._iframeLength;
  }

  public void HandleRoomSpawn()
  {
    base.gameObject.Play("slime_vacuum_sound");
    this._appearOutOfNowhere = true;
    Setup();
  }

  public void HandleFiredFromVacpack(Vector2 dir, PlayerController owner)
  {
    this._activelyBeingLaunched = true;
    this._owner = owner;
    base.gameObject.Play("slime_launch_sound");
    base.aiActor.SetIsFlying(true, VACPACK_FLYING_REASON, adjustShadow: false, modifyPathing: false);
    base.aiActor.CollisionDamage = 1.0f; // TODO: don't hardcode this, should be double the value from the charge behavior
    base.specRigidbody.OnCollision += this.OnCollisionAfterBeingShot;
    RecreateParticleSystem(dir);
  }

  private void RecreateParticleSystem(Vector2 dir)
  {
    Setup();
    if (this._ps)
      DestroyParticleSystem();
    GameObject psObj = UnityEngine.Object.Instantiate(Slimybois.SlimeParticleSystem);
    psObj.transform.position = base.aiActor.sprite.WorldCenter;
    psObj.transform.parent   = base.gameObject.transform;
    psObj.transform.localRotation = dir.EulerZ();
    this._ps = psObj.GetComponent<ParticleSystem>();
    ParticleSystem.MainModule main = this._ps.main;
    main.startColor = this._slimeData.goopColor;
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
    float dtime = BraveTime.DeltaTime;

    // SlimyboiChargeBehavior charge = base.behaviorSpeculator.AttackBehaviors[0] as SlimyboiChargeBehavior;
    // IntVector2? maybeTarget = base.aiActor.Path?.Positions?.Last?.Value;
    // bool willReachTarget = base.aiActor.Path?.WillReachFinalGoal ?? false;
    // bool canPathOverPits = (base.aiActor.PathableTiles & CellTypes.PIT) == CellTypes.PIT;
    // float distance = maybeTarget.HasValue ? (maybeTarget.Value.ToVector2() - base.specRigidbody.UnitCenter).magnitude : 0.0f;
    // if (willReachTarget)
    //   base.aiActor.DebugNametag("");
    // else
    //   base.aiActor.DebugNametag($"target: {(base.aiActor.PlayerTarget is AIActor a ? a.AmmonomiconName() : "null")}\ntargetPos: {(maybeTarget.HasValue ? maybeTarget.Value.ToString() : "nowhere")}\nwillReachTarget: {willReachTarget}\ndistance: {(willReachTarget ? distance.ToString() : "n/a")}\npathOverPits: {canPathOverPits}");
    // base.specRigidbody.DrawDebugHitbox();

    HandleRenderSprite(dtime);
    HandleUnstick(dtime); // NOTE: fixes getting stuck in the Dragun's wings during the Dragun fight
    if (this._activelyBeingLaunched && !base.knockbackDoer.CheckSourceInKnockbacks(base.gameObject))
      HandleNoLongerFiredFromVacpack();
    UpdateTimers(dtime);
    UpdateSlimeSpecificBehaviors(dtime);
  }

  private void HandleUnstick(float dtime)
  {
    Vector2 lastPosition = this._lastPosition;
    this._lastPosition = base.specRigidbody.UnitCenter;
    if (this._lastPosition != lastPosition)
    {
      this._stuckTime = 0.0f;
      return;
    }
    if ((this._stuckTime += dtime) < _MAX_STUCK_TIME)
      return;
    // two more checks to see if we're really stuck: see if we're trying to go anywhere
    if (base.aiActor.Path is not Pathfinding.Path path || path.Positions == null || path.Positions.Last == null)
    {
      this._stuckTime = 0.0f;
      return;
    }
    PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(base.specRigidbody);
    this._stuckTime = 0.0f;
  }

  private void HandleRenderSprite(float dtime)
  {
    const float JUMP_HEIGHT = 1.25f;

    float now = BraveTime.ScaledTimeSinceStartup;
    if (!this._renderSprite)
    {
      if (!this._trueSprite || this._trueSprite.Collection != VFX.Collection)
        return; // NOTE: VFX.Collection check is necessary to make sure we've updated our sprite properly. if not, we have no valid sprite yet
      this._renderSprite = base.specRigidbody.DecoupleSpriteFromCollider();
      if (this.slimeType == SlimyboiType.Quantum)
      {
        this._renderSprite.usesOverrideMaterial = true;
        Material mat = this._renderSprite.renderer.material;
        mat.shader = CwaffShaders.WiggleShader;
        mat.SetFloat("_Amplitude", 0.001f);
        mat.SetFloat("_Distortion", 100.0f);
        mat.SetFloat("_Speed", 10.0f);
        mat.SetFloat("_Tearing", 0.0f);
        mat.SetFloat("_FadeSpeed", 10.0f);
        mat.SetFloat("_FadeAmp", 10.0f);
      }
      else if (this.slimeType == SlimyboiType.Glitch)
      {
        tk2dBaseSprite oldRenderSprite = this._renderSprite;
        this._renderSprite = Lazy.CreateMeshSpriteObject(oldRenderSprite, oldRenderSprite.WorldCenter, pointMesh: true);
        UnityEngine.Object.Destroy(oldRenderSprite.gameObject);
        this._renderSprite.usesOverrideMaterial = true;
        Material mat = this._renderSprite.renderer.material;
        mat.shader = CwaffShaders.ShatterShader;
        mat.SetFloat("_Progressive", 0.0f);
        mat.SetFloat(CwaffVFX._FadeId, 0.0f);
        // mat.SetFloat("_Amplitude", 0.125f);
        mat.SetFloat("_Amplitude", 0.0625f);
        mat.SetFloat(CwaffVFX._RandomSeedId, UnityEngine.Random.value);
      }
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
      if (this._queuedOutlineColor is Color color)
        SpriteOutlineManager.AddOutlineToSprite(this._renderSprite, color);
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
    if (this.slimeType == SlimyboiType.Dervish || this._flightTimer > 0.0f)
      renderPos.y += 0.125f * Mathf.Sin(12f * now);

    this._renderSprite.transform.position = renderPos.Quantize(0.0625f);

    // invulnerability flicker
    // TODO: this doesn't work quite right if other things disable our renderer
    if (this._projectileIframeTimer == this._iframeLength || Mathf.CeilToInt(this._projectileIframeTimer * 20.0f) % 2 == 0)
      this._renderSprite.renderer.enabled = true;
    else
      this._renderSprite.renderer.enabled = false;

  }

  private void UpdateSlimeSpecificBehaviors(float dtime)
  {
    switch (this.slimeType)
    {
      case SlimyboiType.Rad:
      {
        HandleGlow(color: new Color(0.5f, 0.85f, 0.1f), lightColor: null, flickerRate: 10.0f, brightness: 5.0f,
          glowColorPower: 10.0f, minGlow: 5.0f, maxGlow: 15.0f, sensitivity: 0.5f, minLightRadius: 1.0f, maxLightRadius: 3.0f);
        HandleAuraVisuals();
        HandleAuraEffect();
        break;
      }
      case SlimyboiType.Fire:
      {
        HandleGlow(color: new Color(0.9f, 0.1f, 0.1f), lightColor: null, flickerRate: 15.0f, brightness: 0.0f,
          glowColorPower: 8.0f, minGlow: 1.0f, maxGlow: 2.0f, sensitivity: 0.3f, minLightRadius: 1.0f, maxLightRadius: 3.0f);
        HandleAuraVisuals();
        HandleAuraEffect();
        break;
      }
      case SlimyboiType.Crystal:
      {
        float extraGlow = _REFLECT_GLOW_STRENGTH * (this._reflectGlowTimer / _REFLECT_GLOW_TIME);
        HandleGlow(color: new Color(0.6f, 0.8f, 0.8f), lightColor: null, flickerRate: 10.0f, brightness: 0.0f,
          glowColorPower: 15.0f, minGlow: extraGlow +0.1f, maxGlow: extraGlow + 3.1f, sensitivity: 0.5f, minLightRadius: 1.0f, maxLightRadius: 3.0f);
        break;
      }
      case SlimyboiType.Phosphor:
      {
        HandleAuraVisuals(radius: _HEAL_RADIUS);
        HandleGlow(color: new Color(1.0f, 1.0f, 0.8f), lightColor: null, flickerRate: 0.0f, brightness: 3.0f,
          glowColorPower: 8.0f, minGlow: 0.0f, maxGlow: 0.0f, sensitivity: 0.0f, minLightRadius: 5.0f, maxLightRadius: 5.0f);
        if (this._buffCooldownTimer == 0.0f || TickAndCheck(ref this._buffCooldownTimer, dtime))
        {
          this._buffCooldownTimer = _HEAL_COOLDOWN_TIME;
          foreach (SlimyboiController sloim in GetNearbySlimes(base.aiActor.CenterPosition, sqrRadius: _HEAL_RADIUS_SQR, shuffle: true))
            if (sloim.healthHaver.currentHealth < sloim.healthHaver.AdjustedMaxHealth && !sloim.attributes.IsSet(SlimyboiFlags.CantReceiveHealing))
            {
              ApplyHealing(sloim);
              break;
            }
        }
        break;
      }
      case SlimyboiType.Dervish:
      {
        HandleAuraVisuals();
        HandleAuraEffect();
        break;
      }
      case SlimyboiType.Tangle:
      {
        HandleVineAttack(dtime);
        break;
      }
      case SlimyboiType.Gold:
      {
        HandleAuraVisuals();
        HandleAuraEffect();
        break;
      }
      case SlimyboiType.Mosaic:
      {
        float extraGlow = _REFLECT_GLOW_STRENGTH * (this._reflectGlowTimer / _REFLECT_GLOW_TIME);
        HandleGlow(color: new Color(0.35f, 0.7f, 0.1f), lightColor: null, flickerRate: 20.0f, brightness: 0.0f,
          glowColorPower: 10.0f, minGlow: extraGlow + 1.0f, maxGlow: extraGlow + 10.0f, sensitivity: 0.7f, minLightRadius: 1.0f, maxLightRadius: 3.0f);
        break;
      }
      case SlimyboiType.Glitch:
      {
        if (this._renderSprite)
          this._renderSprite.renderer.material.SetFloat(CwaffVFX._RandomSeedId, UnityEngine.Random.value);
        break;
      }
    }
  }

  private void HandleVineAttack(float dtime)
  {
    Vector2 slimeCenter = this._renderSprite.WorldCenter;
    if (!this._vineMesh)
    {
      this._vineMesh = CwaffRopeMesh.Create(
        animation: Slimybois._SlimeVineVFX, startPos: slimeCenter, endPos: slimeCenter,
        numSegments: 8, stretchPolicy: CwaffingTheGungy.RopeSim.StretchPolicy.GROWTEMPORARY);
      this._vineMesh.sprite.HeightOffGround = -10f; // draw behind most things
    }
    if (TickAndCheck(ref this._vineTimer, dtime))
    {
      this._vineTarget = null;
      this._vineTargetPos = null;
    }
    if (this._vineTimer <= 0.0f)
    {
      this._vineTimer = _VINE_TOTAL_TIME;

      Projectile nearestProj = null;
      float nearest = _VINE_RADIUS_SQR;
      ReadOnlyCollection<Projectile> allProj = StaticReferenceManager.AllProjectiles;
      for (int j = allProj.Count - 1; j >= 0; j--)
      {
          Projectile p = allProj[j];
          if (!p || p.Owner is PlayerController)
              continue;

          Vector2 ppos = p.SafeCenter;
          Vector2 delta = ppos - slimeCenter;
          float sqrDist = delta.sqrMagnitude;
          if (sqrDist > nearest)
              continue;

          nearest = sqrDist;
          nearestProj = p;
      }
      this._vineDirection = Lazy.CoinFlip() ? 1.0f : -1.0f;
      this._vineTarget = nearestProj;
      this._vineTargetPos = nearestProj
        ? nearestProj.SafeCenter + (_VINE_ATTACK_TIME * nearestProj.m_currentSpeed) * nearestProj.m_currentDirection.normalized
        : null;
      if (nearestProj)
        base.gameObject.PlayOnce("slime_vine_sound");
    }
    if (this._vineTargetPos is not Vector2 targetPos)
    {
      this._vineMesh.sprite.renderer.enabled = false;
      return;
    }
    this._vineMesh.sprite.renderer.enabled = true;
    this._vineMesh.startPos = slimeCenter;

    float elapsed = _VINE_TOTAL_TIME - this._vineTimer;
    if (elapsed > _VINE_ATTACK_TIME && this._vineTarget)
    {
      this._vineTarget.DieInAir(false, true, true, true);
      this._vineTarget = null;
    }
    this._vineMesh.startPos = slimeCenter; // wiggle the vine around a bit
    if (elapsed < _VINE_ATTACK_TIME)
    {
      const float AMP = 2f; // amplitude of vine-weaving curve
      float ease = Ease.InOutQuad(elapsed / _VINE_ATTACK_TIME);
      Vector2 perp = (targetPos - slimeCenter).normalized.Rotate(this._vineDirection * 90f);
      this._vineMesh.endPos = Vector2.Lerp(slimeCenter, targetPos, ease);
      // magic to make the vine weave out and in
      this._vineMesh.endPos += (AMP * Mathf.Sin(ease * Mathf.PI)) * perp;
    }
    else if (elapsed > (_VINE_TOTAL_TIME - _VINE_RETRACT_TIME))
      this._vineMesh.endPos = Vector2.Lerp(targetPos, slimeCenter, Ease.InOutQuad((elapsed - (_VINE_TOTAL_TIME - _VINE_RETRACT_TIME)) / _VINE_RETRACT_TIME));
    else
      this._vineMesh.endPos = targetPos;
  }

  private void ApplyHealing(SlimyboiController sloim, float healAmount = _HEAL_AMOUNT, bool doSound = true)
  {
    sloim.healthHaver.ApplyHealing(healAmount);
    SpawnManager.SpawnVFX(VFX.MiniPickup, sloim.aiActor.CenterPosition, Lazy.RandomEulerZ());
    if (doSound)
      sloim.gameObject.PlayOnce("slime_heal_sound");
  }

  /// <summary>Tick down a timer and check if it's expired/</summary>
  private static bool TickAndCheck(ref float timer, float dtime)
  {
    if (timer <= 0.0f)
      return false; // was already expired
    timer -= dtime;
    if (timer > 0.0f)
      return false;
    timer = 0.0f;
    return true;
  }

  private void UpdateTimers(float dtime)
  {
    TickAndCheck(ref this._projectileIframeTimer, dtime);
    TickAndCheck(ref this._dodgeCooldown, dtime); // dodge cooldown
    TickAndCheck(ref this._reflectGlowTimer, dtime); // reflect glow cooldown
    if (TickAndCheck(ref this._immuneToPoisonTimer, dtime))
    {
      base.healthHaver.damageTypeModifiers.Remove(this._poisonImmunity);
      base.aiActor.SetResistance(EffectResistanceType.Poison, 0.0f);
    }
    if (TickAndCheck(ref this._immuneToFireTimer, dtime))
    {
      base.healthHaver.damageTypeModifiers.Remove(this._fireImmunity);
      base.aiActor.SetResistance(EffectResistanceType.Fire, 0.0f);
    }
    if (TickAndCheck(ref this._flightTimer, dtime))
    {
      base.aiActor.SetIsFlying(false, DERVISH_FLYING_REASON, adjustShadow: true, modifyPathing: true);
      if (this._chargeBehavior != null)
        this._chargeBehavior.m_cachedPathableTiles &= ~CellTypes.PIT;
      if (this._renderSprite)
        SpriteOutlineManager.RemoveOutlineFromSprite(this._renderSprite);
    }
    if (TickAndCheck(ref this._goldTimer, dtime))
    {
      EndGoldInvulnerability();
    }
    if (this.attributes.IsSet(SlimyboiFlags.PassiveHealthDrain) && TickAndCheck(ref this._healthDrainTimer, dtime))
    {
      this._healthDrainTimer = _HEALTH_DRAIN_RATE;
      if (slimeType == SlimyboiType.Quantum)
      {
        base.healthHaver.IsVulnerable = true;
        base.healthHaver.PreventAllDamage = false;
      }
      base.healthHaver.ApplyDamage(_HEALTH_DRAIN_AMOUNT, Vector2.zero, "Dehydration", CoreDamageTypes.None,
        DamageCategory.DamageOverTime, ignoreInvulnerabilityFrames: true);
      if (slimeType == SlimyboiType.Quantum)
      {
        base.healthHaver.IsVulnerable = false;
        base.healthHaver.PreventAllDamage = true;
      }
    }
  }

  private void BeginGoldInvulnerability()
  {
    base.healthHaver.IsVulnerable = false;
    base.healthHaver.PreventAllDamage = true;
    if (this._renderSprite)
      SpriteOutlineManager.AddOutlineToSprite(this._renderSprite, ExtendedColours.paleYellow);
    else
      this._queuedOutlineColor = ExtendedColours.paleYellow;
  }

  private void EndGoldInvulnerability()
  {
    base.healthHaver.IsVulnerable = true;
    base.healthHaver.PreventAllDamage = false;
    if (this._renderSprite)
      SpriteOutlineManager.RemoveOutlineFromSprite(this._renderSprite);
  }

  private void HandleGlow(Color color, Color? lightColor, float flickerRate, float brightness, float glowColorPower, float minGlow, float maxGlow, float sensitivity,
    float minLightRadius, float maxLightRadius)
  {
    if (!this._renderSprite)
      return;

    float glowamount = Mathf.Abs(Mathf.Sin(flickerRate * BraveTime.ScaledTimeSinceStartup));
    if (maxGlow > 0.0f)
    {
      this._renderSprite.MakeGlowyBetter(glowAmount: minGlow + (maxGlow - minGlow) * glowamount, glowColor: color,
        glowColorPower: glowColorPower, skipSetup: this._didGlowSetup, sensitivity: sensitivity);
      this._didGlowSetup = true;
    }
    if (brightness > 0.0f)
    {
      if (!this._light)
        this._light = EasyLight.Create(base.aiActor.CenterPosition, base.transform, lightColor ?? color, radius: 0f, brightness: brightness);
      this._light.SetRadius(minLightRadius + (maxLightRadius - minLightRadius) * glowamount);
    }
  }

  private static readonly List<SlimyboiController> _TempSlimes = new();
  private static readonly ReadOnlyCollection<SlimyboiController> _TempSlimesRO = new(_TempSlimes);
  private static ReadOnlyCollection<SlimyboiController> GetNearbySlimes(Vector2 pos, float sqrRadius = _AURA_RADIUS_SQR, bool shuffle = false)
  {
    _TempSlimes.Clear();

    float x = pos.x;
    float y = pos.y;
    foreach (SlimyboiController sloim in SlimyboiManager.ActiveSlimes)
    {
      if (!sloim)
        continue;
      Vector2 opos = sloim.aiActor.CenterPosition;
      float dx = x - opos.x;
      float dy = y - opos.y;
      if ((dx * dx + dy * dy) <= sqrRadius)
        _TempSlimes.Add(sloim);
    }
    if (shuffle)
      _TempSlimes.Shuffle();

    return _TempSlimesRO;
  }

  private void HandleAuraVisuals(float radius = _AURA_RADIUS)
  {
    Vector2 pos = base.aiActor.CenterPosition;
    if (!this._aura)
    {
      this._aura = Geometry.Create(Geometry.Shape.RING);
      this._aura.Place(pos: pos, color: this._slimeData.goopColor.WithAlpha(0.05f), radius: radius, radiusInner: radius - _AURA_THICKNESS);
    }
    this._aura.Place(pos: pos);
  }

  private void HandleAuraEffect(float radius = _AURA_RADIUS)
  {
    foreach (SlimyboiController sloim in GetNearbySlimes(base.aiActor.CenterPosition, sqrRadius: radius * radius, shuffle: false))
      if (sloim != this)
        ApplyAuraEffect(sloim);
  }

  private void ApplyAuraEffect(SlimyboiController sloim)
  {
    switch (this.slimeType)
    {
      case SlimyboiType.Rad:
      {
        if (sloim.attributes.IsSet(SlimyboiFlags.PoisonImmunity))
          break;
        if (sloim._immuneToPoisonTimer <= 0.0f)
        {
          sloim.healthHaver.damageTypeModifiers.Add(this._poisonImmunity);
          sloim.aiActor.SetResistance(EffectResistanceType.Poison, 1.0f);
          if (sloim.aiActor.GetEffectBetter(EffectResistanceType.Poison) is GameActorEffect activePoison)
            sloim.aiActor.RemoveEffect(activePoison);
        }
        sloim._immuneToPoisonTimer = _AURA_PERSIST_TIME;
        break;
      }
      case SlimyboiType.Fire:
      {
        if (sloim.attributes.IsSet(SlimyboiFlags.FireImmunity))
          break;
        if (sloim._immuneToFireTimer <= 0.0f)
        {
          sloim.healthHaver.damageTypeModifiers.Add(this._fireImmunity);
          sloim.aiActor.SetResistance(EffectResistanceType.Fire, 1.0f);
          if (sloim.aiActor.GetEffectBetter(EffectResistanceType.Fire) is GameActorEffect activeFire)
            sloim.aiActor.RemoveEffect(activeFire);
        }
        sloim._immuneToFireTimer = _AURA_PERSIST_TIME;
        break;
      }
      case SlimyboiType.Dervish:
      {
        if (sloim.attributes.IsSet(SlimyboiFlags.CanFly))
          break;

        if (sloim._flightTimer <= 0.0f)
        {
          sloim.aiActor.SetIsFlying(true, DERVISH_FLYING_REASON, adjustShadow: true, modifyPathing: true);
          if (sloim._chargeBehavior != null)
            sloim._chargeBehavior.m_cachedPathableTiles |= CellTypes.PIT;
          if (sloim._renderSprite)
            SpriteOutlineManager.AddOutlineToSprite(sloim._renderSprite, Color.white);
          else
            sloim._queuedOutlineColor = Color.white;
        }
        sloim._flightTimer = _AURA_PERSIST_TIME;
        break;
      }
      case SlimyboiType.Gold:
      {
        if (sloim.slimeType == SlimyboiType.Gold)
          break;

        if (sloim._goldTimer <= 0.0f)
          sloim.BeginGoldInvulnerability();
        sloim._goldTimer = _AURA_PERSIST_TIME;
        break;
      }
    }
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
      bool explosiveAttacks = this.attributes.IsSet(SlimyboiFlags.ExplosiveAttacks);
      CwaffVFX.Spawn(
        prefab           : explosiveAttacks ? Slimybois._SlimeExplodeImpactVFX : Slimybois._SlimeImpactVFX, // TODO: better particle
        position         : data.Contact + Lazy.RandomVector(0.25f),
        velocity         : explosiveAttacks ? Vector2.zero : vfxAngle.ToVector(UnityEngine.Random.Range(5.0f, 8.0f)),
        rotation         : vfxAngle.EulerZ(),
        height           : 8.0f,
        copyShaders      : true
        );

      // NOTE: attempt to prevent getting stuck inside enemies and dealing ridiculous damage
      base.specRigidbody.RegisterTemporaryCollisionException(data.OtherRigidbody, 0.01f);
      base.gameObject.Play(explosiveAttacks ? "slime_explode_attack_sound" : "slime_attack_sound");
      base.knockbackDoer.ApplyKnockback(data.Normal, knockback, time: this._jumpDuration);
      Jump();

      if (data.OtherRigidbody is SpeculativeRigidbody otherBody && otherBody.aiActor is AIActor enemy)
      {
        if (this.attributes.IsSet(SlimyboiFlags.AttacksPoison))
          enemy.ApplyEffect(Slimybois._SlimePoisonEffect);
        if (this.attributes.IsSet(SlimyboiFlags.AttacksIgnite))
          enemy.ApplyEffect(Slimybois._SlimeFireEffect);
        if (this.attributes.IsSet(SlimyboiFlags.AttacksSlow))
          enemy.ApplyEffect(Slimybois._SlimeSlowEffect);
        if (this.attributes.IsSet(SlimyboiFlags.ExtraCasingOnKill))
          enemy.gameObject.GetOrAddComponent<LuckyCasingDrop>();
        if (this.attributes.IsSet(SlimyboiFlags.AttacksHealAllies))
          DoGroupHeal();
      }
  }

  private void DoGroupHeal()
  {
    float healingLeft = _MAX_GROUP_HEAL;
    bool didHealing = false;
    foreach (SlimyboiController sloim in GetNearbySlimes(base.aiActor.CenterPosition, sqrRadius: _HEAL_RADIUS_SQR, shuffle: false))
    {
      if (sloim.attributes.IsSet(SlimyboiFlags.CantReceiveHealing))
        continue;
      float missingHealth = sloim.healthHaver.AdjustedMaxHealth - sloim.healthHaver.currentHealth;
      if (missingHealth <= 0.01f)
        continue;
      float healAmount = Mathf.Min(missingHealth, healingLeft);
      healingLeft -= healAmount;
      ApplyHealing(sloim, healAmount: healAmount, doSound: false);
      didHealing = true;
      if (healingLeft < 0.01f)
        break;
    }
    if (didHealing)
    {
      base.gameObject.PlayUnique("slime_mosaic_heal_sound");
      this._reflectGlowTimer = _REFLECT_GLOW_TIME;
    }
  }

  public class LuckyCasingDrop : MonoBehaviour
  {
    private void Start()
    {
      if (base.gameObject.GetComponent<AIActor>() is not AIActor enemy)
        return;

      base.gameObject.PlayUnique("slime_coin_sound");
      LootEngine.SpawnCurrency(enemy.CenterPosition, 1, false, null, null);
    }
  }

  private void OnDeath(Vector2 vector)
  {
    DestroyParticleSystem(true);

    if (base.aiActor.StealthDeath)
      return;

    if (this.attributes.IsSet(SlimyboiFlags.ExplodesOnDeath))
      Exploder.DoDefaultExplosion(base.aiActor.CenterPosition, Vector2.zero, ignoreQueues: true, ignoreDamageCaps: false);

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
    if (this._vineMesh)
      UnityEngine.Object.Destroy(this._vineMesh.gameObject);
    if (this._aura)
      UnityEngine.Object.Destroy(this._aura.gameObject);
    if (this._renderSprite)
      UnityEngine.Object.Destroy(this._renderSprite.gameObject);
    base.OnDestroy();
  }

  private void HandleProjetileDodge(Projectile proj)
  {
    this._projectileIframeTimer = _DODGE_IFRAME_LENGTH;
    this._dodgeCooldown = _DODGE_COOLDOWN;
    this._dodgeVector = _DODGE_LENGTH * proj.Direction.normalized.Rotate(UnityEngine.Random.Range(90f, 270f));
    this._queuedDodge = true;
  }

  private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
  {
    if (this.attributes.IsSet(SlimyboiFlags.QuantumInstability))
    {
      if (!otherRigidbody.aiActor || otherRigidbody.aiActor.gameObject.GetComponent<SlimyboiController>())
        PhysicsEngine.SkipCollision = true;
      return;
    }
    if (otherRigidbody.projectile is Projectile proj)
    {
      if (this.attributes.IsSet(SlimyboiFlags.AbsorbsBullets))
      {
        PhysicsEngine.SkipCollision = true;
        if (proj.isActiveAndEnabled && proj.Owner is not PlayerController)
        {
          proj.DieInAir(false, true, true, true);
          ApplyHealing(this, _HEALTH_FROM_BULLETS);
          this._healthDrainTimer = _HEALTH_DRAIN_RATE;
        }
      }
      else if (this.attributes.IsSet(SlimyboiFlags.ReflectsProjectiles))
      {
        PhysicsEngine.SkipCollision = true;
        if (proj.isActiveAndEnabled && proj.Owner is not PlayerController)
        {
          base.gameObject.PlayUnique("slime_reflect_sound");
          PassiveReflectItem.ReflectBullet(proj, retargetReflectedBullet: false, newOwner: this._owner, minReflectedBulletSpeed: 25.0f);
          proj.Direction = (proj.SafeCenter - myRigidbody.UnitCenter).normalized;
          this._reflectGlowTimer = _REFLECT_GLOW_TIME;
        }
      }
      else if (this.attributes.IsSet(SlimyboiFlags.ProjectileImmunity))
      {
        PhysicsEngine.SkipCollision = true;
        if (proj.isActiveAndEnabled && proj.Owner is not PlayerController)
          proj.DieInAir(false, true, true, true);
      }
      else if (this._projectileIframeTimer > 0)
        PhysicsEngine.SkipCollision = true; // don't collide with projectiles while iframes are active
      else if (proj.Owner is PlayerController playerOwner && playerOwner && !base.aiActor.CanTargetPlayers)
        PhysicsEngine.SkipCollision = true; // don't collide with player projectiles when allied
      else if (this._dodgeCooldown <= 0.0f && this.attributes.IsSet(SlimyboiFlags.DodgesProjectiles))
      {
        PhysicsEngine.SkipCollision = true;
        HandleProjetileDodge(proj);
      }
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
    if (this.attributes.IsSet(SlimyboiFlags.ImmuneToMovingTraps))
    {
      if (otherRigidbody.gameObject.GetComponent<PathingTrapController>() is PathingTrapController)
      {
        PhysicsEngine.SkipCollision = true; // don't collide with traps
        return;
      }
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
    PixelCollider targetCollider = m_aiActor.TargetRigidbody.GetPixelCollider(ColliderType.HitBox);
    if (targetCollider == null)
      return BehaviorResult.Continue;

    // NOTE: better tracking using closest point on collider rather than center (fixes tracking issues with large enemies like Bullet King)
    Vector2 targetCenter = myCenter.ClosestPointOnCollider(targetCollider);
    if (leadAmount > 0f)
    {
      Vector2 vector2 = targetCenter + m_aiActor.TargetRigidbody.Velocity * 0.75f;
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

// mostly the same as SeekTargetBehavior, but uses better distance metrics and enforces a minimum distance to the target
public class SlimyboiSeekBehavior : SeekTargetBehavior
{
  public float CustomMinRange = 0.0f;

  public override BehaviorResult Update()
  {
    if (m_aiActor.TargetRigidbody is not SpeculativeRigidbody targetRigidbody)
    {
      if (m_state == State.PathingToTarget)
      {
        m_aiActor.ClearPath();
        m_state = State.Idle;
      }
      else if (m_state == State.ReturningToSpawn && m_aiActor.PathComplete)
        m_state = State.Idle;
      return BehaviorResult.Continue;
    }

    Vector2 myCenter = m_aiActor.specRigidbody.UnitCenter;
    PixelCollider targetCollider = targetRigidbody.GetPixelCollider(ColliderType.HitBox);
    Vector2 targetCenter = targetCollider != null ? myCenter.ClosestPointOnCollider(targetCollider) : targetRigidbody.UnitCenter;
    Vector2 targetDelta = targetCenter - myCenter;
    float targetSqrDist = targetDelta.sqrMagnitude;

    m_state = State.PathingToTarget;
    if (ExternalCooldownSource)
    {
      m_aiActor.ClearPath();
      return BehaviorResult.Continue;
    }
    if (targetSqrDist < (CustomMinRange * CustomMinRange))
    {
      if (m_repathTimer <= 0f)
        m_aiActor.PathfindToPosition(targetRigidbody.UnitCenter - (CustomMinRange + 0.5f) * targetDelta.normalized, null, true, null);
      return BehaviorResult.SkipRemainingClassBehaviors;
    }
    if (StopWhenInRange && targetSqrDist <= (CustomRange * CustomRange) && (!LineOfSight || m_aiActor.HasLineOfSightToTarget || (targetRigidbody.aiActor && !targetRigidbody.CollideWithOthers)))
    {
      m_aiActor.ClearPath();
      return BehaviorResult.Continue;
    }
    if (m_repathTimer <= 0f)
    {
      m_aiActor.PathfindToPosition(targetCenter, null, true, null);
      m_repathTimer = PathInterval;
    }
    return BehaviorResult.SkipRemainingClassBehaviors;
  }
}

public class SlimyboiFollowPlayerBehavior : MovementBehaviorBase
{
  public float PathInterval = 0.5f;
  public float ComfyDistanceToPlayer = 3.0f;
  public float MaxDistanceToPlayer = 5.0f;

  private SlimyboiController _sloim;
  private PlayerController _owner;
  private float m_repathTimer;
  private bool _wasCloseToPlayer = false;

  public override void Init(GameObject gameObject, AIActor aiActor, AIShooter aiShooter)
  {
    base.Init(gameObject, aiActor, aiShooter);
    this._sloim = gameObject.GetComponent<SlimyboiController>();
    this._owner = this._sloim ? this._sloim._owner : null;
  }

  public override void Upkeep()
  {
    base.Upkeep();
    DecrementTimer(ref m_repathTimer);
  }

  public override BehaviorResult Update()
  {
    if (!this._sloim || !this._owner)
      return BehaviorResult.Continue;

    Vector2 ownerPos = this._owner.CenterPosition;
    Vector2 myPos = m_aiActor.CenterPosition;
    float sqrDistToPlayer = (ownerPos - myPos).sqrMagnitude;

    if (sqrDistToPlayer < (this._wasCloseToPlayer ? (MaxDistanceToPlayer * MaxDistanceToPlayer) : (ComfyDistanceToPlayer * ComfyDistanceToPlayer)))
    {
      this._wasCloseToPlayer = true;
      m_aiActor.ClearPath();
      return BehaviorResult.Continue;
    }

    this._wasCloseToPlayer = false;
    if (m_repathTimer <= 0f)
    {
      m_aiActor.PathfindToPosition(ownerPos, null, true, null);
      m_repathTimer = PathInterval;
    }

    return BehaviorResult.SkipRemainingClassBehaviors;
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
    if (target is not AIActor || target.specRigidbody is not SpeculativeRigidbody targetRigidbody)
      return false;

    Vector2 myPos = m_specRigidbody.UnitCenter;
    PixelCollider targetCollider = targetRigidbody.GetPixelCollider(ColliderType.HitBox);
    Vector2 epos = targetCollider != null ? myPos.ClosestPointOnCollider(targetCollider) : targetRigidbody.UnitCenter;
    float sqrdist = (myPos - epos).sqrMagnitude;
    bool targetMovedFarAway = m_prevDistToTarget + 9f < sqrdist;
    m_prevDistToTarget = sqrdist;
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
            if (candidate.specRigidbody is not SpeculativeRigidbody targetRigidbody)
                continue;

            PixelCollider targetCollider = targetRigidbody.GetPixelCollider(ColliderType.HitBox);
            Vector2 epos = targetCollider != null ? myPos.ClosestPointOnCollider(targetCollider) : targetRigidbody.UnitCenter;
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

public class SlimyboiImmobileInCombatBehavior : OverrideBehaviorBase
{
  private SlimyboiController _sloim;
  private PlayerController _owner;

  public override void Init(GameObject gameObject, AIActor aiActor, AIShooter aiShooter)
  {
    base.Init(gameObject, aiActor, aiShooter);
    this._sloim = gameObject.GetComponent<SlimyboiController>();
    this._owner = this._sloim ? this._sloim._owner : null;
  }

  public override BehaviorResult Update()
  {
    if (!this._owner || !this._owner.IsInCombat || !this._sloim.attributes.IsSet(SlimyboiFlags.ImmobileInCombat) )
      return BehaviorResult.Continue;
    return BehaviorResult.SkipAllRemainingBehaviors;
  }
}

public class SlimyboiDodgeBehavior : OverrideBehaviorBase
{
  private const float _DODGE_TIME = 0.2f;
  private const int _NUM_AFTERIMAGES = 4;

  private SlimyboiController _sloim;
  private int _afterimagesDone;
  private float _dodgeStartTime;
  private float _dodgeEndTime;

  public override void Init(GameObject gameObject, AIActor aiActor, AIShooter aiShooter)
  {
    base.Init(gameObject, aiActor, aiShooter);
    this._sloim = gameObject.GetComponent<SlimyboiController>();
  }

  public override BehaviorResult Update()
  {
    if (!this._sloim || !this._sloim._queuedDodge)
      return BehaviorResult.Continue;

    this._sloim._queuedDodge = false;
    this._afterimagesDone = 0;
    this._dodgeStartTime = BraveTime.ScaledTimeSinceStartup;
    this._dodgeEndTime = this._dodgeStartTime + _DODGE_TIME;
    m_aiActor.gameObject.PlayUnique("slime_dodge_sound");
    SpawnManager.SpawnVFX(GameManager.Instance.Dungeon.dungeonDustups.rollLandDustup, m_aiActor.CenterPosition, Lazy.RandomEulerZ());
    m_aiActor.BehaviorOverridesVelocity = true;
    m_aiActor.BehaviorVelocity = (1f / _DODGE_TIME) * this._sloim._dodgeVector;
    m_updateEveryFrame = true;
    return BehaviorResult.RunContinuous;
  }

  public override ContinuousBehaviorResult ContinuousUpdate()
  {
    float now = BraveTime.ScaledTimeSinceStartup;
    if (now >= this._dodgeEndTime)
      return ContinuousBehaviorResult.Finished;

    float percentDone = (now - this._dodgeStartTime) / _DODGE_TIME;
    int afterimageNum = Mathf.FloorToInt(percentDone * _NUM_AFTERIMAGES);
    if (afterimageNum > this._afterimagesDone)
      this._sloim._renderSprite.DuplicateInWorld().gameObject.ExpireIn(0.2f, 0.2f, 0.5f);
    this._afterimagesDone = afterimageNum;

    return ContinuousBehaviorResult.Continue;
  }

  public override void EndContinuousUpdate()
  {
    base.EndContinuousUpdate();
    m_aiActor.BehaviorOverridesVelocity = false;
    m_aiActor.BehaviorVelocity = Vector2.zero;
    m_updateEveryFrame = false;
  }
}
