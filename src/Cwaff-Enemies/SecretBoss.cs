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
  private const string bossname      = "Bossyboi";
  private const string subtitle      = "It's Literally Just a...";
  // private const string spritePath    = "CwaffingTheGungy/Resources/room_mimic";
  private const string spritePath    = "CwaffingTheGungy/Resources/bossyboi";
  private const string defaultSprite = "bossyboi_idle_001";
  private const string bossCardPath  = "CwaffingTheGungy/Resources/bossyboi_bosscard.png";

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
      bb.AdjustAnimation("idle",   fps:   7f, loop: true);
      bb.AdjustAnimation("teleport",   fps:   2f, loop: false);
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
    // Add custom animation to the generic intro doer, and add a specific intro doer as well
    bb.SetIntroAnimation("intro");
    bb.AddCustomIntro<BossIntro>();
    // Set up the boss's targeting and attacking scripts
    bb.TargetPlayer();
    // Add some named vfx pools to our bank of VFX
    bb.AddNamedVFX(VFX.vfxpool["Tornado"], "mytornado");

    // Add a random teleportation behavior
    // bb.CreateTeleportAttack<TeleportBehavior>(outAnim: "scream", inAnim: "swirl", attackCooldown: 3.5f);
    // bb.CreateTeleportAttack<TeleportBehavior>(outAnim: "teleport", inAnim: "teleport", attackCooldown: 1.15f);
    // Add a basic bullet attack
    // bb.CreateBulletAttack<CeilingBulletsScript>(fireAnim: "idle", attackCooldown: 1.15f, fireVfx: "mytornado");
    // bb.CreateBulletAttack<FancyBulletsScript>(fireAnim: "idle", attackCooldown: 1.15f, fireVfx: "mytornado");
    // bb.CreateBulletAttack<HesitantBulletWallScript>(fireAnim: "idle", attackCooldown: 1.15f, fireVfx: "mytornado");
    // bb.CreateBulletAttack<SquareBulletScript>(fireAnim: "idle", attackCooldown: 1.15f, fireVfx: "mytornado");
    bb.CreateBulletAttack<ChainBulletScript>(fireAnim: "idle", attackCooldown: 1.15f, fireVfx: "mytornado");
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

  // Creates a napalm-strike=esque danger zone
  internal static GameObject DoomZone(Vector2 start, Vector2 target, float width, float lifetime = -1f)
  {
    Vector2 delta                               = target - start;
    float angle                                 = BraveMathCollege.Atan2Degrees(target-start);
    GameObject reticle                          = UnityEngine.Object.Instantiate(napalmReticle);
    tk2dSlicedSprite m_extantReticleQuad        = reticle.GetComponent<tk2dSlicedSprite>();
      m_extantReticleQuad.dimensions              = C.PIXELS_PER_TILE * (new Vector2(delta.magnitude, width));
      m_extantReticleQuad.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
      m_extantReticleQuad.transform.position      = start + (Quaternion.Euler(0f, 0f, -90f) * delta.normalized * (width / 2f)).XY();
    if (lifetime > 0)
      reticle.ExpireIn(lifetime);
    return reticle;
  }

  internal class BossBehavior : BraveBehaviour
  {
    private void Start()
    {
      base.aiActor.healthHaver.OnPreDeath += (obj) =>
      {
        AkSoundEngine.PostEvent("megalo_stop", base.aiActor.gameObject);
        AkSoundEngine.PostEvent("Play_ENM_beholster_death_01", base.aiActor.gameObject);
      };
      base.healthHaver.healthHaver.OnDeath += (obj) =>
      {
        Chest chest2 = GameManager.Instance.RewardManager.SpawnTotallyRandomChest(GameManager.Instance.PrimaryPlayer.CurrentRoom.GetRandomVisibleClearSpot(1, 1));
        chest2.IsLocked = false;
      };
      // this.aiActor.knockbackDoer.SetImmobile(true, "laugh");
    }
  }

  [RequireComponent(typeof(GenericIntroDoer))]
  internal class BossIntro : SpecificIntroDoer
  {
    public override void PlayerWalkedIn(PlayerController player, List<tk2dSpriteAnimator> animators)
    {
      GameManager.Instance.StartCoroutine(PlaySound());
    }

    private IEnumerator PlaySound()
    {
      AkSoundEngine.PostEvent("megalo", base.aiActor.gameObject);
      yield return StartCoroutine(BH.WaitForSecondsInvariant(1.8f));
      AkSoundEngine.PostEvent("Play_BOSS_doormimic_lick_01", base.aiActor.gameObject);
      yield break;
    }
  }

  internal class ChainBulletScript : Script
  {

    public class ChainBullet : Bullet
    {
      public ChainBullet() : base("getboned")
      {
      }

      public override void Initialize()
      {
        this.Projectile.ChangeTintColorShader(0f,new Color(1.0f,0.5f,0.5f,0.5f));
        base.Initialize();
      }

      public override IEnumerator Top()
      {
        AkSoundEngine.PostEvent("Play_WPN_spacerifle_shot_01", GameManager.Instance.PrimaryPlayer.gameObject);
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
    public override IEnumerator Top()
    {

      if (this.BulletBank?.aiActor?.TargetRigidbody == null)
        yield break;

      this.BulletBank.Bullets.Add(boneBullet);

      Rect roomFullBounds = this.BulletBank.aiActor.GetAbsoluteParentRoom().GetBoundingRect();
      Rect roomBounds = roomFullBounds.Inset(topInset: 2f, rightInset: 2f, bottomInset: 4f, leftInset: 2f);
      for (int i = 0; i < PHASES; ++i)
      {
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
          DoomZone(spawnPoint, spawnPoints[s].RaycastToWall(shotAngles[s], roomFullBounds), 1f, 1f);
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

  internal class SquareBulletScript : Script
  {
    public class SquareBullet : Bullet
    {
      private int goFrames;
      private int waitFrames;
      public SquareBullet(int goFrames = 30, int waitFrames = 60) : base("getboned")
      {
        this.waitFrames = waitFrames;
        this.goFrames = goFrames;
      }

      public override void Initialize()
      {
        this.Projectile.ChangeTintColorShader(0f,new Color(1.0f,0.5f,0.5f,0.5f));
        base.Initialize();
      }

      public override IEnumerator Top()
      {
        AkSoundEngine.PostEvent("Play_WPN_spacerifle_shot_01", GameManager.Instance.PrimaryPlayer.gameObject);
        yield return Wait(this.goFrames);
        float initSpeed = this.Speed;
        this.ChangeSpeed(new Speed(0,SpeedType.Absolute));
        yield return Wait(this.waitFrames);
        this.ChangeSpeed(new Speed(initSpeed,SpeedType.Absolute));
        this.ChangeDirection(new Direction(this.DirToNearestPlayer(),DirectionType.Absolute));
        AkSoundEngine.PostEvent("Play_WPN_spacerifle_shot_01", GameManager.Instance.PrimaryPlayer.gameObject);
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
    public override IEnumerator Top()
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
          this.Fire(new Direction(launchAngle.Clamp180(),DirectionType.Absolute), new Speed(SPEED,SpeedType.Absolute), new SquareBullet(GOFRAMES, FINALDELAY));
          yield return this.Wait(SHOTDELAY);
        }
        yield return this.Wait(SIDEDELAY);
      }
      yield return this.Wait(60);
      yield break;
    }

  }

  internal class HesitantBulletWallScript : Script
  {
    public class HesitantBullet : Bullet
    {

      private int waitFrames;
      public HesitantBullet(int waitFrames = 60) : base("getboned")
      {
        this.waitFrames = waitFrames;
      }

      public override void Initialize()
      {
        this.Projectile.ChangeTintColorShader(0f,new Color(1.0f,0.5f,0.5f,0.5f));
        base.Initialize();
      }

      public override IEnumerator Top()
      {
        // AkSoundEngine.PostEvent("megalo_pause", GameManager.Instance.DungeonMusicController.gameObject);
        AkSoundEngine.PostEvent("Play_OBJ_turret_set_01", GameManager.Instance.PrimaryPlayer.gameObject);
        float initSpeed = this.Speed;
        this.ChangeSpeed(new Speed(0,SpeedType.Absolute),waitFrames);
        yield return Wait(waitFrames);
        this.ChangeSpeed(new Speed(initSpeed*2,SpeedType.Absolute));
        AkSoundEngine.PostEvent("Play_WPN_spacerifle_shot_01", GameManager.Instance.PrimaryPlayer.gameObject);
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
    public override IEnumerator Top()
    {

      if (this.BulletBank?.aiActor?.TargetRigidbody == null)
        yield break;

      this.BulletBank.Bullets.Add(boneBullet);

      Vector2 playerpos = GameManager.Instance.PrimaryPlayer.CenterPosition;

      float incidentDirection;
      Vector2 centerPoint;
      do
      {
        incidentDirection = UnityEngine.Random.Range(-180f,180f);
        centerPoint = playerpos + DISTANCE * incidentDirection.ToVector();
      } while(IsPointInTile(centerPoint));
      for (int j = -COUNT/2; j <= COUNT/2; j++)
      {
        float offset = j*SPACING;
        Vector2 spawnPoint = centerPoint + (offset * (incidentDirection - 90f).Clamp180().ToVector());
        Bullet b = new HesitantBullet(60);
        this.Fire(Offset.OverridePosition(spawnPoint), new Direction((incidentDirection+180f).Clamp180(),DirectionType.Absolute), new Speed(SPEED,SpeedType.Absolute), b);
        yield return this.Wait(5);
      }
      yield break;
    }
  }

  internal class FancyBulletsScript : Script
  {

    public class FancyBullet : Bullet
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

      public FancyBullet(Vector2 center, float radius, float captureAngle, float framesToApproach, float degreesToOrbit, float framesToOrbit, int delay)
        : base("getboned")
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
        this.Projectile.ChangeTintColorShader(0f,new Color(1.0f,0.5f,0.5f,0.5f));
        this.Projectile.BulletScriptSettings.surviveTileCollisions = true;
        this.Projectile.specRigidbody.OnPreTileCollision += (_,_,_,_) => { PhysicsEngine.SkipCollision = true; };

        this.initialTarget = this.center + this.radius * this.captureAngle.ToVector();
        this.delta = this.initialTarget - this.Position;
        ChangeDirection(new Direction(delta.ToAngle(), DirectionType.Absolute));
        ChangeSpeed(new Speed(0, SpeedType.Absolute));

        base.Initialize();
      }

      public override IEnumerator Top()
      {
        AkSoundEngine.PostEvent("Play_OBJ_turret_set_01", GameManager.Instance.PrimaryPlayer.gameObject);
        yield return Wait(this.delay);
        ChangeSpeed(new Speed(C.PIXELS_PER_CELL * delta.magnitude / framesToApproach, SpeedType.Absolute));
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
        for (int i = 0; i < framesToOrbit; ++i)
        {
          curAngle = (curAngle+degreesPerFrame).Clamp180();
          Vector2 newTarget = center + radius * curAngle.ToVector();
          this.Position = newTarget;
          yield return Wait(1);
        }
        AkSoundEngine.PostEvent("Play_WPN_spacerifle_shot_01", GameManager.Instance.PrimaryPlayer.gameObject);
        ChangeDirection(new Direction(curAngle,DirectionType.Absolute));
        yield return Wait(60);
      }
    }

    private const int COUNT = 16;
    private const float OUTER_RADIUS = 7f;
    private const float INNER_RADIUS = 3f;
    private const int SPAWN_GAP = 5;

    public override IEnumerator Top()
    {
      if (this.BulletBank?.aiActor?.TargetRigidbody == null)
        yield break;

      this.BulletBank.Bullets.Add(boneBullet);

      Vector2 playerpos = GameManager.Instance.PrimaryPlayer.CenterPosition;
      float angleDelta = 360.0f / COUNT;
      for (int j = 0; j < COUNT; j++)
      {
        AkSoundEngine.PostEvent("Play_OBJ_turret_set_01", GameManager.Instance.PrimaryPlayer.gameObject);
        yield return this.Wait(SPAWN_GAP);
        float realAngle = (j*angleDelta).Clamp180();
        Bullet b = new FancyBullet(playerpos, INNER_RADIUS, realAngle, 30f, 720f, 60f, SPAWN_GAP*(COUNT-j));
        Vector2 spawnPoint = playerpos + OUTER_RADIUS * realAngle.ToVector();
        this.Fire(Offset.OverridePosition(spawnPoint), b);
      }
      yield break;
    }
  }

  // Shoots a bunch of bullets from the ceiling of the current room
  internal class CeilingBulletsScript : Script
  {
    private const int COUNT = 16;

    public override IEnumerator Top()
    {
      if (this.BulletBank?.aiActor?.TargetRigidbody == null)
        yield break;

      this.BulletBank.Bullets.Add(boneBullet);
      Rect roomBounds = this.BulletBank.aiActor.GetAbsoluteParentRoom().GetBoundingRect().Inset(2f);
      float offset = roomBounds.width / (float)COUNT;
      for (int j = 0; j < COUNT; j++)
      {
        Vector2 spawnPoint = new Vector2(roomBounds.xMin + j*offset, roomBounds.yMax);
        DoomZone(spawnPoint, spawnPoint - new Vector2(0f,10f), 1f, 3f);
        AkSoundEngine.PostEvent("Play_OBJ_turret_set_01", GameManager.Instance.PrimaryPlayer.gameObject);
        yield return this.Wait(4);
      }
      for (int j = 0; j < COUNT; j++)
      {
        Vector2 spawnPoint = new Vector2(roomBounds.xMin + j*offset, roomBounds.yMax);
        this.Fire(Offset.OverridePosition(spawnPoint), new Direction(-90f, DirectionType.Absolute), new Speed(20f), new Bullet("getboned"));
        AkSoundEngine.PostEvent("Play_WPN_spacerifle_shot_01", GameManager.Instance.PrimaryPlayer.gameObject);
        yield return this.Wait(4);
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
