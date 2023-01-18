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
    public interface ICustomDodgeRoll
    {
        public bool dodgeButtonHeld   { get; set; }
        public bool isDashing         { get; set; }
        public PlayerController owner { get; set; }

        public bool canDash      { get; }
        public bool canMultidash { get; }

        public void BeginDodgeRoll();
        public IEnumerator ContinueDodgeRoll();
        public void FinishDodgeRoll();
        public void AbortDodgeRoll();
    }

    public class CustomDodgeRoll : MonoBehaviour, ICustomDodgeRoll
    {
        public bool dodgeButtonHeld   { get; set; }
        public bool isDashing         { get; set; }
        public PlayerController owner { get; set; }

        public bool canDash      => true;
        public bool canMultidash => false;

        public void TryDodgeRoll()
        {
            if (!owner || !canDash || (isDashing && !canMultidash))
                return;
            owner.StartCoroutine(DoDodgeRollWrapper());
        }

        private IEnumerator DoDodgeRollWrapper()
        {
            isDashing = true;
            BeginDodgeRoll();
            IEnumerator script = ContinueDodgeRoll();
            while(isDashing && script.MoveNext())
                yield return script.Current;
            FinishDodgeRoll();
            isDashing = false;
            yield break;
        }

        public virtual void BeginDodgeRoll()
        {
            // any dash setup code should be here
        }

        public virtual void FinishDodgeRoll()
        {
            // any succesful dash cleanup code should be here
        }

        public virtual void AbortDodgeRoll()
        {
            // any aborted dash cleanup code should be here
            isDashing = false;
        }

        public virtual IEnumerator ContinueDodgeRoll()
        {
            // code to execute while dodge rolling should be here
            yield break;
        }

        // handled by MonoBehavior
        private void Update()
        {
            if (!this.owner)
                return;

            BraveInput instanceForPlayer = BraveInput.GetInstanceForPlayer(this.owner.PlayerIDX);
            if (instanceForPlayer.ActiveActions.DodgeRollAction.IsPressed)
            {
                instanceForPlayer.ConsumeButtonDown(GungeonActions.GungeonActionType.DodgeRoll);
                // if (!(this.owner.IsDodgeRolling || this.owner.IsFalling || this.owner.IsInputOverridden || this.dodgeButtonHeld || this.isDashing))
                if (this.owner.AcceptingNonMotionInput && !(this.owner.IsDodgeRolling || this.dodgeButtonHeld || this.isDashing))
                {
                    this.dodgeButtonHeld = true;
                    TryDodgeRoll();
                }
            }
            else
                this.dodgeButtonHeld = false;
        }

        // public override DebrisObject Drop(PlayerController player)
        // {
        //     AbortDash();
        //     this.owner = null;
        //     return base.Drop(player);
        // }

        // public override void OnDestroy()
        // {
        //     AbortDash();
        //     base.OnDestroy();
        // }
    }

    public class ExampleCustomDodgeRoll : CustomDodgeRoll
    {
        public virtual IEnumerator ContinueDash()
        {
            const float DASH_SPEED = 50.0f; // Speed of our dash
            const float DASH_TIME = 0.1f; // Time we spend dashing

            float dashspeed = DASH_SPEED * 1.0f;
            float dashtime = DASH_TIME;

            Vector2 vel = dashspeed * this.owner.m_lastNonzeroCommandedDirection.normalized;
            // string anim = (Mathf.Abs(vel.y) > Mathf.Abs(vel.x)) ? (vel.y > 0 ? "slide_up" : "slide_down") : "slide_right";
            // bool hasAnim = player.spriteAnimator.GetClipByName(anim) != null;

            AkSoundEngine.PostEvent("teledash", this.owner.gameObject);
            this.owner.SetInputOverride("hld");
            this.owner.SetIsFlying(true, "hld");

            DustUpVFX dusts = GameManager.Instance.Dungeon.dungeonDustups;
            for (int i = 0; i < 16; ++i)
            {
                float dir = UnityEngine.Random.Range(0.0f,360.0f);
                float rot = UnityEngine.Random.Range(0.0f,360.0f);
                float mag = UnityEngine.Random.Range(0.3f,1.25f);
                SpawnManager.SpawnVFX(
                    dusts.rollLandDustup,
                    this.owner.sprite.WorldCenter + Lazy.AngleToVector(dir, mag),
                    Quaternion.Euler(0f, 0f, rot));
            }

            bool interrupted = false;
            for (float timer = 0.0f; timer < dashtime; )
            {
                this.owner.PlayerAfterImage();
                timer += BraveTime.DeltaTime;
                this.owner.specRigidbody.Velocity = vel;
                GameManager.Instance.Dungeon.dungeonDustups.InstantiateLandDustup(this.owner.sprite.WorldCenter);
                // if (hasAnim && !this.owner.spriteAnimator.IsPlaying(anim))
                //     this.owner.spriteAnimator.Play(anim);  //TODO: the sliding animation itself causes the player to be invincible??? (QueryGroundedFrame())
                yield return null;
                if (this.owner.IsFalling)
                {
                    interrupted = true;
                    break;
                }
            }
            if (!interrupted)
            {
                this.owner.PlayerAfterImage();
                for (int i = 0; i < 8; ++i)
                {
                    float dir = UnityEngine.Random.Range(0.0f,360.0f);
                    float rot = UnityEngine.Random.Range(0.0f,360.0f);
                    float mag = UnityEngine.Random.Range(0.3f,1.0f);
                    SpawnManager.SpawnVFX(
                        dusts.rollLandDustup,
                        this.owner.sprite.WorldCenter + Lazy.AngleToVector(dir, mag),
                        Quaternion.Euler(0f, 0f, rot));
                }
            }
            this.owner.spriteAnimator.Stop();
            this.owner.SetIsFlying(false, "hld");
            this.owner.ClearInputOverride("hld");
        }
    }
}

