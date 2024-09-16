namespace CwaffingTheGungy;

using System;
using static PigmentType;

/* TODO:
    - color switcher ground rune

    - pigment ammo display

    - pigment damage calculations
    - pigment drop calculations

    - fix hollowpoint not working
    - tint corpses
    - save serialization
    - beam impact animations
    - gun animations
    - lore
*/

public class Chroma : CwaffGun
{
    public static string ItemName         = "Chroma";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const float _BASE_PARTICLE_RATE = 0.3f;
    private const float _PER_LEVEL_PARTICLE_RATE = 0.03f;

    internal static GameObject _PigmentPrefab = null;

    public int redPigment   = 0;
    public int greenPigment = 0;
    public int bluePigment  = 0;

    private readonly int[] _pigmentPowers = [0, 0, 0];
    private bool _ammoDisplayDirty = true;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Chroma>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.BEAM, reloadTime: 0.0f, ammo: 600, shootFps: 4, modulesAreTiers: true)
          .Attach<ChromaAmmoDisplay>();

        gun.InitProjectile(GunData.New(baseProjectile: Items.Moonscraper.Projectile(), clipSize: -1, cooldown: 0.18f, //NOTE: inherit from Moonscraper for hitscan
            shootStyle: ShootStyle.Beam, damage: 10f, speed: -1f, /*customClip: true, */ammoCost: 5, angleVariance: 0f,
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

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);

        foreach (AIActor enemy in StaticReferenceManager.AllEnemies)
            OnEnemySpawn(enemy);
        ETGMod.AIActor.OnPreStart -= OnEnemySpawn;
        ETGMod.AIActor.OnPreStart += OnEnemySpawn;
    }

    private static void OnEnemySpawn(AIActor enemy)
    {
        enemy.gameObject.GetOrAddComponent<Desaturator>();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        ETGMod.AIActor.OnPreStart -= OnEnemySpawn;
    }

    public override void OnDestroy()
    {
        ETGMod.AIActor.OnPreStart -= OnEnemySpawn;
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

        this.gun.LoopSoundIf(this.gun.IsFiring, "chroma_fire_sound");
        if (this.gun.IsFiring && GetExtantBeam() is BasicBeamController beam && beam.State == BeamState.Firing)
            UpdateParticles(beam);
    }

    private float _lastParticleSpawn = 0.0f;
    private void UpdateParticles(BasicBeamController beam)
    {
        float now = BraveTime.ScaledTimeSinceStartup;
        float particleSpawnRate = (_BASE_PARTICLE_RATE - _PER_LEVEL_PARTICLE_RATE * this._pigmentPowers[this.gun.CurrentStrengthTier]);
        if ((now - this._lastParticleSpawn) < particleSpawnRate)
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

    public static void DropPigment(AIActor enemy, PigmentType color)
    {
        Vector2 ppos = enemy.CenterPosition;
        float angle = Lazy.RandomAngle();
        Vector2 finalPos = ppos + BraveMathCollege.DegreesToVector(angle);
        Chroma._PigmentPrefab.Instantiate(finalPos).GetComponent<PigmentDrop>()
          .Setup(
            velocity : angle.ToVector(Uppskeruvel._SOUL_LAUNCH_SPEED * UnityEngine.Random.Range(0.8f, 1.2f)),
            pigment  : color);
    }

    public static int PigmentPower(int pigmentNum) => 1 + (int)Mathf.Max(0, Mathf.Log(Mathf.Max(1, pigmentNum), 2) - 3);

    public void AcquirePigment(PigmentType pigment)
    {
        if (pigment == RED)
            this._pigmentPowers[0] = PigmentPower(++this.redPigment);
        else if (pigment == GREEN)
            this._pigmentPowers[1] = PigmentPower(++this.greenPigment);
        else if (pigment == BLUE)
            this._pigmentPowers[2] = PigmentPower(++this.bluePigment);
        this._ammoDisplayDirty = true;
    }

    public override void PostProcessBeam(BeamController beam)
    {
        const float _BOOST_PER_LEVEL = 0.2f;

        if (beam.projectile is not Projectile projectile)
            return;
        if (this.PlayerOwner is not PlayerController pc)
            return;

        //NOTE: this won't update until after firing a new beam
        int level = this._pigmentPowers[this.gun.CurrentStrengthTier];
        #if DEBUG
            string color = this.gun.CurrentStrengthTier switch {
                0 => "red",
                1 => "green",
                _ => "blue"
            };
            ETGModConsole.Log($"firing with level {level} {color} pigment == {_BOOST_PER_LEVEL * level} boost");
        #endif
        projectile.baseData.damage *= (1f + _BOOST_PER_LEVEL * level);
    }

    private class ChromaAmmoDisplay : CustomAmmoDisplay
    {
        private Gun _gun;
        private Chroma _chroma;
        private PlayerController _owner;
        private string _ammoText = "";

        private void Start()
        {
            this._gun = base.GetComponent<Gun>();
            this._chroma = this._gun.GetComponent<Chroma>();
            this._owner = this._gun.CurrentOwner as PlayerController;
        }

        public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
        {
            if (!this._owner)
                return false;

            uic.SetAmmoCountLabelColor(Color.white);
            uic.GunAmmoCountLabel.AutoHeight = true; // enable multiline text
            uic.GunAmmoCountLabel.ProcessMarkup = true; // enable multicolor text
            if (this._chroma._ammoDisplayDirty)
            {
                this._ammoText = $"[color #dd6666]{this._chroma._pigmentPowers[0]}[/color] [color #66dd66]{this._chroma._pigmentPowers[1]}[/color] [color #6666dd]{this._chroma._pigmentPowers[2]}[/color]";
                this._chroma._ammoDisplayDirty = false;
            }
            uic.GunAmmoCountLabel.Text = $"{this._ammoText}\n{this._owner.VanillaAmmoDisplay()}";
            return true;
        }
    }
}

public enum PigmentType
{
    RED,
    GREEN,
    BLUE
}

public class Desaturator : MonoBehaviour
{
    private static readonly Dictionary<string, IntVector3> _PigmentLookupDict = new();

    private AIActor _enemy;
    private float _lastKnownHealth = -1.0f;
    private float _saturation = 1.0f;
    private bool _didSetup = false;
    private bool _addedShader = false;
    private List<Material> _desatMats = new();
    private bool _gotPigment = false;

    private int _rTotal = 0;
    private int _gTotal = 0;
    private int _bTotal = 0;

    private float _rFrac = 0.0f;
    private float _gFrac = 0.0f;
    private float _bFrac = 0.0f;

    private void Start()
    {
        Setup();
    }

    private void Setup()
    {
        if (this._didSetup)
            return;

        this._didSetup = true;
        if (base.gameObject.GetComponent<AIActor>() is not AIActor enemy)
            return;

        this._enemy = enemy;
        //NOTE: doesn't work
        // if (this._enemy.GetComponent<SpeculativeRigidbody>() is SpeculativeRigidbody body)
        //     body.OnPreRigidbodyCollision += this.OnMightTakeDamage;
    }

    // private void OnMightTakeDamage(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    // {
    //     ETGModConsole.Log($"might take damage");
    //     UpdateLastKnownHealth();
    // }

    private void AddShader(tk2dBaseSprite sprite)
    {
        sprite.usesOverrideMaterial = true;
        Material mat = sprite.renderer.material;
        mat.shader = CwaffShaders.ChromaShader;
        mat.SetFloat("_Saturation", 1f);
        mat.SetFloat("_BandStrength", 0f);
        mat.SetFloat("_HueShift", 0f);
        mat.SetFloat("_EmissivePower", 0f);
        this._desatMats.Add(mat);
        this._addedShader = true;
    }

    internal void UpdateLastKnownHealth()
    {
        Setup();
        if (this._enemy && this._enemy.healthHaver is HealthHaver hh)
            this._lastKnownHealth = hh.currentHealth;
    }

    private static readonly Dictionary<Texture, Texture2D> _ReadableTexes = new();

    private static IntVector3 ComputePigmentForEnemy(string guid)
    {
        const float _PIGMENT_FACTOR = 0.04f; // conversion rate from sprite pigment to actual pigment

        AIActor prefab = EnemyDatabase.GetOrLoadByGuid(guid);
        Lazy.DebugLog($"looking up {prefab.ActorName}");
        if (prefab.gameObject.GetComponent<tk2dSpriteAnimator>() is not tk2dSpriteAnimator animator)
        {
            Lazy.DebugLog($"  no animator");
            return IntVector3.zero;
        }
        if (animator.library is not tk2dSpriteAnimation library)
        {
            Lazy.DebugLog($"  no library");
            return IntVector3.zero;
        }
        if (library.FirstValidClip is not tk2dSpriteAnimationClip clip || clip.frames == null || clip.frames.Length == 0)
        {
            Lazy.DebugLog($"  no clip");
            return IntVector3.zero;
        }
        if (clip.frames[0].spriteCollection.spriteDefinitions[clip.frames[0].spriteId] is not tk2dSpriteDefinition def)
        {
            Lazy.DebugLog($"  no def");
            return IntVector3.zero;
        }
        if (!def.material || def.material.mainTexture is not Texture mainTex)
        {
            Lazy.DebugLog($"  no texture");
            return IntVector3.zero;
        }

        Lazy.DebugLog($"  got def {def.name}");
        float r = 0;
        float g = 0;
        float b = 0;
        if (!_ReadableTexes.TryGetValue(mainTex, out Texture2D tex))
            tex = _ReadableTexes[mainTex] = (mainTex as Texture2D).GetRW();
        int w = tex.width;
        int h = tex.height;
        Color[] pixels = tex.GetPixels(
            x           : Mathf.RoundToInt(def.uvs[0].x * w),
            y           : Mathf.RoundToInt(def.uvs[0].y * h),
            blockWidth  : Mathf.RoundToInt((def.uvs[3].x - def.uvs[0].x) * w),
            blockHeight : Mathf.RoundToInt((def.uvs[3].y - def.uvs[0].y) * h)
            );
        int npixels = pixels.Length;
        int nopaque = 0;
        for (int i = 0; i < npixels; ++i)
        {
            Color pixel = pixels[i];
            if (pixel.a < 0.5f)
                continue;
            r += pixel.r;
            g += pixel.g;
            b += pixel.b;
            ++nopaque;
        }
        float norm = ((float)nopaque) / (r + g + b);
        r *= norm;
        g *= norm;
        b *= norm;

        return new((int)(_PIGMENT_FACTOR * r), (int)(_PIGMENT_FACTOR * g), (int)(_PIGMENT_FACTOR * b));
    }

    internal void DoPigmentChecks()
    {
        if (!this._enemy || this._enemy.healthHaver is not HealthHaver hh || string.IsNullOrEmpty(this._enemy.EnemyGuid))
            return;
        string guid = this._enemy.EnemyGuid;

        if (!this._gotPigment)
        {
            if (!_PigmentLookupDict.TryGetValue(guid, out IntVector3 rgb))
            {
                rgb = _PigmentLookupDict[guid] = ComputePigmentForEnemy(guid);
                Lazy.DebugLog($"got {rgb.x}, {rgb.y}, {rgb.z}");
            }
            this._rTotal = rgb.x;
            this._gTotal = rgb.y;
            this._bTotal = rgb.z;
        }

        if (_lastKnownHealth >= 0.0f)
        {
            float pigmentLost = (Mathf.Max(0f, this._lastKnownHealth) - Mathf.Max(0f, hh.currentHealth)) / hh.AdjustedMaxHealth;
            if (pigmentLost > 0)
            {
                this._rFrac += pigmentLost * this._rTotal;
                this._gFrac += pigmentLost * this._gTotal;
                this._bFrac += pigmentLost * this._bTotal;
                // ETGModConsole.Log($"pigment levels {this._rFrac}, {this._gFrac}, {this._bFrac}");
                for (; this._rFrac >= 1.0f; this._rFrac -= 1.0f)
                    Chroma.DropPigment(this._enemy.aiActor, (PigmentType)0);
                for (; this._gFrac >= 1.0f; this._gFrac -= 1.0f)
                    Chroma.DropPigment(this._enemy.aiActor, (PigmentType)1);
                for (; this._bFrac >= 1.0f; this._bFrac -= 1.0f)
                    Chroma.DropPigment(this._enemy.aiActor, (PigmentType)2);
                this._saturation -= pigmentLost;
            }
        }

        this._lastKnownHealth = hh.currentHealth;
        if (!this._addedShader)
            this._enemy.ApplyShader(AddShader);
        foreach (Material mat in this._desatMats)
            if (mat)
                mat.SetFloat("_Saturation", this._saturation);
    }
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

        this._projectile.OnHitEnemy += this.OnHitEnemy;
    }

    private const float _DRAIN_SOUND_TIMER = 0.1f;
    private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool willKill)
    {
        if (!enemy)
            return;

        this._projectile.LoopSoundIf(true, "chroma_drain_sound");
        enemy.gameObject.GetOrAddComponent<Desaturator>().DoPigmentChecks();
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
    const float _PARTICLE_RATE      = 0.167f;

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
                chroma.AcquirePigment(this._pigment);
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
