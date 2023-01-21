using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;

using Gungeon;

/*
    TODO:
      - add proper charge mechanics (incremental rev up / rev down)
      - possibly replace input overrides with 0 velocity passive stat boost like natasha
      - polish graphical effects

      - balance all mechanics
*/

namespace CwaffingTheGungy
{
    public class Gyroscope : PassiveItem
    {
        public static string passiveName      = "Gyroscope";
        public static string spritePath       = "CwaffingTheGungy/Resources/ItemSprites/gyroscope_icon";
        public static string shortDescription = "Spin to Win";
        public static string longDescription  = "(spinspinspinspinspinspinspinspinspinspin)";

        private PlayerController owner = null;
        private GyroscopeRoll dodgeRoller = null;

        public static void Init()
        {
            PickupObject item = Lazy.SetupItem<Gyroscope>(passiveName, spritePath, shortDescription, longDescription, "cg");
            item.quality      = PickupObject.ItemQuality.A;

            var comp = item.gameObject.AddComponent<GyroscopeRoll>();
        }

        private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherCollider)
        {
            if(!(dodgeRoller.isDodging && dodgeRoller.reflectingProjectiles))  // reflect projectiles with hyped synergy
                return;
            Projectile component = otherRigidbody.GetComponent<Projectile>();
            if (component != null && !(component.Owner is PlayerController))
            {
                PassiveReflectItem.ReflectBullet(component, true, Owner.specRigidbody.gameActor, 10f, 1f, 1f, 0f);
                PhysicsEngine.SkipCollision = true;
            }
        }

        public override void Pickup(PlayerController player)
        {
            this.owner = player;
            dodgeRoller = this.gameObject.GetComponent<GyroscopeRoll>();
            dodgeRoller.owner = this.owner;
            SpeculativeRigidbody specRigidbody = player.specRigidbody;
            specRigidbody.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Combine(specRigidbody.OnPreRigidbodyCollision, new SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate(this.OnPreCollision));
            base.Pickup(player);
        }

        public override DebrisObject Drop(PlayerController player)
        {
            SpeculativeRigidbody specRigidbody2 = player.specRigidbody;
            specRigidbody2.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Remove(specRigidbody2.OnPreRigidbodyCollision, new SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate(this.OnPreCollision));
            this.owner = null;
            dodgeRoller.AbortDodgeRoll();
            return base.Drop(player);
        }

        public override void OnDestroy()
        {
            dodgeRoller.AbortDodgeRoll();
            base.OnDestroy();
        }
    }
    public class GyroscopeRoll : CustomDodgeRoll
    {
        const float MAX_DASH_TIME  = 4.0f;   // Max time we spend dashing
        const float MIN_DASH_TIME  = 1.0f;   // Min time we spend dashing
        const float MAX_ROT        = 180.0f; // Max rotation per second
        const float MAX_DRIFT      = 40.0f;  // Max drift per second
        const float GYRO_FRICTION  = 0.99f;  // Friction coefficient
        const float MIN_SPIN       = 2*360.0f; // Starting spin speed (2RPS)
        const float MAX_SPIN       = 6*360.0f; // Ending spin speed (6RPS)
        const float SPIN_DELTA     = MAX_SPIN - MIN_SPIN;
        const float CHARGE_TIME    = 3.0f;    // Time it takes to reach MAX_SPIN speed
        const float DIZZY_THRES    = 0.5f;    // Percent charge required to be dizzy after stop
        const float STUMBLE_THRES  = 0.75f;    // Percent charge required to stumble after stop
        const float STUMBLE_TIME   = 1.5f;   // Amount of time we stumble for after spinning

        public bool reflectingProjectiles { get; private set; }

        private bool useDriftMechanics = true;
        private Vector2 targetVelocity = Vector2.zero;
        private float forcedDirection = 0.0f;
        private Shader oldShader = null;
        private bool tookDamageDuringDodgeRoll = false;
        private StatModifier speedModifier = null;
        private bool isSpeedModifierActive = false;

        private Vector4 GetCenterPointInScreenUV(Vector2 centerPoint)
        {
            Vector3 vector = GameManager.Instance.MainCameraController.Camera.WorldToViewportPoint(centerPoint.ToVector3ZUp());
            return new Vector4(vector.x, vector.y, 0f, 0f);
        }

        private void UpdateForcedDirection(float newDirection)
        {
            this.forcedDirection = newDirection;
            if (this.forcedDirection > 180)
            {
                AkSoundEngine.PostEvent("undertale_arrow", this.owner.gameObject);
                this.forcedDirection -= 360;
            }

            string animName = Lazy.GetBaseIdleAnimationName(this.owner,this.forcedDirection);
            if (!this.owner.spriteAnimator.IsPlaying(animName))
            {
                this.owner.spriteAnimator.Stop();
                this.owner.spriteAnimator.Play(animName);
                this.owner.spriteAnimator.SetFrame(0, false);
            }
            bool lastFlipped = this.owner.sprite.FlipX;
            this.owner.sprite.FlipX = (this.forcedDirection > 90f || this.forcedDirection < -90f);
            if (this.owner.sprite.FlipX != lastFlipped)
            {
                if (this.owner.sprite.FlipX)
                    this.owner.sprite.gameObject.transform.localPosition = new Vector3(this.owner.sprite.GetUntrimmedBounds().size.x, 0f, 0f);
                else
                    this.owner.sprite.gameObject.transform.localPosition = Vector3.zero;
                this.owner.sprite.UpdateZDepth();
            }

            this.owner.m_overrideGunAngle = this.forcedDirection;
            this.owner.forceAimPoint = this.owner.sprite.WorldCenter + Lazy.AngleToVector(this.forcedDirection);
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

        public override IEnumerator ContinueDodgeRoll()
        {
            float minDashSpeed = GetDodgeRollSpeed();    // Min speed of our dash
            float maxDashSpeed = minDashSpeed * 4.0f;  // Max speed of our dash

            #region Initialization
                DustUpVFX dusts = GameManager.Instance.Dungeon.dungeonDustups;
                BraveInput instanceForPlayer = BraveInput.GetInstanceForPlayer(this.owner.PlayerIDX);
                this.oldShader = this.owner.sprite.renderer.material.shader;
                // this.owner.sprite.usesOverrideMaterial = true;
                // this.owner.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
                //     this.owner.sprite.renderer.material.SetFloat("_EmissivePower", 1.55f);
                //     this.owner.sprite.renderer.material.SetFloat("_EmissiveColorPower", 1.55f);
                //     this.owner.sprite.renderer.material.SetColor("_EmissiveColor", Color.magenta);
                this.tookDamageDuringDodgeRoll = false;
                this.owner.OnReceivedDamage += this.OnReceivedDamage;
                this.owner.OnRealPlayerDeath += this.OnReceivedDamage;
            #endregion

            #region The Charge
                float totalTime = 0.0f;
                float curSpinSpeed = 0.0f;
                forcedDirection = this.owner.FacingDirection;
                this.owner.m_overrideGunAngle = forcedDirection;
                Vector3 chargeStartPosition = this.owner.transform.position;

                this.speedModifier = new StatModifier();
                    this.speedModifier.statToBoost = PlayerStats.StatType.MovementSpeed;
                    this.speedModifier.modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE;
                    this.speedModifier.amount = 1.0f;
                this.isSpeedModifierActive = true;
                this.owner.ownerlessStatModifiers.Add(speedModifier);

                float chargePercent = 0.0f;
                while (instanceForPlayer.ActiveActions.DodgeRollAction.IsPressed)
                {
                    if (this.tookDamageDuringDodgeRoll)
                        yield break;
                    totalTime += BraveTime.DeltaTime;
                    chargePercent = Mathf.Min(1.0f,totalTime / CHARGE_TIME);
                    curSpinSpeed = MIN_SPIN + SPIN_DELTA * (chargePercent*chargePercent);
                    // this.owner.sprite.renderer.material.SetFloat("_EmissivePower", 20.0f*Mathf.Abs(Mathf.Sin(totalTime*4.0f*totalTime/CHARGE_TIME)));
                    // TODO: DoDistortionWave() lags the game horrendously if you die
                    // Exploder.DoDistortionWave(this.owner.sprite.WorldCenter, 1.8f, 0.01f, 0.5f, 0.1f);
                    UpdateForcedDirection(this.forcedDirection+curSpinSpeed*BraveTime.DeltaTime);
                    this.speedModifier.amount = 1.0f - (chargePercent*chargePercent);
                    this.owner.stats.RecalculateStats(this.owner);
                    this.owner.specRigidbody.Reinitialize();

                    if (UnityEngine.Random.Range(0.0f,100.0f) < 10)
                    {
                        float dir = forcedDirection;
                        float rot = UnityEngine.Random.Range(0.0f,360.0f);
                        float mag = UnityEngine.Random.Range(0.3f,1.25f);
                        SpawnManager.SpawnVFX(
                            dusts.rollLandDustup,
                            this.owner.sprite.WorldCenter - Lazy.AngleToVector(dir, mag),
                            Quaternion.Euler(0f, 0f, rot));
                    }
                    yield return null;
                }
                this.owner.ownerlessStatModifiers.Remove(speedModifier);
                this.isSpeedModifierActive = false;
                this.owner.stats.RecalculateStats(this.owner);
            #endregion

            #region The Dash
                this.owner.SetIsFlying(true, "gyro", false, false);
                this.reflectingProjectiles = true;
                this.owner.specRigidbody.OnCollision += BounceOffWalls;
                float dash_speed    = minDashSpeed  + chargePercent * (maxDashSpeed  - minDashSpeed);
                float dash_time     = MIN_DASH_TIME + chargePercent * (MAX_DASH_TIME - MIN_DASH_TIME);

                // this.targetVelocity = Lazy.AngleToVector(this.owner.FacingDirection,dash_speed);
                this.targetVelocity = dash_speed*instanceForPlayer.ActiveActions.Move.Value;
                for (float timer = 0.0f; timer < dash_time; timer += BraveTime.DeltaTime)
                {
                    if (this.owner.IsFalling || this.tookDamageDuringDodgeRoll)
                        yield break;
                    // this.owner.PlayerAfterImage();
                    // TODO: DoDistortionWave() lags the game horrendously if you die
                    // Exploder.DoDistortionWave(this.owner.sprite.WorldCenter, 1.8f, 0.01f, 0.5f, 0.1f);
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
                        float newangle = velangle;
                        float deltaToTarget = this.owner.FacingDirection - velangle;
                        if (deltaToTarget > 180)
                            deltaToTarget -= 360f;
                        else if (deltaToTarget < -180)
                            deltaToTarget += 360f;
                        if (Mathf.Abs(deltaToTarget) <= maxRot)
                            newangle = this.owner.FacingDirection;
                        else
                            newangle += Mathf.Sign(deltaToTarget)*maxRot;
                        this.targetVelocity = Lazy.AngleToVector(newangle,dash_speed);
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
                            this.owner.sprite.WorldCenter - Lazy.AngleToVector(dir, mag),
                            Quaternion.Euler(0f, 0f, rot));
                    }
                    yield return null;
                }
                this.owner.specRigidbody.OnCollision -= BounceOffWalls;
                this.reflectingProjectiles = false;
            #endregion

            #region The Stumble
                if (chargePercent >= DIZZY_THRES)
                {
                    // Dissect.PrintSpriteCollectionNames(this.owner.sprite.collection);
                    string stumbleAnim = Lazy.GetBaseDodgeAnimationName(this.owner,this.owner.specRigidbody.Velocity);
                    float stumbleAngle = this.owner.specRigidbody.Velocity.ToAngle();
                    this.owner.specRigidbody.Velocity = Vector2.zero;

                    this.owner.SetIsFlying(false, "gyro", false, false);
                    this.owner.sprite.renderer.material.shader = this.oldShader;
                    this.owner.sprite.usesOverrideMaterial = false;

                    this.owner.SetInputOverride("gyrostumble");
                    this.owner.ToggleGunRenderers(false,"gyrostumble");
                    this.owner.ToggleHandRenderers(false,"gyrostumble");

                    this.owner.spriteAnimator.Stop();
                    this.owner.QueueSpecificAnimation(this.owner.spriteAnimator.GetClipByName("spinfall"/*"timefall"*/).name);
                    this.owner.spriteAnimator.SetFrame(0, false);
                    AkSoundEngine.PostEvent("Play_Fall", this.owner.gameObject);
                    for (float timer = 0.0f; timer < 0.65f; timer += BraveTime.DeltaTime)
                    {
                        if (this.tookDamageDuringDodgeRoll)
                            yield break;
                        yield return null;
                    }
                    this.owner.spriteAnimator.Stop();

                    if (chargePercent >= STUMBLE_THRES)
                    {
                        tk2dSpriteAnimationClip stumbleClip = this.owner.spriteAnimator.GetClipByName(stumbleAnim);
                        this.owner.QueueSpecificAnimation(stumbleClip.name);
                        this.owner.spriteAnimator.SetFrame(0, false);
                        this.owner.spriteAnimator.ClipFps = 24.0f;

                        this.owner.sprite.FlipX = (stumbleAngle > 90f || stumbleAngle < -90f);
                        if (this.owner.sprite.FlipX)
                            this.owner.sprite.gameObject.transform.localPosition = new Vector3(this.owner.sprite.GetUntrimmedBounds().size.x, 0f, 0f);
                        else
                            this.owner.sprite.gameObject.transform.localPosition = Vector3.zero;
                        this.owner.sprite.UpdateZDepth();

                        // hacky nonsense to make the player vulnerable during a roll animation frame
                        List<bool> wasFrameInvulnerable = new List<bool>();
                        foreach (var frame in stumbleClip.frames)
                        {
                            wasFrameInvulnerable.Add(frame.invulnerableFrame);
                            frame.invulnerableFrame = false;
                        }
                        for (float timer = 0.0f; timer < STUMBLE_TIME; timer += BraveTime.DeltaTime)
                        {
                            if (this.tookDamageDuringDodgeRoll)
                                break;
                            this.owner.specRigidbody.Velocity = Vector2.zero;
                            if (this.owner.spriteAnimator.CurrentFrame > 3)
                                this.owner.spriteAnimator.Stop();
                            yield return null;
                        }
                        int i = 0;
                        foreach (var frame in stumbleClip.frames)
                            frame.invulnerableFrame = wasFrameInvulnerable[i++];
                    }
                }
            #endregion

            yield break;
        }

        public override void FinishDodgeRoll()
        {
            #region Cleanup
                this.owner.OnReceivedDamage -= this.OnReceivedDamage;
                this.reflectingProjectiles = false;
                this.owner.specRigidbody.OnCollision -= BounceOffWalls;
                this.owner.ClearInputOverride("gyro");
                this.owner.SetIsFlying(false, "gyro", false, false);
                this.owner.sprite.renderer.material.shader = this.oldShader;
                this.owner.sprite.usesOverrideMaterial = false;
                this.owner.ToggleHandRenderers(true,"gyrostumble");
                this.owner.ToggleGunRenderers(true,"gyrostumble");
                this.owner.ClearInputOverride("gyrostumble");
                this.owner.m_overrideGunAngle = null;
                this.owner.forceAimPoint = null;
                this.owner.spriteAnimator.Stop();
                this.owner.spriteAnimator.Play(this.owner.spriteAnimator.GetClipByName("idle_front"));

                if (this.isSpeedModifierActive)
                {
                    this.owner.ownerlessStatModifiers.Remove(this.speedModifier);
                    this.isSpeedModifierActive = false;
                    this.owner.stats.RecalculateStats(this.owner);
                }
                // this.owner.ClearInputOverride("gyro");
            #endregion
        }

        private void BounceOffWalls(CollisionData tileCollision)
        {
            float velangle = (-this.targetVelocity).ToAngle();
            float normangle = tileCollision.Normal.ToAngle();
            float newangle = BraveMathCollege.ClampAngle360(velangle + 2f * (normangle - velangle));
            this.targetVelocity = Lazy.AngleToVector(newangle,this.targetVelocity.magnitude);
            this.owner.specRigidbody.Velocity = this.targetVelocity;
        }
    }
}

