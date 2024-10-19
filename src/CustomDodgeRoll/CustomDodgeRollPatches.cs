namespace CwaffingTheGungy;

[HarmonyPatch]
internal static class CustomDodgeRollPatches
{
    private static List<CustomDodgeRoll>[] _Overrides = [new(), new()];

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
        if (delegates.Length == 0)
            return;
        object[] args = new object[] { player };
        foreach (Delegate handler in delegates)
            handler.Method.Invoke(handler.Target, args);
    }

    /// <summary>The magic that actually handles initiating custom dodge rolls.</summary>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.HandleStartDodgeRoll))]
    private static bool Prefix(ref PlayerController __instance, Vector2 direction)
    {
        PlayerController player = __instance;
        // Make sure we can actually have all of our movements available (fixes not being able to dodge roll in the Aimless Void)
        if (player.CurrentInputState != PlayerInputState.AllInput || !player.AcceptingNonMotionInput || player.IsDodgeRolling)
            return true;

        // Figure out all of our passives that give us a custom dodge roll
        int pid = player.PlayerIDX;
        if (pid < 0 || _Overrides[pid].Count == 0)  // fall back to default behavior if we don't have overrides
            return true;

        // Turn off dodgeButtonHeld state for all custom rolls if we aren't pushing the dodge button
        BraveInput instanceForPlayer = BraveInput.GetInstanceForPlayer(pid);
        if (!instanceForPlayer.ActiveActions.DodgeRollAction.IsPressed)
        {
            foreach (CustomDodgeRoll customDodgeRoll in _Overrides[pid])
                customDodgeRoll._dodgeButtonHeld = false;
            return false; // skip original method
        }

        // Try initiating all available custom dodge rolls, starting from the end so newly-picked up items invoke first
        bool anyDodgeRollSucceeded = false;
        for (int i = _Overrides[pid].Count - 1; i >= 0; --i)
        {
            CustomDodgeRoll customDodgeRoll = _Overrides[pid][i];
            if (customDodgeRoll._dodgeButtonHeld)
                continue;
            customDodgeRoll._dodgeButtonHeld = true;
            if (customDodgeRoll.TryBeginDodgeRoll(direction))
                anyDodgeRollSucceeded = true;
        }
        if (anyDodgeRollSucceeded)
            InvokeOnPreDodgeRollEvent(__instance); // call the player's OnPreDodgeRoll events
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
        foreach (PassiveItem p in owner.passiveItems)
            if (p is ICustomDodgeRollItem dri && dri.CustomDodgeRoll() is CustomDodgeRoll overrideDodgeRoll)
            {
                _Overrides[pid].Add(overrideDodgeRoll);
                overrideDodgeRoll._owner = owner;
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
}
