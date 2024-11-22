namespace CwaffingTheGungy;

public class Tranquilizer : CwaffGun
{
    public static string ItemName         = "Tranquilizer";
    public static string ShortDescription = "Zzzzzz";
    public static string LongDescription  = "Fires darts that permastun enemies after a few seconds, scaling logarithmically with their current health. Each subsequent dart decreases an enemy's tranquilization timer by 3 seconds. Any enemy tranquilized while holding a gun has a 10% chance to drop their held gun and a 25% to drop a small amount of ammo.";
    public static string Lore             = "Most commonly used for sedating loudly-opinionated supermarket shoppers and other similarly aggressive wild animals, the tranquilizer gun is the pinnacle of non-lethal firearm technology. What it lacks in visual spectacle or firepower it more than makes up for with raw practicality, able to completely pacify all but the mightiest of the Gungeon's denizens with a single shot and a few seconds of your time. As long as you have a plan in place for not getting shot for those few precious seconds, it's hard to beat in terms of ammo-efficiency for dispatching the Gundead.";

    internal static GameObject _DrowsyVFX      = null;
    internal static GameObject _SleepyVFX      = null;
    internal static GameObject _TranqImpactVFX = null;
    internal static GameObject _SleepImpactVFX = null;

    public static void Init()
    {
        Lazy.SetupGun<Tranquilizer>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.D, gunClass: CwaffGunClass.UTILITY, reloadTime: 1.2f, ammo: 80, shootFps: 30, reloadFps: 40,
            muzzleFrom: Items.Mailbox, fireAudio: "blowgun_fire_sound", reloadAudio: "blowgun_reload_sound", banFromBlessedRuns: true)
          .InitProjectile(GunData.New(clipSize: 1, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic, customClip: true, damage: 0f,
            sprite: "tranquilizer_projectile", fps: 12, anchor: Anchor.MiddleLeft))
          .Attach<TranquilizerBehavior>();

        _DrowsyVFX      = VFX.Create("drowsy_cloud", fps: 6, scale: 0.5f);
        _SleepyVFX      = VFX.Create("sheep_vfx", fps: 6, scale: 0.75f);
        _TranqImpactVFX = VFX.Create("tranquilizer_impact", fps: 2);
        _SleepImpactVFX = VFX.Create("sleep_impact_vfx", fps: 6);
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
        if (!enemy.aiActor || !enemy.aiActor.IsHostileAndNotABoss() || !enemy.behaviorSpeculator || enemy.behaviorSpeculator.ImmuneToStun)
            return;
        if (enemy.aiActor.gameObject.GetComponent<EnemyTranquilizedBehavior>() is EnemyTranquilizedBehavior tranq)
            tranq.timeUntilStun -= 3f;
        else
            enemy.aiActor.gameObject.AddComponent<EnemyTranquilizedBehavior>();
    }

    public void OnDestruction(Projectile p)
    {
        CwaffVFX.SpawnBurst(Tranquilizer._TranqImpactVFX, 4, p.SafeCenter, baseVelocity: Vector2.zero,
            velocityVariance: 2f, velType: CwaffVFX.Vel.AwayRadial, uniform: true, lifetime: 0.5f, fadeOutTime: 0.5f);
    }

    private class EnemyTranquilizedBehavior : MonoBehaviour
    {
        private const float _DROP_GUN_CHANCE  = 0.1f;
        private const float _DROP_AMMO_CHANCE = 0.25f;

        private AIActor _enemy = null;
        private OrbitalEffect _orb = null;
        private bool _stunned = false;

        public float timeUntilStun = 9999f;

        private void Start()
        {
            this._enemy = base.GetComponent<AIActor>();
            if (!this._enemy || !this._enemy.healthHaver || this._enemy.healthHaver.currentHealth <= 0)
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
            if (this._enemy.behaviorSpeculator)
            {
                this._enemy.behaviorSpeculator.Stun(_STUN_TIME, createVFX: false);
                this._enemy.behaviorSpeculator.OverrideBehaviors.Clear();
                this._enemy.behaviorSpeculator.TargetBehaviors.Clear();
                this._enemy.behaviorSpeculator.MovementBehaviors.Clear();
                this._enemy.behaviorSpeculator.AttackBehaviors.Clear();
                this._enemy.behaviorSpeculator.OtherBehaviors.Clear();
                this._enemy.behaviorSpeculator.m_behaviors.Clear();
            }
            this._enemy.IgnoreForRoomClear         = true;
            this._enemy.CollisionDamage            = 0f;
            this._enemy.CollisionKnockbackStrength = 0f;

            CwaffVFX.Spawn(Tranquilizer._SleepImpactVFX, this._enemy.CenterPosition, Quaternion.identity,
                lifetime: 0.5f, fadeOutTime: 1.0f, startScale: 0.25f, endScale: 2f, height: 10f);
            this._enemy.gameObject.Play("fall_asleep_sound");
            this._orb.ClearOrbitals();
            this._orb.SetupOrbitals(vfx: Tranquilizer._SleepyVFX, numOrbitals: 2, rps: 0.4f, isEmissive: false, isOverhead: true, rotates: false, flips: true, fades: true, bobAmount: 0.25f);

            if (this._enemy.aiShooter is not AIShooter shooter)
                return;

            if (shooter.CurrentGun is Gun gun)
            {
                if (UnityEngine.Random.value <= _DROP_AMMO_CHANCE)
                    LootEngine.SpawnItem(ScavengingArms._SmallAmmoPickup, this._enemy.Position, Vector2.zero, 0f, false);
                if (UnityEngine.Random.value <= _DROP_GUN_CHANCE)
                {
                    shooter.ToggleHandRenderers(false, "tranquilized");
                    if (shooter.m_cachedBraveBulletSource != null)
                        shooter.m_cachedBraveBulletSource.enabled = false;
                    if (gun.DropGun().gameObject.GetComponentInChildren<Gun>() is Gun droppedGun)
                        droppedGun.CurrentAmmo = Mathf.CeilToInt(0.05f * droppedGun.GetBaseMaxAmmo());
                }
            }
            UnityEngine.Object.Destroy(shooter);
        }
    }
}
