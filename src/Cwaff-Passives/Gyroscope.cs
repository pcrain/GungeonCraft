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
            if(!dodgeRoller.isDodging)  // reflect projectiles with hyped synergy
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
        const float DASH_SPEED    = 20.0f;  // Speed of our dash
        const float DASH_TIME     = 1.0f;   // Time we spend dashing
        const float MAX_ROT       = 180.0f; // Max rotation per second
        const float MAX_DRIFT     = 40.0f;  // Max drift per second
        const float GYRO_FRICTION = 0.99f;  // Friction coefficient
        const float STUMBLE_TIME  = 1.0f;  // Amount of time we stumble for after spinning

        private bool useDriftMechanics = true;
        private Vector2 targetVelocity = Vector2.zero;
        private float forcedDirection = 0.0f;

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
                AkSoundEngine.PostEvent("teledash", this.owner.gameObject);
                this.forcedDirection -= 360;
            }

            string animName = GetBaseIdleAnimationName(this.owner,this.forcedDirection);
            if (!this.owner.spriteAnimator.IsPlaying(animName))
                this.owner.spriteAnimator.Play(animName);
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
        }

        public override IEnumerator ContinueDodgeRoll()
        {
            #region Initialization
                // string anim = (Mathf.Abs(vel.y) > Mathf.Abs(vel.x)) ? (vel.y > 0 ? "slide_up" : "slide_down") : "slide_right";
                // bool hasAnim = player.spriteAnimator.GetClipByName(anim) != null;
                // this.owner.SetInputOverride("gyro");
                this.owner.SetIsFlying(true, "gyro");
                DustUpVFX dusts = GameManager.Instance.Dungeon.dungeonDustups;
                BraveInput instanceForPlayer = BraveInput.GetInstanceForPlayer(this.owner.PlayerIDX);
            #endregion

            #region Shader Setup
                Shader oldShader = this.owner.sprite.renderer.material.shader;
                this.owner.sprite.usesOverrideMaterial = true;
                this.owner.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
                    this.owner.sprite.renderer.material.SetFloat("_EmissivePower", 1.55f);
                    this.owner.sprite.renderer.material.SetFloat("_EmissiveColorPower", 1.55f);
                    this.owner.sprite.renderer.material.SetColor("_EmissiveColor", Color.magenta);
                // this.owner.sprite.usesOverrideMaterial = true;
                //     this.owner.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/HighPriestAfterImage");
                //     this.owner.sprite.renderer.sharedMaterial.SetFloat("_EmissivePower", 100f);
                //     this.owner.sprite.renderer.sharedMaterial.SetFloat("_Opacity", 0.5f);
                //     this.owner.sprite.renderer.sharedMaterial.SetColor("_DashColor", Color.magenta);
            #endregion

            #region The Charge
                float totalTime = 0.0f;
                forcedDirection = this.owner.FacingDirection;
                this.owner.m_overrideGunAngle = forcedDirection;
                Vector3 chargeStartPosition = this.owner.transform.position;
                while (instanceForPlayer.ActiveActions.DodgeRollAction.IsPressed)
                {
                    totalTime += BraveTime.DeltaTime;
                    Exploder.DoDistortionWave(this.owner.sprite.WorldCenter, 1.8f, 0.01f, 0.5f, 0.1f);
                    UpdateForcedDirection(this.forcedDirection+720.0f*BraveTime.DeltaTime);  //2.0 RPS
                    this.owner.transform.position = chargeStartPosition;
                    this.owner.specRigidbody.Reinitialize();

                    float dir = UnityEngine.Random.Range(0.0f,360.0f);
                    float rot = forcedDirection;
                    float mag = UnityEngine.Random.Range(0.3f,1.25f);
                    SpawnManager.SpawnVFX(
                        dusts.rollLandDustup,
                        this.owner.sprite.WorldCenter + Lazy.AngleToVector(dir, mag),
                        Quaternion.Euler(0f, 0f, rot));
                    yield return null;
                }
            #endregion

            #region The Dash
                this.owner.specRigidbody.OnCollision += BounceOffWalls;
                this.targetVelocity = Lazy.AngleToVector(this.owner.FacingDirection,DASH_SPEED);
                for (float timer = 0.0f; timer < DASH_TIME; timer += BraveTime.DeltaTime)
                {
                    if (this.owner.IsFalling)
                        break;
                    // this.owner.PlayerAfterImage();
                    Exploder.DoDistortionWave(this.owner.sprite.WorldCenter, 1.8f, 0.01f, 0.5f, 0.1f);
                    UpdateForcedDirection(this.forcedDirection+720.0f*BraveTime.DeltaTime);  //2.0 RPS

                    // adjust angle / velocity of spin if necessary
                    if (this.useDriftMechanics)
                    {
                        float maxDrift = MAX_DRIFT * BraveTime.DeltaTime;
                        Vector2 drift = maxDrift*instanceForPlayer.ActiveActions.Move.Value;
                        this.targetVelocity += drift;
                        if (this.targetVelocity.magnitude > DASH_SPEED)
                            this.targetVelocity = DASH_SPEED * this.targetVelocity.normalized;
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
                        this.targetVelocity = Lazy.AngleToVector(newangle,DASH_SPEED);
                    }
                    this.targetVelocity *= GYRO_FRICTION;
                    this.owner.specRigidbody.Velocity = this.targetVelocity;

                    dusts.InstantiateLandDustup(this.owner.sprite.WorldCenter);
                    // if (hasAnim && !this.owner.spriteAnimator.IsPlaying(anim))
                    //     this.owner.spriteAnimator.Play(anim);  //TODO: the sliding animation itself causes the player to be invincible??? (QueryGroundedFrame())
                    yield return null;
                }
                this.owner.specRigidbody.OnCollision -= BounceOffWalls;
            #endregion

            #region The Stumble
                // Dissect.PrintSpriteCollectionNames(this.owner.sprite.collection);
                string tumbleAnim = GetBaseDodgeAnimationName(this.owner,this.owner.specRigidbody.Velocity);
                this.owner.specRigidbody.Velocity = Vector2.zero;

                this.owner.SetIsFlying(false, "gyro");
                this.owner.sprite.renderer.material.shader = oldShader;
                this.owner.sprite.usesOverrideMaterial = false;

                this.owner.SetInputOverride("gyrostumble");
                this.owner.ToggleGunRenderers(false,"gyrostumble");
                this.owner.ToggleHandRenderers(false,"gyrostumble");
                tk2dSpriteAnimationClip stumbleAnim = (
                    (this.owner.spriteAnimator.GetClipByName("chest_recover") == null)
                    ? this.owner.spriteAnimator.GetClipByName((!this.owner.UseArmorlessAnim) ? "pitfall_return" : "pitfall_return_armorless")
                    : this.owner.spriteAnimator.GetClipByName((!this.owner.UseArmorlessAnim) ? "chest_recover" : "chest_recover_armorless"));

                this.owner.spriteAnimator.Stop();
                this.owner.QueueSpecificAnimation(this.owner.spriteAnimator.GetClipByName("spinfall"/*"timefall"*/).name);
                this.owner.spriteAnimator.SetFrame(0, false);
                AkSoundEngine.PostEvent("Play_Fall", this.owner.gameObject);
                // while (this.owner.spriteAnimator.IsPlaying("spinfall"))
                for (float timer = 0.0f; timer < 0.65f; timer += BraveTime.DeltaTime)
                    yield return null;

                this.owner.spriteAnimator.Stop();
                this.owner.QueueSpecificAnimation(this.owner.spriteAnimator.GetClipByName(tumbleAnim).name);
                this.owner.spriteAnimator.SetFrame(0, false);
                this.owner.spriteAnimator.ClipFps = 24.0f;
                for (float timer = 0.0f; timer < STUMBLE_TIME; timer += BraveTime.DeltaTime)
                {
                    this.owner.specRigidbody.Velocity = Vector2.zero;
                    if (this.owner.spriteAnimator.CurrentFrame > 3)
                        this.owner.spriteAnimator.Stop();
                    yield return null;
                }
                this.owner.ToggleHandRenderers(true,"gyrostumble");
                this.owner.ToggleGunRenderers(true,"gyrostumble");
                this.owner.ClearInputOverride("gyrostumble");
            #endregion

            #region Cleanup
                this.owner.m_overrideGunAngle = null;
                this.owner.spriteAnimator.Stop();
                this.owner.spriteAnimator.Play(this.owner.spriteAnimator.GetClipByName("idle_front"));
                // this.owner.ClearInputOverride("gyro");
            #endregion

            yield break;
        }


        protected virtual string GetBaseIdleAnimationName(PlayerController p, float gunAngle)
        {
            string anim = string.Empty;
            bool hasgun = p.CurrentGun != null;
            bool invertThresholds = false;
            if (GameManager.Instance.CurrentLevelOverrideState == GameManager.LevelOverrideState.END_TIMES)
            {
                hasgun = false;
            }
            float num = 155f;
            float num2 = 25f;
            if (invertThresholds)
            {
                num = -155f;
                num2 = -25f;
            }
            float num3 = 120f;
            float num4 = 60f;
            float num5 = -60f;
            float num6 = -120f;
            bool flag2 = gunAngle <= num && gunAngle >= num2;
            if (invertThresholds)
                flag2 = gunAngle <= num || gunAngle >= num2;
            if (flag2)
            {
                if (gunAngle < num3 && gunAngle >= num4)
                    anim = (((!hasgun) && !p.ForceHandless) ? "_backward_twohands" : ((!p.RenderBodyHand) ? "_backward" : "_backward_hand"));
                else
                    anim = ((hasgun || p.ForceHandless) ? "_bw" : "_bw_twohands");
            }
            else if (gunAngle <= num5 && gunAngle >= num6)
                anim = (((!hasgun) && !p.ForceHandless) ? "_forward_twohands" : ((!p.RenderBodyHand) ? "_forward" : "_forward_hand"));
            else
                anim = (((!hasgun) && !p.ForceHandless) ? "_twohands" : ((!p.RenderBodyHand) ? "" : "_hand"));
            if (p.UseArmorlessAnim)
                anim += "_armorless";
            return anim;
        }

        protected virtual string GetBaseDodgeAnimationName(PlayerController p, Vector2 vector)
        {
            return ((!(Mathf.Abs(vector.x) < 0.1f)) ? (((!(vector.y > 0.1f)) ? "dodge_left" : "dodge_left_bw") + ((!p.UseArmorlessAnim) ? string.Empty : "_armorless")) : (((!(vector.y > 0.1f)) ? "dodge" : "dodge_bw") + ((!p.UseArmorlessAnim) ? string.Empty : "_armorless")));
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

