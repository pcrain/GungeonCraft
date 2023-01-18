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

        private static Hook customDodgeRollHook = null;

        public static void InitCustomDodgeRollHooks()
        {
            customDodgeRollHook = new Hook(
                typeof(PlayerController).GetMethod("HandleStartDodgeRoll", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(CustomDodgeRoll).GetMethod("CustomDodgeRollHook"));
        }

        public static bool CustomDodgeRollHook(Func<PlayerController,Vector2,bool> orig, PlayerController player, Vector2 direction)
        {
            CustomDodgeRoll overrideDodgeRoll = null;
            foreach (PassiveItem p in player.passiveItems)
            {
                overrideDodgeRoll = p.GetComponent<CustomDodgeRoll>();
                if (overrideDodgeRoll)
                    break;
            }
            if (!overrideDodgeRoll)  // fall back to default behavior if we don't have overrides
                return orig(player,direction);

            BraveInput instanceForPlayer = BraveInput.GetInstanceForPlayer(player.PlayerIDX);
            if (instanceForPlayer.ActiveActions.DodgeRollAction.IsPressed)
            {
                instanceForPlayer.ConsumeButtonDown(GungeonActions.GungeonActionType.DodgeRoll);
                // if (!(this.owner.IsDodgeRolling || this.owner.IsFalling || this.owner.IsInputOverridden || this.dodgeButtonHeld || this.isDashing))
                // if (player.AcceptingNonMotionInput && !(player.IsDodgeRolling || this.dodgeButtonHeld || this.isDashing))
                if (player.AcceptingNonMotionInput && !(player.IsDodgeRolling || overrideDodgeRoll.dodgeButtonHeld))
                {
                    overrideDodgeRoll.dodgeButtonHeld = true;
                    return overrideDodgeRoll.TryDodgeRoll();
                }
            }
            else
                overrideDodgeRoll.dodgeButtonHeld = false;
            return false;
        }

        // handled by MonoBehavior
        private void Update()
        {
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

        private bool TryDodgeRoll()
        {
            if (!owner || !canDash || (isDashing && !canMultidash))
                return false;
            owner.StartCoroutine(DoDodgeRollWrapper());
            return true;
        }
    }
}

