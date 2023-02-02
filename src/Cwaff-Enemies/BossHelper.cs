using System;
using System.Collections.Generic;
using Gungeon;
using ItemAPI;
using EnemyAPI;
using UnityEngine;
//using DirectionType = DirectionalAnimation.DirectionType;
// using AnimationType = ItemAPI.BossBuilder.AnimationType;
using System.Collections;
using Dungeonator;
using System.Linq;
using Brave.BulletScript;
using System.Text.RegularExpressions;
using ResourceExtractor = ItemAPI.ResourceExtractor;
using GungeonAPI;
using System.Reflection;
using MonoMod.RuntimeDetour;

namespace CwaffingTheGungy
{
  public enum Floors // Matches GlobalDungeonData.ValidTilesets
  {
    GUNGEON = 1,
    CASTLEGEON = 2,
    SEWERGEON = 4,
    CATHEDRALGEON = 8,
    MINEGEON = 0x10,
    CATACOMBGEON = 0x20,
    FORGEGEON = 0x40,
    HELLGEON = 0x80,
    SPACEGEON = 0x100,
    PHOBOSGEON = 0x200,
    WESTGEON = 0x400,
    OFFICEGEON = 0x800,
    BELLYGEON = 0x1000,
    JUNGLEGEON = 0x2000,
    FINALGEON = 0x4000,
    RATGEON = 0x8000
  }

  public class BuildABoss
  {
    public GameObject prefab = null;
    private GameObject defaultGunAttachPoint = null;

    // Enemy behavior info
    private BraveBehaviour enemyBehavior = null;

    // Misc private variables
    private string guid = "";

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
      bb.enemyBehavior = BH.AddSaneDefaultBossBehavior<T>(bb.prefab,bossname,subtitle,bossCardPath);

      // Set up default colliders from the default sprite
      var sprite = bb.prefab.GetComponent<HealthHaver>().GetAnySprite();
      Vector2 spriteSize = (16f * sprite.GetBounds().size);
      bb.SetDefaultColliders((int)spriteSize.x,(int)spriteSize.y,0,0);

      // Set up a default shoot point from the center of our sprite
      GameObject shootpoint = new GameObject("attach");
        shootpoint.transform.parent = bb.enemyBehavior.transform;
        shootpoint.transform.position = bb.enemyBehavior.sprite.WorldCenter;
      bb.defaultGunAttachPoint = bb.enemyBehavior.transform.Find("attach").gameObject;

      // Set up a default shadow so teleportation doesn't throw exceptions
      if (bb.enemyBehavior.aiActor.ShadowObject == null)
      {
        GameObject defaultShadow = (GameObject)UnityEngine.Object.Instantiate(ResourceCache.Acquire("DefaultShadowSprite"));
        defaultShadow.SetActive(false);
        FakePrefab.MarkAsFakePrefab(defaultShadow);
        UnityEngine.Object.DontDestroyOnLoad(defaultShadow);
        bb.enemyBehavior.aiActor.ShadowObject = defaultShadow;
      }

      return bb;
    }

    public void SetStats(float? health = null, float? weight = null, float? speed = null, float? collisionDamage = null, float? collisionKnockbackStrength = null, float? hitReactChance = null)
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
    }

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
      if (shootPoint == null)
        shootPoint = this.defaultGunAttachPoint;
      if (healthThresholds == null)
        healthThresholds = new float[0];
      bool anyVFx = (!(
        String.IsNullOrEmpty(vfx)     &&
        String.IsNullOrEmpty(fireVfx) &&
        String.IsNullOrEmpty(tellVfx) &&
        String.IsNullOrEmpty(chargeVfx)));
      ShootBehavior bangbang = new ShootBehavior {
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
      }
      // this.prefab.GetComponent<BehaviorSpeculator>().AttackBehaviors.Add(); // TODO: could also just do this
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
    /// <returns>A TeleportBehavior with sane defaults initalized according to the parameters</returns>
    public TeleportBehavior CreateTeleportAttack<T>(
      bool add = true, float cooldown = 0f, float cooldownVariance = 0f, float attackCooldown = 0f, float globalCooldown = 0f,
      float initialCooldown = 0.5f, float initialCooldownVariance = 0f,
      float probability = 1f, int maxUsages = -1, bool requiresLineOfSight = false,
      float minHealth = 0f, float maxHealth = 1f, float[] healthThresholds = null, bool accumulateHealthThresholds = true,
      float minRange = 0f, float range = 0f, float minWallDist = 0f,
      bool vulnerable = false, bool avoidWalls = false, bool stayOnScreen = false, float minDist = 0f, float maxDist = 0f,
      float goneTime = 1f, bool onlyIfUnreachable = false,  string outAnim = null, string inAnim = null,
      Type outScript = null, Type inScript = null
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
          ManuallyDefineRoom              = false,
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
        this.prefab.GetComponent<BehaviorSpeculator>().AttackBehaviorGroup.AttackBehaviors.Add(theAttack);
      }
      // this.prefab.GetComponent<BehaviorSpeculator>().AttackBehaviors.Add(theAttack.Behavior); // TODO: could also just do this
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
      }
      // this.prefab.GetComponent<BehaviorSpeculator>().AttackBehaviors.Add(); // TODO: could also just do this
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
    }

    public void AdjustAnimation(string name, float? fps = null, bool? loop = null)
    {
      this.enemyBehavior.AdjustAnimation(name, fps, loop);
    }

    public void SetIntroAnimation(string name)
    {
      this.prefab.GetComponent<GenericIntroDoer>().introAnim = name;
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
      this.prefab.AddBossToFloorPool(guid: this.guid, floors: floors, weight: weight);
    }
  }

  public static class BH
  {
    // Used for loading a sane default behavior speculator
    public const string BULLET_KIN_GUID = "01972dee89fc4404a5c408d50007dad5";

    // Per Apache, need reference to BossManager or Unity will muck with the prefab
    private static BossManager theBossMan = null;

    // Little variable for storing our generic boss room prefab for testing
    private static PrototypeDungeonRoom genericBossRoomPrefab = null;

    // Regular expression for teasing apart animation names in a folder
    public static Regex rx_anim = new Regex(@"^(?:(.*?)_)?([^_]*?)_([0-9]+)\.png$",
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
      self.PixelColliders.Add(new PixelCollider
        {
          ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
          CollisionLayer = CollisionLayer.EnemyCollider,
          IsTrigger = false,
          BagleUseFirstFrameOnly = false,
          SpecifyBagelFrame = string.Empty,
          BagelColliderNumber = 0,
          ManualOffsetX = xoff,
          ManualOffsetY = yoff,
          ManualWidth = width,
          ManualHeight = height,
          ManualDiameter = 0,
          ManualLeftX = 0,
          ManualLeftY = 0,
          ManualRightX = 0,
          ManualRightY = 0
        });
      self.PixelColliders.Add(new PixelCollider
        {
          ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
          CollisionLayer = CollisionLayer.EnemyHitBox,
          IsTrigger = false,
          BagleUseFirstFrameOnly = false,
          SpecifyBagelFrame = string.Empty,
          BagelColliderNumber = 0,
          ManualOffsetX = xoff,
          ManualOffsetY = yoff,
          ManualWidth = width,
          ManualHeight = height,
          ManualDiameter = 0,
          ManualLeftX = 0,
          ManualLeftY = 0,
          ManualRightX = 0,
          ManualRightY = 0,
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
        miniBossIntroDoer.BossMusicEvent = "Play_MUS_Boss_Theme_Beholster";
        // miniBossIntroDoer.BossMusicEvent = "sans";
        miniBossIntroDoer.PreventBossMusic = false;
        miniBossIntroDoer.InvisibleBeforeIntroAnim = true;
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
      Dictionary<string,string[]> spriteMaps = new Dictionary<string,string[]>();
      foreach (string s in ResourceExtractor.GetResourceNames())
      {
        if (!s.StartsWith(realPath))
          continue;
        string name = s.Substring(realPath.Length);  // get name of resource relative to the path
        MatchCollection matches = rx_anim.Matches(name);
        foreach (Match match in matches)
        {
          string spriteName = match.Groups[1].Value;  //TODO: verification?
          string animName   = match.Groups[2].Value;
          string animIndex  = match.Groups[3].Value;
          if (!spriteMaps.ContainsKey(animName))
            spriteMaps[animName] = new string[0];
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
        foreach(string v in entry.Value)
        {
          if (String.IsNullOrEmpty(v))
            continue;
          // ETGModConsole.Log($"  {v}");
          SpriteBuilder.AddSpriteToCollection($"{resourcePath}/{v}", bossSprites);
          ++lastAnim;
        }
        DirectionalAnimation.DirectionType dir;
        if (entry.Key == "idle")
          dir = DirectionalAnimation.DirectionType.Single;
        else
          dir = DirectionalAnimation.DirectionType.None;
        // ETGModConsole.Log($"calling self.AddAnimation(bossSprites, BH.Range({firstAnim}, {lastAnim-1}), \"{entry.Key}\", {defaultFps}, {true}, {dir});");
        self.AddAnimation(bossSprites, BH.Range(firstAnim, lastAnim-1), entry.Key, defaultFps, true, dir);
      }

      // string [] fileEntries = Directory.GetFiles(targetDirectory);
      //   foreach(string fileName in fileEntries)
    }

    public static tk2dSpriteCollectionData LoadSpriteCollection(GameObject prefab, string[] spritePaths)
    {
      tk2dSpriteCollectionData bossSprites = SpriteBuilder.ConstructCollection(prefab, (prefab.name+" Collection").Replace(" ","_"));
      UnityEngine.Object.DontDestroyOnLoad(bossSprites);
      for (int i = 0; i < spritePaths.Length; i++)
        SpriteBuilder.AddSpriteToCollection(spritePaths[i], bossSprites);
      return bossSprites;
    }

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

    public static PrototypeDungeonRoom GetGenericBossRoom()
    {
      // Load gatling gull's boss room as a prototype
      if (genericBossRoomPrefab == null) //TODO: might need prefabs?
      {
        AssetBundle sharedAssets = ResourceManager.LoadAssetBundle("shared_auto_001");
        GenericRoomTable bossTable = sharedAssets.LoadAsset<GenericRoomTable>("bosstable_01_gatlinggull");
        genericBossRoomPrefab = bossTable.includedRooms.elements[0].room;
        sharedAssets = null;
      }
      // Instantiate and clear out the room for our personal use
      PrototypeDungeonRoom p = UnityEngine.Object.Instantiate(genericBossRoomPrefab);
        p.placedObjects.Clear();
        p.placedObjectPositions.Clear();
        p.ClearAllObjectData();
        p.additionalObjectLayers = new List<PrototypeRoomObjectLayer>();
        p.eventTriggerAreas = new List<PrototypeEventTriggerArea>();
        p.roomEvents = new List<RoomEventDefinition>();
        p.paths = new List<SerializedPath>();
        p.prerequisites = new List<DungeonPrerequisite>();
        p.rectangularFeatures = new List<PrototypeRectangularFeature>();

      // Make sure it still seals when we enter
        p.roomEvents.Add(new RoomEventDefinition(RoomEventTriggerCondition.ON_ENTER_WITH_ENEMIES, RoomEventTriggerAction.SEAL_ROOM));
        p.roomEvents.Add(new RoomEventDefinition(RoomEventTriggerCondition.ON_ENEMIES_CLEARED, RoomEventTriggerAction.UNSEAL_ROOM));
      // TODO: figure out how to create the smaller secondary boss door
      // TODO: add custom music
        // p.UseCustomMusic = true;
        // p.CustomMusicEvent = "";
      return p;
    }

    /*
      Legal tilesets:
        CASTLEGEON, SEWERGEON, GUNGEON, CATHEDRALGEON, MINEGEON, CATACOMBGEON, FORGEGEON, HELLGEON
      Illegal tilesets:
        SPACEGEON, PHOBOSGEON, WESTGEON, OFFICEGEON, BELLYGEON, JUNGLEGEON, FINALGEON, RATGEON
    */
    public static void AddBossToFloorPool(this GameObject self, string guid, Floors floors = Floors.CASTLEGEON, float weight = 1f)
    {
        // Load our boss manager if it's not loaded already
        if (theBossMan == null)
          theBossMan = GameManager.Instance.BossManager;

        // Convert our Floors enum to a GlobalDungeonData.ValidTilesets enum
        GlobalDungeonData.ValidTilesets allowedFloors =
          (GlobalDungeonData.ValidTilesets)Enum.Parse(typeof(GlobalDungeonData.ValidTilesets), floors.ToString());

        // Get a generic boss room and add it to the center of the room
        PrototypeDungeonRoom p = GetGenericBossRoom();
          Vector2 roomCenter = new Vector2(0.5f*p.Width, 0.5f*p.Height);
          tk2dBaseSprite anySprite = self.GetComponent<tk2dSpriteAnimator>().GetAnySprite();
        AddObjectToRoom(p, roomCenter - anySprite.WorldCenter, EnemyBehaviourGuid: guid);

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

    private static Hook selectBossHook = null;
    public static void InitSelectBossHook()
    {
        if (selectBossHook != null)
          return;
        selectBossHook = new Hook(
          typeof(BossFloorEntry).GetMethod("SelectBoss", BindingFlags.Public | BindingFlags.Instance),
          typeof(BH).GetMethod("SelectBossHook", BindingFlags.Public | BindingFlags.Static));
    }

    public static IndividualBossFloorEntry SelectBossHook(Func<BossFloorEntry, IndividualBossFloorEntry> orig, BossFloorEntry self)
    {
      foreach (IndividualBossFloorEntry i in self.Bosses)
        ETGModConsole.Log($"    {i.TargetRoomTable.name} -> {i.BossWeight}");
      return orig(self);
    }
  }
}
