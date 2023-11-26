namespace CwaffingTheGungy;

public class Alyx : AdvancedGunBehavior
{
    public static string ItemName         = "Alyx";
    public static string SpriteName       = "alyx";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Welcome to the New Age";
    public static string LongDescription  = "Fires shots that poison and ignite enemies. Current and max ammo decay exponentially, leaving radioactive waste behind in the process. Gun decays completely at 10 max ammo.";
    public static string Lore             = "A little known fact of nuclear chemistry is that sufficiently large quantities of Uranium -- under specific circumstances not fully understood at present -- can decay directly into guns. These guns have extremely limited lifespans before decaying completely into radiactive goo, but their sheer utility in battle make them a prized treasure for experienced gungeoneers who are willing to absorb a few gamma rays in the name of DPS";

    internal const float _AMMO_HALF_LIFE_SECS = 90.0f;
    internal const float _GUN_HALF_LIFE_SECS  = 300.0f;
    internal const float _MIN_CALC_RATE       = 0.1f; // we don't need to recalculate every single gosh darn frame
    internal const int   _BASE_MAX_AMMO       = 1000;
    internal const int   _MIN_AMMO_TO_PERSIST = 10;

    internal static readonly float _AMMO_DECAY_LAMBDA = Mathf.Log(2) / _AMMO_HALF_LIFE_SECS;
    internal static readonly float _GUN_DECAY_LAMBDA  = Mathf.Log(2) / _GUN_HALF_LIFE_SECS;

    private Coroutine _decayCoroutine = null;

    public float timeAtLastRecalc   = 0.0f; // must be public so it serializes properly when dropped / picked up

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Alyx>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.A, gunClass: GunClass.FULLAUTO, reloadTime: 0.5f, ammo: _BASE_MAX_AMMO);
            gun.SetAnimationFPS(gun.reloadAnimation, 20);
            gun.SetAnimationFPS(gun.shootAnimation, 20);
            gun.SetMuzzleVFX("muzzle_alyx", fps: 30, scale: 0.5f, anchor: Anchor.MiddleCenter);
            gun.SetReloadAudio("alyx_reload_sound");
            gun.AddToSubShop(ItemBuilder.ShopType.Goopton);

        gun.InitProjectile(clipSize: 10, shootStyle: ShootStyle.Automatic, customClip: SpriteName, damage: 15.0f, speed: 20.0f,
          poison: 1.0f, fire: 1.0f, sprite: "alyx_projectile", fps: 16, scale: 0.5625f, anchor: Anchor.MiddleCenter);
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        AkSoundEngine.PostEvent("alyx_shoot_sound", gun.gameObject);
    }

    public override void Start()
    {
        base.Start();
        gun.sprite.gameObject.SetGlowiness(50f);
        RecalculateAmmo();
    }

    protected override void NonCurrentGunUpdate()
    {
        base.NonCurrentGunUpdate();
        RecalculateAmmo();
    }

    protected override void Update()
    {
        base.Update();
        Material m = gun.sprite.renderer.material;
        m.SetFloat("_EmissivePower", 50f + 100f * Mathf.Abs(Mathf.Sin(BraveTime.ScaledTimeSinceStartup)));
        RecalculateAmmo();
    }

    protected override void OnPickup(GameActor owner)
    {
        if (!this.everPickedUpByPlayer)
        {
            this.gun.SetBaseMaxAmmo(_BASE_MAX_AMMO);
            this.gun.CurrentAmmo = _BASE_MAX_AMMO;
            this.timeAtLastRecalc = BraveTime.ScaledTimeSinceStartup;
            ETGModConsole.Log($"first pickup");
        }
        base.OnPickup(owner);
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
        if (this._decayCoroutine != null)
        {
            StopCoroutine(this._decayCoroutine);
            this._decayCoroutine = null;
        }
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        if (this._decayCoroutine == null)
            this._decayCoroutine = this.Owner.StartCoroutine(DecayWhileInactive());
    }

    public override void OnDestroy()
    {
        if (this._decayCoroutine != null)
        {
            StopCoroutine(this._decayCoroutine);
            this._decayCoroutine = null;
        }
        base.OnDestroy();
    }

    private IEnumerator DecayWhileInactive()
    {
        while (this.gameObject != null)
        {
            if (!GameManager.Instance.IsPaused && !GameManager.Instance.IsLoadingLevel)
                RecalculateAmmo();
            yield return null;
        }
    }

    internal static int ComputeExponentialDecay(float startAmount, float lambda, float timeElapsed)
    {
        return Lazy.RoundWeighted(startAmount * Mathf.Exp(-lambda * timeElapsed));
    }

    private void RecalculateAmmo()
    {
        float timeSinceLastRecalc = BraveTime.ScaledTimeSinceStartup - this.timeAtLastRecalc;
        if (timeSinceLastRecalc <= _MIN_CALC_RATE)
            return;
        this.timeAtLastRecalc = BraveTime.ScaledTimeSinceStartup;

        int newAmmo = ComputeExponentialDecay((float)this.gun.CurrentAmmo, _AMMO_DECAY_LAMBDA, timeSinceLastRecalc);
        int newMaxAmmo = ComputeExponentialDecay((float)this.gun.GetBaseMaxAmmo(), _GUN_DECAY_LAMBDA, timeSinceLastRecalc);

        PlayerController player = this.Owner as PlayerController;
        // If we've decayed at all, create poison goop under our feet
        if (newAmmo < this.gun.CurrentAmmo || newMaxAmmo < this.gun.GetBaseMaxAmmo())
        {
            DeadlyDeadlyGoopManager poisonGooper = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.PoisonDef); // can be null sometimes???
            if (player)
                poisonGooper?.AddGoopCircle(player.sprite.WorldBottomCenter - player.m_currentGunAngle.ToVector(1f), 0.75f);
            else
                poisonGooper?.AddGoopCircle(this.gun.sprite.WorldCenter, 1f);
        }

        this.gun.CurrentAmmo = newAmmo;
        this.gun.SetBaseMaxAmmo(newMaxAmmo);

        if (newMaxAmmo > _MIN_AMMO_TO_PERSIST)
            return;

        if (player)
            player.inventory.DestroyGun(this.gun);
        else // vanish in a puff of smoke on the ground
        {
            Lazy.DoSmokeAt(this.gun.sprite.WorldCenter);
            UnityEngine.Object.Destroy(this.gun.gameObject);
        }
    }
}
