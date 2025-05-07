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
        cursor.CallPrivate(typeof(VeryFragileProjectile), nameof(VeryFragileProjectile.IsVeryFragile));
        cursor.Emit(OpCodes.Brfalse, projectileIsNotFragileLabel);
        cursor.Emit(OpCodes.Ret);
        cursor.MarkLabel(projectileIsNotFragileLabel);
    }

    //NOTE: used by Scavenging Arms to spawn ammo with a small chance upon colliding with a minor breakable
    [HarmonyILManipulator]
    private static void PlayerCollidesWithMinorBreakableIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<MinorBreakable>(nameof(MinorBreakable.Break))))
            return;

        cursor.Emit(OpCodes.Ldarg, 1); // SpeculativeRigidbody myRigidbody
        cursor.Emit(OpCodes.Ldarg, 3); // SpeculativeRigidbody otherRigidbody
        cursor.CallPrivate(typeof(ScavengingArms), nameof(ScavengingArms.HandleCollisionWithMinorBreakable));
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
        cursor.CallPrivate(typeof(DamageAdjuster), nameof(DamageAdjuster.AdjustDamageStatic));
    }

    //NOTE: used by Armor Piercing Rounds to ignore reflection / invulnerability frames for enemies like Lead Maiden
    [HarmonyILManipulator]
    private static void ArmorPiercingIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        // PossiblyDisableArmor() is expensive and has side effects, so cache the result in our own local
        VariableDefinition shouldPierce = il.DeclareLocal<bool>();
        cursor.Emit(OpCodes.Ldarg_0); // load Projectile this onto stack
        cursor.Emit(OpCodes.Ldarg_1); // load SpeculativeRigidbody rigidbody onto stack
        cursor.CallPrivate(typeof(ArmorPiercingRounds), nameof(ArmorPiercingRounds.PossiblyDisableArmor));
        cursor.Emit(OpCodes.Stloc, shouldPierce);

        // the original method returns early if ReflectProjectiles returns true, so patch that really quickly
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<SpeculativeRigidbody>("get_ReflectProjectiles")))
            return;
        cursor.Emit(OpCodes.Ldloc, shouldPierce);
        cursor.CallPrivate(typeof(ProjectileHandleDamagePatches), nameof(AndNot));

        // the original method returns early if QueryInvulnerabilityFrame() returns true, so patch that really quickly
        if (!cursor.TryGotoNext(MoveType.After, instr =>
          instr.MatchCallvirt<tk2dSpriteAnimator>(nameof(tk2dSpriteAnimator.QueryInvulnerabilityFrame))))
            return;
        cursor.Emit(OpCodes.Ldloc, shouldPierce);
        cursor.CallPrivate(typeof(ProjectileHandleDamagePatches), nameof(AndNot));

        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchStloc(9))) // hitPixelCollider (local)
            return;

        // we're right before ApplyDamage(), HealthHaver obj is already on the stack
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(0)))
            return; // match the load for the hardcoded "false" for ignoreInvulnerabilityFrames
        cursor.Emit(OpCodes.Ldloc, shouldPierce);
        cursor.CallPrivate(typeof(ProjectileHandleDamagePatches), nameof(ShouldIgnoreInvulnerabilityFrames));
    }

    private static bool AndNot(bool trueVal, bool falseVal) => trueVal && !falseVal;
    private static bool ShouldIgnoreInvulnerabilityFrames(bool _, bool ignore) => ignore;
}

[HarmonyPatch(typeof(Projectile), nameof(Projectile.OnRigidbodyCollision))]
static class ProjectileOnRigidbodyCollisionPatches
{
    //NOTE: used by CwaffProjectile to allow playing enemy / object impact sounds without the Play_WPN_..._impact_01 format
    [HarmonyILManipulator]
    private static void ImpactSoundIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Projectile>(nameof(Projectile.HandleKnockback))))
            return;
        if (!cursor.TryGotoNext(MoveType.Before, instr => instr.MatchLdloc(5))) // v_5 == flag == whether we hit an enemy (true) or other object (false)
            return;

        cursor.Emit(OpCodes.Ldarg_0); // this Projectile
        cursor.Emit(OpCodes.Ldloc_S, (byte)5); // v_5 == flag == whether we hit an enemy (true) or other object (false)
        cursor.CallPrivate(typeof(CwaffProjectile), nameof(CwaffProjectile.PlayCollisionSounds));
    }
}

[HarmonyPatch(typeof(Projectile), nameof(Projectile.OnTileCollision))]
static class ProjectileOnTileCollisionPatches
{
    //NOTE: used by CwaffProjectile to allow playing enemy / object impact sounds without the Play_WPN_..._impact_01 format
    static void Prefix(Projectile __instance)
    {
        CwaffProjectile.PlayCollisionSounds(__instance, false);
    }
}

[HarmonyPatch(typeof(Gun), nameof(Gun.ShootSingleProjectile))]
static class ShootSingleProjectilePatch
{ //REFACTOR: the accuracy modifiers can all be consolidated
    // NOTE: used by MMAiming to increase reload speed while standing still
    [HarmonyILManipulator]
    private static void ReduceSpreadWhenIdleIL(ILContext il, MethodBase original)
    {
        ILCursor cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchLdcI4(2),
            instr => instr.MatchCallvirt<PlayerStats>(nameof(PlayerStats.GetStatValue))))
            return;

        cursor.Emit(OpCodes.Ldloc_0);  // load PlayerController type
        cursor.CallPrivate(typeof(MMAiming), nameof(MMAiming.ModifySpreadIfIdle));
    }

    // NOTE: used by Bionic Finger synergy to reduce spread while firing semiautomatic weapons
    [HarmonyILManipulator]
    private static void AimBotZeroSpreadIL(ILContext il, MethodBase original)
    {
        ILCursor cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchLdcI4(2),
            instr => instr.MatchCallvirt<PlayerStats>(nameof(PlayerStats.GetStatValue))))
            return;

        cursor.Emit(OpCodes.Ldloc_0);  // load PlayerController type
        cursor.CallPrivate(typeof(BionicFinger), nameof(BionicFinger.ModifySpreadIfSemiautomatic));
    }

    // NOTE: used by CwaffGun to dynamically adjust spread
    [HarmonyILManipulator]
    private static void DynamicAccuracyIL(ILContext il, MethodBase original)
    {
        ILCursor cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchLdcI4(2),
            instr => instr.MatchCallvirt<PlayerStats>(nameof(PlayerStats.GetStatValue))))
            return;

        cursor.Emit(OpCodes.Ldarg_0);  // load Gun type
        cursor.CallPrivate(typeof(CwaffGun), nameof(CwaffGun.ModifyAccuracy));
    }

    // NOTE: used by CwaffProjectile to determine if a projectile was fired for free
    [HarmonyILManipulator]
    private static void CheckFreebieIL(ILContext il, MethodBase original)
    {
        ILCursor cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchCall<Gun>(nameof(Gun.ApplyCustomAmmunitionsToProjectile))))
            return;

        cursor.Emit(OpCodes.Ldloc_S, (byte)10);  // V_10 == our projectile
        cursor.Emit(OpCodes.Ldarg_0);  // load Gun
        cursor.Emit(OpCodes.Ldarg_1);  // load ProjectileModule
        cursor.CallPrivate(typeof(CwaffProjectile), nameof(CwaffProjectile.DetermineIfFiredForFree));
    }
}

[HarmonyPatch(typeof(PlayerItem), nameof(PlayerItem.DidDamage))]
static class PlayerItemDidDamagePatch
{
    // NOTE: used by Alligator + Stuffed Star synergy to increase cooldown rate
    public static void Prefix(PlayerItem __instance, PlayerController Owner, ref float damageDone)
    {
        if (!Owner)
            return;
        if (Owner.HasSynergy(Synergy.MR_ALLIGATOX))
            damageDone *= 2f;
    }
}

// protected void ApplyCooldown(PlayerController user)
[HarmonyPatch(typeof(PlayerItem), nameof(PlayerItem.ApplyCooldown))]
static class PlayerItemApplyCooldownPatch
{
    static void Postfix(PlayerItem __instance, PlayerController user)
    {
        if (__instance is GrapplingHookItem && user.HasSynergy(Synergy.BIONIC_COMMANDO))
            __instance.ClearCooldowns();
    }
}

// NOTE: used by BubbbleWand and BlasTech F-4 mastery
[HarmonyPatch(typeof(AIActor), nameof(AIActor.Start))]
static class ReplaceEnemyGunsPatch
{
    private static Items _BlasTechID = (Items)(-1);

    private static void Prefix(AIActor __instance)
    {
        if (DoBlastechReplacement(__instance))
            return;
        if (DoBubbleWandReplacement(__instance))
            return;
    }

    private static bool DoBlastechReplacement(AIActor enemy)
    {
        if (!enemy.aiShooter)
            return false;
        if (!Lazy.AnyoneHasSynergy(Synergy.MASTERY_BLASTECH_F4))
            return false;
        if ((int)_BlasTechID < 0)
            _BlasTechID = (Items)Lazy.PickupId<BlasTechF4>();
        enemy.ReplaceGun(_BlasTechID);
        return true;
    }

    private static bool DoBubbleWandReplacement(AIActor enemy)
    {
        if (!Lazy.AnyoneHas<BubbleWand>())
            return false;
        if (!enemy.aiShooter || enemy.healthHaver is not HealthHaver hh || hh.IsBoss || hh.IsSubboss)
            return false;
        if (!Lazy.CoinFlip() && (!Lazy.CoinFlip() || !Lazy.AnyoneHasSynergy(Synergy.DUBBLE_BUBBLE)))
            return false;

        enemy.ReplaceGun(Items.BubbleBlaster);
        enemy.aiShooter.PostProcessProjectile += BubbleWand.PostProcessProjectile;

        return true;
    }
}

//NOTE: used by Adrenaline Shot, Sunderbuss, and Macheening
[HarmonyPatch]
static class HeartUIPatch
{
    private static int _AdrenalineShotId = -1;
    private static int _LichguardId      = -1;
    private static int _SunderbussId     = -1;
    private static int _MacheeningId     = -1;
    [HarmonyPatch(typeof(GameUIHeartController), nameof(GameUIHeartController.ProcessHeartSpriteModifications))]
    private static void Postfix(GameUIHeartController __instance, PlayerController associatedPlayer)
    {
        if (_AdrenalineShotId == -1)
        {
            _AdrenalineShotId = Lazy.PickupId<AdrenalineShot>();
            _LichguardId      = Lazy.PickupId<Lichguard>();
            _SunderbussId     = Lazy.PickupId<Sunderbuss>();
            _MacheeningId     = Lazy.PickupId<Macheening>();
        }
        if (associatedPlayer.GetPassive(_AdrenalineShotId) is AdrenalineShot shot && shot._adrenalineActive)
        {
            __instance.m_currentFullHeartName  = "adrenaline_heart_full_ui";
            __instance.m_currentHalfHeartName  = "adrenaline_heart_half_ui";
            __instance.m_currentEmptyHeartName = "adrenaline_heart_empty_ui";
        }
        else if (associatedPlayer.CurrentGun is Gun gun && (gun.PickupObjectId == _SunderbussId || gun.PickupObjectId == _MacheeningId) && !associatedPlayer.HasPassive(_LichguardId))
        {
            __instance.m_currentFullHeartName  = "lichguard_heart_full_ui";
            __instance.m_currentHalfHeartName  = "lichguard_heart_half_ui";
            // __instance.m_currentEmptyHeartName = "lichguard_heart_empty_ui";
        }
    }
}
