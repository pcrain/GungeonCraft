namespace CwaffingTheGungy;

public class SteadyHands : PassiveItem
{
    public static string ItemName         = "Steady Hands";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    public static int ID;

    private const float _RELOAD_FACTOR = 1.33f;

    public static void Init()
    {
        PickupObject item = Lazy.SetupPassive<SteadyHands>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);

        ID = item.PickupObjectId;
    }

    private static float ModifyReloadSpeedIfIdle(Gun gun)
    {
        if (gun.CurrentOwner is not PlayerController pc)
            return 1.0f;
        if (pc.m_playerCommandedDirection != Vector2.zero)
            return 1.0f;
        if (!pc.HasPassiveItem(SteadyHands.ID))
            return 1.0f;
        return SteadyHands._RELOAD_FACTOR;
    }

    private static float ModifyVisualReloadSpeedIfIdle(GameUIReloadBarController c)
    {
        if (c.m_attachPlayer is not PlayerController pc)
            return 1.0f;
        if (pc.m_playerCommandedDirection != Vector2.zero)
            return 1.0f;
        if (!pc.HasPassiveItem(SteadyHands.ID))
            return 1.0f;
        return SteadyHands._RELOAD_FACTOR;
    }

    /// <summary>Increase reload speed while standing still</summary>
    [HarmonyPatch(typeof(Gun), nameof(Gun.HandleReload), MethodType.Enumerator)]
    private class SteadyHandsPatch
    {
        [HarmonyILManipulator]
        private static void SteadyHandsIL(ILContext il, MethodBase original)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall("BraveTime", "get_DeltaTime")))
                return;

            cursor.Emit(OpCodes.Ldarg_0);  // load enumerator type
            // load actual Gun from "$this" field
            cursor.Emit(OpCodes.Ldfld, AccessTools.GetDeclaredFields(original.DeclaringType).Find(f => f.Name == "$this"));
            cursor.Emit(OpCodes.Call, typeof(SteadyHands).GetMethod("ModifyReloadSpeedIfIdle", BindingFlags.Static | BindingFlags.NonPublic));
            cursor.Emit(OpCodes.Mul);  // multiply deltatime by the steady hands reload factor
        }
    }

    /// <summary>Make sure reload bar visually matches</summary>
    [HarmonyPatch(typeof(GameUIReloadBarController), nameof(GameUIReloadBarController.HandlePlayerReloadBar), MethodType.Enumerator)]
    private class SteadyHandsUIPatch
    {
        [HarmonyILManipulator]
        private static void SteadyHandsUIIL(ILContext il, MethodBase original)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall("BraveTime", "get_DeltaTime")))
                return;  // this occurs twice, but we only care about the first one

            cursor.Emit(OpCodes.Ldarg_0);  // load enumerator type
            // load actual GameUIReloadBarController from "$this" field
            cursor.Emit(OpCodes.Ldfld, AccessTools.GetDeclaredFields(original.DeclaringType).Find(f => f.Name == "$this"));
            cursor.Emit(OpCodes.Call, typeof(SteadyHands).GetMethod("ModifyVisualReloadSpeedIfIdle", BindingFlags.Static | BindingFlags.NonPublic));
            cursor.Emit(OpCodes.Mul);  // multiply deltatime by the steady hands reload factor
        }
    }
}
