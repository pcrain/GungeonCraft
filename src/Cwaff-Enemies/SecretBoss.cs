using System;
using System.Collections.Generic;
using Gungeon;
using ItemAPI;
using EnemyAPI;
using UnityEngine;
using System.Collections;
using Dungeonator;
using System.Linq;
using Brave.BulletScript;

using MonoMod.RuntimeDetour;
using System.Reflection;

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

  internal static GameObject napalmReticle = null;
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
    bb.CreateBulletAttack<CeilingBulletsScript>(fireAnim: "swirl", attackCooldown: 3.5f, fireVfx: "mytornado");
    // bb.CreateBulletAttack<SwirlScript>(fireAnim: "swirl", attackCooldown: 3.5f, fireVfx: "mytornado");
    // // Add a bunch of simultaenous bullet attacks
    // bb.CreateSimultaneousAttack(new(){
    //   bb.CreateBulletAttack<RichochetScript> (add: false, tellAnim: "swirl", fireAnim: "suck", attackCooldown: 3.5f, fireVfx: "mytornado"),
    //   bb.CreateBulletAttack<RichochetScript2>(add: false, tellAnim: "swirl", fireAnim: "suck", attackCooldown: 3.5f, fireVfx: "mytornado"),
    //   });
    // // Add a sequential bullet attacks
    // bb.CreateSequentialAttack(new(){
    //   bb.CreateBulletAttack<RichochetScript> (add: false, tellAnim: "swirl", fireAnim: "suck", attackCooldown: 3.5f, fireVfx: "mytornado"),
    //   bb.CreateBulletAttack<RichochetScript2>(add: false, tellAnim: "swirl", fireAnim: "suck", attackCooldown: 3.5f, fireVfx: "mytornado"),
    //   });
    // Add our boss to the enemy database and to the first floor's boss pool
    bb.AddBossToGameEnemies("cg:secretboss");
    bb.AddBossToFloorPool(weight: 9999f, floors: Floors.CASTLEGEON);

    InitPrefabs();  // Do miscellaneous prefab loading
  }

  internal static void InitPrefabs()
  {
    AssetBundle sharedAssets2 = ResourceManager.LoadAssetBundle("shared_auto_002");
    GameObject prefabReticle = sharedAssets2.LoadAsset<GameObject>("NapalmStrikeReticle");
    napalmReticle = UnityEngine.Object.Instantiate(prefabReticle);
    tk2dSlicedSprite m_extantReticleQuad = napalmReticle.GetComponent<tk2dSlicedSprite>();
        m_extantReticleQuad.SetSprite(VFX.SpriteCollection, VFX.sprites["reticle-white"]);

    napalmReticle.SetActive(false); //make sure the projectile isn't an active game object
    FakePrefab.MarkAsFakePrefab(napalmReticle);  //mark the projectile as a prefab
    UnityEngine.Object.DontDestroyOnLoad(napalmReticle); //make sure the projectile isn't destroyed when loaded as a prefab
  }

  internal class Expiration : MonoBehaviour  // kill projectile after a fixed amount of time
  {
      // dummy component
      // private void Start()
      // {
      //     Projectile p = base.GetComponent<Projectile>();
      //     Invoke("Expire", expirationTimer);
      // }

      public void ExpireIn(float seconds)
      {
        this.StartCoroutine(Expire(seconds));
      }

      private IEnumerator Expire(float seconds)
      {
        yield return new WaitForSeconds(seconds);
        UnityEngine.Object.Destroy(this.gameObject);
      }
  }

  internal class BossBehavior : BraveBehaviour
  {
    private void Start()
    {
      //base.aiActor.HasBeenEngaged = true;
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

  // Shoots a bunch of bullets from the ceiling of the current room
  internal class CeilingBulletsScript : Script
  {
    private const int COUNT = 16;
    private bool firstTime = true;

    public override IEnumerator Top()
    {
      if (this.BulletBank?.aiActor?.TargetRigidbody == null)
        yield break;

      if (firstTime)
      {
        AIBulletBank.Entry reversible = EnemyDatabase.GetOrLoadByGuid("1bc2a07ef87741be90c37096910843ab").bulletBank.GetBullet("reversible");
        AIBulletBank.Entry e = new AIBulletBank.Entry(reversible);
          e.Name         = "spicyboi";
          e.BulletObject = (PickupObjectDatabase.GetById(59) as Gun).DefaultModule.projectiles[0].gameObject; // hegemony rifle
          e.PlayAudio    = true;
          e.AudioEvent   = "Play_WPN_golddoublebarrelshotgun_shot_01";
          e.AudioLimitOncePerAttack = false;
          e.AudioLimitOncePerFrame = false;
        this.BulletBank.Bullets.Add(e);
      }

      firstTime = false;
      Rect roomBounds = this.BulletBank.aiActor.GetAbsoluteParentRoom().GetBoundingRect();
      float offset = roomBounds.width / (float)COUNT;
      for (int j = 0; j < COUNT; j++)
      {
        Vector2 spawnPoint = new Vector2(roomBounds.xMin + j*offset, roomBounds.yMax - 1f);
        this.Fire(Offset.OverridePosition(spawnPoint), new Direction(-90f, DirectionType.Absolute), new Speed(9f), new Bullet("spicyboi"));
        // AkSoundEngine.PostEvent("Play_WPN_golddoublebarrelshotgun_shot_01", this.BulletBank.aiActor.gameObject);

        Vector2 target = spawnPoint - new Vector2(0f,10f);
        float startDistance = (target - spawnPoint).magnitude;
        Vector2 normalized = (target - spawnPoint).normalized;

        // Stealing from DirectionalAttackActiveItem
        const float attackLength = 8f;
        const float initialWidth = 4f;
        GameObject reticle = UnityEngine.Object.Instantiate(napalmReticle);
        tk2dSlicedSprite m_extantReticleQuad = reticle.GetComponent<tk2dSlicedSprite>();
          reticle.AddComponent<Expiration>().ExpireIn(3f);
          m_extantReticleQuad.dimensions = new Vector2(attackLength * 16f, initialWidth * 16f);
          m_extantReticleQuad.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
          // m_extantReticleQuad.transform.localRotation = Quaternion.Euler(0f, 0f, BraveMathCollege.Atan2Degrees(normalized));
        Vector2 vector = spawnPoint + normalized * startDistance + (Quaternion.Euler(0f, 0f, -90f) * normalized * (initialWidth / 2f)).XY();
        m_extantReticleQuad.transform.position = vector;
        yield return this.Wait(10);
      }
      yield break;
    }
  }

  internal class RichochetScript : Script  //Stolen and modified from base game DraGunGlockRicochet1
  {
    private bool firstTime = true;
    public override IEnumerator Top()
    {
      if (this.BulletBank?.aiActor?.TargetRigidbody == null)
        return null;

      if (firstTime)
      {
        firstTime = false;
        base.BulletBank.Bullets.Add(EnemyDatabase.GetOrLoadByGuid(EnemyGuidDatabase.Entries["dragun"]).bulletBank.GetBullet("ricochet"));
      }

      int count = 8;
      float start = -45f;
      float delta = 90f / (float)(count - 1);
      for (int j = 0; j < count; j++)
        Fire(new Direction(start + (float)j * delta, DirectionType.Aim), new Speed(9f), new Bullet("ricochet"));
      return null;
    }
  }

  internal class RichochetScript2 : Script  //Stolen and modified from base game DraGunGlockRicochet1
  {
    private bool firstTime = true;
    public override IEnumerator Top()
    {
      if (this.BulletBank?.aiActor?.TargetRigidbody == null)
        return null;

      if (firstTime)
      {
        firstTime = false;
        base.BulletBank.Bullets.Add(EnemyDatabase.GetOrLoadByGuid(EnemyGuidDatabase.Entries["dragun"]).bulletBank.GetBullet("ricochet"));
      }

      int count = 8;
      float start = 135f;
      float delta = 90f / (float)(count - 1);
      for (int j = 0; j < count; j++)
        Fire(new Direction(start + (float)j * delta, DirectionType.Aim), new Speed(9f), new Bullet("ricochet"));
      return null;
    }
  }

  internal class SwirlScript : Script // This BulletScript is just a modified version of the script BulletManShroomed, which you can find with dnSpy.
  {
    public override IEnumerator Top()
    {
      if (this.BulletBank && this.BulletBank.aiActor && this.BulletBank.aiActor.TargetRigidbody)
      {
        base.BulletBank.Bullets.Add(EnemyDatabase.GetOrLoadByGuid("796a7ed4ad804984859088fc91672c7f").bulletBank.GetBullet("default"));
        base.BulletBank.Bullets.Add(EnemyDatabase.GetOrLoadByGuid("1bc2a07ef87741be90c37096910843ab").bulletBank.GetBullet("reversible"));
      }
      for (int i = 0; i < 195; i++)
      {

        base.Fire(new Direction(360 - (i * 20), DirectionType.Absolute, -1f), new Speed(8f, SpeedType.Absolute), new Bullet("default"));
        yield return this.Wait(4);
        if (i % 10 == 9)
        {
          this.Fire(new Direction(0f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), new BurstBullet());
        }
      }

      yield break;
    }

    internal class BurstBullet : Bullet
    {
      // Token: 0x06000A99 RID: 2713 RVA: 0x000085A7 File Offset: 0x000067A7
      public BurstBullet() : base("reversible", false, false, false)
      {
      }

      public override IEnumerator Top()
      {
        this.Projectile.spriteAnimator.Play();
        yield break;
      }
      public override void OnBulletDestruction(Bullet.DestroyType destroyType, SpeculativeRigidbody hitRigidbody, bool preventSpawningProjectiles)
      {
        if (preventSpawningProjectiles)
        {
          return;
        }
        for (int i = 0; i < 4; i++)
        {
          base.Fire(new Direction((float)(i * 45), DirectionType.Absolute, -1f), new Speed(7f, SpeedType.Absolute), new Bullet("default"));
        }
      }
    }

  }

}

}
