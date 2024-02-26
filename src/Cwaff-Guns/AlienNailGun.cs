namespace CwaffingTheGungy;

public class AlienNailgun : AdvancedGunBehavior
{
    public static string ItemName         = "Alien Nailgun";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<AlienNailgun>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.PISTOL, reloadTime: 0.0f, ammo: 175);
            gun.SetAnimationFPS(gun.idleAnimation, 24);
            gun.SetAnimationFPS(gun.shootAnimation, 24);
            gun.SetMuzzleVFX(Items.Mailbox); // innocuous muzzle flash effects
            gun.SetFireAudio("paintball_shoot_sound");
            gun.SetReloadAudio("paintball_reload_sound");
            gun.AddToSubShop(ItemBuilder.ShopType.Goopton);

        gun.InitProjectile(new(clipSize: -1, cooldown: 0.7f, shootStyle: ShootStyle.SemiAutomatic, damage: 9.0f,
            sprite: "alien_nailgun_projectile", customClip: true)
          );
    }
}
