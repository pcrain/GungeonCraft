namespace CwaffingTheGungy;

public class WeddingRing : CwaffPassive
{
    public static string ItemName         = "Wedding Ring";
    public static string ShortDescription = "Commitment";
    public static string LongDescription  = "Every enemy killed without switching guns grants 1% boosts to damage, reload speed, and chance not to consume ammo, up to a maximum of 50% each. Boosts are reset upon firing another gun.";
    public static string Lore             = "Whether it is legal and/or ethical to marry a gun has been the topic of a surprising number of conversations in the Breach and the Gungeon, with the general consensus seeming to be: \"well...um...probably, yes, but it's weird as heck!\" Regardless of its legality, ethics, or sanity, more than one Gungeoneer has slapped a wedding ring on their favorite gun. And whether attributable to the placebo effect, madness, or empirical results, these gunnymooners have reported that their loyalty brings out the best in both them and their guns.";

    private const float _BONUS_PER_KILL     = 0.01f;
    private const float _MAX_BONUS          = 1.50f;

    private int            _committedGunId  = -1;
    private float          _commitmentMult  = 1.00f;
    private int            _lastKnownAmmo   = 0;
    private bool           _refundAmmo      = false;

    public static void Init()
    {
        PassiveItem item   = Lazy.SetupPassive<WeddingRing>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality       = ItemQuality.C;
        item.AddToSubShop(ModdedShopType.Rusty);
        item.passiveStatModifiers = new StatModifier[] {
            new StatModifier {
                amount      = 1.00f,
                statToBoost = PlayerStats.StatType.ReloadSpeed,
                modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE},
            new StatModifier {
                amount      = 1.00f,
                statToBoost = PlayerStats.StatType.Damage,
                modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE},
            new StatModifier {
                amount      = 1.00f,
                statToBoost = PlayerStats.StatType.DamageToBosses,
                modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE},
        };
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        player.OnPreFireProjectileModifier += this.ChanceToRefundAmmo;
        player.PostProcessProjectile += this.PostProcessProjectile;
        player.OnKilledEnemy += this.OnKilledEnemy;
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        if (!player)
            return;
        player.OnKilledEnemy -= this.OnKilledEnemy;
        player.PostProcessProjectile -= this.PostProcessProjectile;
        player.OnPreFireProjectileModifier -= this.ChanceToRefundAmmo;
        UpdateCommitmentStats(player, reset: true);
    }

    private void UpdateCommitmentStats(PlayerController player, bool reset = false)
    {
        this._commitmentMult = reset ? 1.00f : Mathf.Min(this._commitmentMult + _BONUS_PER_KILL, _MAX_BONUS);
        foreach (StatModifier stat in this.passiveStatModifiers)
            stat.amount = (stat.statToBoost == PlayerStats.StatType.ReloadSpeed) ? (1.0f / this._commitmentMult) : this._commitmentMult;
        player.stats.RecalculateStats(player);
    }

    private void OnKilledEnemy(PlayerController player)
    {
        UpdateCommitmentStats(player);
    }

    private Projectile ChanceToRefundAmmo(Gun gun, Projectile projectile)
    {
        this._refundAmmo    = UnityEngine.Random.value < (this._commitmentMult - 1.00f);
        this._lastKnownAmmo = this.Owner.CurrentGun.CurrentAmmo;
        return projectile;
    }

    private void PostProcessProjectile(Projectile proj, float effectChanceScalar)
    {
        if (this.Owner is not PlayerController player)
            return;
        if (player.CurrentGun.PickupObjectId == this._committedGunId)
            return;

        UpdateCommitmentStats(player, reset: true);
        this._committedGunId = player.CurrentGun.PickupObjectId;
        this._refundAmmo = false;
    }

    private void LateUpdate()
    {
        if (!this._refundAmmo || !this.Owner || !this.Owner.CurrentGun)
            return;

        this._refundAmmo = false;
        this.Owner.CurrentGun.CurrentAmmo = this._lastKnownAmmo;
    }

    public override void MidGameSerialize(List<object> data)
    {
        base.MidGameSerialize(data);
        data.Add(this._committedGunId);
        data.Add(this._commitmentMult);
        data.Add(this._lastKnownAmmo);
    }

    public override void MidGameDeserialize(List<object> data)
    {
        base.MidGameDeserialize(data);
        this._committedGunId = (int)data[0];
        this._commitmentMult = (float)data[1];
        this._lastKnownAmmo  = (int)data[2];
    }
}
