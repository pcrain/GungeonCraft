namespace CwaffingTheGungy;

public class VolcanicAmmolet : CwaffBlankModificationItem, ICustomBlankDoer
{
    public static string ItemName         = "Volcanic Ammolet";
    public static string ShortDescription = "Blanks Detonate Projectiles";
    public static string LongDescription  = "Blanks create miniature explosions at the center of each enemy projectile. Grants 1 additional blank per floor.";
    public static string Lore             = "The very talented senior engineers at ACNE corporation's Mt. Fuji Headquarters have discovered how to harness the natural power of volcanoes in Ammolet format...at least that's what their marketing team would have you believe. The reality is that igniting the gunpowder inside projectiles when destroying them is trivial, and most Ammolets have mechanisms that inhibit projectile explosions as a safety feature to comply with local consumer laws. It seems ACNE's legal team has gotten around this by slapping a 'not for retail' label on the Ammolet and giving it away as a free gift with 65-casing glasses of water.";

    private const float NORMAL_EXPLOSION_DELAY = 0.125f; // normal explosion queue delay
    private const float QUICK_EXPLOSION_DELAY  = 0.03125f; // make VolcanicAmmolet queued explosions faster

    private static float _ExplosionTimer = NORMAL_EXPLOSION_DELAY; // the current value we're using for explosion queueing timer

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<VolcanicAmmolet>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.B;
        ItemBuilder.AddPassiveStatModifier(item, StatType.AdditionalBlanksPerFloor, 1f, StatModifier.ModifyMethod.ADDITIVE);
        item.AddToSubShop(ItemBuilder.ShopType.OldRed);
    }

    public void OnCustomBlankedProjectile(Projectile p)
    {
        _ExplosionTimer = QUICK_EXPLOSION_DELAY;  // temporarily sets the queued explosion speed until the next time the queue is empty
        Exploder.Explode(p.transform.position, Scotsman._ScotsmanExplosion, Vector2.zero, ignoreQueues: false);
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
