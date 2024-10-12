namespace CwaffingTheGungy;

public class StuntHelmet : CwaffPassive
{
    public static string ItemName         = "Stunt Helmet";
    public static string ShortDescription = "Aim for the Bushes";
    public static string LongDescription  = "Nullifies damage and quadruples knockback from all explosions that would otherwise affect the player. Increases base damage by 200% for 5 seconds after being pushed by an explosion.";
    public static string Lore             = "In the grand scheme of things, a helmet won't really protect you from catching ablaze when being shot through a ring of fire, shattering all of your bones after falling off a motorcycle, or most of the other things that can go wrong while performing stunts. No, the helmet is there because it makes you look cool, and because it makes you easier to replace after your likely untimely demise. Despite that, the completely unwarranted feelings of safety and confidence it instills in some people is enough to incite extremely high-risk, medium-reward behaviors they otherwise wouldn't be inclined to engage in. And heck, some of those people are even still alive, so maybe this helmet thing has something going for it after all.";

    const float _EXPLOSION_FORCE_MULT = 4f;
    const float _DAMAGE_ADD           = 2f;
    const float _STUNT_TIME           = 5f;

    internal static StatModifier _StuntStats;

    private Coroutine _extantDamageBoostCoroutine = null;

    public static void Init()
    {
        PassiveItem item   = Lazy.SetupPassive<StuntHelmet>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality       = ItemQuality.C;
        item.AddToShop(ModdedShopType.Boomhildr);

        _StuntStats = new StatModifier {
            amount      = _DAMAGE_ADD,
            statToBoost = PlayerStats.StatType.Damage,
            modifyType  = StatModifier.ModifyMethod.ADDITIVE
            };
    }

    [HarmonyPatch(typeof(Exploder), nameof(Exploder.HandleExplosion), MethodType.Enumerator)]
    private class StuntExplosionPatch
    {
        [HarmonyILManipulator]
        private static void StuntExplosionIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            // cursor.DumpILOnce("StuntExplosionIL");

            #region Ignore all damage from explosions
                ILLabel branchPoint = null;
                if (!cursor.TryGotoNext(MoveType.After,
                    instr => instr.MatchLdfld<PlayerController>("IsEthereal"),
                    instr => instr.MatchBrtrue(out branchPoint)))
                    return;

                // after finding the check that ignores explosion damage if we're ethereal, we also want to completely ignore damage if we have the Stunt Helmet
                cursor.Emit(OpCodes.Ldloc_S, (byte)13); // V_13 == the PlayerController
                cursor.CallPrivate(typeof(StuntHelmet), nameof(PlayerHasStuntHelmet));
                cursor.Emit(OpCodes.Brtrue, branchPoint);
            #endregion

            #region Quadruple all knockback from explosions
                ILLabel branchPoint2 = null;
                if (!cursor.TryGotoNext(MoveType.Before,
                    instr => instr.MatchLdfld<ExplosionData>("preventPlayerForce"),
                    instr => instr.MatchBrfalse(out branchPoint2)))
                    return;

                // IL_0a48: ldarg.0
                // IL_0a49: ldfld ExplosionData Exploder/<HandleExplosion>c__Iterator4::data
                // --------WE ARE NOW HERE---------
                // IL_0a4e: ldfld System.Boolean ExplosionData::preventPlayerForce
                // IL_0a53: brfalse IL_0a70
                // IL_0a58: ldloc.s V_10
                cursor.Index -= 2; // jump back to right after computing num2 = 1f - num / data.pushRadius;

                ILLabel branchToTakeIfNoHelmet = cursor.DefineLabel();
                cursor.Emit(OpCodes.Ldloc_S, (byte)13); // V_13 == the PlayerController
                cursor.CallPrivate(typeof(StuntHelmet), nameof(PlayerHasStuntHelmet));
                cursor.Emit(OpCodes.Brfalse, branchToTakeIfNoHelmet);
                    cursor.Emit(OpCodes.Ldloc_S, (byte)26); // V_26 == force multiplier
                    cursor.Emit(OpCodes.Ldc_R4, _EXPLOSION_FORCE_MULT);
                    cursor.Emit(OpCodes.Mul);
                    cursor.Emit(OpCodes.Stloc_S, (byte)26);
                    cursor.Emit(OpCodes.Ldloc_S, (byte)13);
                    cursor.CallPrivate(typeof(StuntHelmet), nameof(DoStuntHelmetBoost));
                    cursor.Emit(OpCodes.Br, branchPoint2);  // skip over the preventPlayerForce check entirely
                cursor.MarkLabel(branchToTakeIfNoHelmet); // move onto the preventPlayerForce check as normal
            #endregion
        }
    }

    private static bool PlayerHasStuntHelmet(PlayerController player)
    {
        return player && player.HasPassive<StuntHelmet>();
    }

    private static void DoStuntHelmetBoost(PlayerController player)
    {
        StuntHelmet helmet = player.GetPassive<StuntHelmet>();
        if (helmet._extantDamageBoostCoroutine != null)
        {
            player.ownerlessStatModifiers.Remove(_StuntStats);
            player.StopCoroutine(helmet._extantDamageBoostCoroutine);
        }
        helmet._extantDamageBoostCoroutine = player.StartCoroutine(HandleDamageBoost(player, helmet));
    }

    private static IEnumerator HandleDamageBoost(PlayerController target, StuntHelmet helmet)
    {
        target.gameObject.PlayUnique("stunt_time");
        Material mat = SpriteOutlineManager.GetOutlineMaterial(target.sprite);
        if (mat)
            mat.SetColor("_OverrideColor", new Color(128f, 0f, 16f));
        target.ownerlessStatModifiers.Add(_StuntStats);
        target.stats.RecalculateStats(target);

        for (float elapsed = 0f; elapsed < _STUNT_TIME; elapsed += BraveTime.DeltaTime)
        {
            yield return null;
            if (!target || !target.AcceptingAnyInput)
                yield break;
        }

        if (mat)
            mat.SetColor("_OverrideColor", new Color(0f, 0f, 0f));
        target.ownerlessStatModifiers.Remove(_StuntStats);
        target.stats.RecalculateStats(target);

        helmet._extantDamageBoostCoroutine = null;
        yield break;
    }
}
