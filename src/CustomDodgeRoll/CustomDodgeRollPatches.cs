namespace CwaffingTheGungy;

[HarmonyPatch]
internal static class CustomDodgeRollPatches
{
    private static List<CustomDodgeRoll>[] _Overrides = [new(), new()];
    private static bool[] _BloodiedScarfActive = [false, false];

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
        if (pid < 0 || _Overrides[pid].Count == 0)
            return;

        // Turn off dodgeButtonHeld state for all custom rolls if we aren't pushing the dodge button
        float now = BraveTime.ScaledTimeSinceStartup;
        bool dodgeButtonPressed = BraveInput.GetInstanceForPlayer(pid).ActiveActions.DodgeRollAction.IsPressed;
        foreach (CustomDodgeRoll customDodgeRoll in _Overrides[pid])
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
    private static bool Prefix(PlayerController __instance, Vector2 direction)
    {
        // Make sure we actually have all of our movements available (fixes not being able to dodge roll in the Aimless Void)
        PlayerController player = __instance;
        if (player.CurrentInputState != PlayerInputState.AllInput || !player.AcceptingNonMotionInput || player.IsDodgeRolling)
            return true;
        int pid = player.PlayerIDX;
        if (pid < 0 || _Overrides[pid].Count == 0 || _BloodiedScarfActive[pid])
            return true; // fall back to default behavior if we don't have overrides

        // Try initiating the most recently added dodge roll
        CustomDodgeRoll customDodgeRoll = _Overrides[pid].Last();
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
    private static void Prefix(PlayerController __instance)
    {
        foreach (CustomDodgeRoll cdr in _Overrides[__instance.PlayerIDX])
            cdr.AbortDodgeRoll();
    }

    /// <summary>Recompute active dodge roll items when the player's stats are recomputed.</summary>
    [HarmonyPatch(typeof(PlayerStats), nameof(PlayerStats.RecalculateStatsInternal))]
    private static void Postfix(PlayerStats __instance, PlayerController owner)
    {
        if (!owner)
            return;
        int pid = owner.PlayerIDX;
        if (pid < 0)
            return;
        _Overrides[pid].Clear();
        _BloodiedScarfActive[pid] = false;
        foreach (PassiveItem p in owner.passiveItems)
        {
            if (p is BlinkPassiveItem) // bloodied scarf
                _BloodiedScarfActive[pid] = true;
            else if (p is ICustomDodgeRollItem dri && dri.CustomDodgeRoll() is CustomDodgeRoll overrideDodgeRoll)
            {
                _Overrides[pid].Add(overrideDodgeRoll);
                overrideDodgeRoll._owner = owner;
                _BloodiedScarfActive[pid] = false; // bloodied scarf is active iff it's the last dodge roll modifier we picked up
            }
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
            if (player.passiveItems[i] is ICustomDodgeRollItem dri)
                oldRolls += dri.ExtraMidairDodgeRolls();
    }

    /// <summary>Allow custom dodge rolls to override Bloodied Scarf.</summary>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.DodgeRollIsBlink), MethodType.Getter)]
    [HarmonyPrefix]
    private static bool DisableBloodiedScarf(PlayerController __instance, ref bool __result)
    {
        int pid = __instance.PlayerIDX;
        if (pid < 0 || _Overrides[pid].Count == 0 || _BloodiedScarfActive[pid])
            return true; // call the original method
        __result = false;
        return false; // skip the original method
    }
}
