namespace CwaffingTheGungy;

public class Groundhog : CwaffGun
{
    public static string ItemName         = "Groundhog";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const float _BASE_FPS = 20f;

    internal static GameObject _EarthClod = null;

    private bool _wasCharging = false;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Groundhog>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.A, gunClass: GunClass.CHARGE, reloadTime: 0.0f, ammo: 50, shootFps: 14, reloadFps: 4, chargeFps: (int)_BASE_FPS);
            gun.SetChargeAudio("groundhog_burrow_sound", 11);
            gun.LoopAnimation(gun.chargeAnimation, 15);
            gun.CanAttackThroughObjects = true;

        Groundhog groundhog = gun.gameObject.GetComponent<Groundhog>();
            groundhog.preventRollingWhenCharging = true;
            groundhog.preventMovingWhenCharging = true;

        gun.InitProjectile(GunData.New(sprite: null, clipSize: 12, cooldown: 0.75f, shootStyle: ShootStyle.Charged, chargeTime: 2f,
            damage: 50.0f, speed: 25f, range: 100f, force: 30f, hitEnemySound: "paintball_impact_enemy_sound", hitWallSound: "paintball_impact_wall_sound"));

        _EarthClod = VFX.Create("groundhog_rock_vfx", fps: 1, loops: true, anchor: Anchor.MiddleCenter, scale: 1.0f);
    }

    //NOTE: logic for forcing the gun to only face directly left or right when charging
    public override void Update()
    {
        base.Update();
        this.gun.OverrideAngleSnap = (this.gun.IsCharging || this.gun.IsReloading) ? 180f : null;
        if (!this.PlayerOwner)
          return;
        if (!this.gun.IsCharging)
        {
          this._wasCharging = false;
          this.PlayerOwner.forceAimPoint = null;
          return;
        }
        if (!this._wasCharging)
        {
          this._wasCharging = true;
          //TODO: this is delayed and takes effect next time the gun is charged; refactor later to use OnStatsChanged listener
          this.gun.SetAnimationFPS(this.gun.chargeAnimation, Mathf.RoundToInt(_BASE_FPS * this.PlayerOwner.ChargeMult()));
          this.PlayerOwner.forceAimPoint = this.PlayerOwner.CenterPosition
            + new Vector2(this.PlayerOwner.sprite.FlipX ? -4f : 4f, 0f); // lock aim point while charging
        }
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        player.forceAimPoint = null;
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
        LaunchAllEnemiesInRoom(this.PlayerOwner, damage, force, shockwaveCenter, i - 1.5f, i + 0.5f);
        Exploder.DoRadialMinorBreakableBreak(shockwaveCenter, i);
        Exploder.DoRadialPush(shockwaveCenter, force, i);
        GameManager.Instance.MainCameraController.DoScreenShake(new ScreenShakeSettings(0.5f,6f,0.5f,0f), shockwaveCenter);
        CwaffVFX.SpawnBurst(
          prefab           : _EarthClod,
          numToSpawn       : i * 10,
          basePosition     : shockwaveCenter,
          positionVariance : i,
          velType          : CwaffVFX.Vel.AwayRadial,
          velocityVariance : 5f,
          rotType          : CwaffVFX.Rot.None,
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
      player.gameObject.Play("earthquake_sound");
      RoomHandler room = player.CurrentRoom;
      foreach (AIActor enemy in room.SafeGetEnemiesInRoom())
      {
        if (enemy.healthHaver is not HealthHaver hh || !hh.IsVulnerable || !hh.IsAlive || !enemy.specRigidbody/* || enemy.IsFlying*/)
          continue;
        float dist = (enemy.CenterPosition - shockwaveCenter).magnitude;
        if (dist < minRange || dist > maxRange)
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