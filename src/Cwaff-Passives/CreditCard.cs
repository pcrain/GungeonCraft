namespace CwaffingTheGungy;

public class CreditCard : CwaffPassive
{
    public static string ItemName         = "Credit Card";
    public static string ShortDescription = "Shop 'til You Drop";
    public static string LongDescription  = "Grants 500 shells while picked up. Grants 1 curse for every 50 shells below 500, and 1 coolness for every 50 shells above 500. Cannot be dropped when possessing fewer than 500 shells.";
    public static string Lore             = "Perhaps the greatest emblem of 20th century economics, this handy little piece of plastic gives unprecedented purchasing power for all of your Gungeon needs. Comes with the teensiest of interest rates, charged directly to your soul for your convenience.";

    internal const int _BASE_CREDIT      = 500;
    internal const int _CHEAT_DEATH_COST = 100;
    internal const int _CREDIT_DELTA     = 50;

    private int oldCurrency       = 0;
    private StatModifier curseMod = null;
    private StatModifier coolMod  = null;

    public static void Init()
    {
        PassiveItem item   = Lazy.SetupPassive<CreditCard>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality       = ItemQuality.B;
        item.AddToSubShop(ItemBuilder.ShopType.Cursula);
    }

    public override void OnFirstPickup(PlayerController player)
    {
        base.OnFirstPickup(player);
        this.curseMod = new StatModifier();
            curseMod.amount = 0f;
            curseMod.modifyType = StatModifier.ModifyMethod.ADDITIVE;
            curseMod.statToBoost = PlayerStats.StatType.Curse;
        this.coolMod = new StatModifier();
            coolMod.amount = 0f;
            coolMod.modifyType = StatModifier.ModifyMethod.ADDITIVE;
            coolMod.statToBoost = PlayerStats.StatType.Coolness;
        this.passiveStatModifiers = new []{curseMod, coolMod};
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        player.healthHaver.ModifyDamage += this.OnTakeDamage;
        oldCurrency = _BASE_CREDIT;
        player.carriedConsumables.Currency += _BASE_CREDIT;
        UpdateCreditScore();
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        if (!player)
            return;
        player.carriedConsumables.Currency -= _BASE_CREDIT;
        player.healthHaver.ModifyDamage -= this.OnTakeDamage;
    }

    private void OnTakeDamage(HealthHaver hh, HealthHaver.ModifyDamageEventArgs data)
    {
        if (!hh.PlayerWillDieFromHit(data))
            return;
        if (hh.GetComponent<PlayerController>() is not PlayerController player)
            return;
        if (player.carriedConsumables.Currency < _CHEAT_DEATH_COST)
            return;
        if (!player.HasSynergy(Synergy.DEATH_AND_TAXES))
            return;
        player.carriedConsumables.Currency -= _CHEAT_DEATH_COST;

        data.ModifiedDamage = 0f;
        hh.TriggerInvulnerabilityPeriod();
        Lazy.DoDamagedFlash(hh);
        player.DoGenericItemActivation(this.sprite, "minecraft_totem_pop_sound");
    }

    public override void Update()
    {
        base.Update();
        if (!this.Owner)
            return;
        UpdateCreditScore();
    }

    private void UpdateCreditScore()
    {
        int newCurrency = GameManager.Instance.PrimaryPlayer.carriedConsumables.Currency;
        if (oldCurrency == newCurrency)
            return;

        this.CanBeDropped = (newCurrency >= _BASE_CREDIT);
        curseMod.amount   = (newCurrency > _BASE_CREDIT) ? 0 : ((_BASE_CREDIT - newCurrency) / _CREDIT_DELTA);
        coolMod.amount    = (newCurrency < _BASE_CREDIT) ? 0 : ((newCurrency - _BASE_CREDIT) / _CREDIT_DELTA);
        this.Owner.stats.RecalculateStats(this.Owner);
    }
}
