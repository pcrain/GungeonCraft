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
        const float DASH_SPEED  = 20.0f; // Speed of our dash
        const float DASH_TIME   = 4.0f; // Time we spend dashing

        private Vector2 targetVelocity = Vector2.zero;

        private Vector4 GetCenterPointInScreenUV(Vector2 centerPoint)
        {
            Vector3 vector = GameManager.Instance.MainCameraController.Camera.WorldToViewportPoint(centerPoint.ToVector3ZUp());
            return new Vector4(vector.x, vector.y, 0f, 0f);
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
            #endregion

            // this.owner.sprite.usesOverrideMaterial = true;
            //     this.owner.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/HighPriestAfterImage");
            //     this.owner.sprite.renderer.sharedMaterial.SetFloat("_EmissivePower", 100f);
            //     this.owner.sprite.renderer.sharedMaterial.SetFloat("_Opacity", 0.5f);
            //     this.owner.sprite.renderer.sharedMaterial.SetColor("_DashColor", Color.magenta);

            #region The Charge
                float totalTime = 0.0f;
                float forcedDirection = this.owner.FacingDirection;
                this.owner.m_overrideGunAngle = forcedDirection;
                while (instanceForPlayer.ActiveActions.DodgeRollAction.IsPressed)
                {
                    totalTime += BraveTime.DeltaTime;
                    Exploder.DoDistortionWave(this.owner.sprite.WorldCenter, 1.8f, 0.01f, 0.5f, 0.1f);
                    forcedDirection += 720.0f*BraveTime.DeltaTime; //2 rotations per second
                    while (forcedDirection > 180)
                    {
                        AkSoundEngine.PostEvent("teledash", this.owner.gameObject);
                        forcedDirection -= 360;
                    }

                    this.owner.m_overrideGunAngle = forcedDirection;
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
                this.targetVelocity = Lazy.AngleToVector(forcedDirection,DASH_SPEED);
                for (float timer = 0.0f; timer < DASH_TIME; )
                {
                    if (this.owner.IsFalling)
                        break;
                    timer += BraveTime.DeltaTime;
                    this.owner.PlayerAfterImage();
                    this.owner.specRigidbody.Velocity = this.targetVelocity;
                    this.owner.specRigidbody.Reinitialize();
                    dusts.InstantiateLandDustup(this.owner.sprite.WorldCenter);
                    // if (hasAnim && !this.owner.spriteAnimator.IsPlaying(anim))
                    //     this.owner.spriteAnimator.Play(anim);  //TODO: the sliding animation itself causes the player to be invincible??? (QueryGroundedFrame())
                    yield return null;
                }
                this.owner.specRigidbody.OnCollision -= BounceOffWalls;
            #endregion

            #region The Stumble
            #endregion

            #region Cleanup
                this.owner.sprite.renderer.material.shader = oldShader;
                this.owner.sprite.usesOverrideMaterial = false;
                this.owner.spriteAnimator.Stop();
                this.owner.SetIsFlying(false, "gyro");
                // this.owner.ClearInputOverride("gyro");
                this.owner.m_overrideGunAngle = null;
            #endregion

            yield break;
        }

        private void BounceOffWalls(CollisionData tileCollision)
        {
            float velangle = (-this.targetVelocity).ToAngle();
            float normangle = tileCollision.Normal.ToAngle();
            float newangle = BraveMathCollege.ClampAngle360(velangle + 2f * (normangle - velangle));
            this.targetVelocity = Lazy.AngleToVector(newangle,DASH_SPEED);
            this.owner.specRigidbody.Velocity = this.targetVelocity;
        }
    }
}

