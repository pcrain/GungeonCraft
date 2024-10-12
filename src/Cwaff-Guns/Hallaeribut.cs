namespace CwaffingTheGungy;

using static Hallaeribut.State;

public class Hallaeribut : CwaffGun
{
    public static string ItemName         = "Hallaeribut";
    public static string ShortDescription = "Modern Warfare Cod";
    public static string LongDescription  = "Fires piranhas that devour any targets in their sight. Becomes increasingly hungry as ammo is depleted, spawning more piranhas per shot but having increasingly negative side effects: when Peckish, cannot be dropped; when Hungry, cannot pick up ammo for other guns; when Starving, cannot switch to other guns; when Famished, feeds on the player every 30 seconds.";
    public static string Lore             = "Piranhas are not known to be picky eaters. It's uncertain who first brought them into the Gungeon or for what purpose, but the fact that they're more than willing to eat flesh, metal, and lead is by all means a good enough reason to stick them in a gun and fire away. Just be extra careful whan handling the ammunition....";

    private const int _SWARM_SIZE = 2;
    private const int _BURSTS_PER_CLIP = 5;
    private const float _STARVE_TIMER = 30f;

    // NOTE: spaced to be at 25% increments assuming we shoot 2, 4, 6, and 8 shots 40 times each == 800 total ammo
    private static readonly float[] _AmmoThresholds = [1.0f, 0.9f, 0.7f, 0.4f, 0.0f];
    internal enum State {
        Satiated, //
        Peckish,  //   can't drop gun
        Hungry,   // + can't pick up ammo for other guns
        Starving, // + can't switch to a different gun
        Famished, // + feeds on the player every 30 seconds
    }

    internal static GameObject _BiteVFX;

    private State _state = Satiated;
    private float _famishTimer = 0.0f;

    public static void Init()
    {
        Lazy.SetupGun<Hallaeribut>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 1.2f, ammo: 800, shootFps: 24, reloadFps: 16,
            loopReloadAt: 0, muzzleVFX: "muzzle_hallaeribut", muzzleFps: 30, muzzleScale: 0.5f, fireAudio: "chomp_small_sound",
            reloadAudio: "chomp_small_sound", modulesAreTiers: true)
          .Attach<Unthrowable>() // throwing circumvents a primary mechanic, so don't allow it
          .Attach<HallaeributAmmoDisplay>()
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(sprite: "hallaeribut_projectile", fps: 24, scale: 0.75f, clipSize: 32, cooldown: 0.33f,
            shootStyle: ShootStyle.Burst, angleVariance: 20f, damage: 9.0f, speed: 75f, range: 1000f, force: 12f, burstCooldown: 0.04f,
            customClip: true))
          .AudioEvent("snap_sound") // play every time animation returns to frame 0, not just on projectile creation
          .Attach<HallaeributProjectile>()
          .AddTrailToProjectilePrefab("hallaeribut_trail", fps: 24, cascadeTimer: C.FRAME, softMaxLength: 1f);

        ProjectileModule mod = gun.DefaultModule;
        gun.Volley.projectiles = new(_AmmoThresholds.Length - 1);
        for (int i = 1; i < _AmmoThresholds.Length; ++i)
        {
            ProjectileModule newMod = ProjectileModule.CreateClone(mod, inheritGuid: false);
            newMod.burstShotCount = i * _SWARM_SIZE;
            newMod.numberOfShotsInClip = newMod.burstShotCount * _BURSTS_PER_CLIP;
            gun.Volley.projectiles.Add(newMod);
        } //REFACTOR: burst builder

        _BiteVFX = VFX.Create("bite_vfx", fps: 40, loops: false, scale: 0.33f);
    }

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner || !this.PlayerOwner.AcceptingNonMotionInput)
            return;
        UpdateAmmo();
        UpdateStarvation();
        this.gun.m_prepThrowTime = -999f; //HACK: prevent the gun from being thrown
    }

    private void UpdateStarvation()
    {
        if (this._state != Famished)
            return;
        if ((BraveTime.ScaledTimeSinceStartup - this._famishTimer) < _STARVE_TIMER)
            return;

        this._famishTimer = BraveTime.ScaledTimeSinceStartup;
        if (this.PlayerOwner.healthHaver is not HealthHaver hh || hh.IsDead)
            return;

        this.PlayerOwner.healthHaver.ApplyDamage(0.5f, Vector2.zero, "Insatiable Hunger", CoreDamageTypes.None,
            DamageCategory.Unstoppable, ignoreInvulnerabilityFrames: true);
        CwaffVFX.Spawn(prefab: Hallaeribut._BiteVFX, position: this.PlayerOwner.CenterPosition, lifetime: 0.3f, fadeOutTime: 0.1f);
        this.PlayerOwner.gameObject.Play("chomp_large_sound");
    }

    private int _cachedAmmo = -1;
    private void UpdateAmmo()
    {
        if (this._cachedAmmo == this.gun.CurrentAmmo)
            return;
        this._cachedAmmo = this.gun.CurrentAmmo;
        float ammoPercent = (float)this._cachedAmmo / this.gun.AdjustedMaxAmmo;
        int ti = _AmmoThresholds.FirstGE(ammoPercent);
        UpdateState((State)ti);
        int newTier = Mathf.Max(ti - 1, 0);
        if (this.gun.CurrentStrengthTier != newTier)
            this.gun.CurrentStrengthTier = newTier;
    }

    private void UpdateState(State newState)
    {
        if (this._state == newState)
            return;

        this.gun.CanBeDropped = newState == Satiated;
        this.gun.CanBeSold = this.gun.CanBeDropped;
        this.PlayerOwner.inventory.GunLocked.SetOverride(ItemName, newState >= Starving);
        if (this._state != Famished && newState == Famished)
            this._famishTimer = BraveTime.ScaledTimeSinceStartup;

        this._state = newState;
    }

    [HarmonyPatch(typeof(AmmoPickup), nameof(AmmoPickup.Interact))]
    private class AmmoPickupInteractPatch
    {
        static bool Prefix(AmmoPickup __instance, PlayerController interactor)
        {
            if (interactor.GetGun<Hallaeribut>() is not Hallaeribut hal)
                return true; // call the original method
            hal.UpdateAmmo(); // refresh ammo just in case it's changed
            if (hal._state <= Peckish)
                return true; // call the original method
            if (interactor.CurrentGun == hal.gameObject.GetComponent<Gun>())
                return true; // call the original method

            foreach (var label in GameUIRoot.Instance.m_extantReloadLabels)
                label.ProcessMarkup = true;
            GameUIRoot.Instance.InformNeedsReload(interactor, new Vector3(interactor.specRigidbody.UnitCenter.x - interactor.transform.position.x, 1.25f, 0f),
                1f, "[color #dd6666]It Hungers[/color]");
            return false;    // skip the original method
        }
    }

    private class HallaeributAmmoDisplay : CustomAmmoDisplay
    {
        private Gun _gun;
        private Hallaeribut _hal;
        private PlayerController _owner;

        private void Start()
        {
            this._gun = base.GetComponent<Gun>();
            this._hal = this._gun.GetComponent<Hallaeribut>();
            this._owner = this._gun.CurrentOwner as PlayerController;
        }

        public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
        {
            if (!this._owner)
                return false;

            if (this._hal._state == Famished)
                uic.GunAmmoCountLabel.Text = $"[color #bb33bb]{this._hal._state}[/color]\n{this._owner.VanillaAmmoDisplay()}";
            else
                uic.GunAmmoCountLabel.Text = $"{this._hal._state}\n{this._owner.VanillaAmmoDisplay()}";
            return true;
        }
    }
}

public class HallaeributProjectile : MonoBehaviour
{
    private const float _DECEL_START = 0.05f;
    private const float _HALT_START  = 0.25f;
    private const float _RELAUNCH_START  = 0.5f;
    private const float _LERP_RATE = 10f;

    private Projectile _projectile;
    private float _lifetime = 0f;
    private State _state = State.START;
    private float _startSpeed;
    private AIActor _target;

    private enum State
    {
        START,
        DECEL,
        HALT,
        RELAUNCH,
    }

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._projectile.OnHitEnemy += this.OnHitEnemy;
        this._startSpeed = this._projectile.baseData.speed;
        this._projectile.m_usesNormalMoveRegardless = true; // ignore all motion module overrides, helix bullets doeesn't play well with speed changing projectiles
    }

    private void OnHitEnemy(Projectile arg1, SpeculativeRigidbody arg2, bool arg3)
    {
        Vector2 center = (arg2.sprite is tk2dBaseSprite sprite) ? sprite.WorldCenter : arg2.UnitCenter;
        CwaffVFX.Spawn(prefab: Hallaeribut._BiteVFX, position: center, lifetime: 0.3f, fadeOutTime: 0.1f);
        arg2.gameObject.Play("chomp_large_sound");
    }

    private void Update()
    {
        this._lifetime += BraveTime.DeltaTime;
        switch (this._state)
        {
            case State.START:
                if (this._lifetime >= _DECEL_START)
                    this._state = State.DECEL;
                break;
            case State.DECEL:
                if (this._lifetime >= _HALT_START)
                {
                    this._projectile.baseData.speed = 0.01f;
                    this._target = Lazy.NearestEnemy(this._projectile.SafeCenter);
                    this._state = State.HALT;
                }
                else
                  this._projectile.baseData.speed = Lazy.SmoothestLerp(this._projectile.baseData.speed, 0f, _LERP_RATE);
                this._projectile.UpdateSpeed();
                break;
            case State.HALT:
                if (this._lifetime >= _RELAUNCH_START)
                {
                    if (this._target)
                        this._projectile.SendInDirection(this._target.CenterPosition - this._projectile.SafeCenter, false);
                    // this._projectile.UpdateSpeed();
                    this._state = State.RELAUNCH;
                }
                break;
            case State.RELAUNCH:
                this._projectile.baseData.speed = Lazy.SmoothestLerp(this._projectile.baseData.speed, this._startSpeed, _LERP_RATE);
                this._projectile.UpdateSpeed();
                break;
        }
    }
}
