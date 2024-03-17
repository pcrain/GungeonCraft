namespace CwaffingTheGungy;

public class Breegull : AdvancedGunBehavior
{
    public static string ItemName         = "Breegull";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Breegull>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.RIFLE, reloadTime: 1.0f, ammo: 320);
            gun.SetAnimationFPS(gun.shootAnimation, 20);
            gun.SetAnimationFPS(gun.reloadAnimation, 12);
            gun.SetAnimationFPS(gun.introAnimation, 8);
            gun.SetMuzzleVFX(); // innocuous muzzle flash effects
            gun.SetFireAudio("breegull_shoot_sound");
            gun.SetReloadAudio("breegull_reload_sound", 0, 4, 8);
            gun.SetGunAudio(gun.introAnimation, "breegull_intro_sound");
            gun.carryPixelOffset = new IntVector2(12, 0);

        gun.InitProjectile(GunData.New(sprite: "breegull_projectile", clipSize: 10, cooldown: 0.18f, shootStyle: ShootStyle.SemiAutomatic, damage: 5.0f));
    }
}
