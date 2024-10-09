namespace CwaffingTheGungy;

public class Natascha : CwaffGun
{
    public static string ItemName         = "Natascha";
    public static string ShortDescription = "Fear no Man";
    public static string LongDescription  = "Rate of fire increases and movement speed decreases as this gun is continuously fired. Reloading toggles whether the gun remains spun up while not firing, maintaining both the increased fire rate and reduced movement speed.";
    public static string Lore             = "The beloved gun of an amicable literature Ph.D., who refused to let anyone else so much as touch his precious Natascha. That is, until convinced by a hulking Australian man to grant ownership rights in exchange for unlimited lifetime access to the \"best sandwiches south of the equator.\"";

    private const float _SPIN_UP_TIME     = 3.5f;
    private const float _MAX_SPIN_UP      = 8.0f;
    private const float _BASE_COOLDOWN    = 0.05f;
    private const int   _FIRE_ANIM_FRAMES = 8;

    private float _rawSpinupTime = 0.0f;
    private float _speedMult     = 1.0f;
    private bool _maintainSpinup = false;

    public static void Init()
    {
        Lazy.SetupGun<Natascha>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.D, gunClass: GunClass.FULLAUTO, reloadTime: 0.0f, ammo: 1500,
            shootFps: (int)((float)_FIRE_ANIM_FRAMES / _BASE_COOLDOWN) + 1, rampUpFireRate: true,
            muzzleVFX: "muzzle_natascha", muzzleFps: 60, muzzleScale: 0.3f, muzzleAnchor: Anchor.MiddleCenter)
          .SetCasing(Items.Ak47)
          .AddToShop(ItemBuilder.ShopType.Trorc)
          .AddToShop(ModdedShopType.Rusty)
          .InitProjectile(GunData.New(clipSize: -1, cooldown: _BASE_COOLDOWN, angleVariance: 15.0f,
            shootStyle: ShootStyle.Automatic, damage: 3.0f, speed: 20.0f, slow: 1.0f, spawnSound: "tomislav_shoot",
            sprite: "natascha_bullet", fps: 12, scale: 0.5f, anchor: Anchor.MiddleCenter, customClip: true));
    }

    public override void Update()
    {
        base.Update();

        if (this.PlayerOwner is not PlayerController player)
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

        if (this._rawSpinupTime < _SPIN_UP_TIME)
        {
            this._rawSpinupTime += BraveTime.DeltaTime;
            float spinupPercent = this._rawSpinupTime / _SPIN_UP_TIME;
            float curSpinUp = 1f + _MAX_SPIN_UP * spinupPercent * spinupPercent;
            ResetSpinup(curSpinUp);
        }
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
        if (this._speedMult == 1.0f)
        {
            gun.gameObject.Play("minigun_wind_down_stop");
            // gun.gameObject.Play("minigun_wind_up");
            gun.gameObject.Play("minigun_spin");
        }
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        player.OnRollStarted += this.OnDodgeRoll;
        this._maintainSpinup = false;
        ResetSpinup();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        player.OnRollStarted -= this.OnDodgeRoll;
        this._maintainSpinup = false;
        ResetSpinup();
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
            this.PlayerOwner.OnRollStarted -= this.OnDodgeRoll;
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

    //TODO: this might be useful for other guns
    private int ComputeAnimationSpeed()
    {
        float fireMultiplier = this.PlayerOwner.stats.GetStatValue(PlayerStats.StatType.RateOfFire) * this.GetDynamicFireRate();
        float cooldownTime   = (this.gun.DefaultModule.cooldownTime + this.gun.gunCooldownModifier) / fireMultiplier;
        float fps            = ((float)_FIRE_ANIM_FRAMES / cooldownTime);
        return 1 + Mathf.CeilToInt(fps); // add 1 to FPS to make sure the animation doesn't skip a loop
    }

    private void ResetSpinup(float speedMult = 1.0f)
    {
        if (!this.PlayerOwner)
            return;

        if (this._speedMult > 1.0f && speedMult == 1.0f)
        {
            gun.gameObject.Play("minigun_spin_stop");
            gun.gameObject.Play("minigun_wind_up_stop");
            gun.gameObject.Play("minigun_wind_down");
            this._rawSpinupTime = 0.0f;
        }
        this._speedMult = speedMult;
        gun.AdjustAnimation(gun.shootAnimation, fps: ComputeAnimationSpeed());

        this.gun.RemoveStatFromGun(PlayerStats.StatType.MovementSpeed);
        if (this.PlayerOwner.HasSynergy(Synergy.MASTERY_NATASCHA))
            this.gun.AddStatToGun(PlayerStats.StatType.MovementSpeed, 1f, StatModifier.ModifyMethod.MULTIPLICATIVE);
        else
            this.gun.AddStatToGun(PlayerStats.StatType.MovementSpeed, 1f / (float)Math.Sqrt(this._speedMult), StatModifier.ModifyMethod.MULTIPLICATIVE);
        //NOTE: if we rebuild our stats while firing, certain projectile modifiers like scattershot or backup gun make the gun fire once per frame, so work around that
        this.PlayerOwner.stats.RecalculateStatsWithoutRebuildingGunVolleys(this.PlayerOwner); //Alexandria helper
    }

    //NOTE: Only works if GainsRateOfFireAsContinueAttack is true (i.e., rampUpFireRate: true is set in attributes)
    public override float GetDynamicFireRate() => (this._speedMult / (1f + _MAX_SPIN_UP));
}
