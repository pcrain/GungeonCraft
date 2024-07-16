namespace CwaffingTheGungy;

public class RingOfDefenestration : CwaffPassive
{
    public static string ItemName         = "Ring of Defenestration";
    public static string ShortDescription = "Babies AND Bathwater";
    public static string LongDescription  = "Pushing an enemy into a pit has an 80% chance to spawn casings and a 20% chance to spawn a random pickup.";
    public static string Lore             = "Every warrior worth their salt occasionally tosses their opponents out of a window or moving vehicle to plummet towards their doom. While stylish and assertive, this maneuver unfortunately makes it very difficult to retrieve any loot the victims may have been carrying. This ring represents a pact with the Pit Lord to return any loot carried by those sacrificed to the pits of the Gungeon.";

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
        PassiveItem item  = Lazy.SetupPassive<RingOfDefenestration>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;
    }

    [HarmonyPatch(typeof(GameStatsManager), nameof(GameStatsManager.RegisterStatChange))]
    private class RingOfDefenestrationPatch
    {
        static void Postfix(TrackedStats stat, float value)
        {
            if (stat != TrackedStats.ENEMIES_KILLED_WITH_PITS || !Lazy.AnyoneHas<RingOfDefenestration>())
                return;

            int pickupID = _RewardWeights.WeightedRandom();
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
}
