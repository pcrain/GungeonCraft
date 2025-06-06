namespace CwaffingTheGungy;

public class Pincushion : CwaffGun
{
    public static string ItemName         = "Pincushion";
    public static string ShortDescription = "Needless Violence";
    public static string LongDescription  = "Fires an extraordinarily fast barrage of extremely weak pins that lose accuracy as the clip is depleted. Pins deal fixed damage, cannot damage anything other than enemies, and are completely blocked by decor such as barrels, crates, etc.";
    public static string Lore             = "Even when shot out of a gun, a pin won't be particularly harmful to most of the Gungeon's inhabitants. Two pins won't fare much better, nor three, nor ten. But, by firing thousands of pins per second, one of them will in all likelihood eventually lodge itself in a weak point or some other location extremely compromising to its target. Needless to say, the probabilistic nature of this gun threads the line of practicality, but its high drum capacity doesn't leave it without merits in the right situation.";

    internal const int   _SIMULTANEOUS_BULLETS = 4;
    internal const float _MAX_SPREAD           = 45f;
    internal const float _MIN_SPREAD           = 8f;
    internal const float _DLT_SPREAD           = _MAX_SPREAD - _MIN_SPREAD;
    internal const float _NEEDLE_DAMAGE        = 0.35f;

    internal static GameObject _ImpactVFX = null;

    public static void Init()
    {
        Lazy.SetupGun<Pincushion>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.D, gunClass: GunClass.FULLAUTO, reloadTime: 1.8f, ammo: 10000, doesScreenShake: false,
            shootFps: 30, reloadFps: 24)
          .SetReloadAudio("pincushion_reload_start_sound", 0)
          .SetReloadAudio("pincushion_reload_sound", 8, 13, 18, 23, 28, 35)
      #if DEBUG
          // .Attach<DebugAmmoDisplay>()
      #endif
          .InitProjectile(GunData.New(clipSize: 1000 / _SIMULTANEOUS_BULLETS, cooldown: C.FRAME, angleVariance: 0.0f, shootStyle: ShootStyle.Automatic,
            damage: 0.0f, speed: 200.0f, force: 0.0f, range: 999f, bossDamageMult: 0.65f, sprite: "needle", fps: 12, preventSparks: true, damagesWalls: false,
            anchor: Anchor.MiddleLeft, barrageSize: _SIMULTANEOUS_BULLETS, customClip: true, overrideColliderPixelSizes: new IntVector2(1, 1)/*, glowAmount: 10f*/))
          .ClearAllImpactVFX()
          .RemoveAnimator()
          .Attach<VeryFragileProjectile>()
          .Attach<EasyTrailBullet>(trail => {
            trail.StartWidth = 0.1f;
            trail.EndWidth   = 0f;
            trail.LifeTime   = 0.1f;
            trail.StartColor = Color.gray;
            trail.BaseColor  = Color.gray;
            trail.EndColor   = Color.gray;
          })
          .RegisterAsPoolable()
          ;

      _ImpactVFX = VFX.Create("microdust", fps: 30, loops: false);
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        base.gameObject.Play("pincushion_fire");
    }

    // GetLowDiscrepancyRandom() makes projectiles not spread as randomly as they could, so override that randomness with our own spread
    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (this.PlayerOwner as PlayerController is not PlayerController pc)
            return;

        float spread = _MIN_SPREAD;
        if (this.Mastered)
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

public class VeryFragileProjectile : MonoBehaviour, IPPPComponent
{
    private static tk2dSpriteAnimator _ImpactVfxAnimator;

    private Projectile _projectile = null;
    public bool breakNextCollision = false;
    public bool phasesThroughBreakables = false;

    private void Start()
    {
        if (_ImpactVfxAnimator == null)
          _ImpactVfxAnimator = Pincushion._ImpactVFX.GetComponent<tk2dSpriteAnimator>();
        this._projectile = base.GetComponent<Projectile>();
        //NOTE: this check can be removed once we've fully committed to pooling this
        if (!base.gameObject.GetComponent<PlayerProjectilePoolInfo>())
        {
            SpeculativeRigidbody body = this._projectile.specRigidbody;
            body.OnPreRigidbodyCollision += this.OnPreCollision;
            body.OnCollision += this.OnCollision;
        }
    }

    public void PPPInit(PlayerProjectilePoolInfo pppi)
    {
        pppi.OnPreRigidbodyCollision += this.OnPreCollision;
        pppi.OnCollision += this.OnCollision;
    }

    public void PPPReset(GameObject prefab)
    {
        this.breakNextCollision = false;
        this.phasesThroughBreakables = false;
    }

    public void PPPRespawn()
    {
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
        float xMag = UnityEngine.Random.Range(-14f, 14f);
        float yMag = UnityEngine.Random.Range(-14f, 14f);
        if ((data.Normal.x != 0) && ((data.Normal.x < 0) == xMag > 0))
            xMag = -xMag;
        if ((data.Normal.y != 0) && ((data.Normal.y < 0) == yMag > 0))
            yMag = -yMag;
        CwaffVFX.Spawn( //NOTE: using animator directly to avoid expensive prefab component lookups, since we're making a big mess
          animator: _ImpactVfxAnimator,
          position: data.Contact,
          emissivePower: 10f,
          velocity: new Vector2(xMag, yMag));
    }

    // NOTE: called by patch in CwaffPatches
    internal static bool IsVeryFragile(Projectile p)
    {
        return p && p.GetComponent<VeryFragileProjectile>();
    }
}

#if DEBUG
public class DebugAmmoDisplay : CustomAmmoDisplay
{
    private static StringBuilder _SB = new StringBuilder("", 1000);
    private static int _TotalProjectiles = 0;
    private static int _DroppedFrames = 0;
    private static int _LagSpikes = 0;
    private static float _LastFrameTime = 0;

    private PlayerController _owner;

    private void Start()
    {
        this._owner = base.GetComponent<Gun>().CurrentOwner as PlayerController;

        _TotalProjectiles = 0;
        _DroppedFrames = 0;
        _LagSpikes = 0;
        _LastFrameTime = Time.realtimeSinceStartup;
        StaticReferenceManager.ProjectileAdded -= OnProjectileAdded;
        StaticReferenceManager.ProjectileAdded += OnProjectileAdded;
    }

    private static void OnProjectileAdded(Projectile projectile)
    {
        ++_TotalProjectiles;
    }

    public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
    {
        if (BraveTime.DeltaTime == 0f)
            return true;

        _SB.Length = 0;

        _SB.Append(_TotalProjectiles);
        _SB.Append("  Total Projectiles");
        _SB.Append("\n");

        _SB.Append(StaticReferenceManager.AllProjectiles.Count);
        _SB.Append(" Active Projectiles");
        _SB.Append("\n");

        float dtime = BraveTime.DeltaTime;
        if (BraveTime.DeltaTime > 1f/59f)
            ++_DroppedFrames;
        _SB.Append(_DroppedFrames);
        _SB.Append(" Frames Dropped");
        _SB.Append("\n");

        float now = Time.realtimeSinceStartup;
        if ((now - _LastFrameTime) > 1f/10f)
            ++_LagSpikes;
        _LastFrameTime = now;
        _SB.Append(_LagSpikes);
        _SB.Append(" Lag Spikes");
        _SB.Append("\n");
        _SB.Append(this._owner.VanillaAmmoDisplay());
        uic.GunAmmoCountLabel.Text = _SB.ToString();
        return true;
    }
}
#endif
