namespace CwaffingTheGungy;

public class BubbleWand : CwaffPassive
{
    public static string ItemName         = "Bubble Wand";
    public static string ShortDescription = "Bring It Around Town";
    public static string LongDescription  = "Upon entering combat, each enemy has a 50% chance of having their held gun replaced with a short-ranged Bubble Blaster.";
    public static string Lore             = "Bubble blowing is a surprisingly popular pastime among the Gundead -- at least for those who have hands -- yet it is rare for Gungeoneers to actually encounter any Gundead enjoying their bubbles. It is believed that they are rather self-conscious about their below-average bubble-blowing abilities, and that showing a shared interest in their passion might be enough to get some of them to open up a bit more.";

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<BubbleWand>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.B;
        item.AddToSubShop(ItemBuilder.ShopType.Goopton);
    }

    [HarmonyPatch(typeof(AIActor), nameof(AIActor.Start))]
    private class BubbleWandPatch
    {
        static void Prefix(AIActor __instance)
        {
            if (!Lazy.AnyoneHas<BubbleWand>())
                return;
            if (__instance.healthHaver is not HealthHaver hh || hh.IsBoss || hh.IsSubboss)
                return;
            if (Lazy.CoinFlip())
                __instance.ReplaceGun(Items.BubbleBlaster);
            else if (GameManager.Instance.PrimaryPlayer.HasSynergy(Synergy.DUBBLE_BUBBLE) && Lazy.CoinFlip())
                __instance.ReplaceGun(Items.BubbleBlaster);
            else if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER && GameManager.Instance.SecondaryPlayer.HasSynergy(Synergy.DUBBLE_BUBBLE) && Lazy.CoinFlip())
                __instance.ReplaceGun(Items.BubbleBlaster);
        }
    }
}
