namespace CwaffingTheGungy;

/// <summary>Class mostly containing debug Harmony patches that can be commented / uncommented as needed</summary>
[HarmonyPatch]
internal static class CwaffDebug
{
    // private static int _DebugItemId => IDs.Pickups["racket_launcher"];
    // // private static int _DebugItemId => (int)Items.CoolantLeak;
    // // private static int _DebugItemId => (int)Items.Casey;
    // // private static int _DebugItemId => (int)Items.Ration;

    // [HarmonyPatch(typeof(RewardPedestal), nameof(RewardPedestal.DetermineContents))]
    // [HarmonyPrefix]
    // static void DebugRewardPedestalPatch(RewardPedestal __instance, PlayerController player)
    // {
    //   if (C.DEBUG_BUILD)
    //     __instance.contents = PickupObjectDatabase.GetById(_DebugItemId);
    // }

    // [HarmonyPatch(typeof(Chest), nameof(Chest.DetermineContents))]
    // [HarmonyPrefix]
    // static void DebugChestContentsPatch(Chest __instance, PlayerController player, int tierShift)
    // {
    //   if (C.DEBUG_BUILD)
    //     __instance.forceContentIds = new(){_DebugItemId};
    // }

    // [HarmonyPatch(typeof(ShopItemController), nameof(ShopItemController.InitializeInternal))]
    // [HarmonyPrefix]
    // static void DebugShopItemPatch(ShopItemController __instance, ref PickupObject i)
    // {
    //   if (C.DEBUG_BUILD)
    //     i = PickupObjectDatabase.GetById(_DebugItemId);
    // }

    // [HarmonyPatch(typeof(CustomShopItemController), nameof(CustomShopItemController.InitializeInternal))]
    // [HarmonyPrefix]
    // static void DebugCustomShopItemPatch(CustomShopItemController __instance, ref PickupObject i)
    // {
    //   if (C.DEBUG_BUILD)
    //     i = PickupObjectDatabase.GetById(_DebugItemId);
    // }
}
