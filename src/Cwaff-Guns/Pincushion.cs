namespace CwaffingTheGungy;

public class Pincushion : CwaffGun
{
    public static string ItemName         = "Pincushion";
    public static string ShortDescription = "Needless Violence";
    public static string LongDescription  = "Fires an extraordinarily fast barrage of extremely weak pins that lose accuracy as the clip is depleted. Pins deal fixed damage, cannot damage anything other than enemies, and are completely blocked by barrels, crates, etc.";
    public static string Lore             = "Even when shot out of a gun, a pin won't be particularly harmful to most of the Gungeon's inhabitants. Two pins won't fare much better, nor three, nor ten. But, by firing thousands of pins per second, one of them will in all likelihood eventually lodge itself in a weak point or some other location extremely compromising to its target. Needless to say, the probabilistic nature of this gun threads the line of practicality, but its high drum capacity doesn't leave it without merits in the right situation.";

    internal const int   _SIMULTANEOUS_BULLETS = 4;
    internal const float _MAX_SPREAD           = 45f;
    internal const float _MIN_SPREAD           = 8f;
    internal const float _DLT_SPREAD           = _MAX_SPREAD - _MIN_SPREAD;
    internal const float _NEEDLE_DAMAGE        = 0.35f;

    public static void Init()
    {
        Lazy.SetupGun<Pincushion>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.D, gunClass: GunClass.FULLAUTO, reloadTime: 1.8f, ammo: 10000, doesScreenShake: false,
            shootFps: 30, reloadFps: 24)
          .SetReloadAudio("pincushion_reload_start_sound", 0)
          .SetReloadAudio("pincushion_reload_sound", 8, 13, 18, 23, 28, 35)
          .InitProjectile(GunData.New(clipSize: 1000 / _SIMULTANEOUS_BULLETS, cooldown: C.FRAME, angleVariance: 0.0f, shootStyle: ShootStyle.Automatic,
            damage: 0.0f, speed: 200.0f, force: 0.0f, range: 999f, bossDamageMult: 0.65f, sprite: "needle", fps: 12, spawnSound: "pincushion_fire",
            anchor: Anchor.MiddleLeft, barrageSize: _SIMULTANEOUS_BULLETS))
          .SetAllImpactVFX(VFX.CreatePool("microdust", fps: 30, loops: false))
          .Attach<VeryFragileProjectile>()
          .Attach<EasyTrailBullet>(trail => {
            trail.TrailPos   = trail.transform.position;
            trail.StartWidth = 0.1f;
            trail.EndWidth   = 0f;
            trail.LifeTime   = 0.1f;
            trail.StartColor = Color.gray;
            trail.BaseColor  = Color.gray;
            trail.EndColor   = Color.gray;
          });
    }

    // GetLowDiscrepancyRandom() makes projectiles not spread as randomly as they could, so override that randomness with our own spread
    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (this.PlayerOwner as PlayerController is not PlayerController pc)
            return;

        float spread = _MIN_SPREAD;
        if (pc.HasSynergy(Synergy.MASTERY_PINCUSHION))
        {
            if (projectile.GetComponent<VeryFragileProjectile>() is VeryFragileProjectile vfp)
                vfp.phasesThroughBreakables = true;
        }
        else
            spread += _DLT_SPREAD * (1f - ((float)this.gun.ClipShotsRemaining / (float)this.gun.ClipCapacity));

        spread *= pc.AccuracyMult();
        projectile.SendInDirection((projectile.OriginalDirection() + spread*(2f*UnityEngine.Random.value - 1f)).ToVector(), false);
    }
}

public class VeryFragileProjectile : MonoBehaviour
{
    private Projectile _projectile = null;
    public bool breakNextCollision = false;
    public bool phasesThroughBreakables = false;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();

        this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
        this._projectile.specRigidbody.OnCollision += this.OnCollision;
    }

    private void OnPreCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, SpeculativeRigidbody other, PixelCollider otherPixelCollider)
    {
        if (other.GetComponent<AIActor>())
            this._projectile.baseData.damage = Pincushion._NEEDLE_DAMAGE;
        else if (this.phasesThroughBreakables)
            PhysicsEngine.SkipCollision = true;
        else
            this.breakNextCollision = true;
    }

    private void OnCollision(CollisionData data) {
        if (this.breakNextCollision)
            this._projectile.DieInAir(suppressInAirEffects: true);
    }

    // NOTE: called by patch in CwaffPatches
    public static bool IsVeryFragile(Projectile p)
    {
        return p && p.GetComponent<VeryFragileProjectile>();
    }
}
