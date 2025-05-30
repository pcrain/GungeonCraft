namespace CwaffingTheGungy;

public class GoopStabilizer : CwaffPassive
{
    public static string ItemName         = "Goop Stabilizer";
    public static string ShortDescription = "Probably Non-toxic";
    public static string LongDescription  = "Quadruples the lifetime of goops.";
    public static string Lore             = "The finest researchers the Gungeon has to offer have spent many sleepless minutes pondering the mechanics of goops. Why do they dissipate so quickly? Why are they so chemically reactive? Is cheese goop safe to eat? Investigations of the latter two questions have all ended in constipation, but a breakthrough in goop stabilization has led to this compound.";

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
