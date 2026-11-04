namespace CwaffingTheGungy;

public class MidasAmmolet : CwaffBlankModificationItem, ICustomBlankDoer
{
    public static string ItemName         = "Midas' Ammolet";
    public static string ShortDescription = "Money from Projectiles";
    public static string LongDescription  = "Blanks have a 25% chance to transmute projectiles into casings. Grants 1 additional blank per floor.";
    public static string Lore             = "Projectiles in the Gungeon tend to be made from either metal or magic -- both prime targets for alchemical transmutation. A joint effort between ACNE Corporation's CFO and research team resulted in an ammolet that effectively prints money from projectiles. So far, hospital bills from the team's internal testing have far outweighed any profits, but an experienced Gungeoneer might have more success using it.";

    private const float _CASING_CHANCE = 0.25f;

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<MidasAmmolet>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.A;
        item.AddPassiveStatModifier(StatType.AdditionalBlanksPerFloor, 1f, StatModifier.ModifyMethod.ADDITIVE);
        item.AddToSubShop(ItemBuilder.ShopType.OldRed);
    }

    public void OnCustomBlankedProjectile(Projectile p)
    {
        if (UnityEngine.Random.value < _CASING_CHANCE || (this.m_owner && this.m_owner.HasSynergy(Synergy.MONEY_MAKES_THE_WORLD_GO_ROUND)))
          LootEngine.SpawnCurrency(p.SafeCenter, 1, false, Lazy.RandomVector(), null);
    }
}
