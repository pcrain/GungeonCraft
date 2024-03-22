namespace CwaffingTheGungy;

public class BlankChecks : PassiveItem
{
    public static string ItemName         = "Blank Checks";
    public static string ShortDescription = "Write-off";
    public static string LongDescription  = "Trying to use a blank without one in your inventory gives you 3 blanks and +1 curse. Will not work if you already have 10 or more curse.";
    public static string Lore             = "Rumor has it that blank checks were originally conceived of outside the domain of weaponry entirely, and were developed primarily for use in large-scale business transactions. As firearms are only very rarely involved in such transactions, why so many business people have any use for extra blanks remains a mystery to this day.";

    public static void Init()
    {
        PickupObject item  = Lazy.SetupPassive<BlankChecks>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality       = ItemQuality.B;
        item.AddToSubShop(ItemBuilder.ShopType.Cursula);
        item.AddToSubShop(ItemBuilder.ShopType.OldRed);
    }

    public override void Pickup(PlayerController player)
    {
        if (!this.m_pickedUpThisRun)
            GameManager.Instance.PrimaryPlayer.Blanks += 1;
        base.Pickup(player);
    }

    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.DoConsumableBlank))]
    private class BlankCheckPatch
    {
        static void Prefix(PlayerController __instance)
        {
            if (!__instance.GetPassive<BlankChecks>())
                return; // if we don't have Blank Checks, we have nothing to do
            if (__instance.Blanks > 0)
                return; // if we have more than 1 blank, we have nothing to do
            if (PlayerStats.GetTotalCurse() >= 10)
                return; // if we have at least 10 curse, we can't blank any more

            __instance.Blanks += 3;
            __instance.IncreaseCurse();
        }
    }
}
