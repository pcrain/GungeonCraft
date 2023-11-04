namespace CwaffingTheGungy;

public class EchoChamber : PassiveItem
{
    public static string ItemName         = "Echo Chamber";
    public static string SpritePath       = "echo_chamber_icon";
    public static string ShortDescription = "Primed and Corrupted";
    public static string LongDescription  = "Fired projectiles leave echoes that shoot weakened duplicates of the original projectile for a few seconds.\n\nMuch like their owners, guns have memories, albeit limited and imperfect ones. The Hollowpoint that possesses this chamber is able to tap into these memories to manifest spectres of recently-fired projectiles. While the quality of these spectral projectiles may not be up to par with the originals in every conceivable way, one ought to count their blessings that the Hollowpoint is inadvertently helping at all.";

    internal static Projectile _FlakProjectile;
    internal static GameObject _EchoPrefab;

    public static void Init()
    {
        PickupObject item  = Lazy.SetupPassive<EchoChamber>(ItemName, SpritePath, ShortDescription, LongDescription);
        item.quality       = PickupObject.ItemQuality.C;

        _FlakProjectile = (ItemHelper.Get(Items.FlakBullets) as ComplexProjectileModifier).CollisionSpawnProjectile;
        _EchoPrefab = VFX.RegisterVFXObject("Echo", ResMap.Get("echo_effect"), 16, loops: true, anchor: tk2dBaseSprite.Anchor.MiddleCenter);
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        player.PostProcessProjectile += this.PostProcessProjectile;
    }

    public override DebrisObject Drop(PlayerController player)
    {
        player.PostProcessProjectile -= this.PostProcessProjectile;
        return base.Drop(player);
    }

    private void PostProcessProjectile(Projectile proj, float effectChanceScalar)
    {
        proj.gameObject.AddComponent<EchoingProjectile>();
    }
}

public class EchoProjectileSpawner : MonoBehaviour
{
    private const float _DAMAGE_SCALE = 0.5f;
    private const int _NUM_ECHOS = 3;
    private const float _INITIAL_DELAY = 1f / 4f;
    private const float _DELAY_SCALE = 2f;

    private Projectile _projectile;
    private PlayerController _owner;
    private SpeculativeRigidbody _shooter;
    private Vector2 _echoPosition;
    private float _echoAngle;
    private Quaternion _echoRotation;
    private int _echoSpriteId;
    private tk2dSpriteCollectionData _echoSpriteCollection;
    private Vector2 _echoVelocity;
    private float _echoDamage;
    private float _echoRange;
    private bool _echoRotates;

    public EchoProjectileSpawner Setup(Projectile p, PlayerController pc)
    {
        this._projectile           = p;
        this._owner                = pc;

        this._shooter              = this._owner.specRigidbody;
        this._echoAngle            = this._owner.m_currentGunAngle;

        this._echoRotates          = this._projectile.shouldRotate;
        this._echoPosition         = this._projectile.transform.position;
        this._echoRotation         = this._projectile.transform.rotation;
        this._echoSpriteCollection = this._projectile.sprite.collection;
        this._echoSpriteId         = this._projectile.sprite.spriteId;
        this._echoVelocity         = this._projectile.baseData.speed * this._echoAngle.ToVector();
        this._echoDamage           = this._projectile.baseData.damage;
        this._echoRange            = this._projectile.baseData.range;

        return this;
    }

    public IEnumerator DelayedTrigger_CR()
    {
        GameObject v = SpawnManager.SpawnVFX(EchoChamber._EchoPrefab, this._echoPosition, Quaternion.identity);
            v.ExpireIn(seconds: 2.0f, fadeFor: 2.0f, startAlpha: 0.25f, shrink: true);

        float baseDamageScale = 0.5f;
        float baseSpeedScale = 1.0f;
        float baseSpriteScale = 1.0f;
        float delay = _INITIAL_DELAY;
        for (int i = 0; i < _NUM_ECHOS; ++i)
        {
            yield return new WaitForSeconds(delay);
            delay *= _DELAY_SCALE;
            baseDamageScale *= _DAMAGE_SCALE;
            baseSpriteScale -= 0.1f;
            SpawnProjectile(baseDamageScale, baseSpeedScale, baseSpriteScale);
        }
        UnityEngine.Object.Destroy(this);
    }

    public void DelayedTrigger()
    {
        StartCoroutine(DelayedTrigger_CR());
    }

    public void Trigger()
    {
        SpawnProjectile();
    }

    private void SpawnProjectile(float damageScale = 1.0f, float speedScale = 1.0f, float spriteScale = 1.0f)
    {
        Projectile echo = SpawnManager.SpawnProjectile(EchoChamber._FlakProjectile.gameObject, this._echoPosition, this._echoAngle.EulerZ()).GetComponent<Projectile>();
        if (!echo)
            return;

        echo.baseData.speed      = speedScale * this._echoVelocity.magnitude;
        echo.baseData.damage     = damageScale * this._echoDamage;
        echo.baseData.range      = this._echoRange;
        echo.baseData.force      = 0.01f;
        echo.collidesWithPlayer  = false;
        echo.collidesWithEnemies = true;
        echo.Owner               = this._owner;
        echo.Shooter             = this._shooter;
        echo.shouldRotate        = this._echoRotates;

        echo.sprite.SetSprite(this._echoSpriteCollection, this._echoSpriteId);
        echo.sprite.transform.localScale = new Vector3(spriteScale, spriteScale, 1.0f);
        echo.sprite.gameObject.SetAlpha(damageScale);

        echo.SendInDirection(this._echoVelocity, true);
        echo.UpdateSpeed();

        echo.gameObject.ExpireIn(seconds: 0.5f, fadeFor: 0.5f, startAlpha: Mathf.Sqrt(damageScale));
        echo.sprite.gameObject.ExpireIn(seconds: 0.5f, fadeFor: 0.5f, startAlpha: Mathf.Sqrt(damageScale));
    }
}

public class EchoingProjectile : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private EchoProjectileSpawner _spawner;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner      = this._projectile.Owner as PlayerController;

        GameObject g = UnityEngine.Object.Instantiate(new GameObject(), this._projectile.sprite.WorldCenter, this._projectile.sprite.transform.rotation);
        this._spawner = g.AddComponent<EchoProjectileSpawner>();
        this._spawner
            .Setup(this._projectile, this._owner)
            .DelayedTrigger();
    }
}
