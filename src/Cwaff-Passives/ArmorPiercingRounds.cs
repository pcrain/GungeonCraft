namespace CwaffingTheGungy;

public class ArmorPiercingRounds : BlankModificationItem
{
    public static string ItemName         = "Armor Piercing Rounds";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    public static int    ID;

    public static void Init()
    {
        PickupObject item = Lazy.SetupPassive<ArmorPiercingRounds>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.D;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);

        ID = item.PickupObjectId;
    }

    /// <summary>Make Armor Piercing Rounds change projectiles to Unstoppable damage and ignore invulnerability frames on collision</summary>
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.HandleDamage))]
    private class ArmorPiercingPatch
    {
        [HarmonyILManipulator]
        private static void ArmorPiercingIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            // VariableDefinition shouldPierce = il.DeclareLocal<bool>();

            cursor.Emit(OpCodes.Call, typeof(ArmorPiercingPatch).GetMethod("SanityCheck", BindingFlags.Static | BindingFlags.NonPublic));
            ETGModConsole.Log($"  patch started");

            // the original method returns early if ReflectProjectiles returns true, so patch that really quickly
            ILLabel cantReflect = null;
            if (!cursor.TryGotoNext(MoveType.After,
              instr => instr.MatchCallvirt<SpeculativeRigidbody>("get_ReflectProjectiles"),
              instr => instr.MatchBrfalse(out cantReflect)
              ))
                return;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Call, typeof(ArmorPiercingPatch).GetMethod("OwnerHasArmorPiercing", BindingFlags.Static | BindingFlags.NonPublic));
            cursor.Emit(OpCodes.Ldarg_1); // disable projectile reflection iff we have armor piercing
            cursor.Emit(OpCodes.Call, typeof(ArmorPiercingPatch).GetMethod("MaybeDisableProjectileReflection", BindingFlags.Static | BindingFlags.NonPublic));
            cursor.Emit(OpCodes.Brtrue, cantReflect);

            // the original method returns early if QueryInvulnerabilityFrame() returns true, so patch that really quickly
            ILLabel notInvulnerable = null;
            if (!cursor.TryGotoNext(MoveType.After,
              instr => instr.MatchCallvirt<tk2dSpriteAnimator>("QueryInvulnerabilityFrame"),
              instr => instr.MatchBrfalse(out notInvulnerable)
              ))
                return;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Call, typeof(ArmorPiercingPatch).GetMethod("OwnerHasArmorPiercing", BindingFlags.Static | BindingFlags.NonPublic));
            cursor.Emit(OpCodes.Brtrue, notInvulnerable);

            // now we need a BUNCH of complicated logic all because ignoreInvulnerabilityFrames is a constant, so we have to rewrite the stack
            if (!cursor.TryGotoNext(MoveType.After,
              instr => instr.MatchStloc(8), // V_8 == damageCategory
              instr => instr.MatchLdarg(2), // hitPixelCollider (parameter)
              instr => instr.MatchStloc(9)  // hitPixelCollider (local)
              ))
                return;

            // we're right before ApplyDamage(), HealthHaver obj is already on the stack
            ILLabel pierced = cursor.DefineLabel();
            ILLabel didNotPierce = cursor.DefineLabel();

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Call, typeof(ArmorPiercingPatch).GetMethod("OwnerHasArmorPiercing", BindingFlags.Static | BindingFlags.NonPublic));
            cursor.Emit(OpCodes.Brfalse, didNotPierce);

            cursor.Emit(OpCodes.Ldloc_S, (byte)4); // V_4 == damage
            cursor.Emit(OpCodes.Ldloc_S, (byte)5); // V_5 == velocity
            cursor.Emit(OpCodes.Ldloc_S, (byte)6); // V_6 == ownerName
            cursor.Emit(OpCodes.Ldloc_S, (byte)7); // V_7 == coreDamageTypes
            cursor.Emit(OpCodes.Ldc_I4, (int)DamageCategory.Unstoppable); // unstoppable damage
            cursor.Emit(OpCodes.Ldc_I4_1);         // ignoreInvulnerabilityFrames == true
            cursor.Emit(OpCodes.Ldloc_S, (byte)9); // V_9 == hit pixel collider
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, typeof(Projectile).GetField("ignoreDamageCaps", BindingFlags.Instance | BindingFlags.Public));
            cursor.Emit(OpCodes.Callvirt, typeof(HealthHaver).GetMethod("ApplyDamage", BindingFlags.Instance | BindingFlags.Public));
            cursor.Emit(OpCodes.Br, pierced);

            // IL_0218: ldarg.2      # hitPixelCollider (parameter)
            // IL_0219: stloc.s V_9  # hitPixelCollider (local)
            // IL_021b: ldloc.s V_4  # damage
            // IL_021d: ldloc.s V_5  # velocity
            // IL_021f: ldloc.s V_6  # ownerName
            // IL_0221: ldloc.s V_7  # coreDamageTypes
            // IL_0223: ldloc.s V_8  # damageCategory
            // IL_0225: ldc.i4.0     # false
            // IL_0226: ldloc.s V_9  # hitPixelCollider (local)
            // IL_0228: ldarg.0      # this
            // IL_0229: ldfld System.Boolean Projectile::ignoreDamageCaps # this.ignoreDamageCaps
            // IL_022e: callvirt System.Void HealthHaver::ApplyDamage(System.Single,UnityEngine.Vector2,System.String,CoreDamageTypes,DamageCategory,System.Boolean,PixelCollider,System.Boolean)

            cursor.MarkLabel(didNotPierce);
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<HealthHaver>("ApplyDamage")))
                return;
            cursor.MarkLabel(pierced);

            ETGModConsole.Log($"  patch done");
        }

        private static void SanityCheck()
        {
            ETGModConsole.Log($"HandleDamage called!");
        }

        private static bool OwnerHasArmorPiercing(Projectile p)
        {
            ETGModConsole.Log($"  checking should pierce!");
            if (p && p.Owner is PlayerController player && player.HasPassiveItem(ArmorPiercingRounds.ID))
            {
                return true;
            }
            return false;
        }

        private static bool MaybeDisableProjectileReflection(bool shouldDisable, SpeculativeRigidbody body)
        {
            if (shouldDisable)
            {
                ETGModConsole.Log($"disabling reflection");
                body.ReflectProjectiles = false;
                body.ReflectBeams = false;
            }
            return shouldDisable;
        }
    }
}
