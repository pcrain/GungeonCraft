namespace CwaffingTheGungy;

/* TODO:
    - make enemy projectiles flash and disappear with enemy
    - add nicer particles and sounds for disappearance
*/

public class SchrodingersGat : AdvancedGunBehavior
{
    public static string ItemName         = "Schrodinger's Gat";
    public static string SpriteName       = "schrodingers_gat";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Proba-ballistic";
    public static string LongDescription  = "Fires bullets that leave enemies in a quantum state until they are observed by either dealing or receiving damage. Once observed, enemies have a 50% chance of already being dead, revealing themselves and their projectiles as illusions. Bullets from this gun cannot affect the same enemy twice.";
    public static string Lore             = "Famously used by a mad scientist who would often fire dozens of rounds into a locked box with an animal inside, claiming it was both alive and dead until the box was opened. That scientist eventually landed in prison on charges for kidnapping and murdering dozens of pet cats, insisting \"we don't know if I kidnapped and murdered dozens of cats until we observe it!\" throughout the entire court procedings.";

    private float _speedMult                      = 1.0f;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<SchrodingersGat>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.A, gunClass: GunClass.FULLAUTO, reloadTime: 0.0f, ammo: 250);
            gun.SetAnimationFPS(gun.idleAnimation, 24);
            gun.SetAnimationFPS(gun.shootAnimation, 24);

        gun.DefaultModule.SetAttributes(clipSize: -1, cooldown: 0.125f, angleVariance: 15.0f, shootStyle: ShootStyle.Automatic, customClip: SpriteName);

        Projectile projectile = gun.InitFirstProjectile(damage: 0.0f, speed: 32.0f);
            projectile.AddDefaultAnimation(AnimatedBullet.Create(name: "schrodingers_gat_projectile", fps: 12, anchor: Anchor.MiddleCenter));

        projectile.gameObject.AddComponent<SchrodingersGatProjectile>();
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        AkSoundEngine.PostEvent("schrodingers_gat_fire_sound", gun.gameObject);
    }

    protected override void OnPickup(GameActor owner)
    {
        if (!this.everPickedUpByPlayer)
            StaticReferenceManager.ProjectileAdded += CheckFromQuantumEnemyOwner;
        base.OnPickup(owner);
    }

    internal static void CheckFromQuantumEnemyOwner(Projectile p)
    {
        if (!p || p.Owner is not AIActor enemy)
            return;
        if (!(enemy.gameObject.GetComponent<SchrodingersStat>()?.IsActuallyDead() ?? false))
            return;
        SchrodingersEnemyProjectile sp = p.gameObject.GetOrAddComponent<SchrodingersEnemyProjectile>();
    }
}

public class SchrodingersEnemyProjectile : MonoBehaviour
{
    private Projectile _projectile;
    private AIActor _enemy;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._enemy = this._projectile.Owner as AIActor;
        this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
        // this._projectile.ChangeColor(0.1f, Color.black);
    }

    private void OnPreCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, SpeculativeRigidbody other, PixelCollider otherPixelCollider)
    {
        if (other?.gameActor is not PlayerController pc)
            return;
        PhysicsEngine.SkipCollision = true;
        this._enemy.healthHaver.ApplyDamage(9999f, Vector2.zero, "Quantum", CoreDamageTypes.Void, DamageCategory.Unstoppable, true, null, true);
        this._projectile?.DieInAir();
    }
}

public class SchrodingersGatProjectile : MonoBehaviour
{
    private void Start()
    {
        base.GetComponent<Projectile>().specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
        base.GetComponent<Projectile>().OnHitEnemy += (Projectile p, SpeculativeRigidbody enemy, bool _) => {
            if (enemy.GetComponent<AIActor>()?.IsHostileAndNotABoss() ?? false)
                enemy.aiActor.gameObject.GetOrAddComponent<SchrodingersStat>();
        };
    }

    private void OnPreCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, SpeculativeRigidbody other, PixelCollider otherPixelCollider)
    {
        if (other.gameActor is not AIActor enemy)
            return;
        if (enemy.gameObject.GetComponent<SchrodingersStat>())
            PhysicsEngine.SkipCollision = true; // can't apply Schrodinger's Stat to an enemy who already has it
    }
}

public class SchrodingersStat : MonoBehaviour
{
    private const float _FLICKER_SPEED = 0.05f;

    private bool _actuallyDead;
    private bool _observed;
    private bool _doneUpdating;
    private bool _enemyVisible;
    private AIActor _enemy;
    private Renderer _renderer;
    private float _flickerTimer;
    private Coroutine _flickerCR;

    private void Start()
    {
        this._enemy                        = base.GetComponent<AIActor>();
        this._observed                     = false;
        this._doneUpdating                 = false;
        this._enemyVisible                 = true;
        this._actuallyDead                 = Lazy.CoinFlip();
        this._enemy.healthHaver.OnDamaged += this.OnDamaged;
        this._renderer                     = this._enemy.GetComponent<Renderer>();
        this._flickerTimer                 = 0.0f;
        this._flickerCR                    = StartCoroutine(Quantum());

        if (this._actuallyDead)
        {
            this._enemy.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
            foreach (Projectile p in StaticReferenceManager.AllProjectiles)
                SchrodingersGat.CheckFromQuantumEnemyOwner(p);
        }

        AkSoundEngine.PostEvent("schrodinger_bullet_hit", base.gameObject);
    }

    private void OnPreCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, SpeculativeRigidbody other, PixelCollider otherPixelCollider)
    {
        if (other?.gameActor is not PlayerController pc)
            return;
        PhysicsEngine.SkipCollision = true;
        this._enemy.healthHaver.ApplyDamage(9999f, Vector2.zero, "Quantum", CoreDamageTypes.Void, DamageCategory.Unstoppable, true, null, true);
    }

    public bool IsActuallyDead()
    {
        return this._actuallyDead;
    }

    private IEnumerator Quantum()
    {
        while (!this._observed)
        {
            yield return null;
            this._flickerTimer += BraveTime.DeltaTime;
            if (this._flickerTimer < _FLICKER_SPEED)
                continue;

            this._flickerTimer -= _FLICKER_SPEED;
            this._enemyVisible     = !this._enemyVisible;
            if (this._enemyVisible)
                this._enemy.RegisterOverrideColor(Color.black, SchrodingersGat.ItemName);
            else
                this._enemy.DeregisterOverrideColor(SchrodingersGat.ItemName);

            if (UnityEngine.Random.value < 0.1f)  // create an afterimage
            {
                FancyVFX fv = FancyVFX.FromCurrentFrame(this._enemy.sprite);
                    fv.Setup(Vector2.zero, 0.5f, 0.5f);
                    fv.StartCoroutine(PhaseOut(fv, Lazy.RandomVector(), 5f, 90f));
            }
        }
    }

    private void LateUpdate()
    {
        if (this._doneUpdating || !this._observed)
            return;

        this._enemy.DeregisterOverrideColor(SchrodingersGat.ItemName);
        this._renderer.enabled = true;
        this._enemy.sprite.usesOverrideMaterial = false;
        StopCoroutine(this._flickerCR);
        this._doneUpdating = true;
    }

    private void OnDamaged(float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection)
    {
        if (!this._enemy)
            return;

        this._observed = true;
        this._enemy.healthHaver.OnDamaged -= this.OnDamaged;
        if (!this._actuallyDead)
            return;

        this._enemy.DeregisterOverrideColor(SchrodingersGat.ItemName);
        for (int i = StaticReferenceManager.AllProjectiles.Count - 1; i >=0; --i)
        {
            Projectile p = StaticReferenceManager.AllProjectiles[i];
            if (!p || !p.sprite || p.Owner != this._enemy)
                continue;
            FancyVFX fv = FancyVFX.FromCurrentFrame(p.sprite);
                fv.Setup(Vector2.zero, 1.0f, 1.0f);
                fv.StartCoroutine(PhaseOut(fv, Vector2.right, 25f, 90f));
            p.DieInAir();
        }

        FancyVFX fe = FancyVFX.FromCurrentFrame(this._enemy.sprite);
            fe.Setup(Vector2.zero, 1.0f, 1.0f);
            fe.StartCoroutine(PhaseOut(fe, Vector2.right, 25f, 90f));

        AkSoundEngine.PostEvent("schrodinger_dead_sound", base.gameObject);
        this._enemy.EraseFromExistenceWithRewards(false);
    }

    private static IEnumerator PhaseOut(FancyVFX fe, Vector2 direction, float amplitude, float frequency)
    {
        fe.GetComponent<tk2dSprite>().color = Color.black;
        float phase = 0f;
        while (fe.gameObject)
        {
            phase += frequency * BraveTime.DeltaTime;
            fe.transform.position += ((amplitude * Mathf.Sin(phase) * BraveTime.DeltaTime) * direction).ToVector3ZUp(0f);
            yield return null;
        }
        yield break;
    }
}
