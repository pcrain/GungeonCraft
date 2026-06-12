namespace CwaffingTheGungy;

public class Akelus : CwaffGun
{
    public static string ItemName         = "Akelus";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject _ShockwaveVFX = null;

    private bool _isDoingAttack = false;
    private int _leapKnockbackId = -1;

    public static void Init()
    {
        Lazy.SetupGun<Akelus>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.A, gunClass: GunClass.SILLY, reloadTime: 0.0f, infiniteAmmo: true, ammo: 20,
            shootFps: 30, reloadFps: 4, dynamicBarrelOffsets: true,
            muzzleFrom: Items.Mailbox, fireAudio: "chain_launch_sound", suppressReloadLabel: true, curse: 1f)
          .AssignGun(out Gun gun)
          .LoopAnimation(gun.shootAnimation, 1)
          .InitProjectile(GunData.New(clipSize: -1, shootStyle: ShootStyle.Charged, chargeTime: float.MaxValue, hideAmmo: true)); // absurdly high charge value so we never actually shoot

        _ShockwaveVFX = VFX.Create("akelus_impact_vfx", fps: 12, loops: false, useBetterEmission: true,
          emissivePower: 10.0f, emissiveColorPower: 10.0f, emissiveSensitivity: 1.0f, emissiveColour: new Color(0.5f, 0.5f, 1.0f));
    }

    public override void OnPlayerPickup(PlayerController player)
    {
      base.OnPlayerPickup(player);
      if (this.Mastered)
        player.SetImmuneToExplosions(true, ItemName);
    }

    public override void OnSwitchedToThisGun()
    {
      base.OnSwitchedToThisGun();
      if (this.Mastered && this.PlayerOwner)
        this.PlayerOwner.SetImmuneToExplosions(true, ItemName);
    }

    public override void OnMasteryStatusChanged()
    {
      base.OnMasteryStatusChanged();
      if (this.Mastered && this.PlayerOwner)
        this.PlayerOwner.SetImmuneToExplosions(true, ItemName);
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
      player.SetImmuneToExplosions(false, ItemName);
      base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
      if (this.PlayerOwner)
        this.PlayerOwner.SetImmuneToExplosions(false, ItemName);
      base.OnDestroy();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
      if (this.PlayerOwner)
        this.PlayerOwner.SetImmuneToExplosions(false, ItemName);
      base.OnSwitchedAwayFromThisGun();
    }

    public override void OnTriedToInitiateAttack(PlayerController player)
    {
      base.OnTriedToInitiateAttack(player);
      player.SuppressThisClick = true; // does not have a normal fire mode, so always suppress the attack
      if (player.IsDodgeRolling || player.CurrentInputState != PlayerInputState.AllInput)
        return; // inactive, do normal firing stuff
      if (this.gun.IsReloading || (this.gun.CurrentAmmo == 0 && !this.gun.InfiniteAmmo))
        return; // inactive, do normal firing stuff
      if (this._isDoingAttack)
        return; // already attacking, do nothing
      this._isDoingAttack = true;
      if (!this.gun.InfiniteAmmo)
        this.gun.LoseAmmo(1);
      player.StartCoroutine(LungeAttack(player));
    }

    private static readonly int _IgnoreCollisions = CollisionMask.LayerToMask(
      CollisionLayer.Projectile, CollisionLayer.EnemyHitBox, CollisionLayer.EnemyCollider);

    private static float GetHeight(float velocity, float gravity, float time)
    {
      return velocity * time - 0.5f * gravity * time * time;
    }

    private IEnumerator LungeAttack(PlayerController player)
    {
      //NOTE: lots of code copied from Pogo Stick, tweak later as needed
      const float GRAVITY = 150f;
      const float VELOCITY = 25f;
      const float BOUNCE_TIME = (2f * VELOCITY) / GRAVITY;
      const float LANDING_TIME = 0.05f;
      const float JUMP_DISTANCE = 6f;
      const float RECOVERY_TIME = 0.5f;
      const float DESTROY_BULLET_RADIUS = 2f;
      const float DESTROY_BULLET_RADIUS_MASTERED = 3f;
      const float LAUNCH_RADIUS = 4f;
      const float LAUNCH_RADIUS_MASTERED = 5f;
      const float LAUNCH_RADIUS_SQR = LAUNCH_RADIUS * LAUNCH_RADIUS;
      const float LAUNCH_RADIUS_MASTERED_SQR = LAUNCH_RADIUS_MASTERED * LAUNCH_RADIUS_MASTERED;

      bool delayInvulnerability = false;
      bool mastered = this.Mastered;
      Vector2 lungeDirection = (player.unadjustedAimPoint.XY() - player.sprite.WorldCenter).normalized;
      player.lockedDodgeRollDirection = lungeDirection; // avoids some animation glitches
      player.SetIsFlying(true, Akelus.ItemName, adjustShadow: false);
      player.spriteAnimator.Play(player.DodgeRollClipForDirection(lungeDirection), 0f, 40f);
      player.m_handlingQueuedAnimation = true;
      player.SetInputOverride(Akelus.ItemName);
      this.gun.CanBeDropped = false;
      player.IsGunLocked = true;
      HealthHaver hh = player.healthHaver;
      if (hh.vulnerable)
          hh.TriggerInvulnerabilityPeriod(BOUNCE_TIME + LANDING_TIME + 0.05f);
      else //WARN: if we start a bounce during invulnerability, it can wear off during bounce since it's handled by a coroutine
          delayInvulnerability = true;
      player.specRigidbody.AddCollisionLayerIgnoreOverride(_IgnoreCollisions);
      int originalLayer = player.gameObject.layer;
      player.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Unoccluded"));
      Vector2 startingPos = player.transform.position;
      Vector2 target = player.CenterPosition + JUMP_DISTANCE * lungeDirection;
      Vector2 velocity = (target - startingPos) / BOUNCE_TIME;
      KnockbackDoer kb = player.knockbackDoer;
      this._leapKnockbackId = kb.ApplyContinuousKnockback(velocity.normalized, velocity.magnitude * 0.1f * kb.weight);
      Transform spriteTransform = player.sprite.transform;
      base.gameObject.Play("akelus_lunge_sound");
      for (float elapsed = 0f; elapsed < BOUNCE_TIME; elapsed += BraveTime.DeltaTime)
      {
          spriteTransform.localPosition = spriteTransform.localPosition.WithY(GetHeight(VELOCITY, GRAVITY, elapsed));
          yield return null;
          if (delayInvulnerability && hh.vulnerable)
          {
              delayInvulnerability = false;
              hh.TriggerInvulnerabilityPeriod(BOUNCE_TIME + LANDING_TIME + 0.05f - elapsed);
          }
      }
      player.m_handlingQueuedAnimation = false;
      kb.EndContinuousKnockback(this._leapKnockbackId);
      kb.SetImmobile(true, ItemName);
      this._leapKnockbackId = -1;
      player.gameObject.SetLayerRecursively(originalLayer);
      this.gun.PlayIfExists(gun.shootAnimation);
      yield return null;

      this.gun.DoSwingVFX();
      Vector2 shockwaveCenter = this.gun.barrelOffset.position;
      Lazy.ScorchGroundAt(shockwaveCenter);
      Exploder.DoDistortionWave(center: shockwaveCenter, distortionIntensity: 1.25f, distortionRadius: 0.5f, maxRadius: 1.5f, duration: 0.175f);

      float damage = 150.0f * player.DamageMult();
      float force = 20.0f;
      float sqrRange = mastered ? LAUNCH_RADIUS_MASTERED_SQR : LAUNCH_RADIUS_SQR;
      float horizontalForce = 100.0f * player.KnockbackMult();
      float range = Mathf.Min(Mathf.Sqrt(sqrRange), 10);

      if (mastered)
      {
        PassiveReflectItem.ReflectBulletsInRange(shockwaveCenter, DESTROY_BULLET_RADIUS_MASTERED, false, player, 30.0f);
        // if (DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.GreenFireDef) is DeadlyDeadlyGoopManager gooper)
        //   gooper.AddGoopRing(shockwaveCenter, minRadius: LAUNCH_RADIUS_MASTERED, maxRadius: LAUNCH_RADIUS_MASTERED + 1f);
      }
      else
        SilencerInstance.DestroyBulletsInRange(shockwaveCenter, DESTROY_BULLET_RADIUS, true, false, player);

      base.gameObject.Play("akelus_impact_sound");
      Lazy.LaunchAllEnemiesAroundPoint(damage, force, shockwaveCenter, 0, range, horizontalForce,
        flipPotency: 0.85f, potencyAtMaxRange: 0.30f);
      Exploder.DoRadialMinorBreakableBreak(shockwaveCenter, range);
      Exploder.DoRadialMajorBreakableDamage(damage, shockwaveCenter, range);
      Exploder.DoRadialPush(shockwaveCenter, force, range);
      GameManager.Instance.MainCameraController.DoScreenShake(new ScreenShakeSettings(0.5f, 2f, 0.1f, 0f), shockwaveCenter);
      CwaffVFX.SpawnBurst(
        prefab           : Akelus._ShockwaveVFX,
        numToSpawn       : (int)(range * 12),
        basePosition     : shockwaveCenter,
        positionVariance : 0.95f * range,
        velType          : CwaffVFX.Vel.AwayRadial,
        velocityVariance : 5f,
        rotType          : CwaffVFX.Rot.Velocity
        );

      player.ClearTableSlides();
      for (float elapsed = 0f; elapsed < LANDING_TIME; elapsed += BraveTime.DeltaTime)
      {
          yield return null;
          if (delayInvulnerability && hh.vulnerable)
          {
              delayInvulnerability = false;
              hh.TriggerInvulnerabilityPeriod(LANDING_TIME + 0.05f - elapsed);
          }
      }

      player.SetIsFlying(false, Akelus.ItemName, adjustShadow: false);
      player.specRigidbody.RemoveCollisionLayerIgnoreOverride(_IgnoreCollisions);
      float recoveryTime = RECOVERY_TIME * Mathf.Min(1f / player.FireRateMult(), player.ReloadRateMult());
      yield return new WaitForSeconds(recoveryTime);

      player.IsGunLocked = false;
      this.gun.CanBeDropped = true;
      this.gun.DoSwingVFX(reverse: true);
      this.gun.PlayIdleAnimation();
      player.ClearInputOverride(Akelus.ItemName);
      kb.SetImmobile(false, ItemName);
      this._isDoingAttack = false;
      yield break;
    }
}
