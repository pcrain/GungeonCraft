namespace CwaffingTheGungy;

public class HolyWaterGun : AdvancedGunBehavior
{
    public static string ItemName         = "Holy Water Gun";
    public static string SpriteName       = "holy_water_gun";
    public static string ProjectileName   = "10"; // mega douser
    public static string ShortDescription = "Water, Gun, & Holy Soak";
    public static string LongDescription  = "Deals quadruple damage to the Jammed. Killing a Jammed enemy reduces curse by 1.";
    public static string Lore             = "Rumored to have been used in exorcisms by the High Priest back while he was still the Low Priest. While the exact composition of the holy water is unknown, scientists have been able to reasonably ascertain the fluid contains koi pond water, primer, rat saliva, and moonshine. In any case, it has proven extremely effective at exorcizing the Jammed and nauseating everyone else.";

    internal const float _JAMMED_DAMAGE_MULT = 4f;

    internal static Dictionary<string, Texture2D> _GhostTextures = new();
    internal static GameObject _ExorcismParticleVFX = null;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<HolyWaterGun>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.BEAM, reloadTime: 1.0f, ammo: 500, audioFrom: Items.MegaDouser, defaultAudio: true);
            gun.AddToSubShop(ItemBuilder.ShopType.Cursula);
            gun.AddToSubShop(ItemBuilder.ShopType.Goopton);

        Projectile projectile = gun.SetupSingularProjectile(clipSize: -1, shootStyle: ShootStyle.Beam, ammoType: GameUIAmmoType.AmmoType.BEAM, damage: 0.0f,
          speed: 50.0f, force: 50.0f).Attach<ExorcismJuice>();

        BasicBeamController beamComp = projectile.SetupBeamSprites(
          spriteName: "holy_water_gun", fps: 20, dims: new Vector2(15, 15), impactDims: new Vector2(7, 7));
            beamComp.sprite.usesOverrideMaterial = true;
            beamComp.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTiltedCutoutEmissive");
            beamComp.sprite.renderer.material.SetFloat("_EmissivePower", 15f);
            // fix some animation glitches (don't blindly copy paste; need to be set on a case by case basis depending on your beam's needs)
            beamComp.muzzleAnimation = beamComp.beamStartAnimation;  //use start animation for muzzle animation, make start animation null
            beamComp.beamStartAnimation = null;

        _ExorcismParticleVFX = VFX.RegisterVFXObject("exorcism_particles", fps: 12, loops: false, anchor: Anchor.MiddleCenter, emissivePower: 2);
    }

    protected override void OnPickup(GameActor owner)
    {
        base.OnPickup(owner);

        foreach (AIActor enemy in StaticReferenceManager.AllEnemies)
            OnEnemySpawn(enemy);
        ETGMod.AIActor.OnPreStart += this.OnEnemySpawn;
    }

    private void OnEnemySpawn(AIActor enemy)
    {
        enemy.gameObject.GetOrAddComponent<Exorcisable>();  // add a dummy component for exorcism checks below
    }
}

public class ExorcismJuice : MonoBehaviour {} // dummy component

public class Exorcisable : MonoBehaviour
{
    private const float _EXORCISM_DPS   = 15.0f; // damage per second
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

        // Can't quite get this to work, just uses a black sprite :\
        // foreach (tk2dBaseSprite sprite in this._enemy.healthHaver.bodySprites)
        // {
        //     if (!sprite)
        //         continue;
        //     sprite.usesOverrideMaterial = true;
        //     sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitCutoutUber");
        //     sprite.renderer.material.SetFloat("_CircleAmount", 1f);
        //     sprite.gameObject.GetOrAddComponent<Encircler>();
        // }

        float epower = _EXORCISM_POWER * (this._enemy.IsBlackPhantom ? HolyWaterGun._JAMMED_DAMAGE_MULT : 1f);
        if (this._enemy.IsBlackPhantom && epower >= this._enemy.healthHaver.currentHealth)
        {
            PlayerController pc = beam.projectile.Owner as PlayerController;
            pc.ownerlessStatModifiers.Add(new StatModifier() {
                amount      = -1f,
                modifyType  = StatModifier.ModifyMethod.ADDITIVE,
                statToBoost = PlayerStats.StatType.Curse,
                });
            pc.stats.RecalculateStats(pc);

            Texture2D ghostSprite;
            if (HolyWaterGun._GhostTextures.ContainsKey(this._enemy.EnemyGuid))
                ghostSprite = HolyWaterGun._GhostTextures[this._enemy.EnemyGuid]; // If we've already computed a texture for this enemy, don't do it again
            else
            {
                ghostSprite = Lazy.GetTexturedEnemyIdleAnimation(this._enemy, new Color(1f,1f,1f,1f), 0.3f);
                HolyWaterGun._GhostTextures[this._enemy.EnemyGuid] = ghostSprite; // Cache the texture for this enemy for later
            }
            Vector3 pos                         = this._enemy.sprite.WorldCenter.ToVector3ZisY(-10f);
            GameObject g                        = UnityEngine.Object.Instantiate(new GameObject(), pos, Quaternion.identity);
            tk2dSpriteCollectionData collection = SpriteBuilder.ConstructCollection(g, "ghostcollection");
            int spriteId                        = SpriteBuilder.AddSpriteToCollection(ghostSprite, collection, "ghostsprite");
            tk2dBaseSprite sprite               = g.AddComponent<tk2dSprite>();
                sprite.SetSprite(collection, spriteId);
                sprite.PlaceAtPositionByAnchor(pos, Anchor.MiddleCenter);
            g.AddComponent<GhostlyDeath>().Setup(beam.Direction);
        }
        this._enemy.healthHaver.ApplyDamage(
            epower, beam.Direction, "Exorcism", CoreDamageTypes.Water, DamageCategory.Unstoppable, true, null, true);

        // Create particles
        if (UnityEngine.Random.Range(0f, 1f) < 0.25f)
        {
            Vector2 ppos = this._enemy.sprite.WorldCenter;
            float angle = Lazy.RandomAngle();
            Vector2 finalpos = ppos + BraveMathCollege.DegreesToVector(angle, magnitude: 1f);
            FancyVFX.Spawn(HolyWaterGun._ExorcismParticleVFX, finalpos.ToVector3ZisY(-1f), Lazy.RandomEulerZ(),
                velocity: Lazy.RandomVector(0.5f), lifetime: 0.34f, fadeOutTime: 0.34f, parent: this._enemy.sprite.transform);
        }

        // Play exorcism noises
        Lazy.PlaySoundUntilDeathOrTimeout(soundName: "exorcism_noises_intensify", source: this._enemy.gameObject, timer: 0.2f);
    }
}

public class GhostlyDeath : MonoBehaviour
{
    private const float _FADE_TIME   = 2.5f;
    private const float _DRIFT_SPEED = 0.15f / C.PIXELS_PER_TILE;

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
        AkSoundEngine.PostEvent("ghost_soul_sound", base.gameObject);
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
