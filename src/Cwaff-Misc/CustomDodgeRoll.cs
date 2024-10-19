namespace CwaffingTheGungy;

public class CustomDodgeRoll : MonoBehaviour
{
    public bool dodgeButtonHeld   { get; protected set; }
    public bool isDodging         { get; private set; }
    public PlayerController owner { get; set; }

    private static List<CustomDodgeRoll> _Overrides = new();

    public virtual bool canDodge      => true;
    public virtual bool canMultidodge => false;
    public virtual bool putsOutFire   => true;

    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.HandleStartDodgeRoll))]
    private class CustomDodgeRollPatch
    {
        static bool Prefix(ref PlayerController __instance, Vector2 direction, ref bool __result)
        { //REFACTOR: cache dodge roll overrides
            PlayerController player = __instance;
            // Make sure we can actually have all of our movements available (fixes not being able to dodge roll in the Aimless Void)
            if (player.CurrentInputState != PlayerInputState.AllInput || !player.AcceptingNonMotionInput || player.IsDodgeRolling)
                return true;

            // Figure out all of our passives that give us a custom dodge roll
            _Overrides.Clear();
            foreach (PassiveItem p in player.passiveItems)
                if (p && p.GetComponent<CustomDodgeRoll>() is CustomDodgeRoll overrideDodgeRoll)
                    _Overrides.Add(overrideDodgeRoll);
            if (_Overrides.Count == 0)  // fall back to default behavior if we don't have overrides
                return true;

            // Turn off dodgeButtonHeld state for all custom rolls if we aren't pushing the dodge button
            BraveInput instanceForPlayer = BraveInput.GetInstanceForPlayer(player.PlayerIDX);
            if (!instanceForPlayer.ActiveActions.DodgeRollAction.IsPressed)
            {
                foreach (CustomDodgeRoll customDodgeRoll in _Overrides)
                    customDodgeRoll.dodgeButtonHeld = false;
                __result = false; // dodge roll failed
                return false; // skip original method
            }

            // Begin the dodge roll for all of our custom dodge rolls available
            // instanceForPlayer.ConsumeButtonDown(GungeonActions.GungeonActionType.DodgeRoll);
            foreach (CustomDodgeRoll customDodgeRoll in _Overrides)
            {
                if (customDodgeRoll.dodgeButtonHeld)
                    continue;
                customDodgeRoll.dodgeButtonHeld = true;
                customDodgeRoll.TryDodgeRoll();
            }
            __result = true; // dodge roll succeeded
            return false; // skip original method
        }
    }

    //NOTE: opening chests disable input until the item get animation finishes playing,
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.TriggerItemAcquisition))]
    private class PlayerControllerTriggerItemAcquisitionPatch
    {
        static void Prefix(PlayerController __instance)
        {
            if (__instance.passiveItems == null)
                return;
            foreach (PassiveItem p in __instance.passiveItems)
                if (p && p.GetComponent<CustomDodgeRoll>() is CustomDodgeRoll cdr)
                    cdr.AbortDodgeRoll();
        }
    }

    protected virtual void BeginDodgeRoll()
    {
        // any dodge setup code should be here
        if (!this.owner)
            return;

        // by default, we want to make sure we can put out fires at the beginning of our dodge roll
        if (this.putsOutFire && this.owner.CurrentFireMeterValue > 0f)
        {
            this.owner.CurrentFireMeterValue = Mathf.Max(0f, this.owner.CurrentFireMeterValue - 0.5f);
            if (this.owner.CurrentFireMeterValue == 0f)
                this.owner.IsOnFire = false;
        }
    }

    protected virtual void FinishDodgeRoll(bool aborted = false)
    {
        // any succesful (or aborted) dodge cleanup code should be here
    }

    public void AbortDodgeRoll()
    {
        if (!isDodging)
            return;
        FinishDodgeRoll(aborted: true);
        isDodging = false;
    }

    protected virtual IEnumerator ContinueDodgeRoll()
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
        if (isDodging)
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
