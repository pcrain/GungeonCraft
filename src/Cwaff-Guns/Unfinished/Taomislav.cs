﻿namespace CwaffingTheGungy;

public class Taomislav : CwaffGun
{
    public static string ItemName         = "Taomislav";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static float                   _BaseCooldownTime = 0.4f;
    internal static int                     _FireAnimationFrames = 8;

    private float _speedMult                      = 1.0f;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Taomislav>(ItemName, ShortDescription, LongDescription, Lore);
            gun.DefaultModule.ammoCost            = 1;
            gun.DefaultModule.shootStyle          = ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle       = ProjectileSequenceStyle.Ordered;
            gun.reloadTime                        = 1.1f;
            gun.DefaultModule.angleVariance       = 15.0f;
            gun.DefaultModule.cooldownTime        = _BaseCooldownTime;
            gun.DefaultModule.numberOfShotsInClip = -1;
            gun.quality                           = ItemQuality.D;
            gun.SetBaseMaxAmmo(2500);
            gun.CurrentAmmo = 2500;
            gun.SetAnimationFPS(gun.shootAnimation, (int)((float)_FireAnimationFrames / _BaseCooldownTime) + 1);

        Projectile projectile = gun.InitFirstProjectile(GunData.New(damage: 3.0f, speed: 20.0f));
            projectile.AddDefaultAnimation(AnimatedBullet.Create(name: "natascha_bullet", fps: 12, scale: 0.5f, anchor: Anchor.MiddleCenter));
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        gun.gameObject.Play("tomislav_shoot");
    }

    private void RecalculateGunStats()
    {
        if (!this.PlayerOwner)
            return;

        this.gun.RemoveStatFromGun(StatType.MovementSpeed);
        this.gun.AddStatToGun(StatType.MovementSpeed.Mult((float)Math.Sqrt(this._speedMult)));
        this.gun.RemoveStatFromGun(StatType.RateOfFire);
        this.gun.AddStatToGun(StatType.RateOfFire.Mult(1.0f / this._speedMult));
        this.PlayerOwner.stats.RecalculateStats(this.PlayerOwner);
    }
}
