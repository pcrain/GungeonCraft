namespace CwaffingTheGungy;

public class PegasusBoots : CwaffDodgeRollItem
{
    public static string ItemName         = "Pegasus Boots";
    public static string ShortDescription = "Dashingly Stylish";
    public static string LongDescription  = "Hold dodge while standing still to charge up a dash. Dashing destroys bullets, deals high damage to enemies, and grants immunity to most forms of contact damage. Dashes cannot cross pits or be interrupted until rebounding off a solid object.";
    public static string Lore             = "These boots were once owned by a green-clad lad whose ears were bigger than his feet. They served him well for many adventures, but repeatedly crashing into walls caused enough head trauma that his short term memory was permanently affected. He forgot about these boots mere seconds after taking them off to take a quick swim.";

    private PegasusBootsRoll _dodgeRoller = null;

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<PegasusBoots>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.D;
        item.gameObject.AddComponent<PegasusBootsRoll>();
    }

    private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherCollider)
    {
        if(!this._dodgeRoller._isDodging || !this._dodgeRoller.DestroysProjectiles())
            return;
        Projectile projectile = otherRigidbody.GetComponent<Projectile>();
        if (projectile != null && projectile.Owner is not PlayerController)
        {
            projectile.DieInAir();
            PhysicsEngine.SkipCollision = true;
        }
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        this._dodgeRoller = this.gameObject.GetComponent<PegasusBootsRoll>();
        player.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        if (!player)
            return;
        player.specRigidbody.OnPreRigidbodyCollision -= this.OnPreCollision;
        if (this._dodgeRoller)
            this._dodgeRoller.AbortDodgeRoll();
    }

    public override CustomDodgeRoll CustomDodgeRoll()
    {
        if (!this._dodgeRoller)
            this._dodgeRoller = this.gameObject.GetComponent<PegasusBootsRoll>();
        return this._dodgeRoller;
    }

    public override void Update()
    {
      base.Update();
      CustomDodgeRoll roll = CustomDodgeRoll();
      if (!this.m_owner || this.m_owner.IsDodgeRolling || roll._isDodging)
        return;  // don't toggle enabled status while we're actively dodge rolling
      if (this.m_owner.CurrentInputState != PlayerInputState.AllInput)
        return;  // don't toggle enabled status while not in control of our character (fixes softlock when opening a chest during dodge roll)
      if (!roll.IsEnabled && roll._dodgeButtonPressed)
        return; // fixes a quirk where if you're coming out of a normal dodge roll and stop moving while the dodge button is held, the Pegasus dodge roll immediately starts charging
      roll.IsEnabled = this.m_owner.m_activeActions.Move.Vector == Vector2.zero;
    }
}

public class PegasusBootsRoll : CustomDodgeRoll
{
    private enum Phase { Charging, Charged, Dashing, Rebound }

    private Vector2 _dashDir;
    private Vector2 _collisionNormal;
    private Phase _phase;
    private bool _ranIntoWall;
    private int _reboundKnockbackId;
    private int _originalLayer;

    public override bool  canUseWeapon         => false;
    public override bool  dodgesProjectiles    => false; // we have our own projectile collision handling
    public override bool  takesContactDamage   => this._phase != Phase.Rebound;
    public override bool  isAirborne           => this._phase == Phase.Rebound;
    public override bool  lockedDirection      => this._phase != Phase.Rebound;
    public override bool  canDodgeInPlace      => true;
    public override bool  countsAsDodgeRolling => this._phase != Phase.Rebound;
    public override float overrideRollDamage   => this._phase == Phase.Dashing ? 40 : 0;

    public bool DestroysProjectiles()
    {
        return this._phase == Phase.Dashing;
    }

    private static float GetHeight(float velocity, float gravity, float time)
    {
      return velocity * time - 0.5f * gravity * time * time;
    }

    private Vector2 GetDashDirection()
    {
      return (this._owner.DetermineAimPointInWorld().XY() - this._owner.CenterPosition).ToAngle().Quantize(90f, VectorConversions.Round).ToVector();
    }

    private bool ShouldAbort()
    {
      return !this._owner || this._owner.IsFalling;
    }

    private void DoDustUps()
    {
        this._owner.gameObject.Play("pegasus_boots_dash_sound");
        float dir = UnityEngine.Random.Range(0.0f,360.0f);
        float mag = UnityEngine.Random.Range(0.3f,0.75f);
        SpawnManager.SpawnVFX(
            GameManager.Instance.Dungeon.dungeonDustups.rollLandDustup,
            this._owner.CenterPosition + BraveMathCollege.DegreesToVector(dir, mag),
            Quaternion.identity);
    }

    private void OnCollision(CollisionData data)
    {
        this._collisionNormal = data.Normal;
        this._owner.gameObject.Play("pegasus_boots_collide_sound");
        this._ranIntoWall = true;
    }

    protected override void BeginDodgeRoll(Vector2 direction, bool buffered, bool wasAlreadyDodging)
    {
        this._dashDir = GetDashDirection();
        this._phase = Phase.Charging;
        this._ranIntoWall = false;
    }

    protected override IEnumerator ContinueDodgeRoll()
    {
        const int STEPS_PER_SEC_CHARGING = 6;
        const int STEPS_PER_SEC_DASHING  = 9;
        const int ANIMATION_RATE_MULT    = 3;
        const float DASH_SPEED           = 60.0f;
        const float CHARGE_TIME          = 0.85f;
        const float GRAVITY              = 300f;
        const float VELOCITY             = 40f;
        const float REBOUND_TIME         = (2f * VELOCITY) / GRAVITY;
        const float REBOUND_SPEED        = 10f;

        // Charging / Charged Phase
        this._phase = Phase.Charging;

        this._owner.m_handlingQueuedAnimation = true;
        this._owner.spriteAnimator.Play(this._owner.RunningClipForDirection(this._dashDir), 0f, STEPS_PER_SEC_CHARGING * ANIMATION_RATE_MULT);
        this._owner.m_overrideGunAngle = this._dashDir.ToAngle();
        this._owner.usingForcedInput = true; // HACK: temporary until lockedDirection() is fixed in Alexandria
        this._owner.forcedInput = Vector2.zero;
        float chargeTime = 0.0f;
        while (this._dodgeButtonHeld)
        {
          if (ShouldAbort())
          {
            AbortDodgeRoll();
            yield break;
          }
          float oldAngle = this._owner.m_overrideGunAngle.Value;
          this._dashDir = GetDashDirection();
          this._owner.m_overrideGunAngle = this._dashDir.ToAngle();
          float oldChargeTime = chargeTime;
          chargeTime += BraveTime.DeltaTime;
          int soundRate = (this._phase == Phase.Charging) ? STEPS_PER_SEC_CHARGING : STEPS_PER_SEC_DASHING;
          if (this._owner.m_overrideGunAngle.Value != oldAngle)
            this._owner.spriteAnimator.Play(this._owner.RunningClipForDirection(this._dashDir), 0f, soundRate * ANIMATION_RATE_MULT);
          if ((int)(oldChargeTime * soundRate) < (int)(chargeTime * soundRate))
            DoDustUps();
          if (this._phase == Phase.Charging && chargeTime >= CHARGE_TIME)
          {
            this._phase = Phase.Charged;
            this._owner.spriteAnimator.Play(this._owner.RunningClipForDirection(this._dashDir), 0f, STEPS_PER_SEC_DASHING * ANIMATION_RATE_MULT);
            this._owner.gameObject.Play("pegasus_boots_ready_sound");
          }
          yield return null;
        }

        // Charged Phase
        if (this._phase != Phase.Charged)
          yield break; // dodge roll aborted, nothing else to do

        // Dashing Phase
        this._owner.forcedInput = this._dashDir;
        this._phase = Phase.Dashing;
        this._owner.specRigidbody.OnCollision += this.OnCollision;
        this._owner.SetInvulnerable(true, PegasusBoots.ItemName);
        Vector2 dashVelocity = DASH_SPEED * this._dashDir;
        this._owner.specRigidbody.Velocity = dashVelocity;
        float dashTime = 0.0f;
        while (!this._ranIntoWall)
        {
          if (ShouldAbort())
          {
            AbortDodgeRoll();
            yield break;
          }
          float oldDashTime = dashTime;
          dashTime += BraveTime.DeltaTime;
          if ((int)(oldDashTime * STEPS_PER_SEC_DASHING) < (int)(dashTime * STEPS_PER_SEC_DASHING))
            DoDustUps();
          this._owner.specRigidbody.Velocity = dashVelocity;
          yield return null;
        }

        // Rebound Phase
        this._phase = Phase.Rebound;
        this._owner.SetInputOverride(PegasusBoots.ItemName);
        this._owner.usingForcedInput = false;
        this._owner.SetIsFlying(true, PegasusBoots.ItemName, adjustShadow: false);
        KnockbackDoer kb = this._owner.knockbackDoer;
        Vector2 velocity = this._collisionNormal;
        this._reboundKnockbackId = kb.ApplyContinuousKnockback(velocity, REBOUND_SPEED * 0.1f * kb.weight);
        this._originalLayer = this._owner.gameObject.layer;
        this._owner.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Unoccluded"));
        Transform spriteTransform = this._owner.sprite.transform;
        for (float elapsed = 0f; elapsed < REBOUND_TIME; elapsed += BraveTime.DeltaTime)
        {
            if (ShouldAbort())
            {
              AbortDodgeRoll();
              yield break;
            }
            spriteTransform.localPosition = spriteTransform.localPosition.WithY(GetHeight(VELOCITY, GRAVITY, elapsed));
            yield return null;
        }
        yield break;
    }

    protected override void FinishDodgeRoll(bool aborted = false)
    {
        if (this._originalLayer >= 0)
        {
          this._owner.gameObject.SetLayerRecursively(this._originalLayer);
          this._originalLayer = -1;
        }
        this._owner.SetIsFlying(false, PegasusBoots.ItemName, adjustShadow: false);
        this._ranIntoWall = false;
        this._owner.ClearInputOverride(PegasusBoots.ItemName);
        this._owner.knockbackDoer.EndContinuousKnockback(this._reboundKnockbackId);
        this._reboundKnockbackId = -1;
        this._owner.SetInvulnerable(false, PegasusBoots.ItemName);
        this._owner.m_handlingQueuedAnimation = false;
        this._owner.spriteAnimator.Stop();
        this._owner.m_overrideGunAngle = null;
        this._owner.specRigidbody.OnCollision -= this.OnCollision;
        this._owner.usingForcedInput = false;
        Transform spriteTransform = this._owner.sprite.transform;
        spriteTransform.localPosition = spriteTransform.localPosition.WithY(0);
    }
}
