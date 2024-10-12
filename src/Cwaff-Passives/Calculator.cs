namespace CwaffingTheGungy;

public class Calculator : CwaffPassive
{
    public static string ItemName         = "Calculator";
    public static string ShortDescription = "Fudging the Numbers";
    public static string LongDescription  = "Stackable active items come in double their normal stack size.";
    public static string Lore             = "An ordinary calculator, lost by one of ACNE corporation's accountants. Due to their short term memory and extreme reliance on technology for doing basic arithmetic, it's unlikely that the contents of the Gungeon shipments they're in charge of will be properly accounted for in the foreseeable future. Fortunately, ACNE's 'when in doubt, ship it out!' policy means any accounting errors usually err on the side of more free stuff for the Gungeon.";

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<Calculator>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;
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

