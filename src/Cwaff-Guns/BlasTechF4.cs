namespace CwaffingTheGungy;

public class BlasTechF4 : CwaffGun
{
    public static string ItemName         = "BlasTech F-4";
    public static string ShortDescription = "Imperial Quality";
    public static string LongDescription  = "Rapidly fires strong, fast projectiles with absolutely zero side effects.";
    public static string Lore             = "Based on the beloved E-11 model, the BlasTech Alternating F-4 Rifle improves upon the previous generation of blasters in power, speed, range, and precision. The difference in performance is so noticeable that regardless of a Gungeoneer's training or experience, each and every firefight will be more consistent than ever.";

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<BlasTechF4>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.D, gunClass: GunClass.SHITTY, reloadTime: 0.75f, ammo: 1000, shootFps: 30, reloadFps: 12,
                muzzleFrom: Items.Mailbox, banFromBlessedRuns: true);
            gun.SetReloadAudio("blastech_jam_sound", 0, 2, 4, 5, 6);

        gun.InitProjectile(GunData.New(clipSize: 20, cooldown: 0.11f, shootStyle: ShootStyle.Automatic,
            damage: 20.0f, speed: 100f, range: 9999f, force: 12f, ignoreDamageCaps: true)
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

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (this.PlayerOwner is not PlayerController pc)
            return;

        const float MAX_DEVIATION = 30f;
        projectile.Start(); // NOTE: need to make sure projectile has a valid specrigidbody
        float oldAngle = projectile.transform.rotation.eulerAngles.z;
        bool flipped = Lazy.CoinFlip();
        for (float f = 0f; f < MAX_DEVIATION; f += UnityEngine.Random.Range(1f, 10f))
        {
            float newAngle = oldAngle + (flipped ? f : -f);
            if (projectile.WouldCollideWithEnemy(newAngle, accountForWalls: false, pixelPerfect: false, outset: 2))
                continue;
            if (newAngle != oldAngle)
                projectile.SendInDirection(newAngle.ToVector(), true, true);
            projectile.gameObject.PlayUnique("blastech_fire_sound");
            return;
        }

        if (this.gun == pc.CurrentGun && !projectile.FiredForFree())
        {
            this.gun.GainAmmo(1);
            this.gun.MoveBulletsIntoClip(1);
        }
        projectile.gameObject.PlayUnique("blastech_jam_sound");
        projectile.DieInAir(suppressInAirEffects: true);
    }
}
