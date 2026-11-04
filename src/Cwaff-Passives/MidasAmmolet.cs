namespace CwaffingTheGungy;

public class MidasAmmolet : CwaffBlankModificationItem, ICustomBlankDoer
{
    public static string ItemName         = "Midas' Ammolet";
    public static string ShortDescription = "Money from Projectiles";
    public static string LongDescription  = "Blanks have a 25% chance to transmute projectiles into casings. Grants 1 additional blank per floor.";
    public static string Lore             = "TBD";

    private const float _CASING_CHANCE = 0.25f;

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<MidasAmmolet>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.S;
        item.AddPassiveStatModifier(StatType.AdditionalBlanksPerFloor, 1f, StatModifier.ModifyMethod.ADDITIVE);
        item.AddToSubShop(ItemBuilder.ShopType.OldRed);
    }

    public void OnCustomBlankedProjectile(Projectile p)
    {
        if (UnityEngine.Random.value < _CASING_CHANCE)
          LootEngine.SpawnCurrency(p.SafeCenter, 1, false, Lazy.RandomVector(), null);
    }
}
