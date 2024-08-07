namespace CwaffingTheGungy;

public class Alyx : CwaffGun
{
    public static string ItemName         = "Alyx";
    public static string ShortDescription = "Welcome to the New Age";
    public static string LongDescription  = "Fires shots that poison and ignite enemies. Current and max ammo decay exponentially, leaving radioactive waste behind in the process. Gun decays completely at 10 max ammo.";
    public static string Lore             = "A little known fact of nuclear chemistry is that sufficiently large quantities of Uranium -- under specific circumstances not fully understood at present -- can decay directly into guns. These guns have extremely limited lifespans before decaying completely into radioactive goo, but their sheer utility in battle make them a prized treasure for experienced gungeoneers who are willing to absorb a few gamma rays in the name of DPS";

    internal const float _AMMO_HALF_LIFE_SECS = 90.0f;
    internal const float _GUN_HALF_LIFE_SECS  = 300.0f;
    internal const float _MIN_CALC_RATE       = 0.1f; // we don't need to recalculate every single gosh darn frame
    internal const int   _BASE_MAX_AMMO       = 1000;
    internal const int   _MIN_AMMO_TO_PERSIST = 10;

    internal static readonly float _AMMO_DECAY_LAMBDA = Mathf.Log(2) / _AMMO_HALF_LIFE_SECS;
    internal static readonly float _GUN_DECAY_LAMBDA  = Mathf.Log(2) / _GUN_HALF_LIFE_SECS;

    private DamageTypeModifier _poisonImmunity = null;
    private Coroutine _decayCoroutine = null;

    public float timeAtLastRecalc   = 0.0f; // must be public so it serializes properly when dropped / picked up

    public static void Init()
    {
        Lazy.SetupGun<Alyx>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.A, gunClass: GunClass.FULLAUTO, reloadTime: 0.5f, ammo: _BASE_MAX_AMMO,
            shootFps: 20, reloadFps: 20, muzzleVFX: "muzzle_alyx", muzzleFps: 30, muzzleScale: 0.5f, muzzleAnchor: Anchor.MiddleCenter,
            reloadAudio: "alyx_reload_sound")
          .AddToSubShop(ItemBuilder.ShopType.Goopton)
          .InitProjectile(GunData.New(clipSize: 10, shootStyle: ShootStyle.Automatic, customClip: true, damage: 15.0f, speed: 20.0f, poison: 1.0f,
            fire: 1.0f, sprite: "alyx_projectile", fps: 16, scale: 0.5625f, anchor: Anchor.MiddleCenter, spawnSound: "alyx_shoot_sound", uniqueSounds: true));
    }

    private void Start()
    {
        gun.sprite.gameObject.SetGlowiness(50f);
        RecalculateAmmo();
    }

    public override void Update()
    {
        base.Update();
        Material m = gun.sprite.renderer.material;
        m.SetFloat("_EmissivePower", 50f + 100f * Mathf.Abs(Mathf.Sin(BraveTime.ScaledTimeSinceStartup)));
        RecalculateAmmo();
        if (this.PlayerOwner && this.PlayerOwner.HasSynergy(Synergy.MASTERY_ALYX))
            this.PlayerOwner.healthHaver.damageTypeModifiers.AddUnique(this._poisonImmunity);
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        this._poisonImmunity ??= new DamageTypeModifier {
            damageType = CoreDamageTypes.Poison,
            damageMultiplier = 0f,
        };
        if (timeAtLastRecalc == 0.0f)
        {
            this.gun.SetBaseMaxAmmo(_BASE_MAX_AMMO);
            this.gun.CurrentAmmo = _BASE_MAX_AMMO;
            this.timeAtLastRecalc = BraveTime.ScaledTimeSinceStartup;
        }
        base.OnPlayerPickup(player);
        RecalculateAmmo();
        if (this._decayCoroutine != null)
        {
            StopCoroutine(this._decayCoroutine);
            this._decayCoroutine = null;
        }
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        RecalculateAmmo();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        this._decayCoroutine ??= this.PlayerOwner.StartCoroutine(DecayWhileInactive());
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        player.healthHaver.damageTypeModifiers.TryRemove(this._poisonImmunity);
    }

    public override void OnDestroy()
    {
        if (this._decayCoroutine != null)
        {
            StopCoroutine(this._decayCoroutine);
            this._decayCoroutine = null;
        }
        if (this.PlayerOwner)
            this.PlayerOwner.healthHaver.damageTypeModifiers.TryRemove(this._poisonImmunity);
        base.OnDestroy();
    }

    private IEnumerator DecayWhileInactive()
    {
        while (this && this.gameObject && this.PlayerOwner && this.PlayerOwner.CurrentGun != this)
        {
            if (GameManager.Instance && !GameManager.Instance.IsPaused && !GameManager.Instance.IsLoadingLevel)
                RecalculateAmmo();
            yield return null;
        }
        this._decayCoroutine = null;
    }

    internal static int ComputeExponentialDecay(float startAmount, float lambda, float timeElapsed)
    {
        return (startAmount * Mathf.Exp(-lambda * timeElapsed)).RoundWeighted();
    }

    private void RecalculateAmmo()
    {
        float timeSinceLastRecalc = BraveTime.ScaledTimeSinceStartup - this.timeAtLastRecalc;
        if (timeSinceLastRecalc <= _MIN_CALC_RATE)
            return;
        this.timeAtLastRecalc = BraveTime.ScaledTimeSinceStartup;

        float decayFactor = (this.PlayerOwner && this.PlayerOwner.HasSynergy(Synergy.MASTERY_ALYX)) ? 0.25f : 1.0f;
        int newAmmo = ComputeExponentialDecay((float)this.gun.CurrentAmmo, decayFactor * _AMMO_DECAY_LAMBDA, timeSinceLastRecalc);
        int newMaxAmmo = ComputeExponentialDecay((float)this.gun.GetBaseMaxAmmo(), decayFactor * _GUN_DECAY_LAMBDA, timeSinceLastRecalc);

        // If we've decayed at all, create poison goop under our feet
        if (newAmmo < this.gun.CurrentAmmo || newMaxAmmo < this.gun.GetBaseMaxAmmo())
        {
            if (DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.PoisonDef) is DeadlyDeadlyGoopManager gooper)
            {
                if (this.PlayerOwner)
                    gooper.AddGoopCircle(this.PlayerOwner.SpriteBottomCenter.XY() - this.PlayerOwner.m_currentGunAngle.ToVector(1f), 0.75f);
                else
                    gooper.AddGoopCircle(this.gun.sprite.WorldCenter, 1f);
            }
        }

        this.gun.CurrentAmmo = newAmmo;
        this.gun.SetBaseMaxAmmo(newMaxAmmo);

        if (newMaxAmmo > _MIN_AMMO_TO_PERSIST)
            return;

        if (this.PlayerOwner)
            this.PlayerOwner.inventory.DestroyGun(this.gun);
        else // vanish in a puff of smoke on the ground
        {
            Lazy.DoSmokeAt(this.gun.sprite.WorldCenter);
            UnityEngine.Object.Destroy(this.gun.gameObject);
        }
    }

    public override void MidGameSerialize(List<object> data, int i)
    {
        base.MidGameSerialize(data, i);
        data.Add(gun.GetBaseMaxAmmo());
    }

    public override void MidGameDeserialize(List<object> data, ref int i)
    {
        base.MidGameDeserialize(data, ref i);
        gun.SetBaseMaxAmmo((int)data[i++]);
    }
}
