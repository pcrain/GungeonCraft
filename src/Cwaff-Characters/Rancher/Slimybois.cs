namespace CwaffingTheGungy;

public static class Slimybois
{
  internal const float _DEFAULT_COOLDOWN = 0.5f;

  public static readonly int NumSlimes = Enum.GetNames(typeof(SlimyboiType)).Length;
  public static SlimeData[] SlimeData = null;

  internal static GameObject _SlimeDeathVFX;

  private static SlimeData Init(this SlimeData sd)
  {
    if (string.IsNullOrEmpty(sd.slimeName))
      sd.slimeName = Enum.GetName(typeof(SlimyboiType), sd.type).ToLower();
    sd.fullName = sd.slimeName.ToTitleCaseInvariant() + " Slime";
    AIActor actor = $"Slime {sd.slimeName}".InitEnemy(health: sd.overrideHealth ?? 40, baseFps: 12, doCorpse: false);
    actor.procedurallyOutlined       = false; // TODO: remove outlines from sprites later
    actor.MovementSpeed              = sd.overrideSpeed ?? 4.5f;
    actor.CollisionDamage            = 0.0f; // Overridden by SlimyboiChargeBehavior at charge time
    actor.CollisionKnockbackStrength = 0.0f; // Overridden by SlimyboiChargeBehavior at charge time
    actor.AvoidRadius                = 4.0f;

    float attackRange = sd.overrideAttackRange ?? 3.0f;
    BehaviorSpeculator bs = actor.gameObject.GetComponent<BehaviorSpeculator>();
    bs.TargetBehaviors.Add(new SlimyboiTargetingBehavior(){ Radius = 35.0f, LineOfSight = false });
    bs.MovementBehaviors.Add(new SeekTargetBehavior(){ StopWhenInRange = true, CustomRange = 2.0f, PathInterval = 0.5f });
    bs.MovementBehaviors.Add(new MoveErraticallyBehavior { PathInterval = 0.5f, StayOnScreen = false, UseTargetsRoom = false, AvoidTarget = false });
    bs.AttackBehaviors.Add(new SlimyboiChargeBehavior(){ chargeDamage = sd.overrideContactDamage ?? 0.5f, chargeKnockback = 5.0f, chargeSpeed = 30.0f,
      minRange = 0.0f, maxRange = attackRange, Cooldown = sd.overrideAttackCooldown ?? _DEFAULT_COOLDOWN, maxChargeDistance = attackRange + 0.5f }); //NOTE: chargeDamage multiplied by 5 on enemies for some reason

    KnockbackDoer kbd = actor.gameObject.GetComponent<KnockbackDoer>();
    kbd.weight = sd.overrideWeight ?? 40.0f;
    kbd.deathMultiplier = 3.0f;

    AIAnimator aiAnim = actor.gameObject.GetComponent<AIAnimator>();
    aiAnim.facingType = AIAnimator.FacingType.Movement;

    GoopDoer gd = actor.gameObject.AddComponent<GoopDoer>();
    gd.goopDefinition     = EasyGoopDefinitions.BlobulonGoopDef;
    gd.positionSource     = GoopDoer.PositionSource.HitBoxCenter;
    gd.updateTiming       = GoopDoer.UpdateTiming.Always;
    gd.updateFrequency    = 0.05f;
    gd.defaultGoopRadius  = 1f;
    gd.radiusMin          = 0.25f;
    gd.radiusMax          = 0.75f;

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

    SlimyboiController controller = actor.gameObject.AddComponent<SlimyboiController>();
    controller.slimeType = sd.type;

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

  public static void Init()
  {
    // set up array
    SlimeData = new SlimeData[NumSlimes];

    // set up individual defs
    SlimeData.SetupEntry(new(){ type = SlimyboiType.Quicksilver, overrideAttackCooldown = 0.15f, overrideSpeed = 12f });

    // pad out unfinished defs
    foreach (SlimyboiType t in Enum.GetValues(typeof(SlimyboiType)))
      if (SlimeData[(int)t] == null)
        SlimeData.SetupEntry(new SlimeData{ type = t }); // TODO: make them unique

    // shared
    _SlimeDeathVFX = VFX.Create("slime_death_vfx");
    _SlimeDeathVFX.GetComponent<tk2dSprite>().MakeGlowyBetter(glowAmount: 100.0f, glowColorPower: 100.0f, glowColor: Color.white);
  }
}

// NOTE: these are in the same order they appear in the aseprite file, can only change if we manage that better
public enum SlimyboiType
{
  Glitch,      // unimplemented
  Saber,       // unimplemented
  Pink,
  Honey,       // unimplemented
  Rad,         // unimplemented
  Tangle,      // unimplemented
  Hunter,      // unimplemented
  Boom,        // unimplemented
  Rock,        // unimplemented
  Quantum,     // unimplemented
  Phosphor,    // unimplemented
  Mosaic,      // unimplemented
  Dervish,     // unimplemented
  Tabby,       // unimplemented
  Lucky,       // unimplemented
  Puddle,      // unimplemented
  Quicksilver, // unfinished
  Fire,        // unimplemented
  Gold,        // unimplemented
  Crystal,     // unimplemented
}

public class SlimeData
{
  public SlimyboiType type;
  public string slimeName;
  public string fullName;
  public AIActor prefab;
  public GameObject debris;

  public SlimyboiFlags flags;
  public Color goopColor;
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
  None              = 0,
  Allied            = 1 << 0, // if set, slime is allied and cannot hurt or be hurt by player characters
  CanFly            = 1 << 1, // if set, slime can fly and path over pits and other hazards
  ExplodesOnDeath   = 1 << 2, // if set, slime exploded upon dying
  ExtraCasingOnKill = 1 << 3, // if set, slime spawns an extra casing upon killing an enemy
}
