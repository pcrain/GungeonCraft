namespace CwaffingTheGungy;

public class JohnsWick : CwaffPassive
{
    public static string ItemName         = "John's Wick";
    public static string ShortDescription = "No Dogs Harmed";
    public static string LongDescription  = "Move faster and deal double damage while on fire; take damage from fire more slowly.";
    public static string Lore             = "According to Bello, the wick inside this lantern was once possessed by a man who survived dozens of assassination attempts en route to grabbing breakfast at a hotel. This raises far more questions than it answers, and Bello refuses to elaborate further.";

    private const float _FIRE_TIMER_MULT = 0.25f;
    private const float _MOVEMENT_BOOST  = 5f;
    private const float _DAMAGE_BOOST    = 2f;

    private bool                 _wasOnFire          = false;
    private StatModifier[]       _flameOn            = null;
    private StatModifier[]       _flameOff           = null;
    private DamageTypeModifier   _fireResistance     = null;

    public static void Init()
    {
        PassiveItem item   = Lazy.SetupPassive<JohnsWick>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality       = ItemQuality.C;
    }

    public override void OnFirstPickup(PlayerController player)
    {
        base.OnFirstPickup(player);
        this._flameOff = [];
        this._flameOn = [
            StatType.MovementSpeed.Add(_MOVEMENT_BOOST),
            StatType.Damage.Mult(_DAMAGE_BOOST),
        ];
        this._wasOnFire = false;
        this._fireResistance = new DamageTypeModifier {
            damageType = CoreDamageTypes.Fire,
            damageMultiplier = _FIRE_TIMER_MULT,
        };
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        this.passiveStatModifiers = _flameOff;
        player.PostProcessProjectile += this.PostProcessProjectile;
        player.healthHaver.damageTypeModifiers.AddUnique(this._fireResistance);
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        if (!player)
            return;
        player.PostProcessProjectile -= this.PostProcessProjectile;
        player.healthHaver.damageTypeModifiers.TryRemove(this._fireResistance);
    }

    private void PostProcessProjectile(Projectile proj, float effectChanceScalar)
    {
        if (!(this.Owner && this._wasOnFire))
            return;
        proj.StartCoroutine(GetWicked(proj.specRigidbody));
    }

    private IEnumerator GetWicked(SpeculativeRigidbody s, bool once = false)
    {
        const int   NUM                = 1;
        const float ANGLE_VARIANCE     = 15f;
        const float BASE_MAGNITUDE     = 2.25f;
        const float MAGNITUDE_VARIANCE = 1f;
        Color? startColor              = Color.blue;
        while (s)
        {
            Vector3 minPosition = s.HitboxPixelCollider.UnitBottomLeft.ToVector3ZisY();
            Vector3 maxPosition = s.HitboxPixelCollider.UnitTopRight.ToVector3ZisY();
            GlobalSparksDoer.DoRadialParticleBurst(
              NUM, minPosition, maxPosition, ANGLE_VARIANCE, BASE_MAGNITUDE, MAGNITUDE_VARIANCE,
              startColor: startColor,
              startLifetime: 0.5f,
              systemType: GlobalSparksDoer.SparksType.STRAIGHT_UP_GREEN_FIRE/*EMBERS_SWIRLING*/
              );
            if (once)
                yield break;
            yield return null;
        }
    }

    public override void Update()
    {
        base.Update();

        if (!this.Owner)
            return;

        if (this._wasOnFire != this.Owner.IsOnFire)
        {
            this._wasOnFire           = this.Owner.IsOnFire;
            this.passiveStatModifiers = this.Owner.IsOnFire ? _flameOn : _flameOff;
            this.Owner.stats.RecalculateStats(this.Owner, false, false);
        }
        if (this.Owner.IsOnFire)
            this.Owner.StartCoroutine(GetWicked(this.Owner.specRigidbody, once: true));
    }
}
