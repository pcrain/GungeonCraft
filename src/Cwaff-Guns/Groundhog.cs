

namespace CwaffingTheGungy;

public class Groundhog : CwaffGun
{
    public static string ItemName         = "Groundhog";
    public static string ShortDescription = "Deep Impact";
    public static string LongDescription  = "Penetrates the ground and detonates a warhead when charged, creating a massive shockwave that launches enemies within a large radius skyward. Immobilizes the player while charging.";
    public static string Lore             = "A highly experimental weapon, its indiscriminate shattering of everything in a 100 foot radius makes it impractical for anything other than single-person infiltrations. The Groundhog is notable for having the most expensive ammunition of any handheld weapon in commission, with each projectile costing as much as a small house. This is partially related to the fact that the projectile's quality assurance process involves using it to destroy a small house.";

    private const float _BASE_FPS = 20f;

    internal static GameObject _EarthClod = null;

    private bool _wasCharging = false;
    private bool _setupAnimator = false;

    public static void Init()
    {
        Lazy.SetupGun<Groundhog>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.A, gunClass: GunClass.CHARGE, reloadTime: 0.0f, ammo: 50, shootFps: 14, reloadFps: 4,
            chargeFps: (int)_BASE_FPS, attacksThroughWalls: true, percentSpeedWhileCharging: 0.0f, preventRollingWhenCharging: true,
            loopChargeAt: 15)
          .SetChargeAudio("groundhog_burrow_sound", 11)
          .InitProjectile(GunData.New(sprite: null, clipSize: 1, cooldown: 0.75f, shootStyle: ShootStyle.Charged, chargeTime: 2f, hideAmmo: true,
            damage: 50.0f, speed: 25f, range: 100f, force: 30f, hitEnemySound: "paintball_impact_enemy_sound", hitWallSound: "paintball_impact_wall_sound"));

        _EarthClod = VFX.Create("groundhog_rock_vfx");
    }

    //NOTE: logic for forcing the gun to only face directly left or right when charging
    public override void Update()
    {
        base.Update();
        if (!this._setupAnimator)
        {
          base.gameObject.GetComponent<tk2dSpriteAnimator>().AnimationEventTriggered += this.AnimationEventTriggered;
          this._setupAnimator = true;
        }
        this.gun.OverrideAngleSnap = (this.gun.IsCharging || this.gun.IsReloading) ? 180f : null;
        if (this.PlayerOwner is not PlayerController pc)
          return;
        if (!this.gun.IsCharging)
          pc.forceAimPoint = null;
        else if (!this._wasCharging)
          pc.forceAimPoint = pc.CenterPosition + new Vector2(pc.sprite.FlipX ? -4f : 4f, 0f); // lock aim point while charging
        this._wasCharging = this.gun.IsCharging;
    }

    private void AnimationEventTriggered(tk2dSpriteAnimator animator, tk2dSpriteAnimationClip clip, int frame)
    {
        if (!this.Mastered || frame != 11 || !this.PlayerOwner || clip.name != "groundhog_charge")
            return;
        Vector2 blankPos = this.gun.sprite.FlipY ? this.gun.sprite.WorldTopCenter : this.gun.sprite.WorldBottomCenter;
        Lazy.DoMicroBlankAt(blankPos, this.PlayerOwner);
        GameManager.Instance.MainCameraController.DoScreenShake(new ScreenShakeSettings(0.2f, 6f, 0.2f, 0f), blankPos);
        for (int i = 1; i <= 3; ++i)
          CwaffVFX.SpawnBurst(prefab: _EarthClod, numToSpawn: 10 * i, basePosition: blankPos, positionVariance: i,
            velType: CwaffVFX.Vel.AwayRadial, velocityVariance: 5f, lifetime: 0.25f, fadeOutTime: 0.25f,
            startScale: 1.0f,  endScale: 0.1f, uniform: true, randomFrame: true);
    }

    private void UpdateChargeAnimation(PlayerController controller)
    {
        if (controller == this.PlayerOwner)
          this.gun.SetAnimationFPS(this.gun.chargeAnimation, Mathf.RoundToInt(_BASE_FPS * this.PlayerOwner.ChargeMult()));
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        CwaffEvents.OnStatsRecalculated += this.UpdateChargeAnimation;
    }
    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        player.forceAimPoint = null;
        CwaffEvents.OnStatsRecalculated -= this.UpdateChargeAnimation;
    }

    public override void OnDestroy()
    {
        CwaffEvents.OnStatsRecalculated -= this.UpdateChargeAnimation;
        base.OnDestroy();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        if (this.PlayerOwner)
          this.PlayerOwner.forceAimPoint = null;
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (!this.PlayerOwner)
        {
          projectile.DieInAir();
          return;
        }
        float damage = projectile.baseData.damage;
        float force = projectile.baseData.force;
        float sqrRange = projectile.baseData.range;
        this.PlayerOwner.StartCoroutine(GroundhogShockwaveCR(damage, force, sqrRange));
        projectile.DieInAir();
    }

    public IEnumerator GroundhogShockwaveCR(float damage, float force, float sqrRange)
    {
      float range = Mathf.Sqrt(sqrRange);
      int intRange = 2 * Mathf.FloorToInt(range);
      Vector2 shockwaveCenter = this.PlayerOwner.CenterPosition;
      for (int i = 1; i < intRange; ++i)
      {
        this.PlayerOwner.gameObject.Play("earthquake_sound");
        LaunchAllEnemiesInRoom(this.PlayerOwner, damage, force, shockwaveCenter, i - 1.5f, i + 0.5f);
        Exploder.DoRadialMinorBreakableBreak(shockwaveCenter, i);
        Exploder.DoRadialPush(shockwaveCenter, force, i);
        GameManager.Instance.MainCameraController.DoScreenShake(new ScreenShakeSettings(0.5f, 6f, 0.5f, 0f), shockwaveCenter);
        CwaffVFX.SpawnBurst(
          prefab           : _EarthClod,
          numToSpawn       : i * 10,
          basePosition     : shockwaveCenter,
          positionVariance : i,
          velType          : CwaffVFX.Vel.AwayRadial,
          velocityVariance : 5f,
          lifetime         : 0.25f,
          fadeOutTime      : 0.25f,
          startScale       : 1.0f,
          endScale         : 0.1f,
          uniform          : true,
          randomFrame      : true
          );
        yield return new WaitForSeconds(0.025f);
      }
    }

    private static void LaunchAllEnemiesInRoom(PlayerController player, float damage, float force, Vector2 shockwaveCenter, float minRange, float maxRange)
    {
      float minRangeSqr = minRange * minRange;
      float maxRangeSqr = maxRange * maxRange;
      foreach (AIActor enemy in player.CurrentRoom.SafeGetEnemiesInRoom())
      {
        if (enemy.healthHaver is not HealthHaver hh || !hh.IsVulnerable || hh.IsDead || hh.PreventAllDamage || !enemy.specRigidbody/* || enemy.IsFlying*/)
          continue;
        float sqrDist = (enemy.CenterPosition - shockwaveCenter).sqrMagnitude;
        if (sqrDist < minRangeSqr || sqrDist > maxRangeSqr)
          continue;
        if (enemy.behaviorSpeculator is not BehaviorSpeculator bs || bs.ImmuneToStun || hh.IsBoss || hh.IsSubboss)
          enemy.healthHaver.ApplyDamage(damage, Vector2.zero, ItemName, CoreDamageTypes.None, DamageCategory.Collision);
        else
          enemy.StartCoroutine(LaunchTime(enemy, shockwaveCenter, damage, force));
      }
    }

    private static IEnumerator LaunchTime(AIActor enemy, Vector2 center, float damage, float force)
    {
      // prevent enemy from moving or taking damage normally
      int originalLayer = enemy.gameObject.layer;
      enemy.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Unoccluded"));
      if (enemy.behaviorSpeculator)
        enemy.behaviorSpeculator.Stun(3f, true);
      if (enemy.healthHaver)
        enemy.healthHaver.vulnerable = false;
      if (enemy.knockbackDoer)
        enemy.knockbackDoer.SetImmobile(true, ItemName);

      SpeculativeRigidbody body = enemy.specRigidbody;
      body.enabled = false;
      body.CollideWithOthers = false;
      body.CollideWithTileMap = false;

      // launch enemy into the air
      const float gravity = 60f;
      float launchSpeed = force;
      Transform t = enemy.sprite.transform;
      float startY = t.position.y;
      float yOffset = 0f;
      while (true)
      {
        launchSpeed -= BraveTime.DeltaTime * gravity;
        yOffset += launchSpeed * BraveTime.DeltaTime;
        if (yOffset < 0)
          break;
        t.position = t.position.WithY(startY + yOffset); //TODO: doesn't work quite right for ball and chain gun nuts
        yield return null;
      }

      // restore once we touch down
      enemy.gameObject.SetLayerRecursively(originalLayer);
      if (enemy.healthHaver)
      {
        enemy.healthHaver.vulnerable = true;
        enemy.healthHaver.ApplyDamage(damage, Vector2.zero, ItemName, CoreDamageTypes.None, DamageCategory.Collision);
      }
      if (enemy.knockbackDoer)
        enemy.knockbackDoer.SetImmobile(false, ItemName);
      if (body)
      {
        body.CollideWithOthers = true;
        body.CollideWithTileMap = true;
        body.enabled = true;
      }
    }
}
