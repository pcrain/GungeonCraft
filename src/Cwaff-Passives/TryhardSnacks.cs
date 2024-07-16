namespace CwaffingTheGungy;

public class TryhardSnacks : CwaffPassive
{
    public static string ItemName         = "Tryhard Snacks";
    public static string ShortDescription = "Spawn Camping";
    public static string LongDescription  = "Projectiles deal 10x damage to enemies that spawned in less than a second ago.";
    public static string Lore             = "TBD";

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

    public override DebrisObject Drop(PlayerController player)
    {
        player.PostProcessProjectile -= PostProcessProjectile;
        return base.Drop(player);
    }

    public override void OnDestroy()
    {
        if (this.Owner)
            this.Owner.PostProcessProjectile -= PostProcessProjectile;
        base.OnDestroy();
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
          if ((BraveTime.ScaledTimeSinceStartup - spawnTime) > _SPAWN_CAMP_TIME)
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
