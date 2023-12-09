namespace CwaffingTheGungy;

public class Uppskeruvel : AdvancedGunBehavior
{
    public static string ItemName         = "Uppskeruvel"; // é
    public static string SpriteName       = "uppskeruvel";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal const string _SoulSpriteUI   = $"{C.MOD_PREFIX}:_SoulSpriteUI";  // need the string immediately for preloading in Main()

    internal static int        _UppskeruvelId  = -1;
    internal static GameObject _SoulPrefab     = null;
    internal static GameObject _SoulCollectVFX = null;

    public int souls = 0;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Uppskeruvel>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 0.5f, ammo: 800);
            gun.SetMuzzleVFX(Items.Mailbox); // innocuous muzzle flash effects

        gun.InitProjectile(new(clipSize: 12, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic,
          sprite: "uppskeruvel_projectile", fps: 12, scale: 0.5f, anchor: Anchor.MiddleLeft)
        ).Attach<UppskeruvelProjectile>(
        );

        gun.gameObject.AddComponent<UppskeruvelAmmoDisplay>();

        GameObject soul = VFX.Create("poe_soul", fps: 8, loops: true, anchor: Anchor.LowerCenter, emissivePower: 0.4f);
        soul.AddComponent<UppskeruvelSoul>();

        _SoulPrefab = soul;
        _SoulCollectVFX = VFX.Create("soul_collect", 16, loops: true, anchor: Anchor.MiddleCenter);

        _UppskeruvelId = gun.PickupObjectId;
    }
}

public class UppskeruvelAmmoDisplay : CustomAmmoDisplay
{
    private Gun _gun;
    private Uppskeruvel _uppies;
    private PlayerController _owner;
    private void Start()
    {
        this._gun = base.GetComponent<Gun>();
        this._uppies = this._gun.GetComponent<Uppskeruvel>();
        this._owner = this._gun.CurrentOwner as PlayerController;
    }

    public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
    {
        if (!this._owner)
            return false;

        uic.SetAmmoCountLabelColor(Color.white);
        Vector3 relVec = Vector3.zero;
        uic.GunAmmoCountLabel.AutoHeight = true; // enable multiline text
        uic.GunAmmoCountLabel.ProcessMarkup = true; // enable multicolor text
        uic.GunAmmoCountLabel.Text = $"[sprite \"{Uppskeruvel._SoulSpriteUI}\"][color #6666dd]x{this._uppies.souls}[/color]\n{this._gun.CurrentAmmo}/{this._gun.AdjustedMaxAmmo}";
        return true;
    }
}

public class UppskeruvelProjectile : MonoBehaviour
{
    private const float _SOUL_LAUNCH_SPEED = 7f;
    private const float _SOULS_PER_HEALTH  = 0.1f;

    private Projectile _projectile;
    private PlayerController _owner;
    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;

        this._projectile.OnWillKillEnemy += this.OnWillKillEnemy;
    }

    private void OnWillKillEnemy(Projectile bullet, SpeculativeRigidbody enemy)
    {
        if (!(enemy?.aiActor?.IsHostile() ?? false))
            return; // avoid processing effect for non-hostile enemies

        Vector2 ppos = enemy.UnitCenter;
        int soulsToSpawn = Mathf.CeilToInt(_SOULS_PER_HEALTH * (enemy.healthHaver?.GetMaxHealth() ?? 0));
        for (int i = 0; i < soulsToSpawn; ++i)
        {
            float angle = Lazy.RandomAngle();
            Vector2 finalPos = ppos + BraveMathCollege.DegreesToVector(angle);
            UnityEngine.Object.Instantiate(Uppskeruvel._SoulPrefab, finalPos, Quaternion.identity
                ).GetComponent<UppskeruvelSoul>(
                ).Setup(angle.ToVector(_SOUL_LAUNCH_SPEED));
        }
    }
}

public class UppskeruvelSoul : MonoBehaviour
{
    const float _ATTRACT_RADIUS_SQR = 25f;  // range before we start homing in on player
    const float _PICKUP_RADIUS_SQR  = 2f;   // range before we are picked up by player
    const float _HOME_ACCEL         = 44f;  // acceleration per second towards player
    const float _BOB_SPEED          = 4f;
    const float _BOB_HEIGHT         = 0.20f;
    const float _FRICTION           = 0.96f;
    const float _MAX_LIFE           = 3f;

    private bool _setup             = false;
    private PlayerController _owner = null;
    private float _homeSpeed        = 0.0f;
    private tk2dSprite _sprite      = null;
    private float _lifetime         = 0.0f;
    private Vector2 _velocity       = Vector2.zero;
    private Vector3 _basePos        = Vector2.zero;

    public void Setup(Vector2 velocity)
    {
        this._sprite = base.GetComponent<tk2dSprite>();
        this._velocity = velocity;
        this._basePos = base.transform.position;
        this._setup = true;
    }

    private void Update()
    {
        if (!this._setup)
            return;

        this._lifetime += BraveTime.DeltaTime;
        if (this._owner)
        {
            Vector2 delta = (this._owner.CenterPosition - base.transform.position.XY());
            Vector2 deltaNorm = delta.normalized;
            this._homeSpeed += _HOME_ACCEL * BraveTime.DeltaTime;
            // Weighted average of natural and direct velocity towards player
            this._velocity = this._homeSpeed * Vector2.Lerp((this._velocity.normalized + deltaNorm).normalized, deltaNorm, 0.2f);
            this._basePos += (this._velocity * BraveTime.DeltaTime).ToVector3ZUp();
            base.transform.position = new Vector2(this._basePos.x, this._basePos.y + _BOB_HEIGHT * Mathf.Sin(_BOB_SPEED * this._lifetime)).ToVector3ZisY();

            FancyVFX.Spawn(Outbreak._OutbreakSmokeVFX, base.transform.position, Lazy.RandomEulerZ(),
                velocity: Lazy.RandomVector(0.1f), lifetime: 0.25f, fadeOutTime: 0.5f);

            if (delta.sqrMagnitude > _PICKUP_RADIUS_SQR)
                return;

            foreach (Gun gun in this._owner.inventory.AllGuns)
            {
                if (gun.GetComponent<Uppskeruvel>() is not Uppskeruvel uppies)
                    continue;
                uppies.souls += 1;
                break;
            }
            AkSoundEngine.PostEvent("pickup_poe_soul_sound_stop_all", base.gameObject);
            AkSoundEngine.PostEvent("pickup_poe_soul_sound", base.gameObject);
            float rotOffset = 90f * UnityEngine.Random.value;
            for (int i = 0; i < 4; ++i)
                FancyVFX.Spawn(Uppskeruvel._SoulCollectVFX, base.transform.position, Lazy.RandomEulerZ(),
                  velocity: (rotOffset + 90f * i).ToVector(4f), lifetime: 0.5f, fadeOutTime: 0.75f);
            UnityEngine.Object.Destroy(base.gameObject);
            return;
        }

        if (this._velocity.sqrMagnitude > 1f)
        {
            this._velocity *= (float)Lazy.FastPow(_FRICTION, C.FPS * BraveTime.DeltaTime);
            this._basePos += (this._velocity * BraveTime.DeltaTime).ToVector3ZUp();
        }
        else
            this._velocity = this._velocity.normalized;
        base.transform.position = new Vector2(this._basePos.x, this._basePos.y + _BOB_HEIGHT * Mathf.Sin(_BOB_SPEED * this._lifetime)).ToVector3ZisY();

        if (this._lifetime > _MAX_LIFE)
        {
            for (int i = 0; i < 4; ++i)
            {
                FancyVFX.Spawn(Outbreak._OutbreakSmokeVFX, base.transform.position, Lazy.RandomEulerZ(),
                    velocity: Lazy.RandomVector(0.5f), lifetime: 0.3f, fadeOutTime: 0.6f);
            }
            UnityEngine.Object.Destroy(base.gameObject);
            return;
        }

        foreach (PlayerController player in GameManager.Instance.AllPlayers)
        {
            if (!player || !player.isActiveAndEnabled || player.IsGhost)
                continue;
            if ((base.transform.position.XY() - player.CenterPosition).sqrMagnitude > _ATTRACT_RADIUS_SQR)
                continue;
            if (!player.HasGun(Uppskeruvel._UppskeruvelId))
                continue;
            this._owner = player;
        }
    }
}
