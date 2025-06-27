namespace CwaffingTheGungy;

public class Breegull : CwaffGun
{
    public static string ItemName         = "Breegull";
    public static string ShortDescription = "Rare Wares";
    public static string LongDescription  = "Fires eggs with varying effects and ammo costs. Reloading with a full clip cycles through the following egg types:\n normal (1 ammo)\n fire (2 ammo)\n grenade (5 ammo)\n ice (2 ammo)\n clockwork (3 ammo)";
    public static string Lore             = "With bear no more,\n  the bird's alone.\nAmidst the Gungeon's\n  walls of stone.\nArmed with her trusty\n  eggs and beak.\nShe'll help you kill\n  the past you seek.\n- Guntilda";

    private const float TALON_TROT_TIMER = 0.16f;

    internal static GameObject _Shrapnel     = null;
    internal static GameObject _TalonDust    = null;
    internal static Projectile _EggNormal    = null;
    internal static Projectile _EggFire      = null;
    internal static Projectile _EggGrenade   = null;
    internal static Projectile _EggIce       = null;
    internal static Projectile _EggClockwork = null;
    internal static List<EggData> _Eggs      = new();

    internal int _currentEggType = 0;
    private float _noiseTimer = 0.0f;
    private bool _altNoise = false;
    private int _trueAmmo = -1;

    internal class EggData
    {
        public Projectile projectile;
        public string sound;
        public string ui;
        public int ammo;
    }

    public static void Init()
    {
        Lazy.SetupGun<Breegull>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 1.0f, ammo: 480, shootFps: 20, reloadFps: 12,
            introFps: 8, fireAudio: "breegull_shoot_sound", introAudio: "breegull_intro_sound", carryOffset: new IntVector2(6, 0))
          .SetReloadAudio("breegull_reload_sound", 0, 4, 8)
          .Attach<BreegullAmmoDisplay>()
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(sprite: "breegull_projectile_normal", clipSize: 10, cooldown: 0.18f, shootStyle: ShootStyle.SemiAutomatic, damage: 7.0f,
            shrapnelVFX: VFX.Create("breegull_impact_normal"), shrapnelCount: 10, destroySound: "egg_hit_enemy_sound", customClip: true))
          .Assign(out _EggNormal);

        gun.QuickUpdateGunAnimation($"dragon_idle", fps: 1);
        gun.SetGunAudio(gun.QuickUpdateGunAnimation($"dragon_reload", fps: 12, returnToIdle: true), "breegull_dragon_reload_sound", 0, 4, 8);
        gun.SetGunAudio(gun.QuickUpdateGunAnimation($"dragon_fire", fps: 20, returnToIdle: true), "breegull_dragon_shoot_sound");
        gun.SetGunAudio(gun.QuickUpdateGunAnimation($"dragon_intro", fps: 8, returnToIdle: true), "breegull_dragon_intro_sound");

        _EggFire      = _EggNormal.Clone(GunData.New(sprite: "breegull_projectile_fire", shrapnelVFX: VFX.Create("breegull_impact_fire"), fire: 1.0f));
        _EggGrenade   = _EggNormal.Clone(GunData.New(sprite: "breegull_projectile_grenade", shrapnelVFX: VFX.Create("breegull_impact_grenade")))
          .Attach<ExplosiveModifier>(ex => ex.explosionData = Explosions.DefaultLarge);
        _EggIce       = _EggNormal.Clone(GunData.New(sprite: "breegull_projectile_ice", shrapnelVFX: VFX.Create("breegull_impact_ice"), freeze: 0.75f));
        _EggClockwork = _EggNormal.Clone(GunData.New(sprite: "breegull_projectile_clockwork", shrapnelVFX: VFX.Create("breegull_impact_clockwork")))
          .Attach<ExplosiveModifier>(ex => ex.explosionData = Explosions.DefaultSmall.With(
            damage: 7f, force: 20f, debrisForce: 10f, radius: 0.5f, preventPlayerForce: true, shake: false))
          .Attach<HomingModifier>(home => { home.HomingRadius = 10f; home.AngularVelocity = 720f; });

        _Eggs = new List<EggData>() {
            new EggData(){ projectile = _EggNormal,    sound = "collect_egg_normal_sound",    ui = "breegull_normal_ui",    ammo = 1, },
            new EggData(){ projectile = _EggFire,      sound = "collect_egg_fire_sound",      ui = "breegull_fire_ui",      ammo = 2, },
            new EggData(){ projectile = _EggGrenade,   sound = "collect_egg_grenade_sound",   ui = "breegull_grenade_ui",   ammo = 5, },
            new EggData(){ projectile = _EggIce,       sound = "collect_egg_ice_sound",       ui = "breegull_ice_ui",       ammo = 2, },
            new EggData(){ projectile = _EggClockwork, sound = "collect_egg_clockwork_sound", ui = "breegull_clockwork_ui", ammo = 3, },
        };

        _TalonDust = VFX.Create("talon_trot_dust", fps: 30, loops: false);
    }

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner)
            return;
        if (this.Mastered && this._currentEggType == 1 && this.gun.ClipShotsRemaining < this.gun.DefaultModule.numberOfShotsInClip)
            this.gun.ClipShotsRemaining = this.gun.DefaultModule.numberOfShotsInClip;
        if (!this.PlayerOwner.HasSynergy(Synergy.TALON_TROT))
            return;
        if (!this.gun.spriteAnimator.CurrentClip.name.Contains("idle"))
            return;
        if (!this.PlayerOwner.spriteAnimator.CurrentClip.name.Contains("run_"))
        {
            this._noiseTimer = 0.0f;
            this._altNoise = false;
            return;
        }
        if ((_noiseTimer += BraveTime.DeltaTime) < TALON_TROT_TIMER)
            return;
        this.PlayerOwner.gameObject.Play(_altNoise ? "kazooie_walk_b" : "kazooie_walk_a");
        this._noiseTimer = 0.0f;
        this._altNoise = !this._altNoise;
        SpawnManager.SpawnVFX(_TalonDust, this.PlayerOwner.sprite.WorldBottomCenter, Quaternion.identity);
    }

    public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
    {
        UpdateEggs(playSound: false);
        return _Eggs[this._currentEggType].projectile;
    }

    public override void OnFullClipReload(PlayerController player, Gun gun)
    {
        this._currentEggType = (this._currentEggType + 1) % _Eggs.Count;
        UpdateEggs(playSound: true);
    }

    public override void OnMasteryStatusChanged()
    {
        base.OnMasteryStatusChanged();
        CheckDragonForm(force: true);
    }

    internal void CheckDragonForm(bool force = false)
    {
        if (!this.Mastered && !force)
            return;

        this.gun.idleAnimation = "breegull_dragon_idle";
        this.gun.shootAnimation = "breegull_dragon_fire";
        this.gun.reloadAnimation = "breegull_dragon_reload";
        this.gun.introAnimation = "breegull_dragon_intro";
        this.gun.spriteAnimator.playAutomatically = false;
        this.gun.spriteAnimator.StopAndResetFrameToDefault();
        this.gun.spriteAnimator.Play(this.PlayerOwner ? this.gun.introAnimation : this.gun.idleAnimation);
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        UpdateEggs(playSound: false);
        CheckDragonForm();
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        player.OnRollStarted += this.OnDodgeRoll;
        UpdateEggs(playSound: false);
        CheckDragonForm();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        player.OnRollStarted -= this.OnDodgeRoll;
        if (this._trueAmmo > -1)
        {
            this.gun.CurrentAmmo = this._trueAmmo;
            this._trueAmmo = -1;
        }
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
            this.PlayerOwner.OnRollStarted -= this.OnDodgeRoll;
        base.OnDestroy();
    }

    private void OnDodgeRoll(PlayerController player, Vector2 dirVec)
    {
        if (player.CurrentGun == this.gun && player.HasSynergy(Synergy.TALON_TROT))
            player.gameObject.Play("kazooie_roll_sound");
    }

    private void UpdateEggs(bool playSound = false)
    {
        EggData e = _Eggs[this._currentEggType];
        this.gun.DefaultModule.ammoCost = e.ammo;  //BUG: with certain items, the ammo cost here seems to be ignored...can't replicate though
        bool hadInfiniteAmmo = this.gun.LocalInfiniteAmmo;
        if (this._currentEggType == 0 && this.PlayerOwner && this.PlayerOwner.HasSynergy(Synergy.CHEATO_PAGE))
        {
            this.gun.LocalInfiniteAmmo = true;
            this.gun.DefaultModule.ammoCost = 0;
        }
        else if (this._currentEggType == 1 && this.PlayerOwner && this.Mastered)  // free fire eggs in dragon form
        {
            this.gun.LocalInfiniteAmmo = true;
            this.gun.DefaultModule.ammoCost = 0;
        }
        else
            this.gun.LocalInfiniteAmmo = false;
        if (hadInfiniteAmmo != this.gun.LocalInfiniteAmmo)
        {
            //NOTE: possibly a vanilla bug: LocalInfiniteAmmo isn't respected when the gun has 0 ammo
            if (this.gun.LocalInfiniteAmmo)
            {
                this._trueAmmo = this.gun.CurrentAmmo;
                this.gun.CurrentAmmo = Mathf.Max(this._trueAmmo, this.gun.DefaultModule.numberOfShotsInClip);
                this.gun.MoveBulletsIntoClip(this.gun.DefaultModule.numberOfShotsInClip);
            }
            else
            {
                if (this._trueAmmo != -1)
                    this.gun.CurrentAmmo = this._trueAmmo;
                this._trueAmmo = -1;
            }
        }
        if (playSound)
            base.gameObject.Play(e.sound);
    }

    private class BreegullAmmoDisplay : CustomAmmoDisplay
    {
        private Gun _gun;
        private Breegull _breegull;
        private PlayerController _owner;

        private void Start()
        {
            this._gun      = base.GetComponent<Gun>();
            this._breegull = this._gun.GetComponent<Breegull>();
            this._owner    = this._gun.CurrentOwner as PlayerController;
        }

        public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
        {
            if (!this._owner)
                return false;

            string uiString = Breegull._Eggs[this._breegull._currentEggType].ui;
            uic.GunAmmoCountLabel.Text = $"[sprite \"{uiString}\"]\n{this._owner.VanillaAmmoDisplay()}";
            return true;
        }
    }
}

