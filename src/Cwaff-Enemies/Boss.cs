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

namespace CwaffingTheGungy
{
public class RoomMimic : AIActor
{
  public  const string guid          = "Room Mimic";
  private const string bossname      = guid;
  private const string subtitle      = "Face Off!";
  private const string spritePath    = "CwaffingTheGungy/Resources/room_mimic";
  private const string defaultSprite = "room_mimic_idle_001";
  private const string bossCardPath  = "CwaffingTheGungy/Resources/roomimic_bosscard.png";
  public static void Init()
  {
    // Create our build-a-boss
    BuildABoss bb = BuildABoss.LetsMakeABoss<EnemyBehavior>(
      bossname, guid, $"{spritePath}/{defaultSprite}", new IntVector2(8, 9), subtitle, bossCardPath);
    // Set our stats
    bb.SetStats(health: 1000f, weight: 200f, speed: 2f, collisionDamage: 1f,
      hitReactChance: 0.05f, collisionKnockbackStrength: 5f);
    // Set up our animations
    bb.InitSpritesFromResourcePath(spritePath);
      bb.AdjustAnimation("idle",   fps:   7f, loop: true);
      bb.AdjustAnimation("swirl",  fps:   9f, loop: true);
      bb.AdjustAnimation("scream", fps: 5.3f, loop: true);
      bb.AdjustAnimation("tell",   fps:   8f, loop: false);
      bb.AdjustAnimation("suck",   fps:   4f, loop: false);
      bb.AdjustAnimation("tell2",  fps:   6f, loop: false);
      bb.AdjustAnimation("puke",   fps:   7f, loop: false);
      bb.AdjustAnimation("intro",  fps:  11f, loop: false);
      bb.AdjustAnimation("die",    fps:   6f, loop: false);
    // Set our defaulf pixel colliders (TODO: should automatically be set from sprite width and height)
    bb.SetDefaultColliders(101,27);
    // Add custom animation to the generic intro doer, and add a specific intro doer as well
    bb.SetIntroAnimation("intro");
    bb.AddCustomIntro<RoomMimicIntro>();
    // Set up the boss's targeting and attacking scripts
    bb.TargetPlayer();
    bb.CreateAttack<SwirlScript>(fireAnim: "swirl", cooldown: 3.5f);
    bb.CreateAttack<AAAAAAAAAAAAAAScript>(fireAnim: "scream", cooldown: 3.5f);
    bb.CreateAttack<SkeletonBulletScript>(tellAnim: "tell2", fireAnim: "puke", cooldown: 5f, maxUsages: 2);
    bb.CreateAttack<SpitUpScript>(tellAnim: "tell", fireAnim: "suck", cooldown: 4.5f);
    // Add our boss to the enemy database and to the first floor's boss pool
    bb.AddBossToGameEnemies("kp:room_mimic");
    bb.AddBossToFloorPool(weight: 99f, floors: Floors.CASTLEGEON);
  }
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
    };
    base.healthHaver.healthHaver.OnDeath += (obj) =>
    {
      Chest chest2 = GameManager.Instance.RewardManager.SpawnTotallyRandomChest(GameManager.Instance.PrimaryPlayer.CurrentRoom.GetRandomVisibleClearSpot(1, 1));
      chest2.IsLocked = false;
    };
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
