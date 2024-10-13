namespace CwaffingTheGungy;

public class ReserveAmmolet : CwaffBlankModificationItem, ICustomBlankDoer
{
    public static string ItemName         = "Reserve Ammolet";
    public static string ShortDescription = "Blanks Restore Ammo";
    public static string LongDescription  = "Blanks restore 1% ammo to the current gun per projectile cleared. Grants 1 additional blank per floor.";
    public static string Lore             = "The latest and greatest from the brilliant minds of ACNE Corporation's Ammolet Division. Instead of destroying projectiles like most off-the-shelf ammolets, ACNE's premium Reserve Ammolet uses a proprietary suite of technologies to halt the momentum of all hostile projectiles and siphon them into its wearer's arms (* to clarify for legal reasons, this is not referring to the wearer's biological arms, but their fire arms [** to clarify further for more legal reasons, this ammolet replenishes ammunition to guns, and does not require its wearer to set their arms on fire to function]).";

    private int _stashedAmmo = 0;

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<ReserveAmmolet>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.B;
        ItemBuilder.AddPassiveStatModifier(item, StatType.AdditionalBlanksPerFloor, 1f, StatModifier.ModifyMethod.ADDITIVE);
        item.AddToSubShop(ItemBuilder.ShopType.OldRed);
    }

    public override void Update()
    {
        base.Update();
        if (this._stashedAmmo <= 0)
            return;

        this.Owner.CurrentGun.GainAmmo(Mathf.CeilToInt(0.01f * this._stashedAmmo * this.Owner.CurrentGun.AdjustedMaxAmmo));
        this._stashedAmmo = 0;
    }

    public void OnCustomBlankedProjectile(Projectile p) => ++this._stashedAmmo;
}
