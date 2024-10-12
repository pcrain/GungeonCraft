namespace CwaffingTheGungy;

public class MMReloading : CwaffPassive
{
    public static string ItemName         = "MM: Reloading";
    public static string ShortDescription = "Chapter 1";
    public static string LongDescription  = "Guns reload 33% faster while standing still.";
    public static string Lore             = "Gungeoneers aren't particularly good at multitasking -- as evidenced by the number of ridiculous ways they tend to get hit while exploring -- and countless hours spent reloading while running around has ingrained rather subpar reloading techniques deeply into their muscle memory. A quick refresher from the Reloading chapter of Manuel's Manual is more than enough for most Gungeoneers to instill some semblance of discipline into their reloading practices.";

    private const float _RELOAD_FACTOR = 1.33f;

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<MMReloading>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);
    }

    private static float ModifyReloadSpeedIfIdle(Gun gun)
    {
        if (gun.CurrentOwner is not PlayerController pc)
            return 1.0f;
        if (pc.m_playerCommandedDirection != Vector2.zero)
            return 1.0f;
        if (!pc.HasPassive<MMReloading>())
            return 1.0f;
        return MMReloading._RELOAD_FACTOR;
    }

    private static float ModifyVisualReloadSpeedIfIdle(GameUIReloadBarController c)
    {
        if (c.m_attachPlayer is not PlayerController pc)
            return 1.0f;
        if (pc.m_playerCommandedDirection != Vector2.zero)
            return 1.0f;
        if (!pc.HasPassive<MMReloading>())
            return 1.0f;
        return MMReloading._RELOAD_FACTOR;
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
            cursor.Emit(OpCodes.Ldfld, original.DeclaringType.GetEnumeratorField("$this"));
            cursor.CallPrivate(typeof(MMReloading), nameof(MMReloading.ModifyReloadSpeedIfIdle));
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
            cursor.Emit(OpCodes.Ldfld, original.DeclaringType.GetEnumeratorField("$this"));
            cursor.CallPrivate(typeof(MMReloading), nameof(MMReloading.ModifyVisualReloadSpeedIfIdle));
            cursor.Emit(OpCodes.Mul);  // multiply deltatime by the steady hands reload factor
        }
    }
}
