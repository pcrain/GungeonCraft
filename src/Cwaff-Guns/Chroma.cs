namespace CwaffingTheGungy;

using static PigmentType;

public class Chroma : CwaffGun
{
    public static string ItemName         = "Chroma";
    public static string ShortDescription = "Spectroscopic";
    public static string LongDescription  = "Fires beams that extract pigment from enemies. Reloading cycles through red, green, and blue beams, which gain power from their respective pigments. Each beam is most effective at extracting the next pigment in the cycle (e.g., red > green > blue > red), and deals increased or reduced damage depending on the presence of that pigment in the enemy.";
    public static string Lore             = "A device designed for extracting pigment-containing compounds from inorganic materials for the purpose of high-end nail polish production. Retail models have a carbon scanner preventing the beams from firing at organic material, but removing the scanner is trivial and makes for a bizarrely effective weapon in a pinch.";

    private const float _BASE_PARTICLE_RATE = 0.5f;
    private const float _PER_LEVEL_PARTICLE_RATE = 0.05f;
    private const float _TRIBEAM_COLOR_CYCLE_SPEED = 10f;

    internal static GameObject _PigmentPrefab = null;

    public int redPigment   = 0;
    public int greenPigment = 0;
    public int bluePigment  = 0;

    internal Color _cachedTribeamColor = default;

    private readonly int[] _pigmentPowers = [0, 0, 0, 0];
    private bool _ammoDisplayDirty = true;
    private Material _cachedTribeamMat = null;

    public static void Init()
    {
        Lazy.SetupGun<Chroma>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.BEAM, reloadTime: 0.0f, ammo: 600, idleFps: 20, shootFps: 60,
            modulesAreTiers: true)
          .Attach<ChromaAmmoDisplay>()
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(baseProjectile: Items.Moonscraper.Projectile(), clipSize: -1, cooldown: 0.18f, //NOTE: inherit from Moonscraper for hitscan
            shootStyle: ShootStyle.Beam, damage: 7f, speed: -1f, ammoCost: 5, angleVariance: 0f,
            beamSprite: "chroma_beam", beamFps: 60, beamChargeFps: 8, beamImpactFps: 30,
            beamLoopCharge: false, beamReflections: 0, beamChargeDelay: 0f, beamEmission: 1500f))
          .Attach<ChromaProjectile>();

        string[] ammoTypes = ["chroma_red", "chroma_green", "chroma_blue", "chroma_tri"];
        //NOTE: dispersal doesn't work with instant beams (since they have no bones)
        for (int i = 0; i < 4; ++i)
        {
            ProjectileModule mod = i > 0 ? gun.DuplicateDefaultModule() : gun.DefaultModule;
            mod.SetupCustomAmmoClip(ammoTypes[i]);
            if (i == 3)  // tribeam drains ammo twice as quickly
                mod.ammoCost *= 2;
            Projectile p = mod.projectiles[0];
            p.GetComponent<ChromaProjectile>()._pigment = (PigmentType)i;
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
        if (!manualReload || !player.AcceptingNonMotionInput)
            return;
        bool wasFiring = gun.IsFiring;
        this.gun.CurrentStrengthTier = (this.gun.CurrentStrengthTier + 1) % (this.Mastered ? 4 : 3);
        if (wasFiring)
            gun.CeaseAttack();
        this._ammoDisplayDirty = true;
        UpdateBeamShaders();
        ClearCachedShootData(); // reset particle effects
        if (wasFiring)
            gun.Attack();
    }

    private void UpdateBeamShaders()
    {
        int i = this.gun.CurrentStrengthTier;
        Material mat = this.gun.DefaultModule.projectiles[0].GetComponent<BasicBeamController>().sprite.renderer.material;
        mat.SetFloat("_EmissivePower", 300f * (1 + this._pigmentPowers[i]));
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        RecalculateAllPigmentPowers();
        UpdateBeamShaders();
        ClearCachedShootData(); // reset particle effects
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);

        foreach (AIActor enemy in StaticReferenceManager.AllEnemies)
            OnEnemySpawn(enemy);
        ETGMod.AIActor.OnPreStart -= this.OnEnemySpawn;
        ETGMod.AIActor.OnPreStart += this.OnEnemySpawn;
        CwaffEvents.OnCorpseCreated -= TransferDesaturatedShadersToCorpse;
        CwaffEvents.OnCorpseCreated += TransferDesaturatedShadersToCorpse;
        RecalculateAllPigmentPowers();
        UpdateBeamShaders();
        ClearCachedShootData(); // reset particle effects
    }

    private static void TransferDesaturatedShadersToCorpse(DebrisObject debris, AIActor original)
    {
        if (original.GetComponent<Desaturator>() is not Desaturator desat)
            return;
        if (debris.gameObject.GetComponent<tk2dSprite>() is not tk2dSprite sprite)
            return;
        sprite.usesOverrideMaterial = true;
        Material mat = sprite.renderer.material;
        mat.shader = CwaffShaders.DesatShader;
        mat.SetFloat("_Saturation", desat._saturation);
        if (original.optionalPalette != null)
        {
            mat.SetFloat("_UsePalette", 1f);
            mat.SetTexture("_PaletteTex", original.optionalPalette);
        }
    }

    private void OnEnemySpawn(AIActor enemy)
    {
        enemy.gameObject.GetOrAddComponent<Desaturator>();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        ETGMod.AIActor.OnPreStart -= this.OnEnemySpawn;
        CwaffEvents.OnCorpseCreated -= TransferDesaturatedShadersToCorpse;
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
        {
            ETGMod.AIActor.OnPreStart -= this.OnEnemySpawn;
            CwaffEvents.OnCorpseCreated -= TransferDesaturatedShadersToCorpse; //NOTE: not completely robust if two copies of the gun exist
        }
        base.OnDestroy();
    }

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner)
            return;

        if (this.gun.CurrentStrengthTier == 3)
        {
            float now = _TRIBEAM_COLOR_CYCLE_SPEED * BraveTime.ScaledTimeSinceStartup;
            if (this._cachedTribeamMat == null)
                this._cachedTribeamMat = this.gun.DefaultModule.projectiles[0].GetComponent<BasicBeamController>().sprite.renderer.material;
            Color c = this._cachedTribeamColor = Color.HSVToRGB(now - Mathf.Floor(now), 0.75f, 1.0f);
            this._cachedTribeamMat.SetColor("_EmissiveColor", c);
            this._cachedTribeamMat.SetColor("_OverrideColor", Color.Lerp(c, Color.white, 0.5f));
            this._ammoDisplayDirty = true;
        }

        if (!this.PlayerOwner.AcceptingNonMotionInput)
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

        int level = this.gun.CurrentStrengthTier;
        for (int i = 0; i < (int)mag; ++i)
            CwaffVFX.Spawn(
                prefab         : VFX.SinglePixel,
                position       : barrelPos + (mag * UnityEngine.Random.value) * deltaNorm,
                velocity       : Lazy.RandomVector(4f),
                lifetime       : 0.4f,
                emissivePower  : 2000f,
                overrideColor  : PigmentDrop._Primaries[(level < 3) ? level : UnityEngine.Random.Range(0, 3)],
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

    private const int _MAX_PIGMENT_LEVEL = 10;
    private static readonly int[] _LEVELS = [16,  32,  64,  128,  256,  512,  1024, 2048, 4096, 9999999];
    // private static readonly int[] _LEVELS = [100, 300, 600, 1000, 1500, 2100, 2800, 3600, 4500, 9999999];
    public static int PigmentPower(int pigmentNum) => 1 + _LEVELS.FirstLT(pigmentNum);
    // 0, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096
    // public static int PigmentPower(int pigmentNum) => Mathf.Clamp((int)(Mathf.Log(Mathf.Max(1, pigmentNum), 2) - 2), 1, _MAX_PIGMENT_LEVEL);

    public void AcquirePigment(PigmentType pigment)
    {
        int i = (int)pigment;
        int oldPigment = this._pigmentPowers[i];
        if (pigment == RED)
            this._pigmentPowers[i] = PigmentPower(++this.redPigment);
        else if (pigment == GREEN)
            this._pigmentPowers[i] = PigmentPower(++this.greenPigment);
        else if (pigment == BLUE)
            this._pigmentPowers[i] = PigmentPower(++this.bluePigment);
        if (oldPigment != this._pigmentPowers[i])
            this._ammoDisplayDirty = true;
    }

    public override void PostProcessBeam(BeamController beam)
    {
        const float _BOOST_PER_LEVEL = 0.2f;

        base.PostProcessBeam(beam);
        if (beam.projectile is not Projectile projectile)
            return;
        if (this.PlayerOwner is not PlayerController pc)
            return;

        //NOTE: this won't update until after firing a new beam
        int level = this._pigmentPowers[this.gun.CurrentStrengthTier];
        // #if DEBUG
        //     string color = this.gun.CurrentStrengthTier switch {
        //         0 => "red",
        //         1 => "green",
        //         _ => "blue"
        //     };
        //     ETGModConsole.Log($"firing with level {level} {color} pigment == {_BOOST_PER_LEVEL * level} boost");
        // #endif
        projectile.baseData.damage *= (1f + _BOOST_PER_LEVEL * level);

        int i = this.gun.CurrentStrengthTier;
        Color c = PigmentDrop._Primaries[i];
        tk2dSprite sprite = (beam as BasicBeamController).m_impactSprite;
        sprite.usesOverrideMaterial = true;
        Material mat = sprite.renderer.material;
        mat.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
        mat.DisableKeyword("BRIGHTNESS_CLAMP_ON");
        mat.EnableKeyword("BRIGHTNESS_CLAMP_OFF");
        mat.SetFloat("_EmissivePower", 400f);
        mat.SetFloat("_EmissiveColorPower", 8f);
        mat.SetColor("_EmissiveColor", c);
        mat.SetColor("_OverrideColor", Color.Lerp(c, Color.white, 0.5f));
    }

    private void RecalculateAllPigmentPowers()
    {
        this._pigmentPowers[0] = PigmentPower(this.redPigment);
        this._pigmentPowers[1] = PigmentPower(this.greenPigment);
        this._pigmentPowers[2] = PigmentPower(this.bluePigment);
        this._pigmentPowers[3] = Mathf.Min(this._pigmentPowers[0], this._pigmentPowers[1], this._pigmentPowers[2]);
        this._ammoDisplayDirty = true;
    }

    public override void MidGameSerialize(List<object> data, int i)
    {
        base.MidGameSerialize(data, i);
        data.Add(this.redPigment);
        data.Add(this.greenPigment);
        data.Add(this.bluePigment);
    }

    public override void MidGameDeserialize(List<object> data, ref int i)
    {
        base.MidGameDeserialize(data, ref i);
        this.redPigment = (int)data[i++];
        this.greenPigment = (int)data[i++];
        this.bluePigment = (int)data[i++];
        RecalculateAllPigmentPowers();
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

            if (this._chroma._ammoDisplayDirty)
            {
                int[] pp = this._chroma._pigmentPowers;
                string colorString = (this._gun.CurrentStrengthTier < 3)
                    ? PigmentDrop._HexPrimaries[this._gun.CurrentStrengthTier]
                    : ColorUtility.ToHtmlStringRGB(this._chroma._cachedTribeamColor);
                this._ammoText = $"[sprite \"pigment_red_ui{1+pp[0]}\"] [sprite \"pigment_green_ui{1+pp[1]}\"] [sprite \"pigment_blue_ui{1+pp[2]}\"] \n[color #{colorString}]";
            }
            uic.GunAmmoCountLabel.Text = $"{this._ammoText}{this._owner.VanillaAmmoDisplay()}[/color]";
            return true;
        }
    }
}

public enum PigmentType
{
    RED,
    GREEN,
    BLUE,
    TRI, // unused
}

public class Desaturator : MonoBehaviour
{
    private static readonly Dictionary<string, IntVector3> _PigmentLookupDict = new();

    internal float _saturation = 1.0f;
    private AIActor _enemy;
    private float _lastKnownHealth = -1.0f;
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

    private float[] damageScales = [1.0f, 1.0f, 1.0f, 1.0f];

    private void Start()
    {
        Setup();
    }

    private void Setup()
    {
        if (this._didSetup)
            return;

        this._didSetup = true;
        this._enemy = base.gameObject.GetComponent<AIActor>();
    }

    private void AddShader(tk2dBaseSprite sprite)
    {
        sprite.usesOverrideMaterial = true;
        Material mat = sprite.renderer.material;
        mat.shader = CwaffShaders.DesatShader;
        mat.SetFloat("_Saturation", 1f);
        this._desatMats.Add(mat);
        this._addedShader = true;
    }

    internal void UpdateLastKnownHealth()
    {
        Setup();
        if (this._enemy && this._enemy.healthHaver is HealthHaver hh)
            this._lastKnownHealth = hh.currentHealth;
    }

    private static IntVector3 ComputePigmentForEnemy(string guid)
    {
        float r = 0;
        float g = 0;
        float b = 0;

        Color[] pixels = Lazy.GetPixelColorsForEnemy(guid);
        AIActor prefab = EnemyDatabase.GetOrLoadByGuid(guid);
        Texture2D paletteTex = prefab.optionalPalette ? prefab.optionalPalette.GetRW() : null;
        int npixels = pixels.Length;
        for (int i = 0; i < npixels; ++i)
        {
            Color pixel = pixels[i];
            if (pixel.a < 0.5f)
                continue; // mostly-transparent pixels don't count
            if (paletteTex)
                pixel = Lazy.GetPaletteColor(paletteTex, pixel.r);
            Color.RGBToHSV(pixel, out float h, out float s, out float v);
            if (s < 0.25f)
                continue; // mostly-gray pixels don't count
            h *= 6.0f;
            if (h < 0.5f || h > 5.5f)
                r += 1.0f;
            else if (h < 1.5f)
            {
                r += 0.5f;
                g += 0.5f;
            }
            else if (h < 2.5f)
                g += 1.0f;
            else if (h < 3.5f)
            {
                g += 0.5f;
                b += 0.5f;
            }
            else if (h < 4.5f)
                b += 1.0f;
            else if (h < 5.5f)
            {
                b += 0.5f;
                r += 0.5f;
            }
        }
        float norm = Mathf.Clamp(prefab.healthHaver.maximumHealth, 10, 1000) / Mathf.Max(1f, r + g + b);
        r = Mathf.Max(1f, r * norm);
        g = Mathf.Max(1f, g * norm);
        b = Mathf.Max(1f, b * norm);
        return new(Mathf.CeilToInt(r), Mathf.CeilToInt(g), Mathf.CeilToInt(b));
    }

    internal float GetPigmentMult(PigmentType pigment) => this.damageScales[(int)pigment];

    internal void DoPigmentChecks(PigmentType hitPigment, bool willKill)
    {
        const float _PIGMENT_FACTOR = 0.25f; // conversion rate from sprite pigment to actual pigment
        const float RESIST = 0.25f;
        const float WEAK   = 2.0f;

        if (!this._enemy || this._enemy.healthHaver is not HealthHaver hh || string.IsNullOrEmpty(this._enemy.EnemyGuid))
            return;
        string guid = this._enemy.EnemyGuid;

        if (!this._gotPigment)
        {
            if (!_PigmentLookupDict.TryGetValue(guid, out IntVector3 rgb))
                rgb = _PigmentLookupDict[guid] = ComputePigmentForEnemy(guid);
            this._rTotal = Mathf.CeilToInt(_PIGMENT_FACTOR * rgb.x);
            this._gTotal = Mathf.CeilToInt(_PIGMENT_FACTOR * rgb.y);
            this._bTotal = Mathf.CeilToInt(_PIGMENT_FACTOR * rgb.z);

            // shots do 25%, 100%, or 200% damage depending on pigment weaknesses
            int sum = (rgb.x + rgb.y + rgb.z);
            if (sum > 0)
            {
                float rgbNorm = 1f / sum;
                float rNorm = rgb.x * rgbNorm;
                float gNorm = rgb.y * rgbNorm;
                float bNorm = rgb.z * rgbNorm;
                this.damageScales[2] = (rNorm < 0.16f) ? RESIST : (rNorm > 0.45f) ? WEAK : 1f; // blue pigment damage based on red pigment in enemy
                this.damageScales[0] = (gNorm < 0.16f) ? RESIST : (gNorm > 0.45f) ? WEAK : 1f; // red pigment damage based on green pigment in enemy
                this.damageScales[1] = (bNorm < 0.16f) ? RESIST : (bNorm > 0.45f) ? WEAK : 1f; // green pigment damage based on blue pigment in enemy
            }
            this.damageScales[3] = 3f * WEAK;

            this._gotPigment = true;
        }

        if (_lastKnownHealth >= 0.0f)
        {
            float pigmentLost = (Mathf.Max(0f, this._lastKnownHealth) - Mathf.Max(0f, hh.currentHealth)) / hh.AdjustedMaxHealth;
            if (pigmentLost > 0)
            {
                this._rFrac += pigmentLost * this._rTotal;
                this._gFrac += pigmentLost * this._gTotal;
                this._bFrac += pigmentLost * this._bTotal;
                for (; this._rFrac >= 1.0f; this._rFrac -= 1.0f)
                    Chroma.DropPigment(this._enemy.aiActor, (PigmentType)0);
                for (; this._gFrac >= 1.0f; this._gFrac -= 1.0f)
                    Chroma.DropPigment(this._enemy.aiActor, (PigmentType)1);
                for (; this._bFrac >= 1.0f; this._bFrac -= 1.0f)
                    Chroma.DropPigment(this._enemy.aiActor, (PigmentType)2);
                this._saturation -= pigmentLost;
            }
        }

        if (willKill && hitPigment != PigmentType.TRI)
        {
            PigmentType bonusPigment = (PigmentType)(((int)hitPigment + 2) % 3); // red gives bonus blue, blue gives bonus green, and green gives bonus red
            int bonusAmount = (int)(Mathf.Log(Mathf.Max(1f, hh.AdjustedMaxHealth), 2));
            for (int i = 0; i < bonusAmount; ++i)
                Chroma.DropPigment(this._enemy.aiActor, bonusPigment);
        }

        this._lastKnownHealth = hh.currentHealth;
        if (!this._addedShader)
        {
            this._enemy.ApplyShader(AddShader);
            this._enemy.SetOutlines(true);
        }
        foreach (Material mat in this._desatMats)
            if (mat)
                mat.SetFloat("_Saturation", this._saturation);
    }
}

public class ChromaProjectile : MonoBehaviour
{
    [SerializeField]
    internal PigmentType _pigment;

    private Projectile _projectile;
    private PlayerController _owner;
    private Chroma _gun;
    private float _baseDamage = 0.0f;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        if (!this._owner)
            return;

        if (this._owner.CurrentGun is Gun gun)
            this._gun = gun.gameObject.GetComponent<Chroma>();

        this._projectile.OnHitEnemy += this.OnHitEnemy;
        this._baseDamage = this._projectile.baseData.damage;
    }

    private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool willKill)
    {
        if (!enemy)
            return;

        Desaturator desat = enemy.gameObject.GetOrAddComponent<Desaturator>();
        desat.DoPigmentChecks(this._pigment, willKill);
        float mult = desat.GetPigmentMult(this._pigment);
        //NOTE: sets damage on a one frame delay since setting it before hit would require patching beam controller logic before it applies damage
        this._projectile.baseData.damage = this._baseDamage * mult;
        if (mult > 1f)
            this._owner.LoopSoundIf(true, "chroma_drain_sound");
        else if (mult < 1f)
            this._owner.LoopSoundIf(true, "chroma_resist_sound");
    }
}

public class PigmentDrop : MonoBehaviour
{
    internal const float _BOB_SPEED  = 4f;
    internal const float _BOB_HEIGHT = 0.20f;

    const float _PICKUP_RADIUS_SQR  = 2f;   // range before we are picked up by player
    const float _HOME_ACCEL         = 44f;  // acceleration per second towards player
    const float _FRICTION           = 0.96f;
    const float _MAX_LIFE           = 10f;  // time before despawning
    const float _PARTICLE_RATE      = 0.167f;

    internal static readonly List<Color> _Primaries = [
        new(230f / 255f, 87f  / 255f, 149f / 255f), //red
        new(149f / 255f, 230f / 255f, 87f  / 255f), //green
        new( 90f / 255f, 180f / 255f, 230f / 255f), //blue
        new(255f / 255f, 255f / 255f, 255f / 255f), //white
    ];

    internal static readonly List<string> _HexPrimaries = [
        "E65795", //red
        "95E657", //green
        "5795E6", //blue
        "FFFFFF", //white
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

        if (_ChromaId < 0)
            _ChromaId = Lazy.PickupId<Chroma>();
        if (!this._owner)
            foreach (PlayerController player in GameManager.Instance.AllPlayers)
            {
                if (!player || !player.isActiveAndEnabled || player.IsGhost)
                    continue;
                if (!player.HasGun(_ChromaId))
                    continue;
                this._owner = player;
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
    }
}
