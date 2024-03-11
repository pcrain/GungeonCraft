namespace CwaffingTheGungy;

public class Natascha : AdvancedGunBehavior
{
    public static string ItemName         = "Natascha";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Fear no Man";
    public static string LongDescription  = "Rate of fire increases and movement speed decreases as this gun is continuously fired. Reloading toggles whether the gun remains spun up while not firing, maintaining both the increased fire rate and reduced movement speed.";
    public static string Lore             = "The beloved gun of an amicable literature Ph.D., who refused to let anyone else so much as touch his precious Natascha. That is, until convinced by a hulking Australian man to grant ownership rights in exchange for unlimited lifetime access to the \"best sandwiches south of the equator.\"";

    internal static float _BaseCooldownTime    = 0.4f;
    internal static int   _FireAnimationFrames = 8;

    private float _speedMult     = 1.0f;
    private bool _maintainSpinup = false;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Natascha>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.D, gunClass: GunClass.FULLAUTO, reloadTime: 0.0f, ammo: 1500);
            gun.SetAnimationFPS(gun.shootAnimation, (int)((float)_FireAnimationFrames / _BaseCooldownTime) + 1);
            gun.SetMuzzleVFX("muzzle_natascha", fps: 60, scale: 0.3f, anchor: Anchor.MiddleCenter);
            gun.SetCasing(Items.Ak47);
            gun.AddToSubShop(ItemBuilder.ShopType.Trorc);
            gun.AddToSubShop(ModdedShopType.Rusty);

        gun.InitProjectile(GunData.New(clipSize: -1, cooldown: _BaseCooldownTime, angleVariance: 15.0f,
          shootStyle: ShootStyle.Automatic, damage: 3.0f, speed: 20.0f, slow: 1.0f,
          sprite: "natascha_bullet", fps: 12, scale: 0.5f, anchor: Anchor.MiddleCenter));
    }

    protected override void Update()
    {
        base.Update();

        if (this.Owner is not PlayerController player)
            return;

        if (player.IsDodgeRolling || (!this._maintainSpinup && !this.gun.IsFiring))
        {
            if (this._speedMult != 1.0f)
                ResetSpinup();
            return;
        }

        if (this._maintainSpinup && this.gun.spriteAnimator.currentClip.name != gun.shootAnimation)
        {
            if (this._speedMult == 1.0f)
            {
                gun.gameObject.Play("minigun_wind_down_stop");
                // gun.gameObject.Play("minigun_wind_up");
                gun.gameObject.Play("minigun_spin");
            }
            this.gun.spriteAnimator.currentClip = gun.spriteAnimator.GetClipByName(gun.shootAnimation);
            this.gun.spriteAnimator.Play();
        }

        if (this._speedMult > 0.15f)
            ResetSpinup(Mathf.Max(0.15f, this._speedMult - BraveTime.DeltaTime * 0.4f));
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        if (!player.IsDodgeRolling && player.AcceptingNonMotionInput)
            this._maintainSpinup = !this._maintainSpinup;
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        gun.gameObject.Play("tomislav_shoot");
        if (this._speedMult == 1.0f)
        {
            gun.gameObject.Play("minigun_wind_down_stop");
            // gun.gameObject.Play("minigun_wind_up");
            gun.gameObject.Play("minigun_spin");
        }
    }

    protected override void OnPickedUpByPlayer(PlayerController player)
    {
        base.OnPickedUpByPlayer(player);
        player.OnRollStarted += this.OnDodgeRoll;
        this._maintainSpinup = false;
        ResetSpinup();
    }

    protected override void OnPostDroppedByPlayer(PlayerController player)
    {
        base.OnPostDroppedByPlayer(player);
        player.OnRollStarted -= this.OnDodgeRoll;
        this._maintainSpinup = false;
        ResetSpinup();
    }

    public override void OnDestroy()
    {
        if (this.Player)
            this.Player.OnRollStarted -= this.OnDodgeRoll;
        base.OnDestroy();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        this._maintainSpinup = false;
        ResetSpinup();
    }

    private void OnDodgeRoll(PlayerController player, Vector2 dirVec)
    {
        this._maintainSpinup = false;
        ResetSpinup();
    }

    public override void OnFinishAttack(PlayerController player, Gun gun)
    {
        if (!this._maintainSpinup)
            ResetSpinup();
    }

    private void ResetSpinup(float speedMult = 1.0f)
    {
        if (this._speedMult < 1.0f && speedMult == 1.0f)
        {
            gun.gameObject.Play("minigun_spin_stop");
            gun.gameObject.Play("minigun_wind_up_stop");
            gun.gameObject.Play("minigun_wind_down");
        }
        this._speedMult = speedMult;
        gun.AdjustAnimation( // add 1 to FPS to make sure the animation doesn't skip a loop
            gun.shootAnimation, fps: (int)((float)_FireAnimationFrames / (this._speedMult * _BaseCooldownTime)) + 1);
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
