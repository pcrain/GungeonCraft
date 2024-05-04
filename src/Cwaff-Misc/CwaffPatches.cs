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

[HarmonyPatch(typeof(Projectile), nameof(Projectile.HandleDamage))]
static class ProjectileHandleDamagePatches
{
    //NOTE: used by DamageAdjuster for adjusting damage based on the enemy a projectile is colliding with
    [HarmonyILManipulator]
    private static void HandleDamageIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.Before,instr => instr.MatchStloc(4)))  // V_4 == damage -> float damage = num; in source
            return;
                                       // float damage is already on stack
        cursor.Emit(OpCodes.Ldarg_0);  // load Projectile this onto stack
        cursor.Emit(OpCodes.Ldarg_1);  // load SpeculativeRigidbody rigidbody onto stack
        cursor.Emit(OpCodes.Call, typeof(DamageAdjuster).GetMethod("AdjustDamageStatic", BindingFlags.Static | BindingFlags.NonPublic));
    }

    //NOTE: used by Armor Piercing Rounds to ignore reflection / invulnerability frames for enemies like Lead Maiden
    [HarmonyILManipulator]
    private static void ArmorPiercingIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        VariableDefinition shouldPierce = il.DeclareLocal<bool>();

        cursor.Emit(OpCodes.Ldarg_0); // load Projectile this onto stack
        cursor.Emit(OpCodes.Ldarg_1); // load SpeculativeRigidbody rigidbody onto stack
        cursor.Emit(OpCodes.Call, typeof(ArmorPiercingRounds).GetMethod("PossiblyDisableArmor", BindingFlags.Static | BindingFlags.NonPublic));
        cursor.Emit(OpCodes.Stloc, shouldPierce);

        // the original method returns early if ReflectProjectiles returns true, so patch that really quickly
        ILLabel preventReflect = null;
        if (!cursor.TryGotoNext(MoveType.After,
          instr => instr.MatchCallvirt<SpeculativeRigidbody>("get_ReflectProjectiles"),
          instr => instr.MatchBrfalse(out preventReflect)
          ))
            return;
        cursor.Emit(OpCodes.Ldloc, shouldPierce);
        cursor.Emit(OpCodes.Brtrue, preventReflect);

        // the original method returns early if QueryInvulnerabilityFrame() returns true, so patch that really quickly
        ILLabel preventInvulnerable = null;
        if (!cursor.TryGotoNext(MoveType.After,
          instr => instr.MatchCallvirt<tk2dSpriteAnimator>("QueryInvulnerabilityFrame"),
          instr => instr.MatchBrfalse(out preventInvulnerable)
          ))
            return;
        cursor.Emit(OpCodes.Ldloc, shouldPierce);
        cursor.Emit(OpCodes.Brtrue, preventInvulnerable);

        // now we need a BUNCH of complicated logic all because ignoreInvulnerabilityFrames is a constant, so we have to rewrite the stack
        if (!cursor.TryGotoNext(MoveType.After,
          instr => instr.MatchStloc(8), // V_8 == damageCategory
          instr => instr.MatchLdarg(2), // hitPixelCollider (parameter)
          instr => instr.MatchStloc(9)  // hitPixelCollider (local)
          ))
            return;

        // we're right before ApplyDamage(), HealthHaver obj is already on the stack
        ILLabel pierced = cursor.DefineLabel();
        ILLabel didNotPierce = cursor.DefineLabel();

        cursor.Emit(OpCodes.Ldloc, shouldPierce);
        cursor.Emit(OpCodes.Brfalse, didNotPierce);

        cursor.Emit(OpCodes.Ldloc_S, (byte)4); // V_4 == damage
        cursor.Emit(OpCodes.Ldloc_S, (byte)5); // V_5 == velocity
        cursor.Emit(OpCodes.Ldloc_S, (byte)6); // V_6 == ownerName
        cursor.Emit(OpCodes.Ldloc_S, (byte)7); // V_7 == coreDamageTypes
        cursor.Emit(OpCodes.Ldc_I4, (int)DamageCategory.Unstoppable); // unstoppable damage
        cursor.Emit(OpCodes.Ldc_I4_1);         // ignoreInvulnerabilityFrames == true
        cursor.Emit(OpCodes.Ldloc_S, (byte)9); // V_9 == hit pixel collider
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldfld, typeof(Projectile).GetField("ignoreDamageCaps", BindingFlags.Instance | BindingFlags.Public));
        cursor.Emit(OpCodes.Callvirt, typeof(HealthHaver).GetMethod("ApplyDamage", BindingFlags.Instance | BindingFlags.Public));
        cursor.Emit(OpCodes.Br, pierced);

        cursor.MarkLabel(didNotPierce);
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<HealthHaver>("ApplyDamage")))
            return;
        cursor.MarkLabel(pierced);

        // ETGModConsole.Log($"  patch applied");
    }
}
