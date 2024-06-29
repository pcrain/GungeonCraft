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
        Gun gun = Lazy.SetupGun<Nycterian>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.PISTOL, reloadTime: 1.1f, ammo: 425, shootFps: 20, reloadFps: 20,
                muzzleFrom: Items.Mailbox, fireAudio: "nycterian_shoot_sound", reloadAudio: "nycterian_reload_sound");

        gun.InitProjectile(GunData.New(clipSize: 10, cooldown: 0.19f, shootStyle: ShootStyle.SemiAutomatic, scale: 1.5f,
          damage: 7.0f, speed: 27.0f, range: 100.0f, sprite: "bat_projectile", fps: 12)).Attach<BatProjectile>();
        _DistractedVFX = VFX.Create("distracted_vfx",
            fps: 18, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 1, emissiveColour: Color.magenta);
    }
}

public class BatProjectile : MonoBehaviour
{
    private const float _ECHO_INIT_DELAY = 0.1f;
    private const float _ECHO_DELAY = 0.5f;

    private static readonly Color _BatGray = new Color(0.2f, 0.2f, 0.3f, 1f);

    private float _timer = _ECHO_INIT_DELAY;
    private Projectile _proj = null;

    private void Start()
    {
        tk2dBaseSprite sprite = base.GetComponent<Projectile>().sprite;
        sprite.usesOverrideMaterial = true;
        Material m = sprite.renderer.material;
        m.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
        m.SetFloat("_EmissivePower", 3f);
        // m.SetColor("_EmissiveColor", new Color(0.25f, 0.25f, 0.25f));
        m.SetFloat("_EmissiveColorPower", 1.55f);

        this._proj = base.gameObject.GetComponent<Projectile>();
        EasyTrailBullet trail = this._proj.gameObject.AddComponent<EasyTrailBullet>();
            trail.StartWidth = 0.2f;
            trail.EndWidth   = 0.125f;
            trail.LifeTime   = 0.25f;
            trail.BaseColor  = _BatGray;
            trail.StartColor = _BatGray;
            trail.EndColor   = Color.clear;

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
        Lazy.CreateDecoy(this._proj.SafeCenter).AddComponent<DecoyEcho>();
        base.gameObject.Play("bat_screech_sound");
        Exploder.DoDistortionWave(center: this._proj.SafeCenter,
            distortionIntensity: 1.5f, distortionRadius: 0.05f, maxRadius: 2.75f, duration: 0.25f);
    }
}

public class DecoyEcho : MonoBehaviour
{
    private const float _DECOY_TIME     = 1f;
    private const float _DECOY_MIN_DIST = 2f;
    private const float _DECOY_MAX_DIST = 8f;

    private RoomHandler _room = null;

    private void Start()
    {
        base.gameObject.ExpireIn(_DECOY_TIME);
        if (GameManager.Instance.Dungeon.GetRoomFromPosition(base.transform.position.IntXY(VectorConversions.Floor)) is not RoomHandler room)
            return;

        this._room = room;
        SpeculativeRigidbody body = base.gameObject.GetComponent<SpeculativeRigidbody>();
        foreach (AIActor enemy in room.SafeGetEnemiesInRoom())
        {
            if (!enemy || !enemy.IsHostileAndNotABoss(canBeNeutral: true))
                continue;
            float dist = (base.transform.position.XY() - enemy.CenterPosition).magnitude;
            if (dist > _DECOY_MAX_DIST)
                continue;
            float decoyChance = Mathf.InverseLerp(_DECOY_MAX_DIST, _DECOY_MIN_DIST, dist);
            if ((decoyChance * decoyChance) < UnityEngine.Random.value)
                continue;

            enemy.OverrideTarget = body;
            GameObject vfx = SpawnManager.SpawnVFX(Nycterian._DistractedVFX, enemy.sprite.WorldTopCenter + new Vector2(0f, 1f), Quaternion.identity, ignoresPools: true);
                tk2dSprite sprite = vfx.GetComponent<tk2dSprite>();
                    sprite.HeightOffGround = 1f;
                vfx.transform.parent = enemy.sprite.transform;
                vfx.AddComponent<GlowAndFadeOut>().Setup(
                    fadeInTime: 0.15f, glowInTime: 0.10f, glowOutTime: 0.10f, fadeOutTime: 0.15f, maxEmit: 50f, destroy: true);
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
