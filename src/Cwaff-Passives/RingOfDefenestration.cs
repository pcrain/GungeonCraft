namespace CwaffingTheGungy;

public class RingOfDefenestration : PassiveItem
{
    public static string ItemName         = "Ring of Defenestration";
    public static string ShortDescription = "Babies AND Bathwater";
    public static string LongDescription  = "Pushing an enemy into a pit has an 80% chance to spawn casings and a 20% chance to spawn a random pickup.";
    public static string Lore             = "Every warrior worth their salt occasionally tosses their opponents out of a window or moving vehicle to plummet towards their doom. While stylish and assertive, this maneuver unfortunately makes it very difficult to retrieve any loot the victims may have been carrying. This ring represents a pact with the Pit Lord to return any loot carried by those sacrificed to the pits of the Gungeon.";
    public static int    ID;

    internal static readonly List<IntVector2> _RewardWeights = new() {
        new(-3,                     40), // 3 shells
        new(-5,                     24), // 5 shells
        new(-10,                    16), // 10 shells
        new((int)Items.Heart,        4),
        new((int)Items.Armor,        4),
        new((int)Items.Blank,        4),
        new((int)Items.Ammo,         4),
        new((int)Items.PartialAmmo,  4),
    };

    public static void Init()
    {
        PickupObject item = Lazy.SetupPassive<RingOfDefenestration>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;
        ID                = item.PickupObjectId;
        new Hook(
            typeof(GameStatsManager).GetMethod("RegisterStatChange", BindingFlags.Instance | BindingFlags.Public),
            typeof(RingOfDefenestration).GetMethod("OnRegisterStatChange", BindingFlags.Static | BindingFlags.NonPublic)
            );
    }

    private static void OnRegisterStatChange(Action<GameStatsManager, TrackedStats, float> orig, GameStatsManager manager, TrackedStats stat, float value)
    {
        orig(manager, stat, value);
        if (stat != TrackedStats.ENEMIES_KILLED_WITH_PITS || !GameManager.Instance.AnyPlayerHasPickupID(ID))
            return;

        int pickupID = _RewardWeights.GetWeightedPickupID();
        if (pickupID < 0) // currency
            LootEngine.SpawnCurrency(GameManager.Instance.BestActivePlayer.CenterPosition, -pickupID);
        else
            LootEngine.SpawnItem(
              item              : PickupObjectDatabase.GetById(pickupID).gameObject,
              spawnPosition     : GameManager.Instance.BestActivePlayer.CenterPosition,
              spawnDirection    : Vector2.zero,
              force             : 0,
              doDefaultItemPoof : true);
    }
}
