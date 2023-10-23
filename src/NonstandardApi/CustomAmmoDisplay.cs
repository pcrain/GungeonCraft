using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using Mono.Cecil.Cil; //Instruction

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace CwaffingTheGungy
{
    public abstract class CustomAmmoDisplay : MonoBehaviour
    {
        private static ILHook _AimuUIAmmoILHook;

        public static void Init()
        {
            _AimuUIAmmoILHook = new ILHook(
                typeof(GameUIAmmoController).GetMethod("UpdateUIGun", BindingFlags.Instance | BindingFlags.Public),
                CustomAmmoCountDisplayIL
                );
        }

        public abstract bool DoCustomAmmoDisplay(GameUIAmmoController uic);  // must return true if doing a custom ammo display, or false to revert to vanilla behavior

        // must be public
        public static bool DoAmmoOverride(GameUIAmmoController uic, GunInventory guns)
        {
            // returns true if we override vanilla behavior, or false if not
            return guns?.CurrentGun?.GetComponent<CustomAmmoDisplay>()?.DoCustomAmmoDisplay(uic) ?? false;
        }

        private static void CustomAmmoCountDisplayIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            // cursor.DumpILOnce("CustomAmmoCountDisplayIL");

            if (!cursor.TryGotoNext(MoveType.Before,
              instr => instr.MatchLdarg(0),
              instr => instr.MatchLdloc(2),
              instr => instr.MatchCall<GameUIAmmoController>("CleanupLists")))
                return; // failed to find what we need

            ILLabel skipPoint = cursor.MarkLabel(); // mark our own label right before the call to CleanupLists(), which is at the end of the conditional we want to skip
            cursor.Index = 0;

            if (!cursor.TryGotoNext(MoveType.Before,
              instr => instr.MatchLdarg(0),
              instr => instr.MatchLdfld<GameUIAmmoController>("m_cachedTotalAmmo")))
                return; // failed to find what we need

            ++cursor.Index; // trying to emit code directly here won't work because a bfalse can skip over us, so we have to move forward once
            cursor.Emit(OpCodes.Ldarg_1); // 1st parameter is GunInventory
            cursor.Emit(OpCodes.Call, typeof(CustomAmmoDisplay).GetMethod("DoAmmoOverride")); // replace with our own custom hook
            cursor.Emit(OpCodes.Brtrue, skipPoint); // skip over all the logic for doing normal updates to the ammo counter
            cursor.Emit(OpCodes.Ldarg_0); // replace 0th parameter -> GameUIAmmoController
            // cursor.Emit(OpCodes.Pop);
        }
    }
}
