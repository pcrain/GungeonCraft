namespace CwaffingTheGungy;

public class Natascha : AdvancedGunBehavior
{
    public static string ItemName         = "Natascha";
    public static string SpriteName       = "natascha";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Fear no Man";
    public static string LongDescription  = "Rate of fire increases and movement speed decreases as this gun is continuously fired.";
    public static string Lore             = "The beloved gun of an amicable literature Ph.D., who refused to let anyone else so much as touch his precious Natascha. That is, until convinced by a hulking Australian man to grant ownership rights in exchange for unlimited lifetime access to the \"best sandwiches south of the equator.\"";

    internal static float _BaseCooldownTime    = 0.4f;
    internal static int   _FireAnimationFrames = 8;

    private float _speedMult = 1.0f;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Natascha>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.D, gunClass: GunClass.BEAM, reloadTime: 0.0f, ammo: 2500);
            gun.SetAnimationFPS(gun.shootAnimation, (int)((float)_FireAnimationFrames / _BaseCooldownTime) + 1);
            gun.SetMuzzleVFX("muzzle_natascha", fps: 60, scale: 0.3f, anchor: Anchor.MiddleCenter);
            gun.AddToSubShop(ItemBuilder.ShopType.Trorc);
            gun.AddToSubShop(ModdedShopType.Rusty);

        gun.InitProjectile(clipSize: -1, cooldown: _BaseCooldownTime, angleVariance: 15.0f, shootStyle: ShootStyle.Automatic, damage: 3.0f, speed: 20.0f,
          sprite: "natascha_bullet", fps: 12, scale: 0.5f, anchor: Anchor.MiddleCenter);
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
