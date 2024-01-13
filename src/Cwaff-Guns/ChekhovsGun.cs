namespace CwaffingTheGungy;

public class ChekhovsGun : AdvancedGunBehavior
{
    public static string ItemName         = "Chekhov's Gun";
    public static string SpriteName       = "chekhovs_gun";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<ChekhovsGun>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 1f, ammo: 200);
            gun.SetAnimationFPS(gun.shootAnimation, 30);
            gun.SetAnimationFPS(gun.reloadAnimation, 40);
            // gun.SetMuzzleVFX(Items.Mailbox); // innocuous muzzle flash effects
            // gun.SetFireAudio("blowgun_fire_sound");
            // gun.SetReloadAudio("blowgun_reload_sound");

        Projectile proj = gun.InitProjectile(new(clipSize: 8, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic, damage: 10f, range: 1000f, speed: 200f,
          sprite: "chekhov_projectile", fps: 12, scale: 0.5f, anchor: Anchor.MiddleCenter)).Attach<ChekhovBullet>();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        projectile.GetComponent<ChekhovBullet>()?.Setup(/*this.gun.CurrentAngle*/);
    }
}

public class ChekhovBullet : MonoBehaviour
{
    private const float _MIN_FREEZE_TIME = 1.0f;
    private const float _SCAN_RATE       = 0.1f;

    internal static readonly Color _SightColor = new Color(1f, 1f, 1f, 0.1f);

    private float _lastScan        = 0.0f;
    // private bool _activated        = false;
    private Projectile _projectile = null;

    public void Setup(/*float angle*/)
    {
        this._projectile = base.GetComponent<Projectile>();
        StartCoroutine(PlotDevice(this._projectile.transform.right.XY()));
    }

    private IEnumerator PlotDevice(Vector2 angleVec)
    {
        // Phase 1: stop immediately and wait for a few seconds
        AkSoundEngine.PostEvent("aimu_shoot_sound", base.gameObject);
        float angle = angleVec.ToAngle();
        Vector2 start = this._projectile.transform.position.XY();
        float lifeTime = 0.0f;
        float oldSpeed = this._projectile.baseData.speed;
        this._projectile.baseData.speed = 0.0001f;
        this._projectile.UpdateSpeed();
        this._projectile.specRigidbody.CollideWithOthers  = false;
        this._projectile.specRigidbody.CollideWithTileMap = false;
        RaycastResult result;
        float length = 999f;
        if (PhysicsEngine.Instance.Raycast(start, angleVec, length, out result, true, false))
            length = C.PIXELS_PER_TILE * result.Distance;
        RaycastResult.Pool.Free(ref result);
        GameObject sightLine = VFX.CreateLaserSight(
            start, length: length, width: 1f, angle: angle, colour: Color.white, power: 0f);
        sightLine.transform.parent = this._projectile.transform;
        sightLine.SetAlpha(0.1f);

        yield return new WaitForSeconds(_MIN_FREEZE_TIME);
        lifeTime += _MIN_FREEZE_TIME;

        // Phase 2: wait for an enemy to walk into view
        Vector2? target = null;
        while (target == null)
        {
            lifeTime += BraveTime.DeltaTime;
            if ((BraveTime.ScaledTimeSinceStartup - this._lastScan) >= _SCAN_RATE)
            {
                this._lastScan = BraveTime.ScaledTimeSinceStartup;
                target = Lazy.NearestEnemyWithinConeOfVision(
                    start: this._projectile.SafeCenter, coneAngle: angle, maxDeviation: 4f, useNearestAngleInsteadOfDistance: false);
            }
            yield return null;
        }

        // Phase 3: launch
        AkSoundEngine.PostEvent("aimu_shoot_sound", base.gameObject);
        this._projectile.AddTrailToProjectileInstance(SubtractorBeam._GreenTrailPrefab).gameObject.SetGlowiness(100f);

        UnityEngine.Object.Destroy(sightLine);
        this._projectile.specRigidbody.CollideWithOthers  = true;
        this._projectile.specRigidbody.CollideWithTileMap = true;
        // yield return null; // wait for colliders to update
        this._projectile.baseData.speed = oldSpeed;
        this._projectile.UpdateSpeed();
        this._projectile.SendInDirection(target.Value - this._projectile.SafeCenter, true);
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
