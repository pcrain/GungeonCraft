namespace CwaffingTheGungy;

public class UtilityVest : CwaffPassive
{
    public static string ItemName         = "Utility Vest";
    public static string ShortDescription = "Pocket Protector";
    public static string LongDescription  = "When taking otherwise fatal damage, destroys the least valuable item in your inventory instead.";
    public static string Lore             = "Most Gungeoneers opt to bring the R&G department's classic Bag-O'-Holding model for stashing the ludicrous amount of guns and gear they accrue across the Gungeon's many floors. Although being able to carry an unlimited amount of items is already a pretty sweet deal, this latest evolution in hammerspace techno-magic automatically deploys your loot in the precise location that would block mortally-wounding projectiles. Reception to this model has been mixed, with Gungeoneers who prefer preserving their loot over their lives or vice versa being split 50-50.";

    public static void Init()
    {
        PickupObject item = Lazy.SetupPassive<UtilityVest>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);
        item.AddToSubShop(ModdedShopType.Ironside);
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        player.healthHaver.ModifyDamage += this.OnTakeDamage;
    }

    public override DebrisObject Drop(PlayerController player)
    {
        player.healthHaver.ModifyDamage -= this.OnTakeDamage;
        return base.Drop(player);
    }

    public override void OnDestroy()
    {
        if (this.Owner)
            this.Owner.healthHaver.ModifyDamage -= this.OnTakeDamage;
        base.OnDestroy();
    }

    private void OnTakeDamage(HealthHaver hh, HealthHaver.ModifyDamageEventArgs data)
    {
        if (!hh.PlayerWillDieFromHit(data))
            return; // if we're not going to die, we don't need to activate

        if (GetWorstItemWeCanScrapAsArmor() is not PickupObject worst)
            return; // we have no items to scrap (theoretically can't happen since this item is itself scrappable)

        UsedItemAsArmor(worst);
        data.ModifiedDamage = 0f;
    }

    private IEnumerator DecayOverTime(DebrisObject debris, float time)
    {
        for (float lifeleft = time; lifeleft > 0; lifeleft -= BraveTime.DeltaTime)
        {
            debris.sprite.renderer.SetAlpha(lifeleft / time);
            yield return null;
        }
        UnityEngine.Object.Destroy(debris.gameObject);
    }

    private void UsedItemAsArmor(PickupObject item)
    {
        this.Owner.gameObject.Play("sentry_shoot");
        this.Owner.ForceBlank();
        this.Owner.healthHaver.TriggerInvulnerabilityPeriod();

        DebrisObject debris = Lazy.MakeDebrisFromSprite(item.sprite, this.Owner.sprite.WorldCenter, new Vector2(4f, 4f));
            debris.doesDecay     = true;
            debris.decayOnBounce = 0.5f;
            debris.bounceCount   = 1;
            debris.breaksOnFall  = true;
            debris.canRotate     = true;
            debris.StartCoroutine(DecayOverTime(debris, 2f));

        // drop and destroy the item so we properly call the Drop() / Destroy() events and can't pick it back up
        if (item is Gun g)
            UnityEngine.Object.Destroy(this.Owner.ForceDropGun(g).gameObject);
        else if (item is PassiveItem p)
            UnityEngine.Object.Destroy(this.Owner.DropPassiveItem(p).gameObject);
        else if (item is PlayerItem i)
            UnityEngine.Object.Destroy(this.Owner.DropActiveItem(i).gameObject);
    }

    /* Item Priorities:
        -1: [undroppable item]
         0: Utility Vest
         1: S tier (+0.5 for empty gun)
         2: A tier (+0.5 for empty gun)
         3: B tier (+0.5 for empty gun)
         4: C tier (+0.5 for empty gun)
         5: D tier (+0.5 for empty gun)
         6: Junk
    */
    private float GetItemArmorPriority(PickupObject item)
    {
        if (!item.CanActuallyBeDropped(this.Owner))
            return -1f;
        if (item.PickupObjectId == IDs.Pickups["utility_vest"])
            return 0f;
        if (item.PickupObjectId == (int)Items.Junk)
            return 6f;
        float priority;
        switch(item.quality)
        {
            case ItemQuality.S: priority = 1f; break;
            case ItemQuality.A: priority = 2f; break;
            case ItemQuality.B: priority = 3f; break;
            case ItemQuality.C: priority = 4f; break;
            case ItemQuality.D: priority = 5f; break;
            default:
                return -1f; // unknown item quality
        }
        if (item is Gun g)
            return priority + ((g.CurrentAmmo == 0) ? 0.5f: 0f);
        return priority;
    }

    private PickupObject GetWorstItemWeCanScrapAsArmor()
    {
        float highestPriority  = -1;
        PickupObject worstItem = null;
        foreach(PickupObject item in this.Owner.AllItems())
        {
            float p = GetItemArmorPriority(item);
            if (p <= highestPriority)
                continue;
            worstItem       = item;
            highestPriority = p;
        }
        return worstItem;
    }

}
