namespace CwaffingTheGungy;

public class DemolitionContract : CwaffPassive
{
    public static string ItemName         = "Demolition Contract";
    public static string ShortDescription = "Kablooie!";
    public static string LongDescription  = "Every enemy killed by an explosion drops an extra casing.";
    public static string Lore             = "One key difference between a good demoman and a bad demoman is that a good demoman typically survives their first contracted demolition. On an unrelated note, this contract did not belong to a good demoman.";

    public static void Init()
    {
        PassiveItem item = Lazy.SetupPassive<DemolitionContract>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality     = ItemQuality.B;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        CustomActions.OnAnyHealthHaverDie += HandleEnemyDeath;
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        CustomActions.OnAnyHealthHaverDie -= HandleEnemyDeath;
    }

    private void HandleEnemyDeath(HealthHaver hh)
    {
        if (hh.aiActor is not AIActor enemy)
          return;
        if (hh.lastIncurredDamageSource != StringTableManager.GetEnemiesString("#EXPLOSION"))
          return;
        if (this.Owner is not PlayerController pc)
          return;
        LootEngine.SpawnCurrency(enemy.CenterPosition, pc.HasSynergy(Synergy.GLUED_BACK_TOGETHER_IN_HELL) ? 2 : 1);
    }
}
