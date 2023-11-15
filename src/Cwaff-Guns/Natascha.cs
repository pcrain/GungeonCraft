namespace CwaffingTheGungy;

public class Natascha : AdvancedGunBehavior
{
    public static string ItemName         = "Natascha";
    public static string SpriteName       = "natascha";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Fear no Man";
    public static string LongDescription  = "Rate of fire increases and movement speed decreases as this gun is continuously fired.\n\nThe beloved gun of an amicable literature Ph.D., who refused to let anyone else so much as touch his precious Natascha. That is, until convinced by a hulking Australian man to grant ownership rights in exchange for unlimited lifetime access to the \"best sandwiches south of the equator.\"";

    internal static tk2dSpriteAnimationClip _BulletSprite;
    internal static float                   _BaseCooldownTime = 0.4f;
    internal static int                     _FireAnimationFrames = 8;

    private const float _NATASHA_PROJECTILE_SCALE = 0.5f;
    private float _speedMult                      = 1.0f;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Natascha>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
            gun.SetAttributes(quality: PickupObject.ItemQuality.D, gunClass: GunClass.BEAM, reloadTime: 0.0f, ammo: 2500);
            gun.SetAnimationFPS(gun.shootAnimation, (int)((float)_FireAnimationFrames / _BaseCooldownTime) + 1);
            gun.SetMuzzleVFX("muzzle_natascha", fps: 60, scale: 0.3f, anchor: tk2dBaseSprite.Anchor.MiddleCenter);
            gun.AddToSubShop(ItemBuilder.ShopType.Trorc);
            gun.AddToSubShop(ModdedShopType.Rusty);

        ProjectileModule mod = gun.DefaultModule;
            mod.ammoCost            = 1;
            mod.shootStyle          = ProjectileModule.ShootStyle.Automatic;
            mod.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            mod.angleVariance       = 15.0f;
            mod.cooldownTime        = _BaseCooldownTime;
            mod.numberOfShotsInClip = -1;

        _BulletSprite = AnimateBullet.CreateProjectileAnimation(
            ResMap.Get("natascha_bullet").Base(),
            12, true, new IntVector2((int)(_NATASHA_PROJECTILE_SCALE * 15), (int)(_NATASHA_PROJECTILE_SCALE * 7)),
            false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);

        Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
            projectile.AddDefaultAnimation(_BulletSprite);
            projectile.baseData.damage  = 3f;
            projectile.baseData.speed   = 20.0f;
            projectile.transform.parent = gun.barrelOffset;
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        AkSoundEngine.PostEvent("tomislav_shoot", gun.gameObject);
        if (this._speedMult <= 0.15f)
            return;

        this._speedMult *= 0.85f;
        float secondsBetweenShots = this._speedMult * _BaseCooldownTime;
        gun.AdjustAnimation( // add 1 to FPS to make sure the animation doesn't skip a loop
            gun.shootAnimation, fps: (int)((float)_FireAnimationFrames / secondsBetweenShots) + 1);
        this.RecalculateGunStats();
    }

    public override void OnFinishAttack(PlayerController player, Gun gun)
    {
        this._speedMult = 1.0f;
        gun.AdjustAnimation( // add 1 to FPS to make sure the animation doesn't skip a loop
            gun.shootAnimation, fps: (int)((float)_FireAnimationFrames / _BaseCooldownTime) + 1);
        this.RecalculateGunStats();
    }

    protected override void OnPickedUpByPlayer(PlayerController player)
    {
        base.OnPickedUpByPlayer(player);
        player.OnRollStarted += this.OnDodgeRoll;
    }

    protected override void OnPostDroppedByPlayer(PlayerController player)
    {
        base.OnPostDroppedByPlayer(player);
        player.OnRollStarted -= this.OnDodgeRoll;
    }

    private void OnDodgeRoll(PlayerController player, Vector2 dirVec)
    {
        this._speedMult = 1.0f;
        this.RecalculateGunStats();
    }

    private void RecalculateGunStats()
    {
        if (!this.Player)
            return;

        this.gun.RemoveStatFromGun(PlayerStats.StatType.MovementSpeed);
        this.gun.AddStatToGun(PlayerStats.StatType.MovementSpeed, (float)Math.Sqrt(this._speedMult), StatModifier.ModifyMethod.MULTIPLICATIVE);
        this.gun.RemoveStatFromGun(PlayerStats.StatType.RateOfFire);
        this.gun.AddStatToGun(PlayerStats.StatType.RateOfFire, 1.0f / this._speedMult, StatModifier.ModifyMethod.MULTIPLICATIVE);
        this.Player.stats.RecalculateStats(this.Player);
    }
}
