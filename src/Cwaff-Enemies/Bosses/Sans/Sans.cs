using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;
using Brave.BulletScript;

namespace CwaffingTheGungy
{
public partial class SansBoss : AIActor
{
  public  const string BOSS_GUID         = "Sans Boss";
  private const string BOSS_NAME         = "Sans Gundertale";
  private const string SUBTITLE          = "Introducing...";
  private const string SPRITE_PATH       = "CwaffingTheGungy/Resources/sans";
  private const string DEFAULT_SPRITE    = "sans_idle_1";
  private const string BOSS_CARD_PATH    = "CwaffingTheGungy/Resources/sans_bosscard.png";
  private const string MUSIC_NAME        = "electromegalo";
  private const int    MUSIC_LOOP_END    = 152512;
  private const int    MUSIC_LOOP_LENGTH = 137141;
  private const int    NUM_HITS          = 1;//60;

  internal static GameObject napalmReticle      = null;
  internal static AIBulletBank.Entry boneBullet = null;

  public static void Init()
  {
    // Create our build-a-boss
    BuildABoss bb = BuildABoss.LetsMakeABoss<BossBehavior>(
      BOSS_NAME, BOSS_GUID, $"{SPRITE_PATH}/{DEFAULT_SPRITE}", new IntVector2(8, 9), SUBTITLE, BOSS_CARD_PATH);
    // Set our stats
    bb.SetStats(health: NUM_HITS, weight: 200f, speed: 0.4f, collisionDamage: 0f,
      hitReactChance: 0.05f, collisionKnockbackStrength: 0f, healthIsNumberOfHits: true, invulnerabilityPeriod: 1.0f);
    // Set up our animations
    bb.InitSpritesFromResourcePath(SPRITE_PATH);
      bb.AdjustAnimation("idle",         fps:   12f, loop: true);
      bb.AdjustAnimation("idle_cloak",   fps:   12f, loop: true);
      bb.AdjustAnimation("decloak",      fps:    6f, loop: false);
      bb.AdjustAnimation("teleport_in",  fps:   60f, loop: false);
      bb.AdjustAnimation("teleport_out", fps:   60f, loop: false);
      bb.SetIntroAnimations(introAnim: "decloak", preIntroAnim: "idle_cloak");
    // Set our default pixel colliders
    bb.SetDefaultColliders(15,30,24,2);
    // Add custom animation to the generic intro doer
    bb.AddCustomIntro<BossIntro>();
    // Set up the boss's targeting scripts
    bb.TargetPlayer();
    // Add some named vfx pools to our bank of VFX
    bb.AddNamedVFX(VFX.vfxpool["Tornado"], "mytornado");
    // Add some attacks
    bb.CreateTeleportAttack<CustomTeleportBehavior>(
      goneTime: 0.25f, outAnim: "teleport_out", inAnim: "teleport_in", cooldown: 0.26f, attackCooldown: 0.15f, probability: 3f);
    bb.CreateBulletAttack<CeilingBulletsScript>    (fireAnim: "laugh",       cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<OrbitBulletScript>       (fireAnim: "throw_up",    cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<HesitantBulletWallScript>(fireAnim: "throw_down",  cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<SquareBulletScript>      (fireAnim: "throw_left",  cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<ChainBulletScript>       (fireAnim: "throw_right", cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<WallSlamScript>          (fireAnim: "laugh",       cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<SineWaveScript>          (fireAnim: "throw_right", cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<OrangeAndBlueScript>     (fireAnim: "throw_right", cooldown: 0.25f, attackCooldown: 0.15f);
    bb.CreateBulletAttack<WiggleWaveScript>        (fireAnim: "throw_right", cooldown: 0.25f, attackCooldown: 0.15f);
    // bb.CreateSimultaneousAttack(new(){
    //   bb.CreateBulletAttack<RichochetScript> (add: false, tellAnim: "swirl", fireAnim: "suck", attackCooldown: 3.5f, fireVfx: "mytornado"),
    //   bb.CreateBulletAttack<RichochetScript2>(add: false, tellAnim: "swirl", fireAnim: "suck", attackCooldown: 3.5f, fireVfx: "mytornado"),
    //   bb.CreateBulletAttack<CeilingBulletsScript>(add: false, fireAnim: "swirl", attackCooldown: 3.5f, fireVfx: "mytornado"),
    //   });
    // Add our boss to the enemy database and to the first floor's boss pool
    bb.AddBossToGameEnemies("cg:sansboss");
    bb.AddBossToFloorPool(Floors.CASTLEGEON, weight: 9999f);
    InitPrefabs(); // Do miscellaneous prefab loading
  }

  private static void InitPrefabs()
  {
    // Targeting reticle
    napalmReticle = ResourceManager.LoadAssetBundle("shared_auto_002").LoadAsset<GameObject>("NapalmStrikeReticle").ClonePrefab();
      napalmReticle.GetComponent<tk2dSlicedSprite>().SetSprite(VFX.SpriteCollection, VFX.sprites["reticle-white"]);
      UnityEngine.Object.Destroy(napalmReticle.GetComponent<ReticleRiserEffect>());
    // Bone bullet
    boneBullet = new AIBulletBank.Entry(EnemyDatabase.GetOrLoadByGuid("1bc2a07ef87741be90c37096910843ab").bulletBank.GetBullet("reversible")) {
      Name               = "getboned",
      BulletObject       = Lazy.GunDefaultProjectile(59).gameObject.ClonePrefab(),
      PlayAudio          = false,
      MuzzleFlashEffects = VFX.CreatePoolFromVFXGameObject(Lazy.GunDefaultProjectile(29).hitEffects.overrideMidairDeathVFX),
    };
  }

  private class DoomZoneGrowth : MonoBehaviour
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
      this.gameObject.GetOrAddComponent<ReticleRiserEffect>().NumRisers = 3; // restore reticle riser settings
    }
  }

  // Creates a napalm-strike-esque danger zone
  private static GameObject DoomZone(Vector2 start, Vector2 target, float width, float lifetime = -1f, int growthTime = 1, string sprite = null)
  {
    Vector2 delta                = target - start;
    GameObject reticle           = UnityEngine.Object.Instantiate(napalmReticle);
    tk2dSlicedSprite reticleQuad = reticle.GetComponent<tk2dSlicedSprite>();
      if (sprite != null)
        reticleQuad.SetSprite(VFX.SpriteCollection, VFX.sprites[sprite]);
      reticleQuad.dimensions = C.PIXELS_PER_TILE * (new Vector2(delta.magnitude / growthTime, width));
      reticle.AddComponent<DoomZoneGrowth>().Lengthen(delta.magnitude,growthTime);
      reticleQuad.transform.localRotation = Quaternion.Euler(0f, 0f, BraveMathCollege.Atan2Degrees(target-start));
      reticleQuad.transform.position = start + (Quaternion.Euler(0f, 0f, -90f) * delta.normalized * (width / 2f)).XY();
    if (lifetime > 0)
      reticle.ExpireIn(lifetime);
    return reticle;
  }

  private static void SpawnDust(Vector2 where, int howMany = 1)
  {
    DustUpVFX dusts = GameManager.Instance.Dungeon.dungeonDustups;
    for (int i = 0; i < howMany; ++i)
      SpawnManager.SpawnVFX(
          dusts.rollLandDustup,
          where + Lazy.RandomVector(UnityEngine.Random.Range(0.3f,1.25f)),
          Quaternion.Euler(0f, 0f, Lazy.RandomAngle()));
  }

  private class BossBehavior : BraveBehaviour
  {
    private bool                    hasFinishedIntro = false;
    private HeatIndicatorController aura             = null;

    private void Start()
    {
      base.aiActor.healthHaver.OnPreDeath += (_) => {
        FlipSpriteIfNecessary(overrideFlip: false);
        AkSoundEngine.PostEvent("Play_ENM_beholster_death_01", base.aiActor.gameObject);
      };
      base.healthHaver.healthHaver.OnDeath += (_) => {
        FlipSpriteIfNecessary(overrideFlip: false);
        GameManager.Instance.RewardManager.SpawnTotallyRandomChest(GameManager.Instance.PrimaryPlayer.CurrentRoom.GetRandomVisibleClearSpot(1, 1)).IsLocked = false;
      };
      this.aiActor.bulletBank.Bullets.Add(boneBullet);
    }

    private void Update()
    {
      if (!hasFinishedIntro || BraveTime.DeltaTime == 0)
        return; // don't do anything if we're paused or pre-intro
      base.aiActor.PathfindToPosition(GameManager.Instance.PrimaryPlayer.specRigidbody.UnitCenter); // drift around
    }

    private void LateUpdate()
    {
      const float JIGGLE = 4f;
      const float SPEED  = 4f;
      if (!hasFinishedIntro || BraveTime.DeltaTime == 0)
        return; // don't do anything if we're paused or pre-intro

      FlipSpriteIfNecessary();
      base.sprite.transform.localPosition += Vector3.zero.WithY(Mathf.CeilToInt(JIGGLE*Mathf.Sin(SPEED*BraveTime.ScaledTimeSinceStartup))/C.PIXELS_PER_TILE);
      if (Lazy.CoinFlip())
        SpawnDust(base.specRigidbody.UnitCenter); // spawn dust particles
    }

    private void FlipSpriteIfNecessary(bool? overrideFlip = null)
    {
      base.sprite.FlipX  = overrideFlip ?? (GameManager.Instance.BestActivePlayer.sprite.WorldBottomCenter.x < base.specRigidbody.UnitBottomCenter.x);
      Vector3 spriteSize = base.sprite.GetUntrimmedBounds().size;
      Vector3 offset     = Vector3.zero.WithX(spriteSize.x / (base.sprite.FlipX ? 2f : -2f));
      base.sprite.transform.localPosition = (Vector3)base.specRigidbody.UnitBottomCenter/*.RoundToInt()*/ + offset;
      if (aura != null)
        aura.transform.localPosition = new Vector3(0,spriteSize.y / 2,0) - offset;
    }

    public void FinishedIntro()
    {
      hasFinishedIntro = true;
      aura = ((GameObject)UnityEngine.Object.Instantiate(ResourceCache.Acquire("Global VFX/HeatIndicator"), base.aiActor.CenterPosition.ToVector3ZisY(), Quaternion.identity, base.aiActor.sprite.transform)).GetComponent<HeatIndicatorController>();
        aura.CurrentColor  = Color.white;
        aura.IsFire        = true;
        aura.CurrentRadius = 2f; // activate aura (from basegame AuraOnReloadModifier)
    }
  }

  private class BossIntro : SpecificIntroDoer
  {
    public override void PlayerWalkedIn(PlayerController player, List<tk2dSpriteAnimator> animators)
    {
      // Play boss music
      this.PlayBossMusic(MUSIC_NAME, MUSIC_LOOP_END, MUSIC_LOOP_LENGTH);
      // Set up room specific attacks
      Rect roomTeleportBounds = base.aiActor.GetAbsoluteParentRoom().GetBoundingRect().Inset(8f);
      foreach (AttackBehaviorGroup.AttackGroupItem attack in base.aiActor.GetComponent<BehaviorSpeculator>().AttackBehaviorGroup.AttackBehaviors)
      {
        if (attack.Behavior is not TeleportBehavior)
          continue;
        TeleportBehavior tb = attack.Behavior as TeleportBehavior;
          tb.ManuallyDefineRoom = true;
          tb.roomMin            = roomTeleportBounds.min;
          tb.roomMax            = roomTeleportBounds.max;
      }
    }

    public override void EndIntro()
      { base.aiActor.GetComponent<BossBehavior>().FinishedIntro(); }
  }
}

}
