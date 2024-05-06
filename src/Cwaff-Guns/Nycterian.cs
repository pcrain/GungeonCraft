namespace CwaffingTheGungy;

public class Nycterian : AdvancedGunBehavior
{
    public static string ItemName         = "Nycterian";
    public static string ShortDescription = "Locate the Echoes";
    public static string LongDescription  = "Fires bats that occasionally screech while airborne. Screeches have a chance to draw fire from nearby enemies, with the chance decreasing with distance.";
    public static string Lore             = "Bats. Flittery, noisy, but usually not explosive or otherwise as harmful as their Bullat cousins. They're still weighty enough to pack a punch when launched at high velocity, and their incessant screeching can be weaponized as a useful distraction, making them the 7th most effective blind mammalian projectile known to modern ammunition specialists.";

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Nycterian>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.PISTOL, reloadTime: 1.1f, ammo: 425, shootFps: 20, reloadFps: 20,
                muzzleFrom: Items.Mailbox, fireAudio: "nycterian_shoot_sound", reloadAudio: "nycterian_reload_sound");

        gun.InitProjectile(GunData.New(clipSize: 10, cooldown: 0.19f, shootStyle: ShootStyle.SemiAutomatic, scale: 1.5f,
          damage: 7.0f, speed: 27.0f, range: 100.0f, sprite: "bat_projectile", fps: 12)).Attach<BatProjectile>();
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
        if (room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All) is not List<AIActor> activeEnemies)
            return;
        this._room = room;

        SpeculativeRigidbody body = base.gameObject.GetComponent<SpeculativeRigidbody>();
        foreach (AIActor enemy in activeEnemies)
        {
            if (!enemy)
                continue;
            float dist = (base.transform.position.XY() - enemy.CenterPosition).magnitude;
            if (dist > _DECOY_MAX_DIST)
                continue;
            float decoyChance = Mathf.InverseLerp(_DECOY_MAX_DIST, _DECOY_MIN_DIST, dist);
            if ((decoyChance * decoyChance) >= UnityEngine.Random.value)
                enemy.OverrideTarget = body;
        }
    }

    private void OnDestroy()
    {
        if (this._room == null)
            return;
        if (this._room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All) is not List<AIActor> activeEnemies)
            return;
        SpeculativeRigidbody body = base.gameObject.GetComponent<SpeculativeRigidbody>();
        foreach (AIActor enemy in activeEnemies)
            if (enemy && enemy.OverrideTarget == body)
                enemy.OverrideTarget = null;
    }
}
