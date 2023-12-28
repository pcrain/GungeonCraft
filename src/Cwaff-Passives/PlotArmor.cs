namespace CwaffingTheGungy;

public class PlotArmor : PassiveItem
{
    public static string ItemName         = "Plot Armor";
    public static string SpritePath       = "plot_armor_icon";
    public static string ShortDescription = "Can't Die Yet";
    public static string LongDescription  = "Gain enough armor before every boss fight to have 3 total armor (4 for zero-health characters). Always grants at least 1 armor.";
    public static string Lore             = "The single most effective piece of armor ever created, under the right circumstances. The amount of protection it actually offers seems to vary from person to person over time, and the Gungeon's best blacksmiths are still trying to figure out how to fully harness the properties of \"plot\" to their advantage.";

    internal const int _MIN_PLAYER_ARMOR  = 3;
    internal const int _MIN_ARMOR_TO_GIVE = 1;

    private RoomHandler _lastVisitedRoom  = null;

    public static void Init()
    {
        PickupObject item = Lazy.SetupPassive<PlotArmor>(ItemName, SpritePath, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.A;
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

        int minArmor = _MIN_PLAYER_ARMOR + (this.Owner.ForceZeroHealthState ? 1 : 0);
        int currentArmor = (int)this.Owner.healthHaver.Armor;
        int armorToGain = Mathf.Max(_MIN_ARMOR_TO_GIVE, minArmor - currentArmor);
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
