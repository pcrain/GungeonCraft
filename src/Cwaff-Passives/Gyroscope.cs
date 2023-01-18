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
        const float DASH_SPEED  = 50.0f; // Speed of our dash
        const float DASH_TIME   = 0.1f; // Time we spend dashing

        private Vector4 GetCenterPointInScreenUV(Vector2 centerPoint)
        {
            Vector3 vector = GameManager.Instance.MainCameraController.Camera.WorldToViewportPoint(centerPoint.ToVector3ZUp());
            return new Vector4(vector.x, vector.y, 0f, 0f);
        }

        public override IEnumerator ContinueDodgeRoll()
        {
            // string anim = (Mathf.Abs(vel.y) > Mathf.Abs(vel.x)) ? (vel.y > 0 ? "slide_up" : "slide_down") : "slide_right";
            // bool hasAnim = player.spriteAnimator.GetClipByName(anim) != null;

            AkSoundEngine.PostEvent("teledash", this.owner.gameObject);
            // this.owner.SetInputOverride("gyro");
            this.owner.SetIsFlying(true, "gyro");

            DustUpVFX dusts = GameManager.Instance.Dungeon.dungeonDustups;

            BraveInput instanceForPlayer = BraveInput.GetInstanceForPlayer(this.owner.PlayerIDX);

            Shader oldShader = this.owner.sprite.renderer.material.shader;

            // this.owner.sprite.usesOverrideMaterial = true;
            // this.owner.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
            //     this.owner.sprite.renderer.material.SetFloat("_EmissivePower", 300f);
            //     this.owner.sprite.renderer.material.SetFloat("_EmissiveColorPower", 1.55f);
            //     this.owner.sprite.renderer.material.SetColor("_EmissiveColor", Color.magenta);

            // this.owner.sprite.usesOverrideMaterial = true;
            //     this.owner.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/HighPriestAfterImage");
            //     this.owner.sprite.renderer.sharedMaterial.SetFloat("_EmissivePower", 100f);
            //     this.owner.sprite.renderer.sharedMaterial.SetFloat("_Opacity", 0.5f);
            //     this.owner.sprite.renderer.sharedMaterial.SetColor("_DashColor", Color.magenta);

            // this.owner.sprite.usesOverrideMaterial = true;
            //     this.owner.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/DistortionRadius");
            //     this.owner.sprite.renderer.material.SetFloat("_Strength", 0.1f);
            //     this.owner.sprite.renderer.material.SetFloat("_TimePulse", 0.1f);
            //     this.owner.sprite.renderer.material.SetFloat("_RadiusFactor", 0.1f);
            //     this.owner.sprite.renderer.material.SetVector("_WaveCenter", GetCenterPointInScreenUV(this.owner.sprite.WorldCenter));

            // this.owner.sprite.usesOverrideMaterial = true;
            //     this.owner.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/DistortionWave");
            //     this.owner.sprite.renderer.material.SetVector("_WaveCenter", GetCenterPointInScreenUV(this.owner.sprite.WorldCenter));
            //     this.owner.sprite.renderer.material.SetFloat("_DistortProgress", UnityEngine.Random.Range(0.0f,1.0f));

            // /* doesn't work */
            this.owner.sprite.usesOverrideMaterial = true;
                this.owner.sprite.renderer.material.shader = Pixelator.Instance.GetComponent<SENaturalBloomAndDirtyLens>().shader;
                this.owner.sprite.renderer.material.SetFloat("_BlurSize", 10.5f);
                this.owner.sprite.renderer.material.SetFloat("_BloomIntensity", Mathf.Exp(2.0f) - 1f);
                this.owner.sprite.renderer.material.SetFloat("_LensDirtIntensity", Mathf.Exp(2.0f) - 1f);

            // this.owner.sprite.usesOverrideMaterial = true;
            //     this.owner.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/MeduziWaterCaustics");

            // work, but not sure how to use well
                // this.owner.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/GoopShader");
                // this.owner.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/Effects/PixelFog");

            float forcedDirection = this.owner.FacingDirection;
            this.owner.m_overrideGunAngle = forcedDirection;
            while (instanceForPlayer.ActiveActions.DodgeRollAction.IsPressed)
            {
                forcedDirection += 720.0f*BraveTime.DeltaTime; //2 rotations per second
                while (forcedDirection > 180)
                    forcedDirection -= 360;
                while (forcedDirection < -180)
                    forcedDirection += 360;

                this.owner.m_overrideGunAngle = forcedDirection;
                float dir = UnityEngine.Random.Range(0.0f,360.0f);
                // float rot = UnityEngine.Random.Range(0.0f,360.0f);
                float rot = forcedDirection;
                float mag = UnityEngine.Random.Range(0.3f,1.25f);
                SpawnManager.SpawnVFX(
                    dusts.rollLandDustup,
                    this.owner.sprite.WorldCenter + Lazy.AngleToVector(dir, mag),
                    Quaternion.Euler(0f, 0f, rot));
                yield return null;
            }

            Vector2 vel = Lazy.AngleToVector(forcedDirection,DASH_SPEED);
            for (float timer = 0.0f; timer < DASH_TIME; )
            {
                this.owner.PlayerAfterImage();
                timer += BraveTime.DeltaTime;
                this.owner.specRigidbody.Velocity = vel;
                dusts.InstantiateLandDustup(this.owner.sprite.WorldCenter);
                // if (hasAnim && !this.owner.spriteAnimator.IsPlaying(anim))
                //     this.owner.spriteAnimator.Play(anim);  //TODO: the sliding animation itself causes the player to be invincible??? (QueryGroundedFrame())
                yield return null;
                if (this.owner.IsFalling)
                    break;
            }

            this.owner.sprite.renderer.material.shader = oldShader;
            this.owner.sprite.usesOverrideMaterial = false;
            this.owner.spriteAnimator.Stop();
            this.owner.SetIsFlying(false, "gyro");
            // this.owner.ClearInputOverride("gyro");
            this.owner.m_overrideGunAngle = null;
        }
    }
}

