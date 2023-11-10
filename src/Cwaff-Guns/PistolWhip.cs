namespace CwaffingTheGungy;

public class PistolWhip : AdvancedGunBehavior
{
    public static string ItemName         = "Pistol Whip";
    public static string SpriteName       = "pistol_whip";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";

    internal static tk2dSpriteAnimationClip _BulletSprite;
    internal static Projectile _WhipProjectile;
    internal static Projectile _WhipStartProjectile;
    internal static GameObject _WhipChainVFX;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<PistolWhip>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
            gun.SetAttributes(quality: PickupObject.ItemQuality.D, gunClass: GunClass.PISTOL, reloadTime: 1.2f, ammo: 80);
            gun.SetAnimationFPS(gun.shootAnimation, 30);
            gun.SetAnimationFPS(gun.reloadAnimation, 40);
            gun.AddStatToGun(PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);
            gun.AddToSubShop(ItemBuilder.ShopType.Cursula);

        ProjectileModule mod = gun.DefaultModule;
            mod.ammoCost            = 1;
            mod.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
            mod.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            mod.cooldownTime        = 0.1f;
            mod.numberOfShotsInClip = 1;

        _BulletSprite = AnimateBullet.CreateProjectileAnimation(
            ResMap.Get("whip_segment").Base(),
            12, true, new IntVector2(10, 3),
            false, tk2dBaseSprite.Anchor.MiddleLeft, true, true);

        _WhipStartProjectile = Lazy.PrefabProjectileFromGun(gun, setGunDefaultProjectile: false);
            _WhipStartProjectile.AddDefaultAnimation(_BulletSprite);
            _WhipStartProjectile.transform.parent = gun.barrelOffset;
            _WhipStartProjectile.baseData.speed   = 0.01f;
            _WhipStartProjectile.baseData.damage  = 0f;
            _WhipStartProjectile.baseData.range   = 999f;
            _WhipStartProjectile.shouldRotate     = false;
            _WhipStartProjectile.gameObject.GetOrAddComponent<PierceProjModifier>().penetration = 999;
            _WhipStartProjectile.gameObject.AddComponent<WhipChainStart>();

        _WhipProjectile = Lazy.PrefabProjectileFromGun(gun, setGunDefaultProjectile: false);
            _WhipProjectile.AddDefaultAnimation(_BulletSprite);
            _WhipProjectile.transform.parent = gun.barrelOffset;
            _WhipProjectile.baseData.speed   = 0.01f;
            _WhipProjectile.baseData.damage  = 0f;
            _WhipProjectile.baseData.range   = 999f;
            _WhipProjectile.shouldRotate     = false;
            _WhipProjectile.gameObject.GetOrAddComponent<PierceProjModifier>().penetration = 999;

        gun.DefaultModule.projectiles[0] = _WhipStartProjectile;

        _WhipChainVFX   = VFX.RegisterVFXObject("WhipChainVFX", ResMap.Get("whip_segment"),
            fps: 16, loops: true, anchor: tk2dBaseSprite.Anchor.MiddleLeft);
    }
}

public class WhipChainStart : MonoBehaviour
{
    internal const int _CHAIN_LENGTH = 10;

    private Projectile _projectile;
    private PlayerController _owner;
    private float _angle;
    private List<Projectile> _links;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner      = this._projectile.Owner as PlayerController;
        this._angle      = this._owner.m_currentGunAngle;
        this._links      = new();

        Projectile lastProjectile = this._projectile;
            lastProjectile.specRigidbody.CollideWithTileMap = false;
        for (int i = 0; i < _CHAIN_LENGTH; ++i)
        {
            GameObject projObject = SpawnManager.SpawnProjectile(PistolWhip._WhipProjectile.gameObject, this._projectile.transform.position, Quaternion.identity);
                projObject.GetOrAddComponent<WhipChain>().Setup(lastProjectile, this._owner);

            Projectile proj = projObject.GetComponent<Projectile>();
                proj.specRigidbody.CollideWithTileMap = false;
                proj.collidesWithEnemies = false;
                proj.collidesWithPlayer = false;
                proj.collidesWithProjectiles = false;
                proj.collidesOnlyWithPlayerProjectiles = false;
                lastProjectile = proj;
                this._links.Add(proj);
        }

        StartCoroutine(WhipItGood());
    }

    private void DestroyAllProjectiles()
    {
        if (this._projectile)
        {
            this._projectile.transform.parent = null;
            UnityEngine.Object.Destroy(this._projectile.sprite);
            UnityEngine.Object.Destroy(this._projectile.specRigidbody);
            UnityEngine.Object.Destroy(this._projectile.gameObject);
        }
        foreach (Projectile p in this._links)
        {
            if (!p)
                continue;
            UnityEngine.Object.Destroy(p.sprite);
            UnityEngine.Object.Destroy(p.specRigidbody);
            UnityEngine.Object.Destroy(p.gameObject);
        }
        this._links.Clear();
    }

    private void OnDestroy()
    {
        DestroyAllProjectiles();
    }

    private void Update()
    {
      // enter update code here
    }

    const float SPEED_SCALE = 10f; // animation speed scale, for debugging
    const float WHIP_RANGE = 4.0f;
    const float RETRACT_MAX = 0.65f;
    const float RETRACT_MIN = 0.25f;
    const float SEGMENT_PIXELS = 8.0f;
    static readonly Vector2 SEGMENT_VEC = new Vector2(SEGMENT_PIXELS, 0);
    static readonly float[] TIMES = {
        SPEED_SCALE * 0.00f, // start
        SPEED_SCALE * 0.05f, // charge
        SPEED_SCALE * 0.20f, // whip
        SPEED_SCALE * 0.30f,  // retract
        // 10f * 0.40f,  // hold
        // 10f * 0.50f,  // retract
    };
    static readonly float TOTAL_TIME = TIMES[TIMES.Length - 1];
    private IEnumerator WhipItGood()
    {
        int phase = 1;
        float numLinks = (float)this._links.Count();
        float invLinks = 1.0f / numLinks;
        // this._projectile.sprite.transform.parent = this._owner.specRigidbody.transform;
        for (float elapsed = 0f; elapsed < TOTAL_TIME; elapsed += BraveTime.DeltaTime)
        {
            if (elapsed > TIMES[phase])
                ++phase;
            float phasePercent = (elapsed - TIMES[phase-1]) / (TIMES[phase] - TIMES[phase-1]);
            // ETGModConsole.Log($"timer = {phasePercent} of phase {phase}");

            float firstAngle = 0f;
            float angleDelta = 0f;
            float maxDistance = 0f;
            switch(phase)
            {
                case 1: // charge
                    firstAngle = -180f; // pull back whip in the opposite direction that we're facing
                    angleDelta = 0f; // segments are all in a straight line
                    maxDistance = phasePercent * WHIP_RANGE * RETRACT_MAX; // pull back the whip to 65% of its max length
                    break;
                case 2: // whip
                    phasePercent = (phasePercent * phasePercent);
                    firstAngle = (1 - phasePercent) * -180f; // handle of whip gradually faces the direction we're aiming
                    angleDelta = phasePercent * -firstAngle; // whip and tail will eventually be facing opposite angles
                    if (phasePercent < 0.5f)
                        maxDistance = WHIP_RANGE * Mathf.Lerp(RETRACT_MAX, RETRACT_MIN, 2f * phasePercent); // whip slowly retracts
                    else
                        maxDistance = WHIP_RANGE * Mathf.Lerp(RETRACT_MIN, 1.0f, 2f * (phasePercent - 0.5f)); // whip slowly extends
                    break;
                // case 3: // hold
                //     firstAngle = 0f; // handle of whip is now facing in the direction we're aiming
                //     angleDelta = (1 - phasePercent) * 180f; // whip and tail will eventually reconvene on the facing angle
                //     maxDistance = WHIP_RANGE; // whip is at max length
                //     break;
                // case 4: // retract
                case 3: // retract
                    firstAngle = 0f; // handle of whip is still facing in the direction we're aiming
                    angleDelta = 0f; // segments are once again in a straight line
                    maxDistance = (1 - phasePercent) * WHIP_RANGE; // retract the whip fully
                    break;
                default: // shouldn't happen
                    break;
            }

            float trueFirstAngle = this._angle + firstAngle;
            float linkAngleDelta = angleDelta * invLinks;
            float linkDistance   = maxDistance * invLinks;
            Projectile lastLink  = this._projectile;
                lastLink.sprite.transform.position =
                    this._owner?.CurrentGun?.barrelOffset?.transform?.position
                    ?? this._owner?.sprite.WorldCenter
                    ?? Vector2.zero;
                lastLink.sprite.transform.rotation = trueFirstAngle.EulerZ();
            for (int i = 0 ; i < numLinks; ++i)
            {
                Projectile link                = this._links[i];
                float myAngle                  = (trueFirstAngle + i * linkAngleDelta).Clamp360();
                link.sprite.transform.position = lastLink.sprite.transform.position.XY() + linkDistance * myAngle.ToVector();
                link.sprite.transform.rotation = myAngle.EulerZ();
                lastLink                       = link;
            }
            yield return null;
        }

        DestroyAllProjectiles();
        yield break;
    }
}

public class WhipChain : MonoBehaviour
{
    private Projectile _parent;
    private Projectile _projectile;
    private PlayerController _owner;

    private void Start()
    {
        base.GetComponent<Projectile>().specRigidbody.CollideWithTileMap = false;
    }

    public void Setup(Projectile parent, PlayerController owner)
    {
        this._projectile                  = base.GetComponent<Projectile>();
        this._owner                       = owner;
        this._parent                      = parent;
        // this._projectile.transform.parent = this._parent.transform;
    }
}
