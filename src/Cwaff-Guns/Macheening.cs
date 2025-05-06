namespace CwaffingTheGungy;

public class Macheening : CwaffGun
{
    public static string ItemName         = "Macheening";
    public static string ShortDescription = "Let the Daggers Fall";
    public static string LongDescription  = "Fires magic blade projectiles conjured through sheer willpower. Requires unbroken concentration while firing, preventing movement or rolling. User receives double damage from all sources while this weapon is equipped.";
    public static string Lore             = "An ancient artifact created by the first great gunsmith, Lord Kagreflak. The range of its blade is bounded only by the focus of its user, which was fantastic for warriors of yore and is awful for most scatterbrained Gungeoneers today. Despite not drawing the attention of the Jammed, you still feel uneasy holding this weapon.";

    private const float _BASE_SPINUP_TIME = 1.2f;

    private static string _PrefireAnim;

    private bool _hasLichguard = false;

    public static void Init()
    {
        Lazy.SetupGun<Macheening>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.FULLAUTO, reloadTime: 0.1f, ammo: 100,
            infiniteAmmo: true, canReloadNoMatterAmmo: true, fireAudio: "macheening_fire_sound", shootFps: 45,
            muzzleFrom: Items.Origuni, dynamicBarrelOffsets: true, percentSpeedWhileCharging: 0.0f, continuousFire: true,
            continuousFireAnimation: true /* makes fire animation not reset with each projectile */)
          .IncreaseLootChance(typeof(Lichguard), 20f)
          .AssignGun(out Gun gun)
          .LoopAnimation(gun.shootAnimation, 4)
          .InitProjectile(GunData.New(ammoCost: 0, clipSize: -1, cooldown: 0.11f, shootStyle: ShootStyle.Automatic, scale: 0.75f,
            damage: 7.0f, speed: 50f, range: 1000f, sprite: "macheening_projectile", hideAmmo: true, spinupTime: _BASE_SPINUP_TIME,
            hitEnemySound: "knife_hit_enemy_sound", hitWallSound: "knife_hit_wall_sound", glowAmount: 20f, glowColor: Color.yellow,
            lightStrength: 5f, lightRange: 2f))
          .SetAllImpactVFX(Items.Excaliber.AsGun().DefaultModule.projectiles[0].hitEffects.enemy)
          .Attach<CombineEvaporateEffect>(c => {
            CombineEvaporateEffect cvePrefab =
                Items.CombinedRifle.AsGun().alternateVolley.projectiles[0].projectiles[0].GetComponent<CombineEvaporateEffect>();
            c.FallbackShader = cvePrefab.FallbackShader;
            c.ParticleSystemToSpawn = cvePrefab.ParticleSystemToSpawn;
          });

        _PrefireAnim = gun.QuickUpdateGunAnimation("prefire", returnToIdle: true, fps: 20);
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
        CwaffEvents.OnStatsRecalculated += this.CheckForLichguard;
        CheckForLichguard(player);
    }

    private void OnTakeDamage(HealthHaver hh, HealthHaver.ModifyDamageEventArgs data)
    {
        if (!this._hasLichguard && this.gun.CurrentOwner is PlayerController player && player.CurrentGun == this.gun)
            data.ModifiedDamage *= 2f;
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        player.healthHaver.ModifyDamage -= this.OnTakeDamage;
        CwaffEvents.OnStatsRecalculated -= this.CheckForLichguard;
        CheckForLichguard(player);
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
        {
            this.PlayerOwner.healthHaver.ModifyDamage -= this.OnTakeDamage;
            CwaffEvents.OnStatsRecalculated -= this.CheckForLichguard;
        }
        base.OnDestroy();
    }

    public override void OnMasteryStatusChanged()
    {
        base.OnMasteryStatusChanged();
        this.spinupTime = _BASE_SPINUP_TIME * (this.Mastered ? 0.5f : 1.0f);
    }

    private void CheckForLichguard(PlayerController player)
    {
        this._hasLichguard = player.HasPassive<Lichguard>();
        this.percentSpeedWhileCharging = this._hasLichguard ? 1.0f : 0.0f;
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (!this.Mastered)
            return;

        projectile.collidesWithProjectiles           = true;
        projectile.collidesOnlyWithPlayerProjectiles = false;
        projectile.UpdateCollisionMask();
        projectile.specRigidbody.OnCollision += this.OnProjectileCollision;
    }

    private void OnProjectileCollision(CollisionData data)
    {
        if (!data.OtherRigidbody || data.OtherRigidbody.gameObject.GetComponent<Projectile>() is not Projectile other)
            return;
        if (other.Owner is PlayerController)
            return;
        if (!data.MyRigidbody || data.MyRigidbody.gameObject.GetComponent<Projectile>() is not Projectile me)
            return;
        me.DieInAir();
        other.DieInAir();
        this.gun.gameObject.Play("aimu_reflect_sound");
    }
}
