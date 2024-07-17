namespace CwaffingTheGungy;

public class BulletproofTablecloth : CwaffPassive
{
    public static string ItemName         = "Bulletproof Tablecloth";
    public static string ShortDescription = "Kevlar for Dinner";
    public static string LongDescription  = "Makes flippable tables immune to bullets and most other sources of damage.";
    public static string Lore             = "TBD";

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<BulletproofTablecloth>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.B;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);
    }

    [HarmonyPatch(typeof(MajorBreakable), nameof(MajorBreakable.ApplyDamage))]
    private class MajorBreakableApplyDamagePatch
    {
        static bool Prefix(MajorBreakable __instance, float damage, Vector2 sourceDirection, bool isSourceEnemy, bool isExplosion, bool ForceDamageOverride)
        {
            if (__instance.gameObject.transform.parent is not Transform parent)
                return true; // call the original method
            if (!parent.gameObject.GetComponent<FlippableCover>())
                return true; // call the original method
            if (!Lazy.AnyoneHas<BulletproofTablecloth>())
                return true; // call the original method
            return false; // skip the original method
        }
    }

    //WARNING: commented out because you get stuck on tables if you dodge roll on them twice in quick succession...and i'm not about to patch that D:
    // [HarmonyPatch(typeof(MajorBreakable), nameof(MajorBreakable.Break))]
    // private class MajorBreakableBreakPatch
    // {
    //     static bool Prefix(MajorBreakable __instance, Vector2 sourceDirection)
    //     {
    //         if (__instance.gameObject.transform.parent is not Transform parent)
    //             return true; // call the original method
    //         if (!parent.gameObject.GetComponent<FlippableCover>())
    //             return true; // call the original method
    //         if (!Lazy.AnyoneHas<BulletproofTablecloth>())
    //             return true; // call the original method
    //         return false; // skip the original method
    //     }
    // }
}
