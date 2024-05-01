namespace CwaffingTheGungy;

public class Nycterian : AdvancedGunBehavior
{
    public static string ItemName         = "Nycterian";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Nycterian>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.PISTOL, reloadTime: 1.2f, ammo: 300, shootFps: 24, reloadFps: 20,
                fireAudio: "outbreak_shoot_sound", reloadAudio: "outbreak_reload_sound");

        Projectile proj = gun.InitProjectile(GunData.New(clipSize: 10, cooldown: 0.2f, shootStyle: ShootStyle.SemiAutomatic,
          damage: 8.0f, speed: 27.0f, range: 100.0f, sprite: "bat_projectile", fps: 12, anchor: Anchor.MiddleLeft)).Attach<BatProjectile>();
    }
}

public class BatProjectile : MonoBehaviour
{
    private void Start()
    {
        tk2dBaseSprite sprite = base.GetComponent<Projectile>().sprite;
        sprite.usesOverrideMaterial = true;
        Material m = sprite.renderer.material;
        m.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
        m.SetFloat("_EmissivePower", 3f);
        // m.SetColor("_EmissiveColor", new Color(0.25f, 0.25f, 0.25f));
        m.SetFloat("_EmissiveColorPower", 1.55f);
    }
}
