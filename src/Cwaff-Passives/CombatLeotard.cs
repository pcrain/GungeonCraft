namespace CwaffingTheGungy;

public class CombatLeotard : CwaffPassive
{
    public static string ItemName         = "Combat Leotard";
    public static string ShortDescription = "Move Like They Do";
    public static string LongDescription  = "Allows the user to fire most weapons while dodge rolling.";
    public static string Lore             = "Touted as the world's only leotard built to both olympic and military specifications. On top of affording surprising flexibility and mobility, the suit also grants its wearer the confidence they can do just about anything when contorted at just about any angle. Whether this feeling comes from the suit's sleek design or the hundreds of electrodes lining the suit's interior is left to individual discretion.";

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
