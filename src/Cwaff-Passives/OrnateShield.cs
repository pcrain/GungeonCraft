namespace CwaffingTheGungy;

public class OrnateShield : CwaffPassive
{
    public static string ItemName         = "Ornate Shield";
    public static string ShortDescription = "Financial Security";
    public static string LongDescription  = "Adds a 0.1% chance to negate damage and trigger a mini blank for every casing the player possesses, up to a maximum of 50%.";
    public static string Lore             = "TBD";

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<OrnateShield>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.B;
        item.AddToShop(ModdedShopType.Ironside);
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        player.healthHaver.ModifyDamage += this.OnTakeDamage;
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        if (!player)
            return;
        player.healthHaver.ModifyDamage -= this.OnTakeDamage;
    }

    private void OnTakeDamage(HealthHaver hh, HealthHaver.ModifyDamageEventArgs data)
    {
        if (hh.gameActor is not PlayerController player)
          return;
        float blockChance = Mathf.Min(0.5f, player.carriedConsumables.Currency * 0.001f);
        if (UnityEngine.Random.value > blockChance)
          return;
        data.ModifiedDamage = 0f;
        hh.TriggerInvulnerabilityPeriod();
        Lazy.DoDamagedFlash(hh);
        player.DoColorfulMiniBlank(color: ExtendedColours.paleYellow);
        player.gameObject.PlayOnce("the_sound_of_financial_security");
    }
}
