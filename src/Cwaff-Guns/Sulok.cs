namespace CwaffingTheGungy;

public class Sulok : CwaffGun
{
    public static string ItemName         = "Sulok";
    public static string ShortDescription = "Breach of Covenant";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static CwaffTrailController _SulokTrailPrefab;

    public static void Init()
    {
        Lazy.SetupGun<Sulok>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.S, gunClass: GunClass.RIFLE, reloadTime: 1.25f, ammo: 20, idleFps: 10, shootFps: 24, reloadFps: 30,
            smoothReload: 0.1f, reloadAudio: "sulok_reload_sound")
          .InitProjectile(GunData.New(clipSize: 2, cooldown: 1.25f, angleVariance: 1.0f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 300.0f, speed: 300.0f, force: 10.0f, range: 1000.0f, spawnSound: "sulok_fire_sound"))
          .Attach<SulokProjectile>();

        _SulokTrailPrefab = VFX.CreateSpriteTrailObject("sulok_beam_mid", fps: 60, startAnim: "sulok_beam_start",
            softMaxLength: 1f, cascadeTimer: C.FRAME, destroyOnEmpty: true);
    }
}

public class SulokProjectile : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private CwaffTrailController _trail = null;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        this._trail = this._projectile.AddTrail(Sulok._SulokTrailPrefab);
        this._trail.gameObject.SetGlowiness(100f);
        this._projectile.sprite.renderer.enabled = false;
        this._projectile.OnWillKillEnemy += this.OnWillKillEnemy;
    }

    private static float SulokEmit(float t)
    {
      if (t < 0.4f)
        return 0f;
      if (t < 0.5f)
        return 10f * (t - 0.4f);
      return 1f;
    }

    private static float SulokFade(float t)
    {
      if (t < 0.5f)
        return t * 0.1f;
      return 0.05f + 2 * (t - 0.5f);
    }

    private void OnWillKillEnemy(Projectile projectile, SpeculativeRigidbody enemy)
    {
        if (enemy.healthHaver is not HealthHaver hh || hh.IsBoss || hh.IsSubboss)
          return;
        if (enemy.aiActor is not AIActor actor || actor.sprite is not tk2dBaseSprite sprite)
          return;
        sprite.DuplicateInWorldAsMesh(optionalPalette: actor.optionalPalette)
          .Dissipate(time: 1.5f, amplitudeStart: 0.0625f, amplitudeEnd: 2f, emissionEnd: 100f,
            easeEmit: SulokEmit, easeFade: SulokFade, easeAmp: SulokFade);
        actor.EraseFromExistenceWithRewards(true);
    }
}
