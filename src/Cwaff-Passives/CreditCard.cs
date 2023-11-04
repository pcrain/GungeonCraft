namespace CwaffingTheGungy;

public class CreditCard : PassiveItem
{
    public static string ItemName         = "Credit Card";
    public static string SpritePath       = "credit_card_icon";
    public static string ShortDescription = "Shop 'til You Drop";
    public static string LongDescription  = "Grants 500 shells while picked up. Grants 1 curse for every 50 shells below 500, and 1 coolness for every 50 shells above 500. Cannot be dropped when possessing fewer than 500 shells.\n\nPerhaps the greatest emblem of 20th century economics, this handly little piece of plastic gives unprecedented purchasing power for all of your Gungeon needs. Comes with the teensiest of interest rates, charged directly to your soul for your convenience.";

    internal const int _BASE_CREDIT  = 500;
    internal const int _CREDIT_DELTA = 50;

    private int oldCurrency       = 0;
    private StatModifier curseMod = null;
    private StatModifier coolMod  = null;

    public static void Init()
    {
        PickupObject item  = Lazy.SetupPassive<CreditCard>(ItemName, SpritePath, ShortDescription, LongDescription);
        item.quality       = PickupObject.ItemQuality.B;
        item.AddToSubShop(ItemBuilder.ShopType.Cursula);
    }

    public override void Pickup(PlayerController player)
    {
        if (!this.m_pickedUpThisRun)
        {
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

        base.Pickup(player);
        oldCurrency = _BASE_CREDIT;
        GameManager.Instance.PrimaryPlayer.carriedConsumables.Currency += _BASE_CREDIT;
        UpdateCreditScore();
    }

    public override DebrisObject Drop(PlayerController player)
    {
        GameManager.Instance.PrimaryPlayer.carriedConsumables.Currency -= _BASE_CREDIT;
        return base.Drop(player);
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
