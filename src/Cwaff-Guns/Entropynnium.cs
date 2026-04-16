namespace CwaffingTheGungy;

/* TODO:
    - handle mana / ammo banking from explosions
    - handle mana particle effects from absorbing explosions
    - prevent explosion re-entrancy when detonating entropynnium's own explosions
    - make enemies glow / shimmer when being targeted for a detonation
    - make damage force and radius scale with mana reserves
    - make damage force and radius scale inversely with number of enemies hit
    - add proper gun sprites
    - add proper muzzle flash
    - add custom ammo clip
    - fix ammo display
    - tweak sound volumes
*/

public class Entropynnium : CwaffGun
{
    public static string ItemName         = "Entropynnium";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const int   _MAX_MANA            = 10000;
    private const float _MAX_RADIUS          = 16;
    private const float _CHARGE_TIME         = 5;
    private const float _THICKNESS           = 0.5f;
    private const float _PARTICLE_TIMER      = 0.1f;
    private const float _SOUND_TIMER         = 0.75f;

    internal static GameObject _ManaParticlePrefab = null;
    internal static ExplosionData _SmallManaExplosion = null;

    [SerializeField]
    private int _storedMana = 0;

    [SerializeField]
    private bool _gatheringMana = false;

    private float _manaRadius = 0f;
    private float _nextParticleTime = 0f;
    private float _nextSoundTime = 0f;

    private Geometry _extantManaRing = null;

    public static void Init()
    {
        Lazy.SetupGun<Entropynnium>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.A, gunClass: GunClass.EXPLOSIVE, reloadTime: 1.5f, ammo: 720, shootFps: 30, reloadFps: 20,
            chargeFps: 10, muzzleFps: 30, muzzleScale: 0.5f, muzzleAnchor: Anchor.MiddleCenter, fireAudio: "carpet_bomber_shoot_sound", smoothReload: 0.1f,
            attacksThroughWalls: true, canGainAmmo: false, canReloadNoMatterAmmo: true)
          .Attach<EntropynniumAmmoDisplay>()
          .AssignGun(out Gun gun)
          .InitSpecialProjectile<ManaExplosionProjectile>(GunData.New(clipSize: -1, cooldown: 0.15f, damage: 20f,
            shootStyle: ShootStyle.Charged, range: 9999f, sequenceStyle: ProjectileSequenceStyle.Ordered, invisibleProjectile: true)) //TODO: add custom ammo clip
          .SetupChargeProjectiles(gun.DefaultModule, 1, (i, p) => new() {
            Projectile = p.Clone(GunData.New(speed: 40f + 20f * i)),
            ChargeTime = 1.0f });

        _ManaParticlePrefab = VFX.Create("mana_particle", fps: 30);

        _SmallManaExplosion = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultSmallExplosionData.Clone();
        _SmallManaExplosion.damageToPlayer = 0f;
    }

    public override void Update()
    {
        base.Update();
        UpdateDetonationRing();
    }

    private void GatherManaFromExplosion(Vector3 position, ExplosionData data, Vector2 sourceNormal)
    {
        //TODO: call from patch
    }

    private void UpdateDetonationRing()
    {
        if (!this.gun.IsCharging)
        {
            DestroyDetonationRing();
            return;
        }
        this._manaRadius += (_MAX_RADIUS / _CHARGE_TIME) * BraveTime.DeltaTime;
        if (this._manaRadius < _THICKNESS)
            return;
        if (this._manaRadius > _MAX_RADIUS)
            this._manaRadius = _MAX_RADIUS;

        if (!this._extantManaRing)
            this._extantManaRing = new GameObject("mana_ring").AddComponent<Geometry>();

        Transform gunTransform = this.gun.barrelOffset.transform;
        Vector2 ppos = gunTransform.position;
        this._extantManaRing.Setup(shape: Geometry.Shape.RING, color: ExtendedColours.purple.WithAlpha(0.05f),
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
            CwaffVFX.SpawnBurst(
                prefab           : _ManaParticlePrefab,
                numToSpawn       : 16,
                basePosition     : ppos,
                positionVariance : this._manaRadius,
                minVariance      : this._manaRadius - _THICKNESS,
                velType          : CwaffVFX.Vel.InwardToCenter,
                rotType          : CwaffVFX.Rot.Random,
                lifetime         : 0.25f * Mathf.Sqrt(this._manaRadius),
                startScale       : 0.6f,
                endScale         : 0.2f,
                emissivePower    : 100f,
                emissiveColor    : ExtendedColours.purple,
                anchorTransform  : gunTransform,
                unoccluded       : true
              );
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

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        DestroyDetonationRing();
    }

    public override void OnDestroy()
    {
        DestroyDetonationRing();
        base.OnDestroy();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (projectile is not ManaExplosionProjectile mep)
            return;
        mep.Setup(radius: this._manaRadius, damageMult: Mathf.Sqrt(this._manaRadius), forceMult: 1f);
        DestroyDetonationRing();
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        if (!player.IsDodgeRolling && player.AcceptingNonMotionInput)
            ToggleManaGathering();
    }

    private void ToggleManaGathering()
    {
        this._gatheringMana = !this._gatheringMana;
    }

    private void DetonateMana()
    {

    }

    private class EntropynniumAmmoDisplay : CustomAmmoDisplay
    {
        private Entropynnium _ent;
        private PlayerController _owner;

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

            uic.GunAmmoCountLabel.Text = $"[sprite \"vacuum_debris_ui\"]x{this._ent._storedMana}";
            return true;
        }
    }
}

public class ManaExplosionProjectile : Projectile
{
    private float _radius = 0f;
    private float _damageMult = 0f;
    private float _forceMult = 0f;

    internal static GameObject _ExplosionPrefab = null;

    public void Setup(float radius, float damageMult, float forceMult)
    {
        this._radius = radius;
        this._damageMult = damageMult;
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

    private void Detonate()
    {
        if (this._damageMult == 0f || this._radius == 0f)
            return; // nothing to do, just die instantly
        if (this.Owner is not PlayerController player)
            return; // nothing to do, just die instantly

        float damage = this.baseData.damage * this._damageMult;
        float force = damage * this._forceMult;
        Vector2 ppos = this.m_transform.position;

        ExplosionData boom = Entropynnium._SmallManaExplosion.Clone();
        boom.damage = damage;
        boom.force = force;

        // Lazy.DebugLog($"TODO: detonating with radius {this._radius}, damage {damage}, force {force}");
        bool anythingDetonated = false;
        foreach (AIActor enemy in Lazy.GetAllNearbyEnemies(ppos, this._radius, ignoreWalls: true))
        {
            if (enemy && enemy.isActiveAndEnabled && enemy.healthHaver is HealthHaver hh && hh.IsAlive)
            {
                Exploder.Explode(
                    position     : enemy.CenterPosition,
                    data         : boom,
                    sourceNormal : Vector2.zero,
                    ignoreQueues : true);
                CwaffVFX.SpawnBurst(
                    prefab           : Entropynnium._ManaParticlePrefab,
                    numToSpawn       : 32,
                    basePosition     : enemy.CenterPosition,
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
                anythingDetonated = true;
            }
        }
        if (anythingDetonated)
            base.gameObject.Play("mana_detonate");
    }
}
