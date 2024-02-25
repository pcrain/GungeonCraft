namespace CwaffingTheGungy;

public class ReserveAmmolet : BlankModificationItem
{
    public static string ItemName         = "Reserve Ammolet";
    public static string ShortDescription = "Blanks Restore Ammo";
    public static string LongDescription  = "Blanks restore 1% ammo to the current gun per projectile cleared.";
    public static string Lore             = "TBD";

    private int _stashedAmmo = 0;

    public static void Init()
    {
        PickupObject item = Lazy.SetupPassive<ReserveAmmolet>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;
        ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalBlanksPerFloor, 1f, StatModifier.ModifyMethod.ADDITIVE);
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

    private void OnCustomBlankedProjectile(Projectile p) => ++this._stashedAmmo;

    [HarmonyPatch(typeof(SilencerInstance), nameof(SilencerInstance.ProcessBlankModificationItemAdditionalEffects))]
    private class AmmoAmmoletProcessBlankModificationPatch
    {
        static void Postfix(SilencerInstance __instance, BlankModificationItem bmi, Vector2 centerPoint, PlayerController user)
        {
            if (bmi is not ReserveAmmolet ammoAmmolet)
                return;

            __instance.UsesCustomProjectileCallback = true;
            __instance.OnCustomBlankedProjectile += ammoAmmolet.OnCustomBlankedProjectile;
        }
    }
}
