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

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        player.SetImmuneToExplosions(true, ItemName);
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        player.SetImmuneToExplosions(false, ItemName);
    }

    //NOTE: used by HandleExplosionPatch in CwaffPatches
    internal static float DoStuntHelmetBoost(float origForce, bool hasStuntHelment, PlayerController player)
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
