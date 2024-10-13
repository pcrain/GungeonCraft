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
        { //REFACTOR: clean up to avoid using Remove()
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchAdd(), instr => instr.MatchStfld<PlayerController>("m_controllerSemiAutoTimer")))
            {
                /* the next four instructions after this point are as follows
                    [keep   ] IL_0272: ldarg.0
                    [keep   ] IL_0273: ldfld System.Single PlayerController::m_controllerSemiAutoTimer
                    [replace] IL_0278: call System.Single BraveInput::get_ControllerFakeSemiAutoCooldown()
                    [keep   ] 637 ... ble.un ... MonoMod.Cil.ILLabel
                */
                cursor.Index += 2; // skip the next two instructions so we still have m_controllerSemiAutoTimer on the stack
                cursor.Remove(); // remove the get_ControllerFakeSemiAutoCooldown() instruction
                cursor.Emit(OpCodes.Ldarg_0); // load the player instance as arg0
                cursor.CallPrivate(typeof(BionicFinger), nameof(OverrideSemiAutoCooldown)); // replace with our own custom hook
                break; // we only care about the first occurrence of this pattern in the function
            }
        }
    }

    private static float OverrideSemiAutoCooldown(PlayerController pc)
    {
      if (pc.HasPassive<BionicFinger>())
          return 0f; // replace the value we're checking against with 0f to completely remove semi-automatic fake cooldown
      return BraveInput.ControllerFakeSemiAutoCooldown; // return the original value
    }

    // NOTE: called by patch in CwaffPatches
    internal static float ModifySpreadIfSemiautomatic(float oldSpread, PlayerController player)
    {
        if (!player.CurrentGun || player.CurrentGun.DefaultModule.shootStyle != ShootStyle.SemiAutomatic)
            return oldSpread;
        return player.HasSynergy(Synergy.AIM_BOTS) ? 0f : oldSpread;
    }
}
