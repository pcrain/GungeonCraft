namespace CwaffingTheGungy;

public class Tranquilizer : AdvancedGunBehavior
{
    public static string ItemName         = "Tranquilizer";
    public static string SpriteName       = "tranquilizer";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Zzzzzz";
    public static string LongDescription  = "Fires projectiles that permastun enemies after a few seconds, scaling logarithmically with their current health.";
    public static string Lore             = "Most commonly used for sedating loudly-opinionated supermarket shoppers and other similarly aggressive wild animals, the tranquilizer gun is the pinnacle of non-lethal firearm technology. What it lacks in visual spectacle or firepower it more than makes up for with raw practicality, able to completely pacify all but the mightiest of the Gungeon's denizens with a single shot and a few seconds of your time. As long as you have a plan in place for not getting shot for those few precious seconds, it's hard to beat in terms of ammo-efficiency for dispatching the Gundead.";

    internal static GameObject _DrowsyVFX = null;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Tranquilizer>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.D, gunClass: GunClass.POISON, reloadTime: 1.2f, ammo: 80);
            gun.SetAnimationFPS(gun.shootAnimation, 30);
            gun.SetAnimationFPS(gun.reloadAnimation, 40);
            gun.SetMuzzleVFX(Items.Mailbox); // innocuous muzzle flash effects
            gun.SetFireAudio("blowgun_fire_sound");
            gun.SetReloadAudio("blowgun_reload_sound");

        gun.DefaultModule.SetAttributes(clipSize: 1, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic, customClip: SpriteName);

        Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
            projectile.AddDefaultAnimation(AnimatedBullet.Create(name: "tranquilizer_projectile", fps: 12, anchor: Anchor.MiddleLeft));
            projectile.transform.parent = gun.barrelOffset;
            projectile.gameObject.AddComponent<TranquilizerBehavior>();

        _DrowsyVFX = VFX.RegisterVFXObject("DrowsyParticle", ResMap.Get("drowsy_cloud"),
            fps: 6, loops: true, anchor: Anchor.MiddleCenter, scale: 0.5f);
    }

}

public class TranquilizerBehavior : MonoBehaviour
{
    private const int _STUN_DELAY = 10;
    private const int _STUN_TIME  = 3600; // one hour

    private void Start()
    {
        base.GetComponent<Projectile>().OnHitEnemy += (Projectile _, SpeculativeRigidbody enemy, bool _) => {
            if ((enemy.aiActor?.IsHostileAndNotABoss() ?? false) && !(enemy.behaviorSpeculator?.ImmuneToStun ?? true))
                enemy.aiActor.gameObject.GetOrAddComponent<EnemyTranquilizedBehavior>();
        };
    }

    private class EnemyTranquilizedBehavior : MonoBehaviour
    {
        private AIActor _enemy = null;
        private OrbitalEffect _orb = null;

        private void Start()
        {
            this._enemy = base.GetComponent<AIActor>();
            if ((this._enemy?.healthHaver?.currentHealth ?? 0) <= 0)
                return;

            this._orb = this._enemy.gameObject.AddComponent<OrbitalEffect>();
                this._orb.SetupOrbitals(vfx: Tranquilizer._DrowsyVFX, numOrbitals: 1, rps: 0.2f, isEmissive: false, isOverhead: true);

            AkSoundEngine.PostEvent("drowsy_sound", this._enemy.gameObject);
            Invoke("Permastun", Mathf.Max(1, Mathf.CeilToInt(Mathf.Log(this._enemy.healthHaver.currentHealth) / Mathf.Log(2))));
        }

        private void Permastun()
        {
            this._enemy.behaviorSpeculator?.Stun(_STUN_TIME, createVFX: false);
            this._enemy.IgnoreForRoomClear         = true;
            this._enemy.CollisionDamage            = 0f;
            this._enemy.CollisionKnockbackStrength = 0f;

            this._orb.AddOrbital(vfx: Tranquilizer._DrowsyVFX);
            this._orb.AddOrbital(vfx: Tranquilizer._DrowsyVFX);
        }
    }
}
