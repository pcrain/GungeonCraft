namespace CwaffingTheGungy;

using static SlimyboiFlags;

/* TODO:
    - allow enemies to target slimes (chance of temporary override)
    - unique particles / vfx / sounds / potentially goops?
    - only use override sprite renderer when attacking
*/

public static class Slimybois
{
  internal const float _DEFAULT_COOLDOWN = 2.0f;
  internal const int _BASE_HEALTH = 10;
  internal const float _BASE_DAMAGE = 0.6f; //NOTE: chargeDamage multiplied by 5 on enemies for some reason
  internal const float _BASE_ATTACK_RANGE = 3.0f;
  internal const float _BASE_ATTACK_KB = 5.0f;
  internal const float _BASE_WEIGHT = 40.0f;
  internal const float _BASE_SPEED = 4.5f;

  public static readonly int NumSlimes = Enum.GetNames(typeof(SlimyboiType)).Length;
  public static SlimeData[] SlimeData = null;
  public static GameObject SlimeParticleSystem = null;

  internal static GameObject _SlimeDeathVFX;
  internal static GameObject _SlimeImpactVFX;
  internal static GameObject _SlimeExplodeImpactVFX;
  internal static tk2dSpriteAnimationClip _SlimeVineVFX;

  internal static GameActorEffect _SlimePoisonEffect;
  internal static GameActorEffect _SlimeFireEffect;
  internal static GameActorEffect _SlimeSlowEffect;

  public static void Init()
  {
    // set up array
    SlimeData = new SlimeData[NumSlimes];

    // set up individual defs
    SlimeData.SetupEntry(new(){ type = SlimyboiType.Quicksilver, overrideAttackCooldown = 0.25f * _DEFAULT_COOLDOWN,
      overrideSpeed = _BASE_SPEED * 2.5f, goopColor = Color.white });
    SlimeData.SetupEntry(new(){ type = SlimyboiType.Dervish, flags = CanFly, goopColor = Color.gray,
      overrideSpeed = _BASE_SPEED * 2.0f, overrideAttackCooldown = 0.5f * _DEFAULT_COOLDOWN});
    SlimeData.SetupEntry(new(){ type = SlimyboiType.Phosphor, flags = CanFly | FullStatusImmunity, goopColor = Color.cyan,
      overrideContactDamage = 0.2f, overrideHealth = _BASE_HEALTH / 2});
    SlimeData.SetupEntry(new(){ type = SlimyboiType.Pink, goopColor = Color.magenta });
    SlimeData.SetupEntry(new(){ type = SlimyboiType.Hunter, flags = DodgesProjectiles });
    SlimeData.SetupEntry(new(){ type = SlimyboiType.Rad, goopColor = ExtendedColours.lime, flags = AttacksPoison | PoisonImmunity });
    SlimeData.SetupEntry(new(){ type = SlimyboiType.Fire, goopColor = Color.red, flags = AttacksIgnite | FireImmunity });
    SlimeData.SetupEntry(new(){ type = SlimyboiType.Crystal, goopColor = Color.blue, overrideWeight = _BASE_WEIGHT * 4f,
      flags = ImmobileInCombat | ReflectsProjectiles | ImmuneToMovingTraps });
    SlimeData.SetupEntry(new(){ type = SlimyboiType.Tangle, goopColor = Color.green, overrideHealth = _BASE_HEALTH * 2 });

    SlimeData.SetupEntry(new(){ type = SlimyboiType.Rock, goopColor = ExtendedColours.darkBrown, overrideHealth = _BASE_HEALTH * 5,
      overrideSpeed = _BASE_SPEED * 0.5f, overrideAttackCooldown = _DEFAULT_COOLDOWN * 2.0f, overrideWeight = _BASE_WEIGHT * 2f });
    SlimeData.SetupEntry(new(){ type = SlimyboiType.Saber, goopColor = ExtendedColours.brown,
      overrideSpeed = _BASE_SPEED * 0.75f, overrideAttackCooldown = _DEFAULT_COOLDOWN / 1.5f, overrideContactDamage = _BASE_DAMAGE * 1.5f });
    SlimeData.SetupEntry(new(){ type = SlimyboiType.Honey, goopColor = Color.yellow, overrideSpeed = _BASE_SPEED * 0.5f, flags = AttacksSlow });
    SlimeData.SetupEntry(new(){ type = SlimyboiType.Boom, goopColor = ExtendedColours.vibrantOrange, flags = ExplodesOnDeath | ExplosiveAttacks,
      overrideContactDamage = _BASE_DAMAGE * 4.0f });

    // pad out unfinished defs
    foreach (SlimyboiType t in Enum.GetValues(typeof(SlimyboiType)))
      if (SlimeData[(int)t] == null)
        SlimeData.SetupEntry(new SlimeData{ type = t }); // TODO: make them unique

    // shared
    _SlimeImpactVFX = VFX.Create("slime_impact_vfx", fps: 60, loops: false);
    _SlimeImpactVFX.GetComponent<tk2dSprite>().MakeGlowyBetter(glowAmount: 5.0f, glowColorPower: 5.0f, glowColor: Color.white);
    _SlimeExplodeImpactVFX = VFX.Create("slime_explode_impact_vfx", fps: 30, loops: false);
    _SlimeExplodeImpactVFX.GetComponent<tk2dSprite>().MakeGlowyBetter(glowAmount: 15.0f, glowColorPower: 5.0f, glowColor: Color.white);
    _SlimeDeathVFX = VFX.Create("slime_death_vfx");
    _SlimeDeathVFX.GetComponent<tk2dSprite>().MakeGlowyBetter(glowAmount: 100.0f, glowColorPower: 100.0f, glowColor: Color.white);
    _SlimeVineVFX = VFX.Create("slime_vine_vfx", fps: 20).DefaultAnimation();
    SlimeParticleSystem = MakeSlimeParticleSystem(Color.white);
    _SlimePoisonEffect = ItemHelper.Get(Items.IrradiatedLead).GetComponent<BulletStatusEffectItem>().HealthModifierEffect;
    _SlimeFireEffect = ItemHelper.Get(Items.HotLead).GetComponent<BulletStatusEffectItem>().FireModifierEffect;
    _SlimeSlowEffect = Items.TripleCrossbow.AsGun().DefaultModule.projectiles[0].speedEffect;
  }

  private static SlimeData Init(this SlimeData sd)
  {
    if (string.IsNullOrEmpty(sd.slimeName))
      sd.slimeName = Enum.GetName(typeof(SlimyboiType), sd.type).ToLower();
    sd.fullName = sd.slimeName.ToTitleCaseInvariant() + " Slime";
    AIActor actor = $"Slime {sd.slimeName}".InitEnemy(health: sd.overrideHealth ?? _BASE_HEALTH, baseFps: 12, doCorpse: false,
      bodyDims: new IntVector2(16, 8), useUntrimmedBounds: true);
    actor.procedurallyOutlined       = false; // TODO: remove outlines from sprites later
    actor.MovementSpeed              = sd.overrideSpeed ?? _BASE_SPEED;
    actor.CollisionDamage            = 0.0f; // Overridden by SlimyboiChargeBehavior at charge time
    actor.CollisionKnockbackStrength = 0.0f; // slimes do no knockback by default
    if (sd.flags.IsSet(CanFly))
      actor.PathableTiles |= CellTypes.PIT;
    if (sd.flags.IsSet(FullStatusImmunity))
      actor.ImmuneToAllEffects = true;

    float attackRange = sd.overrideAttackRange ?? _BASE_ATTACK_RANGE;
    BehaviorSpeculator bs = actor.gameObject.GetComponent<BehaviorSpeculator>();
    bs.InstantFirstTick = true; // NOTE: fixes delays between being able to attack after spawning in
    bs.OverrideBehaviors.Add(new SlimyboiImmobileInCombatBehavior(){ });
    bs.OverrideBehaviors.Add(new SlimyboiDodgeBehavior(){ });
    bs.TargetBehaviors.Add(new SlimyboiTargetingBehavior(){ Radius = 35.0f, LineOfSight = false, ObjectPermanence = false });
    bs.MovementBehaviors.Add(new SlimyboiSeekBehavior(){ StopWhenInRange = true, CustomMinRange = 1.75f, CustomRange = 2.75f, PathInterval = 0.5f });
    bs.MovementBehaviors.Add(new MoveErraticallyBehavior { PathInterval = 0.5f, StayOnScreen = false, UseTargetsRoom = false, AvoidTarget = false });
    bs.AttackBehaviors.Add(new SlimyboiChargeBehavior(){ chargeDamage = sd.overrideContactDamage ?? _BASE_DAMAGE, chargeKnockback = _BASE_ATTACK_KB, chargeSpeed = 30.0f,
      minRange = 0.0f, maxRange = attackRange, Cooldown = sd.overrideAttackCooldown ?? _DEFAULT_COOLDOWN, maxChargeDistance = attackRange + 0.5f });

    KnockbackDoer kbd = actor.gameObject.GetComponent<KnockbackDoer>();
    kbd.weight = sd.overrideWeight ?? _BASE_WEIGHT;

    AIAnimator aiAnim = actor.gameObject.GetComponent<AIAnimator>();
    aiAnim.facingType = AIAnimator.FacingType.Movement;

    // GoopDoer gd = actor.gameObject.AddComponent<GoopDoer>();
    // gd.goopDefinition     = EasyGoopDefinitions.BlobulonGoopDef;
    // gd.positionSource     = GoopDoer.PositionSource.HitBoxCenter;
    // gd.updateTiming       = GoopDoer.UpdateTiming.Always;
    // gd.updateFrequency    = 0.05f;
    // gd.defaultGoopRadius  = 1f;
    // gd.radiusMin          = 0.25f;
    // gd.radiusMax          = 0.75f;

    DebrisObject goopDebris = BreakableAPIToolbox.GenerateDebrisObject(
      shardSpritePath         : $"slime_debris",
      debrisObjectsCanRotate  : false,
      LifeSpanMin             : 1.0f,
      LifeSpanMax             : 2.0f,
      AngularVelocity         : 0,
      AngularVelocityVariance : 0,
      DebrisBounceCount       : 1,
      DoesGoopOnRest          : true,
      GoopType                : EasyGoopDefinitions.BlobulonGoopDef,
      GoopRadius              : 0.5f,
      Mass                    : 1.0f);
    goopDebris.decayOnBounce = 0.8f;
    goopDebris.usesLifespan = true;

    SlimyboiController controller = actor.gameObject.AddComponent<SlimyboiController>();
    controller.slimeType = sd.type;
    controller.attributes = sd.flags;

    // finish initializing SlimeData
    sd.debris = goopDebris.gameObject;
    sd.prefab = actor;
    return sd;
  }

  public static SlimeData Data(this SlimyboiType type)
  {
    return SlimeData[(int)type];
  }

  public static void SetupEntry(this SlimeData[] slimeData, SlimeData sd)
  {
    slimeData[(int)sd.type] = sd.Init();
  }

  public static bool IsSet(this SlimyboiFlags flags, SlimyboiFlags flag)
  {
    return (flags & flag) == flag;
  }

  public static bool Attribute(this SlimyboiController sloim, SlimyboiFlags flag)
  {
    return (sloim.attributes & flag) == flag;
  }

  private static GameObject MakeSlimeParticleSystem(Color particleColor)
  {
      GameObject psBasePrefab = Items.CombinedRifle.AsGun().alternateVolley.projectiles[0].projectiles[0].GetComponent<CombineEvaporateEffect>().ParticleSystemToSpawn;
      GameObject psnewPrefab = UnityEngine.Object.Instantiate(psBasePrefab).RegisterPrefab();
      //NOTE: look at CombineSparks.prefab for reference
      //NOTE: uses shader https://github.com/googlearchive/soundstagevr/blob/master/Assets/third_party/Sonic%20Ether/Shaders/SEParticlesAdditive.shader
      ParticleSystem ps = psnewPrefab.GetComponent<ParticleSystem>();

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
      vcurve.AddKey(0.0f, 1.0f);
      vcurve.AddKey(0.5f, 0.1f);
      vcurve.AddKey(1.0f, 0.0f);
      vel.x = vel.y = vel.z = new ParticleSystem.MinMaxCurve(40.0f, vcurve);
      vel.xMultiplier = vel.yMultiplier = vel.zMultiplier = 1.0f;
      vel.yMultiplier = 0.0f;

      ParticleSystem.RotationOverLifetimeModule rotl = ps.rotationOverLifetime;
      rotl.enabled = false;

      ParticleSystem.RotationBySpeedModule rots = ps.rotationBySpeed;
      rots.enabled = false;

      Gradient g = new Gradient();
      g.SetKeys(
          new GradientColorKey[] {},
          new GradientAlphaKey[] { new GradientAlphaKey(1f, 0.0f), new GradientAlphaKey(0.5f, 0.25f), new GradientAlphaKey(0.15f, 0.5f),  new GradientAlphaKey(0.01f, 0.75f), new GradientAlphaKey(0.01f, 1.0f) }
      );
      ParticleSystem.ColorOverLifetimeModule colm = ps.colorOverLifetime;
      colm.color = new ParticleSystem.MinMaxGradient(g); // looks jank

      ParticleSystem.EmissionModule em = ps.emission;
      em.rateOverTime = 42f;

      ParticleSystemRenderer psr = psnewPrefab.GetComponent<ParticleSystemRenderer>();
      psr.sortingLayerName = "Background";
      psr.material.SetFloat("_InvFade", 3.0f);
      psr.material.SetFloat("_EmissionGain", 0.75f);
      psr.material.SetColor("_EmissionColor", Color.white);
      psr.material.SetColor("_DiffuseColor", Color.white);

      ParticleSystem.SizeOverLifetimeModule psz = ps.sizeOverLifetime;
      psz.enabled = true;
      AnimationCurve sizeCurve = new AnimationCurve();
      sizeCurve.AddKey(0.0f, 1.0f);
      sizeCurve.AddKey(0.75f, 1.0f);
      sizeCurve.AddKey(1.0f, 0.0f);
      psz.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);

      ParticleSystem.ShapeModule shape = ps.shape;
      shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
      shape.randomDirectionAmount = 0.1f;
      shape.alignToDirection = false;
      shape.scale           = Vector3.one;
      shape.radiusThickness = 1.0f;
      shape.radiusMode      = ParticleSystemShapeMultiModeValue.Random;
      shape.length          = 16f;
      shape.radius          = 0.5f;
      // shape.rotation        = new Vector3(0.0f, 90.0f, 90.0f);
      shape.rotation        = new Vector3(0.0f, 0.0f, 90.0f);
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
      ivcurve.AddKey(0.05f, 0.0f);
      iv.curve = new ParticleSystem.MinMaxCurve(1.0f, ivcurve);

      return psnewPrefab;
  }
}

// NOTE: these are in the same order they appear in the aseprite file, can only change if we manage that better
public enum SlimyboiType
{
  Glitch,      // unimplemented
  Saber,       // unimplemented
  Pink,
  Honey,       // unimplemented
  Rad,
  Tangle,
  Hunter,      // unfinished VFX
  Boom,        // unimplemented
  Rock,        // unimplemented
  Quantum,     // unimplemented
  Phosphor,
  Mosaic,      // unimplemented
  Dervish,     // unfinished
  Tabby,       // unimplemented
  Lucky,       // unimplemented
  Puddle,      // unimplemented
  Quicksilver, // unfinished
  Fire,
  Gold,        // unimplemented
  Crystal,
}

public class SlimeData
{
  public SlimyboiType type;
  public string slimeName;
  public string fullName;
  public AIActor prefab;
  public GameObject debris;

  public SlimyboiFlags flags;
  public Color goopColor = Color.white;
  public int? overrideHealth;
  public float? overrideWeight;
  public float? overrideSpeed;
  public float? overrideContactDamage;
  public float? overrideAttackCooldown;
  public float? overrideAttackRange;
  public GoopDefinition goopDefinition;
  public GameObject attackVFX;
}

[Flags]
public enum SlimyboiFlags // : ulong
{
  None                   = 0,
  Allied                 = 1 << 0,  // if set, slime is allied and cannot hurt or be hurt by player characters
  CanFly                 = 1 << 1,  // if set, slime can fly and path over pits and other hazards
  ExplodesOnDeath        = 1 << 2,  // if set, slime exploded upon dying
  ExtraCasingOnKill      = 1 << 3,  // [unimplemented] if set, slime spawns an extra casing upon killing an enemy
  PitImmunity            = 1 << 4,  // [unimplemented] immune to pits, but can't fly per se (vulnerable to other hazards)
  FireImmunity           = 1 << 5,  // if set, slime is not affected by fire or fire damage
  PoisonImmunity         = 1 << 6,  // if set, slime is not affected by poison or poison damage
  ExplosionImmunity      = 1 << 7,  // [unimplemented]
  ProjectileImmunity     = 1 << 8,  // [unimplemented]
  QuantumInstability     = 1 << 9,  // [unimplemented]
  ImmobileInCombat       = 1 << 10, // if set, slime will not attempt to move while in combat
  Absorbant              = 1 << 11, // [unimplemented]
  PassivelyGoops         = 1 << 12, // [unimplemented]
  DodgesProjectiles      = 1 << 13, // [unimplemented]
  ImmuneToMovingTraps    = 1 << 14, // if set, slime can not collide with moving traps
  CantVacInCombat        = 1 << 15, // if set, slime can not be vacuumed while in combat
  AttacksPoison          = 1 << 16, // if set, slime's attacks poison enemies
  AttacksIgnite          = 1 << 17, // if set, slime's attacks ignite enemies
  ReflectsProjectiles    = 1 << 18, // if set, slime will reflect all enemy projectiles
  FullStatusImmunity     = 1 << 19, // if set, slime is immune to all status effects
  AttacksSlow            = 1 << 20, // if set, slime's attacks slow enemies down
  ExplosiveAttacks       = 1 << 21, // if set, slime's attacks have an explosive effect
}
