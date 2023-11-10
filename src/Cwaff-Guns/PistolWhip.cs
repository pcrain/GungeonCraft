namespace CwaffingTheGungy;

public class PistolWhip : AdvancedGunBehavior
{
    public static string ItemName         = "Pistol Whip";
    public static string SpriteName       = "pistol_whip";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";

    internal static tk2dSpriteAnimationClip _BulletSprite;
    internal static Projectile _WhipStartProjectile;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<PistolWhip>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
            gun.SetAttributes(quality: PickupObject.ItemQuality.D, gunClass: GunClass.PISTOL, reloadTime: 0.01f, ammo: 800);
            gun.SetAnimationFPS(gun.shootAnimation, 30);
            gun.SetAnimationFPS(gun.reloadAnimation, 40);
            gun.AddStatToGun(PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);
            gun.AddToSubShop(ItemBuilder.ShopType.Cursula);
            gun.muzzleFlashEffects = null;

        ProjectileModule mod = gun.DefaultModule;
            mod.ammoCost            = 1;
            mod.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
            mod.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            mod.cooldownTime        = WhipChainStart.TOTAL_TIME + C.FRAME;
            mod.numberOfShotsInClip = -1;

        _BulletSprite = AnimateBullet.CreateProjectileAnimation(
            ResMap.Get("whip_segment").Base(),
            12, true, new IntVector2(10, 3),
            false, tk2dBaseSprite.Anchor.MiddleLeft, true, true);

        _WhipStartProjectile = Lazy.PrefabProjectileFromGun(gun);
            _WhipStartProjectile.AddDefaultAnimation(_BulletSprite);
            _WhipStartProjectile.transform.parent = gun.barrelOffset;
            _WhipStartProjectile.baseData.speed   = 0.01f;
            _WhipStartProjectile.baseData.damage  = 0f;
            _WhipStartProjectile.baseData.range   = 999f;
            _WhipStartProjectile.gameObject.AddComponent<WhipChainStartProjectile>();
    }
}

public class WhipChainStartProjectile : MonoBehaviour
{
    private void Start()
    {
        UnityEngine.GameObject.Instantiate(new GameObject(), Vector3.zero, Quaternion.identity)
            .AddComponent<WhipChainStart>()
            .Setup(base.gameObject.GetComponent<Projectile>()?.Owner as PlayerController);
        UnityEngine.GameObject.Destroy(base.gameObject);
    }
}

public class WhipChainStart : MonoBehaviour
{
    internal const int   _CHAIN_LENGTH     = 60;
    internal const float _INVLINKS         = 1.0f / _CHAIN_LENGTH;
    internal const int   _HANDLE_LENGTH    = _CHAIN_LENGTH / 10;
    internal const float _WHIP_WIDTH       = 3f * C.PIXEL_SIZE;

    internal const float _SPEED_SCALE      = 1f; // animation speed scale, for debugging

    internal const float _WHIP_RANGE       = 6.0f;
    internal const float _RETRACT_MAX      = 0.65f;
    internal const float _MAX_AMP          = 0.5f;
    internal static readonly float[] TIMES = {
        _SPEED_SCALE * 0.00f, // start
        _SPEED_SCALE * 0.05f, // charge
        _SPEED_SCALE * 0.20f, // whip
        _SPEED_SCALE * 0.40f, // retract
    };
    internal static readonly float TOTAL_TIME = TIMES[TIMES.Length - 1];

    private PlayerController _owner;
    private float _angle;
    private List<GameObject> _links;

    public void Setup(PlayerController owner)
    {
        this._owner = owner;
        this._angle = this._owner.m_currentGunAngle.Clamp180();
        this._links = new();
        for (int i = 0; i < _CHAIN_LENGTH; ++i)
            this._links.Add(null);

        StartCoroutine(WhipItGood());
    }

    private void DestroyWhip()
    {
        foreach (GameObject g in this._links)
            if (g)
                UnityEngine.Object.Destroy(g);
        this._links.Clear();
    }

    private void OnDestroy()
    {
        DestroyWhip();
    }

    private IEnumerator WhipItGood()
    {
        const float CYCLES = 3f;
        float freq = (CYCLES * (2f * Mathf.PI)) / _CHAIN_LENGTH;

        this._owner.CurrentGun.ToggleRenderers(false);
        AkSoundEngine.PostEvent("whip_sound", base.gameObject);

        int phase = 1;
        for (float elapsed = 0f; elapsed < TOTAL_TIME; elapsed += BraveTime.DeltaTime)
        {
            if (elapsed > TIMES[phase])
                ++phase;
            float phasePercent = (elapsed - TIMES[phase-1]) / (TIMES[phase] - TIMES[phase-1]);

            float maxDistance = 0f;
            switch(phase)
            {
                case 1: // charge
                    float invPhasePercent = 1f - phasePercent;
                    float invEaseSquare = 1f - (invPhasePercent * invPhasePercent);
                    maxDistance = -invEaseSquare * _WHIP_RANGE * _RETRACT_MAX; // pull back the whip to 65% of its max length
                    break;
                case 2: // whip
                    float easeCubic = (phasePercent * phasePercent * phasePercent);
                    maxDistance = _WHIP_RANGE * Mathf.Lerp(-_RETRACT_MAX, 1.0f, easeCubic); // whip slowly extends
                    break;
                case 3: // retract
                    maxDistance = (1 - phasePercent) * _WHIP_RANGE; // retract the whip fully
                    break;
                default: // shouldn't happen
                    break;
            }
            float absRange       = Mathf.Abs(maxDistance) / _WHIP_RANGE;
            bool flipped         = Mathf.Abs(this._angle) > 90f;
            float maxAmp         = (flipped ? -_MAX_AMP : _MAX_AMP) * (1f - (absRange * absRange));
            float linkDistance   = maxDistance * _INVLINKS;
            Quaternion baseEuler = this._angle.EulerZ();
            Vector3 basePos      =
                this._owner?.primaryHand.transform.position
                ?? this._owner?.sprite.WorldCenter
                ?? Vector3.zero;
            Vector2 segBegin = Vector2.zero;

            for (int i = 0 ; i < _CHAIN_LENGTH; ++i)
            {
                Vector2 segEnd = basePos + baseEuler * (new Vector2((i + 1) * linkDistance, maxAmp * Mathf.Sin((i + 1) * freq)));
                if (this._links[i] == null)
                    this._links[i] = Ticonderogun.FancyLine(
                        segBegin, segEnd, _WHIP_WIDTH, spriteId: VFX.sprites[i >= _HANDLE_LENGTH ? "whip_segment" : "whip_segment_base"]);
                else
                {
                    Vector2 delta                = segEnd - segBegin;
                    Quaternion rot               = delta.EulerZ();
                    tk2dSlicedSprite quad        = this._links[i].GetComponent<tk2dSlicedSprite>();
                    quad.dimensions              = C.PIXELS_PER_TILE * (new Vector2(delta.magnitude, _WHIP_WIDTH));
                    quad.transform.localRotation = rot;
                    quad.transform.position      = segBegin + (0.5f * _WHIP_WIDTH * delta.normalized.Rotate(-90f));
                }
                segBegin = segEnd;
            }
            yield return null;
        }

        this._owner.CurrentGun.ToggleRenderers(true);
        DestroyWhip();
        yield break;
    }
}
