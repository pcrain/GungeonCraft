namespace CwaffingTheGungy;

public class Uppskeruvel : CwaffGun
{
    public static string ItemName         = "Uppskeruvel";
    public static string ShortDescription = "Aimless Souls";
    public static string LongDescription  = "Enemies killed with this gun drop soul fragments proportional to their max health. Collecting these fragments spawns Aimless Souls that will attack enemies shot by this gun. New souls are spawned after collecting 10, 30, 60, 100, 150, etc. fragments.";
    public static string Lore             = "The Gungeon has claimed its share of lives from Gungeoneers and Gundead alike. With a few notable exceptions, their souls wander the Gungeon aimlessly, yearning to bear arms once more. The Uppskeruvel calls out to these Aimless Souls and gives them purpose, transforming them into the projectiles they were always meant to be.\n\n\"By the sweat of your brow you will fire your weapon until your last projectile falls to the ground from whence you were both taken; for gunpowder you are and to gunpowder you will return.\" ~ Gunesis 3:19";

    internal const int _MAX_SOULS           = 40; // the max souls we can have following us at once
    internal const int _MAX_DROPS           = 500; // the max souls that can be dropped by a single enemy
    internal const float _DAMAGE_PER_SOUL   = 8f;
    internal const float _SOUL_LAUNCH_SPEED = 7f;
    internal const float _SOULS_PER_HEALTH  = 0.1f;
    internal const float _SPAWN_DELAY       = 0.1f;
    internal const string _SoulSpriteUI     = $"{C.MOD_PREFIX}:_SoulSpriteUI";  // need the string immediately for preloading in Main()

    internal static int        _UppskeruvelId              = -1;
    internal static GameObject _LostSoulPrefab             = null;
    internal static GameObject _CombatSoulPrefab           = null;
    internal static GameObject _SoulCollectVFX             = null;
    internal static GameObject _SoulExplodePrefab          = null;
    internal static SpriteTrailController _SoulTrailPrefab = null;

    private static int[] _LevelThresholds = new int[_MAX_SOULS];

    public int souls = 0;

    private int _level = 0;
    private int _nextLevelThreshold = 0;
    private bool _spawningSouls = false;
    private List<UppskeruvelCombatSoul> _extantSouls = new();
    private List<int> _usedIndices = new();
    private UppskeruvelCombatSoul[] _soulTracker = Enumerable.Repeat((UppskeruvelCombatSoul)null, _MAX_SOULS).ToArray();

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Uppskeruvel>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.A, gunClass: GunClass.CHARM, reloadTime: 1.25f, ammo: 400, shootFps: 24, reloadFps: 30,
                muzzleVFX: "muzzle_uppskeruvel", muzzleFps: 60, muzzleScale: 0.2f, muzzleAnchor: Anchor.MiddleCenter,
                fireAudio: "uppskeruvel_fire_sound");
            gun.SetReloadAudio("uppskeruvel_reload_sound", 4, 22);

        gun.InitProjectile(GunData.New(clipSize: 12, cooldown: 0.18f, shootStyle: ShootStyle.Automatic, damage: 4f, customClip: true,
          sprite: "uppskeruvel_projectile", fps: 12, anchor: Anchor.MiddleLeft)).Attach<UppskeruvelProjectile>();

        gun.gameObject.AddComponent<UppskeruvelAmmoDisplay>();

        _SoulTrailPrefab = VFX.CreateSpriteTrailObject("uppskeruvel_soul_trail", fps: 60, cascadeTimer: 4f * C.FRAME, softMaxLength: 2f, destroyOnEmpty: false);

        _LostSoulPrefab = VFX.Create("poe_soul", fps: 8, loops: true, anchor: Anchor.LowerCenter/*, emissivePower: 0.4f*/);
            _LostSoulPrefab.AddComponent<UppskeruvelLostSoul>();

        _CombatSoulPrefab = VFX.Create("large_poe_soul", fps: 8, loops: true, anchor: Anchor.MiddleCenter, scale: 2.0f/*, emissivePower: 0.4f*/);
            _CombatSoulPrefab.AddComponent<UppskeruvelCombatSoul>();

        _SoulExplodePrefab = VFX.Create("soul_explode", fps: 32, loops: false, anchor: Anchor.MiddleCenter);

        _SoulCollectVFX = VFX.Create("soul_collect", 16, loops: true, anchor: Anchor.MiddleCenter);

        // level ups at 10, 30, 60, 100, 150, etc. souls
        for (int i = 0; i < _MAX_SOULS; ++i)
            _LevelThresholds[i] = 5 * (i*i+i);

        _UppskeruvelId = gun.PickupObjectId;
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        GameManager.Instance.OnNewLevelFullyLoaded += this.OnNewFloor;
        StartCoroutine(SpawnSoulsOnceWeCanMove());
        base.OnPlayerPickup(player);
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
        base.OnDroppedByPlayer(player);
        StopAllCoroutines();
        this._spawningSouls = false;
        DestroyExtantCombatSouls();
    }

    public override void OnDestroy()
    {
        GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
        StopAllCoroutines();
        DestroyExtantCombatSouls();
        base.OnDestroy();
    }

    public void AcquireSoul(int n = 1)
    {
        this.souls += n;
        RecalculateLevel();
        for (int i = this._extantSouls.Count; i <= this._level; ++i)
            SpawnCombatSoul();
    }

    private void RecalculateLevel()
    {
        while (this.souls >= _LevelThresholds[this._level + 1])
            ++this._level;
    }

    private void OnNewFloor()
    {
        if (!this)
            return;
        DestroyExtantCombatSouls();
        StartCoroutine(SpawnSoulsOnceWeCanMove());
    }

    private IEnumerator SpawnSoulsOnceWeCanMove()
    {
        if (this._spawningSouls)
            yield break;
        this._spawningSouls = true;
        while (this.PlayerOwner)
        {
            if (this.PlayerOwner.AcceptingNonMotionInput)
                break;
            yield return null;
        }
        if (!this.PlayerOwner)
            yield break;

        RecalculateLevel();
        for (int i = this._extantSouls.Count; i <= this._level; ++i)
        {
            SpawnCombatSoul();
            yield return new WaitForSeconds(_SPAWN_DELAY);
        }
        this._spawningSouls = false;
    }

    private void DestroyExtantCombatSouls()
    {
        foreach (int index in this._usedIndices)
            this._soulTracker[index] = null;
        this._usedIndices.Clear();

        foreach (UppskeruvelCombatSoul soul in this._extantSouls)
            if (soul)
                soul.Despawn();
        this._extantSouls.Clear();
    }

    public int GetNextAvailableIndex(UppskeruvelCombatSoul soul)
    {
        for (int i = 0; i < _MAX_SOULS; ++i)
        {
            if (this._usedIndices.Contains(i))
                continue;

            this._usedIndices.Add(i);
            this._soulTracker[i] = soul;
            return i;
        }
        ETGModConsole.Log($"  GetNextIndex() FAILED, THIS SHOULD NEVER HAPPEN");
        return -1;
    }

    public void LaunchAvailableSouls(AIActor enemy)
    {
        // Launch enough souls to kill the enemy, or all of them if we don't have enough
        int soulsToLaunch = Mathf.Min(Mathf.CeilToInt(enemy.healthHaver.currentHealth / _DAMAGE_PER_SOUL), this._usedIndices.Count);
        if (soulsToLaunch == 0)
            return;

        int maxIndex = this._usedIndices.Max();
        int soulsLaunched = 0;
        for (int i = 0; i <= maxIndex; ++i)
        {
            if (this._soulTracker[i] is not UppskeruvelCombatSoul soul)
                continue;
            if (!soul.CanLaunch() || !soul.index.HasValue)
                continue;

            LaunchCombatSoul(soul, enemy, ++soulsLaunched);
            if (soulsLaunched == soulsToLaunch)
                break;
        }
    }

    private void LaunchCombatSoul(UppskeruvelCombatSoul soul, AIActor enemy, int order)
    {
        int i = soul.index.Value;
        this._soulTracker[i] = null;
        this._usedIndices.Remove(i);
        soul.Launch(enemy, order);
    }

    private UppskeruvelCombatSoul SpawnCombatSoul()
    {
        UppskeruvelCombatSoul soul = Uppskeruvel._CombatSoulPrefab.Instantiate(this.PlayerOwner.CenterPosition).GetComponent<UppskeruvelCombatSoul>();
        this._extantSouls.Add(soul);
        soul.Setup(this.PlayerOwner, this);
        return soul;
    }

    public static void DropLostSouls(AIActor enemy, bool hasSoulSearchingSynergy = false)
    {
        Vector2 ppos = enemy.CenterPosition;
        int soulsToSpawn = Mathf.Min(_MAX_DROPS, enemy.healthHaver ? Mathf.CeilToInt(_SOULS_PER_HEALTH * enemy.healthHaver.GetMaxHealth()) : 0);
        if (hasSoulSearchingSynergy)
            soulsToSpawn *= 2;
        for (int i = 0; i < soulsToSpawn; ++i)
        {
            float angle = Lazy.RandomAngle();
            Vector2 finalPos = ppos + BraveMathCollege.DegreesToVector(angle);
            Uppskeruvel._LostSoulPrefab.Instantiate(finalPos).GetComponent<UppskeruvelLostSoul>(
                ).Setup(angle.ToVector(_SOUL_LAUNCH_SPEED * UnityEngine.Random.Range(0.8f, 1.2f)));
        }
    }

    public override void MidGameSerialize(List<object> data, int i)
    {
        base.MidGameSerialize(data, i);
        data.Add(this.souls);
    }

    public override void MidGameDeserialize(List<object> data, ref int i)
    {
        base.MidGameDeserialize(data, ref i);
        this.souls = (int)data[i++];
        RecalculateLevel();
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
        uic.GunAmmoCountLabel.AutoHeight = true; // enable multiline text
        uic.GunAmmoCountLabel.ProcessMarkup = true; // enable multicolor text
        uic.GunAmmoCountLabel.Text = $"[sprite \"{Uppskeruvel._SoulSpriteUI}\"][color #6666dd]x{this._uppies.souls}[/color]\n{this._gun.CurrentAmmo}/{this._gun.AdjustedMaxAmmo}";
        return true;
    }
}

public class UppskeruvelProjectile : MonoBehaviour
{
    private Projectile _projectile = null;
    private PlayerController _owner = null;
    private Uppskeruvel _gun = null;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        if (!this._owner || !this._owner.CurrentGun || this._owner.CurrentGun.GetComponent<Uppskeruvel>() is not Uppskeruvel uppies)
            return;

        this._gun = uppies;
        this._projectile.OnWillKillEnemy += this.OnWillKillEnemy;
        this._projectile.OnHitEnemy += this.OnHitEnemy;
    }

    private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool killed)
    {
        if (!enemy || !enemy.aiActor || killed)
            return;
        this._gun.LaunchAvailableSouls(enemy.aiActor);
    }

    private void OnWillKillEnemy(Projectile bullet, SpeculativeRigidbody enemy)
    {
        if (!enemy || !enemy.aiActor || !enemy.aiActor.IsHostile())
            return; // avoid processing effect for non-hostile enemies

        Uppskeruvel.DropLostSouls(enemy.aiActor, this._owner.PlayerHasActiveSynergy(Synergy.SOUL_SEARCHING));
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
    const float _MAX_LIFE           = 10f;  // time before despawning

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

        if (GameManager.Instance.PrimaryPlayer.AcceptingAnyInput)
            this._lifetime += BraveTime.DeltaTime; // don't increase life timer unless player can move

        if (this._owner)
        {
            Vector2 delta = (this._owner.CenterPosition - base.transform.position.XY());
            Vector2 deltaNorm = delta.normalized;
            this._homeSpeed += _HOME_ACCEL * BraveTime.DeltaTime;
            // Weighted average of natural and direct velocity towards player
            this._velocity = this._homeSpeed * Lazy.SmoothestLerp((this._velocity.normalized + deltaNorm).normalized, deltaNorm, 10f);
            this._basePos += (this._velocity * BraveTime.DeltaTime).ToVector3ZUp();
            base.transform.position = this._basePos.HoverAt(amplitude: _BOB_HEIGHT, frequency: _BOB_SPEED);

            FancyVFX.Spawn(Outbreak._OutbreakSmokeVFX, base.transform.position, Lazy.RandomEulerZ(),
                velocity: Lazy.RandomVector(0.1f), lifetime: 0.25f, fadeOutTime: 0.5f);

            if (delta.sqrMagnitude > _PICKUP_RADIUS_SQR)
                return;

            if (this._owner.FindGun<Uppskeruvel>() is Uppskeruvel upp)
                upp.AcquireSoul();
            base.gameObject.PlayUnique("pickup_poe_soul_sound");
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
        base.transform.position = this._basePos.HoverAt(amplitude: _BOB_HEIGHT, frequency: _BOB_SPEED);

        if (this._lifetime > _MAX_LIFE)
        {
            FancyVFX.SpawnBurst(prefab: Outbreak._OutbreakSmokeVFX, numToSpawn: 4, basePosition: base.transform.position,
                baseVelocity: Vector2.zero, velocityVariance: 0.5f, velType: FancyVFX.Vel.Radial, rotType: FancyVFX.Rot.Random,
                lifetime: 0.3f, fadeOutTime: 0.6f);
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
        PRELAUNCH,  // queued for launch after hitting an enemy with a projectile
        SEEKING,    // seeking an enemy that has been recently shot by a projectile
        VANISH,     // vanish after hitting an enemy
        COOLDOWN,   // on cooldown after vanishing but before reappearing
        DESPAWNING, // being destroyed at the end of a level or when the original owner no longer exists
    }

    public int? index                = null;

    private const float _SPAWN_TIME  = 0.5f;
    private const float _VANISH_TIME = 0.25f;
    private const float _LAUNCH_GAP  = 0.1f;
    private const float _COOLDOWN    = 3.0f;
    private const float _ACCEL_SEC   = 0.5f;
    private const float _HALF_TIME   = 0.1f;  // in GlideTowardsTarget mode, time required to get halfway to our target
    private const float _SPACING     = 0.6f;  // spacing between followers

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
    private SpriteTrailController _trail = null;

    public void Setup(PlayerController owner, Uppskeruvel gun)
    {
        this._owner   = owner;
        this._gun     = gun;
        this._sprite  = base.GetComponent<tk2dSprite>();
        this._basePos = base.transform.position;
        this._timer   = _SPAWN_TIME;
        this._jiggle  = UnityEngine.Random.Range(-30f,30f);
        this._sprite.SetAlphaImmediate(0.0f);
        this._trail = this._sprite.AddTrailToSpriteInstance(Uppskeruvel._SoulTrailPrefab);
        base.gameObject.Play("soul_spawn_sound");
        this._setup   = true;
    }

    public void Reassign(PlayerController newOwner)
    {
        this._owner = newOwner;
    }

    public void Despawn()
    {
        if (this._state == State.DESPAWNING)
            return;
        this._timer = _VANISH_TIME;
        this._state = State.DESPAWNING;
    }

    private void OnDestroy()
    {
        this._trail.SafeDestroy();
    }

    private Vector2 BehindPlayer()
    {
        this.index ??= this._gun.GetNextAvailableIndex(this);
        return this._owner.CenterPosition + (this._owner.m_currentGunAngle + 180f + this._jiggle).Clamp360().ToVector(1f + _SPACING * this.index.Value);
    }

    private void GlideTowardsTarget()
    {
        const float SQR_PIXEL = C.PIXEL_SIZE * C.PIXEL_SIZE;
        if ((this._targetPos - this._basePos).sqrMagnitude < SQR_PIXEL)
            this._basePos = this._targetPos; // snap immediately if within a pixel of our target
        else
            this._basePos = Lazy.SmoothestLerp(this._basePos, this._targetPos, 10f);
    }

    private void HomeTowardsTarget()
    {
        this._velocity = this._basePos.XY().LerpDirectAndNaturalVelocity(
            target          : this._targetPos,
            naturalVelocity : this._velocity,
            accel           : _ACCEL_SEC * BraveTime.DeltaTime,
            lerpFactor      : 1f);
        this._basePos += this._velocity.ToVector3ZUp();
    }

    public bool CanLaunch()
    {
        return this._state == State.FOLLOWING;
    }

    public void Launch(AIActor enemy, int order)
    {
        this.index = null;
        this._enemy = enemy;
        this._state = State.PRELAUNCH;
        this._timer = _LAUNCH_GAP * order;
        base.gameObject.Play("soul_launch_sound");
    }

    private void Update()
    {
        if (!this._setup)
            return;
        if (BraveTime.DeltaTime == 0)
            return; // don't do anything if we're paused

        this._lifetime += BraveTime.DeltaTime;
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
            case State.PRELAUNCH:
                if (!this._enemy || !this._enemy.healthHaver || !this._enemy.healthHaver.IsAlive)
                {
                    this._state = State.FOLLOWING; // if we lost our target before getting to it, revert to following
                    break;
                }
                if (this._timer <= 0)
                {
                    this._state = State.SEEKING;
                    break;
                }
                this._targetPos = BehindPlayer();
                GlideTowardsTarget();
                break;
            case State.SEEKING:
                if (!this._enemy || !this._enemy.healthHaver || !this._enemy.healthHaver.IsAlive)
                {
                    this._state = State.FOLLOWING; // if we lost our target before getting to it, revert to following
                    break;
                }
                this._targetPos = this._enemy.CenterPosition;
                HomeTowardsTarget();
                if ((this._basePos - this._targetPos).sqrMagnitude < 2f)
                {
                    this._enemy.healthHaver.ApplyDamage(Uppskeruvel._DAMAGE_PER_SOUL, this._velocity, "Uppskeruvel Soul", CoreDamageTypes.None, DamageCategory.Collision);
                    if (this._enemy.healthHaver.IsDead)
                        Uppskeruvel.DropLostSouls(this._enemy);
                    SpawnManager.SpawnVFX(Uppskeruvel._SoulExplodePrefab, this._targetPos.XY() + Lazy.RandomVector(0.4f), Quaternion.identity);
                    base.gameObject.PlayUnique("soul_impact_sound");
                    this._trail.Toggle(false);
                    this._timer = _VANISH_TIME;
                    this._state = State.VANISH;
                }
                break;
            case State.VANISH:
                this._sprite.SetAlpha(this._timer / _VANISH_TIME);
                if (this._timer <= 0)
                {
                    this._timer     = _COOLDOWN;
                    this._state     = State.COOLDOWN;
                }
                break;
            case State.COOLDOWN:
                if (this._timer <= 0)
                {
                    this._targetPos = BehindPlayer();
                    this._jiggle    = UnityEngine.Random.Range(-60f, 60f);  // pick a random angle roughly behind the player
                    this._basePos   = this._targetPos;
                    this._trail.Toggle(true);
                    this._timer     = _SPAWN_TIME;
                    this._state     = State.SPAWNING;
                    base.gameObject.Play("soul_spawn_sound");
                    this._sprite.renderer.enabled = true;
                }
                else
                    this._sprite.renderer.enabled = false;
                break;
            case State.DESPAWNING:
                this._sprite.SetAlpha(this._timer / _VANISH_TIME);
                if (this._timer <= 0)
                    UnityEngine.Object.Destroy(base.gameObject);
                break;
        }

        base.transform.position = this._basePos.HoverAt(amplitude: UppskeruvelLostSoul._BOB_HEIGHT, frequency: UppskeruvelLostSoul._BOB_SPEED);
    }
}
