namespace CwaffingTheGungy;

using static PigmentType;

/* TODO:
    - color switcher ground rune

    - pigment ammo display
    - enemy shaders

    - enemy pigment calculations
    - pigment damage calculations

    - beam impact animations
    - beam sounds
    - gun animations
    - lore
*/

public class Chroma : CwaffGun
{
    public static string ItemName         = "Chroma";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const float _PARTICLE_RATE = 0.05f;

    internal static GameObject _PigmentPrefab = null;

    public int redPigment   = 0;
    public int greenPigment = 0;
    public int bluePigment  = 0;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Chroma>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.BEAM, reloadTime: 0.0f, ammo: 600, shootFps: 4, modulesAreTiers: true);

        gun.InitProjectile(GunData.New(baseProjectile: Items.Moonscraper.Projectile(), clipSize: -1, cooldown: 0.18f, //NOTE: inherit from Moonscraper for hitscan
            shootStyle: ShootStyle.Beam, damage: 100f, speed: -1f, /*customClip: true, */ammoCost: 5, angleVariance: 0f,
            beamSprite: "chroma_beam", beamFps: 60, beamChargeFps: 8, beamImpactFps: 14,
            beamLoopCharge: false, beamReflections: 0, beamChargeDelay: 0f, beamEmission: 1500f))
          .Attach<ChromaProjectile>();

        //NOTE: dispersal doesn't work with instant beams (since they have no bones)
        for (int i = 0; i < 3; ++i)
        {
            ProjectileModule mod = i > 0 ? gun.DuplicateDefaultModule() : gun.DefaultModule;
            Projectile p = mod.projectiles[0];
            BasicBeamController beamComp = p.gameObject.GetComponent<BasicBeamController>();
            Color c = PigmentDrop._Primaries[i];
            Material mat = beamComp.sprite.renderer.material;
            mat.DisableKeyword("BRIGHTNESS_CLAMP_ON");
            mat.EnableKeyword("BRIGHTNESS_CLAMP_OFF");
            mat.SetFloat("_EmissiveColorPower", 8f);
            mat.SetColor("_EmissiveColor", c);
            mat.SetColor("_OverrideColor", Color.Lerp(c, Color.white, 0.5f));
        }

        _PigmentPrefab = VFX.Create("pigment_ball_white_small").Attach<PigmentDrop>();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        //
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        if (!manualReload || !player.AcceptingNonMotionInput || gun.IsFiring)
            return;
        this.gun.CurrentStrengthTier = (this.gun.CurrentStrengthTier + 1) % 3;
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        //
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        //
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        //
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        //
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
        {
            //
        }
        base.OnDestroy();
    }

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner || !this.PlayerOwner.AcceptingNonMotionInput)
            return;

        if (GetExtantBeam() is BasicBeamController beam && beam.State == BeamState.Firing)
            UpdateParticles(beam);
    }

    private float _lastParticleSpawn = 0.0f;
    private void UpdateParticles(BasicBeamController beam)
    {
        float now = BraveTime.ScaledTimeSinceStartup;
        if ((now - this._lastParticleSpawn) < _PARTICLE_RATE)
            return;

        this._lastParticleSpawn = now;

        Vector2 barrelPos = this.gun.barrelOffset.position;
        Vector2 deltaNorm = beam.Direction.normalized;
        float mag = beam.m_currentBeamDistance;

        for (int i = 0; i < (int)mag; ++i)
            CwaffVFX.Spawn(
                prefab         : VFX.SinglePixel,
                position       : barrelPos + (mag * UnityEngine.Random.value) * deltaNorm,
                velocity       : Lazy.RandomVector(4f),
                lifetime       : 0.4f,
                emissivePower  : 2000f,
                overrideColor  : PigmentDrop._Primaries[this.gun.CurrentStrengthTier],
                emitColorPower : 8f
              );
    }

    public static void DropPigment(AIActor enemy)
    {
        Vector2 ppos = enemy.CenterPosition;
        // TODO: actual per-enemy pigment calculations
        // int red      = 1;
        // int green    = 1;
        // int blue     = 1;
        for (int i = 0; i < 3; ++i)
        {
            int redGreenOrBlue = i;
            float angle = Lazy.RandomAngle();
            Vector2 finalPos = ppos + BraveMathCollege.DegreesToVector(angle);
            Chroma._PigmentPrefab.Instantiate(finalPos).GetComponent<PigmentDrop>()
              .Setup(
                velocity : angle.ToVector(Uppskeruvel._SOUL_LAUNCH_SPEED * UnityEngine.Random.Range(0.8f, 1.2f)),
                pigment  : (PigmentType)i);
        }
    }

    public void AcquirePigment()
    {
        // ETGModConsole.Log($"pigment obtained!");
    }
}

public enum PigmentType
{
    RED,
    GREEN,
    BLUE
}

public class ChromaProjectile : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private Chroma _gun;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        if (!this._owner)
            return;

        if (this._owner.CurrentGun is Gun gun)
            this._gun = gun.gameObject.GetComponent<Chroma>();

        this._projectile.OnHitEnemy += this.OnMightKillEnemy;
    }

    private void Update()
    {
      // enter update code here
    }

    private void OnDestroy()
    {
      // enter destroy code here
    }

    private void OnMightKillEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool willKill)
    {
        if (willKill && enemy && enemy.aiActor)
            Chroma.DropPigment(enemy.aiActor);
    }
}

public class PigmentDrop : MonoBehaviour
{
    internal const float _BOB_SPEED  = 4f;
    internal const float _BOB_HEIGHT = 0.20f;

    const float _ATTRACT_RADIUS_SQR = 25f;  // range before we start homing in on player
    const float _PICKUP_RADIUS_SQR  = 2f;   // range before we are picked up by player
    const float _HOME_ACCEL         = 44f;  // acceleration per second towards player
    const float _FRICTION           = 0.96f;
    const float _MAX_LIFE           = 10f;  // time before despawning
    const float _PARTICLE_RATE      = 0.1f;

    internal static readonly List<Color> _Primaries = [
        new(230f / 255f, 87f   / 255f, 149f / 255f), //red
        new(149f / 255f, 230f / 255f, 87f  / 255f), //green
        new( 90f / 255f, 180f / 255f, 230f / 255f), //blue
    ];

    internal static int _ChromaId = -1;

    private bool _setup             = false;
    private PlayerController _owner = null;
    private float _homeSpeed        = 0.0f;
    private tk2dSprite _sprite      = null;
    private float _lifetime         = 0.0f;
    private Vector2 _velocity       = Vector2.zero;
    private Vector3 _basePos        = Vector2.zero;
    private float _lastParticle     = 0.0f;
    private PigmentType _pigment;
    private Color _pigmentColor;

    public void Setup(Vector2 velocity, PigmentType pigment)
    {
        Color color = _Primaries[(int)pigment];
        this._pigment = pigment;
        this._pigmentColor = color;
        this._sprite = base.GetComponent<tk2dSprite>();
        this._sprite.SetGlowiness(400f, glowColor: this._pigmentColor, overrideColor: Color.Lerp(this._pigmentColor, Color.white, 0.5f));
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

        float now = BraveTime.ScaledTimeSinceStartup;
        if ((now - this._lastParticle) >= _PARTICLE_RATE)
        {
            this._lastParticle = now;
            CwaffVFX.Spawn(
                prefab        : VFX.SinglePixel,
                position      : base.transform.position,
                velocity      : Lazy.RandomVector(3f),
                lifetime      : 0.5f,
                emissivePower : 3000f,
                overrideColor : this._pigmentColor,
                emitColorPower : 8f
              );
        }

        if (this._owner)
        {
            Vector2 delta = (this._owner.CenterPosition - base.transform.position.XY());
            Vector2 deltaNorm = delta.normalized;
            this._homeSpeed += _HOME_ACCEL * BraveTime.DeltaTime;
            // Weighted average of natural and direct velocity towards player
            this._velocity = this._homeSpeed * Lazy.SmoothestLerp((this._velocity.normalized + deltaNorm).normalized, deltaNorm, 10f);
            this._basePos += (this._velocity * BraveTime.DeltaTime).ToVector3ZUp();
            base.transform.position = this._basePos.HoverAt(amplitude: _BOB_HEIGHT, frequency: _BOB_SPEED);

            if (delta.sqrMagnitude > _PICKUP_RADIUS_SQR)
                return;

            if (this._owner.FindGun<Chroma>() is Chroma chroma)
                chroma.AcquirePigment();
            base.gameObject.PlayUnique("pigment_collect_sound");
            CwaffVFX.SpawnBurst(
                prefab           : VFX.SinglePixel,
                numToSpawn       : 4,
                basePosition     : this._owner.CenterPosition,
                velocityVariance : 5f,
                velType          : CwaffVFX.Vel.Random,
                lifetime         : 0.5f,
                emissivePower    : 3000f, //1000 green
                overrideColor    : this._pigmentColor,
                emitColorPower   : 8f
              );
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

        // TODO: dissipate when leaving room
        // if (this._lifetime > _MAX_LIFE)
        // {
        //     CwaffVFX.SpawnBurst(prefab: Outbreak._OutbreakSmokeVFX, numToSpawn: 4, basePosition: base.transform.position,
        //         baseVelocity: Vector2.zero, velocityVariance: 0.5f, velType: CwaffVFX.Vel.Radial, rotType: CwaffVFX.Rot.Random,
        //         lifetime: 0.3f, fadeOutTime: 0.6f);
        //     UnityEngine.Object.Destroy(base.gameObject);
        //     return;
        // }

        if (_ChromaId < 0)
            _ChromaId = Lazy.PickupId<Chroma>();
        foreach (PlayerController player in GameManager.Instance.AllPlayers)
        {
            if (!player || !player.isActiveAndEnabled || player.IsGhost)
                continue;
            if ((base.transform.position.XY() - player.CenterPosition).sqrMagnitude > _ATTRACT_RADIUS_SQR)
                continue;
            if (!player.HasGun(_ChromaId))
                continue;
            this._owner = player;
        }
    }
}
