namespace CwaffingTheGungy;

public class GoopStabilizer : CwaffPassive
{
    public static string ItemName         = "Goop Stabilizer";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const float _STABILIZATION_MULT = 4f;

    private static bool _AnyoneHasGoopStabilizer = false;

    public static void Init()
    {
        PassiveItem item = Lazy.SetupPassive<GoopStabilizer>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality     = ItemQuality.D;
        item.AddToSubShop(ItemBuilder.ShopType.Goopton);
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        PassiveItem.IncrementFlag(player, typeof(GoopStabilizer));
        _AnyoneHasGoopStabilizer = true;
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        if (player)
            PassiveItem.DecrementFlag(player, typeof(GoopStabilizer));
        _AnyoneHasGoopStabilizer = PassiveItem.IsFlagSetAtAll(typeof(GoopStabilizer));
    }

    [HarmonyPatch(typeof(GoopDefinition), nameof(GoopDefinition.GetLifespan))]
    private class GoopStabilizerPatch
    {
        //NOTE: using a static variable since this function can get called A LOT and checking a dictionary will be very slow
        static void Postfix(GoopDefinition __instance, ref float __result)
        {
            if (_AnyoneHasGoopStabilizer)
                __result *= _STABILIZATION_MULT;
        }
    }
}
