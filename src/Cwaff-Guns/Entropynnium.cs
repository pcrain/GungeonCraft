namespace CwaffingTheGungy;

public class Entropynnium : CwaffGun
{
    public static string ItemName         = "Entropynnium";
    public static string ShortDescription = "Applied Botanics";
    public static string LongDescription  = "Passively gathers mana from explosions. Can be charged to consume mana and detonate enemies around the player. Longer charges result in a larger detonation range and more potent detonations, but consume dramatically more mana.";
    public static string Lore             = "A flower from another dimension where the nutritional needs of plants are significantly more complicated than soil, water, and sunlight. This particular specimen draws its nutrients from ambient explosions in its environment, a trait powerful in its natural habitat and even more so in the Gungeon. If only Gungeoneers had the same diet....";

    private const int   _MAX_MANA            = 10000;
    private const float _CHARGE_TIME         = 5;
    private const float _THICKNESS           = 0.5f;
    private const float _PARTICLE_TIMER      = 0.1f;
    private const float _SOUND_TIMER         = 0.75f;

    internal const float _MAX_RADIUS         = 16;
    internal const float _MANA_ROI           = 0.25f; // mana return on investment for detonating explosions
    internal const float _MANA_DRAIN         = 10f; // mana drain rate, scaled by square of radius

    internal static GameObject _ManaParticlePrefab = null;
    internal static GameObject _ExplosionManaPrefab = null;
    internal static ExplosionData _SmallManaExplosion = null;

    [SerializeField]
    private int _storedMana = 0;

    private float _manaRadius = 0f;
    private float _nextParticleTime = 0f;
    private float _nextSoundTime = 0f;
    private int _manaDrainedThisCharge = 0;

    private Geometry _extantManaRing = null;

    public static void Init()
    {
        Lazy.SetupGun<Entropynnium>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.EXPLOSIVE, reloadTime: 0.0f, ammo: 720, shootFps: 20,
            chargeFps: 20, muzzleFps: 30, muzzleAnchor: Anchor.MiddleCenter,
            attacksThroughWalls: true, canGainAmmo: false, canReloadNoMatterAmmo: true, infiniteAmmo: true)
          .Attach<EntropynniumAmmoDisplay>()
          .AssignGun(out Gun gun)
          .InitSpecialProjectile<ManaExplosionProjectile>(GunData.New(clipSize: -1, cooldown: 0.15f, damage: 25f, hideAmmo: true,
            shootStyle: ShootStyle.Charged, range: 9999f, sequenceStyle: ProjectileSequenceStyle.Ordered, invisibleProjectile: true))
          .SetupChargeProjectiles(gun.DefaultModule, 1, (i, p) => new() { Projectile = p, ChargeTime = 0.5f });

        _ManaParticlePrefab = VFX.Create("mana_particle", fps: 30);

        _SmallManaExplosion = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultSmallExplosionData.Clone();
        _SmallManaExplosion.damageToPlayer = 0f;

        _ExplosionManaPrefab = _ManaParticlePrefab.ClonePrefab();
        _ExplosionManaPrefab.AddComponent<ExplosionMana>();
    }

    public override void Update()
    {
        base.Update();
        gun.sprite.renderer.material.SetFloat(CwaffVFX._EmissivePowerId, 5f + 10f * Mathf.Abs(Mathf.Sin(BraveTime.ScaledTimeSinceStartup)));
        UpdateDetonationRing();
    }

    internal void UpdateMana(int mana)
    {
        this._storedMana = Mathf.Min(this._storedMana + mana, _MAX_MANA);
    }

    private void UpdateDetonationRing()
    {
        const float MIN_MANA_COST = _THICKNESS * _THICKNESS * _MANA_DRAIN;
        if (!this.gun.IsCharging || (this._manaDrainedThisCharge == 0 && this._storedMana < MIN_MANA_COST))
        {
            DestroyDetonationRing();
            this._manaDrainedThisCharge = 0;
            return;
        }
        float dtime = BraveTime.DeltaTime;
        float newRadius = Mathf.Min(this._manaRadius + (_MAX_RADIUS / _CHARGE_TIME) * dtime, _MAX_RADIUS);
        int manaCost = Mathf.CeilToInt((this.Mastered ? 0.5f : 1.0f) * newRadius * newRadius * _MANA_DRAIN) - this._manaDrainedThisCharge;
        if (manaCost <= this._storedMana)
        {
            this._storedMana -= manaCost;
            this._manaDrainedThisCharge += manaCost;
            this._manaRadius = newRadius;
        }
        if (this._manaRadius < _THICKNESS)
            return;

        if (!this._extantManaRing)
            this._extantManaRing = Geometry.Create(Geometry.Shape.RING);

        Transform gunTransform = this.gun.barrelOffset.transform;
        Vector2 ppos = gunTransform.position;
        this._extantManaRing.Place(color: ExtendedColours.purple.WithAlpha(0.05f),
          pos: ppos, radius: this._manaRadius, radiusInner: this._manaRadius - _THICKNESS);

        // spawn some intimindating looking mana particles
        float now = BraveTime.ScaledTimeSinceStartup;
        if (this._nextSoundTime < now)
        {
            this._nextSoundTime = now + _SOUND_TIMER;
            base.gameObject.Play("entropynnium_charge_sound");
        }
        if (this._nextParticleTime < now)
        {
            this._nextParticleTime = now + _PARTICLE_TIMER;
            float particleLifetime = 0.25f * Mathf.Sqrt(this._manaRadius);
            // float effectiveRadius = FancyMathSoParticlesAreSynedWithRing(this._manaRadius - _THICKNESS, particleLifetime);
            float effectiveRadius = this._manaRadius + (_MAX_RADIUS / _CHARGE_TIME) * particleLifetime;
            CwaffVFX.SpawnBurst(
                prefab           : _ManaParticlePrefab,
                numToSpawn       : 16,
                basePosition     : ppos,
                positionVariance : effectiveRadius,
                minVariance      : effectiveRadius - _THICKNESS,
                velType          : CwaffVFX.Vel.OutwardFromCenter,
                rotType          : CwaffVFX.Rot.Random,
                lifetime         : particleLifetime,
                startScale       : 0.6f,
                endScale         : 0.2f,
                emissivePower    : 100f,
                emissiveColor    : ExtendedColours.purple,
                anchorTransform  : gunTransform,
                unoccluded       : true
              );
            foreach (AIActor enemy in Lazy.GetAllNearbyEnemies(ppos, this._manaRadius, ignoreWalls: true))
            {
                if (!enemy || !enemy.isActiveAndEnabled || enemy.healthHaver is not HealthHaver hh || !hh.IsAlive)
                    continue;
                tk2dBaseSprite dupe = enemy.DuplicateInWorld(copyShader: false);
                dupe.ApplyShader(CwaffShaders.WiggleShader, enemy.optionalPalette);
                dupe.StartCoroutine(dupe.PhaseOut(Lazy.RandomVector(), 32f, 200f, 0.75f));
            }
        }
    }

    private void DestroyDetonationRing()
    {
        this._manaRadius = 0f;
        if (this._extantManaRing)
            UnityEngine.Object.Destroy(this._extantManaRing);
        this._extantManaRing = null;
        base.gameObject.Play("entropynnium_charge_sound_stop");
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        DestroyDetonationRing();
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        CustomActions.OnExplosionComplex += HandleExplosion;
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        CustomActions.OnExplosionComplex -= HandleExplosion;
        DestroyDetonationRing();
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        CustomActions.OnExplosionComplex -= HandleExplosion;
        DestroyDetonationRing();
        base.OnDestroy();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (projectile is not ManaExplosionProjectile mep)
            return;
        if (this._manaRadius == _MAX_RADIUS && this.Mastered && this.PlayerOwner is PlayerController player)
          player.ForceBlank();
        mep.Setup(radius: this._manaRadius, forceMult: 1f);
        DestroyDetonationRing();
    }

    private class EntropynniumAmmoDisplay : CustomAmmoDisplay
    {
        private Entropynnium _ent;
        private PlayerController _owner;

        private static readonly Color _AmmoLabelColor = Color.Lerp(ExtendedColours.purple, Color.white, 0.5f);

        private void Start()
        {
            Gun gun     = base.GetComponent<Gun>();
            this._ent   = gun.GetComponent<Entropynnium>();
            this._owner = gun.CurrentOwner as PlayerController;
        }

        public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
        {
            if (!this._owner)
                return false;

            uic.SetAmmoCountLabelColor(_AmmoLabelColor);
            uic.GunAmmoCountLabel.Text = $"{this._ent._storedMana}[sprite \"mana_ui\"]";
            return true;
        }
    }

    private void HandleExplosion(Vector3 position, ExplosionData data, Vector2 dir, Action onbegin, bool ignoreQueues, CoreDamageTypes damagetypes, bool ignoreDamageCaps)
    {
        const float MANA_PARTICLE_SPEED = 15f;
        if (this.PlayerOwner is not PlayerController player)
            return;

        float damage = data.damage;
        float radius = data.GetDefinedDamageRadius();
        int mana = Mathf.CeilToInt(_MANA_ROI * damage * radius * radius);
        while (mana > 0)
        {
            ExplosionMana manaParticle = UnityEngine.Object.Instantiate(_ExplosionManaPrefab, position, Quaternion.identity).GetComponent<ExplosionMana>();
            if (mana > 25)
            {
                mana -= 25;
                manaParticle.Setup(this, player, 25, 1.0f, Lazy.RandomVector(MANA_PARTICLE_SPEED));
            }
            else if (mana > 5)
            {
                mana -= 5;
                manaParticle.Setup(this, player, 5, 0.5f, Lazy.RandomVector(MANA_PARTICLE_SPEED));
            }
            else
            {
                mana -= 1;
                manaParticle.Setup(this, player, 1, 0.25f, Lazy.RandomVector(MANA_PARTICLE_SPEED));
            }
        }
    }

    public override void MidGameSerialize(List<object> data, int i)
    {
        base.MidGameSerialize(data, i);
        data.Add(this._storedMana);
    }

    public override void MidGameDeserialize(List<object> data, ref int i)
    {
        base.MidGameDeserialize(data, ref i);
        this._storedMana = ((int)data[i++]);
    }
}

public class ExplosionMana : MonoBehaviour
{
    private const float _MAX_RADIUS     = 1f;
    private const float _MAX_SQR_RADIUS = _MAX_RADIUS * _MAX_RADIUS;
    private const float _MAX_SPEED      = 50f;
    private const float _MAX_SPEED_SQR  = _MAX_SPEED * _MAX_SPEED;
    private const float _BOB_SPEED      = 2f;
    private const float _BOB_HEIGHT     = 0.30f;
    private const float _HOME_ACCEL     = 4.0f;
    private  const float _FRICTION      = 0.96f;

    private Entropynnium _gun       = null;
    private PlayerController _owner = null;
    private int _mana               = 1;
    private float _homeStrength     = 0f;
    private bool _setup             = false;
    private Vector2 _velocity       = default;
    private Vector3 _basePos        = default;

    public void Setup(Entropynnium gun, PlayerController owner, int mana, float scale, Vector2 velocity)
    {
        this._gun      = gun;
        this._owner    = owner;
        this._mana     = mana;
        this._setup    = true;
        this._velocity = velocity;
        this._basePos  = base.transform.position;
        tk2dBaseSprite sprite = base.gameObject.GetComponent<tk2dBaseSprite>();
        sprite.scale = new Vector3(scale, scale, 1f);
        sprite.SetGlowiness(100f, glowColor: ExtendedColours.purple);
    }

    private void Update()
    {
        if (!this._setup)
            return;
        if (!this._owner)
        {
            UnityEngine.Object.Destroy(base.gameObject);
            return;
        }
        Vector2 pos = base.transform.position;
        Vector2 opos = this._owner.CenterPosition;
        Vector2 pdelta = (opos - pos);
        float sqrMag = pdelta.sqrMagnitude;
        if (sqrMag < _MAX_SQR_RADIUS)
        {
            if (this._gun)
            {
                this._gun.UpdateMana(this._mana);
                this._gun.gameObject.PlayOnce("mana_gather");
            }
            CwaffVFX.SpawnBurst(
                prefab           : Entropynnium._ManaParticlePrefab,
                numToSpawn       : 4,
                basePosition     : opos,
                positionVariance : 1.0f,
                minVariance      : 0.5f,
                minVelocity      : 2f,
                velocityVariance : 2f,
                velType          : CwaffVFX.Vel.AwayRadial,
                rotType          : CwaffVFX.Rot.Random,
                lifetime         : 0.2f,
                startScale       : 0.5f,
                endScale         : 0.2f,
                emissivePower    : 100f,
                emissiveColor    : ExtendedColours.purple
              );
            UnityEngine.Object.Destroy(base.gameObject);
            return;
        }

        float dtime = BraveTime.DeltaTime;
        Vector2 deltaNorm = pdelta.normalized;
        this._homeStrength += _HOME_ACCEL * dtime;
        this._velocity = Lazy.SmoothestLerp(this._velocity, Mathf.Clamp(sqrMag, 10f * this._homeStrength, 50f) * deltaNorm, _homeStrength);
        this._basePos += (this._velocity * dtime).ToVector3ZUp();
        base.transform.position = this._basePos.HoverAt(amplitude: _BOB_HEIGHT, frequency: _BOB_SPEED);
    }
}

public class ManaExplosionProjectile : Projectile
{
    private const float _DEFAULT_RADIUS = 4f;

    private float _radius = -1f;
    private float _forceMult = 0f;

    internal static GameObject _ExplosionPrefab = null;

    public void Setup(float radius, float forceMult)
    {
        this._radius = radius;
        this._forceMult = forceMult;
    }

    public override void Start()
    {
        base.Start();
        this.m_usesNormalMoveRegardless = true; // ignore Helix Bullets, etc.
        this.collidesWithEnemies = false; // don't collide with anything, we handle this manually
    }

    public override void Move()
    {
        Detonate();

        DieInAir(true, false, false, false);
    }

    private void DoParticles(Vector2 pos, int num)
    {
      CwaffVFX.SpawnBurst(
        prefab           : Entropynnium._ManaParticlePrefab,
        numToSpawn       : num,
        basePosition     : pos,
        positionVariance : 1.0f,
        minVariance      : 0.5f,
        minVelocity      : 8f,
        velocityVariance : 4f,
        velType          : CwaffVFX.Vel.AwayRadial,
        rotType          : CwaffVFX.Rot.Random,
        lifetime         : 0.4f,
        startScale       : 0.6f,
        endScale         : 0.2f,
        emissivePower    : 100f,
        emissiveColor    : ExtendedColours.purple,
        height           : 8f
      );
    }

    private void Detonate()
    {
        if (this._radius == 0f || this.Owner is not PlayerController player)
            return; // nothing to do, just die instantly

        Vector2 ppos = this.m_transform.position;
        if (this._radius < 0f)
        {
          // Lazy.DebugConsoleLog($"default mana radius at {ppos.x},{ppos.y}");
          this._radius = _DEFAULT_RADIUS;
        }

        float potency = Mathf.Min(this._radius / (0.67f * Entropynnium._MAX_RADIUS), 1f); // allow it to reach max potency at 2/3 max radius
        float damage = this.baseData.damage * Mathf.Sqrt(this._radius);
        float force = damage * this._forceMult;

        ExplosionData boom = Entropynnium._SmallManaExplosion.Clone();
        boom.damage = damage;
        boom.force = force;

        bool anythingDetonated = false;
        foreach (AIActor enemy in Lazy.GetAllNearbyEnemies(ppos, this._radius, ignoreWalls: true))
        {
            if (!enemy || !enemy.isActiveAndEnabled || enemy.healthHaver is not HealthHaver hh || !hh.IsAlive)
                continue;
            boom.damage = Mathf.Max(damage, hh.AdjustedMaxHealth * potency * ((hh.IsBoss || hh.IsSubboss) ? 0.5f : 1.0f));
            Exploder.Explode(
                position         : enemy.CenterPosition,
                data             : boom,
                sourceNormal     : Vector2.zero,
                ignoreQueues     : true,
                ignoreDamageCaps : true);
            DoParticles(enemy.CenterPosition, 32);
            anythingDetonated = true;
        }
        if (anythingDetonated)
          base.gameObject.Play("mana_detonate");
    }
}
