namespace CwaffingTheGungy;

public class Lichguard : CwaffPassive
{
    public static string ItemName         = "Lichguard";
    public static string ShortDescription = "Arena Ready";
    public static string LongDescription  = "Prevents most item-induced stat decreases. Empowers Sunderbuss and Macheening, and removes their negative side effects.";
    public static string Lore             = "An ancient artifact created by the first great gunsmith, Lord Kagreflak. Unlike most armor that protects the user from physical damage, this gauntlet was crafted to protect the user from the more ineffable magics and ailments that predominate the battlefield -- and as luck would have it, the Gungeon.";

    public static void Init()
    {
        PassiveItem item   = Lazy.SetupPassive<Lichguard>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality       = ItemQuality.B;
        item.IncreaseLootChance(typeof(Sunderbuss), 20f);
        item.IncreaseLootChance(typeof(Macheening), 20f);
    }

    [HarmonyPatch]
    private static class PreventHarmfulStatModificationsPatches
    {
        private static bool _CurrentStatsPlayerHasLichguard = false;
        private enum StatValues { IGNORE, HIGHER_IS_BETTER, LOWER_IS_BETTER }
        private static readonly StatValues[] _StatValues = [
            StatValues.HIGHER_IS_BETTER, // MovementSpeed
            StatValues.HIGHER_IS_BETTER, // RateOfFire
            StatValues.LOWER_IS_BETTER,  // Accuracy
            StatValues.IGNORE,           // Health
            StatValues.HIGHER_IS_BETTER, // Coolness
            StatValues.HIGHER_IS_BETTER, // Damage
            StatValues.HIGHER_IS_BETTER, // ProjectileSpeed
            StatValues.IGNORE,           // AdditionalGunCapacity
            StatValues.HIGHER_IS_BETTER, // AdditionalItemCapacity
            StatValues.HIGHER_IS_BETTER, // AmmoCapacityMultiplier
            StatValues.LOWER_IS_BETTER,  // ReloadSpeed
            StatValues.HIGHER_IS_BETTER, // AdditionalShotPiercing
            StatValues.HIGHER_IS_BETTER, // KnockbackMultiplier
            StatValues.LOWER_IS_BETTER,  // GlobalPriceMultiplier
            StatValues.IGNORE,           // Curse
            StatValues.IGNORE,           // PlayerBulletScale
            StatValues.HIGHER_IS_BETTER, // AdditionalClipCapacityMultiplier
            StatValues.HIGHER_IS_BETTER, // AdditionalShotBounces
            StatValues.HIGHER_IS_BETTER, // AdditionalBlanksPerFloor
            StatValues.HIGHER_IS_BETTER, // ShadowBulletChance
            StatValues.HIGHER_IS_BETTER, // ThrownGunDamage
            StatValues.HIGHER_IS_BETTER, // DodgeRollDamage
            StatValues.HIGHER_IS_BETTER, // DamageToBosses
            StatValues.LOWER_IS_BETTER,  // EnemyProjectileSpeedMultiplier
            StatValues.HIGHER_IS_BETTER, // ExtremeShadowBulletChance
            StatValues.HIGHER_IS_BETTER, // ChargeAmountMultiplier
            StatValues.HIGHER_IS_BETTER, // RangeMultiplier
            StatValues.HIGHER_IS_BETTER, // DodgeRollDistanceMultiplier
            StatValues.HIGHER_IS_BETTER, // DodgeRollSpeedMultiplier
            StatValues.HIGHER_IS_BETTER, // TarnisherClipCapacityMultiplier
            StatValues.HIGHER_IS_BETTER, // MoneyMultiplierFromEnemies
        ];

        private static float PreventHarmfulStatModifications(float oldMod, StatModifier stat)
        {
            if (!_CurrentStatsPlayerHasLichguard)
                return oldMod;

            int statType = (int)stat.statToBoost;
            if (statType >= _StatValues.Length || _StatValues[statType] == StatValues.IGNORE)
                return oldMod;

            bool higherIsBetter = _StatValues[statType] == StatValues.HIGHER_IS_BETTER;
            if (stat.modifyType == StatModifier.ModifyMethod.MULTIPLICATIVE)
            {
                if (higherIsBetter)
                    return Mathf.Max(oldMod, 1f);
                return Mathf.Min(oldMod, 1f);
            }
            if (stat.modifyType == StatModifier.ModifyMethod.ADDITIVE)
            {
                if (higherIsBetter)
                    return Mathf.Max(oldMod, 0f);
                return Mathf.Min(oldMod, 0f);
            }
            return oldMod;
        }

        [HarmonyPatch(typeof(PlayerStats), nameof(PlayerStats.ApplyStatModifier))]
        [HarmonyILManipulator]
        private static void PlayerStatsApplyStatModifierPatchIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<StatModifier>(nameof(StatModifier.amount))))
            {
                cursor.Emit(OpCodes.Ldarg_1); // StatModifier
                cursor.CallPrivate(typeof(PreventHarmfulStatModificationsPatches), nameof(PreventHarmfulStatModifications));
            }
        }

        [HarmonyPatch(typeof(PlayerStats), nameof(PlayerStats.RecalculateStatsInternal))]
        [HarmonyPrefix]
        private static void PlayerStatsRecalculateStatsInternalPrefix(PlayerStats __instance, PlayerController owner)
        {
            _CurrentStatsPlayerHasLichguard = owner.HasPassive<Lichguard>();
        }

        [HarmonyPatch(typeof(PlayerStats), nameof(PlayerStats.RecalculateStatsInternal))]
        [HarmonyILManipulator]
        private static void PlayerStatsRecalculateStatsInternalIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            int statLocal = 0;
            while (cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdloc(out statLocal),
                instr => instr.MatchLdfld<StatModifier>(nameof(StatModifier.amount))))
            {
                cursor.Emit(OpCodes.Ldloc, statLocal); // StatModifier
                cursor.CallPrivate(typeof(PreventHarmfulStatModificationsPatches), nameof(PreventHarmfulStatModifications));
            }
        }
    }
}
