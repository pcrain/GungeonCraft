namespace CwaffingTheGungy;

public class Tranquilizer : AdvancedGunBehavior
{
    public static string ItemName         = "Tranquilizer";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Zzzzzz";
    public static string LongDescription  = "Fires darts that permastun enemies after a few seconds, scaling logarithmically with their current health. Each subsequent dart decreases an enemy's tranquilization timer by 3 seconds.";
    public static string Lore             = "Most commonly used for sedating loudly-opinionated supermarket shoppers and other similarly aggressive wild animals, the tranquilizer gun is the pinnacle of non-lethal firearm technology. What it lacks in visual spectacle or firepower it more than makes up for with raw practicality, able to completely pacify all but the mightiest of the Gungeon's denizens with a single shot and a few seconds of your time. As long as you have a plan in place for not getting shot for those few precious seconds, it's hard to beat in terms of ammo-efficiency for dispatching the Gundead.";

    internal static GameObject _DrowsyVFX      = null;
    internal static GameObject _SleepyVFX      = null;
    internal static GameObject _TranqImpactVFX = null;
    internal static GameObject _SleepImpactVFX = null;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Tranquilizer>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.D, gunClass: GunClass.POISON, reloadTime: 1.2f, ammo: 80);
            gun.SetAnimationFPS(gun.shootAnimation, 30);
            gun.SetAnimationFPS(gun.reloadAnimation, 40);
            gun.SetMuzzleVFX(Items.Mailbox); // innocuous muzzle flash effects
            gun.SetFireAudio("blowgun_fire_sound");
            gun.SetReloadAudio("blowgun_reload_sound");

        gun.InitProjectile(new(clipSize: 1, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic, customClip: true, damage: 0f,
          sprite: "tranquilizer_projectile", fps: 12, anchor: Anchor.MiddleLeft)).Attach<TranquilizerBehavior>();

        _DrowsyVFX      = VFX.Create("drowsy_cloud", fps: 6, loops: true, anchor: Anchor.MiddleCenter, scale: 0.5f);
        _SleepyVFX      = VFX.Create("sheep_vfx", fps: 6, loops: true, anchor: Anchor.MiddleCenter, scale: 0.75f);
        _TranqImpactVFX = VFX.Create("tranquilizer_impact", fps: 2, loops: true, anchor: Anchor.MiddleCenter);
        _SleepImpactVFX = VFX.Create("sleep_impact_vfx", fps: 6, loops: true, anchor: Anchor.MiddleCenter);
    }

    private void LateUpdate()
    {
        if (this.gun.spriteAnimator.currentClip.name == this.gun.shootAnimation)
            this.gun.RenderInFrontOfPlayer();
    }
}

public class TranquilizerBehavior : MonoBehaviour
{
    private const int _STUN_TIME = 3600; // one hour

    private void Start()
    {
        Projectile proj = base.GetComponent<Projectile>();
        proj.OnHitEnemy += this.OnHitEnemy;
        proj.OnDestruction += this.OnDestruction;
    }

    private void OnHitEnemy(Projectile p, SpeculativeRigidbody enemy, bool killed)
    {
        if (!(enemy.aiActor?.IsHostileAndNotABoss() ?? false) || (enemy.behaviorSpeculator?.ImmuneToStun ?? true))
            return;
        if (enemy.aiActor.gameObject.GetComponent<EnemyTranquilizedBehavior>() is EnemyTranquilizedBehavior tranq)
            tranq.timeUntilStun -= 3f;
        else
            enemy.aiActor.gameObject.AddComponent<EnemyTranquilizedBehavior>();
    }

    public void OnDestruction(Projectile p)
    {
        FancyVFX.SpawnBurst(Tranquilizer._TranqImpactVFX, 4, p.sprite.WorldCenter, baseVelocity: Vector2.zero,
            velocityVariance: 2f, velType: FancyVFX.Vel.AwayRadial, uniform: true, lifetime: 0.5f, fadeOutTime: 0.5f);
    }

    private class EnemyTranquilizedBehavior : MonoBehaviour
    {
        private AIActor _enemy = null;
        private OrbitalEffect _orb = null;
        private bool _stunned = false;

        public float timeUntilStun = 9999f;

        private void Start()
        {
            this._enemy = base.GetComponent<AIActor>();
            if ((this._enemy?.healthHaver?.currentHealth ?? 0) <= 0)
                return;

            this._orb = this._enemy.gameObject.AddComponent<OrbitalEffect>();
            this._orb.SetupOrbitals(vfx: Tranquilizer._DrowsyVFX, numOrbitals: 1, rps: 0.2f, isEmissive: false, isOverhead: true, rotates: true, flips: false);

            this._enemy.gameObject.Play("drowsy_sound");
            this.timeUntilStun = Mathf.Max(1, Mathf.CeilToInt(Mathf.Log(this._enemy.healthHaver.currentHealth) / Mathf.Log(2)));
        }

        private void Update()
        {
            if (this._stunned)
                return;
            if ((this.timeUntilStun -= BraveTime.DeltaTime) <= 0.0f)
                Permastun();
        }

        private void Permastun()
        {
            this._stunned = true;
            this._enemy.behaviorSpeculator?.Stun(_STUN_TIME, createVFX: false);
            this._enemy.IgnoreForRoomClear         = true;
            this._enemy.CollisionDamage            = 0f;
            this._enemy.CollisionKnockbackStrength = 0f;

            FancyVFX.Spawn(Tranquilizer._SleepImpactVFX, this._enemy.CenterPosition, Quaternion.identity,
                lifetime: 0.5f, fadeOutTime: 1.0f, parent: this._enemy.transform, startScale: 0.25f, endScale: 2f, height: 10f);
            this._enemy.gameObject.Play("fall_asleep_sound");
            this._orb.ClearOrbitals();
            this._orb.SetupOrbitals(vfx: Tranquilizer._SleepyVFX, numOrbitals: 2, rps: 0.4f, isEmissive: false, isOverhead: true, rotates: false, flips: true, fades: true, bobAmount: 0.25f);
        }
    }
}
