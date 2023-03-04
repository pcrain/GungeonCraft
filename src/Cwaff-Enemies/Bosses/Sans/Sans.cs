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
  private const int    NUM_HITS          = 60;

  internal static GameObject napalmReticle      = null;
  internal static AIBulletBank.Entry boneBullet = null;
  internal static VFXPool bonevfx               = null;
  internal static uint megalo_event_id          = 0;

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
    // Set up the boss's targeting and attacking scripts
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
    napalmReticle = Instantiate(ResourceManager.LoadAssetBundle("shared_auto_002").LoadAsset<GameObject>("NapalmStrikeReticle"));
      napalmReticle.GetComponent<tk2dSlicedSprite>().SetSprite(VFX.SpriteCollection, VFX.sprites["reticle-white"]);
      napalmReticle.RegisterPrefab();

    // Bone bullet Spawn VFX
    bonevfx = VFX.CreatePoolFromVFXGameObject(Lazy.GunDefaultProjectile(29).hitEffects.overrideMidairDeathVFX);

    // Bone bullet
    boneBullet = new AIBulletBank.Entry(EnemyDatabase.GetOrLoadByGuid("1bc2a07ef87741be90c37096910843ab").bulletBank.GetBullet("reversible")) {
      Name               = "getboned",
      BulletObject       = Instantiate(Lazy.GunDefaultProjectile(59).gameObject).RegisterPrefab(),
      PlayAudio          = false,
      MuzzleFlashEffects = bonevfx,
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
  private static GameObject DoomZone(Vector2 start, Vector2 target, float width, float lifetime = -1f, int growthTime = 0, string sprite = null)
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

  private static void SpawnDust(Vector2 where, int howMany = 1)
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

  private class BossBehavior : BraveBehaviour
  {
    private bool                    hasFinishedIntro = false;
    private float                   yoffset          = 0;
    private bool                    auraActive       = false;
    private HeatIndicatorController aura;

    // from basegame AuroOnReloadModifier
    private void ActivateAura()
    {
      if (auraActive)
        return;
      auraActive = true;
      aura = ((GameObject)UnityEngine.Object.Instantiate(ResourceCache.Acquire("Global VFX/HeatIndicator"), base.aiActor.CenterPosition.ToVector3ZisY(), Quaternion.identity, base.aiActor.sprite.transform)).GetComponent<HeatIndicatorController>();
        aura.CurrentColor  = new Color(1f, 1f, 1f);
        aura.IsFire        = true;
        aura.CurrentRadius = 2f;
    }

    private void Start()
    {
      base.aiActor.healthHaver.OnPreDeath += (obj) =>
      {
        FlipSpriteIfNecessary(forceUnflip: true);
        megalo_event_id = 0;
        AkSoundEngine.PostEvent(MUSIC_NAME+"_stop", GameManager.Instance.DungeonMusicController.gameObject);
        AkSoundEngine.PostEvent("Play_ENM_beholster_death_01", base.aiActor.gameObject);
      };
      base.healthHaver.healthHaver.OnDeath += (obj) =>
      {
        FlipSpriteIfNecessary(forceUnflip: true);
        Chest chest2 = GameManager.Instance.RewardManager.SpawnTotallyRandomChest(GameManager.Instance.PrimaryPlayer.CurrentRoom.GetRandomVisibleClearSpot(1, 1));
        chest2.IsLocked = false;
      };
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
      int pos;
      AKRESULT status = AkSoundEngine.GetSourcePlayPosition(megalo_event_id, out pos);
      if (status == AKRESULT.AK_Success && pos >= MUSIC_LOOP_END)
        AkSoundEngine.SeekOnEvent(MUSIC_NAME, GameManager.Instance.DungeonMusicController.gameObject,pos - MUSIC_LOOP_LENGTH);

      // don't do anything if we're paused
      if (BraveTime.DeltaTime == 0)
        return;

      DriftAround();
    }

    private void LateUpdate()
    {
      const float JIGGLE = 4.0f;
      const float SPEED  = 4.0f;

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
      base.aiActor.PathfindToPosition(GameManager.Instance.PrimaryPlayer.specRigidbody.UnitCenter);
    }

    private void FlipSpriteIfNecessary(bool forceUnflip = false)
    {
      bool lastFlip      = base.sprite.FlipX;
      bool shouldFlip    = (GameManager.Instance.BestActivePlayer.sprite.WorldBottomCenter.x < base.specRigidbody.UnitBottomCenter.x);
      base.sprite.FlipX  = shouldFlip && (!forceUnflip);
      Vector3 spriteSize = base.sprite.GetUntrimmedBounds().size;
      Vector3 offset     = new Vector3(spriteSize.x / 2, 0f, 0f);
      if (!base.sprite.FlipX)
        offset *= -1;

      Vector3 finalPosition = (Vector3)base.specRigidbody.UnitBottomCenter/*.RoundToInt()*/ + offset;
      base.sprite.transform.localPosition = finalPosition;
      if (auraActive)
        aura.transform.localPosition = new Vector3(0,spriteSize.y / 2,0) - offset;
    }
  }

  [RequireComponent(typeof(GenericIntroDoer))]
  private class BossIntro : SpecificIntroDoer
  {
    public override void PlayerWalkedIn(PlayerController player, List<tk2dSpriteAnimator> animators)
    {
      SetupRoomSpecificAttacks();
      GameManager.Instance.StartCoroutine(PlayMusic());
    }

    private void SetupRoomSpecificAttacks()
    {
      Rect roomFullBounds = base.aiActor.GetAbsoluteParentRoom().GetBoundingRect();
      Rect roomTeleportBounds = roomFullBounds.Inset(8f);
      foreach (AttackBehaviorGroup.AttackGroupItem attack in base.aiActor.gameObject.GetComponent<BehaviorSpeculator>().AttackBehaviorGroup.AttackBehaviors)
      {
        if (attack.Behavior is TeleportBehavior)
        {
          TeleportBehavior tb = attack.Behavior as TeleportBehavior;
          tb.ManuallyDefineRoom = true;
          tb.roomMin = roomTeleportBounds.min;
          tb.roomMax = roomTeleportBounds.max;
        }
      }
    }

    private IEnumerator PlayMusic()
    {
      megalo_event_id = AkSoundEngine.PostEvent(MUSIC_NAME, GameManager.Instance.DungeonMusicController.gameObject, in_uFlags: (uint)AkCallbackType.AK_EnableGetSourcePlayPosition);
      yield return StartCoroutine(BH.WaitForSecondsInvariant(1.8f));
      yield break;
    }

    public override void EndIntro()
    {
      base.aiActor.gameObject.GetComponent<BossBehavior>().FinishedIntro();
    }
  }
}

}
