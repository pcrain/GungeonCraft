namespace CwaffingTheGungy;

public class ReflexAmmolet : BlankModificationItem
{
    public static string ItemName         = "Reflex Ammolet";
    public static string ShortDescription = "Blanks Return Fire";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";
    public static int    ID;

    public static void Init()
    {
        PickupObject item = Lazy.SetupPassive<ReflexAmmolet>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.B;
        ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalBlanksPerFloor, 1f, StatModifier.ModifyMethod.ADDITIVE);
        item.AddToSubShop(ItemBuilder.ShopType.OldRed);

        ID = item.PickupObjectId;
    }

    /// <summary>Make reflex ammolet reflect all bullets by hijacking the Elder Blanks synergy</summary>
    [HarmonyPatch(typeof(SilencerInstance), nameof(SilencerInstance.TriggerSilencer))]
    private class ReflexAmmoletReflectPatch
    {
        private static bool BlankShouldReflect(bool orig, PlayerController user) => orig || user.HasPassiveItem(ReflexAmmolet.ID);

        [HarmonyILManipulator]
        private static void ReflexAmmoletIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            int user = 0;
            if (!cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdarg(out user),
                instr => instr.MatchLdcI4(383), // elder blanks synergy
                instr => instr.MatchLdcI4(0),
                instr => instr.MatchCallvirt<PlayerController>("HasActiveBonusSynergy")
                ))
                return;

            cursor.Emit(OpCodes.Ldarg, user);
            cursor.Emit(OpCodes.Call, typeof(ReflexAmmoletReflectPatch).GetMethod("BlankShouldReflect", BindingFlags.Static | BindingFlags.NonPublic));
        }
    }

    /// <summary>Increase the minimum reflect speed from elder bullets</summary>
    [HarmonyPatch(typeof(SilencerInstance), nameof(SilencerInstance.DestroyBulletsInRange))]
    private class ReflexAmmoletSpeedPatch
    {
        private static float ReflectSpeed(float orig, PlayerController user) => user.HasPassiveItem(ReflexAmmolet.ID) ? 50f : orig;

        [HarmonyILManipulator]
        private static void ReflexAmmoletIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.DumpIL();

            int user = 0;
            if (!cursor.TryGotoNext(MoveType.Before,
                instr => instr.MatchLdarg(out user),  // figure out which player is reflecting
                instr => instr.MatchLdcR4(10f),       // default minimum speed for ReflectBullet is 10f
                instr => instr.MatchLdcR4(1f),        // default parameter to ReflectBullet()
                instr => instr.MatchLdcR4(1f),        // default parameter to ReflectBullet()
                instr => instr.MatchLdcR4(0f),        // default parameter to ReflectBullet()
                instr => instr.MatchCall<PassiveReflectItem>("ReflectBullet")
                ))
                return;

            cursor.Index += 2; // skip over playercontroller and float
            cursor.Emit(OpCodes.Ldarg, user);  // push playercontroller back on the stack
            // increase speed to 50 if we have Reflex Ammolet
            cursor.Emit(OpCodes.Call, typeof(ReflexAmmoletSpeedPatch).GetMethod("ReflectSpeed", BindingFlags.Static | BindingFlags.NonPublic));
        }
    }
}
