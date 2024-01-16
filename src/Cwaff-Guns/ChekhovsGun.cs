namespace CwaffingTheGungy;

public class ChekhovsGun : AdvancedGunBehavior
{
    public static string ItemName         = "Chekhov's Gun";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Keeps its Promise";
    public static string LongDescription  = "Places a rifle that automatically fires a single round at any enemy that crosses its line of sight. Cannot fire within 3 seconds of its placement. Half of all unfired shots are returned as ammo on room clear.";
    public static string Lore             = "";

    internal static TrailController _ChekhovTrailPrefab = null;
    internal static GameObject _ChekhovGunVFX           = null;
    internal static GameObject _ChekhovGunFireVFX       = null;

    private List<ChekhovBullet> _extantBullets          = new();

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<ChekhovsGun>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 0.75f, ammo: 200);
            gun.SetAnimationFPS(gun.shootAnimation, 16);
            gun.SetAnimationFPS(gun.reloadAnimation, 16);
            gun.SetMuzzleVFX("muzzle_chekhovs_gun"); // innocuous muzzle flash effects
            gun.SetFireAudio("chekhovs_gun_place_sound");
            gun.SetReloadAudio("chekhovs_gun_reload_sound");

        Projectile proj = gun.InitProjectile(new(clipSize: 8, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic,
          damage: 15f, range: 1000f, speed: 200f, sprite: "chekhov_projectile", fps: 12, scale: 0.5f, anchor: Anchor.MiddleCenter)
        ).Attach<ChekhovBullet>();

        _ChekhovTrailPrefab = VFX.CreateTrailObject(ResMap.Get("chekhov_trail_mid")[0], new Vector2(24, 4), new Vector2(0, 0),
            ResMap.Get("chekhov_trail_mid"), 60, ResMap.Get("chekhov_trail_start"), 60, cascadeTimer: C.FRAME, destroyOnEmpty: true);

        _ChekhovGunVFX = VFX.Create("chekhovs_gun_idle", 12, loops: true, anchor: Anchor.UpperRight);

        _ChekhovGunFireVFX = VFX.Create("chekhovs_gun_fire", 12, loops: false, anchor: Anchor.UpperRight);
    }

    protected override void OnPickedUpByPlayer(PlayerController player)
    {
        base.OnPickedUpByPlayer(player);
        player.OnRoomClearEvent += this.OnRoomClear;
    }

    protected override void OnPostDroppedByPlayer(PlayerController player)
    {
        player.OnRoomClearEvent -= this.OnRoomClear;
        base.OnPostDroppedByPlayer(player);
    }

    public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
    {
        if (projectile.GetComponent<ChekhovBullet>() is ChekhovBullet cb)
            cb.isAFreebie = projectile.FiredForFree(gun, mod);
        return projectile;
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        if (projectile.GetComponent<ChekhovBullet>() is ChekhovBullet cb)
            this._extantBullets.Add(cb);
    }

    private void OnRoomClear(PlayerController player)
    {
        int ammoToRestore = 0;
        foreach (ChekhovBullet cb in this._extantBullets)
        {
            if (!cb)
                continue;
            if (cb.GetComponent<Projectile>() is not Projectile projectile)
                continue;
            if (!projectile.isActiveAndEnabled)
                continue;
            projectile.DieInAir(false, false, false, true);
            if (!cb.isAFreebie)
                ++ammoToRestore;
        }
        this._extantBullets.Clear();
        ammoToRestore /= 2;
        this.gun.GainAmmo(ammoToRestore);
    }
}

public class ChekhovBullet : MonoBehaviour
{
    private const float _MIN_FREEZE_TIME = 3.0f;
    private const float _SCAN_RATE       = 0.1f;
    private const float _TRAIL_GLOW      = 10.0f;

    private Projectile _projectile       = null;
    private GameObject _sightline        = null;
    private GameObject _gunVfx           = null;

    public bool isAFreebie               = false;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        StartCoroutine(PlotDevice(this._projectile.transform.right.XY()));
    }

    private void OnDestroy()
    {
        this._sightline.SafeDestroy();
        this._gunVfx.SafeDestroy();
    }

    private IEnumerator PlotDevice(Vector2 angleVec)
    {
        // Phase 1: stop immediately and wait for a few seconds
        float angle = angleVec.ToAngle();
        Vector2 start = this._projectile.transform.position.XY();
        RoomHandler room = GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(start.ToIntVector2());
        float oldSpeed = this._projectile.baseData.speed;
        bool wasElectric = (this._projectile.damageTypes & CoreDamageTypes.Electric) > 0;
        this._projectile.damageTypes &= (~CoreDamageTypes.Electric);
        this._projectile.baseData.speed = 0.0001f;
        this._projectile.UpdateSpeed();
        this._projectile.specRigidbody.CollideWithOthers  = false;
        this._projectile.specRigidbody.CollideWithTileMap = false;
        this._projectile.sprite.renderer.enabled = false;
        RaycastResult result;
        float length = 999f;
        Vector2 target = Vector2.zero;
        if (PhysicsEngine.Instance.Raycast(start, angleVec, length, out result, true, false))
        {
            length = C.PIXELS_PER_TILE * result.Distance;
            target = result.Contact;
        }
        RaycastResult.Pool.Free(ref result);
        this._sightline = VFX.CreateLaserSight(start, length: length, width: 1f, angle: angle, colour: Color.white, power: 0f);
        this._sightline.transform.parent = this._projectile.transform;
        this._sightline.SetAlpha(0.05f);
        this._gunVfx = SpawnManager.SpawnVFX(ChekhovsGun._ChekhovGunVFX, start, angle.EulerZ());

        float lifeTime = 0.0f;
        while (lifeTime < _MIN_FREEZE_TIME)
        {
            lifeTime += BraveTime.DeltaTime;
            this._gunVfx.SetAlpha(Mathf.Max(0.1f, 1f - lifeTime));
            yield return null;
        }

        // Phase 2: wait for an enemy to walk into view
        float lastScan = 0.0f;
        while (true)
        {
            yield return null;
            lifeTime += BraveTime.DeltaTime;
            if ((BraveTime.ScaledTimeSinceStartup - lastScan) < _SCAN_RATE)
                continue;
            lastScan = BraveTime.ScaledTimeSinceStartup;
            if (Lazy.AnyEnemyInLineOfSight(start, target))
                break;
        }

        // Phase 3: launch
        AkSoundEngine.PostEvent("chekhovs_gun_launch_sound_alt", base.gameObject);
        this._projectile.AddTrailToProjectileInstance(ChekhovsGun._ChekhovTrailPrefab).gameObject.SetGlowiness(_TRAIL_GLOW);
        UnityEngine.Object.Destroy(this._sightline);
        this._sightline = null;
        FancyVFX.Spawn(ChekhovsGun._ChekhovGunFireVFX, this._gunVfx.transform.position, this._gunVfx.transform.rotation,
            lifetime: 0.25f, fadeOutTime: 0.25f);
        UnityEngine.Object.Destroy(this._gunVfx);
        this._gunVfx = null;
        this._projectile.specRigidbody.CollideWithOthers  = true;
        this._projectile.specRigidbody.CollideWithTileMap = true;
        this._projectile.baseData.speed = oldSpeed;
        this._projectile.UpdateSpeed();
        // this._projectile.SendInDirection(target.Value - this._projectile.SafeCenter, true);
        if (wasElectric)
            this._projectile.damageTypes |= CoreDamageTypes.Electric;
        this._projectile.Attach<EasyTrailBullet>(trail => {
            trail.StartWidth = 0.1f;
            trail.EndWidth   = 0f;
            trail.LifeTime   = 0.25f;
            trail.BaseColor  = Color.yellow;
            trail.StartColor = Color.yellow;
            trail.EndColor   = Color.yellow;
        });
    }
}
