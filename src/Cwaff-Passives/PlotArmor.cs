namespace CwaffingTheGungy;

public class PlotArmor : PassiveItem
{
    public static string ItemName         = "Plot Armor";
    public static string SpritePath       = "plot_armor_icon";
    public static string ShortDescription = "Can't Die Yet";
    public static string LongDescription  = $"Gain enough armor before every boss fight to have 3 total armor (4 for zero-health characters). Always grants at least 1 armor.\n\nThe single most effective piece of armor ever created, under the right circumstances. The amount of protection it actually offers seems to vary from person to person over time, and the Gungeon's best blacksmiths are still trying to figure out how to fully harness the properties of \"plot\" to their advantage.";

    internal const int _MIN_ARMOR = 3;

    private RoomHandler _lastVisitedRoom = null;

    public static void Init()
    {
        PickupObject item = Lazy.SetupPassive<PlotArmor>(ItemName, SpritePath, ShortDescription, LongDescription);
        item.quality      = PickupObject.ItemQuality.S;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);
        item.AddToSubShop(ModdedShopType.Ironside);
    }

    public override void Update()
    {
        base.Update();
        if (!this.Owner)
            return;

        RoomHandler room = this.Owner.CurrentRoom;
        if (room == this._lastVisitedRoom)
            return;

        this._lastVisitedRoom = room;
        if (room.hasEverBeenVisited || room != ChestTeleporterItem.FindBossFoyer())
            return;

        int currentArmor = (int)this.Owner.healthHaver.Armor;
        if (this.Owner.ForceZeroHealthState)
            currentArmor -= 1; // Robot gets set to 4 armor
        int armorToGain = Mathf.Max(1, _MIN_ARMOR - currentArmor);
        this.Owner.StartCoroutine(SpawnSomeArmor(room, armorToGain));
    }

    private IEnumerator SpawnSomeArmor(RoomHandler room, int armorToGain)
    {
        for (int i = 0; i < armorToGain; ++i)
        {
            yield return new WaitForSeconds(0.33f);
            bool success;
            Vector2 armorSpot = room.GetCenteredVisibleClearSpot(2, 2, out success).ToVector2();
            if (!success)
                armorSpot = this.Owner.sprite.WorldCenter;
            LootEngine.SpawnItem(ItemHelper.Get(Items.Armor).gameObject, armorSpot, Vector2.zero, 0f, true, true, false);
        }
    }
}
