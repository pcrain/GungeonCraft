namespace CwaffingTheGungy;

public class Nycterian : CwaffGun
{
    public static string ItemName         = "Nycterian";
    public static string ShortDescription = "Locate the Echoes";
    public static string LongDescription  = "Fires bats that occasionally screech while airborne. Screeches have a chance to draw fire from nearby enemies, with the chance decreasing with distance.";
    public static string Lore             = "Bats. Flittery, noisy, but usually not explosive or otherwise as harmful as their Bullat cousins. They're still weighty enough to pack a punch when launched at high velocity, and their incessant screeching can be weaponized as a useful distraction, making them the 7th most effective blind mammalian projectile known to modern ammunition specialists.";

    internal static GameObject _DistractedVFX;

    public static void Init()
    {
        Lazy.SetupGun<Nycterian>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.PISTOL, reloadTime: 1.1f, ammo: 325, shootFps: 20, reloadFps: 20,
            muzzleFrom: Items.Mailbox, fireAudio: "nycterian_shoot_sound", reloadAudio: "nycterian_reload_sound", smoothReload: 0.1f)
          .InitProjectile(GunData.New(clipSize: 10, cooldown: 0.3f, shootStyle: ShootStyle.SemiAutomatic, scale: 1.5f, customClip: true,
            damage: 7.0f, speed: 18.0f, range: 100.0f, sprite: "bat_projectile", fps: 12))
          .Attach<BounceProjModifier>(bounce => { bounce.numberOfBounces = 1; })
          .Attach<PierceProjModifier>(pierce => {
              pierce.penetration = 99;
              pierce.penetratesBreakables = true;
            })
          .Attach<BatProjectile>();

        _DistractedVFX = VFX.Create("distracted_vfx", fps: 18, emissivePower: 1, emissiveColour: Color.magenta);
    }
}

public class BatProjectile : MonoBehaviour
{
    private const float _ECHO_INIT_DELAY = 0.1f;
    private const float _ECHO_DELAY = 0.5f;

    private static readonly Color _BatGray = new Color(0.2f, 0.2f, 0.3f, 1f);

    private float _timer = _ECHO_INIT_DELAY;
    private Projectile _proj = null;
    private bool _mastered = false;

    private void Start()
    {
        tk2dBaseSprite sprite = base.GetComponent<Projectile>().sprite;
        sprite.usesOverrideMaterial = true;
        Material m = sprite.renderer.material;
        m.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
        m.SetFloat(CwaffVFX._EmissivePowerId, 3f);
        m.SetFloat(CwaffVFX._EmissiveColorPowerId, 1.55f);

        this._proj = base.gameObject.GetComponent<Projectile>();
        EasyTrailBullet trail = this._proj.gameObject.AddComponent<EasyTrailBullet>();
            trail.StartWidth = 0.2f;
            trail.EndWidth   = 0.125f;
            trail.LifeTime   = 0.25f;
            trail.BaseColor  = _BatGray;
            trail.StartColor = _BatGray;
            trail.EndColor   = Color.clear;

        this._mastered = this._proj.Mastered<Nycterian>();
        this._timer = _ECHO_INIT_DELAY + UnityEngine.Random.value * (_ECHO_DELAY - _ECHO_INIT_DELAY);
    }

    private void Update()
    {
        if (!this._proj || !this._proj.isActiveAndEnabled) //TODO: check if projectile is actually alive and not decommisioned
            return;
        if ((this._timer -= BraveTime.DeltaTime) > 0)
            return;
        this._timer = _ECHO_DELAY;
        DoEcho();
    }

    private void DoEcho()
    {
        Lazy.CreateDecoy(this._proj.SafeCenter).AddComponent<DecoyEcho>().mastered = this._mastered;
        base.gameObject.Play("bat_screech_sound");
        Exploder.DoDistortionWave(center: this._proj.SafeCenter,
            distortionIntensity: (this._mastered ? 3.0f : 1.5f), distortionRadius: 0.05f, maxRadius: (this._mastered ? 5.0f : 2.75f), duration: 0.25f);
    }
}

public class DecoyEcho : MonoBehaviour
{
    private const float _DECOY_TIME     = 1f;
    private const float _DECOY_MIN_DIST = 2f;
    private const float _DECOY_MAX_DIST = 10f;

    private RoomHandler _room = null;

    public bool mastered = false;

    private void Start()
    {
        base.gameObject.ExpireIn(_DECOY_TIME);
        if (GameManager.Instance.Dungeon.GetRoomFromPosition(base.transform.position.IntXY(VectorConversions.Floor)) is not RoomHandler room)
            return;

        this._room = room;
        SpeculativeRigidbody body = base.gameObject.GetComponent<SpeculativeRigidbody>();
        foreach (AIActor enemy in room.SafeGetEnemiesInRoom())
        {
            if (!enemy || !enemy.IsHostile(canBeNeutral: true) || (!this.mastered && enemy.IsABoss()))
                continue;

            if (!this.mastered)
            {
                float dist = (base.transform.position.XY() - enemy.CenterPosition).magnitude;
                if (dist > _DECOY_MAX_DIST)
                    continue;
                float decoyChance = Mathf.InverseLerp(_DECOY_MAX_DIST, _DECOY_MIN_DIST, dist);
                if (decoyChance < UnityEngine.Random.value)
                    continue;
            }

            enemy.OverrideTarget = body;
            GameObject vfx = SpawnManager.SpawnVFX(Nycterian._DistractedVFX, enemy.sprite.WorldTopCenter + new Vector2(0f, 1f), Quaternion.identity, ignoresPools: true);
                tk2dSprite sprite = vfx.GetComponent<tk2dSprite>();
                    sprite.HeightOffGround = 1f;
                vfx.transform.parent = enemy.sprite.transform;
                vfx.AddComponent<GlowAndFadeOut>().Setup(
                    fadeInTime: 0.15f, glowInTime: 0.10f, holdTime: 0.0f, glowOutTime: 0.10f, fadeOutTime: 0.15f, maxEmit: 50f, destroy: true);
            enemy.gameObject.PlayUnique("distracted_sound");
        }
    }

    private void OnDestroy()
    {
        if (this._room == null)
            return;
        SpeculativeRigidbody body = base.gameObject.GetComponent<SpeculativeRigidbody>();
        foreach (AIActor enemy in this._room.SafeGetEnemiesInRoom())
            if (enemy && enemy.OverrideTarget == body)
                enemy.OverrideTarget = null;
    }
}
