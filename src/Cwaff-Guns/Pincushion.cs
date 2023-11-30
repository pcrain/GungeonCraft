namespace CwaffingTheGungy;

public class Pincushion : AdvancedGunBehavior
{
    public static string ItemName         = "Pincushion";
    public static string SpriteName       = "pincushion";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal const int   _SIMULTANEOUS_BULLETS = 4;
    internal const float _MAX_SPREAD           = 45f;
    internal const float _MIN_SPREAD           = 8f;
    internal const float _DLT_SPREAD           = _MAX_SPREAD - _MIN_SPREAD;
    internal const float _NEEDLE_DAMAGE        = 0.5f;

    private static ILHook _VeryFragileILHook;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Pincushion>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.D, gunClass: GunClass.BEAM, reloadTime: 1.8f, ammo: 10000, doesScreenShake: false);
            gun.SetAnimationFPS(gun.shootAnimation, 30);
            gun.SetAnimationFPS(gun.reloadAnimation, 24);
            gun.SetReloadAudio("pincushion_reload_start_sound", frame: 0);
            gun.SetReloadAudio("pincushion_reload_sound", 8, 13, 18, 23, 28, 35);
            gun.SetMuzzleVFX(null); // too many projectiles to benefit from muzzle VFX

        gun.InitProjectile(new(clipSize: 1000 / _SIMULTANEOUS_BULLETS, cooldown: C.FRAME, angleVariance: 35.0f, shootStyle: ShootStyle.Automatic,
          damage: 0.0f, speed: 200.0f, force: 0.0f, range: 999f, bossDamageMult: 0.65f, sprite: "needle", fps: 12,
          anchor: Anchor.MiddleLeft, barrageSize: 4
          )).SetAllImpactVFX(VFX.CreatePool("microdust", fps: 30, loops: false)
          ).Attach<VeryFragileProjectile>(
          ).Attach<EasyTrailBullet>(trail => {
            trail.TrailPos   = trail.transform.position;
            trail.StartWidth = 0.1f;
            trail.EndWidth   = 0f;
            trail.LifeTime   = 0.1f;
            trail.StartColor = Color.gray;
            trail.BaseColor  = Color.gray;
            trail.EndColor   = Color.gray;
          });

        _VeryFragileILHook = new ILHook(
            typeof(MinorBreakable).GetMethod("OnPreCollision", BindingFlags.Instance | BindingFlags.NonPublic),
            VeryFragileIL
            );
    }

    // GetLowDiscrepancyRandom() makes projectiles not spread as randomly as they could, so override that randomness with our own spread
    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (this.Owner as PlayerController is not PlayerController pc)
            return;
        float spread = _MIN_SPREAD + _DLT_SPREAD * (1f - ((float)this.gun.ClipShotsRemaining / (float)this.gun.ClipCapacity));
        spread *= pc.stats.GetStatValue(PlayerStats.StatType.Accuracy);
        projectile.SendInDirection((pc.m_currentGunAngle + spread*(2f*UnityEngine.Random.value - 1f)).ToVector(), false);
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        AkSoundEngine.PostEvent("soul_kaliber_fire", gun.gameObject);
    }

    private static void VeryFragileIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchStloc(0)))
            return;

        // Skip past the part where the MinorBreakable actually breaks if we have the VeryFragileProjectile component
        ILLabel projectileIsNotFragileLabel = cursor.DefineLabel();
        cursor.Emit(OpCodes.Ldloc_0);
        cursor.Emit(OpCodes.Call, typeof(Pincushion).GetMethod("BreakFragileProjectiles", BindingFlags.Static | BindingFlags.NonPublic));
        cursor.Emit(OpCodes.Brfalse, projectileIsNotFragileLabel);
        cursor.Emit(OpCodes.Ret);
        cursor.MarkLabel(projectileIsNotFragileLabel);
    }

    private static bool BreakFragileProjectiles(Projectile p)
    {
        if (p == null)
            return false;
        if (p.GetComponent<VeryFragileProjectile>() is not VeryFragileProjectile fragile)
            return false;

        fragile.breakNextCollision = true;
        return true;
    }
}

public class VeryFragileProjectile : MonoBehaviour
{
    private Projectile _projectile = null;
    public bool breakNextCollision = false;

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
        else
            this.breakNextCollision = true;
    }

    private void OnCollision(CollisionData data) {
        if (this.breakNextCollision)
            this._projectile.DieInAir(suppressInAirEffects: true);
    }
}
