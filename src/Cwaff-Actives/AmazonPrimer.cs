namespace CwaffingTheGungy;

public class AmazonPrimer : CwaffActive
{
    public static string ItemName         = "Amazon Primer";
    public static string ShortDescription = "Cancel Any* Time!";
    public static string LongDescription  = "Begins a Primer subscription when consumed. Primer subscription drains 5 casings per combat encounter, but doubles fire rate and projectile speed and slightly boosts damage. Per-room subscription cost increases by 5 casings each floor. Cannot be activated with fewer than 25 casings.";
    public static string Lore             = "Once upon a time, money couldn't buy firearm proficiency, and gun-toting peasants needed to practice things often described using buzzwords such as \"aiming,\" \"timing,\" and \"strategy\". Fortunately, the stone ages are behind us, and for a low** fee, you too can join the dozens of happy*** Primers and shoot with the best of them!\n\n*(any time you run out of money, your subscription will be automatically cancelled for your convenience)\n\n**(low fee increases as you descend lower into the Gungeon)\n\n***(happiness is subjective and relative)";

    internal static GameObject _PrimeLogo;

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<AmazonPrimer>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.A;

        _PrimeLogo = VFX.Create("prime_logo_overhead", anchor: Anchor.LowerCenter, emissivePower: 100f);
        FakeItem.Create<PrimerSubscription>();
    }

    public override bool CanBeUsed(PlayerController user)
    {
        return GameManager.Instance.PrimaryPlayer.carriedConsumables.Currency >= 25;
    }

    public override void DoEffect(PlayerController user)
    {
        user.AcquireFakeItem<PrimerSubscription>().Setup();
    }
}

public class PrimerSubscription : FakeItem
{
    internal const int _PRIME_SUB_COST  = 5;
    internal const int _FLOOR_INFLATION = 5;

    private PlayerController _primer        = null;
    private StatModifier[]   _primeBenefits = null;
    private int              _currentCost   = _PRIME_SUB_COST;

    public void Setup()
    {
        this._primer = base.Owner;
        this._primer.OnEnteredCombat += AnyPrimers;
        this._primer.OnRoomClearEvent += ThanksForPriming;
        GameManager.Instance.OnNewLevelFullyLoaded += Inflation;
        this._primeBenefits = [
            StatType.RateOfFire.Mult(2f),
            StatType.ProjectileSpeed.Mult(2f),
            StatType.Damage.Mult(1.25f),
            StatType.DamageToBosses.Mult(1.25f),
        ];
        DoPrimeVFX();
    }

    private void Inflation()
    {
        this._currentCost += _FLOOR_INFLATION;
    }

    private void AnyPrimers()
    {
        if (GameManager.Instance.PrimaryPlayer.carriedConsumables.Currency < this._currentCost)
        {
            this._primer.OnEnteredCombat -= AnyPrimers;
            this._primer.OnRoomClearEvent -= ThanksForPriming;
            GameManager.Instance.OnNewLevelFullyLoaded -= Inflation;
            this._primer.gameObject.Play("prime_ran_out");
            Lazy.CustomNotification("Primer Expired", "Thanks for Trying Amazon Primer", Lazy.Pickup<AmazonPrimer>().sprite);
            UnityEngine.Object.Destroy(this._primer.DropPassiveItem(this).gameObject);
            return;
        }

        GameManager.Instance.PrimaryPlayer.carriedConsumables.Currency -= this._currentCost;
        foreach (StatModifier stat in this._primeBenefits)
            this._primer.ownerlessStatModifiers.Add(stat);
        this._primer.stats.RecalculateStats(this._primer);
        DoPrimeVFX();
    }

    private void ThanksForPriming(PlayerController player)
    {
        if (player != this._primer)
            return;
        foreach (StatModifier stat in this._primeBenefits)
            this._primer.ownerlessStatModifiers.Remove(stat);
        this._primer.stats.RecalculateStats(this._primer);
    }

    private void DoPrimeVFX()
    {
        this._primer.FlashVFXAbovePlayer(AmazonPrimer._PrimeLogo, sound: "prime_sound");
    }

    public override void MidGameSerialize(List<object> data)
    {
        base.MidGameSerialize(data);
        data.Add(this._currentCost);
    }

    public override void MidGameDeserialize(List<object> data)
    {
        base.MidGameDeserialize(data);
        this._currentCost = (int)data[0];
        this.Setup();
    }
}
