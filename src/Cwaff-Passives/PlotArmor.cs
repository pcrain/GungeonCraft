namespace CwaffingTheGungy;

public class PlotArmor : CwaffPassive
{
    public static string ItemName         = "Plot Armor";
    public static string ShortDescription = "Can't Die Yet";
    public static string LongDescription  = "Upon first entering a boss room foyer, spawns enough armor pickups to bring the player to 3 total armor (4 for zero-health characters). Always grants at least 1 armor.";
    public static string Lore             = "The single most effective piece of armor ever created, under the right circumstances. The amount of protection it actually offers seems to vary from person to person over time, and the Gungeon's best blacksmiths are still trying to figure out how to fully harness the properties of \"plot\" to their advantage.";

    internal const int _MIN_PLAYER_ARMOR  = 3;
    internal const int _MIN_ARMOR_TO_GIVE = 1;

    private RoomHandler _lastVisitedRoom  = null;

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<PlotArmor>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.A;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);
        item.AddToShop(ModdedShopType.Ironside);
    }

    public override void Update()
    {
        base.Update();
        if (!this.Owner || !this.Owner.healthHaver)
            return;

        RoomHandler room = this.Owner.CurrentRoom;
        if (room == this._lastVisitedRoom)
            return;

        this._lastVisitedRoom = room;
        if (room == null || room.hasEverBeenVisited || room != ChestTeleporterItem.FindBossFoyer())
            return;

        int minArmor = _MIN_PLAYER_ARMOR + (this.Owner.ForceZeroHealthState ? 1 : 0);
        int currentArmor = (int)this.Owner.healthHaver.Armor;
        int armorToGain = Mathf.Max(_MIN_ARMOR_TO_GIVE, minArmor - currentArmor);
        this.Owner.StartCoroutine(SpawnSomeArmor(room, armorToGain));

        if (!this.Owner.HasSynergy(Synergy.DEUS_EX_MACHINA))
            return;
        if (this.Owner.GetGun<ChekhovsGun>() is not ChekhovsGun chekhovGun)
            return;
        Gun gun = chekhovGun.gun;
        gun.GainAmmo(gun.AdjustedMaxAmmo - gun.CurrentAmmo);
        this.Owner.gameObject.Play("chekhovs_gun_launch_sound_alt");
    }

    private IEnumerator SpawnSomeArmor(RoomHandler room, int armorToGain)
    {
        for (int i = 0; i < armorToGain; ++i)
        {
            yield return new WaitForSeconds(0.33f);
            bool success;
            Vector2 armorSpot = room.GetCenteredVisibleClearSpot(2, 2, out success).ToVector2();
            if (!success)
                armorSpot = this.Owner.CenterPosition;
            LootEngine.SpawnItem(ItemHelper.Get(Items.Armor).gameObject, armorSpot, Vector2.zero, 0f, true, true, false);
        }
    }
}
