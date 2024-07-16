namespace CwaffingTheGungy;

public class DisplayStand : CwaffPassive
{
    public static string ItemName         = "Display Stand";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private static int displayStandId;

    private class PristineItem : MonoBehaviour
    {
        private PlayerItem _item;

        private void Start()
        {
            this._item = base.GetComponent<PlayerItem>();
        }
    }

    private class PristineGun : MonoBehaviour
    {
        private void Start()
        {

        }
    }

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<DisplayStand>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.D;

        displayStandId   = item.PickupObjectId;

        // new Hook(
        //     typeof(PlayerItem).GetMethod("Pickup", BindingFlags.Instance | BindingFlags.NonPublic),
        //     typeof(DisplayStand).GetMethod("OnPickupActive", BindingFlags.Static | BindingFlags.NonPublic)
        //     );

        // new Hook(
        //     typeof(Gun).GetMethod("Pickup", BindingFlags.Instance | BindingFlags.NonPublic),
        //     typeof(DisplayStand).GetMethod("OnPickupGun", BindingFlags.Static | BindingFlags.NonPublic)
        //     );
    }

    private static void OnPickupActive(Action<PlayerItem, PlayerController> orig, PlayerItem active, PlayerController player)
    {
        if (player.HasPassive<DisplayStand>() && !active.m_pickedUpThisRun)
            active.gameObject.AddComponent<PristineItem>();
        orig(active, player);
    }

    private static void OnPickupGun(Action<Gun, PlayerController> orig, Gun gun, PlayerController player)
    {
        if (player.HasPassive<DisplayStand>() && !gun.HasBeenPickedUp)
            gun.gameObject.AddComponent<PristineGun>();
        orig(gun, player);
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
    }

    public override DebrisObject Drop(PlayerController player)
    {
        // player.OnReceivedDamage -= this.OnReceivedDamage;
        return base.Drop(player);
    }

    public override void Update()
    {
        base.Update();
        if (!this.Owner)
            return;
    }
}
