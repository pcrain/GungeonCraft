﻿namespace CwaffingTheGungy;

public class Breegull : AdvancedGunBehavior
{
    public static string ItemName         = "Breegull";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject _Shrapnel     = null;
    internal static Projectile _EggNormal    = null;
    internal static Projectile _EggFire      = null;
    internal static Projectile _EggGrenade   = null;
    internal static Projectile _EggIce       = null;
    internal static Projectile _EggClockwork = null;
    internal static List<EggData> _Eggs      = new();

    internal static string _NormalUI    = $"{C.MOD_PREFIX}:_NormalUI";
    internal static string _FireUI      = $"{C.MOD_PREFIX}:_FireUI";
    internal static string _GrenadeUI   = $"{C.MOD_PREFIX}:_GrenadeUI";
    internal static string _IceUI       = $"{C.MOD_PREFIX}:_IceUI";
    internal static string _ClockworkUI = $"{C.MOD_PREFIX}:_ClockworkUI";

    internal int _currentEggType = 0;

    internal class EggData
    {
        public Projectile projectile;
        public string sound;
        public string ui;
        public int ammo;
    }

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Breegull>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.RIFLE, reloadTime: 1.0f, ammo: 320);
            gun.SetAnimationFPS(gun.shootAnimation, 20);
            gun.SetAnimationFPS(gun.reloadAnimation, 12);
            gun.SetAnimationFPS(gun.introAnimation, 8);
            gun.SetMuzzleVFX(); // no muzzle flash effects
            gun.SetFireAudio("breegull_shoot_sound");
            gun.SetReloadAudio("breegull_reload_sound", 0, 4, 8);
            gun.SetGunAudio(gun.introAnimation, "breegull_intro_sound");
            gun.carryPixelOffset = new IntVector2(6, 0);

        gun.gameObject.AddComponent<BreegullAmmoDisplay>();

        ExplosionData clockworkExplosion = new ExplosionData()
        {
            forceUseThisRadius     = true,
            pushRadius             = 0.5f,
            damageRadius           = 0.5f,
            damageToPlayer         = 0f,
            doDamage               = true,
            damage                 = 5,
            doDestroyProjectiles   = false,
            doForce                = true,
            force                  = 20f,
            debrisForce            = 10f,
            preventPlayerForce     = true,
            explosionDelay         = 0.01f,
            usesComprehensiveDelay = false,
            doScreenShake          = false,
            playDefaultSFX         = true,
            effect                 = Explosions.DefaultSmall.effect,
            ignoreList             = Explosions.DefaultSmall.ignoreList,
            ss                     = Explosions.DefaultSmall.ss,
        };

        _EggNormal = gun.InitProjectile(GunData.New(sprite: "breegull_projectile_normal", clipSize: 10, cooldown: 0.18f, shootStyle: ShootStyle.SemiAutomatic, damage: 5.0f,
            shrapnelVFX: VFX.Create("breegull_impact_normal"), shrapnelCount: 10, deathSound: "egg_hit_enemy_sound"));

        _EggFire      = gun.CloneProjectile(GunData.New(sprite: "breegull_projectile_fire", shrapnelVFX: VFX.Create("breegull_impact_fire"), fire: 0.5f));
        _EggGrenade   = gun.CloneProjectile(GunData.New(sprite: "breegull_projectile_grenade", shrapnelVFX: VFX.Create("breegull_impact_grenade"))
            ).Attach<ExplosiveModifier>(ex => ex.explosionData = Explosions.DefaultLarge);
        _EggIce       = gun.CloneProjectile(GunData.New(sprite: "breegull_projectile_ice", shrapnelVFX: VFX.Create("breegull_impact_ice"), freeze: 0.75f));
        _EggClockwork = gun.CloneProjectile(GunData.New(sprite: "breegull_projectile_clockwork", shrapnelVFX: VFX.Create("breegull_impact_clockwork"))
            ).Attach<ExplosiveModifier>(ex => ex.explosionData = clockworkExplosion
            ).Attach<HomingModifier>(home => { home.HomingRadius = 10f; home.AngularVelocity = 720f; });

        _Eggs = new List<EggData>() {
            new EggData(){ projectile = _EggNormal,    sound = "collect_egg_normal_sound",    ui = _NormalUI,    ammo = 1, },
            new EggData(){ projectile = _EggFire,      sound = "collect_egg_fire_sound",      ui = _FireUI,      ammo = 2, },
            new EggData(){ projectile = _EggGrenade,   sound = "collect_egg_grenade_sound",   ui = _GrenadeUI,   ammo = 5, },
            new EggData(){ projectile = _EggIce,       sound = "collect_egg_ice_sound",       ui = _IceUI,       ammo = 2, },
            new EggData(){ projectile = _EggClockwork, sound = "collect_egg_clockwork_sound", ui = _ClockworkUI, ammo = 4, },
        };
    }

    public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
    {
        if (gun.GetComponent<Breegull>())
            return _Eggs[this._currentEggType].projectile;
        return projectile;
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        if (gun.IsReloading || !manualReload || (gun.ClipShotsRemaining < gun.ClipCapacity))
            return;
        this._currentEggType = (this._currentEggType + 1) % _Eggs.Count;
        UpdateEggs(playSound: true);
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        UpdateEggs(playSound: false);
    }

    private void UpdateEggs(bool playSound = false)
    {
        EggData e = _Eggs[this._currentEggType];
        this.gun.DefaultModule.ammoCost = e.ammo;
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

            uic.SetAmmoCountLabelColor(Color.white);
            uic.GunAmmoCountLabel.AutoHeight = true; // enable multiline text
            uic.GunAmmoCountLabel.ProcessMarkup = true; // enable multicolor text

            string uiString = Breegull._Eggs[this._breegull._currentEggType].ui;
            uic.GunAmmoCountLabel.Text = $"[sprite \"{uiString}\"]\n{this._gun.CurrentAmmo}/{this._gun.AdjustedMaxAmmo}";
            return true;
        }
    }
}
