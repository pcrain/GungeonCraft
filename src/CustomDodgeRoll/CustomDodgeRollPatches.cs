namespace CwaffingTheGungy;

[HarmonyPatch]
internal static class CustomDodgeRollPatches
{
    private class CustomDodgeRollData
    {
        internal List<CustomDodgeRoll> _Overrides = new();
        internal CustomDodgeRoll _CurrentRoll = null;
        internal bool _BloodiedScarfActive = false;
    }

    private static readonly CustomDodgeRollData[] _CustomDodgeRollData = [new(), new()];

    private static readonly EventInfo OnPreDodgeRollEvent = typeof(PlayerController).GetEvent(
        nameof(PlayerController.OnPreDodgeRoll), BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public);

    private static readonly FieldInfo OnPreDodgeRollField = typeof(PlayerController).GetField(
        nameof(PlayerController.OnPreDodgeRoll), BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.NonPublic);

    private static void InvokeOnPreDodgeRollEvent(PlayerController player)
    {
        MulticastDelegate md = (MulticastDelegate)OnPreDodgeRollField.GetValue(player);
        if (md == null)
            return;
        Delegate[] delegates = md.GetInvocationList();
        if (delegates == null || delegates.Length == 0)
            return;
        object[] args = new object[] { player };
        foreach (Delegate handler in delegates)
            handler.Method.Invoke(handler.Target, args);
    }

    /// <summary>Buffer dodge roll inputs even if we're not otherwise accepting inputs.</summary>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.Update))]
    [HarmonyPrefix]
    private static void HandleDodgeRollBuffering(PlayerController __instance)
    {
        int pid = __instance.PlayerIDX;
        if (pid < 0)
            return;
        CustomDodgeRollData data = _CustomDodgeRollData[pid];
        if (data._Overrides.Count == 0)
            return;

        // Turn off dodgeButtonHeld state for all custom rolls if we aren't pushing the dodge button
        float now = BraveTime.ScaledTimeSinceStartup;
        bool dodgeButtonPressed = BraveInput.GetInstanceForPlayer(pid).ActiveActions.DodgeRollAction.IsPressed;
        foreach (CustomDodgeRoll customDodgeRoll in data._Overrides)
        {
            if (!dodgeButtonPressed)
                customDodgeRoll._dodgeButtonHeld = false;
            else if (!customDodgeRoll._dodgeButtonHeld && customDodgeRoll._isDodging && customDodgeRoll.bufferWindow > 0.0f)
            {
                customDodgeRoll._dodgeButtonHeld = true;
                customDodgeRoll._bufferTime = now; // keep track of the last time we buffered an input
            }
        }
    }

    /// <summary>The magic that actually handles initiating custom dodge rolls.</summary>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.HandleStartDodgeRoll))]
    [HarmonyPrefix]
    private static bool HandleStartCustomDodgeRoll(PlayerController __instance, Vector2 direction)
    {
        // Make sure we actually have all of our movements available (fixes not being able to dodge roll in the Aimless Void)
        PlayerController player = __instance;
        if (player.CurrentInputState != PlayerInputState.AllInput || !player.AcceptingNonMotionInput)
            return true;
        int pid = player.PlayerIDX;
        if (pid < 0)
            return true; // fall back to default behavior if we have an invalid player index

        CustomDodgeRollData data = _CustomDodgeRollData[pid];
        if (data._CurrentRoll is not CustomDodgeRoll customDodgeRoll)
            return true; // fall back to default behavior if we don't have overrides

        // Try initiating the most recently added dodge roll
        bool dodgeButtonPressed = BraveInput.GetInstanceForPlayer(pid).ActiveActions.DodgeRollAction.IsPressed;
        bool isBuffered = (BraveTime.ScaledTimeSinceStartup - customDodgeRoll._bufferTime) < customDodgeRoll.bufferWindow;
        if (!isBuffered && (!dodgeButtonPressed || customDodgeRoll._dodgeButtonHeld))
            return false; // skip original method

        customDodgeRoll._dodgeButtonHeld = true;
        if (customDodgeRoll.TryBeginDodgeRoll(direction, isBuffered))
        {
            InvokeOnPreDodgeRollEvent(__instance); // call the player's OnPreDodgeRoll events
            customDodgeRoll._bufferTime = 0.0f;
        }
        return false; // skip original method
    }

    /// <summary>Make sure opening chests disables input until the item get animation finishes playing.</summary>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.TriggerItemAcquisition))]
    [HarmonyPrefix]
    private static void AbortDodgeRollWhenOpeningChest(PlayerController __instance)
    {
        CustomDodgeRollData data = _CustomDodgeRollData[__instance.PlayerIDX];
        if (data._CurrentRoll)
            data._CurrentRoll.AbortDodgeRoll();
    }

    /// <summary>Recompute active dodge roll items when the player's stats are recomputed.</summary>
    [HarmonyPatch(typeof(PlayerStats), nameof(PlayerStats.RecalculateStatsInternal))]
    [HarmonyPostfix]
    private static void RecomputeActiveDodgeRoll(PlayerStats __instance, PlayerController owner)
    {
        if (!owner)
            return;

        int pid = owner.PlayerIDX;
        if (pid < 0)
            return;

        CustomDodgeRollData data = _CustomDodgeRollData[pid];
        data._Overrides.Clear();
        data._BloodiedScarfActive = false;
        foreach (PassiveItem p in owner.passiveItems)
        {
            if (p is BlinkPassiveItem) // bloodied scarf
                data._BloodiedScarfActive = true;
            else if (p is CustomDodgeRollItem dri && dri.CustomDodgeRoll() is CustomDodgeRoll overrideDodgeRoll)
            {
                data._Overrides.Add(overrideDodgeRoll);
                overrideDodgeRoll._owner = owner;
                data._BloodiedScarfActive = false; // bloodied scarf is active iff it's the last dodge roll modifier we picked up
            }
        }

        CustomDodgeRoll oldActiveDodgeRoll = data._CurrentRoll; // prevent infinite recursion into RecalculateStats()
        data._CurrentRoll = null;
        if (!data._BloodiedScarfActive && data._Overrides.Count > 0)
            data._CurrentRoll = data._Overrides.Last();
        if (oldActiveDodgeRoll != data._CurrentRoll)
        {
            if (oldActiveDodgeRoll != null)
                oldActiveDodgeRoll.AbortDodgeRoll(); // stop modded dodge rolls
            owner.ForceStopDodgeRoll(); // stop vanilla dodge rolls (calls AbortDodgeRoll() only for data._CurrentRoll, not oldActiveDodgeRoll)
        }
    }

    /// <summary>Allow dodge roll items to increase the number of midair dodge rolls a-la springheel boots</summary>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.CheckDodgeRollDepth))]
    [HarmonyILManipulator]
    private static void PlayerControllerCheckDodgeRollDepthIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchStloc(1)))
            return;
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldloca, 1);
        cursor.CallPrivate(typeof(CustomDodgeRollPatches), nameof(CheckAdditionalMidairDodgeRolls));
    }

    private static void CheckAdditionalMidairDodgeRolls(PlayerController player, ref int oldRolls)
    {
        for (int i = 0, n = player.passiveItems.Count; i < n; ++i)
            if (player.passiveItems[i] is CustomDodgeRollItem dri)
                oldRolls += dri.ExtraMidairDodgeRolls();
    }

    /// <summary>Allow custom dodge rolls to override Bloodied Scarf.</summary>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.DodgeRollIsBlink), MethodType.Getter)]
    [HarmonyPrefix]
    private static bool DisableBloodiedScarf(PlayerController __instance, ref bool __result)
    {
        int pid = __instance.PlayerIDX;
        if (pid < 0)
            return true;
        CustomDodgeRollData data = _CustomDodgeRollData[pid];
        if(!data._CurrentRoll)
            return true; // call the original method
        __result = false;
        return false; // skip the original method
    }

    /// <summary>Allow custom dodge rolls to effectively use IsDodgeRolling unless they specifically opt out of it.</summary>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.IsDodgeRolling), MethodType.Getter)]
    [HarmonyPostfix]
    private static void CountsAsDodgeRolling(PlayerController __instance, ref bool __result)
    {
        int pid = __instance.PlayerIDX;
        if (pid < 0)
            return;
        if (_CustomDodgeRollData[pid]._CurrentRoll is not CustomDodgeRoll roll)
            return;
        if (roll._isDodging && roll.countsAsDodgeRolling)
            __result = true; // force IsDodgeRolling to return true
    }

    /// <summary>Allow the player to attack during custom dodge rolls.</summary>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.m_CanAttack), MethodType.Getter)]
    [HarmonyILManipulator]
    private static void CheckCanAttackDuringCustomDodgeRollIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<PlayerController>("get_IsDodgeRolling")))
            return;

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.CallPrivate(typeof(CustomDodgeRollPatches), nameof(CheckCanAttackDuringCustomDodgeRoll));
    }

    private static bool CheckCanAttackDuringCustomDodgeRoll(bool origValue, PlayerController player)
    {
        int pid = player.PlayerIDX;
        if (pid < 0)
            return origValue;
        if (_CustomDodgeRollData[pid]._CurrentRoll is not CustomDodgeRoll roll || !roll._isDodging)
            return origValue;
        return !roll.canUseWeapon; //NOTE: need to invert since the original check is for !IsDodgeRolling
    }

    /// <summary>Determine whether a custom dodge roll counts as airborne for pit traversal purposes.</summary>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.QueryGroundedFrame))]
    [HarmonyPostfix]
    private static void CheckIfCustomDodgeRollIsGrounded(PlayerController __instance, ref bool __result)
    {
        int pid = __instance.PlayerIDX;
        if (pid >= 0 && _CustomDodgeRollData[pid]._CurrentRoll is CustomDodgeRoll roll && roll._isDodging)
            __result = !roll.isAirborne;
    }

    /// <summary>Determine whether a custom dodge roll grants the player projectile immunity while active.</summary>
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.OnPreCollision))]
    [HarmonyILManipulator]
    private static void CheckIfCustomDodgeRollDodgesProjectilesIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        for (int i = 0; i < 2; ++i) // go after second instance
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<tk2dSpriteAnimator>(nameof(tk2dSpriteAnimator.QueryInvulnerabilityFrame))))
                return;

        cursor.Emit(OpCodes.Ldarg_3); // otherRigidBody
        cursor.CallPrivate(typeof(CustomDodgeRollPatches), nameof(CheckIfCustomDodgeRollDodgesProjectiles));
    }

    private static bool CheckIfCustomDodgeRollDodgesProjectiles(bool origValue, SpeculativeRigidbody body)
    {
        if (!body || body.gameObject.GetComponent<PlayerController>() is not PlayerController player)
            return origValue;
        int pid = player.PlayerIDX;
        if (pid >= 0 && _CustomDodgeRollData[pid]._CurrentRoll is CustomDodgeRoll roll && roll._isDodging)
            return roll.dodgesProjectiles;
        return origValue;
    }

    /// <summary>Make sure custom dodge rolls respond to ForceStopDodgeRoll()</summary>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.ForceStopDodgeRoll))]
    [HarmonyPostfix]
    private static void ForceStopCustomDodgeRoll(PlayerController __instance)
    {
        int pid = __instance.PlayerIDX;
        if (pid >= 0 && _CustomDodgeRollData[pid]._CurrentRoll is CustomDodgeRoll roll && roll._isDodging)
            roll.AbortDodgeRoll();
    }

    /// <summary>Allow custom dodge rolls to ignore locked dodge roll direction if they want.</summary>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.Update))]
    [HarmonyILManipulator]
    private static void CheckIfCustomDodgeRollIsDirectionLockedIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        for (int i = 0; i < 5; ++i) // go after the fifth instance
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<PlayerController>("get_IsDodgeRolling")))
                return;

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.CallPrivate(typeof(CustomDodgeRollPatches), nameof(CheckIfCustomDodgeRollDodgesProjectiles));
    }

    private static bool CheckIfCustomDodgeRollIsDirectionLocked(bool origValue, PlayerController player)
    {
        int pid = player.PlayerIDX;
        if (pid >= 0 && _CustomDodgeRollData[pid]._CurrentRoll is CustomDodgeRoll roll && roll._isDodging)
            return roll.lockedDirection;
        return origValue;
    }

    /// <summary>Allow custom dodge rolls to avoid contact damage with enemies if they want.</summary>
    [HarmonyPatch(typeof(AIActor), nameof(AIActor.OnCollision))]
    [HarmonyILManipulator]
    private static void CheckIfCustomDodgeRollTakesContactDamageIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<PlayerController>("get_ReceivesTouchDamage")))
            return;

        cursor.Emit(OpCodes.Ldloc_1); // PlayerController
        cursor.CallPrivate(typeof(CustomDodgeRollPatches), nameof(CheckIfCustomDodgeRollTakesContactDamage));
    }

    private static bool CheckIfCustomDodgeRollTakesContactDamage(bool origValue, PlayerController player)
    {
        if (!origValue)
            return false; // player is already immune to contact damage for other reasons
        int pid = player.PlayerIDX;
        if (pid >= 0 && _CustomDodgeRollData[pid]._CurrentRoll is CustomDodgeRoll roll && roll._isDodging)
            return roll.takesContactDamage;
        return true;
    }

    /// <summary>Allow custom dodge rolls to override base roll damage for both normal roll damage and check-if-enemy-is-killed roll damage.</summary>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.ApplyRollDamage))]
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.OnPreRigidbodyCollision))]
    [HarmonyILManipulator]
    private static void GetOverrideDodgeRollDamageIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After,
          instr => instr.MatchLdfld<PlayerController>(nameof(PlayerController.stats)),
          instr => instr.MatchLdfld<PlayerStats>(nameof(PlayerStats.rollDamage))
          ))
            return;

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.CallPrivate(typeof(CustomDodgeRollPatches), nameof(GetOverrideDodgeRollDamage));
    }

    private static float GetOverrideDodgeRollDamage(float origValue, PlayerController player)
    {
        float rollDamage = origValue;
        int pid = player.PlayerIDX;
        if (pid >= 0 && _CustomDodgeRollData[pid]._CurrentRoll is CustomDodgeRoll roll && roll._isDodging)
            rollDamage = roll.overrideRollDamage;
        return rollDamage >= 0 ? rollDamage : origValue;
    }
}
