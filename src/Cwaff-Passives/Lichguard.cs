namespace CwaffingTheGungy;

public class Lichguard : CwaffPassive
{
    public static string ItemName         = "Lichguard";
    public static string ShortDescription = "Arena Ready";
    public static string LongDescription  = "Empowers Sunderbuss and Macheening and removes their negative side effects.";
    public static string Lore             = "TBD";

    public static void Init()
    {
        PassiveItem item   = Lazy.SetupPassive<Lichguard>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality       = ItemQuality.B;
        item.IncreaseLootChance(typeof(Sunderbuss), 20f);
        item.IncreaseLootChance(typeof(Macheening), 20f);
    }
}
