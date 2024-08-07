namespace CwaffingTheGungy;

public class HolyWaterGun : CwaffGun
{
    public static string ItemName         = "Holy Water Gun";
    public static string ShortDescription = "Water, Gun, & Holy Soak";
    public static string LongDescription  = "Deals quadruple damage to the Jammed. Killing a Jammed enemy reduces curse by 0.5.";
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
          .InitProjectile(GunData.New(baseProjectile: Items.MegaDouser.Projectile(), clipSize: -1, shootStyle: ShootStyle.Beam, jammedDamageMult: _JAMMED_DAMAGE_MULT,
            ammoType: GameUIAmmoType.AmmoType.BEAM, damage: Exorcisable._EXORCISM_DPS, speed: 50.0f, force: 15.0f, beamSprite: "holy_water_gun",
            beamFps: 20, beamDims: new Vector2(15, 15), beamImpactDims: new Vector2(7, 7), beamEmission: 15f, beamInterpolate: false, beamStartIsMuzzle: true))
          .Attach<ExorcismJuice>();

        _ExorcismParticleVFX = VFX.Create("exorcism_particles", fps: 12, loops: false, anchor: Anchor.MiddleCenter, emissivePower: 2);
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

        pc.ownerlessStatModifiers.Add(new StatModifier() {
            amount      = -0.5f,
            modifyType  = StatModifier.ModifyMethod.ADDITIVE,
            statToBoost = PlayerStats.StatType.Curse,
            });
        pc.stats.RecalculateStats(pc);

        Texture2D ghostSprite;
        if (HolyWaterGun._GhostTextures.ContainsKey(enemy.EnemyGuid)) //TODO: why am i not just setting the alpha here???
            ghostSprite = HolyWaterGun._GhostTextures[enemy.EnemyGuid]; // If we've already computed a texture for this enemy, don't do it again
        else
        {
            ghostSprite = Lazy.GetTexturedEnemyIdleAnimation(enemy, new Color(1f,1f,1f,1f), 0.3f);
            HolyWaterGun._GhostTextures[enemy.EnemyGuid] = ghostSprite; // Cache the texture for this enemy for later
        }
        Vector3 pos                         = enemy.CenterPosition.ToVector3ZisY(-10f);
        GameObject g                        = UnityEngine.Object.Instantiate(new GameObject(), pos, Quaternion.identity);
        tk2dSpriteCollectionData collection = SpriteBuilder.ConstructCollection(g, "ghostcollection");
        int spriteId                        = SpriteBuilder.AddSpriteToCollection(ghostSprite, collection, "ghostsprite");  //NOTE: this doesn't use PackerHelper since it's done at runtime
        tk2dBaseSprite sprite               = g.AddComponent<tk2dSprite>();
            sprite.SetSprite(collection, spriteId);
            sprite.FlipX = enemy.sprite.FlipX;
            sprite.FlipY = enemy.sprite.FlipY;
            sprite.transform.localScale = enemy.sprite.transform.localScale;
            sprite.transform.rotation = enemy.sprite.transform.rotation;
            sprite.PlaceAtRotatedPositionByAnchor(pos, Anchor.MiddleCenter);
        g.AddComponent<GhostlyDeath>().Setup(beam.Direction);

        if (this._mastered)
        {
            pc.gameObject.Play("holy_sound");
            if (DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.HolyGoop) is DeadlyDeadlyGoopManager holyGooper)
                holyGooper.AddGoopCircle(pos, _HOLY_GOOP_RADIUS);
        }
    }
}

public class Exorcisable : MonoBehaviour
{
    internal const float _EXORCISM_DPS   = 15.0f; // damage per second
    private const float _EXORCISM_POWER = _EXORCISM_DPS / C.FPS; // damage per frame

    private static uint ExorcismSoundId = 0;
    private static float ExorcismTimer = 0.0f;

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
        if (UnityEngine.Random.Range(0f, 1f) < 0.25f)
        {
            Vector2 ppos = this._enemy.CenterPosition;
            float angle = Lazy.RandomAngle();
            Vector2 finalpos = ppos + BraveMathCollege.DegreesToVector(angle, magnitude: 1f);
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
        if (this._velocity.x < 0)
            this._sprite.transform.localScale = new Vector3(-1f, 1f, 1f);
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
        this._sprite.renderer.SetAlpha(1f - (this._lifetime / _FADE_TIME));
    }
}

public class GameActorHolyGoopEffect : GameActorSpeedEffect
{
    private static StatModifier[] _CaffeineGoopBuffs = null;

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
