namespace CwaffingTheGungy;

public abstract class CustomAmmoDisplay : MonoBehaviour
{
    public abstract bool DoCustomAmmoDisplay(GameUIAmmoController uic);  // must return true if doing a custom ammo display, or false to revert to vanilla behavior

    // must be public
    public static bool DoAmmoOverride(GameUIAmmoController uic, GunInventory guns)
    {
        if (guns == null || !guns.CurrentGun || guns.CurrentGun.GetComponent<CustomAmmoDisplay>() is not CustomAmmoDisplay ammoDisplay)
          return false; // no custom ammo override, so use the vanilla behavior
        if (!ammoDisplay.DoCustomAmmoDisplay(uic))
          return false; // custom ammo override does not want to change vanilla behavior

        // Need to do some vanilla postprocessing to make sure label alignment doesn't get all screwed up
        Gun currentGun = guns.CurrentGun;
        if (currentGun.IsUndertaleGun)
        {
          if (!uic.IsLeftAligned && uic.m_cachedMaxAmmo == int.MaxValue)
            uic.GunAmmoCountLabel.RelativePosition += new Vector3(3f, 0f, 0f);
        }
        else if (currentGun.InfiniteAmmo)
        {
          if (!uic.IsLeftAligned && (!uic.m_cachedGun || !uic.m_cachedGun.InfiniteAmmo))
            uic.GunAmmoCountLabel.RelativePosition += new Vector3(-3f, 0f, 0f);
        }
        else if (currentGun.AdjustedMaxAmmo > 0)
        {
          if (!uic.IsLeftAligned && uic.m_cachedMaxAmmo == int.MaxValue)
            uic.GunAmmoCountLabel.RelativePosition += new Vector3(3f, 0f, 0f);
        }
        else
        {
          if (!uic.IsLeftAligned && uic.m_cachedMaxAmmo == int.MaxValue)
            uic.GunAmmoCountLabel.RelativePosition += new Vector3(3f, 0f, 0f);
        }

        return true; // ammo was overridden, so skip remaining vanilla updates
    }

    [HarmonyPatch(typeof(GameUIAmmoController), nameof(GameUIAmmoController.UpdateUIGun))]
    private class CustomAmmoDisplayPatch
    {
        [HarmonyILManipulator]
        private static void CustomAmmoCountDisplayIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

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
        }
    }
}
