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

  internal static GameObject napalmReticle      = null;
  internal static AIBulletBank.Entry boneBullet = null;
  internal static VFXPool bonevfx               = null;
  public static void Init()
  {
    // Create our build-a-boss
    BuildABoss bb = BuildABoss.LetsMakeABoss<BossBehavior>(
      bossname, guid, $"{spritePath}/{defaultSprite}", new IntVector2(8, 9), subtitle, bossCardPath);
      // bossname, guid, $"{spritePath}/{defaultSprite}", new IntVector2(8, 9), subtitle, bossCardPath);
    // Set our stats
    bb.SetStats(health: 100f, weight: 200f, speed: 2f, collisionDamage: 1f,
      hitReactChance: 0.05f, collisionKnockbackStrength: 5f);
    // Set up our animations
    bb.InitSpritesFromResourcePath(spritePath);
      bb.AdjustAnimation("idle",   fps:   12f, loop: true);
      bb.AdjustAnimation("idle_cloak",   fps:   12f, loop: true);
      bb.AdjustAnimation("decloak",   fps:   6f, loop: false);
      bb.AdjustAnimation("teleport_in",   fps:   60f, loop: false);
      bb.AdjustAnimation("teleport_out",   fps:   60f, loop: false);
      // bb.AdjustAnimation("swirl",  fps:   9f, loop: false);
      // bb.AdjustAnimation("scream", fps: 5.3f, loop: false);
      // bb.AdjustAnimation("tell",   fps:   8f, loop: false);
      // bb.AdjustAnimation("suck",   fps:   4f, loop: false);
      // bb.AdjustAnimation("tell2",  fps:   6f, loop: false);
      // bb.AdjustAnimation("puke",   fps:   7f, loop: false);
      // bb.AdjustAnimation("intro",  fps:  11f, loop: false);
      // bb.AdjustAnimation("die",    fps:   6f, loop: false);
    // Set our default pixel colliders
    // bb.SetDefaultColliders(101,27,0,10);
    bb.SetDefaultColliders(15,30,24,2);
    // Add custom animation to the generic intro doer, and add a specific intro doer as well
    bb.SetIntroAnimation("decloak");
    bb.prefab.GetComponent<GenericIntroDoer>().preIntroAnim = "idle_cloak";
    bb.AddCustomIntro<BossIntro>();
    // Set up the boss's targeting and attacking scripts
    bb.TargetPlayer();
    // Add some named vfx pools to our bank of VFX
    bb.AddNamedVFX(VFX.vfxpool["Tornado"], "mytornado");

    // Add a random teleportation behavior (moved to constructor)
    bb.CreateTeleportAttack<CustomTeleportBehavior>(
      goneTime: 0.25f, outAnim: "teleport_out", inAnim: "teleport_in", cooldown: 1f, probability: 200, inScript: typeof(TeleportScript));
    // Add a basic bullet attack
    bb.CreateBulletAttack<CeilingBulletsScript>(fireAnim: "laugh", cooldown: 1.5f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<OrbitBulletScript>(fireAnim: "throw_up", cooldown: 1.5f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<HesitantBulletWallScript>(fireAnim: "throw_down", cooldown: 1.5f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<SquareBulletScript>(fireAnim: "throw_left", cooldown: 1.5f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<ChainBulletScript>(fireAnim: "throw_right", cooldown: 1.5f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<WallSlamScript>(fireAnim: "laugh", cooldown: 2.5f, attackCooldown: 0.15f);
    // Add a bunch of simultaenous bullet attacks
    // bb.CreateSimultaneousAttack(new(){
    //   bb.CreateBulletAttack<RichochetScript> (add: false, tellAnim: "swirl", fireAnim: "suck", attackCooldown: 3.5f, fireVfx: "mytornado"),
    //   bb.CreateBulletAttack<RichochetScript2>(add: false, tellAnim: "swirl", fireAnim: "suck", attackCooldown: 3.5f, fireVfx: "mytornado"),
    //   bb.CreateBulletAttack<CeilingBulletsScript>(add: false, fireAnim: "swirl", attackCooldown: 3.5f, fireVfx: "mytornado"),
    //   });
    // // Add a sequential bullet attacks
    // bb.CreateSequentialAttack(new(){
    //   bb.CreateBulletAttack<RichochetScript> (add: false, tellAnim: "swirl", fireAnim: "suck", attackCooldown: 3.5f, fireVfx: "mytornado"),
    //   bb.CreateBulletAttack<RichochetScript2>(add: false, tellAnim: "swirl", fireAnim: "suck", attackCooldown: 3.5f, fireVfx: "mytornado"),
    //   });
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
        // quad.UpdateIndices();
        quad.UpdateZDepth();
        yield return null;
      }
      // restore reticle riser settings
      this.gameObject.GetOrAddComponent<ReticleRiserEffect>().NumRisers = 3;
    }
  }

  // Creates a napalm-strike=esque danger zone
  internal static GameObject DoomZone(Vector2 start, Vector2 target, float width, float lifetime = -1f, int growthTime = 0)
  {
    Vector2 delta                               = target - start;
    float angle                                 = BraveMathCollege.Atan2Degrees(target-start);
    GameObject reticle                          = UnityEngine.Object.Instantiate(napalmReticle);
    tk2dSlicedSprite m_extantReticleQuad        = reticle.GetComponent<tk2dSlicedSprite>();
      if (growthTime == 0)
        m_extantReticleQuad.dimensions              = C.PIXELS_PER_TILE * (new Vector2(delta.magnitude, width));
      else
      {
        UnityEngine.Object.Destroy(reticle.GetComponent<ReticleRiserEffect>());
        m_extantReticleQuad.dimensions              = C.PIXELS_PER_TILE * (new Vector2(delta.magnitude / growthTime, width));
        reticle.AddComponent<DoomZoneGrowth>().Lengthen(delta.magnitude,growthTime);
      }
      m_extantReticleQuad.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
      m_extantReticleQuad.transform.position      = start + (Quaternion.Euler(0f, 0f, -90f) * delta.normalized * (width / 2f)).XY();
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

  internal class BossBehavior : BraveBehaviour
  {
    private bool hasFinishedIntro = false;
    private float yoffset = 0;

    private void Start()
    {
      base.aiActor.healthHaver.OnPreDeath += (obj) =>
      {
        FlipSpriteIfNecessary(forceUnflip: true);
        AkSoundEngine.PostEvent("megalo_stop", base.aiActor.gameObject);
        AkSoundEngine.PostEvent("Play_ENM_beholster_death_01", base.aiActor.gameObject);
      };
      base.healthHaver.healthHaver.OnDeath += (obj) =>
      {
        FlipSpriteIfNecessary(forceUnflip: true);
        Chest chest2 = GameManager.Instance.RewardManager.SpawnTotallyRandomChest(GameManager.Instance.PrimaryPlayer.CurrentRoom.GetRandomVisibleClearSpot(1, 1));
        chest2.IsLocked = false;
      };
      // this.aiActor.knockbackDoer.SetImmobile(true, "laugh");
    }

    public void FinishedIntro()
    {
      hasFinishedIntro = true;
    }

    private void Update()
    {
      const float JIGGLE = 4.0f;
      const float SPEED = 4.0f;
      FlipSpriteIfNecessary();

      if (!hasFinishedIntro)
        return;
      yoffset = Mathf.CeilToInt(JIGGLE * Mathf.Sin(SPEED*BraveTime.ScaledTimeSinceStartup))/16.0f;
      base.sprite.transform.localPosition += new Vector3(0,yoffset,0);
      if (UnityEngine.Random.Range(0.0f,1.0f) < 0.5f)
        SpawnDust(base.specRigidbody.UnitCenter);
    }

    private void FlipSpriteIfNecessary(bool forceUnflip = false)
    {
      bool lastFlip = base.sprite.FlipX;
      bool shouldFlip = (GameManager.Instance.BestActivePlayer.sprite.WorldBottomCenter.x < base.sprite.WorldBottomCenter.x);
      base.sprite.FlipX = shouldFlip && (!forceUnflip);
      base.sprite.transform.localPosition = base.specRigidbody.UnitBottomCenter.RoundToInt();
      if (base.sprite.FlipX)
          base.sprite.transform.localPosition += new Vector3(base.sprite.GetUntrimmedBounds().size.x / 2, 0f, 0f);
      else
          base.sprite.transform.localPosition -= new Vector3(base.sprite.GetUntrimmedBounds().size.x / 2, 0f, 0f);
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
      Rect roomTrimmedBounds = roomFullBounds.Inset(4f);
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
      AkSoundEngine.PostEvent("megalo", base.aiActor.gameObject);
      yield return StartCoroutine(BH.WaitForSecondsInvariant(1.8f));
      AkSoundEngine.PostEvent("Play_BOSS_doormimic_lick_01", base.aiActor.gameObject);
      yield break;
    }

    public override void EndIntro()
    {
      base.aiActor.gameObject.GetComponent<BossBehavior>().FinishedIntro();
    }
  }

  internal class CustomTeleportBehavior : TeleportBehavior
  {
    private bool playedSound = false;
    private Vector3 oldPos = Vector3.zero;
    private Vector3 newPos = Vector3.zero;
    public override ContinuousBehaviorResult ContinuousUpdate()
    {
      if (State == TeleportState.TeleportOut)
      {
        if (!playedSound)
        {
          oldPos = base.m_aiActor.Position;
          AkSoundEngine.PostEvent("teledash", GameManager.Instance.PrimaryPlayer.gameObject);
        }
        playedSound = true;
      }
      else if (State == TeleportState.TeleportIn)
      {
        if (playedSound)
        {
          newPos = base.m_aiActor.Position;
          Vector3 delta = (newPos-oldPos);
          for(int i = 0; i < 10; ++i)
            SpawnDust(oldPos + (i/10.0f) * delta);
        }
        playedSound = false;
      }
      return base.ContinuousUpdate();
    }
  }

  internal class TeleportScript : Script
  {
      public override IEnumerator Top()
      {
        AkSoundEngine.PostEvent("teledash", GameManager.Instance.PrimaryPlayer.gameObject);
        yield break;
      }
  }

  internal class SecretBullet : Bullet
  {
      public SecretBullet() : base("getboned")
      {
      }

      public override void Initialize()
      {
        this.Projectile.ChangeTintColorShader(0f,new Color(1.0f,0.5f,0.5f,0.5f));
        base.Initialize();
      }
  }

  internal class WallSlamScript : FluidBulletScript
  {

    internal class GravityBullet : SecretBullet
    {
      private const int LIFETIME = 30;
      private const int VANISHTIME = 120;
      private Vector2 gravity = Vector2.zero;
      private Vector2 startVelocity = Vector2.zero;
      private bool skipCollisions = true;
      public GravityBullet(Vector2 velocity, Vector2 gravity) : base()
      {
        this.startVelocity  = velocity;
        this.gravity        = gravity;
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
        Rect roomFullBounds = this.BulletBank.aiActor.GetAbsoluteParentRoom().GetBoundingRect();
        AkSoundEngine.PostEvent(soundShoot, GameManager.Instance.PrimaryPlayer.gameObject);
        Vector2 newVelocity = this.startVelocity;
        for (int i = 0; i < VANISHTIME; ++i)
        {
          if (i >= LIFETIME && this.skipCollisions && roomFullBounds.Contains(this.Position))
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

    protected override List<FluidBulletInfo> BuildChain()
    {
      return Run(DoTheThing())
      .Finish();
    }

    private const int COUNT = 10;
    private IEnumerator DoTheThing()
    {
      if (this.BulletBank?.aiActor?.TargetRigidbody == null)
        yield break;

      this.BulletBank.Bullets.Add(boneBullet);

      PlayerController p = GameManager.Instance.PrimaryPlayer;
      Rect roomFullBounds = this.BulletBank.aiActor.GetAbsoluteParentRoom().GetBoundingRect();
      Rect slamBounds = roomFullBounds.Inset(topInset: 2f, rightInset: 2.5f, bottomInset: 2f, leftInset: 1.5f);
      if (!slamBounds.Contains(p.specRigidbody.Position.GetPixelVector2()))
        yield break;

      Vector2 gravity  = 1.2f*(p.CenterPosition - this.BulletBank.aiActor.CenterPosition).normalized;
      Vector2 baseVel  = -20 * gravity;
      float baseSpeed = baseVel.magnitude;
      float baseDir   = baseVel.ToAngle();
      Speed s = new Speed(baseSpeed,SpeedType.Absolute);
      Offset o = Offset.OverridePosition(this.BulletBank.aiActor.sprite.WorldCenter);
      AkSoundEngine.PostEvent("sans_laugh", GameManager.Instance.PrimaryPlayer.gameObject);
      for(int i = 0; i < COUNT; ++i)
      {
        Vector2 bulletvel = baseVel.Rotate(UnityEngine.Random.Range(-18f,18f));
        Direction d = new Direction(bulletvel.ToAngle().Clamp180(),DirectionType.Absolute);
        try
        {
          this.Fire(o, d, s, new GravityBullet(bulletvel,gravity));
        }
        catch (Exception ex)
        {
          ETGModConsole.Log($"{ex}");
        }
        yield return Wait(3);
      }

      if (!slamBounds.Contains(p.specRigidbody.Position.GetPixelVector2()))
        yield break;

      p.SetInputOverride("comeonandslam");
      Vector2 slamStart        = slamBounds.position;
      Vector2 slamEnd          = slamBounds.position + slamBounds.size;
      Vector2 finalPos         = Vector2.zero;
      p.specRigidbody.Velocity = Vector2.zero;
      for (int frames = 0; frames < 120; ++frames) // give up after two seconds
      {
        p.specRigidbody.Velocity += gravity;
        Vector2 oldPos = p.specRigidbody.Position.GetPixelVector2();
        Vector2 newPos = oldPos + p.specRigidbody.Velocity;
        if (BraveMathCollege.LineSegmentRectangleIntersection(oldPos, newPos, slamStart, slamEnd, ref finalPos))
          break;
        p.transform.position = newPos;
        p.specRigidbody.Reinitialize();
        yield return Wait(1);
      }
      p.specRigidbody.Velocity = Vector2.zero;
      p.transform.position = finalPos;
      p.specRigidbody.Reinitialize();
      yield return Wait(1);

      p.ClearInputOverride("comeonandslam");
      GameManager.Instance.MainCameraController.DoScreenShake(new ScreenShakeSettings(0.5f,6f,0.5f,0f), null);
      AkSoundEngine.PostEvent("undertale_damage", GameManager.Instance.PrimaryPlayer.gameObject);
    }
  }

  internal class ChainBulletScript : FluidBulletScript
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
    private const int STREAMDELAY     = 20;
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

      if (this.BulletBank?.aiActor?.TargetRigidbody == null)
        yield break;

      this.BulletBank.Bullets.Add(boneBullet);

      Rect roomFullBounds = this.BulletBank.aiActor.GetAbsoluteParentRoom().GetBoundingRect();
      ETGModConsole.Log($"room size:{roomFullBounds.position},{roomFullBounds.size}");
      Rect roomBounds = roomFullBounds.Inset(topInset: 2f, rightInset: 2f, bottomInset: 4f, leftInset: 2f);
      for (int i = 0; i < PHASES; ++i)
      {
        AkSoundEngine.PostEvent("sans_laugh", GameManager.Instance.PrimaryPlayer.gameObject);
        Vector2 ppos = GameManager.Instance.PrimaryPlayer.CenterPosition;
        List<Vector2> spawnPoints = new List<Vector2>(STREAMSPERPHASE);
        List<float> shotAngles = new List<float>(STREAMSPERPHASE);
        for (int s = 0; s < STREAMSPERPHASE; ++s)
        {
          Vector2 spawnPoint = roomBounds.RandomPointOnPerimeter();
          while((ppos-spawnPoint).magnitude < MINDIST)
            spawnPoint = roomBounds.RandomPointOnPerimeter();
          spawnPoints.Add(spawnPoint);
          shotAngles.Add((ppos-spawnPoint).ToAngle().Clamp180());
          DoomZone(spawnPoint, spawnPoints[s].RaycastToWall(shotAngles[s], roomFullBounds), 1f, STREAMDELAY / 60.0f, 10);
          AkSoundEngine.PostEvent(soundSpawn, GameManager.Instance.PrimaryPlayer.gameObject);
          yield return Wait(SHOTDELAY);
        }
        yield return Wait(STREAMDELAY);
        for (int j = 0; j < SHOTSPERSTREAM; ++j)
        {
          for (int s = 0; s < STREAMSPERPHASE; ++s)
            this.Fire(Offset.OverridePosition(spawnPoints[s]), new Direction(shotAngles[s],DirectionType.Absolute), new Speed(SHOTSPEED,SpeedType.Absolute), new ChainBullet());
          yield return Wait(SHOTDELAY);
        }
      }
    }
  }

  internal class SquareBulletScript : FluidBulletScript
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

    private const int SIDES        = 4;
    private const int COUNTPERSIDE = 3;
    private const float SPREAD     = 0.5f; // percent of each side filled with bullets
    private const float SPEED      = 25f;
    private const int GOFRAMES     = 15;
    private const int SHOTDELAY    = 5;
    private const int SIDEDELAY    = 10;

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

      if (this.BulletBank?.aiActor?.TargetRigidbody == null)
        yield break;

      this.BulletBank.Bullets.Add(boneBullet);

      float initAngle = UnityEngine.Random.Range(-180f,180f);
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

  internal class HesitantBulletWallScript : FluidBulletScript
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

      if (this.BulletBank?.aiActor?.TargetRigidbody == null)
        yield break;

      this.BulletBank.Bullets.Add(boneBullet);

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
        this.Fire(Offset.OverridePosition(spawnPoint), towardsPlayerDirection, new Speed(SPEED,SpeedType.Absolute), new HesitantBullet(60));
        yield return this.Wait(5);
      }
      yield break;
    }
  }

  internal class OrbitBulletScript : FluidBulletScript
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
        ChangeSpeed(new Speed(60.0f * delta.magnitude / (framesToApproach+1), SpeedType.Absolute));
        // IEnumerator[] scripts = {OrbitAndScatter(),OrbitAndScatter()};
        IEnumerator[] scripts = {OrbitAndScatter()};
        foreach(IEnumerator e in scripts)
          while(e.MoveNext())
            yield return e.Current;
        Vanish();
      }

      public IEnumerator OrbitAndScatter()
      {
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
        // ChangeDirection(new Direction(curAngle,DirectionType.Absolute));
        yield return Wait(60);
      }
    }

    private const float ROTATIONS    = 5.0f;
    private const int   COUNT        = 37;
    private const float OUTER_RADIUS = 8f;
    private const float INNER_RADIUS = 1f;
    private const int   SPAWN_GAP    = 2;
    private const float SPIRAL       = 1.0f;  // higher spiral factor = bullets form a spiral instead of a circle
    private const float ANGLE_DELTA  = ROTATIONS * 360.0f / COUNT;

    protected override List<FluidBulletInfo> BuildChain()
    {
      return Run(DoTheThing())
      .Finish();
    }

    private IEnumerator DoTheThing()
    {
      if (this.BulletBank?.aiActor?.TargetRigidbody == null)
        yield break;

      this.BulletBank.Bullets.Add(boneBullet);

      Vector2 playerpos = GameManager.Instance.PrimaryPlayer.CenterPosition;
      for (int j = 0; j < COUNT; j++)
      {
        if (j % 2 == 0)
          AkSoundEngine.PostEvent(soundSpawn, GameManager.Instance.PrimaryPlayer.gameObject);
        yield return this.Wait(SPAWN_GAP);
        float realAngle = (j*ANGLE_DELTA).Clamp180();
        float targetRadius = INNER_RADIUS+(j*SPIRAL/COUNT);
        Bullet b = new OrbitBullet(playerpos, targetRadius, realAngle, 12f, 360f, 60f, SPAWN_GAP*(COUNT-j));
        Vector2 spawnPoint = playerpos + (targetRadius * realAngle.ToVector()) + (OUTER_RADIUS * (realAngle-90f).ToVector());
        this.Fire(Offset.OverridePosition(spawnPoint), b);
      }
      yield break;
    }
  }

  // Shoots a bunch of bullets from the ceiling of the current room
  internal class CeilingBulletsScript : FluidBulletScript
  {
    private const int COUNT = 16;
    private const int SPAWN_DELAY = 4;
    private Rect roomBounds;

    protected override List<FluidBulletInfo> BuildChain()
    {
      if (this.BulletBank?.aiActor?.TargetRigidbody == null)
        return new();
      this.BulletBank.Bullets.Add(boneBullet);

      Rect roomFullBounds = this.BulletBank.aiActor.GetAbsoluteParentRoom().GetBoundingRect();
      this.roomBounds = roomFullBounds.Inset(topInset: 2f, rightInset: 2f, bottomInset: 4f, leftInset: 2f);

      return
        Run(Laugh(10))
          .And(DoTheThing(15, warn: true))
          .And(DoTheThing(15, warn: true , reverse: true), withDelay: SPAWN_DELAY*COUNT)
          .And(DoTheThing(30               ),              withDelay: 10)
          .And(DoTheThing(30, reverse: true),              withDelay: 10 + SPAWN_DELAY*COUNT)
        .Then(Laugh(10))
          .And(DoTheThing(15, warn: true))
          .And(DoTheThing(15, warn: true, reverse: true),  withDelay: SPAWN_DELAY*COUNT)
          .And(DoTheThing(45               ),              withDelay: 20)
          .And(DoTheThing(45, reverse: true),              withDelay: 20 + SPAWN_DELAY*COUNT)
        .Finish();
    }

    private IEnumerator Laugh(float delay)
    {
      AkSoundEngine.PostEvent("sans_laugh", GameManager.Instance.PrimaryPlayer.gameObject);
      yield return this.Wait(delay);
    }

    private IEnumerator DoTheThing(float speed, bool reverse = false, bool warn = false)
    {
      float offset = roomBounds.width / (float)COUNT;
      for (float j = (reverse ? 0.5f : 0); j < COUNT; j++)
      {
        Vector2 topPoint = new Vector2(roomBounds.xMin + j*offset, reverse ? roomBounds.yMin : roomBounds.yMax);
        if (warn)
          DoomZone(topPoint, topPoint.RaycastToWall(reverse ? 90f : -90f, roomBounds), 1f, COUNT / 15.0f, 20);
        if ((int)j % 2 == 0)
          AkSoundEngine.PostEvent(soundSpawnQuiet, GameManager.Instance.PrimaryPlayer.gameObject);
        yield return this.Wait(SPAWN_DELAY);
      }
      for (float j = (reverse ? 0.5f : 0); j < COUNT; j++)
      {
        Vector2 topPoint = new Vector2(roomBounds.xMin + j*offset, reverse ? roomBounds.yMin : roomBounds.yMax);
        this.Fire(Offset.OverridePosition(topPoint), new Direction(reverse ? 90f : -90f, DirectionType.Absolute), new Speed(speed), new SecretBullet());
        if ((int)j % 2 == 1)
          AkSoundEngine.PostEvent(soundShoot, GameManager.Instance.PrimaryPlayer.gameObject);
        yield return this.Wait(SPAWN_DELAY);
      }
      yield break;
    }
  }

  internal class RichochetScript : Script  //Stolen and modified from base game DraGunGlockRicochet1
  {
    protected float start = -45f;
    public override IEnumerator Top() {
      if (this.BulletBank?.aiActor?.TargetRigidbody == null)
        return null;
      base.BulletBank.AddBulletFromEnemy("dragun","ricochet");
      int count = 8;
      float delta = 90f / (float)(count - 1);
      for (int j = 0; j < count; j++)
        Fire(new Direction(start + (float)j * delta, DirectionType.Aim), new Speed(9f), new Bullet("ricochet"));
      return null;
    }
  }

  internal class RichochetScript2 : RichochetScript
  {
    public override IEnumerator Top() {
      start = 135f;
      return base.Top();
    }
  }
}

}
