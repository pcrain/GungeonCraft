namespace CwaffingTheGungy;

public class BionicFinger : PassiveItem
{
    public static string ItemName         = "Bionic Finger";
    public static string SpritePath       = "bionic_finger_icon";
    public static string ShortDescription = "Trigger Happiest";
    public static string LongDescription  = "Allows semi-automatic weapons to automatically fire at their maximum manual fire rate.";
    public static string Lore             = "The latest and greatest in cyborg prosthetic technology. In addition to negating one of the only downsides of using semi-automatic weaponry, this finger has the added benefit of reducing the incidence rate of carpal tunnel syndrome and repetitive wrist strain among arms-bearers, making it a must-have for both the health-conscious and the lazy alike.";

    private static int _BionicFingerId;
    private static Hook _RemoveSemiautoCooldownHook;

    public static void Init()
    {
        PickupObject item  = Lazy.SetupPassive<BionicFinger>(ItemName, SpritePath, ShortDescription, LongDescription, Lore);
        item.quality       = ItemQuality.C;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);
        item.AddToSubShop(ModdedShopType.Rusty);

        _BionicFingerId   = item.PickupObjectId;

        _RemoveSemiautoCooldownHook = new Hook(
          typeof(Gunfiguration.QoLConfig).GetMethod("OverrideSemiAutoCooldown", BindingFlags.Static | BindingFlags.NonPublic),
          typeof(BionicFinger).GetMethod("OverrideSemiAutoCooldown", BindingFlags.Static | BindingFlags.NonPublic)
          );
    }

    private static float OverrideSemiAutoCooldown(Func<PlayerController, float> orig, PlayerController pc)
    {
        if (pc.passiveItems.Contains(_BionicFingerId))
            return 0f; // replace the value we're checking against with 0f to completely remove semi-automatic fake cooldown
        return orig(pc); // return the original value
    }
}

// ***old code for setting up new break labels. turned out not to be necessary, but keeping around for reference and posterity

// ILLabel vanillaTarget = null; // set up a new label for our new branch point
// cursor.Next.Next.Next.MatchBleUn(out vanillaTarget); //mark the vanilla branch point for later
// if (vanillaTarget != null)
//     ETGModConsole.Log($"  found vanila break target at {vanillaTarget.Target.Offset}");
// ILCursor cursor2 = new ILCursor(il); //cursor.Clone();
// cursor2.GotoLabel(vanillaTarget, MoveType.AfterLabel); // move to the vanilla branch point
// ILLabel replacementLabel = cursor2.MarkLabel(); // mark our own label
// cursor2.Index = 0;
// while (cursor2.TryGotoNext(MoveType.Before, instr => instr.MatchBleUn(vanillaTarget)))
// {
//     ETGModConsole.Log($"    replacing vanilla label at {cursor2.Next.Offset}");
//     cursor2.Remove();
//     cursor2.Emit(OpCodes.Ble_Un, replacementLabel);
// }
// cursor.Emit(OpCodes.Ble_Un, replacementLabel); // branch to our replacement target

// Hex number conversions
// var offset = int.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
