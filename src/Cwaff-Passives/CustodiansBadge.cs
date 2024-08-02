namespace CwaffingTheGungy;

/*
    TODO:
        - handle teleporting out of rooms or otherwise leaving them unceremoniously
*/

public class CustodiansBadge : CwaffPassive
{
    public static string ItemName         = "Custodian's Badge";
    public static string ShortDescription = "Neat and Tidy";
    public static string LongDescription  = "Rewards extra shells for leaving minor breakables unscathed after each combat encounter. Incurs one strike after letting more than 70% of breakables in a room break. Disappears after incurring three strikes.";
    public static string Lore             = "The role of Gungeon Custodian has been unfilled since the hiring of the Gungeon Janitorial Crew, who rendered the position mostly obsolete. The pay is mediocre, the working conditions are rather dangerous, and the job itself is surprisingly difficult. However, for adventurers who happen to be passing through the Gungeon, earning a few extra shells before inevitably getting fired for breaking everything with a BSG just might help pay for that extra piece of armor -- which will promptly be lost by stumbling into a pit.";

    internal const int   _MAX_CHANCES      = 3;
    internal const float _FAIL_THRESHOLD   = 0.3f;
    internal const float _REWARD_THRESHOLD = 0.7f;
    internal const int   _PERFECT_BONUS    = 3;

    internal static string _MSG_JOIN =
        "Hey! Thanks for joining the curation crew! We like to keep an orderly Gungeon, so make sure you keep those mischievous Gundead from breaking everything.";
    internal static string _MSG_STRIKE_ONE =
        "Alright, you let a few too many things break. I'll let you off with a warning this time, but please be more careful in the future.";
    internal static string _MSG_STRIKE_TWO =
        "Look, your job is to keep stuff from breaking...and you're not doing that. One more muck-up like this and you're fired!";
    internal static string _MSG_FIRED =
        "ALRIGHT, THAT'S IT, YOU'RE OUT OF THE JOB!";

    // NOTE: controllerbutton.prefab contains a list of valid sprites we can insert into notes
    internal static string _SIGNATURE =
        "\n\n- Your Boss"
        + "[sprite \"resourceful_rat_icon_001\"]"
        ;

    private int curRoomBreakables = 0;
    private int maxRoomBreakables = 0;

    public int chancesLeft       = _MAX_CHANCES;

    public static void Init()
    {
        PassiveItem item   = Lazy.SetupPassive<CustodiansBadge>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality       = ItemQuality.D;
        item.CanBeDropped  = false;
    }

    public override void OnFirstPickup(PlayerController player)
    {
        base.OnFirstPickup(player);
        this.chancesLeft = _MAX_CHANCES;
        string s = _MSG_JOIN + _SIGNATURE;
        CustomNoteDoer.CreateNote(player.CenterPosition, s);
    }

    public override void Pickup(PlayerController player)
    {
        player.OnEnteredCombat += this.OnEnteredCombat;
        base.Pickup(player);
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        if (!player)
            return;
        player.OnEnteredCombat -= this.OnEnteredCombat;
    }

    private void OnEnteredCombat()
    {
        if (!this.Owner)
            return;
        RoomHandler currentRoom = this.Owner.CurrentRoom;
        this.curRoomBreakables = 0;
        foreach (MinorBreakable minorBreakable in StaticReferenceManager.AllMinorBreakables)
        {
            if (minorBreakable && !minorBreakable.IsBroken && minorBreakable.CenterPoint.GetAbsoluteRoom() == currentRoom)
            {
                ++this.curRoomBreakables;
                minorBreakable.OnBreakContext += this.HandleBroken;
            }
        }
        this.maxRoomBreakables = this.curRoomBreakables;
        if (this.maxRoomBreakables == 0)
            return;
        currentRoom.OnEnemiesCleared += this.OnRoomCleared;
    }

    private void OnRoomCleared()
    {
        bool success = true;
        Vector2 clearSpot;
        float percentIntact = (float)this.curRoomBreakables / (float)this.maxRoomBreakables;
        // ETGModConsole.Log("ended with " + this.curRoomBreakables + " / " + this.maxRoomBreakables + " ("+percentIntact+") breakables");
        if (percentIntact < _FAIL_THRESHOLD) // we failed, take a strike
        {
            if (this.Owner.CurrentRoom.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS)
                return; // be a little more forgiving in boss rooms
            --chancesLeft;
            string angry;
            if (chancesLeft == 2)
                angry = _MSG_STRIKE_ONE;
            else if (chancesLeft == 1)
                angry = _MSG_STRIKE_TWO;
            else
                angry = _MSG_FIRED;
            angry += _SIGNATURE;
            clearSpot = this.Owner.CurrentRoom.GetCenteredVisibleClearSpot(2,2, out success).ToVector2();
            if (success)
                CustomNoteDoer.CreateNote(clearSpot, angry);
            if (chancesLeft == 0)
                this.Owner.RemovePassiveItem(this.PickupObjectId);
            return;
        }

        if (percentIntact <= _REWARD_THRESHOLD)
            return; // didn't mess up too badly, but didn't do well enough to earn a reward

        // 1 shell bonus for every 5% above 70%
        int percentBonus = Mathf.CeilToInt(20f * (percentIntact - _REWARD_THRESHOLD));
        // 1 shell bonus for every 10 breakables left standing
        int absoluteBonus = this.curRoomBreakables / 10;
        // Use the max of percent and absolute bonus
        int shellBonus = Mathf.Max(percentBonus, absoluteBonus);
        string happy = $"Here's {shellBonus} casing{(shellBonus==1?"":"s")}, keep up the good work! :)";
        if (percentIntact == 1.0f)
        {
            shellBonus += _PERFECT_BONUS;
            happy = $"Marvelous!\n\n" + happy;
        }
        clearSpot = this.Owner.CurrentRoom.GetCenteredVisibleClearSpot(2,2, out success).ToVector2();
        if (success)
            CustomNoteDoer.CreateNote(clearSpot, happy + _SIGNATURE);
        LootEngine.SpawnCurrency(success ? clearSpot : this.Owner.CenterPosition, shellBonus, false, null, null, startingZForce: 40f);
    }

    private void HandleBroken(MinorBreakable mb)
    {
        --this.curRoomBreakables;
    }

    public override void MidGameSerialize(List<object> data)
    {
        base.MidGameSerialize(data);
        data.Add(chancesLeft);
    }

    public override void MidGameDeserialize(List<object> data)
    {
        base.MidGameDeserialize(data);
        chancesLeft = (int)data[0];
    }
}
