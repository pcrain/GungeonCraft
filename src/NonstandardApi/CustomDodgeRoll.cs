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

        private void TryDodgeRoll()
        {
            if (!owner || !canDash || (isDashing && !canMultidash))
                return;
            owner.StartCoroutine(DoDodgeRollWrapper());
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
}

