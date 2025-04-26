namespace CwaffingTheGungy;

/* TODO:
    - make enemy projectiles flash and disappear with enemy
    - add nicer particles and sounds for disappearance
    - use new owner detection pioneered by alien nail gun for quantum detection
*/

public class SchrodingersGat : CwaffGun
{
    public static string ItemName         = "Schrodinger's Gat";
    public static string ShortDescription = "Proba-ballistic";
    public static string LongDescription  = "Fires bullets that leave enemies in a quantum state until they are observed by either dealing or receiving damage. Once observed, enemies have a 50% chance of already being dead, revealing themselves and their projectiles as illusions. Bullets from this gun cannot affect the same enemy twice.";
    public static string Lore             = "Famously used by a mad scientist who would often fire dozens of rounds into a locked box with an animal inside, claiming it was both alive and dead until the box was opened. That scientist eventually landed in prison on charges for kidnapping and murdering dozens of pet cats, insisting \"we don't know if I kidnapped and murdered dozens of cats until we observe it!\" throughout the entire court proceedings.";

    private float _speedMult                      = 1.0f;

    public static void Init()
    {
        Lazy.SetupGun<SchrodingersGat>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.A, gunClass: GunClass.FULLAUTO, reloadTime: 0.0f, ammo: 250, idleFps: 24, shootFps: 24, banFromBlessedRuns: true)
          .InitProjectile(GunData.New(clipSize: -1, cooldown: 0.125f, angleVariance: 15.0f, shootStyle: ShootStyle.Automatic, customClip: true,
            damage: 0.0f, speed: 32.0f, sprite: "schrodingers_gat_projectile", fps: 12, anchor: Anchor.MiddleCenter, spawnSound: "schrodingers_gat_fire_sound"))
          .Attach<SchrodingersGatProjectile>();
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        StaticReferenceManager.ProjectileAdded -= CheckFromQuantumEnemyOwner;
        StaticReferenceManager.ProjectileAdded += CheckFromQuantumEnemyOwner;
        base.OnPlayerPickup(player);
    }

    internal static void CheckFromQuantumEnemyOwner(Projectile p)
    {
        if (!p || p.Owner is not AIActor enemy)
            return;
        if (!enemy || !enemy.gameObject || enemy.gameObject.GetComponent<SchrodingersStat>() is not SchrodingersStat ss)
            return;
        if (!ss.IsActuallyDead())
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
    }

    private void OnPreCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, SpeculativeRigidbody other, PixelCollider otherPixelCollider)
    {
        if (!other || other.gameActor is not PlayerController pc)
            return;
        PhysicsEngine.SkipCollision = true;
        this._enemy.healthHaver.ApplyDamage(9999f, Vector2.zero, "Quantum", CoreDamageTypes.Void, DamageCategory.Unstoppable, true, null, true);
        if (this._projectile)
            this._projectile.DieInAir();
    }
}

public class SchrodingersGatProjectile : MonoBehaviour
{
    private void Start()
    {
        base.GetComponent<Projectile>().specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
        base.GetComponent<Projectile>().OnHitEnemy += this.OnHitEnemy;
    }

    private void OnHitEnemy(Projectile p, SpeculativeRigidbody body, bool _)
    {
        if (body.GetComponent<AIActor>() is AIActor enemy && enemy.IsHostileAndNotABoss() && !enemy.gameObject.GetComponent<SchrodingersStat>())
        {
            SchrodingersStat ss = enemy.gameObject.AddComponent<SchrodingersStat>();
            if (p.Owner is PlayerController pc && pc.HasSynergy(Synergy.MASTERY_SCHRODINGERS_GAT))
                ss.Observe();
        }
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

    private bool _observed = false;
    private bool _actuallyDead;
    private bool _doneUpdating;
    private bool _enemyVisible;
    private AIActor _enemy;
    private Renderer _renderer;
    private float _flickerTimer;
    private Coroutine _flickerCR;

    private void Start()
    {
        this._enemy                        = base.GetComponent<AIActor>();
        this._doneUpdating                 = false;
        this._enemyVisible                 = true;
        this._actuallyDead                 = Lazy.CoinFlip();
        this._renderer                     = this._enemy.GetComponent<Renderer>();
        this._flickerTimer                 = 0.0f;

        if (this._observed)
        {
            Observe();
            base.gameObject.Play("schrodinger_bullet_hit");
            return;
        }

        if (this._actuallyDead)
        {
            this._enemy.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
            foreach (Projectile p in StaticReferenceManager.AllProjectiles)
                SchrodingersGat.CheckFromQuantumEnemyOwner(p);
        }

        this._enemy.healthHaver.OnDamaged += this.OnDamaged;
        this._flickerCR = StartCoroutine(Quantum());
        base.gameObject.Play("schrodinger_bullet_hit");
    }

    public void Observe()
    {
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
            tk2dBaseSprite dupe = p.sprite.DuplicateInWorld();
            dupe.StartCoroutine(PhaseOut(dupe, Vector2.right, 25f, 90f, 1.0f));
            p.DieInAir();
        }

        tk2dBaseSprite edupe = this._enemy.DuplicateInWorld();
        edupe.StartCoroutine(PhaseOut(edupe, Vector2.right, 25f, 90f, 1.0f));

        base.gameObject.Play("schrodinger_dead_sound");
        this._enemy.EraseFromExistenceWithRewards(false);
    }

    private void OnPreCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, SpeculativeRigidbody other, PixelCollider otherPixelCollider)
    {
        if (!other || other.gameActor is not PlayerController pc)
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
                tk2dBaseSprite dupe = this._enemy.DuplicateInWorld();
                dupe.StartCoroutine(PhaseOut(dupe, Lazy.RandomVector(), 5f, 90f, 0.5f));
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
        if (this._enemy)
            Observe();
    }

    internal static IEnumerator PhaseOut(tk2dBaseSprite sprite, Vector2 direction, float amplitude, float frequency, float lifetime)
    {
        sprite.color = Color.black;
        for (float elapsed = 0f; elapsed < lifetime; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / lifetime;
            sprite.renderer.SetAlpha(1f - percentDone);
            sprite.transform.position += ((amplitude * Mathf.Sin(frequency * elapsed) * BraveTime.DeltaTime) * direction).ToVector3ZUp(0f);
            yield return null;
        }
        UnityEngine.Object.Destroy(sprite.gameObject);
        yield break;
    }
}
