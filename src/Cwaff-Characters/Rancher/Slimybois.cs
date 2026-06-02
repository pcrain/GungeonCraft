namespace CwaffingTheGungy;

public static class Slimybois
{
  public static AIActor PinkSlimeEnemyPrefab = null;

  internal static GameObject _SlimeGoopDebris;
  internal static GameObject _SlimeDeathVFX;

  // TODO: most parameters copied from Blobulons, tweak later as needed
  public static void Init()
  {
    AIActor actor = PinkSlimeEnemyPrefab = "Slime Pink".InitEnemy(health: 20, baseFps: 12, doCorpse: false);
    actor.procedurallyOutlined       = false; // TODO: remove outlines from sprites later
    actor.MovementSpeed              = 4.5f;
    actor.CollisionDamage            = 0.5f;
    actor.CollisionKnockbackStrength = 5.0f;
    actor.AvoidRadius                = 4.0f;

    BehaviorSpeculator bs = PinkSlimeEnemyPrefab.gameObject.GetComponent<BehaviorSpeculator>();
    bs.TargetBehaviors.Add(new TargetPlayerBehavior(){ Radius = 35.0f, LineOfSight = false });
    bs.MovementBehaviors.Add(new SeekTargetBehavior(){ StopWhenInRange = false, CustomRange = 6.0f, PathInterval = 0.5f });

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
  CanFly            = 1 << 0,
  ExplodesOnDeath   = 1 << 1,
  ExtraCasingOnKill = 1 << 2,
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
  // GoopDefinition goopDefinition;
}

public class SlimyboiController : BraveBehaviour
{
  private void Start()
  {
    HealthHaver hh = base.healthHaver;
    hh.SuppressDeathSounds = true;
    hh.OnDeath += this.OnDeath;
    base.specRigidbody.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
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

    // for (int i = 0; i < 10; ++i)
    // {
    //   DebrisObject debris = UnityEngine.Object.Instantiate(
    //     Slimybois._SlimeGoopDebris, base.aiActor.CenterPosition, Quaternion.identity).GetComponent<DebrisObject>();
    //   debris.GravityOverride = 30.0f;
    //   debris.Trigger(Lazy.RandomVector(3f * UnityEngine.Random.value).ToVector3ZUp(4f), 0.25f);
    //   debris.sprite.MakeGlowyBetter(glowAmount: 10.0f, glowColor: new Color(1.0f, 0.75f, 0.9f), glowColorPower: 20.0f, sensitivity: 0.3f);
    // }
  }

  private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
  {
    if (otherRigidbody.gameActor is not PlayerController pc)
      return;
    if (pc.CurrentGun is Gun gun1 && gun1.gameObject.GetComponent<Vacpack>() is Vacpack v1 && v1.IsVacuumingSlime(this))
      PhysicsEngine.SkipCollision = true;
    if (GameManager.Instance.CurrentGameType != GameManager.GameType.COOP_2_PLAYER || GameManager.Instance.GetOtherPlayer(pc) is not PlayerController p2)
      return;
    if (p2.CurrentGun is Gun gun2 && gun2.gameObject.GetComponent<Vacpack>() is Vacpack v2 && v2.IsVacuumingSlime(this))
      PhysicsEngine.SkipCollision = true;
  }
}
