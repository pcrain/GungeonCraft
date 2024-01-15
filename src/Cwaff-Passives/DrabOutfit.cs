namespace CwaffingTheGungy;

public class DrabOutfit : PassiveItem
{
    public static string ItemName         = "Drab Outfit";
    public static string ShortDescription = "Completely Unremarkable";
    public static string LongDescription  = "Sets Magnificence stat to 0 while in inventory, increasing the frequency of red and black chest spawns.";
    public static string Lore             = "This garment seems to go slightly out of its way to be as plain and boring as possible. It does not go completely out of its way, however, as that would actually make it notable in some sense. Which it certainly isn't.";

    private static int _DrabOutfitId;
    private static Hook _DrabOutfitHook;

    public static void Init()
    {
        PickupObject item = Lazy.SetupPassive<DrabOutfit>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.D;
        item.AddToSubShop(ModdedShopType.Rusty);

        _DrabOutfitId   = item.PickupObjectId;
        _DrabOutfitHook = new Hook(
            typeof(FloorRewardData).GetMethod("DetermineCurrentMagnificence", BindingFlags.Instance | BindingFlags.Public),
            typeof(DrabOutfit).GetMethod("OnDetermineCurrentMagnificence", BindingFlags.Static | BindingFlags.NonPublic)
            );
    }

    private static float OnDetermineCurrentMagnificence(Func<FloorRewardData, bool, float> orig, FloorRewardData rewardData, bool isGenerationForMagnificence)
    {
        if (GameManager.Instance.AnyPlayerHasPickupID(_DrabOutfitId))
            return 0f;  // completely disable the vanilla magnificence system as long as we have Drab Outfit
        return orig(rewardData, isGenerationForMagnificence);
    }
}
