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

/// <summary>Profiling helper class</summary>
public static class CwaffProfile
{
    private class ProfileData
    {
        public System.Diagnostics.Stopwatch watch = new();
        public long startBytes = 0;
        public string shortName = null;
    }

    private static readonly Dictionary<string, ProfileData> _ProfileData = new();

    public static void ProfileType<T>(this Harmony harmony)
    {
        Type t = typeof(T);
        MethodInfo Prefix = typeof(CwaffProfile).GetMethod(nameof(ProfileMethodPrefix), AccessTools.all);
        MethodInfo Postfix = typeof(CwaffProfile).GetMethod(nameof(ProfileMethodPostfix), AccessTools.all);
        foreach (MethodInfo mi in typeof(T).GetMethods())
        {
            if (mi.DeclaringType != t)
                continue;
            try
            {
                harmony.Patch(mi, prefix: new HarmonyMethod(Prefix), new HarmonyMethod(Postfix));
                ETGModConsole.Log($"  Patched {mi.FullDescription()}");
            }
            catch (Exception)
            {
                ETGModConsole.Log($"  Exception patching {mi.FullDescription()}");
            }
        }
    }

    private static readonly string _RX = @".*<.*::(.*)>";
    private static void ProfileMethodPrefix()
    {
        MethodBase m = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod();
        string name = m.Name;
        if (!_ProfileData.TryGetValue(name, out ProfileData pd))
        {
            pd = _ProfileData[name] = new();
            pd.shortName = Regex.Replace(name, _RX, "$1");
        }
        pd.watch.Reset();
        pd.watch.Start();
        pd.startBytes = GC.GetTotalMemory(false);
    }

    private static void ProfileMethodPostfix()
    {
        string name = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name;
        if (!_ProfileData.TryGetValue(name, out ProfileData pd))
            return;
        pd.watch.Stop();
        long bytesUsed = GC.GetTotalMemory(false) - pd.startBytes;
        if (bytesUsed <= 8192)
            return;
        System.Console.WriteLine($"  used {bytesUsed:8} bytes in {pd.watch.ElapsedTicks,8} ticks during {pd.shortName}");
    }
}
