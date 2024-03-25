namespace CwaffingTheGungy;

public class Natascha : AdvancedGunBehavior
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

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Natascha>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.D, gunClass: GunClass.FULLAUTO, reloadTime: 0.0f, ammo: 1500,
                shootFps: (int)((float)_FIRE_ANIM_FRAMES / _BASE_COOLDOWN) + 1,
                muzzleVFX: "muzzle_natascha", muzzleFps: 60, muzzleScale: 0.3f, muzzleAnchor: Anchor.MiddleCenter);
            gun.SetCasing(Items.Ak47);
            gun.AddToSubShop(ItemBuilder.ShopType.Trorc);
            gun.AddToSubShop(ModdedShopType.Rusty);
            gun.GainsRateOfFireAsContinueAttack = true; //NOTE: necessary for the patch below to work
            gun.RateOfFireMultiplierAdditionPerSecond = 0f; // also necessary for patch below

        gun.InitProjectile(GunData.New(clipSize: -1, cooldown: _BASE_COOLDOWN, angleVariance: 15.0f,
          shootStyle: ShootStyle.Automatic, damage: 3.0f, speed: 20.0f, slow: 1.0f, spawnSound: "tomislav_shoot",
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

    //TODO: this might be useful for other guns
    private int ComputeAnimationSpeed()
    {
        float fireMultiplier = this.Player.stats.GetStatValue(PlayerStats.StatType.RateOfFire) * this.GetSpinupFireRate();
        float cooldownTime   = (this.gun.DefaultModule.cooldownTime + this.gun.gunCooldownModifier) / fireMultiplier;
        float fps            = ((float)_FIRE_ANIM_FRAMES / cooldownTime);
        return 1 + Mathf.CeilToInt(fps); // add 1 to FPS to make sure the animation doesn't skip a loop
    }

    private void ResetSpinup(float speedMult = 1.0f)
    {
        if (!this.Player)
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
        this.gun.AddStatToGun(PlayerStats.StatType.MovementSpeed, 1f / (float)Math.Sqrt(this._speedMult), StatModifier.ModifyMethod.MULTIPLICATIVE);
        //HACK: if we rebuild our stats while firing, certain projectile modifiers like scattershot or backup gun make the gun fire once per frame, so work around that
        NataschaMovementSpeedPatch.skipRebuildingGunVolleys = true;
        this.Player.stats.RecalculateStats(this.Player);
        NataschaMovementSpeedPatch.skipRebuildingGunVolleys = false;
    }

    public float GetSpinupFireRate() => (this._speedMult / (1f + _MAX_SPIN_UP));

    private static float ModifyRateOfFire(Gun gun)
    {
        return (gun.GetComponent<Natascha>() is Natascha nat) ? nat.GetSpinupFireRate() : 1f;
    }

    /// <summary>Use Natascha's custom rate of fire spinup code</summary>
    [HarmonyPatch(typeof(Gun), nameof(Gun.HandleModuleCooldown), MethodType.Enumerator)]
    private class NataschaSpinupPatch
    {
        [HarmonyILManipulator]
        private static void NataschaSpinupIL(ILContext il, MethodBase original)
        {
            ILCursor cursor = new ILCursor(il);
            Type ot = original.DeclaringType;

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchAdd())) // immediately after the first add is where we're looking for
                return;

            cursor.Emit(OpCodes.Ldarg_0);  // load enumerator type
            cursor.Emit(OpCodes.Ldfld, AccessTools.GetDeclaredFields(ot).Find(f => f.Name == "$this")); // load actual "$this" field
            cursor.Emit(OpCodes.Call, typeof(Natascha).GetMethod("ModifyRateOfFire", BindingFlags.Static | BindingFlags.NonPublic));
            cursor.Emit(OpCodes.Mul);  // multiply the additional natascha rate of fire by fireMultiplier

            // if (!cursor.TryGotoNext(MoveType.After,
            //   instr => instr.MatchLdfld<Gun>("m_continuousAttackTime"),
            //   instr => instr.MatchMul()))
            //     return;

            // // load the gun itself onto the stack and call our fire speed
            // cursor.Emit(OpCodes.Ldarg_0);  // load enumerator type
            // cursor.Emit(OpCodes.Ldfld, AccessTools.GetDeclaredFields(ot).Find(f => f.Name == "$this")); // load actual "$this" field
            // cursor.Emit(OpCodes.Call, typeof(Natascha).GetMethod("ModifyRateOfFire", BindingFlags.Static | BindingFlags.NonPublic));
        }
    }

    /// <summary>Prevent gun volleys from being rebuilt when recalculating movement speed in Natascha's ResetSpinup() method</summary>
    [HarmonyPatch(typeof(PlayerStats), nameof(PlayerStats.RebuildGunVolleys))]
    private class NataschaMovementSpeedPatch
    {
        internal static bool skipRebuildingGunVolleys = false;
        static bool Prefix(PlayerController owner)
        {
            return !skipRebuildingGunVolleys; // skip original method iff skipRebuildingGunVolleys is true
        }
    }
}
