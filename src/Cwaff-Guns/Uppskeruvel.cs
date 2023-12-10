namespace CwaffingTheGungy;

public class Uppskeruvel : AdvancedGunBehavior
{
    public static string ItemName         = "Uppskeruvel"; // é
    public static string SpriteName       = "uppskeruvel";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal const string _SoulSpriteUI   = $"{C.MOD_PREFIX}:_SoulSpriteUI";  // need the string immediately for preloading in Main()

    internal static int        _UppskeruvelId    = -1;
    internal static GameObject _LostSoulPrefab   = null;
    internal static GameObject _CombatSoulPrefab = null;
    internal static GameObject _SoulCollectVFX   = null;

    public int souls = 0;

    private List<UppskeruvelCombatSoul> _extantSouls = new();
    private List<int> _usedIndices = new();

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Uppskeruvel>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 0.5f, ammo: 800);
            gun.SetFireAudio("uppskeruvel_fire_sound");
            gun.SetReloadAudio("alyx_reload_sound");
            gun.SetMuzzleVFX(Items.Mailbox); // innocuous muzzle flash effects

        gun.InitProjectile(new(clipSize: 12, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic, damage: 4f,
          sprite: "uppskeruvel_projectile", fps: 12, scale: 0.5f, anchor: Anchor.MiddleLeft)
        ).Attach<UppskeruvelProjectile>(
        );

        gun.gameObject.AddComponent<UppskeruvelAmmoDisplay>();

        _LostSoulPrefab = VFX.Create("poe_soul", fps: 8, loops: true, anchor: Anchor.LowerCenter/*, emissivePower: 0.4f*/);
            _LostSoulPrefab.AddComponent<UppskeruvelLostSoul>();

        _CombatSoulPrefab = VFX.Create("poe_soul", fps: 8, loops: true, anchor: Anchor.MiddleCenter, scale: 2.0f/*, emissivePower: 0.4f*/);
            _CombatSoulPrefab.AddComponent<UppskeruvelCombatSoul>();

        _SoulCollectVFX = VFX.Create("soul_collect", 16, loops: true, anchor: Anchor.MiddleCenter);

        _UppskeruvelId = gun.PickupObjectId;
    }

    protected override void OnPickup(GameActor owner)
    {
        if (!this.everPickedUpByPlayer)
        {
            for (int i = 0; i < 5; ++i)
                SpawnSoul();
        }
        base.OnPickup(owner);
    }

    public int GetNextAvailableIndex()
    {
        for (int i = 0; i < 100; ++i)
        {
            if (!this._usedIndices.Contains(i))
            {
                this._usedIndices.Add(i);
                return i;
            }
        }
        ETGModConsole.Log($"  GetNextIndex() FAILED, THIS SHOULD NEVER HAPPEN");
        return -1;
    }

    public void LaunchNextAvailableSoul(AIActor enemy)
    {
        int minIndex = 999;
        UppskeruvelCombatSoul nextSoul = null;
        foreach (UppskeruvelCombatSoul soul in this._extantSouls)
        {
            if (!soul.CanLaunch() || !soul.index.HasValue || soul.index.Value >= minIndex)
                continue;
            nextSoul = soul;
            minIndex = soul.index.Value;
        }
        if (!nextSoul)
            return;

        RemoveIndex(nextSoul.index.Value);
        nextSoul.index = null;
        nextSoul.Launch(enemy);
    }

    private void RemoveIndex(int i)
    {
        this._usedIndices.Remove(i);
    }

    public UppskeruvelCombatSoul SpawnSoul()
    {
        UppskeruvelCombatSoul soul = UnityEngine.Object.Instantiate(
            Uppskeruvel._CombatSoulPrefab, this.Player.CenterPosition, Quaternion.identity).GetComponent<UppskeruvelCombatSoul>();
        this._extantSouls.Add(soul);
        soul.Setup(this.Player, this);
        return soul;
    }
}

public class UppskeruvelAmmoDisplay : CustomAmmoDisplay
{
    private Gun _gun;
    private Uppskeruvel _uppies;
    private PlayerController _owner;
    private void Start()
    {
        this._gun = base.GetComponent<Gun>();
        this._uppies = this._gun.GetComponent<Uppskeruvel>();
        this._owner = this._gun.CurrentOwner as PlayerController;
    }

    public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
    {
        if (!this._owner)
            return false;

        uic.SetAmmoCountLabelColor(Color.white);
        Vector3 relVec = Vector3.zero;
        uic.GunAmmoCountLabel.AutoHeight = true; // enable multiline text
        uic.GunAmmoCountLabel.ProcessMarkup = true; // enable multicolor text
        uic.GunAmmoCountLabel.Text = $"[sprite \"{Uppskeruvel._SoulSpriteUI}\"][color #6666dd]x{this._uppies.souls}[/color]\n{this._gun.CurrentAmmo}/{this._gun.AdjustedMaxAmmo}";
        return true;
    }
}

public class UppskeruvelProjectile : MonoBehaviour
{
    private const float _SOUL_LAUNCH_SPEED = 7f;
    private const float _SOULS_PER_HEALTH  = 0.1f;

    private Projectile _projectile = null;
    private PlayerController _owner = null;
    private Uppskeruvel _gun = null;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        if (this._owner.CurrentGun.GetComponent<Uppskeruvel>() is not Uppskeruvel uppies)
            return;

        this._gun = uppies;
        this._projectile.OnWillKillEnemy += this.OnWillKillEnemy;
        this._projectile.OnHitEnemy += this.OnHitEnemy;
    }

    private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool killed)
    {
        if (!enemy?.aiActor)
            return;
        this._gun.LaunchNextAvailableSoul(enemy.aiActor);
    }

    private void OnWillKillEnemy(Projectile bullet, SpeculativeRigidbody enemy)
    {
        if (!(enemy?.aiActor?.IsHostile() ?? false))
            return; // avoid processing effect for non-hostile enemies

        Vector2 ppos = enemy.UnitCenter;
        int soulsToSpawn = Mathf.CeilToInt(_SOULS_PER_HEALTH * (enemy.healthHaver?.GetMaxHealth() ?? 0));
        for (int i = 0; i < soulsToSpawn; ++i)
        {
            float angle = Lazy.RandomAngle();
            Vector2 finalPos = ppos + BraveMathCollege.DegreesToVector(angle);
            UnityEngine.Object.Instantiate(Uppskeruvel._LostSoulPrefab, finalPos, Quaternion.identity
                ).GetComponent<UppskeruvelLostSoul>(
                ).Setup(angle.ToVector(_SOUL_LAUNCH_SPEED));
        }
    }
}

public class UppskeruvelLostSoul : MonoBehaviour
{
    internal const float _BOB_SPEED  = 4f;
    internal const float _BOB_HEIGHT = 0.20f;

    const float _ATTRACT_RADIUS_SQR = 25f;  // range before we start homing in on player
    const float _PICKUP_RADIUS_SQR  = 2f;   // range before we are picked up by player
    const float _HOME_ACCEL         = 44f;  // acceleration per second towards player
    const float _FRICTION           = 0.96f;
    const float _MAX_LIFE           = 3f;

    private bool _setup             = false;
    private PlayerController _owner = null;
    private float _homeSpeed        = 0.0f;
    private tk2dSprite _sprite      = null;
    private float _lifetime         = 0.0f;
    private Vector2 _velocity       = Vector2.zero;
    private Vector3 _basePos        = Vector2.zero;

    public void Setup(Vector2 velocity)
    {
        this._sprite = base.GetComponent<tk2dSprite>();
        this._velocity = velocity;
        this._basePos = base.transform.position;
        this._setup = true;
    }

    private void Update()
    {
        if (!this._setup)
            return;

        this._lifetime += BraveTime.DeltaTime;
        if (this._owner)
        {
            Vector2 delta = (this._owner.CenterPosition - base.transform.position.XY());
            Vector2 deltaNorm = delta.normalized;
            this._homeSpeed += _HOME_ACCEL * BraveTime.DeltaTime;
            // Weighted average of natural and direct velocity towards player
            this._velocity = this._homeSpeed * Vector2.Lerp((this._velocity.normalized + deltaNorm).normalized, deltaNorm, 0.2f);
            this._basePos += (this._velocity * BraveTime.DeltaTime).ToVector3ZUp();
            base.transform.position = new Vector2(this._basePos.x, this._basePos.y + _BOB_HEIGHT * Mathf.Sin(_BOB_SPEED * this._lifetime)).ToVector3ZisY();

            FancyVFX.Spawn(Outbreak._OutbreakSmokeVFX, base.transform.position, Lazy.RandomEulerZ(),
                velocity: Lazy.RandomVector(0.1f), lifetime: 0.25f, fadeOutTime: 0.5f);

            if (delta.sqrMagnitude > _PICKUP_RADIUS_SQR)
                return;

            foreach (Gun gun in this._owner.inventory.AllGuns)
            {
                if (gun.GetComponent<Uppskeruvel>() is not Uppskeruvel uppies)
                    continue;
                uppies.souls += 1;
                break;
            }
            AkSoundEngine.PostEvent("pickup_poe_soul_sound_stop_all", base.gameObject);
            AkSoundEngine.PostEvent("pickup_poe_soul_sound", base.gameObject);
            float rotOffset = 90f * UnityEngine.Random.value;
            for (int i = 0; i < 4; ++i)
                FancyVFX.Spawn(Uppskeruvel._SoulCollectVFX, base.transform.position, Lazy.RandomEulerZ(),
                  velocity: (rotOffset + 90f * i).ToVector(4f), lifetime: 0.5f, fadeOutTime: 0.75f);
            UnityEngine.Object.Destroy(base.gameObject);
            return;
        }

        if (this._velocity.sqrMagnitude > 1f)
        {
            this._velocity *= (float)Lazy.FastPow(_FRICTION, C.FPS * BraveTime.DeltaTime);
            this._basePos += (this._velocity * BraveTime.DeltaTime).ToVector3ZUp();
        }
        else
            this._velocity = this._velocity.normalized;
        base.transform.position = new Vector2(this._basePos.x, this._basePos.y + _BOB_HEIGHT * Mathf.Sin(_BOB_SPEED * this._lifetime)).ToVector3ZisY();

        if (this._lifetime > _MAX_LIFE)
        {
            for (int i = 0; i < 4; ++i)
            {
                FancyVFX.Spawn(Outbreak._OutbreakSmokeVFX, base.transform.position, Lazy.RandomEulerZ(),
                    velocity: Lazy.RandomVector(0.5f), lifetime: 0.3f, fadeOutTime: 0.6f);
            }
            UnityEngine.Object.Destroy(base.gameObject);
            return;
        }

        foreach (PlayerController player in GameManager.Instance.AllPlayers)
        {
            if (!player || !player.isActiveAndEnabled || player.IsGhost)
                continue;
            if ((base.transform.position.XY() - player.CenterPosition).sqrMagnitude > _ATTRACT_RADIUS_SQR)
                continue;
            if (!player.HasGun(Uppskeruvel._UppskeruvelId))
                continue;
            this._owner = player;
        }
    }
}


public class UppskeruvelCombatSoul : MonoBehaviour
{
    private enum State {
        SPAWNING,   // spawning in after fulfilling requirements to spawn or entering a new floor
        FOLLOWING,  // following the player around after spawning in
        SEEKING,    // seeking an enemy that has been recently shot by a projectile
        COOLDOWN,   // on cooldown after hitting an enemy
    }

    public int? index                = null;

    private const float _SPAWN_TIME  = 0.5f;
    private const float _VANISH_TIME = 0.25f;
    private const float _ACCEL_SEC   = 0.5f;
    private const float _HALF_TIME   = 0.1f;  // in GlideTowardsTarget mode, time required to get halfway to our target
    private const float _SPACING     = 0.5f;  // spacing between followers

    private bool _setup             = false;
    private PlayerController _owner = null;
    private AIActor _enemy          = null;
    private Uppskeruvel _gun        = null;
    private float _homeSpeed        = 0.0f;
    private tk2dSprite _sprite      = null;
    private float _lifetime         = 0.0f;
    private Vector2 _velocity       = Vector2.zero;
    private Vector3 _basePos        = Vector2.zero;
    private Vector3 _targetPos      = Vector2.zero;
    private float _jiggle           = 0f; // random offset from directly behind player
    private float _timer            = 0.0f;
    private State _state            = State.SPAWNING;

    public void Setup(PlayerController owner, Uppskeruvel gun)
    {
        this._owner   = owner;
        this._gun     = gun;
        this._sprite  = base.GetComponent<tk2dSprite>();
        this._basePos = base.transform.position;
        this._timer   = _SPAWN_TIME;
        this._jiggle  = UnityEngine.Random.Range(-30f,30f);
        this._sprite.SetAlphaImmediate(0.0f);
        this._setup   = true;
    }

    private Vector2 BehindPlayer()
    {
        this.index ??= this._gun.GetNextAvailableIndex();
        return this._owner.CenterPosition + (this._owner.m_currentGunAngle + 180f + this._jiggle).Clamp360().ToVector(1f + _SPACING * this.index.Value);
    }

    private void GlideTowardsTarget()
    {
        // Get a point between our current position and target
        Vector2 halfDist = Vector2.Lerp(this._basePos, this._targetPos, 0.9f) - this._basePos.XY();
        if (halfDist.magnitude < C.PIXEL_SIZE)
            this._basePos = this._targetPos; // snap immediately
        else
            this._basePos += (BraveTime.DeltaTime / _HALF_TIME) * halfDist.ToVector3ZUp();
    }

    private void HomeTowardsTarget()
    {
        this._velocity = this._basePos.XY().LerpNaturalAndDirectVelocity(
            target          : this._targetPos,
            naturalVelocity : this._velocity,
            accel           : _ACCEL_SEC * BraveTime.DeltaTime,
            lerpFactor      : 0.1f);
        this._basePos += this._velocity.ToVector3ZUp();
    }

    private void Update()
    {
        if (!this._setup)
            return;

        this._timer -= BraveTime.DeltaTime;

        switch(this._state)
        {
            case State.SPAWNING:
                this._sprite.SetAlpha(1f - this._timer / _SPAWN_TIME);
                this._targetPos = BehindPlayer();
                if (this._timer <= 0)
                    this._state = State.FOLLOWING;
                GlideTowardsTarget();
                break;
            case State.FOLLOWING:
                this._targetPos = BehindPlayer();
                GlideTowardsTarget();
                break;
            case State.SEEKING:
                if (!(this._enemy?.healthHaver?.IsAlive ?? false))
                {
                    this._timer = _VANISH_TIME;
                    this._state = State.COOLDOWN;
                    break;
                }
                this._targetPos = this._enemy.CenterPosition;
                HomeTowardsTarget();
                if ((this._basePos - this._targetPos).sqrMagnitude < 1f)
                {
                    this._enemy?.healthHaver?.ApplyDamage(10f, this._velocity, "Uppskeruvel Soul", CoreDamageTypes.None, DamageCategory.Collision);
                    this._timer = _VANISH_TIME;
                    this._state = State.COOLDOWN;
                }
                break;
            case State.COOLDOWN:
                this._sprite.SetAlpha(this._timer / _VANISH_TIME);
                if (this._timer <= 0)
                {
                    this._targetPos = BehindPlayer();
                    this._jiggle    = UnityEngine.Random.Range(-30f,30f);
                    this._basePos   = this._targetPos;
                    this._timer     = _SPAWN_TIME;
                    this._state     = State.SPAWNING;
                }
                break;
        }

        base.transform.position = new Vector2(this._basePos.x,
            this._basePos.y + UppskeruvelLostSoul._BOB_HEIGHT * Mathf.Sin(UppskeruvelLostSoul._BOB_SPEED * this._lifetime)).ToVector3ZisY();
    }

    public bool CanLaunch()
    {
        return this._state == State.FOLLOWING;
    }

    public void Launch(AIActor enemy)
    {
        this._enemy = enemy;
        this._state = State.SEEKING;
        AkSoundEngine.PostEvent("soul_launch_sound", base.gameObject);
    }
}
