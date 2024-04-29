namespace CwaffingTheGungy;

// Class for harmony patches that need to be shared by multiple classes (e.g., functions that are patched multiple times)


[HarmonyPatch(typeof(MinorBreakable), nameof(MinorBreakable.OnPreCollision))]
static class MinorBreakablePrecollisionPatches
{
    //NOTE: used by Pincushion to prevent projectiles from breaking MinorBreakables
    [HarmonyILManipulator]
    private static void VeryFragileIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchStloc(0)))
            return;

        // Skip past the part where the MinorBreakable actually breaks if we have the VeryFragileProjectile component
        ILLabel projectileIsNotFragileLabel = cursor.DefineLabel();
        cursor.Emit(OpCodes.Ldloc_0);
        cursor.Emit(OpCodes.Call, typeof(Pincushion).GetMethod("BreakFragileProjectiles", BindingFlags.Static | BindingFlags.NonPublic));
        cursor.Emit(OpCodes.Brfalse, projectileIsNotFragileLabel);
        cursor.Emit(OpCodes.Ret);
        cursor.MarkLabel(projectileIsNotFragileLabel);
    }

    //NOTE: used by Scavenging Arms to spawn ammo with a small chance upon colliding with a minor breakable
    [HarmonyILManipulator]
    private static void PlayerCollidesWithMinorBreakableIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<MinorBreakable>("Break")))
            return;

        cursor.Emit(OpCodes.Ldarg, 1); // SpeculativeRigidbody myRigidbody
        cursor.Emit(OpCodes.Ldarg, 3); // SpeculativeRigidbody otherRigidbody
        cursor.Emit(OpCodes.Call, typeof(ScavengingArms).GetMethod("HandleCollisionWithMinorBreakable", BindingFlags.Static | BindingFlags.NonPublic));
    }
}
