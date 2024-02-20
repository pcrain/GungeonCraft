namespace CwaffingTheGungy;

public interface ICustomDodgeRoll
{
    public bool dodgeButtonHeld   { get; set; }
    public bool isDodging         { get; set; }
    public PlayerController owner { get; set; }

    public bool canDodge      { get; }  // if false, disables a CustomDodgeRoll from activating
    public bool canMultidodge { get; }  // if true, enables dodging while already mid-dodge
    public bool putsOutFire   { get; }  // if true, puts out fires when the dodge roll starts up

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
    public virtual bool putsOutFire   => true;

    private static Hook customDodgeRollHook = null;

    public static void InitCustomDodgeRollHooks()
    {
        customDodgeRollHook = new Hook(
            typeof(PlayerController).GetMethod("HandleStartDodgeRoll", BindingFlags.NonPublic | BindingFlags.Instance),
            typeof(CustomDodgeRoll).GetMethod("CustomDodgeRollHook"));
    }

    public static bool CustomDodgeRollHook(Func<PlayerController,Vector2,bool> orig, PlayerController player, Vector2 direction)
    {
        // Make sure we can actually have all of our movements available (fixes not being able to dodge roll in the Aimless Void)
        if (player.CurrentInputState != PlayerInputState.AllInput || !player.AcceptingNonMotionInput || player.IsDodgeRolling)
            return orig(player,direction);

        // Figure out all of our passives that give us a custom dodge roll
        List<CustomDodgeRoll> overrides = new List<CustomDodgeRoll>();
        foreach (PassiveItem p in player.passiveItems)
            if (p && p.GetComponent<CustomDodgeRoll>() is CustomDodgeRoll overrideDodgeRoll)
                overrides.Add(overrideDodgeRoll);
        if (overrides.Count == 0)  // fall back to default behavior if we don't have overrides
            return orig(player,direction);

        // Turn off dodgeButtonHeld state for all custom rolls if we aren't pushing the dodge button
        BraveInput instanceForPlayer = BraveInput.GetInstanceForPlayer(player.PlayerIDX);
        if (!instanceForPlayer.ActiveActions.DodgeRollAction.IsPressed)
        {
            foreach (CustomDodgeRoll customDodgeRoll in overrides)
                customDodgeRoll.dodgeButtonHeld = false;
            return false;
        }

        // Begin the dodge roll for all of our custom dodge rolls available
        // instanceForPlayer.ConsumeButtonDown(GungeonActions.GungeonActionType.DodgeRoll);
        foreach (CustomDodgeRoll customDodgeRoll in overrides)
        {
            if (customDodgeRoll.dodgeButtonHeld)
                continue;
            customDodgeRoll.dodgeButtonHeld = true;
            customDodgeRoll.TryDodgeRoll();
        }
        return true;
    }

    public virtual void BeginDodgeRoll()
    {
        // any dodge setup code should be here
        if (!this.owner)
            return;

        // by default, we want to make sure we can put out fires at the beginning of our dodge roll
        if (this.putsOutFire && this.owner.CurrentFireMeterValue > 0f)
        {
            this.owner.CurrentFireMeterValue = Mathf.Max(0f, this.owner.CurrentFireMeterValue -= 0.5f);
            if (this.owner.CurrentFireMeterValue == 0f)
                this.owner.IsOnFire = false;
        }
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
