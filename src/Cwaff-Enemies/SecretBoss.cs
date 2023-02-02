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
  private const string spritePath    = "CwaffingTheGungy/Resources/room_mimic";
  private const string defaultSprite = "room_mimic_idle_001";
  private const string bossCardPath  = "CwaffingTheGungy/Resources/roomimic_bosscard.png";

  internal static GameObject napalmReticle      = null;
  internal static AIBulletBank.Entry boneBullet = null;
  public static void Init()
  {
    // Create our build-a-boss
    BuildABoss bb = BuildABoss.LetsMakeABoss<BossBehavior>(
      bossname, guid, $"{spritePath}/{defaultSprite}", new IntVector2(8, 9), subtitle, bossCardPath);
    // Set our stats
    bb.SetStats(health: 100f, weight: 200f, speed: 2f, collisionDamage: 1f,
      hitReactChance: 0.05f, collisionKnockbackStrength: 5f);
    // Set up our animations
    bb.InitSpritesFromResourcePath(spritePath);
      bb.AdjustAnimation("idle",   fps:   7f, loop: true);
      bb.AdjustAnimation("swirl",  fps:   9f, loop: false);
      bb.AdjustAnimation("scream", fps: 5.3f, loop: false);
      bb.AdjustAnimation("tell",   fps:   8f, loop: false);
      bb.AdjustAnimation("suck",   fps:   4f, loop: false);
      bb.AdjustAnimation("tell2",  fps:   6f, loop: false);
      bb.AdjustAnimation("puke",   fps:   7f, loop: false);
      bb.AdjustAnimation("intro",  fps:  11f, loop: false);
      bb.AdjustAnimation("die",    fps:   6f, loop: false);
    // Set our default pixel colliders
    bb.SetDefaultColliders(101,27,0,10);
    // Add custom animation to the generic intro doer, and add a specific intro doer as well
    bb.SetIntroAnimation("intro");
    bb.AddCustomIntro<BossIntro>();
    // Set up the boss's targeting and attacking scripts
    bb.TargetPlayer();
    // Add some named vfx pools to our bank of VFX
    bb.AddNamedVFX(VFX.vfxpool["Tornado"], "mytornado");

    // Add a random teleportation behavior
    // bb.CreateTeleportAttack<TeleportBehavior>(outAnim: "scream", inAnim: "swirl", attackCooldown: 3.5f);
    // Add a basic bullet attack
    // bb.CreateBulletAttack<CeilingBulletsScript>(fireAnim: "swirl", attackCooldown: 3.5f, fireVfx: "mytornado");
    // bb.CreateBulletAttack<SwirlScript>(fireAnim: "swirl", attackCooldown: 3.5f, fireVfx: "mytornado");
    bb.CreateBulletAttack<FancyBulletsScript>(fireAnim: "swirl", attackCooldown: 3.5f, fireVfx: "mytornado");
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
      yield return StartCoroutine(BH.WaitForSecondsInvariant(1.8f));
      AkSoundEngine.PostEvent("Play_BOSS_doormimic_lick_01", base.aiActor.gameObject);
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
      private float releaseAngle;

      private Vector2 initialTarget;
      private Vector2 delta;

      public FancyBullet(Vector2 center, float radius, float captureAngle, float framesToApproach, float degreesToOrbit, float framesToOrbit, float releaseAngle)
        : base("getboned")
      {
        this.center           = center;
        this.radius           = radius;
        this.captureAngle     = captureAngle;
        this.framesToApproach = framesToApproach;
        this.degreesToOrbit   = degreesToOrbit;
        this.framesToOrbit    = framesToOrbit;
        this.releaseAngle     = releaseAngle;
      }

      public override void Initialize()
      {
        this.Projectile.BulletScriptSettings.surviveTileCollisions = true;
        this.Projectile.specRigidbody.OnPreTileCollision += (_,_,_,_) => { PhysicsEngine.SkipCollision = true; };
        this.Projectile.ChangeTintColorShader(0f,new Color(1.0f,0.5f,0.5f,0.5f));
        this.initialTarget = this.center + this.radius * this.captureAngle.ToVector();
        this.delta = this.initialTarget - this.Position;
        ChangeDirection(new Direction(delta.ToAngle(),DirectionType.Absolute));
        ChangeSpeed(new Speed(C.PIXELS_PER_CELL * delta.magnitude / framesToApproach,SpeedType.Absolute));
        base.Initialize();
      }

      public override IEnumerator Top()
      {
        IEnumerator[] scripts = {OrbitAndScatter(),OrbitAndScatter()};
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
          curAngle = BraveMathCollege.ClampAngle180(curAngle+degreesPerFrame);
          Vector2 newTarget = center + radius * curAngle.ToVector();
          this.Position = newTarget;
          yield return Wait(1);
        }
        ChangeDirection(new Direction(curAngle,DirectionType.Absolute));
        yield return Wait(60);
      }
    }

    private const int COUNT = 16;
    private const float OUTER_RADIUS = 7f;
    private const float INNER_RADIUS = 3f;

    public override IEnumerator Top()
    {
      if (this.BulletBank?.aiActor?.TargetRigidbody == null)
        yield break;

      this.BulletBank.Bullets.Add(boneBullet);

      Vector2 playerpos = GameManager.Instance.PrimaryPlayer.CenterPosition;
      float angleDelta = 360.0f / COUNT;
      for (int j = 0; j < COUNT; j++)
      {
        float realAngle = BraveMathCollege.ClampAngle180(j*angleDelta);
        Bullet b = new FancyBullet(playerpos, INNER_RADIUS, realAngle, 120f, 720f, 120f, -1);
        Vector2 spawnPoint = playerpos + OUTER_RADIUS * realAngle.ToVector();
        this.Fire(Offset.OverridePosition(spawnPoint), b);
      }
      yield return this.Wait(120);
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
        Vector2 spawnPoint = new Vector2(roomBounds.xMin + j*offset, roomBounds.yMax - 1f);
        this.Fire(Offset.OverridePosition(spawnPoint), new Direction(-90f, DirectionType.Absolute), new Speed(9f), new Bullet("getboned"));
        DoomZone(spawnPoint, spawnPoint - new Vector2(0f,10f), 1f, 3f);
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
