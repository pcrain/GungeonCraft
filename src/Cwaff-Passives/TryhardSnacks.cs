namespace CwaffingTheGungy;

public class TryhardSnacks : CwaffPassive
{
    public static string ItemName         = "Tryhard Snacks";
    public static string ShortDescription = "Spawn Camping";
    public static string LongDescription  = "Projectiles deal 10x damage to enemies that spawned in less than a second ago.";
    public static string Lore             = "A bag of triangular snacks shaped in the image of Mt. Dew, a holy place where tryhards have made pilgrimages to hone their tryharding skills for generations. Those who make the pilgrimage learn to embody the core values of alertness, preparedness, ruthlessness, smack-talking, and teabagging. Each bite-sized morsel is crafted to refinforce these values, as well as one's stomach lining.";

    internal static Dictionary<AIActor, float> _EngageTimes = new();

    public static void Init()
    {
        PassiveItem item   = Lazy.SetupPassive<TryhardSnacks>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality       = ItemQuality.B;
        CwaffEvents.OnNewFloorFullyLoaded += ResetEngageDictionary;
    }

    private static void ResetEngageDictionary() => _EngageTimes.Clear();

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        player.PostProcessProjectile += PostProcessProjectile;
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        if (!player)
            return;
        player.PostProcessProjectile -= PostProcessProjectile;
    }

    private static void PostProcessProjectile(Projectile proj, float effectChanceScalar)
    {
        if (!proj.gameObject.GetComponent<TryhardDamage>())
            proj.gameObject.GetOrAddComponent<TryhardDamage>();
    }

    private class TryhardDamage : DamageAdjuster
    {
        private const float _TRYHARD_MULT    = 10f;
        private const float _SPAWN_CAMP_TIME = 1f;

        protected override float AdjustDamage(float currentDamage, Projectile proj, AIActor enemy)
        {
          if (!enemy.IsInReinforcementLayer)
            return currentDamage;
          if (!_EngageTimes.TryGetValue(enemy, out float spawnTime))
            return currentDamage;
          float activeTime = _SPAWN_CAMP_TIME;
          if (Lazy.AnyoneHasSynergy(Synergy.GAMER_REFLEXES))
            activeTime *= 2f;
          if ((BraveTime.ScaledTimeSinceStartup - spawnTime) > activeTime)
            return currentDamage;
          return _TRYHARD_MULT * currentDamage;
        }
    }

    [HarmonyPatch(typeof(AIActor), nameof(AIActor.OnEngaged))]
    private class TrackEngageTimePatch
    {
        static void Prefix(AIActor __instance, bool isReinforcement)
        {
            if (isReinforcement && !__instance.HasBeenEngaged)
                _EngageTimes[__instance] = BraveTime.ScaledTimeSinceStartup;
        }
    }
}
