namespace CwaffingTheGungy;

public class Uppskeruvel : AdvancedGunBehavior
{
    public static string ItemName         = "Uppskeruvel"; // é
    public static string SpriteName       = "uppskeruvel";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Uppskeruvel>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 1.2f, ammo: 80);
            gun.SetAnimationFPS(gun.shootAnimation, 30);
            gun.SetAnimationFPS(gun.reloadAnimation, 40);
            gun.SetMuzzleVFX(Items.Mailbox); // innocuous muzzle flash effects

        gun.InitProjectile(new(clipSize: 1, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic,
          sprite: "uppskeruvel_projectile", fps: 12, scale: 0.5f, anchor: Anchor.MiddleLeft)
        // ).Attach<TranquilizerBehavior>(
        );
    }
}
