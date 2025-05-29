namespace CwaffingTheGungy;

public class CombatLeotard : CwaffPassive
{
    public static string ItemName         = "Combat Leotard";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    //NOTE: uses AttackWhileRollingPatch for the bulk of its effects
    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<CombatLeotard>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.B;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        PassiveItem.IncrementFlag(player, typeof(CombatLeotard));
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        if (player)
            PassiveItem.DecrementFlag(player, typeof(CombatLeotard));
    }
}
