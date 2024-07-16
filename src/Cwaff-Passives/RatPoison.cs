namespace CwaffingTheGungy;

public class RatPoison : CwaffPassive
{
    public static string ItemName         = "Rat Poison";
    public static string ShortDescription = "Swiper no Swiping";
    public static string LongDescription  = "Completely prevents the Resourceful Rat from stealing items.";
    public static string Lore             = "The Hegemony has invested hundreds of thousands of credits into researching both diplomatic and military means of discouraging the Resourceful Rat's thievery. It turns out that splashing some pickle juice on your items is enough to keep the rodent at bay indefinitely, though the lingering odor is far from pleasant.";

    public static void Init()
    {
        PassiveItem item                   = Lazy.SetupPassive<RatPoison>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality                       = ItemQuality.C;
        item.IgnoredByRat                  = true;
        item.ClearIgnoredByRatFlagOnPickup = false;
        item.AddToSubShop(ItemBuilder.ShopType.Cursula);
    }

    [HarmonyPatch(typeof(PickupObject), nameof(PickupObject.ShouldBeTakenByRat))]
    private class RatPoisonPatch
    {
        static bool Prefix(Vector2 point, ref bool __result)
        {
            if (!Lazy.AnyoneHas<RatPoison>())
                return true;

            __result = false;  // don't let rat take any items
            return false;  // skip the original check
        }
    }
}
