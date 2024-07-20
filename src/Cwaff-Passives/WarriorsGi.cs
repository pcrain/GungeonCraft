namespace CwaffingTheGungy;

public class WarriorsGi : CwaffPassive
{
    public static string ItemName         = "Warrior's Gi";
    public static string ShortDescription = "Going Further Beyond";
    public static string LongDescription  = "After taking damage that leaves you one hit from death, gives a permanent buff to several stats. Can be activated up to 5 times, with diminishing returns.";
    public static string Lore             = "A battle-torn training garment worn by the great Gunsoku, an almost-mythical being widely considered to be the greatest master of finger guns in history. Known best for saving the planet from galactic terrors such as Shell, Majin Boom, and Beebeerus, his unflinching drive for self-improvement and fighting stronger opponents has inspired many generations of Gungeoneers.";

    internal const    float   _MAX_POWER        = 5;
    internal readonly float[] _FIRE_RATE_MULT   = {1.00f, 1.20f, 1.30f, 1.35f, 1.38f, 1.40f};
    internal readonly float[] _MOVEMENT_MULT    = {1.00f, 1.20f, 1.35f, 1.45f, 1.48f, 1.50f};
    internal readonly float[] _DODGE_MULT       = {1.00f, 1.15f, 1.25f, 1.30f, 1.33f, 1.35f};
    internal readonly float[] _DAMAGE_MULT      = {1.00f, 1.40f, 1.70f, 1.85f, 1.95f, 2.00f};
    internal readonly float[] _BOSS_DAMAGE_MULT = {1.00f, 1.40f, 1.70f, 1.85f, 1.95f, 2.00f};

    internal static GameObject _SaiyanSpark;
    internal static GameObject _ZenkaiAura;

    private bool _canActivate = false;

    private StatModifier _rateOfFireStat     = null;
    private StatModifier _movementSpeedStat  = null;
    private StatModifier _dodgeRollSpeedStat = null;
    private StatModifier _damageStat         = null;
    private StatModifier _bossDamageStat     = null;

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<WarriorsGi>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.B;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);

        _SaiyanSpark = VFX.Create("saiyan_spark",
            fps: 12, loops: false, anchor: Anchor.MiddleCenter, scale: 0.5f);
        _ZenkaiAura  = VFX.Create("zenkai_aura",
            fps: 12, loops: true, anchor: Anchor.LowerCenter, scale: 0.4f, emissivePower: 5f, emissiveColour: Color.yellow);
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        player.healthHaver.OnDamaged += this.OnDamaged;
        player.healthHaver.OnHealthChanged += this.OnHealthChanged;
        this._canActivate = !player.IsOneHitFromDeath();
        RecalculatePower(player);
    }

    public override void OnFirstPickup(PlayerController player)
    {
        this._rateOfFireStat = new StatModifier {
            amount      = 1.00f,
            statToBoost = PlayerStats.StatType.RateOfFire,
            modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE};
        this._movementSpeedStat = new StatModifier {
            amount      = 1.00f,
            statToBoost = PlayerStats.StatType.MovementSpeed,
            modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE};
        this._dodgeRollSpeedStat = new StatModifier {
            amount      = 1.00f,
            statToBoost = PlayerStats.StatType.DodgeRollSpeedMultiplier,
            modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE};
        this._damageStat = new StatModifier {
            amount      = 1.00f,
            statToBoost = PlayerStats.StatType.Damage,
            modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE};
        this._bossDamageStat = new StatModifier {
            amount      = 1.00f,
            statToBoost = PlayerStats.StatType.DamageToBosses,
            modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE};
        this.passiveStatModifiers = new StatModifier[] {
            this._rateOfFireStat,
            this._movementSpeedStat,
            this._dodgeRollSpeedStat,
            this._damageStat,
            this._bossDamageStat,
        };
    }

    public override DebrisObject Drop(PlayerController player)
    {
        player.healthHaver.OnDamaged -= this.OnDamaged;
        player.healthHaver.OnHealthChanged -= this.OnHealthChanged;
        return base.Drop(player);
    }

    public override void OnDestroy()
    {
        if (this.Owner)
        {
            this.Owner.healthHaver.OnDamaged -= this.OnDamaged;
            this.Owner.healthHaver.OnHealthChanged -= this.OnHealthChanged;
        }
        base.OnDestroy();
    }

    private void OnHealthChanged(float resultValue, float maxValue)
    {
        if (!this.Owner.IsOneHitFromDeath())
            this._canActivate = true;
    }

    private void OnDamaged(float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection)
    {
        if (this._canActivate && this.Owner.IsOneHitFromDeath())
        {
            DoZenkaiBoost(this.Owner);
            this._canActivate = false;
        }
    }

    private void DoZenkaiBoost(PlayerController player)
    {
        ZenkaiAura z = player.gameObject.GetOrAddComponent<ZenkaiAura>();
            z.SetupIfNecessary();
            z.IncreasePower();

        RecalculatePower(player);
    }

    private void RecalculatePower(PlayerController player)
    {
        ZenkaiAura z = player.gameObject.GetOrAddComponent<ZenkaiAura>();
        int newPower = z ? z.GetPower() : 0;
        this._rateOfFireStat.amount     = _FIRE_RATE_MULT[newPower];
        this._movementSpeedStat.amount  = _MOVEMENT_MULT[newPower];
        this._dodgeRollSpeedStat.amount = _DODGE_MULT[newPower];
        this._damageStat.amount         = _DAMAGE_MULT[newPower];
        this._bossDamageStat.amount     = _BOSS_DAMAGE_MULT[newPower];
        player.stats.RecalculateStats(player);
    }

    public override void MidGameSerialize(List<object> data)
    {
        base.MidGameSerialize(data);
        int p1Zenkai = (GameManager.Instance.PrimaryPlayer is PlayerController p1) ? p1.gameObject.GetOrAddComponent<ZenkaiAura>()._zenkaiLevel : 0;
        data.Add(p1Zenkai);
        if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
        {
            int p2Zenkai = (GameManager.Instance.SecondaryPlayer is PlayerController p2) ? p2.gameObject.GetOrAddComponent<ZenkaiAura>()._zenkaiLevel : 0;
            data.Add(p2Zenkai);
        }
    }

    public override void MidGameDeserialize(List<object> data)
    {
        base.MidGameDeserialize(data);

        int p1ZenkaiLevel = (int)data[0];
        if (p1ZenkaiLevel > 0 && GameManager.Instance.PrimaryPlayer is PlayerController p1)
        {
            if (p1.gameObject.GetOrAddComponent<ZenkaiAura>() is ZenkaiAura z1)
                z1._zenkaiLevel = p1ZenkaiLevel;
            if (GameManager.Instance.PrimaryPlayer.passiveItems.Contains(this))
                RecalculatePower(GameManager.Instance.PrimaryPlayer);
        }
        if (GameManager.Instance.CurrentGameType != GameManager.GameType.COOP_2_PLAYER)
            return;

        int p2ZenkaiLevel = (int)data[1];
        if (p2ZenkaiLevel > 0 && GameManager.Instance.SecondaryPlayer is PlayerController p2)
        {
            if (p2.gameObject.GetOrAddComponent<ZenkaiAura>() is ZenkaiAura z2)
                z2._zenkaiLevel = p2ZenkaiLevel;
            if (GameManager.Instance.SecondaryPlayer.passiveItems.Contains(this))
                RecalculatePower(GameManager.Instance.SecondaryPlayer);
        }
    }
}

public class ZenkaiAura : MonoBehaviour
{
    private const float _AURA_LIFE      = 1.5f;
    private const float _AURA_FADE_TIME = 1.0f;

    private readonly float[] _MIN_SPARK_GAPS = {999f, 2.00f, 1.00f, 0.75f, 0.50f, 0.33f};
    private readonly float[] _MAX_SPARK_GAPS = {999f, 15.0f, 9.00f, 6.00f, 3.00f, 2.00f};
    private readonly float[] _SPARK_ALPHAS   = {0.0f, 0.35f, 0.65f, 5.00f, 25.0f,  225f};

    internal int _zenkaiLevel           = 0;

    private PlayerController _saiyan    = null;
    private GameObject _extantAura      = null;
    private float _auraLife             = 0;
    private bool _didSetup              = false;
    private float _nextSparkTime        = 0f;

    private void Start()
    {
        SetupIfNecessary();
    }

    public void SetupIfNecessary()
    {
        if (this._didSetup)
            return;
        if (base.gameObject.GetComponent<PlayerController>() is not PlayerController pc)
            return;

        this._saiyan = pc;
        this._didSetup = true;
    }

    private void DoRandomSparks()
    {
        if (this._zenkaiLevel < 1)
            return;

        this._nextSparkTime -= BraveTime.DeltaTime;
        if (this._nextSparkTime > 0f)
            return;

        this._saiyan.gameObject.Play("dbz_spark_sound");
        GameObject v = SpawnManager.SpawnVFX(WarriorsGi._SaiyanSpark, (this._saiyan.CenterPosition + Lazy.RandomVector(0.3f)).ToVector3ZUp(10f), Lazy.RandomEulerZ());
            tk2dSprite sprite = v.GetComponent<tk2dSprite>();
            sprite.HeightOffGround = 10f;
            // DepthLookupManager.AssignRendererToSortingLayer(sprite.renderer, DepthLookupManager.GungeonSortingLayer.FOREGROUND);
            sprite.UpdateZDepth();

            float a = _SPARK_ALPHAS[this._zenkaiLevel];
            if (a < 1.0f)
                v.SetAlphaImmediate(a);
            else
                v.SetGlowiness(a);
            v.transform.parent = this._saiyan.transform;

        this._nextSparkTime =
            UnityEngine.Random.Range(_MIN_SPARK_GAPS[this._zenkaiLevel], _MAX_SPARK_GAPS[this._zenkaiLevel]);
    }

    private void DoAuraChecks()
    {
        if (!this._extantAura)
            return;

        this._auraLife -= BraveTime.DeltaTime;
        float alpha = (this._auraLife / _AURA_FADE_TIME).Clamp();
        if (alpha > 0)
        {
            this._extantAura.SetAlpha(alpha);
            return;
        }

        UnityEngine.Object.Destroy(this._extantAura);
        this._extantAura = null;
    }


    private void LateUpdate()
    {
        if (!this._saiyan)
            return;
        DoRandomSparks();
        DoAuraChecks();
    }

    public void IncreasePower()
    {
        if (!this._saiyan || this._zenkaiLevel == WarriorsGi._MAX_POWER)
            return;

        ++this._zenkaiLevel;

        this._extantAura.SafeDestroy();
        this._extantAura                  = SpawnManager.SpawnVFX(WarriorsGi._ZenkaiAura, this._saiyan.SpriteBottomCenter, Quaternion.identity);
        this._extantAura.transform.parent = this._saiyan.transform;
        this._auraLife                    = _AURA_LIFE;
        this._nextSparkTime               =
            UnityEngine.Random.Range(_MIN_SPARK_GAPS[this._zenkaiLevel], _MAX_SPARK_GAPS[this._zenkaiLevel]);

        this._saiyan.gameObject.Play("zenkai_aura_sound");
    }

    public int GetPower()
    {
        return this._zenkaiLevel;
    }
}
