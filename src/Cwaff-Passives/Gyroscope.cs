namespace CwaffingTheGungy;

public class Gyroscope : CwaffPassive
{
    public static string ItemName         = "Gyroscope";
    public static string ShortDescription = "Spin to Win";
    public static string LongDescription  = "Grants a chargeable dodge roll that transforms the user into a rampant tornado, reflecting projectiles but effectively randomizing shooting direction. Longer charges result in longer invulnerability periods, but may cause dizziness leaving the user briefly immobile and vulnerable.";
    public static string Lore             = "Watching this simple toy spin for even a few seconds is completely mesmerizing. Its trifold axes of rotation inspire truly revolutionary possibilities for avoiding projectiles.";

    internal static GameObject _TornadoVFX;

    private PlayerController _owner = null;
    private GyroscopeRoll _dodgeRoller = null;

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<Gyroscope>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.B;

        var comp = item.gameObject.AddComponent<GyroscopeRoll>();

        _TornadoVFX = VFX.Create("tornado", 20, loops: true, anchor: Anchor.LowerCenter);
    }

    private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherCollider)
    {
        if(!_dodgeRoller.isDodging)
            return;
        Projectile component = otherRigidbody.GetComponent<Projectile>();

        if (component == null || component.Owner is PlayerController)
            return;
        if (_dodgeRoller.reflectingProjectiles)
            PassiveReflectItem.ReflectBullet(component, true, Owner.specRigidbody.gameActor, 10f, 1f, 1f, 0f);
        if (!this.Owner.healthHaver.IsVulnerable)
            PhysicsEngine.SkipCollision = true;
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        this._dodgeRoller = this.gameObject.GetComponent<GyroscopeRoll>();
            this._dodgeRoller.owner = player;
        player.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
    }

    public override DebrisObject Drop(PlayerController player)
    {
        player.specRigidbody.OnPreRigidbodyCollision -= this.OnPreCollision;
        this._dodgeRoller.AbortDodgeRoll();
        return base.Drop(player);
    }

    public override void OnDestroy()
    {
        if (this.Owner)
            this.Owner.specRigidbody.OnPreRigidbodyCollision -= this.OnPreCollision;
        if (this._dodgeRoller)
            _dodgeRoller.AbortDodgeRoll();
        base.OnDestroy();
    }
}
public class GyroscopeRoll : CustomDodgeRoll
{
    const float MAX_DASH_TIME  = 4.0f;     // Max time we spend dashing
    const float MIN_DASH_TIME  = 1.0f;     // Min time we spend dashing
    const float MAX_ROT        = 180.0f;   // Max rotation per second
    const float MAX_DRIFT      = 60.0f;    // Max drift per second
    const float GYRO_FRICTION  = 0.99f;    // Friction coefficient
    const float MIN_SPIN       = 2*360.0f; // Starting spin speed (2RPS)
    const float MAX_SPIN       = 6*360.0f; // Ending spin speed (6RPS)
    const float CHARGE_TIME    = 3.0f;     // Time it takes to reach MAX_SPIN speed
    const float DIZZY_THRES    = 0.4f;     // Percent charge required to be dizzy after stop
    const float STUMBLE_THRES  = 0.75f;    // Percent charge required to stumble after stop
    const float STUMBLE_TIME   = 1.25f;    // Amount of time we stumble for after spinning
    const float STOP_FRICTION  = 0.9f;     // Friction when sliding to a halt
    const float TORNADO_ALPHA  = 0.5f;     // Max alpha of tornado VFX
    const float SPIN_DELTA     = MAX_SPIN - MIN_SPIN;

    public override bool putsOutFire => false; // We have custom fire extinguishing behavior

    public bool reflectingProjectiles { get; private set; }

    private bool useDriftMechanics          = true;
    private bool isSpeedModActive           = false;
    private bool isRollModActive            = false;
    private bool tookDamageDuringDodgeRoll  = false;
    private float forcedDirection           = 0.0f;
    private StatModifier speedModifier      = null;
    private StatModifier rollDamageModifier = null;
    private GameObject tornadoVFX           = null;
    private Vector2 targetVelocity          = Vector2.zero;

    private tk2dSpriteAnimationClip stumbleClip = null; // animation to use for stumbling
    private List<bool> wasFrameInvulnerable = new List<bool>(); // cache for whether stumble frames were vulnerable

    private void UpdateForcedDirection(float newDirection)
    {
        this.forcedDirection = newDirection;
        if (this.forcedDirection > 180)
        {
            this.owner.gameObject.Play("undertale_arrow");
            this.forcedDirection -= 360;
        }

        string animName = Lazy.GetBaseIdleAnimationName(this.owner,this.forcedDirection);
        if (!this.owner.spriteAnimator.IsPlaying(animName))
        {
            this.owner.spriteAnimator.Stop();
            this.owner.spriteAnimator.Play(animName);
        }
        if (this.owner.sprite.FlipX != (Mathf.Abs(this.forcedDirection) > 90f))
        {
            this.owner.sprite.FlipX ^= true;
            if (this.owner.sprite.FlipX)
                this.owner.sprite.gameObject.transform.localPosition = new Vector3(this.owner.sprite.GetUntrimmedBounds().size.x, 0f, 0f);
            else
                this.owner.sprite.gameObject.transform.localPosition = Vector3.zero;
            this.owner.sprite.UpdateZDepth();
        }

        this.owner.m_overrideGunAngle = this.forcedDirection;
        this.owner.forceAimPoint = this.owner.CenterPosition + BraveMathCollege.DegreesToVector(this.forcedDirection);
    }

    private void OnReceivedDamage(PlayerController p)
    {
        this.FinishDodgeRoll();
        this.tookDamageDuringDodgeRoll = true;
    }

    private float GetDodgeRollSpeed()
    {
        return (this.owner.rollStats.GetModifiedTime(this.owner) / this.owner.rollStats.GetModifiedDistance(this.owner)) / BraveTime.DeltaTime;
    }

    private void DoElasticCollision(SpeculativeRigidbody b1, SpeculativeRigidbody b2, out Vector2 newv1, out Vector2 newv2, bool ignoreOtherVelocity = false)
    {
        Vector2 x1 = b1.UnitCenter;
        Vector2 x2 = b2.UnitCenter;
        Vector2 v1 = b1.Velocity;
        Vector2 v2 = ignoreOtherVelocity ? Vector2.zero : b2.Velocity;
        float distNorm = Mathf.Max(0.1f,(x1-x2).sqrMagnitude);
        newv1 = v1 - (Vector2.Dot(v1-v2,x1-x2) / distNorm) * (x1-x2);
        newv2 = v2 - (Vector2.Dot(v2-v1,x2-x1) / distNorm) * (x2-x1);
    }

    private void BounceAwayEnemies(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherCollider)
    {
        if (!otherRigidbody || !otherRigidbody.aiActor || !otherRigidbody.aiActor.healthHaver || otherRigidbody.aiActor.healthHaver.IsDead)
            return;
        AIActor aIActor = otherRigidbody.aiActor;
        Vector2 myNewVelocity, theirNewVelocity;
        DoElasticCollision(myRigidbody,otherRigidbody,out myNewVelocity,out theirNewVelocity, true);
        float halfTotalMagnitude = 0.5f * (theirNewVelocity.magnitude + myNewVelocity.magnitude);
        aIActor.knockbackDoer.ApplyKnockback(theirNewVelocity, C.PIXELS_PER_TILE * halfTotalMagnitude);
        this.targetVelocity = this.targetVelocity.magnitude * myNewVelocity.normalized;
        this.owner.specRigidbody.Velocity = myNewVelocity;

        this.rollDamageModifier.amount = Mathf.Sqrt(this.targetVelocity.magnitude);
        this.owner.stats.RecalculateStats(this.owner,true);
        this.owner.ApplyRollDamage(aIActor);
        this.owner.gameObject.PlayOnce("undertale_damage");
    }

    private void ExtinguishFire()
    {
        if (this.owner.CurrentFireMeterValue <= 0f)
            return;

        this.owner.CurrentFireMeterValue = Mathf.Max(0f, this.owner.CurrentFireMeterValue - 1.0f * BraveTime.DeltaTime);
        if (this.owner.CurrentFireMeterValue == 0f)
            this.owner.IsOnFire = false;
    }

    public override IEnumerator ContinueDodgeRoll()
    {
        float minDashSpeed = GetDodgeRollSpeed(); // Min speed of our dash
        float maxDashSpeed = minDashSpeed * 5.0f; // Max speed of our dash

        #region Initialization
            DustUpVFX dusts = GameManager.Instance.Dungeon.dungeonDustups;
            BraveInput instanceForPlayer = BraveInput.GetInstanceForPlayer(this.owner.PlayerIDX);
            this.tookDamageDuringDodgeRoll = false;
            this.owner.OnReceivedDamage += this.OnReceivedDamage;
            this.owner.OnRealPlayerDeath += this.OnReceivedDamage;
            this.stumbleClip = null;

            this.tornadoVFX = UnityEngine.Object.Instantiate(
                Gyroscope._TornadoVFX, this.owner.specRigidbody.UnitBottomCenter, Quaternion.identity);
            tk2dSpriteAnimator tornadoAnimator = this.tornadoVFX.GetComponent<tk2dSpriteAnimator>();
                tornadoAnimator.sprite.transform.parent = this.owner.transform;
                tornadoAnimator.sprite.transform.position = this.owner.SpriteBottomCenter;
                tornadoAnimator.sprite.usesOverrideMaterial = true;
                tornadoAnimator.renderer.SetAlpha(0.0f);
        #endregion

        #region The Charge
            float totalTime = 0.0f;
            float curSpinSpeed = 0.0f;
            float tornadoCurAlpha = 0.0f;
            forcedDirection = this.owner.FacingDirection;
            this.owner.m_overrideGunAngle = forcedDirection;
            Vector3 chargeStartPosition = this.owner.transform.position;

            this.rollDamageModifier = new StatModifier();
                this.rollDamageModifier.statToBoost = PlayerStats.StatType.DodgeRollDamage;
                this.rollDamageModifier.modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE;
                this.rollDamageModifier.amount = 1.0f;
                this.owner.ownerlessStatModifiers.Add(rollDamageModifier);
            this.speedModifier = new StatModifier();
                this.speedModifier.statToBoost = PlayerStats.StatType.MovementSpeed;
                this.speedModifier.modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE;
                this.speedModifier.amount = 1.0f;
                this.owner.ownerlessStatModifiers.Add(speedModifier);
            this.isSpeedModActive = true;
            this.isRollModActive = true;
            this.owner.stats.RecalculateStats(this.owner);

            float chargePercent = 0.0f;
            while (instanceForPlayer.ActiveActions.DodgeRollAction.IsPressed)
            {
                if (this.owner.IsFalling || this.tookDamageDuringDodgeRoll)
                    yield break;
                totalTime += BraveTime.DeltaTime;
                chargePercent = Mathf.Min(1.0f,totalTime / CHARGE_TIME);
                curSpinSpeed = MIN_SPIN + SPIN_DELTA * (chargePercent*chargePercent);
                UpdateForcedDirection(this.forcedDirection+curSpinSpeed*BraveTime.DeltaTime);
                this.speedModifier.amount = 1.0f - (chargePercent*chargePercent);
                this.owner.stats.RecalculateStats(this.owner,true);
                this.owner.specRigidbody.Reinitialize();

                if (UnityEngine.Random.Range(0.0f,100.0f) < 10)
                {
                    float dir = forcedDirection;
                    float rot = UnityEngine.Random.Range(0.0f,360.0f);
                    float mag = UnityEngine.Random.Range(0.3f,1.25f);
                    SpawnManager.SpawnVFX(
                        dusts.rollLandDustup,
                        this.owner.CenterPosition - BraveMathCollege.DegreesToVector(dir, mag),
                        Quaternion.Euler(0f, 0f, rot));
                }
                if (chargePercent >= DIZZY_THRES)
                {
                    tornadoCurAlpha = TORNADO_ALPHA * (chargePercent - DIZZY_THRES) / (1.0f - DIZZY_THRES);
                    tornadoAnimator.sprite.transform.position = this.owner.SpriteBottomCenter;
                    tornadoAnimator.renderer.SetAlpha(tornadoCurAlpha);
                }

                ExtinguishFire();
                yield return null;
            }
            this.owner.ownerlessStatModifiers.Remove(this.speedModifier);
            this.isSpeedModActive = false;
            this.owner.stats.RecalculateStats(this.owner);
        #endregion

        #region The Dash
            this.owner.SetIsFlying(true, "gyro", false, false);
            this.reflectingProjectiles = chargePercent >= DIZZY_THRES;
            this.owner.specRigidbody.OnPreRigidbodyCollision += BounceAwayEnemies;
            this.owner.specRigidbody.OnCollision += BounceOffWalls;
            this.owner.healthHaver.IsVulnerable = false;

            float dash_speed    = minDashSpeed  + chargePercent * (maxDashSpeed  - minDashSpeed);
            float dash_time     = MIN_DASH_TIME + chargePercent * (MAX_DASH_TIME - MIN_DASH_TIME);
            this.targetVelocity = dash_speed*instanceForPlayer.ActiveActions.Move.Value;
            for (float timer = 0.0f; timer < dash_time; timer += BraveTime.DeltaTime)
            {
                if (this.owner.IsFalling || this.tookDamageDuringDodgeRoll)
                    yield break;
                UpdateForcedDirection(this.forcedDirection+curSpinSpeed*BraveTime.DeltaTime);  //2.0 RPS

                // adjust angle / velocity of spin if necessary
                if (this.useDriftMechanics)
                {
                    float maxDrift = MAX_DRIFT * BraveTime.DeltaTime;
                    Vector2 drift = maxDrift*instanceForPlayer.ActiveActions.Move.Value;
                    this.targetVelocity += drift;
                    if (this.targetVelocity.magnitude > dash_speed)
                        this.targetVelocity = dash_speed * this.targetVelocity.normalized;
                }
                else // use turn mechanics
                {
                    float maxRot = MAX_ROT * BraveTime.DeltaTime;
                    float velangle = this.targetVelocity.ToAngle();
                    float deltaToTarget = BraveMathCollege.ClampAngle180(this.owner.FacingDirection - velangle);
                    if (Mathf.Abs(deltaToTarget) <= maxRot)
                        this.targetVelocity = BraveMathCollege.DegreesToVector(this.owner.FacingDirection,dash_speed);
                    else
                        this.targetVelocity = BraveMathCollege.DegreesToVector(velangle+Mathf.Sign(deltaToTarget)*maxRot,dash_speed);
                }
                this.targetVelocity *= GYRO_FRICTION;
                this.owner.specRigidbody.Velocity = this.targetVelocity;

                if (UnityEngine.Random.Range(0.0f,100.0f) < 10)
                {
                    float dir = forcedDirection;
                    float rot = UnityEngine.Random.Range(0.0f,360.0f);
                    float mag = UnityEngine.Random.Range(0.3f,1.25f);
                    SpawnManager.SpawnVFX(
                        dusts.rollLandDustup,
                        this.owner.CenterPosition - BraveMathCollege.DegreesToVector(dir, mag),
                        Quaternion.Euler(0f, 0f, rot));
                }

                tornadoAnimator.sprite.transform.position = this.owner.SpriteBottomCenter;
                ExtinguishFire();
                yield return null;
            }
            this.owner.specRigidbody.OnPreRigidbodyCollision -= BounceAwayEnemies;
            this.owner.specRigidbody.OnCollision -= BounceOffWalls;
            this.owner.healthHaver.IsVulnerable = true;
            this.reflectingProjectiles = false;

            this.owner.ownerlessStatModifiers.Remove(this.rollDamageModifier);
            this.isRollModActive = false;
            this.owner.stats.RecalculateStats(this.owner);
            this.owner.SetIsFlying(false, "gyro", false, false);
        #endregion

        #region The Stumble
            if (chargePercent >= DIZZY_THRES)
            {
                this.owner.SetInputOverride("gyrostumble");
                this.owner.ToggleGunRenderers(false,"gyrostumble");
                this.owner.ToggleHandRenderers(false,"gyrostumble");

                this.owner.spriteAnimator.Stop();
                this.owner.QueueSpecificAnimation(this.owner.spriteAnimator.GetClipByName("spinfall"/*"timefall"*/).name);
                this.owner.spriteAnimator.SetFrame(0, false);
                this.owner.gameObject.Play("Play_Fall");
                float spinTimer = 0.65f;
                for (float timer = spinTimer; timer > 0; timer -= BraveTime.DeltaTime)
                {
                    if (this.tookDamageDuringDodgeRoll)
                        yield break;
                    tornadoAnimator.renderer.SetAlpha(tornadoCurAlpha * (timer / spinTimer));
                    tornadoAnimator.sprite.transform.position = this.owner.sprite.WorldBottomCenter;
                    this.targetVelocity *= STOP_FRICTION;
                    this.owner.specRigidbody.Velocity = this.targetVelocity;
                    yield return null;
                }
                UnityEngine.Object.Destroy(this.tornadoVFX);
                this.owner.spriteAnimator.Stop();

                if (chargePercent >= STUMBLE_THRES)
                {
                    string stumbleAnim = Lazy.GetBaseDodgeAnimationName(this.owner, this.owner.specRigidbody.Velocity);
                    this.stumbleClip = this.owner.spriteAnimator.GetClipByName(stumbleAnim);
                    this.owner.QueueSpecificAnimation(stumbleClip.name);
                    this.owner.spriteAnimator.SetFrame(0, false);
                    this.owner.spriteAnimator.ClipFps = 24.0f;
                    this.wasFrameInvulnerable = new List<bool>();
                    foreach (var frame in this.stumbleClip.frames) // hack to make player vulnerable during roll animation frames
                    {
                        this.wasFrameInvulnerable.Add(frame.invulnerableFrame);
                        frame.invulnerableFrame = false;
                    }

                    this.owner.sprite.FlipX = (Mathf.Abs(this.owner.specRigidbody.Velocity.ToAngle()) > 90f);
                    if (this.owner.sprite.FlipX)
                        this.owner.sprite.gameObject.transform.localPosition = new Vector3(this.owner.sprite.GetUntrimmedBounds().size.x, 0f, 0f);
                    else
                        this.owner.sprite.gameObject.transform.localPosition = Vector3.zero;
                    this.owner.sprite.UpdateZDepth();

                    for (float timer = 0.0f; timer < STUMBLE_TIME; timer += BraveTime.DeltaTime)
                    {
                        if (this.tookDamageDuringDodgeRoll)
                            break;
                        this.owner.specRigidbody.Velocity = Vector2.zero;
                        if (this.owner.spriteAnimator.CurrentFrame > 3)
                            this.owner.spriteAnimator.Stop();
                        yield return null;
                    }
                }
            }
        #endregion

        yield break;
    }

    public override void FinishDodgeRoll()
    {
        #region Cleanup
            if (this.stumbleClip != null)
            {
                for (int i = 0; i < this.stumbleClip.frames.Length; ++i)
                    this.stumbleClip.frames[i].invulnerableFrame = this.wasFrameInvulnerable[i];
            }

            this.owner.OnReceivedDamage -= this.OnReceivedDamage;
            this.reflectingProjectiles = false;
            this.owner.specRigidbody.OnPreRigidbodyCollision -= BounceAwayEnemies;
            this.owner.specRigidbody.OnCollision -= BounceOffWalls;
            this.owner.ClearInputOverride("gyro");
            this.owner.SetIsFlying(false, "gyro", false, false);
            this.owner.ToggleHandRenderers(true,"gyrostumble");
            this.owner.ToggleGunRenderers(true,"gyrostumble");
            this.owner.ClearInputOverride("gyrostumble");
            this.owner.m_overrideGunAngle = null;
            this.owner.forceAimPoint = null;
            if (this.owner.CurrentGun is Gun gun) // fix upside down gun sprites when starting a dodge roll facing right and ending facing left
                gun.HandleSpriteFlip(this.owner.SpriteFlipped);
            this.owner.spriteAnimator.Stop();
            this.owner.spriteAnimator.Play(this.owner.spriteAnimator.GetClipByName("idle_front"));
            this.owner.healthHaver.IsVulnerable = true;

            if (this.isSpeedModActive)
                this.owner.ownerlessStatModifiers.Remove(this.speedModifier);
            if (this.isRollModActive)
                this.owner.ownerlessStatModifiers.Remove(this.rollDamageModifier);
            this.isSpeedModActive = false;
            this.isRollModActive = false;
            this.owner.stats.RecalculateStats(this.owner);

            if (this.tornadoVFX)
                UnityEngine.Object.Destroy(this.tornadoVFX);
        #endregion
    }

    private void BounceOffWalls(CollisionData tileCollision)
    {
        float velangle = (-this.targetVelocity).ToAngle();
        float normangle = tileCollision.Normal.ToAngle();
        float newangle = BraveMathCollege.ClampAngle360(velangle + 2f * (normangle - velangle));
        this.targetVelocity = BraveMathCollege.DegreesToVector(newangle,this.targetVelocity.magnitude);
        this.owner.specRigidbody.Velocity = this.targetVelocity;
    }
}

