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

    internal static StatModifier _StuntStats = StatType.Damage.Add(_DAMAGE_ADD);

    private Coroutine _extantDamageBoostCoroutine = null;

    public static void Init()
    {
        PassiveItem item   = Lazy.SetupPassive<StuntHelmet>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality       = ItemQuality.C;
        item.AddToShop(ModdedShopType.Boomhildr);
    }

    [HarmonyPatch(typeof(Exploder), nameof(Exploder.HandleExplosion), MethodType.Enumerator)]
    private class StuntExplosionPatch
    {
        [HarmonyILManipulator]
        private static void StuntExplosionIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            VariableDefinition hasStuntHelmet = il.DeclareLocal<bool>(); // false by default

            #region Determine if player is wearing stunt helmet
                if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchStloc(13))) // PlayerController
                    return;
                cursor.Emit(OpCodes.Ldloc_S, (byte)13); // V_13 == the PlayerController
                cursor.CallPrivate(typeof(StuntHelmet), nameof(PlayerHasStuntHelmet));
                cursor.Emit(OpCodes.Stloc, hasStuntHelmet);
            #endregion

            #region Ignore all damage from explosions
                if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<PlayerController>("IsEthereal")))
                    return;
                cursor.Emit(OpCodes.Ldloc, hasStuntHelmet);
                cursor.CallPrivate(typeof(StuntExplosionPatch), nameof(Or));
            #endregion

            #region Override preventPlayerForce
                if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<ExplosionData>("preventPlayerForce")))
                    return;
                cursor.Emit(OpCodes.Ldloc, hasStuntHelmet);
                cursor.CallPrivate(typeof(StuntExplosionPatch), nameof(AndNot));
            #endregion

            #region Quadruple all knockback from explosions and provide a damage boost
                if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<ExplosionData>("force")))
                    return;
                cursor.Emit(OpCodes.Ldloc, hasStuntHelmet);
                cursor.Emit(OpCodes.Ldloc_S, (byte)13); // V_13 == the PlayerController
                cursor.CallPrivate(typeof(StuntHelmet), nameof(DoStuntHelmetBoost));
            #endregion
        }

        private static bool Or(bool val1, bool val2) => val1 || val2;
        private static bool AndNot(bool val1, bool val2) => val1 && !val2;
    }

    private static bool PlayerHasStuntHelmet(PlayerController player)
    {
        return player && player.HasPassive<StuntHelmet>();
    }

    private static float DoStuntHelmetBoost(float origForce, bool hasStuntHelment, PlayerController player)
    {
        if (!hasStuntHelment)
            return origForce;

        StuntHelmet helmet = player.GetPassive<StuntHelmet>();
        if (helmet._extantDamageBoostCoroutine != null)
        {
            player.ownerlessStatModifiers.Remove(_StuntStats);
            player.StopCoroutine(helmet._extantDamageBoostCoroutine);
        }
        helmet._extantDamageBoostCoroutine = player.StartCoroutine(HandleDamageBoost(player, helmet));
        return origForce * _EXPLOSION_FORCE_MULT;
    }

    private static IEnumerator HandleDamageBoost(PlayerController target, StuntHelmet helmet)
    {
        target.gameObject.PlayUnique("stunt_time");
        Material mat = SpriteOutlineManager.GetOutlineMaterial(target.sprite);
        if (mat)
            mat.SetColor(CwaffVFX._OverrideColorId, new Color(128f, 0f, 16f));
        target.ownerlessStatModifiers.Add(_StuntStats);
        target.stats.RecalculateStats(target);

        for (float elapsed = 0f; elapsed < _STUNT_TIME; elapsed += BraveTime.DeltaTime)
        {
            yield return null;
            if (!target || !target.AcceptingAnyInput)
                yield break;
        }

        if (mat)
            mat.SetColor(CwaffVFX._OverrideColorId, new Color(0f, 0f, 0f));
        target.ownerlessStatModifiers.Remove(_StuntStats);
        target.stats.RecalculateStats(target);

        helmet._extantDamageBoostCoroutine = null;
        yield break;
    }
}
