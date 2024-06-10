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
        PickupObject item = Lazy.SetupPassive<MMAiming>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);
    }

    private static float ModifySpreadIfIdle(float oldSpread, PlayerController player)
    {
        // if (player.m_playerCommandedDirection != Vector2.zero) //NOTE: this happens inside HandlePlayerInput(), where m_playerCommandedDirection is always zero
        if ((player.m_activeActions.Move.Vector.sqrMagnitude > 0.1f) || !player.HasPassive<MMAiming>())
            return oldSpread;
        return _SPREAD_FACTOR * oldSpread;
    }

    /// <summary>Increase reload speed while standing still</summary>
    [HarmonyPatch(typeof(Gun), nameof(Gun.ShootSingleProjectile))]
    private class ReduceSpreadWhenIdlePatch
    {
        [HarmonyILManipulator]
        private static void ReduceSpreadWhenIdleIL(ILContext il, MethodBase original)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdcI4(2),
                instr => instr.MatchCallvirt<PlayerStats>("GetStatValue")))
                return;

            cursor.Emit(OpCodes.Ldloc_0);  // load PlayerController type
            cursor.Emit(OpCodes.Call, typeof(MMAiming).GetMethod("ModifySpreadIfIdle", BindingFlags.Static | BindingFlags.NonPublic));
        }
    }
}
