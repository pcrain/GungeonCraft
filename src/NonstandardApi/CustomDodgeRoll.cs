using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;

namespace CwaffingTheGungy
{
    public interface ICustomDodgeRoll
    {
        public bool dodgeButtonHeld   { get; set; }
        public bool isDodging         { get; set; }
        public PlayerController owner { get; set; }

        public bool canDodge      { get; }  // if false, disables a CustomDodgeRoll from activating
        public bool canMultidodge { get; }  // if true, enables dodging while already mid-dodge

        public void BeginDodgeRoll();  // called once before a dodge roll begins
        public IEnumerator ContinueDodgeRoll();  // called every frame until dodge roll ends
        public void FinishDodgeRoll(); // called once after a dodge roll ends
        public void AbortDodgeRoll(); // called if the dodge roll is interrupted prematurely
    }

    public class CustomDodgeRoll : MonoBehaviour, ICustomDodgeRoll
    {
        public bool dodgeButtonHeld   { get; set; }
        public bool isDodging         { get; set; }
        public PlayerController owner { get; set; }

        public virtual bool canDodge      => true;
        public virtual bool canMultidodge => false;

        private static Hook customDodgeRollHook = null;

        public static void InitCustomDodgeRollHooks()
        {
            customDodgeRollHook = new Hook(
                typeof(PlayerController).GetMethod("HandleStartDodgeRoll", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(CustomDodgeRoll).GetMethod("CustomDodgeRollHook"));
        }

        public static bool CustomDodgeRollHook(Func<PlayerController,Vector2,bool> orig, PlayerController player, Vector2 direction)
        {
            List<CustomDodgeRoll> overrides = new List<CustomDodgeRoll>();
            foreach (PassiveItem p in player.passiveItems)
            {
                CustomDodgeRoll overrideDodgeRoll = p.GetComponent<CustomDodgeRoll>();
                if (overrideDodgeRoll)
                    overrides.Add(overrideDodgeRoll);
            }
            if (overrides.Count == 0)  // fall back to default behavior if we don't have overrides
                return orig(player,direction);

            BraveInput instanceForPlayer = BraveInput.GetInstanceForPlayer(player.PlayerIDX);
            if (instanceForPlayer.ActiveActions.DodgeRollAction.IsPressed)
            {
                instanceForPlayer.ConsumeButtonDown(GungeonActions.GungeonActionType.DodgeRoll);
                // if (!(this.owner.IsDodgeRolling || this.owner.IsFalling || this.owner.IsInputOverridden || this.dodgeButtonHeld || this.isDodging))
                // if (player.AcceptingNonMotionInput && !(player.IsDodgeRolling || this.dodgeButtonHeld || this.isDodging))
                if (player.AcceptingNonMotionInput && !player.IsDodgeRolling)
                {
                    foreach (CustomDodgeRoll customDodgeRoll in overrides)
                    {
                        if (customDodgeRoll.dodgeButtonHeld)
                            continue;
                        customDodgeRoll.dodgeButtonHeld = true;
                        customDodgeRoll.TryDodgeRoll();
                    }
                    return true;
                }
            }
            else
            {
                foreach (CustomDodgeRoll customDodgeRoll in overrides)
                    customDodgeRoll.dodgeButtonHeld = false;
            }
            return false;
        }

        public virtual void BeginDodgeRoll()
        {
            // any dodge setup code should be here
        }

        public virtual void FinishDodgeRoll()
        {
            // any succesful dodge cleanup code should be here
        }

        public virtual void AbortDodgeRoll()
        {
            // any aborted dodge cleanup code should be here
            isDodging = false;
        }

        public virtual IEnumerator ContinueDodgeRoll()
        {
            // code to execute while dodge rolling should be here
            yield break;
        }

        private IEnumerator DoDodgeRollWrapper()
        {
            isDodging = true;
            BeginDodgeRoll();
            IEnumerator script = ContinueDodgeRoll();
            while(isDodging && script.MoveNext())
                yield return script.Current;
            FinishDodgeRoll();
            isDodging = false;
            yield break;
        }

        private bool TryDodgeRoll()
        {
            if (!owner || !canDodge || (isDodging && !canMultidodge))
                return false;
            owner.StartCoroutine(DoDodgeRollWrapper());
            return true;
        }
    }
}

