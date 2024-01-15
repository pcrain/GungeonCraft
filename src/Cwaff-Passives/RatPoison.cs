namespace CwaffingTheGungy;

public class RatPoison : PassiveItem
{
    public static string ItemName         = "Rat Poison";
    public static string ShortDescription = "Swiper no Swiping";
    public static string LongDescription  = "Completely prevents the Resourceful Rat from stealing items.";
    public static string Lore             = "The Hegemony has invested hundreds of thousands of credits into researching both diplomatic and military means of discouraging the Resourceful Rat's thievery. It turns out that splashing some pickle juice on your items is enough to keep the rodent at bay indefinitely, though the lingering odor is far from pleasant.";

    private static int ratPoisonId;
    private static Hook ratPoisonHook;

    public static void Init()
    {
        PickupObject item                  = Lazy.SetupPassive<RatPoison>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality                       = ItemQuality.C;
        item.IgnoredByRat                  = true;
        item.ClearIgnoredByRatFlagOnPickup = false;
        item.AddToSubShop(ItemBuilder.ShopType.Cursula);

        ratPoisonId   = item.PickupObjectId;
        ratPoisonHook = new Hook(
            typeof(PickupObject).GetMethod("ShouldBeTakenByRat", BindingFlags.Instance | BindingFlags.NonPublic),
            typeof(RatPoison).GetMethod("ShouldBeTakenByRat", BindingFlags.Static | BindingFlags.NonPublic)
            );
    }

    private static bool ShouldBeTakenByRat(Func<PickupObject, Vector2, bool> orig, PickupObject pickup, Vector2 point)
    {
        if (GameManager.Instance.AnyPlayerHasPickupID(ratPoisonId))
            return false;
        return orig(pickup, point);
    }
}
