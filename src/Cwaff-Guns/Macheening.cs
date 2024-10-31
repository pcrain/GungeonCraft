namespace CwaffingTheGungy;

public class Macheening : CwaffGun
{
    public static string ItemName         = "Macheening";
    public static string ShortDescription = "Let the Daggers Fall";
    public static string LongDescription  = "Fires magic blade projectiles conjured through sheer willpower. Requires unbroken concentration while firing, preventing movement or rolling. User receives double damage from all sources while this gun is equipped.";
    public static string Lore             = "TBD";

    private static string _PrefireAnim;

    public static void Init()
    {
        Lazy.SetupGun<Macheening>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.SILLY, reloadTime: 0.1f, ammo: 100,
            infiniteAmmo: true, canReloadNoMatterAmmo: true, fireAudio: "macheening_fire_sound", shootFps: 45,
            muzzleFrom: Items.Origuni, dynamicBarrelOffsets: true, percentSpeedWhileCharging: 0.0f, continuousFire: true,
            continuousFireAnimation: true /* makes fire animation not reset with each projectile */)
          .AssignGun(out Gun gun)
          .LoopAnimation(gun.shootAnimation, 4)
          .InitProjectile(GunData.New(ammoCost: 0, clipSize: -1, cooldown: 0.11f, shootStyle: ShootStyle.Automatic, scale: 0.75f,
            damage: 7.0f, speed: 50f, range: 1000f, sprite: "macheening_projectile", hideAmmo: true, spinupTime: 1.2f,
            hitEnemySound: "knife_hit_enemy_sound", hitWallSound: "knife_hit_wall_sound"))
          .SetAllImpactVFX(Items.Excaliber.AsGun().DefaultModule.projectiles[0].hitEffects.enemy)
          .Assign(out Projectile proj);

        proj.sprite.SetGlowiness(20f, glowColor: Color.yellow);

        _PrefireAnim = gun.QuickUpdateGunAnimation("prefire", returnToIdle: true);
        gun.SetAnimationFPS(_PrefireAnim, 20);
        gun.LoopAnimation(_PrefireAnim, 11);
        gun.SetGunAudio(_PrefireAnim, "macheening_brandish", frame: 6);
    }

    public override void Update()
    {
        base.Update();
        if (this.PlayerOwner is not PlayerController player)
            return;
        if (player.IsDodgeRolling)
        {
            this._spinupRemaining = this.spinupTime;
            BraveInput.GetInstanceForPlayer(player.PlayerIDX).ConsumeAll(GungeonActions.GungeonActionType.Shoot);
        }
        if (this.IsSpinningUp())
            this.gun.spriteAnimator.PlayIfNotPlaying(_PrefireAnim);
        else if (!this.IsSpunUp())
            this.gun.spriteAnimator.PlayIfNotPlaying(this.gun.idleAnimation);
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        player.healthHaver.ModifyDamage += this.OnTakeDamage;
    }

    private void OnTakeDamage(HealthHaver hh, HealthHaver.ModifyDamageEventArgs data)
    {
        if (this.gun.CurrentOwner is PlayerController player && player.CurrentGun == this.gun)
            data.ModifiedDamage *= 2f;
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        player.healthHaver.ModifyDamage -= this.OnTakeDamage;
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
            this.PlayerOwner.healthHaver.ModifyDamage -= this.OnTakeDamage;
        base.OnDestroy();
    }
}
