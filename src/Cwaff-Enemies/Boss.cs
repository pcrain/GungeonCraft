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
// using GungeonAPI;

namespace CwaffingTheGungy
{
public class RoomMimic : AIActor
{
  const string BULLET_KIN_GUID = "01972dee89fc4404a5c408d50007dad5";

  public static GameObject prefab;
  public static readonly string guid = "Room Mimic";
  public static GameObject shootpoint;
  private static string BossCardPath = "CwaffingTheGungy/Resources/roomimic_bosscard.png";
  public static string TargetVFX;
  public static void Init()
  {
    // source = EnemyDatabase.GetOrLoadByGuid("c50a862d19fc4d30baeba54795e8cb93");
    if (prefab != null || BossBuilder.Dictionary.ContainsKey(guid))
      return;
    prefab = BossBuilder.BuildPrefab("Room Mimic", guid, spritePaths[0], new IntVector2(0, 0), new IntVector2(8, 9), false, true);
    EnemyBehavior companion = BH.AddSaneDefaultBossBehavior(prefab,"Room Mimic","Face Off!",BossCardPath);
      companion.aiActor.knockbackDoer.weight = 200;
      companion.aiActor.MovementSpeed = 2f;
      companion.aiActor.CollisionDamage = 1f;
      companion.aiActor.aiAnimator.HitReactChance = 0.05f;
      companion.aiActor.healthHaver.SetHealthMaximum(1000f);
      companion.aiActor.healthHaver.ForceSetCurrentHealth(1000f);
      companion.aiActor.CollisionKnockbackStrength = 5f;
      companion.aiActor.specRigidbody.SetDefaultColliders(101,27);
      companion.aiActor.CorpseObject = EnemyDatabase.GetOrLoadByGuid(BULLET_KIN_GUID).CorpseObject;

    // Add custom animation to the generic intro doer, and add a specific intro doer as well
    GenericIntroDoer miniBossIntroDoer = prefab.GetComponent<GenericIntroDoer>();
      miniBossIntroDoer.introAnim = "intro"; //TODO: check if this actually exists
      prefab.AddComponent<RoomMimicIntro>();

    // Set up sprites, using BH.Range for easy consecutively-numbered sprites
    // tk2dSpriteCollectionData bossSprites = BH.LoadSpriteCollection(prefab,spritePaths);
    //   companion.AddAnimation(bossSprites, BH.Range(0, 7 ), "idle",     7f, true, DirectionalAnimation.DirectionType.Single);
    //   companion.AddAnimation(bossSprites, BH.Range(8, 15), "swirl",    9f, true);
    //   companion.AddAnimation(bossSprites, BH.Range(16,19), "scream", 5.3f, true);
    //   companion.AddAnimation(bossSprites, BH.Range(20,24), "tell",     8f, false);
    //   companion.AddAnimation(bossSprites, BH.Range(25,41), "suck",     4f, false);
    //   companion.AddAnimation(bossSprites, BH.Range(42,47), "tell2",    6f, false);
    //   companion.AddAnimation(bossSprites, BH.Range(48,54), "puke",     7f, false);
    //   companion.AddAnimation(bossSprites, BH.Range(55,75), "intro",   11f, false);
    //   companion.AddAnimation(bossSprites, BH.Range(76,86), "die",      6f, false);

    companion.InitSpritesFromResourcePath("CwaffingTheGungy/Resources/room_mimic");
      companion.AdjustAnimation("idle",     7f, true);
      companion.AdjustAnimation("swirl",    9f, true);
      companion.AdjustAnimation("scream", 5.3f, true);
      companion.AdjustAnimation("tell",     8f, false);
      companion.AdjustAnimation("suck",     4f, false);
      companion.AdjustAnimation("tell2",    6f, false);
      companion.AdjustAnimation("puke",     7f, false);
      companion.AdjustAnimation("intro",   11f, false);
      companion.AdjustAnimation("die",      6f, false);

    shootpoint = new GameObject("attach");
    shootpoint.transform.parent = companion.transform;
    shootpoint.transform.position = companion.sprite.WorldCenter;
    GameObject m_CachedGunAttachPoint = companion.transform.Find("attach").gameObject;

    BehaviorSpeculator bs = prefab.GetComponent<BehaviorSpeculator>();
      bs.CopySaneDefaultBehavior(EnemyDatabase.GetOrLoadByGuid(BULLET_KIN_GUID).behaviorSpeculator);
      bs.TargetBehaviors = new List<TargetBehaviorBase>
      {
        new TargetPlayerBehavior
        {
          Radius = 35f,
          LineOfSight = false,
          ObjectPermanence = true,
          SearchInterval = 0.25f,
          PauseOnTargetSwitch = false,
          PauseTime = 0.25f
        }
      };
      bs.AttackBehaviorGroup.AttackBehaviors = new List<AttackBehaviorGroup.AttackGroupItem>
      {
        new AttackBehaviorGroup.AttackGroupItem()
        {

          Probability = 1,
          Behavior = new ShootBehavior {
            ShootPoint = m_CachedGunAttachPoint,
            BulletScript = new CustomBulletScriptSelector(typeof(SwirlScript)),
            LeadAmount = 0f,
            AttackCooldown = 3.5f,
            FireAnimation = "swirl",
            RequiresLineOfSight = false,
            StopDuring = ShootBehavior.StopType.Attack,
            Uninterruptible = true
          },
          NickName = "Swirl Whirly"

        },
        new AttackBehaviorGroup.AttackGroupItem()
        {
          Probability = 1,
          Behavior = new ShootBehavior {
            ShootPoint = m_CachedGunAttachPoint,
            BulletScript = new CustomBulletScriptSelector(typeof(AAAAAAAAAAAAAAScript)),
            LeadAmount = 0f,
            AttackCooldown = 3.5f,
            FireAnimation = "scream",
            RequiresLineOfSight = false,
            StopDuring = ShootBehavior.StopType.Attack,
            Uninterruptible = true
          },
          NickName = "SCREAMMMMMMMM AAAAAAAAAHHHHHHHHH"
        },
        new AttackBehaviorGroup.AttackGroupItem()
        {
          Probability = 1,
          Behavior = new ShootBehavior {
            ShootPoint = m_CachedGunAttachPoint,
            BulletScript = new CustomBulletScriptSelector(typeof(SkeletonBulletScript)),
            LeadAmount = 0f,
            MaxUsages = 2,
            AttackCooldown = 5f,
            TellAnimation = "tell2",
            FireAnimation = "puke",
            RequiresLineOfSight = false,
            StopDuring = ShootBehavior.StopType.Attack,
            Uninterruptible = true
          },
          NickName = "Skeleton Spookerino Wowie Zowie AHHHHH AHH, The Skeletons Are Eating Me, AHHHHHHH, man this name is stupid as hell. Stop reading my shit. Stop posting this on the Discord AHHH. At least hope you enjoy the mod tho."
        },
        new AttackBehaviorGroup.AttackGroupItem()
        {
          Probability = 1,
          Behavior = new ShootBehavior {
            ShootPoint = m_CachedGunAttachPoint,
            BulletScript = new CustomBulletScriptSelector(typeof(SpitUpScript)),
            LeadAmount = 0f,
            AttackCooldown = 4.5f,
            TellAnimation = "tell",
            FireAnimation = "suck",
            RequiresLineOfSight = false,
            StopDuring = ShootBehavior.StopType.Attack,
            Uninterruptible = true
          },
          NickName = "Cuck and Suck"
        },
      };
    Game.Enemies.Add("kp:room_mimic", companion.aiActor);
  }

  private static string[] spritePaths = new string[]
  {

    //idles (0-7)
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_idle_001",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_idle_002",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_idle_003",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_idle_004",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_idle_005",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_idle_006",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_idle_007",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_idle_008",
    //swirl (8-15)
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_swirl_001",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_swirl_002",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_swirl_003",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_swirl_004",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_swirl_005",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_swirl_006",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_swirl_007",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_swirl_008",
    //scream (16-19)
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_scream_001",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_scream_002",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_scream_003",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_scream_004",
    //tell (20-24)
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_tell_001",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_tell_002",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_tell_003",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_tell_004",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_tell_005",
    //suck (25-41)
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_suck_001",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_suck_002",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_suck_003",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_suck_004",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_suck_005",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_suck_006",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_suck_007",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_suck_008",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_suck_009",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_suck_010",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_suck_011",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_suck_012",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_suck_013",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_suck_014",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_suck_015",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_suck_016",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_suck_017",
    //tell2 (42-47)
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_tell2_001",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_tell2_002",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_tell2_003",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_tell2_004",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_tell2_005",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_tell2_006",
    //puke (48-54)
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_puke_001",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_puke_002",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_puke_003",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_puke_004",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_puke_005",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_puke_006",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_puke_007",
    //intro (55-75)
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_intro_001",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_intro_002",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_intro_003",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_intro_004",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_intro_005",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_intro_006",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_intro_007",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_intro_008",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_intro_009",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_intro_010",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_intro_011",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_intro_012",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_intro_013",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_intro_014",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_intro_015",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_intro_016",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_intro_017",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_intro_018",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_intro_019",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_intro_020",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_intro_021",
    //die (76-86)
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_die_003",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_die_003",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_die_003",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_die_004",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_die_005",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_die_006",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_die_007",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_die_008",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_die_009",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_die_010",
    "CwaffingTheGungy/Resources/room_mimic/room_mimic_die_011",

  };
}

public class SwirlScript : Script // This BulletScript is just a modified version of the script BulletManShroomed, which you can find with dnSpy.
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

      base.Fire(new Direction(360 - (i * 20), DirectionType.Absolute, -1f), new Speed(8f, SpeedType.Absolute), new WallBullet());
      yield return this.Wait(4);
      if (i % 10 == 9)
      {
        this.Fire(new Direction(0f, DirectionType.Aim, -1f), new Speed(8f, SpeedType.Absolute), new BurstBullet());
      }
    }

    yield break;
  }

}

public class SkeletonBulletScript : Script
{
  public override IEnumerator Top()
  {
    if (this.BulletBank && this.BulletBank.aiActor && this.BulletBank.aiActor.TargetRigidbody)
    {
      base.BulletBank.Bullets.Add(EnemyDatabase.GetOrLoadByGuid("5288e86d20184fa69c91ceb642d31474").bulletBank.GetBullet("skull"));
    }
    AkSoundEngine.PostEvent("Play_BOSS_doormimic_vomit_01", this.BulletBank.aiActor.gameObject);
    yield return this.Wait(22);
    base.Fire(new Direction(-40f, DirectionType.Aim, -1f), new Speed(11f, SpeedType.Absolute), new SpawnSkeletonBullet());
    base.Fire(new Direction(40f, DirectionType.Aim, -1f), new Speed(11f, SpeedType.Absolute), new SpawnSkeletonBullet());
    yield break;
  }
}

public class SpawnSkeletonBullet : Bullet
{
  // Token: 0x06000A99 RID: 2713 RVA: 0x000085A7 File Offset: 0x000067A7
  public SpawnSkeletonBullet() : base("skull", false, false, false)
  {
  }

  public override void OnBulletDestruction(Bullet.DestroyType destroyType, SpeculativeRigidbody hitRigidbody, bool preventSpawningProjectiles)
  {
    if (preventSpawningProjectiles)
    {
      return;
    }
    var list = new List<string> {
      //"shellet",
      "336190e29e8a4f75ab7486595b700d4a"
    };
    string guid = BraveUtility.RandomElement<string>(list);
    var Enemy = EnemyDatabase.GetOrLoadByGuid(guid);
    AIActor.Spawn(Enemy.aiActor, this.Projectile.sprite.WorldCenter, GameManager.Instance.PrimaryPlayer.CurrentRoom, true, AIActor.AwakenAnimationType.Default, true);
  }

}


public class AAAAAAAAAAAAAAScript : Script // This BulletScript is just a modified version of the script BulletManShroomed, which you can find with dnSpy.
{
  public override IEnumerator Top()
  {
    if (this.BulletBank && this.BulletBank.aiActor && this.BulletBank.aiActor.TargetRigidbody)
    {
      base.BulletBank.Bullets.Add(EnemyDatabase.GetOrLoadByGuid("796a7ed4ad804984859088fc91672c7f").bulletBank.GetBullet("default"));
    }
    AkSoundEngine.PostEvent("Play_ENM_beholster_intro_01", this.BulletBank.aiActor.gameObject);
    for (int k = 0; k < 70; k++)
    {
      this.Fire(new Direction((float)k * 360f / 64f, DirectionType.Absolute, -1f), new Speed (10f, SpeedType.Absolute), new WallBullet());
    }
    yield return Wait(70);
    AkSoundEngine.PostEvent("Play_ENM_beholster_intro_01", this.BulletBank.aiActor.gameObject);
    for (int k = 0; k < 70; k++)
    {
      this.Fire(new Direction((float)k * 360f / 64f, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new WallBullet());
    }
    yield return Wait(70);
    AkSoundEngine.PostEvent("Play_ENM_beholster_intro_01", this.BulletBank.aiActor.gameObject);
    for (int k = 0; k < 70; k++)
    {
      this.Fire(new Direction((float)k * 360f / 64f, DirectionType.Absolute, -1f), new Speed(10f, SpeedType.Absolute), new WallBullet());
    }
    yield break;
  }

}

public class SpitUpScript : Script // This BulletScript is just a modified version of the script BulletManShroomed, which you can find with dnSpy.
{
  public override IEnumerator Top()
  {
    if (this.BulletBank && this.BulletBank.aiActor && this.BulletBank.aiActor.TargetRigidbody)
    {
      base.BulletBank.Bullets.Add(EnemyDatabase.GetOrLoadByGuid("1bc2a07ef87741be90c37096910843ab").bulletBank.GetBullet("reversible"));
    }
    AkSoundEngine.PostEvent("Play_ENM_beholster_intro_01", this.BulletBank.aiActor.gameObject);
    for (int k = 0; k < 70; k++)
    {
      this.Fire(new Direction(UnityEngine.Random.Range(0f, 360f), DirectionType.Aim, -1f), new Speed(UnityEngine.Random.Range(8f, 13f), SpeedType.Absolute), new ReverseBullet());
    }
    yield break;
  }
}


public class BurstBullet : Bullet
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
      base.Fire(new Direction((float)(i * 45), DirectionType.Absolute, -1f), new Speed(7f, SpeedType.Absolute), new WallBullet());
    }
  }
}

public class ReverseBullet : Bullet
{
  // Token: 0x06000A91 RID: 2705 RVA: 0x00030B38 File Offset: 0x0002ED38
  public ReverseBullet() : base("reversible", false, false, false)
  {
  }

  // Token: 0x06000A92 RID: 2706 RVA: 0x00030B48 File Offset: 0x0002ED48
  public override IEnumerator Top()
  {
    float speed = this.Speed;
    yield return this.Wait(100);
    this.ChangeSpeed(new Speed(0f, SpeedType.Absolute), 20);
    yield return this.Wait(40);
    this.Direction += 180f;
    this.Projectile.spriteAnimator.Play();
    yield return this.Wait(60);
    this.ChangeSpeed(new Speed(speed, SpeedType.Absolute), 40);
    yield return this.Wait(130);
    this.Vanish(true);
    yield break;
  }
}

public class WallBullet : Bullet
{
  // Token: 0x06000A91 RID: 2705 RVA: 0x00030B38 File Offset: 0x0002ED38
  public WallBullet() : base("default", false, false, false)
  {
  }

}

public class EnemyBehavior : BraveBehaviour
{
  private void Start()
  {
    //base.aiActor.HasBeenEngaged = true;
    base.aiActor.healthHaver.OnPreDeath += (obj) =>
    {
      AkSoundEngine.PostEvent("Play_ENM_beholster_death_01", base.aiActor.gameObject);
      //Chest chest2 = GameManager.Instance.RewardManager.SpawnTotallyRandomChest(spawnspot);
      //chest2.IsLocked = false;

    };
    base.healthHaver.healthHaver.OnDeath += (obj) =>
    {
      Chest chest2 = GameManager.Instance.RewardManager.SpawnTotallyRandomChest(GameManager.Instance.PrimaryPlayer.CurrentRoom.GetRandomVisibleClearSpot(1, 1));
      chest2.IsLocked = false;

    }; ;
    this.aiActor.knockbackDoer.SetImmobile(true, "laugh");
  }


}

[RequireComponent(typeof(GenericIntroDoer))]
public class RoomMimicIntro : SpecificIntroDoer
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

}