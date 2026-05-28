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
      const float LAUNCH_RADIUS = 4f;
      const float LAUNCH_RADIUS_SQR = LAUNCH_RADIUS * LAUNCH_RADIUS;

      bool delayInvulnerability = false;
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
      this._leapKnockbackId = -1;
      player.gameObject.SetLayerRecursively(originalLayer);
      this.gun.PlayIfExists(gun.shootAnimation);
      yield return null;

      this.gun.DoSwingVFX();
      Vector2 hammerPos = this.gun.barrelOffset.position;
      Lazy.ScorchGroundAt(hammerPos);
      Exploder.DoDistortionWave(center: hammerPos, distortionIntensity: 1.25f, distortionRadius: 0.5f, maxRadius: 1.5f, duration: 0.175f);
      SilencerInstance.DestroyBulletsInRange(hammerPos, DESTROY_BULLET_RADIUS, true, false, player);
      GameObject shockwave = new GameObject("akelus shockwave");
      shockwave.transform.position = hammerPos;
      AkelusShockwaveDoer asd = shockwave.AddComponent<AkelusShockwaveDoer>();
      asd.StartCoroutine(asd.AkelusShockwaveCR(150.0f * player.DamageMult(), 20.0f, LAUNCH_RADIUS_SQR, horizontalForce: 100.0f * player.KnockbackMult()));
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
      this._isDoingAttack = false;
      yield break;
    }
}

public class AkelusShockwaveDoer : MonoBehaviour
{
    public IEnumerator AkelusShockwaveCR(float damage, float force, float sqrRange, float horizontalForce)
    {
      float range = Mathf.Min(Mathf.Sqrt(sqrRange), 10);
      Vector2 shockwaveCenter = base.transform.position;
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
      yield return new WaitForSeconds(0.025f);
      UnityEngine.Object.Destroy(base.gameObject);;
    }
}
