namespace CwaffingTheGungy;

public class Gunflower : CwaffGun
{
    public static string ItemName         = "Gunflower";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Gunflower>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.PISTOL, reloadTime: 0.9f, ammo: 600, shootFps: 14, reloadFps: 4,
                muzzleFrom: Items.Mailbox, fireAudio: "paintball_shoot_sound", reloadAudio: "paintball_reload_sound");

        Projectile proj = gun.InitProjectile(GunData.New(sprite: null, clipSize: 12, cooldown: 0.18f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 9.0f, speed: 1f, range: 1000f, force: 12f, hitEnemySound: "paintball_impact_enemy_sound", hitWallSound: "paintball_impact_wall_sound"))
          .Attach<LightProjectile>();
        proj.AddLight(useCone: true, fadeInTime: 0.5f, fadeOutTime: 0.25f, color: Color.red, rotateWithParent: false);

        gun.AddLight(useCone: true, fadeInTime: 0.5f, fadeOutTime: 0.25f, color: Color.yellow, grownIn: true, turnOnImmediately: false);
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        if (gun.GetComponentInChildren<EasyLight>() is EasyLight light)
            light.Toggle();
    }
}

public class LightProjectile : MonoBehaviour
{
    private GameActor _owner;
    private EasyLight _light;
    private void Start()
    {
        this._light = base.gameObject.GetComponentInChildren<EasyLight>();
        this._owner = base.gameObject.GetComponent<Projectile>().Owner;
    }

    private void Update()
    {
        if (this._light && this._owner)
            this._light.PointAt(this._owner.CenterPosition);
    }
}
