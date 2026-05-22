namespace CwaffingTheGungy;

public class HolyWaterGun : CwaffGun
{
    public static string ItemName         = "Holy Water Gun";
    public static string ShortDescription = "Water, Gun, & Holy Soak";
    public static string LongDescription  = "Deals 8x damage to the Jammed. Killing a Jammed enemy reduces curse by 0.5.";
    public static string Lore             = "Rumored to have been used in exorcisms by the High Priest back while he was still the Low Priest. While the exact composition of the holy water is unknown, scientists have been able to reasonably ascertain the fluid contains koi pond water, primer, rat saliva, and moonshine. In any case, it has proven extremely effective at exorcising the Jammed and nauseating everyone else.";

    internal const float _JAMMED_DAMAGE_MULT = 4f;
    internal const float _MASTERY_JAMMED_DAMAGE_MULT = 16f;

    internal static Dictionary<string, Texture2D> _GhostTextures = new();
    internal static GameObject _ExorcismParticleVFX = null;

    public static void Init()
    {
        Lazy.SetupGun<HolyWaterGun>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.BEAM, reloadTime: 1.0f, ammo: 100, audioFrom: Items.MegaDouser, defaultAudio: true)
          .AddToShop(ItemBuilder.ShopType.Cursula)
          .AddToShop(ItemBuilder.ShopType.Goopton)
          .InitProjectile(GunData.New(baseProjectile: Items.MegaDouser.Projectile(), clipSize: -1, shootStyle: ShootStyle.Beam,
            customClip: true, damage: Exorcisable._EXORCISM_DPS, speed: 50.0f, force: 15.0f, beamSprite: "holy_water_gun",
            beamFps: 20, beamEmission: 15f, beamInterpolate: false, beamStartIsMuzzle: true))
          .Attach<ExorcismJuice>();

        _ExorcismParticleVFX = VFX.Create("exorcism_particles", fps: 12, loops: false, emissivePower: 2);
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);

        foreach (AIActor enemy in StaticReferenceManager.AllEnemies)
            OnEnemySpawn(enemy);
        ETGMod.AIActor.OnPreStart -= OnEnemySpawn;
        ETGMod.AIActor.OnPreStart += OnEnemySpawn;
    }

    private static void OnEnemySpawn(AIActor enemy)
    {
        enemy.gameObject.GetOrAddComponent<Exorcisable>();  // add a dummy component for exorcism checks below
    }
}

public class ExorcismJuice : MonoBehaviour
{
    private const float _HOLY_GOOP_RADIUS = 5f;

    private Projectile _projectile;
    private PlayerController _owner;
    private bool _mastered = false;
    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;

        this._mastered = this._owner && this._owner.HasSynergy(Synergy.MASTERY_HOLY_WATER_GUN);
        if (this._mastered)
            this._projectile.BlackPhantomDamageMultiplier = HolyWaterGun._MASTERY_JAMMED_DAMAGE_MULT;
          else
            this._projectile.BlackPhantomDamageMultiplier = HolyWaterGun._JAMMED_DAMAGE_MULT;

        this._projectile.OnHitEnemy += ExorciseTheJammed;
    }

    private void ExorciseTheJammed(Projectile bullet, SpeculativeRigidbody body, bool willKill)
    {
        if (!willKill)
            return;
        if (!body || body.GetComponent<AIActor>() is not AIActor enemy)
            return;
        if (!enemy.IsBlackPhantom)
            return;
        if (bullet.Owner is not PlayerController pc)
            return;
        if (bullet.GetComponent<BasicBeamController>() is not BasicBeamController beam)
            return;

        pc.ownerlessStatModifiers.Add(StatType.Curse.Add(-0.5f));
        pc.stats.RecalculateStats(pc);

        tk2dBaseSprite sprite = enemy.sprite.DuplicateInWorld(enemy.optionalPalette);
        sprite.ApplyShader(CwaffShaders.DesatShader, enemy.optionalPalette);
        sprite.renderer.material.SetFloat(CwaffVFX._SaturationId, 0f);
        sprite.renderer.material.SetFloat(CwaffVFX._FadeId, 1f);
        sprite.gameObject.AddComponent<GhostlyDeath>().Setup(beam.Direction);

        if (!this._mastered)
            return;

        pc.gameObject.Play("holy_sound");
        if (DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.HolyGoop) is DeadlyDeadlyGoopManager holyGooper)
            holyGooper.AddGoopCircle(enemy.CenterPosition, _HOLY_GOOP_RADIUS);
    }
}

public class Exorcisable : MonoBehaviour
{
    internal const float _EXORCISM_DPS = 15.0f; // damage per second
    private const float _EXORCISM_POWER = _EXORCISM_DPS / C.FPS; // damage per frame

    private AIActor _enemy;
    private void Start()
    {
        this._enemy = base.GetComponent<AIActor>();
        this._enemy.specRigidbody.OnBeamCollision += this.CheckForHolyWater;
    }

    private void CheckForHolyWater(BeamController beam)
    {
        if (!this._enemy.IsHostileAndNotABoss())
            return;
        if (beam.GetComponent<ExorcismJuice>() is not ExorcismJuice exorcism)
            return;

        // Create particles
        if (UnityEngine.Random.value < 0.25f)
        {
            Vector2 finalpos = this._enemy.CenterPosition + BraveMathCollege.DegreesToVector(Lazy.RandomAngle(), magnitude: 1f);
            CwaffVFX.Spawn(HolyWaterGun._ExorcismParticleVFX, finalpos.ToVector3ZisY(-1f), Lazy.RandomEulerZ(),
                velocity: Lazy.RandomVector(0.5f), lifetime: 0.34f, fadeOutTime: 0.34f);
        }

        // Play exorcism noises
        Lazy.PlaySoundUntilDeathOrTimeout(soundName: "exorcism_noises_intensify", source: this._enemy.gameObject, timer: 0.2f);
    }
}

public class GhostlyDeath : MonoBehaviour
{
    private const float _FADE_TIME   = 2.5f;
    private const float _DRIFT_SPEED = 0.25f * C.PIXEL_SIZE;

    private float _lifetime;
    private tk2dSprite _sprite;
    private Vector3 _velocity;

    public void Setup(Vector2 direction)
    {
        Vector2 normdir = direction.normalized;
        this._velocity = _DRIFT_SPEED * (new Vector3(normdir.x, normdir.y, 0f));
        this._sprite = base.gameObject.GetComponent<tk2dSprite>();
        this._lifetime = 0.0f;
    }

    private void Start()
    {
        base.gameObject.Play("ghost_soul_sound");
    }

    private void Update()
    {
        this._lifetime += BraveTime.DeltaTime;
        if (this._lifetime >= _FADE_TIME)
        {
            UnityEngine.Object.Destroy(this.gameObject);
            return;
        }
        this._sprite.transform.position += this._velocity;
        this._sprite.renderer.material.SetFloat(CwaffVFX._FadeId, 1f - (this._lifetime / _FADE_TIME));
    }
}

public class GameActorHolyGoopEffect : GameActorSpeedEffect
{
    public override void OnEffectApplied(GameActor actor, RuntimeGameActorEffectData effectData, float partialAmount = 1f)
    {
        base.OnEffectApplied(actor, effectData, partialAmount);
        if (actor is not PlayerController player)
            return;
        player.InfiniteAmmo.AddOverride("Holy Goop");
        Material[] array = player.SetOverrideShader(ShaderCache.Acquire("Brave/Internal/RainbowChestShader"));
        for (int i = 0; i < array.Length; i++)
            if (array[i] != null)
                array[i].SetFloat("_AllColorsToggle", 1f);
        player.healthHaver.IsVulnerable = false;
    }

    public override void OnEffectRemoved(GameActor actor, RuntimeGameActorEffectData effectData)
    {
        base.OnEffectRemoved(actor, effectData);
        if (actor is not PlayerController player)
            return;
        player.InfiniteAmmo.RemoveOverride("Holy Goop");
        player.ClearOverrideShader();
        player.healthHaver.IsVulnerable = true;
    }
}
