namespace CwaffingTheGungy;

public class ChekhovsGun : CwaffGun
{
    public static string ItemName         = "Chekhov's Gun";
    public static string ShortDescription = "Keeps its Promise";
    public static string LongDescription  = "Places a rifle that automatically fires a single round at any enemy that crosses its line of sight. Cannot fire within 3 seconds of its placement. Half of all unfired shots are returned as ammo on room clear.";
    public static string Lore             = "There is no record of this gun having ever been brought into the Gungeon, nor of it having belonged to anyone named Chekhov. Rather, the arcane magics enveloping the Gungeon have seemingly managed to produce a physical manifestation of a purely metaphorical weapon. Whether its unique properties will render it a deus ex machina or a brick joke remains to be seen.";

    internal static TrailController _ChekhovTrailPrefab = null;
    internal static GameObject _ChekhovGunVFX           = null;
    internal static GameObject _ChekhovGunFireVFX       = null;

    private List<ChekhovBullet> _extantBullets          = new();

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<ChekhovsGun>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 0.75f, ammo: 200, shootFps: 16, reloadFps: 16,
                muzzleVFX: "muzzle_chekhovs_gun", fireAudio: "chekhovs_gun_place_sound", reloadAudio: "chekhovs_gun_reload_sound");

        Projectile proj = gun.InitProjectile(GunData.New(clipSize: 8, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic,
          damage: 15f, range: 1000f, speed: 200f, sprite: "chekhov_projectile", fps: 12, scale: 0.5f, anchor: Anchor.MiddleCenter)
        ).Attach<ChekhovBullet>();

        _ChekhovTrailPrefab = VFX.CreateTrailObject("chekhov_trail_mid", fps: 60, startAnim: "chekhov_trail_start", cascadeTimer: C.FRAME, destroyOnEmpty: true);

        //WARNING: don't use actual gun animations for VFX or the actual gun's sprites can get messed up
        _ChekhovGunVFX = VFX.Create("chekhovs_gun_idle_vfx", 12, loops: true, anchor: Anchor.UpperRight);
        _ChekhovGunFireVFX = VFX.Create("chekhovs_gun_fire_vfx", 12, loops: false, anchor: Anchor.UpperRight);
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        player.OnRoomClearEvent += this.OnRoomClear;
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        player.OnRoomClearEvent -= this.OnRoomClear;
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
            this.PlayerOwner.OnRoomClearEvent -= this.OnRoomClear;
        base.OnDestroy();
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
            if (!projectile.FiredForFree())
                ++ammoToRestore;
        }
        this._extantBullets.Clear();
        if (!player.HasSynergy(Synergy.MASTERY_CHEKHOVS_GUN))
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

    public bool mastered                 = false;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        if (this._projectile.Owner is PlayerController pc)
            this.mastered = pc.HasSynergy(Synergy.MASTERY_CHEKHOVS_GUN);
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
        this._projectile.SetSpeed(0.0001f);
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
        this._sightline.SetAlpha(0.05f);
        this._gunVfx = SpawnManager.SpawnVFX(ChekhovsGun._ChekhovGunVFX, start, angle.EulerZ(), ignoresPools: true);
        this._gunVfx.GetComponent<tk2dSprite>().PlaceAtRotatedPositionByAnchor(start, Anchor.MiddleCenter);

        float lifeTime = 0.0f;
        if (!this.mastered)
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
        base.gameObject.PlayUnique("chekhovs_gun_launch_sound_alt");
        this._projectile.AddTrailToProjectileInstance(ChekhovsGun._ChekhovTrailPrefab).gameObject.SetGlowiness(_TRAIL_GLOW);
        UnityEngine.Object.Destroy(this._sightline);
        this._sightline = null;
        FancyVFX.Spawn(ChekhovsGun._ChekhovGunFireVFX, this._gunVfx.transform.position, this._gunVfx.transform.rotation,
            lifetime: 0.25f, fadeOutTime: 0.25f);
        UnityEngine.Object.Destroy(this._gunVfx);
        this._gunVfx = null;
        this._projectile.specRigidbody.CollideWithOthers  = true;
        this._projectile.specRigidbody.CollideWithTileMap = true;
        this._projectile.SetSpeed(oldSpeed);
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
