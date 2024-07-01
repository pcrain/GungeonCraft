namespace CwaffingTheGungy;

public class VolcanicAmmolet : CwaffBlankModificationItem
{
    public static string ItemName         = "Volcanic Ammolet";
    public static string ShortDescription = "Blanks Detonate Projectiles";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const float NORMAL_EXPLOSION_DELAY = 0.125f; // normal explosion queue delay
    private const float QUICK_EXPLOSION_DELAY  = 0.03125f; // make VolcanicAmmolet queued explosions faster

    private static float _ExplosionTimer = NORMAL_EXPLOSION_DELAY; // the current value we're using for explosion queueing timer

    public static void Init()
    {
        PickupObject item = Lazy.SetupPassive<VolcanicAmmolet>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.A;
        ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalBlanksPerFloor, 1f, StatModifier.ModifyMethod.ADDITIVE);
        item.AddToSubShop(ItemBuilder.ShopType.OldRed);
    }

    private void OnCustomBlankedProjectile(Projectile p)
    {
        _ExplosionTimer = QUICK_EXPLOSION_DELAY;  // temporarily sets the queued explosion speed until the next time the queue is empty
        Exploder.Explode(p.transform.position, Scotsman._ScotsmanExplosion, Vector2.zero, ignoreQueues: false);
    }

    [HarmonyPatch(typeof(SilencerInstance), nameof(SilencerInstance.ProcessBlankModificationItemAdditionalEffects))]
    private class AmmoAmmoletProcessBlankModificationPatch
    {
        static void Postfix(SilencerInstance __instance, BlankModificationItem bmi, Vector2 centerPoint, PlayerController user)
        {
            if (bmi is not VolcanicAmmolet volcanicAmmolet)
                return;

            __instance.UsesCustomProjectileCallback = true;
            __instance.OnCustomBlankedProjectile += volcanicAmmolet.OnCustomBlankedProjectile;
        }
    }

    // keep accelerated explosion queue rate until after our queue is depleted
    [HarmonyPatch(typeof(ExplosionManager), nameof(ExplosionManager.Dequeue))]
    private class ExplosionManagerDequeuePatch
    {
        static void Postfix(ExplosionManager __instance)
        {
            if (__instance.m_queue.Count == 0)
                _ExplosionTimer = NORMAL_EXPLOSION_DELAY;
            if (__instance.m_timer > _ExplosionTimer) // only reduce the queue time, don't increase it
                __instance.m_timer = _ExplosionTimer;
        }
    }
}
