namespace CwaffingTheGungy;

public class BlasTechF4 : CwaffGun
{
    public static string ItemName         = "BlasTech F-4";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "The BlasTech Alternating F-4 Rifle...";

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<BlasTechF4>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.D, gunClass: GunClass.SHITTY, reloadTime: 0.9f, ammo: 1000, shootFps: 14, reloadFps: 4,
                muzzleFrom: Items.Mailbox);

        gun.InitProjectile(GunData.New(clipSize: 20, cooldown: 0.11f, shootStyle: ShootStyle.Automatic,
          damage: 20.0f, speed: 100f, range: 9999f, force: 12f)
        ).Attach<EasyTrailBullet>(trail => {
          trail.TrailPos   = trail.transform.position;
          trail.StartWidth = 0.3f;
          trail.EndWidth   = 0.05f;
          trail.LifeTime   = 0.07f;
          trail.BaseColor  = ExtendedColours.purple;
          trail.StartColor = Color.Lerp(ExtendedColours.purple, Color.white, 0.25f);
          trail.EndColor   = ExtendedColours.purple;
        });
    }

    public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
    {
        if (projectile.FiredForFree(gun, mod))
            projectile.gameObject.AddComponent<BlasTechFreebie>();
        return projectile;
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (this.PlayerOwner is not PlayerController pc)
            return;

        const float MAX_DEVIATION = 30f;
        projectile.Start(); // NOTE: need to make sure projectile has a valid specrigidbody
        float angle = projectile.transform.rotation.eulerAngles.z;
        bool flipped = Lazy.CoinFlip();
        for (float f = 0f; f < MAX_DEVIATION; f += UnityEngine.Random.Range(1f, 10f))
        {
            float a = angle + (flipped ? f : -f);
            if (projectile.WouldCollideWithEnemy(a, accountForWalls: false, pixelPerfect: false, outset: 2))
                continue;
            if (a != angle)
                projectile.SendInDirection(a.ToVector(), true, true);
            projectile.gameObject.PlayUnique("blastech_fire_sound");
            return;
        }

        if (this.gun == pc.CurrentGun && !projectile.GetComponent<BlasTechFreebie>())
        {
            this.gun.GainAmmo(1);
            this.gun.MoveBulletsIntoClip(1);
        }
        projectile.gameObject.PlayUnique("blastech_jam_sound");
        projectile.DieInAir();
    }

    private class BlasTechFreebie : MonoBehaviour { }
}
