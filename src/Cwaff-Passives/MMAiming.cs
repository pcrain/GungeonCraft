namespace CwaffingTheGungy;

public class MMAiming : CwaffPassive
{
    public static string ItemName         = "MM: Aiming";
    public static string ShortDescription = "Chapter 2";
    public static string LongDescription  = "Spread is reduced by 50% while standing still.";
    public static string Lore             = "Every Gungeoneer seems to believe they can fend off the Gundead by just running around guns akimbo, and their occasional successes do nothing to dissuade these Rambo wannabes from poor shooting form. As outlined in the Aiming chapter of Manuel's Manual, an ancient technique known as 'standing still and taking the time to look where you are shooting' can dramatically improve any arms-bearer's accuracy in the heat of battle.";

    private const float _SPREAD_FACTOR = 0.5f;

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<MMAiming>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);
    }

    // NOTE: called by patch in CwaffPatches
    internal static float ModifySpreadIfIdle(float oldSpread, PlayerController player)
    {
        if ((player.m_activeActions.Move.Vector.sqrMagnitude > 0.1f) || !player.HasPassive<MMAiming>())
            return oldSpread;
        return _SPREAD_FACTOR * oldSpread;
    }
}
