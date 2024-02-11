namespace CwaffingTheGungy;

public class ChamberJammer : PlayerItem
{
    public static string ItemName         = "Chamber Jammer";
    public static string ShortDescription = "Jam it All";
    public static string LongDescription  = "Reduces the current gun's max ammo to its current ammo (max 90% reduction), and multiplies the gun's damage by (1 / [% ammo left]).";
    public static string Lore             = "A demonic bullet that once resided inside the chamber of a Jammed's gun. These bullets grow to fill the empty space in whatever chamber they find themselves inside, and infuse their dark energies into any ammunition within their host gun's chamber. Two notable effects of this infusion are 1) an increased potency of all fired bullets and 2) an ominous aura radiating from the host gun. Fortunately, you can hear whispers coming from inside the chamber assuring you that the ominous aura is nothing you need to worry about.";

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<ChamberJammer>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;
        item.consumable   = true;
        item.CanBeDropped = true;
    }

    public override bool CanBeUsed(PlayerController user)
    {
        if (user.CurrentGun is not Gun gun)
            return false;
        if (gun.gameObject.GetComponent<ChamberJammedBehavior>())
            return false; // chamber is already jammed
        if (gun.InfiniteAmmo || gun.CurrentAmmo == 0 || !gun.CanActuallyBeDropped(user))
            return false;
        if (((float)gun.CurrentAmmo / (float)gun.AdjustedMaxAmmo) > 0.9f)
            return false; // can't use with more than 90% ammo remaining
        return base.CanBeUsed(user);
    }

    public override void DoEffect(PlayerController user)
    {
        if (user.CurrentGun is not Gun gun)
            return;
        if (gun.InfiniteAmmo || gun.CurrentAmmo == 0 || !gun.CanActuallyBeDropped(user))
            return;

        float percentAmmoToLose = Mathf.Min(0.9f, 1f - (float)gun.CurrentAmmo / (float)gun.AdjustedMaxAmmo);
        gun.gameObject.AddComponent<ChamberEaterAmmoDisplay>().Setup(percentAmmoToLose);
        gun.gameObject.AddComponent<ChamberJammedBehavior>().Setup(percentAmmoToLose);
        gun.SetBaseMaxAmmo(Mathf.CeilToInt((1f - percentAmmoToLose) * gun.GetBaseMaxAmmo()));

        float amountToBoostDamage = 1f / (1f - percentAmmoToLose);
        gun.AddCurrentGunStatModifier(PlayerStats.StatType.Damage, amountToBoostDamage, StatModifier.ModifyMethod.MULTIPLICATIVE);
        user.stats.RecalculateStats(user);

        GlobalSparksDoer.DoRadialParticleBurst(
            400, user.sprite.WorldBottomLeft.ToVector3ZisY(), user.sprite.WorldTopRight.ToVector3ZisY(),
            360f, 20f, 0f,
            systemType: GlobalSparksDoer.SparksType.BLACK_PHANTOM_SMOKE);

        user.gameObject.Play("chamber_eater_activate_sound");
    }
}

public class ChamberEatenProjectileParticles : MonoBehaviour
{
    // must all be public so serialization works correctly
    public Projectile _projectile = null;
    public PlayerController _owner = null;
    public int _particlesToSpawn   = 0;

    public void Setup(int particlesToSpawn)
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        this._particlesToSpawn = particlesToSpawn;
    }

    private void Update()
    {
        if (!_projectile || UnityEngine.Random.value > 0.35f)
            return;

        Vector3 minPosition = this._projectile.sprite.WorldBottomLeft.ToVector3ZisY();
        Vector3 maxPosition = this._projectile.sprite.WorldTopRight.ToVector3ZisY();
        GlobalSparksDoer.DoRandomParticleBurst(
            this._particlesToSpawn, minPosition, maxPosition, Lazy.RandomVector(1f).ToVector3ZisY(), 0f, 1f,
            systemType: GlobalSparksDoer.SparksType.BLACK_PHANTOM_SMOKE);
    }
}

public class ChamberJammedBehavior : MonoBehaviour
{
    private static Shader _FakePhantomShader = null;
    private static Color _FakePhantomRed = new Color(1.0f, 0.2f, 0.2f);

    // must all be public so serialization works correctly
    public Gun _gun                = null;
    public float _percentAmmoEaten = 0f;
    public int _particlesToSpawn   = 0;

    public void Setup(float percentAmmoToLose)
    {
        this._gun = base.GetComponent<Gun>();
        this._percentAmmoEaten = percentAmmoToLose;
        this._particlesToSpawn = Mathf.CeilToInt(percentAmmoToLose * 10f);

        this._gun.PostProcessProjectile += this.OnFired;
        _FakePhantomShader ??= ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
    }

    private void Update()
    {
        if (!this._gun || UnityEngine.Random.value > 0.1f)
            return;

        Vector3 minPosition = this._gun.sprite.WorldBottomLeft.ToVector3ZisY();
        Vector3 maxPosition = this._gun.sprite.WorldTopRight.ToVector3ZisY();
        GlobalSparksDoer.DoRandomParticleBurst(
            this._particlesToSpawn, minPosition, maxPosition, Lazy.RandomVector(1f).ToVector3ZisY(), 0f, 1f,
            systemType: GlobalSparksDoer.SparksType.BLACK_PHANTOM_SMOKE/*, null, startLifetime, null, systemType*/);
    }

    private void LateUpdate()
    {
        Material m = this._gun.sprite.renderer.material;
        if (m.shader == _FakePhantomShader)
            return;

        m.shader = _FakePhantomShader;
        m.SetColor("_OverrideColor", _FakePhantomRed);
        m.SetFloat("_EmissivePower", 30.0f);
    }

    private void OnFired(Projectile p)
    {
        p.gameObject.GetOrAddComponent<ChamberEatenProjectileParticles>().Setup(this._particlesToSpawn);
    }
}

public class ChamberEaterAmmoDisplay : CustomAmmoDisplay
{
    const float _DRAIN_TIME          = 1f;

    private Gun _gun                 = null;
    private PlayerController _owner  = null;
    private float _lifeTime          = 0f;
    private float _startMaxAmmo      = 0f;
    private int _displayedMaxAmmo    = 0;
    private bool _ammoDrainCompleted = false;

    public void Setup(float percentAmmoToLose)
    {
        this._gun = base.GetComponent<Gun>();
        this._owner = this._gun.CurrentOwner as PlayerController;
        this._startMaxAmmo = this._gun.AdjustedMaxAmmo;
        StartCoroutine(DrainAmmo(percentAmmoToLose));
    }

    private IEnumerator DrainAmmo(float percentAmmoToLose)
    {
        for (float elapsed = 0f; elapsed < _DRAIN_TIME; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / _DRAIN_TIME;
            float lostAmmo = _startMaxAmmo * percentAmmoToLose * percentDone;
            this._displayedMaxAmmo = Mathf.FloorToInt(this._startMaxAmmo - lostAmmo);
            yield return null;
        }
        this._ammoDrainCompleted = true;
    }

    public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
    {
        if (!this._owner || !this._gun || this._ammoDrainCompleted || this._owner.CurrentGun != this._gun)
        {
            UnityEngine.Object.Destroy(this);
            return false;
        }

        uic.SetAmmoCountLabelColor(Color.red);
        uic.GunAmmoCountLabel.Text = $"{this._gun.CurrentAmmo}/{this._displayedMaxAmmo}";
        return true;
    }
}
