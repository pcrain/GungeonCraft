namespace CwaffingTheGungy;

public class Vladimir : AdvancedGunBehavior
{
    public static string ItemName         = "Vladimir";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private List<AIActor> _skeweredEnemies = new();

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Vladimir>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.SILLY, reloadTime: 0.01f, ammo: 100, infiniteAmmo: true, canReloadNoMatterAmmo: true);

        gun.InitProjectile(new(ammoCost: 0, clipSize: -1, cooldown: 0.5f, shootStyle: ShootStyle.SemiAutomatic,
          damage: 0.0f, speed: 0.01f, range: 999.0f)
        ).SetAllImpactVFX(VFX.CreatePool("whip_particles", fps: 20, loops: false, anchor: Anchor.MiddleCenter, scale: 0.5f)
        ).Attach<PistolButtProjectile>(
        ).Attach<VladimirProjectile>();

        // _VladimirProjectile = Items.Ak47.CloneProjectile(new(damage: 30.0f, speed: 1.0f, force: 40.0f, range: 0.01f
        //   )).AddAnimations(AnimatedBullet.Create(name: "pistol_whip_dummy_bullet", fps: 12, anchor: Anchor.MiddleCenter) // Not really visible, just used for pixel collider size
        //   ).SetAllImpactVFX(VFX.CreatePool("whip_particles", fps: 20, loops: false, anchor: Anchor.MiddleCenter, scale: 0.5f)
        //   ).Attach<PistolButtProjectile>();
    }
}


public class VladimirProjectile : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
    }

    private void Update()
    {
      // enter update code here
    }
}
