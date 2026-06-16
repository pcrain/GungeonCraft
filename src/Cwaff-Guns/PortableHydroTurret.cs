namespace CwaffingTheGungy;

public class PortableHydroTurret : CwaffGun
{
    public static string ItemName         = "Portable Hydro Turret";
    public static string ShortDescription = "Power Rinse";
    public static string LongDescription  = "Fires short bursts of compressed water. Infinite ammo. Does not reveal secret walls.";
    public static string Lore             = "A portable variant of the hydro turret, capable of blasting water with 100 times more force than your average water gun. This sounds impressive, until you consider the combat effectiveness of your average water gun. Maybe you can trigger a bit of rust or snuff out a Muzzle Wisp, if you're lucky.";

    public static void Init()
    {
        Lazy.SetupGun<PortableHydroTurret>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.EXCLUDED, gunClass: GunClass.SHITTY, reloadTime: 0.5f, ammo: 100, shootFps: 30, smoothReload: 0.1f,
            infiniteAmmo: true, fireAudio: "portable_hydro_turret_fire_sound", isStarterGun: true, undroppableStarter: true)
          .SetReloadAudio("portable_hydro_turret_reload_sound", 0, 2, 4, 6)
          .SetMuzzleVFX((ItemHelper.Get(Items.EyeOfTheBeholster) as Gun).DefaultModule.finalProjectile.hitEffects.enemy)
          .InitProjectile(GunData.New(sprite: "portable_hydro_turret_projectile", clipSize: 10, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic, customClip: false,
            damage: 3.0f, speed: 60f, range: 10.0f, force: 1.0f, hitSound: "portable_hydro_turret_impact_sound", useBetterEmissiveShader: true,
            glowAmount: 7.0f, glowColorPower: 7.0f, glowSensitivity: 2.0f, glowColor: new Color(0.5f, 0.5f, 1.0f))) // TODO: custom clip
          .SetAllImpactVFX((ItemHelper.Get(Items.EyeOfTheBeholster) as Gun).DefaultModule.finalProjectile.hitEffects.enemy);
    }
}
