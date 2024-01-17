namespace CwaffingTheGungy;

public class AmmoConservationManual : PassiveItem
{
    public static string ItemName         = "Ammo Conservation Manual";
    public static string ShortDescription = "Waste Not, Want Not";
    public static string LongDescription  = "Picking up any ammo box that would fill the current gun past 100% ammo will conserve half of any overfilled ammo for later. Spawns an extra ammo box for every 100% ammo conserved.";
    public static string Lore             = "The immediate reaction of most unenlightened Gungeoneers upon encountering an ammo box is to dump all of the ammo out of their weapon before replenishing it with the ammo in the box. The pages of this manual are completely blank aside from a single line on the first page that reads 'hey moron, you can just replace the missing ammo and save the rest for later.' Seeing as the manual's a bestseller at the local bookstore, apparently a lot of people needed these words of wisdom.";
    public static int    ID;

    private const float _PERCENT_AMMO_TO_CONSERVE = 0.5f;

    private float _conservedAmmo = 0.0f;

    public static void Init()
    {
        PickupObject item = Lazy.SetupPassive<AmmoConservationManual>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.B;

        ID = item.PickupObjectId;
        new Hook(
            typeof(AmmoPickup).GetMethod("Pickup", BindingFlags.Instance | BindingFlags.Public),
            typeof(AmmoConservationManual).GetMethod("OnAmmoPickup", BindingFlags.Static | BindingFlags.NonPublic)
            );
    }

    private static void OnAmmoPickup(Action<AmmoPickup, PlayerController> orig, AmmoPickup ammo, PlayerController p)
    {
        AmmoConservationManual manual = p.GetPassive<AmmoConservationManual>();
        if (!manual || p.CurrentGun.InfiniteAmmo || p.CurrentGun.AdjustedMaxAmmo <= 0)
        {
            orig(ammo, p);
            return;
        }

        float currentAmmoPercent = (float)p.CurrentGun.CurrentAmmo / (float)p.CurrentGun.AdjustedMaxAmmo;
        orig(ammo, p); // need to calculate ammo percent before actually doing the pickup

        float extraAmmo = ammo.mode switch {
            AmmoPickup.AmmoPickupMode.FULL_AMMO   => currentAmmoPercent,
            AmmoPickup.AmmoPickupMode.SPREAD_AMMO => Mathf.Max(0f, currentAmmoPercent - 0.5f),
            _                                     => 0f,
        };
        manual._conservedAmmo += _PERCENT_AMMO_TO_CONSERVE * extraAmmo;
        if (manual._conservedAmmo < 1f)
            return;

        manual._conservedAmmo -= 1f;
        LootEngine.SpawnItem(ItemHelper.Get(Items.Ammo).gameObject, p.CenterPosition, Vector2.zero, 0f, doDefaultItemPoof: true);
        // p.DoGenericItemActivation(manual);
    }

    public override void MidGameSerialize(List<object> data)
    {
        base.MidGameSerialize(data);
        data.Add(this._conservedAmmo);
    }

    public override void MidGameDeserialize(List<object> data)
    {
        base.MidGameDeserialize(data);
        this._conservedAmmo = (float)data[0];
    }
}
