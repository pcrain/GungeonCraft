using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;
using Brave.BulletScript;

using Gungeon;
using ItemAPI;
using EnemyAPI;
using Dungeonator;

namespace CwaffingTheGungy
{
public class SecretBoss : AIActor
{
  public  const string guid          = "Secret Boss";
  private const string bossname      = "Sans Gundertale";
  private const string subtitle      = "Introducing...";
  // private const string spritePath    = "CwaffingTheGungy/Resources/room_mimic";
  private const string spritePath    = "CwaffingTheGungy/Resources/sans";
  private const string defaultSprite = "sans_idle_1";
  private const string bossCardPath  = "CwaffingTheGungy/Resources/sans_bosscard.png";

  private const string soundSpawn      = "Play_OBJ_turret_set_01";
  private const string soundSpawnQuiet = "undertale_pullback";
  private const string soundSpawnAlt   = "undertale_arrow";
  private const string soundShoot      = "Play_WPN_spacerifle_shot_01";

  private const int MEGALO_LOOP_END    = 152512;
  private const int MEGALO_LOOP_LENGTH = 137141;

  private const int NUM_HITS = 60;

  internal static GameObject napalmReticle      = null;
  internal static AIBulletBank.Entry boneBullet = null;
  internal static VFXPool bonevfx               = null;
  internal static uint megalo_event_id          = 0;
  public static void Init()
  {
    // Create our build-a-boss
    BuildABoss bb = BuildABoss.LetsMakeABoss<BossBehavior>(
      bossname, guid, $"{spritePath}/{defaultSprite}", new IntVector2(8, 9), subtitle, bossCardPath);
    // Set our stats
    bb.SetStats(health: NUM_HITS, weight: 200f, speed: 2f, collisionDamage: 0f,
      hitReactChance: 0.05f, collisionKnockbackStrength: 0f);
    // Set up our animations
    bb.InitSpritesFromResourcePath(spritePath);
      bb.AdjustAnimation("idle",   fps:   12f, loop: true);
      bb.AdjustAnimation("idle_cloak",   fps:   12f, loop: true);
      bb.AdjustAnimation("decloak",   fps:   6f, loop: false);
      bb.AdjustAnimation("teleport_in",   fps:   60f, loop: false);
      bb.AdjustAnimation("teleport_out",   fps:   60f, loop: false);
    // Set our default pixel colliders
    bb.SetDefaultColliders(15,30,24,2);
    // Add custom animation to the generic intro doer, and add a specific intro doer as well
    bb.SetIntroAnimation("decloak");
    bb.prefab.GetComponent<GenericIntroDoer>().preIntroAnim = "idle_cloak";
    bb.AddCustomIntro<BossIntro>();
    // Set up the boss's targeting and attacking scripts
    bb.TargetPlayer();
    // Add some named vfx pools to our bank of VFX
    bb.AddNamedVFX(VFX.vfxpool["Tornado"], "mytornado");

    // Add a random teleportation behavior
    bb.CreateTeleportAttack<CustomTeleportBehavior>(
      goneTime: 0.25f, outAnim: "teleport_out", inAnim: "teleport_in", cooldown: 0.26f, attackCooldown: 0.15f, probability: 10f);
    // Add some basic bullet attacks
    bb.CreateBulletAttack<CeilingBulletsScript>(fireAnim: "laugh", cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<OrbitBulletScript>(fireAnim: "throw_up", cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<HesitantBulletWallScript>(fireAnim: "throw_down", cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<SquareBulletScript>(fireAnim: "throw_left", cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<ChainBulletScript>(fireAnim: "throw_right", cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<WallSlamScript>(fireAnim: "laugh", cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<SineWaveScript>(fireAnim: "throw_right", cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<OrangeAndBlueScript>(fireAnim: "throw_right", cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<WiggleWaveScript>(fireAnim: "throw_right", cooldown: 0.25f, attackCooldown: 0.15f);
    // Add a bunch of simultaenous bullet attacks
    // bb.CreateSimultaneousAttack(new(){
    //   bb.CreateBulletAttack<RichochetScript> (add: false, tellAnim: "swirl", fireAnim: "suck", attackCooldown: 3.5f, fireVfx: "mytornado"),
    //   bb.CreateBulletAttack<RichochetScript2>(add: false, tellAnim: "swirl", fireAnim: "suck", attackCooldown: 3.5f, fireVfx: "mytornado"),
    //   bb.CreateBulletAttack<CeilingBulletsScript>(add: false, fireAnim: "swirl", attackCooldown: 3.5f, fireVfx: "mytornado"),
    //   });

    // Add a wandering behavior
    // bb.prefab.GetComponent<BehaviorSpeculator>().MovementBehaviors.Add(new WanderBehavior());

    // Add our boss to the enemy database and to the first floor's boss pool
    bb.AddBossToGameEnemies("cg:secretboss");
    bb.AddBossToFloorPool(Floors.CASTLEGEON, weight: 9999f);

    InitPrefabs();  // Do miscellaneous prefab loading
  }

  internal static void InitPrefabs()
  {
    // Targeting reticle
    AssetBundle sharedAssets2 = ResourceManager.LoadAssetBundle("shared_auto_002");
    GameObject prefabReticle = sharedAssets2.LoadAsset<GameObject>("NapalmStrikeReticle");
    napalmReticle = UnityEngine.Object.Instantiate(prefabReticle);
    tk2dSlicedSprite m_extantReticleQuad = napalmReticle.GetComponent<tk2dSlicedSprite>();
        m_extantReticleQuad.SetSprite(VFX.SpriteCollection, VFX.sprites["reticle-white"]);
    napalmReticle.RegisterPrefab();

    // Bone bullet Spawn VFX
    bonevfx = VFX.CreatePoolFromVFXGameObject((PickupObjectDatabase.GetById(29) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);

    // Bone bullet
    AIBulletBank.Entry reversible = EnemyDatabase.GetOrLoadByGuid("1bc2a07ef87741be90c37096910843ab").bulletBank.GetBullet("reversible");
    boneBullet = new AIBulletBank.Entry(reversible);
      boneBullet.Name         = "getboned";
      GameObject fancyBullet = UnityEngine.Object.Instantiate((PickupObjectDatabase.GetById(59) as Gun).DefaultModule.projectiles[0].gameObject); // hegemony rifle
        fancyBullet.RegisterPrefab();
        boneBullet.BulletObject = fancyBullet;
      boneBullet.PlayAudio    = true;
      boneBullet.PlayAudio    = false;
      boneBullet.AudioEvent   = "Play_WPN_golddoublebarrelshotgun_shot_01";
      boneBullet.AudioLimitOncePerAttack = false;
      boneBullet.AudioLimitOncePerFrame = false;
      boneBullet.MuzzleFlashEffects = bonevfx;
  }

  internal class DoomZoneGrowth : MonoBehaviour
  {
    public void Lengthen(float targetLength, int numFrames)
    {
      this.StartCoroutine(Lengthen_CR(targetLength,numFrames));
    }

    private IEnumerator Lengthen_CR(float targetLength, int numFrames)
    {
      tk2dSlicedSprite quad = this.GetComponent<tk2dSlicedSprite>();
      float scaleFactor = C.PIXELS_PER_TILE * targetLength / numFrames;
      for (int i = 1 ; i <= numFrames; ++i)
      {
        quad.dimensions = quad.dimensions.WithX(scaleFactor * i);
        quad.UpdateZDepth();
        yield return null;
      }
      // restore reticle riser settings
      this.gameObject.GetOrAddComponent<ReticleRiserEffect>().NumRisers = 3;
    }
  }

  // Creates a napalm-strike-esque danger zone
  internal static GameObject DoomZone(Vector2 start, Vector2 target, float width, float lifetime = -1f, int growthTime = 0, string sprite = null)
  {
    Vector2 delta                        = target - start;
    GameObject reticle                   = UnityEngine.Object.Instantiate(napalmReticle);
    tk2dSlicedSprite m_extantReticleQuad = reticle.GetComponent<tk2dSlicedSprite>();
      if (sprite != null)
        m_extantReticleQuad.SetSprite(VFX.SpriteCollection, VFX.sprites[sprite]);
      if (growthTime == 0)
        m_extantReticleQuad.dimensions = C.PIXELS_PER_TILE * (new Vector2(delta.magnitude, width));
      else
      {
        UnityEngine.Object.Destroy(reticle.GetComponent<ReticleRiserEffect>());
        m_extantReticleQuad.dimensions = C.PIXELS_PER_TILE * (new Vector2(delta.magnitude / growthTime, width));
        reticle.AddComponent<DoomZoneGrowth>().Lengthen(delta.magnitude,growthTime);
      }
      m_extantReticleQuad.transform.localRotation = Quaternion.Euler(0f, 0f, BraveMathCollege.Atan2Degrees(target-start));
      m_extantReticleQuad.transform.position = start + (Quaternion.Euler(0f, 0f, -90f) * delta.normalized * (width / 2f)).XY();
    if (lifetime > 0)
      reticle.ExpireIn(lifetime);
    return reticle;
  }

  internal static void SpawnDust(Vector2 where, int howMany = 1)
  {
    for (int i = 0; i < howMany; ++i)
    {
      DustUpVFX dusts = GameManager.Instance.Dungeon.dungeonDustups;
      float dir = UnityEngine.Random.Range(0.0f,360.0f);
      float rot = UnityEngine.Random.Range(0.0f,360.0f);
      float mag = UnityEngine.Random.Range(0.3f,1.25f);
      SpawnManager.SpawnVFX(
          dusts.rollLandDustup,
          where + BraveMathCollege.DegreesToVector(dir, mag),
          Quaternion.Euler(0f, 0f, rot));
    }
  }

  // internal class WanderBehavior : MovementBehaviorBase
  // {
  //   public float PathInterval = 0.25f;

  //   public float TargetInterval = 3f;

  //   public float MillRadius = 5f;

  //   private Vector2 m_currentTargetPosition;

  //   private float m_repathTimer;

  //   private float m_newPositionTimer;

  //   public override void Start()
  //   {
  //     base.Start();
  //   }

  //   public override void Upkeep()
  //   {
  //     base.Upkeep();
  //     DecrementTimer(ref m_repathTimer);
  //     DecrementTimer(ref m_newPositionTimer);
  //   }

  //   public override BehaviorResult Update()
  //   {
  //     ETGModConsole.Log($"once");
  //     PlayerController playerController = GameManager.Instance.PrimaryPlayer;
  //     if (!playerController)
  //       return BehaviorResult.Continue;
  //     return BehaviorResult.RunContinuous;
  //   }

  //   public override ContinuousBehaviorResult ContinuousUpdate()
  //   {
  //     PlayerController playerController = GameManager.Instance.PrimaryPlayer;
  //     if (!playerController)
  //       return ContinuousBehaviorResult.Finished;
  //     m_aiActor.MovementSpeed = m_aiActor.BaseMovementSpeed;
  //     float num = Vector2.Distance(playerController.CenterPosition, m_currentTargetPosition);
  //     float num2 = Vector2.Distance(m_aiActor.CenterPosition, m_currentTargetPosition);
  //     if (m_newPositionTimer <= 0f || num > MillRadius * 1.75f || num2 <= 0.25f)
  //     {
  //       m_aiActor.ClearPath();
  //       m_currentTargetPosition = playerController.specRigidbody.HitboxPixelCollider.UnitBottomCenter;
  //       m_newPositionTimer = TargetInterval;
  //     }
  //     m_aiActor.MovementSpeed = Mathf.Lerp(m_aiActor.BaseMovementSpeed, m_aiActor.BaseMovementSpeed * 2f, Mathf.Clamp01(num2 / 30f));
  //     if (m_repathTimer <= 0f && !playerController.IsOverPitAtAll && !playerController.IsInMinecart)
  //     {
  //       m_repathTimer = PathInterval;
  //       m_aiActor.FallingProhibited = false;
  //       m_aiActor.PathfindToPosition(m_currentTargetPosition);
  //       if (m_aiActor.Path != null && m_aiActor.Path.InaccurateLength > 50f)
  //       {
  //         m_aiActor.ClearPath();
  //         m_aiActor.CompanionWarp(m_aiActor.CompanionOwner.CenterPosition);
  //       }
  //       else if (m_aiActor.Path != null && !m_aiActor.Path.WillReachFinalGoal)
  //       {
  //         m_aiActor.CompanionWarp(m_aiActor.CompanionOwner.CenterPosition);
  //       }
  //     }
  //     return base.ContinuousUpdate();
  //   }

  //   // public override void EndContinuousUpdate()
  //   // {
  //   //   m_updateEveryFrame = false;
  //   //   m_triedToPathOverPit = false;
  //   //   m_groundRolling = false;
  //   //   m_aiActor.FallingProhibited = false;
  //   //   m_aiActor.BehaviorOverridesVelocity = false;
  //   //   base.EndContinuousUpdate();
  //   // }
  // }

  internal class BossBehavior : BraveBehaviour
  {
    private bool hasFinishedIntro = false;
    private float yoffset = 0;
    private bool auraActive = false;
    private HeatIndicatorController aura;

    // from basegame AuroOnReloadModifier
    private void ActivateAura()
    {
      if (auraActive)
        return;
      auraActive = true;
      aura = ((GameObject)UnityEngine.Object.Instantiate(ResourceCache.Acquire("Global VFX/HeatIndicator"), base.aiActor.CenterPosition.ToVector3ZisY(), Quaternion.identity, base.aiActor.sprite.transform)).GetComponent<HeatIndicatorController>();
        aura.CurrentColor = new Color(1f, 1f, 1f);
        aura.IsFire = true;
        aura.CurrentRadius = 2f;
    }

    private void Start()
    {
      base.aiActor.healthHaver.healthIsNumberOfHits = true;
      base.aiActor.healthHaver.usesInvulnerabilityPeriod = true;
      base.aiActor.healthHaver.invulnerabilityPeriod = 1.0f;
      base.aiActor.healthHaver.OnPreDeath += (obj) =>
      {
        FlipSpriteIfNecessary(forceUnflip: true);
        megalo_event_id = 0;
        AkSoundEngine.PostEvent("electromegalo_stop", base.aiActor.gameObject);
        AkSoundEngine.PostEvent("Play_ENM_beholster_death_01", base.aiActor.gameObject);
      };
      base.healthHaver.healthHaver.OnDeath += (obj) =>
      {
        FlipSpriteIfNecessary(forceUnflip: true);
        Chest chest2 = GameManager.Instance.RewardManager.SpawnTotallyRandomChest(GameManager.Instance.PrimaryPlayer.CurrentRoom.GetRandomVisibleClearSpot(1, 1));
        chest2.IsLocked = false;
      };
      // this.aiActor.knockbackDoer.SetImmobile(true, "laugh");
      this.aiActor.bulletBank.Bullets.Add(boneBullet);
    }

    public void FinishedIntro()
    {
      hasFinishedIntro = true;
      ActivateAura();
    }

    private void Update()
    {

      if (!hasFinishedIntro)
        return;

      // loop music if necessary
      int pos = 0;
      AKRESULT status = AkSoundEngine.GetSourcePlayPosition(megalo_event_id, out pos);
      if (status == AKRESULT.AK_Success)
      {
        // ETGModConsole.Log($"{megalo_event_id}: position {pos}");
        if (pos >= MEGALO_LOOP_END)
          AkSoundEngine.SeekOnEvent("electromegalo", base.aiActor.gameObject,pos - MEGALO_LOOP_LENGTH);
      }

      // don't do anything if we're paused
      if (BraveTime.DeltaTime == 0)
        return;

      DriftAround();
    }

    private void LateUpdate()
    {
      const float JIGGLE = 4.0f;
      const float SPEED = 4.0f;

      if (!hasFinishedIntro)
        return;

      // don't do anything if we're paused
      if (BraveTime.DeltaTime == 0)
        return;

      FlipSpriteIfNecessary();

      yoffset = Mathf.CeilToInt(JIGGLE * Mathf.Sin(SPEED*BraveTime.ScaledTimeSinceStartup))/16.0f;
      base.sprite.transform.localPosition += new Vector3(0,yoffset,0);
      if (Lazy.CoinFlip())
        SpawnDust(base.specRigidbody.UnitCenter);
    }

    private void DriftAround()
    {
      // // base.aiActor.specRigidbody.Velocity = Lazy.RandomVector();
      // Vector2 rng = Lazy.RandomVector();
      // // Vector3 movement = (1.0f/(float)C.PIXELS_PER_TILE)*rng.ToVector3ZisY();
      // Vector3 movement = rng.ToVector3ZisY();
      // // base.aiActor.specRigidbody.Reinitialize();
      // base.aiActor.transform.position += movement;
      // base.aiActor.specRigidbody.transform.position += movement;
      // base.sprite.transform.localPosition += movement;

      base.aiActor.PathfindToPosition(GameManager.Instance.PrimaryPlayer.specRigidbody.UnitCenter);
    }

    private void FlipSpriteIfNecessary(bool forceUnflip = false)
    {
      bool lastFlip = base.sprite.FlipX;
      bool shouldFlip = (GameManager.Instance.BestActivePlayer.sprite.WorldBottomCenter.x < base.specRigidbody.UnitBottomCenter.x);
      base.sprite.FlipX = shouldFlip && (!forceUnflip);
      Vector3 spriteSize = base.sprite.GetUntrimmedBounds().size;
      Vector3 offset = new Vector3(spriteSize.x / 2, 0f, 0f);
      if (!base.sprite.FlipX)
        offset *= -1;

      Vector3 finalPosition = (Vector3)base.specRigidbody.UnitBottomCenter/*.RoundToInt()*/ + offset;
      base.sprite.transform.localPosition = finalPosition;
      if (auraActive)
        aura.transform.localPosition = new Vector3(0,spriteSize.y / 2,0) - offset;
    }
  }

  [RequireComponent(typeof(GenericIntroDoer))]
  internal class BossIntro : SpecificIntroDoer
  {
    public override void PlayerWalkedIn(PlayerController player, List<tk2dSpriteAnimator> animators)
    {
      SetupRoomSpecificAttacks();
      GameManager.Instance.StartCoroutine(PlaySound());
    }

    private void SetupRoomSpecificAttacks()
    {
      Rect roomFullBounds = base.aiActor.GetAbsoluteParentRoom().GetBoundingRect();
      Rect roomTrimmedBounds = roomFullBounds.Inset(8f);
      foreach (AttackBehaviorGroup.AttackGroupItem attack in base.aiActor.gameObject.GetComponent<BehaviorSpeculator>().AttackBehaviorGroup.AttackBehaviors)
      {
        if (attack.Behavior is TeleportBehavior)
        {
          TeleportBehavior tb = attack.Behavior as TeleportBehavior;
          tb.ManuallyDefineRoom = true;
          tb.roomMin = roomTrimmedBounds.min;
          tb.roomMax = roomTrimmedBounds.max;
          // ETGModConsole.Log($"FIXED TELEPORTATION");
        }
      }
    }

    private IEnumerator PlaySound()
    {
      megalo_event_id = AkSoundEngine.PostEvent("electromegalo", base.aiActor.gameObject, in_uFlags: (uint)AkCallbackType.AK_EnableGetSourcePlayPosition);
      yield return StartCoroutine(BH.WaitForSecondsInvariant(1.8f));
      // AkSoundEngine.PostEvent("Play_BOSS_doormimic_lick_01", base.aiActor.gameObject);
      yield break;
    }

    public override void EndIntro()
    {
      base.aiActor.gameObject.GetComponent<BossBehavior>().FinishedIntro();
    }
  }

  internal class CustomTeleportBehavior : TeleportBehavior
  {
    private bool playedInSound = false;
    private bool playedOutSound = false;
    private Vector3 oldPos = Vector3.zero;
    private Vector3 newPos = Vector3.zero;
    public override ContinuousBehaviorResult ContinuousUpdate()
    {
      if (State == TeleportState.TeleportOut)
      {
        if (!playedOutSound)
        {
          AkSoundEngine.PostEvent("teledasher", GameManager.Instance.PrimaryPlayer.gameObject);
          oldPos = base.m_aiActor.Position;
        }
        playedOutSound = true;
        playedInSound = false;
      }
      else if (State == TeleportState.TeleportIn)
      {
        if (!playedInSound)
        {
          AkSoundEngine.PostEvent("teledasher", GameManager.Instance.PrimaryPlayer.gameObject);
          newPos = base.m_aiActor.Position;
          Vector3 delta = (newPos-oldPos);
          for(int i = 0; i < 10; ++i)
            SpawnDust(oldPos + (i/10.0f) * delta);
        }
        playedInSound = true;
        playedOutSound = false;
      }
      return base.ContinuousUpdate();
    }
  }

  internal class SecretBullet : Bullet
  {
      private Color? tint = null;
      public SecretBullet(Color? tint = null) : base("getboned")
      {
        this.tint = tint;
      }

      public override void Initialize()
      {
        this.Projectile.ChangeTintColorShader(0f,tint ?? new Color(1.0f,0.5f,0.5f,0.5f));
        base.Initialize();
      }
  }

  internal abstract class SecretBulletScript : FluidBulletScript
  {
    protected AIActor boss {get; private set;}
    protected Rect roomFullBounds {get; private set;}
    protected Rect roomBulletBounds {get; private set;}
    protected Rect roomSlamBounds {get; private set;}
    protected Rect roomTeleportBounds {get; private set;}

    public override void Initialize()
    {
      base.Initialize();
      this.boss               = this.BulletBank.aiActor;
      this.roomFullBounds     = this.boss.GetAbsoluteParentRoom().GetBoundingRect();
      this.roomBulletBounds   = this.roomFullBounds.Inset(topInset: 2f, rightInset: 2f, bottomInset: 4f, leftInset: 2f);
      this.roomSlamBounds     = this.roomFullBounds.Inset(topInset: 2f, rightInset: 2.5f, bottomInset: 2f, leftInset: 1.5f);
      this.roomTeleportBounds = this.roomFullBounds.Inset(8f);
    }
  }

  internal class OrangeAndBlueScript : SecretBulletScript
  {
    private static readonly string orangeReticle = "reticle-orange";
    private static readonly string blueReticle   = "reticle-blue";
    private static readonly Color orangeColor    = new Color(1.0f,0.75f,0f,0.5f);
    private static readonly Color blueColor      = new Color(0.65f,0.65f,1.0f,0.5f);

    // orange = harmless if you're moving; blue = harmless if you're stationary
    internal class OrangeAndBlueBullet : SecretBullet
    {
      private bool orange;
      public OrangeAndBlueBullet(bool orange) : base(orange ? orangeColor : blueColor)
      {
        this.orange = orange;
      }

      public override IEnumerator Top()
      {
        this.Projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
        yield break;
      }

      private void OnPreCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, SpeculativeRigidbody other, PixelCollider otherPixelCollider)
      {
        if (!(other.gameActor is PlayerController))
          return;
        PlayerController p = other.gameActor as PlayerController;
        bool playerIsIdle = p.spriteAnimator.CurrentClip.name.Contains("idle",true);
        PhysicsEngine.SkipCollision = (this.orange != playerIsIdle);
      }
    }

    protected override List<FluidBulletInfo> BuildChain()
    {
      return
      Run(DoTheThing(Lazy.CoinFlip()))
        .Then(DoTheThing(Lazy.CoinFlip()))
        .Then(DoTheThing(Lazy.CoinFlip()))
      .Finish();
    }

    private const int COUNT      = 32;
    private const int WAVES      = 5;
    private const int BATCH      = 8;
    private const float SPEED    = 50f;
    private const float LENIENCE = 30f;
    private const float COOLDOWN = 30f;
    private IEnumerator DoTheThing(bool orange)
    {
      Vector2 ppos = GameManager.Instance.PrimaryPlayer.CenterPosition;
      for (float i = 1f; i <= 4f; ++i)
        DoomZone(ppos - i*0.5f*Vector2.right, ppos + i*0.5f*Vector2.right, i, 0.5f, 0, orange ? orangeReticle : blueReticle);
      AkSoundEngine.PostEvent("undertale_eyeflash", GameManager.Instance.PrimaryPlayer.gameObject);
      PathRect roomPath   = new PathRect(base.roomBulletBounds);
      List<Vector2> points = roomPath.SampleUniform(COUNT,0.0f,1.0f);
      yield return Wait(LENIENCE);
      for(int wave = 0; wave < WAVES; ++wave)
      {
        ppos = GameManager.Instance.PrimaryPlayer.CenterPosition;
        AkSoundEngine.PostEvent(soundShoot, GameManager.Instance.PrimaryPlayer.gameObject);
        int batch = 0;
        foreach(Vector2 p in points)
        {
          Vector2 wiggle = 3f * Lazy.RandomAngle().ToVector();
          this.Fire(Offset.OverridePosition(p), new Direction(((ppos+wiggle)-p).ToAngle(),DirectionType.Absolute), new Speed(SPEED,SpeedType.Absolute), new OrangeAndBlueBullet(orange:orange));
          if (++batch == BATCH)
          {
            batch = 0;
            yield return Wait(1);
          }
        }
      }
      yield return Wait(COOLDOWN);
    }
  }

  internal class SineBullet : SecretBullet
  {
    private float  amplitude        = 1f;
    private float  freq             = 1f;
    private float  phase            = 0f;
    private float? rotationOverride = null;

    private float lifetime  = 0f;

    public SineBullet(float amplitude, float freq, float phase = 0, float? rotationOverride = null) : base()
    {
      this.amplitude        = amplitude;
      this.freq             = freq;
      this.phase            = phase;
      this.rotationOverride = rotationOverride;
    }

    public override IEnumerator Top()
    {
      AkSoundEngine.PostEvent(soundShoot, GameManager.Instance.PrimaryPlayer.gameObject);

      Vector2 startSpeed   = this.RealVelocity();
      float rotationNormal = ((this.rotationOverride ?? startSpeed.ToAngle()) + 90f).Clamp180();
      Vector2 amp          = amplitude * rotationNormal.ToVector();
      Vector2 anchorPos    = this.Position - Mathf.Sin(phase) * amp;
      float adjfreq        = freq * 2f * Mathf.PI;
      this.ChangeSpeed(new Speed(0));
      while (true)
      {
        this.lifetime       += BraveTime.DeltaTime;
        anchorPos           += startSpeed;
        Vector2 oldPosition  = this.Position;
        float curPhase       = Mathf.Sin(adjfreq*this.lifetime + phase);
        this.Position        = anchorPos + curPhase * amp;
        this.ChangeDirection(new Direction((this.Position-oldPosition).ToAngle(),DirectionType.Absolute));
        yield return Wait(1);
      }
    }
  }

  internal class SineWaveScript : SecretBulletScript
  {

    protected override List<FluidBulletInfo> BuildChain()
    {
      AkSoundEngine.PostEvent("sans_laugh", GameManager.Instance.PrimaryPlayer.gameObject);
      return
      Run(DoTheThing())
        .And(DoTheThing(reverse: true))
        .And(DoTheThing(inverse: true))
        .And(DoTheThing(reverse: true, inverse: true))
      .Finish();
    }

    private const int COUNT = 50;
    private IEnumerator DoTheThing(bool reverse = false, bool inverse = false)
    {
      PathLine theEdge    = inverse ? (new PathRect(base.roomBulletBounds).Right()) : (new PathRect(base.roomBulletBounds).Left());
      int i = 0;
      foreach(Vector2 p in theEdge.SampleUniform(COUNT,reverse ? 0.9f : 0.1f,reverse ? 0.1f : 0.9f))
      {
          this.Fire(Offset.OverridePosition(p), new Direction(inverse ? 180f : 0f,DirectionType.Absolute),
            new Speed(20f,SpeedType.Absolute), new SineBullet(3f,reverse ? -1f : 1f, (reverse ? -i : i) * 0.1f, 0f));
          yield return Wait(5);
          ++i;
      }
    }
  }

  internal class WiggleWaveScript : SecretBulletScript
  {

    protected override List<FluidBulletInfo> BuildChain()
    {
      AkSoundEngine.PostEvent("sans_laugh", GameManager.Instance.PrimaryPlayer.gameObject);
      int version = Lazy.CoinFlip() ? 1 : 2;
      return
      Run(DoTheThing(0f, version))
        .And(DoTheThing(0.25f, version))
        .And(DoTheThing(0.50f, version))
        .And(DoTheThing(0.75f, version))
      .Finish();
    }

    private const int COUNT = 57;
    private IEnumerator DoTheThing(float start, int version)
    {
      // Vector2 middle = this.BulletBank.aiActor.GetAbsoluteParentRoom().GetBoundingRect().Center();
      Vector2 middle = this.BulletBank.aiActor.sprite.WorldCenter;
      PathCircle theCircle = new PathCircle(middle,2f);
      int i = 0;
      int waitTime = (version == 1 ? 5 : 4);
      foreach(Vector2 p in theCircle.SampleUniform(COUNT,start: start, end:start + (version == 1 ? 1f : 2f))) // 2 rotations
      {
          this.Fire(Offset.OverridePosition(p), new Direction(theCircle.AngleTo(p),DirectionType.Absolute),
            new Speed(12f,SpeedType.Absolute), new SineBullet(3f, 0.5f, 0f, null));
          yield return Wait(waitTime);
          if (version == 2 && ++i % 5 == 0)
          {
            yield return Wait(30);
          }
      }
    }
  }

  internal class WallSlamScript : SecretBulletScript
  {

    internal class GravityBullet : SecretBullet
    {
      private const int LIFETIME = 30;
      private const int VANISHTIME = 120;
      private Vector2 gravity = Vector2.zero;
      private bool skipCollisions = true;
      private Vector2 startVelocity = Vector2.zero;
      private Rect roomFullBounds;
      public GravityBullet(Vector2 velocity, Vector2 gravity, Rect roomFullBounds) : base()
      {
        this.gravity        = gravity;
        this.startVelocity  = velocity;
        this.roomFullBounds = roomFullBounds;
      }

      public override void Initialize()
      {
        base.Initialize();
        this.skipCollisions = true;
        this.Projectile.BulletScriptSettings.surviveTileCollisions = true;
        this.Projectile.specRigidbody.OnPreTileCollision += (_,_,_,_) => {
          if (this.skipCollisions)
            PhysicsEngine.SkipCollision = true;
        };
      }

      public override IEnumerator Top()
      {
        AkSoundEngine.PostEvent(soundShoot, GameManager.Instance.PrimaryPlayer.gameObject);
        // Vector2 newVelocity = this.RealVelocity();
        Vector2 newVelocity = this.startVelocity;
        for (int i = 0; i < VANISHTIME; ++i)
        {
          if (i >= LIFETIME && this.skipCollisions && this.roomFullBounds.Contains(this.Position))
          {
            this.skipCollisions = false;
            this.Projectile.BulletScriptSettings.surviveTileCollisions = false;
          }
          newVelocity += gravity;
          this.ChangeDirection(new Direction(newVelocity.ToAngle(),DirectionType.Absolute));
          this.ChangeSpeed(new Speed(newVelocity.magnitude,SpeedType.Absolute));
          yield return Wait(1);
        }
        Vanish();
        yield break;
      }
    }

    private const int COUNT = 10;
    private const float SPREAD = 9f;
    private const float GRAVITY = 1.0f;
    private const float VELOCITY = 20f;
    private const float BASESPEED = VELOCITY*GRAVITY;
    private const int SHOTDELAY = 3;
    private const int SLAMS = 5;
    private const int SLAMDELAY = 60;

    private PathRect slamBoundsPath;

    protected override List<FluidBulletInfo> BuildChain()
    {
      this.slamBoundsPath = new PathRect(base.roomSlamBounds);

      bool vertical = Lazy.CoinFlip();
      FluidBulletInfo f = Run(DoTheThing(Lazy.CoinFlip() ? (vertical ? "up" : "left") : (vertical ? "down" : "right")));
      for (int i = 1 ; i < SLAMS; ++i)
      {
        vertical = (!vertical);
        f = f.And(DoTheThing(Lazy.CoinFlip() ? (vertical ? "up" : "left") : (vertical ? "down" : "right")),withDelay: i * SLAMDELAY);
      }

      return f.Finish();
    }

    private IEnumerator DoTheThing(string direction)
    {
      PlayerController p = GameManager.Instance.PrimaryPlayer;

      PathLine segment;
      if (direction.Equals("up"))
        segment = this.slamBoundsPath.Top();
      else if (direction.Equals("down"))
        segment = this.slamBoundsPath.Bottom();
      else if (direction.Equals("left"))
        segment = this.slamBoundsPath.Left();
      else if (direction.Equals("right"))
        segment = this.slamBoundsPath.Right();
      else // should never happen
        segment = this.slamBoundsPath.Top();
      Vector2 target =  segment.At(0.5f);

      Vector2 delta    = (target - p.CenterPosition);
      Vector2 gravity  = GRAVITY*delta.normalized;
      Vector2 gravityB = GRAVITY*(target - this.BulletBank.aiActor.sprite.WorldCenter).normalized;
      Vector2 baseVel  = -VELOCITY * gravityB;
      Speed s = new Speed(BASESPEED,SpeedType.Absolute);
      Offset o = Offset.OverridePosition(this.BulletBank.aiActor.sprite.WorldCenter);
      AkSoundEngine.PostEvent("sans_laugh", GameManager.Instance.PrimaryPlayer.gameObject);
      for(int i = 0; i < COUNT; ++i)
      {
        Vector2 bulletvel = baseVel.Rotate(UnityEngine.Random.Range(-SPREAD,SPREAD));
        Direction d = new Direction(bulletvel.ToAngle().Clamp180(),DirectionType.Absolute);
        this.Fire(o, d, s, new GravityBullet(bulletvel,gravityB,base.roomFullBounds));
        yield return Wait(SHOTDELAY);
      }

      p.SetInputOverride("comeonandslam");
      p.ForceStopDodgeRoll();
      int collisionmask = CollisionMask.LayerToMask(CollisionLayer.EnemyHitBox, CollisionLayer.EnemyCollider, CollisionLayer.Projectile);
      p.specRigidbody.AddCollisionLayerIgnoreOverride(collisionmask);
      Vector2 slamStart        = base.roomSlamBounds.position;
      Vector2 slamEnd          = base.roomSlamBounds.position + base.roomSlamBounds.size;
      Vector2 finalPos         = Vector2.zero;
      p.specRigidbody.Velocity = Vector2.zero;
      int framesToReachTarget = Mathf.FloorToInt(Mathf.Sqrt(2*delta.magnitude/GRAVITY)); // solve x = (0.5*a*t*t) for t
      for (int frames = 0; frames < framesToReachTarget; ++frames)
      {
        p.specRigidbody.Velocity += gravity;
        Vector2 oldPos = p.specRigidbody.Position.GetPixelVector2();
        Vector2 newPos = oldPos + p.specRigidbody.Velocity;
        if (BraveMathCollege.LineSegmentRectangleIntersection(oldPos, newPos, segment.start, segment.end, ref finalPos))
          break;
        p.transform.position = newPos;
        p.specRigidbody.Reinitialize();
        yield return Wait(1);
      }
      p.specRigidbody.RemoveCollisionLayerIgnoreOverride(collisionmask);
      p.specRigidbody.Velocity = Vector2.zero;
      p.transform.position = (finalPos != Vector2.zero) ? finalPos : target;
      p.specRigidbody.Reinitialize();
      yield return Wait(1);

      p.ClearInputOverride("comeonandslam");
      GameManager.Instance.MainCameraController.DoScreenShake(new ScreenShakeSettings(0.5f,6f,0.5f,0f), null);
      AkSoundEngine.PostEvent("undertale_damage", GameManager.Instance.PrimaryPlayer.gameObject);
    }
  }

  internal class ChainBulletScript : SecretBulletScript
  {

    public class ChainBullet : SecretBullet
    {
      public override IEnumerator Top()
      {
        AkSoundEngine.PostEvent(soundShoot, GameManager.Instance.PrimaryPlayer.gameObject);
        yield break;
      }
    }

    private const int PHASES          = 3;
    private const int STREAMSPERPHASE = 5;
    private const int PHASEDELAY      = 20;
    private const int SHOTSPERSTREAM  = 12;
    private const int SHOTDELAY       = 5;
    private const int SHOTSPEED       = 20;
    private const float MINDIST       = 12f;

    protected override List<FluidBulletInfo> BuildChain()
    {
      return Run(DoTheThing())
      .Finish();
    }

    private IEnumerator DoTheThing()
    {
      for (int i = 0; i < PHASES; ++i)
      {
        AkSoundEngine.PostEvent("sans_laugh", GameManager.Instance.PrimaryPlayer.gameObject);
        Vector2 ppos = GameManager.Instance.PrimaryPlayer.CenterPosition;
        List<Vector2> spawnPoints = new List<Vector2>(STREAMSPERPHASE);
        List<float> shotAngles = new List<float>(STREAMSPERPHASE);
        for (int s = 0; s < STREAMSPERPHASE; ++s)
        {
          Vector2 spawnPoint = base.roomBulletBounds.RandomPointOnPerimeter();
          while((ppos-spawnPoint).magnitude < MINDIST)
            spawnPoint = base.roomBulletBounds.RandomPointOnPerimeter();
          spawnPoints.Add(spawnPoint);
          shotAngles.Add((ppos-spawnPoint).ToAngle().Clamp180());
          DoomZone(spawnPoint, spawnPoints[s].RaycastToWall(shotAngles[s], base.roomFullBounds), 1f, PHASEDELAY / C.FPS, 10);
          AkSoundEngine.PostEvent(soundSpawn, GameManager.Instance.PrimaryPlayer.gameObject);
          yield return Wait(SHOTDELAY);
        }
        for (int j = 0; j < SHOTSPERSTREAM; ++j)
        {
          for (int s = 0; s < STREAMSPERPHASE; ++s)
            this.Fire(Offset.OverridePosition(spawnPoints[s]), new Direction(shotAngles[s],DirectionType.Absolute), new Speed(SHOTSPEED,SpeedType.Absolute), new ChainBullet());
          yield return Wait(SHOTDELAY);
        }
        yield return Wait(PHASEDELAY);
      }
    }
  }

  internal class SquareBulletScript : SecretBulletScript
  {
    public class SquareBullet : SecretBullet
    {
      private int goFrames;
      private int waitFrames;
      public SquareBullet(int goFrames = 30, int waitFrames = 60) : base()
      {
        this.waitFrames = waitFrames;
        this.goFrames = goFrames;
      }

      public override IEnumerator Top()
      {
        AkSoundEngine.PostEvent(soundShoot, GameManager.Instance.PrimaryPlayer.gameObject);
        yield return Wait(this.goFrames);
        float initSpeed = this.Speed;
        this.ChangeSpeed(new Speed(0,SpeedType.Absolute));
        yield return Wait(this.waitFrames);
        this.ChangeSpeed(new Speed(initSpeed,SpeedType.Absolute));
        this.ChangeDirection(new Direction(this.DirToNearestPlayer(),DirectionType.Absolute));
        AkSoundEngine.PostEvent(soundShoot, GameManager.Instance.PrimaryPlayer.gameObject);
        yield return Wait(120);
        Vanish();
        yield break;
      }
    }

    private const int SIDES        = 5;
    private const int COUNTPERSIDE = 3;
    private const float SPREAD     = 0.5f; // percent of each side filled with bullets
    private const float SPEED      = 25f;
    private const int GOFRAMES     = 15;
    private const int SHOTDELAY    = 4;
    private const int SIDEDELAY    = 8;

    private const float SIDESPAN   = 360.0f / SIDES;
    private const float SPREADSPAN = SPREAD * SIDESPAN;
    private const float OFFSET     = 0.5f * (COUNTPERSIDE - 1);
    private const int FINALDELAY   = ((SHOTDELAY * COUNTPERSIDE) + SIDEDELAY) * SIDES;

    protected override List<FluidBulletInfo> BuildChain()
    {
      return Run(DoTheThing())
      .Finish();
    }

    private IEnumerator DoTheThing()
    {
      float initAngle = Lazy.RandomAngle();
      for (int i = 0; i < SIDES; ++i)
      {
        float sideAngle = (initAngle + i * SIDESPAN).Clamp180();
        for (int j = 0; j < COUNTPERSIDE; ++j)
        {
          float launchAngle = sideAngle + SPREADSPAN * ((1f+j) / (1f+COUNTPERSIDE) - 0.5f);
          this.Fire(Offset.OverridePosition(this.BulletBank.aiActor.sprite.WorldCenter), new Direction(launchAngle.Clamp180(),DirectionType.Absolute), new Speed(SPEED,SpeedType.Absolute), new SquareBullet(GOFRAMES, FINALDELAY));
          yield return this.Wait(SHOTDELAY);
        }
        yield return this.Wait(SIDEDELAY);
      }
      yield return this.Wait(60);
      yield break;
    }
  }

  internal class HesitantBulletWallScript : SecretBulletScript
  {
    public class HesitantBullet : SecretBullet
    {

      private int waitFrames;
      public HesitantBullet(int waitFrames = 60) : base()
      {
        this.waitFrames = waitFrames;
      }

      public override IEnumerator Top()
      {
        // AkSoundEngine.PostEvent("megalo_pause", GameManager.Instance.DungeonMusicController.gameObject);
        AkSoundEngine.PostEvent(soundSpawn, GameManager.Instance.PrimaryPlayer.gameObject);
        float initSpeed = this.Speed;
        this.ChangeSpeed(new Speed(0,SpeedType.Absolute),waitFrames);
        yield return Wait(waitFrames);
        this.ChangeSpeed(new Speed(initSpeed*2,SpeedType.Absolute));
        AkSoundEngine.PostEvent(soundShoot, GameManager.Instance.PrimaryPlayer.gameObject);
        // AkSoundEngine.PostEvent("megalo_resume", GameManager.Instance.DungeonMusicController.gameObject);
        yield return Wait(120);
        Vanish();
        yield break;
      }
    }

    private const int COUNT       = 10;
    private const int WAIT        = 60;
    private const int SPAWN_DELAY = 5;
    private const float WALLWIDTH = 10f;
    private const float DISTANCE  = 7f;
    private const float SPEED     = 10f;
    private const float SPACING   = WALLWIDTH / COUNT;

    protected override List<FluidBulletInfo> BuildChain()
    {
      return Run(DoTheThing())
      .Finish();
    }

    private IEnumerator DoTheThing()
    {
      Vector2 incidentDirection, centerPoint, playerpos = GameManager.Instance.PrimaryPlayer.CenterPosition;
      do
      {
        incidentDirection = Lazy.RandomAngle().ToVector();
        centerPoint = playerpos + DISTANCE * incidentDirection;
      } while(IsPointInTile(centerPoint));
      List<Vector2> points = centerPoint.TangentLine(playerpos,WALLWIDTH).SampleUniform(COUNT);
      Direction towardsPlayerDirection = new Direction((-incidentDirection).ToAngle().Clamp180(),DirectionType.Absolute);
      foreach (Vector2 spawnPoint in points)
      {
        this.Fire(Offset.OverridePosition(spawnPoint), towardsPlayerDirection, new Speed(SPEED,SpeedType.Absolute), new HesitantBullet(WAIT));
        yield return this.Wait(SPAWN_DELAY);
      }
      yield break;
    }
  }

  internal class OrbitBulletScript : SecretBulletScript
  {

    public class OrbitBullet : SecretBullet
    {
      private Vector2 center;
      private float radius;
      private float captureAngle;
      private float framesToApproach;
      private float degreesToOrbit;
      private float framesToOrbit;
      private int delay;

      private Vector2 initialTarget;
      private Vector2 delta;

      private const float SPEED = 60f;
      private const int DELAY = 60;

      public OrbitBullet(Vector2 center, float radius, float captureAngle, float framesToApproach, float degreesToOrbit, float framesToOrbit, int delay)
        : base()
      {
        this.center           = center;
        this.radius           = radius;
        this.captureAngle     = captureAngle;
        this.framesToApproach = framesToApproach;
        this.degreesToOrbit   = degreesToOrbit;
        this.framesToOrbit    = framesToOrbit;
        this.delay            = delay;
      }

      public override void Initialize()
      {
        base.Initialize();

        this.Projectile.BulletScriptSettings.surviveTileCollisions = true;
        this.Projectile.specRigidbody.OnPreTileCollision += (_,_,_,_) => { PhysicsEngine.SkipCollision = true; };

        this.initialTarget = this.center + this.radius * this.captureAngle.ToVector();
        this.delta = this.initialTarget - this.Position;
        ChangeDirection(new Direction(delta.ToAngle(), DirectionType.Absolute));
        ChangeSpeed(new Speed(0, SpeedType.Absolute));
      }

      public override IEnumerator Top()
      {
        yield return Wait(this.delay);
        ChangeSpeed(new Speed(SPEED * delta.magnitude / (framesToApproach+1), SpeedType.Absolute));

        yield return Wait(framesToApproach);
        float degreesPerFrame = degreesToOrbit / framesToOrbit;
        float curAngle = captureAngle;
        float oldSpeed = this.Speed;
        this.UpdatePosition();
        ChangeSpeed(new Speed(0f,SpeedType.Absolute));
        this.UpdateVelocity();
        for (int i = 0; i < framesToOrbit; ++i)
        {
          yield return Wait(1);
          curAngle = (curAngle+degreesPerFrame).Clamp180();
          Vector2 newTarget = center + radius * curAngle.ToVector();
          ChangeDirection(new Direction((newTarget-this.Position).ToAngle(),DirectionType.Absolute));
          this.Position = newTarget;
        }
        AkSoundEngine.PostEvent(soundShoot, GameManager.Instance.PrimaryPlayer.gameObject);
        ChangeSpeed(new Speed(oldSpeed,SpeedType.Absolute));
        yield return Wait(DELAY);

        Vanish();
      }
    }

    private const float ROTATIONS       = 5.0f;
    private const int   COUNT           = 37;
    private const float OUTER_RADIUS    = 8f;
    private const float INNER_RADIUS    = 1f;
    private const int   SPAWN_GAP       = 2;
    private const float SPIRAL          = 1.0f;  // higher spiral factor = bullets form a spiral instead of a circle
    private const float ANGLE_DELTA     = ROTATIONS * 360.0f / COUNT;
    private const float APPROACH_FRAMES = 12f;
    private const float ORBIT_FRAMES    = 60f;

    protected override List<FluidBulletInfo> BuildChain()
    {
      return Run(DoTheThing())
      .Finish();
    }

    private IEnumerator DoTheThing()
    {
      Vector2 playerpos = GameManager.Instance.PrimaryPlayer.CenterPosition;
      for (int j = 0; j < COUNT; j++)
      {
        if (j % 2 == 0)
          AkSoundEngine.PostEvent(soundSpawn, GameManager.Instance.PrimaryPlayer.gameObject);
        yield return this.Wait(SPAWN_GAP);
        float realAngle = (j*ANGLE_DELTA).Clamp180();
        float targetRadius = INNER_RADIUS+(j*SPIRAL/COUNT);
        Bullet b = new OrbitBullet(playerpos, targetRadius, realAngle, APPROACH_FRAMES, 360f, ORBIT_FRAMES, SPAWN_GAP*(COUNT-j));
        Vector2 spawnPoint = playerpos + (targetRadius * realAngle.ToVector()) + (OUTER_RADIUS * (realAngle-90f).ToVector());
        this.Fire(Offset.OverridePosition(spawnPoint), b);
      }
      yield break;
    }
  }

  // Shoots a bunch of bullets from the ceiling of the current room
  internal class CeilingBulletsScript : SecretBulletScript
  {
    private const int COUNT = 16;
    private const int SPAWN_DELAY = 4;

    protected override List<FluidBulletInfo> BuildChain()
    {
      bool flip = Lazy.CoinFlip();

      return
        Run(Laugh(10))
          .And(DoTheThing(15, warn: true, reverse:  flip)                                   )
          .And(DoTheThing(15, warn: true, reverse: !flip), withDelay:      SPAWN_DELAY*COUNT)
          .And(DoTheThing(30,             reverse:  flip), withDelay: 10                    )
          .And(DoTheThing(30,             reverse: !flip), withDelay: 10 + SPAWN_DELAY*COUNT)
        .Then(Laugh(10))
          .And(DoTheThing(15, warn: true, reverse:  flip)                                   )
          .And(DoTheThing(15, warn: true, reverse: !flip), withDelay:      SPAWN_DELAY*COUNT)
          .And(DoTheThing(45,             reverse:  flip), withDelay: 20                    )
          .And(DoTheThing(45,             reverse: !flip), withDelay: 20 + SPAWN_DELAY*COUNT)
        .Finish();
    }

    private IEnumerator Laugh(float delay)
    {
      AkSoundEngine.PostEvent("sans_laugh", GameManager.Instance.PrimaryPlayer.gameObject);
      yield return this.Wait(delay);
    }

    private IEnumerator DoTheThing(float speed, bool reverse = false, bool warn = false)
    {
      float offset = base.roomBulletBounds.width / (float)COUNT;
      float angle = reverse ? 90f : -90f;
      List<Vector2> points = new List<Vector2>();
      for (float j = (reverse ? 0.5f : 0); j < COUNT; j++)
        points.Add(new Vector2(base.roomBulletBounds.xMin + j*offset, reverse ? base.roomBulletBounds.yMin : base.roomBulletBounds.yMax));

      for(int i = 0; i < points.Count; ++i)
      {
        if (warn)
        {
          DoomZone(points[i], points[i].RaycastToWall(angle, base.roomBulletBounds), 1f, COUNT / 15.0f, 20);
          if (i % 2 == 0)
            AkSoundEngine.PostEvent(soundSpawnQuiet, GameManager.Instance.PrimaryPlayer.gameObject);
        }
        yield return this.Wait(SPAWN_DELAY);
      }
      for(int i = 0; i < points.Count; ++i)
      {
        this.Fire(Offset.OverridePosition(points[i]), new Direction(angle, DirectionType.Absolute), new Speed(speed), new SecretBullet());
        if (i % 2 == 1)
          AkSoundEngine.PostEvent(soundShoot, GameManager.Instance.PrimaryPlayer.gameObject);
        yield return this.Wait(SPAWN_DELAY);
      }
      yield break;
    }
  }
}

}
