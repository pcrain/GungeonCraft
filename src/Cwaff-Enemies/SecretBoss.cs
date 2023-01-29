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
  public static void Init()
  {
    // Create our build-a-boss
    BuildABoss bb = BuildABoss.LetsMakeABoss<BossBehavior>(
      bossname, guid, $"{spritePath}/{defaultSprite}", new IntVector2(8, 9), subtitle, bossCardPath);
    // Set our stats
    bb.SetStats(health: 1000f, weight: 200f, speed: 2f, collisionDamage: 1f,
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
    bb.CreateTeleportAttack<TeleportBehavior>(outAnim: "scream", inAnim: "swirl", attackCooldown: 3.5f);
    // bb.CreateBulletAttack<SwirlScript>(fireAnim: "swirl", attackCooldown: 3.5f);
    // bb.CreateBulletAttack<AAAAAAAAAAAAAAScript>(fireAnim: "scream", attackCooldown: 3.5f);
    // bb.CreateBulletAttack<SkeletonBulletScript>(tellAnim: "tell2", fireAnim: "puke", attackCooldown: 5f, maxUsages: 2);
    // bb.CreateBulletAttack<SpitUpScript>(tellAnim: "tell", fireAnim: "suck", attackCooldown: 4.5f);
    // Add our boss to the enemy database and to the first floor's boss pool
    bb.AddBossToGameEnemies("cg:secretboss");
    bb.AddBossToFloorPool(weight: 9999f, floors: Floors.CASTLEGEON);

    // new Hook(
    //     typeof(TeleportBehavior).GetMethod("Update", BindingFlags.Public | BindingFlags.Instance),
    //     typeof(SecretBoss).GetMethod("UpdateHook", BindingFlags.Public | BindingFlags.Static));

    // new Hook(
    //     typeof(TeleportBehavior).GetMethod("ContinuousUpdate", BindingFlags.Public | BindingFlags.Instance),
    //     typeof(SecretBoss).GetMethod("ContinuousUpdateHook", BindingFlags.Public | BindingFlags.Static));
  }

  // public static BehaviorResult UpdateHook(Func<TeleportBehavior, BehaviorResult> orig, TeleportBehavior self)
  // {
  //   ETGModConsole.Log($"tryin normal!");
  //   BehaviorResult result = BehaviorResult.Continue;
  //   try
  //   {
  //     result = orig(self);
  //   }
  //   catch (Exception e)
  //   {
  //     ETGModConsole.Log(e);
  //   }
  //   ETGModConsole.Log($"did {result}");
  //   return result;
  // }

  // public static ContinuousBehaviorResult ContinuousUpdateHook(Func<TeleportBehavior, ContinuousBehaviorResult> orig, TeleportBehavior self)
  // {
  //   ETGModConsole.Log($"trying continuous!");
  //   ContinuousBehaviorResult result = ContinuousBehaviorResult.Continue;
  //   try
  //   {
  //     result = orig(self);
  //   }
  //   catch (Exception e)
  //   {
  //     ETGModConsole.Log(e);
  //   }
  //   ETGModConsole.Log($"did {result}");
  //   return result;
  // }

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

}

}
