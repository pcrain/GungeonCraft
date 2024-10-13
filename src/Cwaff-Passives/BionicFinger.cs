namespace CwaffingTheGungy;

public class BionicFinger : CwaffPassive
{
    public static string ItemName         = "Bionic Finger";
    public static string ShortDescription = "Trigger Happiest";
    public static string LongDescription  = "Allows semi-automatic weapons to automatically fire at their maximum manual fire rate.";
    public static string Lore             = "The latest and greatest in cyborg prosthetic technology. In addition to negating one of the only downsides of using semi-automatic weaponry, this finger has the added benefit of reducing the incidence rate of carpal tunnel syndrome and repetitive wrist strain among arms-bearers, making it a must-have for both the health-conscious and the lazy alike.";

    public static void Init()
    {
        PassiveItem item   = Lazy.SetupPassive<BionicFinger>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality       = ItemQuality.C;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);
        item.AddToShop(ModdedShopType.Rusty);
        item.AddToShop(ModdedShopType.Handy);
    }

    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.HandleGunFiringInternal))]
    private class RemoveSemiAutoCooldownPatch // Remove cooldown for semiautomatic weapons
    {
        [HarmonyILManipulator]
        private static void HandleGunFiringInternalIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<BraveInput>("get_ControllerFakeSemiAutoCooldown")))
                return;
            cursor.Emit(OpCodes.Ldarg_0); // PlayerController
            cursor.CallPrivate(typeof(BionicFinger), nameof(OverrideSemiAutoCooldown)); // replace with our own custom hook
        }
    }

    private static float OverrideSemiAutoCooldown(float oldCooldown, PlayerController pc)
    {
      return pc.HasPassive<BionicFinger>() ? 0f : oldCooldown;
    }

    // NOTE: called by patch in CwaffPatches
    internal static float ModifySpreadIfSemiautomatic(float oldSpread, PlayerController player)
    {
        if (!player.CurrentGun || player.CurrentGun.DefaultModule.shootStyle != ShootStyle.SemiAutomatic)
            return oldSpread;
        return player.HasSynergy(Synergy.AIM_BOTS) ? 0f : oldSpread;
    }
}
