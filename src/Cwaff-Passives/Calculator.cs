namespace CwaffingTheGungy;

public class Calculator : CwaffPassive
{
    public static string ItemName         = "Calculator";
    public static string ShortDescription = "Fudging the Numbers";
    public static string LongDescription  = "Stackable active items come in double their normal stack size.";
    public static string Lore             = "TBD";

    public static void Init()
    {
        PickupObject item = Lazy.SetupPassive<Calculator>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.B;
    }

    [HarmonyPatch(typeof(PlayerItem), nameof(PlayerItem.Pickup))]
    private class PlayerItemPickupPatch
    {
        static void Prefix(PlayerItem __instance, PlayerController player)
        {
            if (__instance.m_pickedUp || __instance.m_pickedUpThisRun || !__instance.consumable || !__instance.canStack || __instance.numberOfUses < 1)
                return;
            int copies = 0;
            for (int i = 0; i < player.passiveItems.Count; ++i)
              if (player.passiveItems[i] is Calculator)
                ++copies;
            __instance.numberOfUses *= (copies + 1);
            __instance.m_cachedNumberOfUses = __instance.numberOfUses;
        }
    }
}

