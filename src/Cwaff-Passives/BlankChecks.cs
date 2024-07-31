namespace CwaffingTheGungy;

public class BlankChecks : CwaffPassive
{
    public static string ItemName         = "Blank Checks";
    public static string ShortDescription = "Write-off";
    public static string LongDescription  = "Trying to use a blank without one in your inventory gives you 3 blanks and +1 curse. Will not work if you already have 10 or more curse. Grants 1 blank when first picked up.";
    public static string Lore             = "Rumor has it that blank checks were originally conceived of outside the domain of weaponry entirely, and were developed primarily for use in large-scale business transactions. As firearms are only very rarely involved in such transactions, why so many business people have any use for extra blanks remains a mystery to this day.";

    private const float _BLANK_EXPRESSION_DISCOUNT = 0.1f;

    public static void Init()
    {
        PassiveItem item   = Lazy.SetupPassive<BlankChecks>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality       = ItemQuality.B;
        item.AddToSubShop(ItemBuilder.ShopType.Cursula);
        item.AddToSubShop(ItemBuilder.ShopType.OldRed);
    }

    public override void OnFirstPickup(PlayerController player)
    {
        base.OnFirstPickup(player);
        GameManager.Instance.PrimaryPlayer.Blanks += 1;
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        player.OnUsedBlank += this.OnUsedBlank;
    }

    public override DebrisObject Drop(PlayerController player)
    {
        player.OnUsedBlank -= this.OnUsedBlank;
        return base.Drop(player);
    }

    public override void OnDestroy()
    {
        if (this.Owner)
            this.Owner.OnUsedBlank -= this.OnUsedBlank;
        base.OnDestroy();
    }

    private void OnUsedBlank(PlayerController player, int remainingBlanks)
    {
        if (!player.HasSynergy(Synergy.BLANK_EXPRESSION))
            return;
        foreach (BaseShopController shop in StaticReferenceManager.AllShops)
            if (shop.GetAbsoluteParentRoom() == player.CurrentRoom)
                shop.ShopCostModifier = Mathf.Max(shop.ShopCostModifier - _BLANK_EXPRESSION_DISCOUNT, 0f);
    }

    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.DoConsumableBlank))]
    private class BlankCheckPatch
    {
        static void Prefix(PlayerController __instance)
        {
            if (!__instance.HasPassive<BlankChecks>())
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
