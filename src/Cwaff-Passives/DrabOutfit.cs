namespace CwaffingTheGungy;

public class DrabOutfit : CwaffPassive
{
    public static string ItemName         = "Drab Outfit";
    public static string ShortDescription = "Completely Unremarkable";
    public static string LongDescription  = "Sets Magnificence stat to 0 while in inventory, increasing the frequency of red and black chest spawns.";
    public static string Lore             = "This garment seems to go slightly out of its way to be as plain and boring as possible. It does not go completely out of its way, however, as that would actually make it notable in some sense. Which it certainly isn't.";

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<DrabOutfit>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.D;
        item.AddToSubShop(ModdedShopType.Rusty);
    }

    [HarmonyPatch(typeof(FloorRewardData), nameof(FloorRewardData.DetermineCurrentMagnificence))]
    private class DrabOutfitPatch
    {
        static bool Prefix(bool isGenerationForMagnificence, ref float __result)
        {
            if (!Lazy.AnyoneHas<DrabOutfit>())
                return true;

            __result = 0f;  // set magnificence to 0
            return false;  // skip the original check
        }
    }
}
