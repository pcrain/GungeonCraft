namespace CwaffingTheGungy;

public class SubtractorBeam : AdvancedGunBehavior
{
    public static string ItemName         = "Subtractor Beam";
    public static string SpriteName       = "subtractor_beam";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "What's the Difference?";
    public static string LongDescription  = "Fires a fast piercing beam that uses the health of the first enemy it hits as its damage. The beam's damage is reduced by the health of each subsequent enemy it hits, and dissipates once its damage reaches zero. Cannot damage the first enemy it hits. Reveals enemies' health while active.";

    internal static Dictionary<int, Nametag> _Nametags = new();

    internal static TrailController _GreenTrailPrefab;
    internal static TrailController _RedTrailPrefab;
    internal static GameObject _HitEffects;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<SubtractorBeam>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
            gun.SetAttributes(quality: PickupObject.ItemQuality.D, gunClass: GunClass.FULLAUTO, reloadTime: 1.25f, ammo: 300);
            gun.SetAnimationFPS(gun.shootAnimation, 24);
            gun.SetAnimationFPS(gun.idleAnimation, 10);
            gun.SetAnimationFPS(gun.reloadAnimation, 30);
            gun.SetMuzzleVFX("muzzle_subtractor_beam", fps: 30, scale: 0.3f, anchor: tk2dBaseSprite.Anchor.MiddleCenter);
            gun.SetReloadAudio("subtractor_beam_reload_sound");

        _GreenTrailPrefab = VFX.CreateTrailObject(ResMap.Get("subtractor_beam_mid")[0], new Vector2(20, 4), new Vector2(0, 0),
            ResMap.Get("subtractor_beam_mid"), 60, ResMap.Get("subtractor_beam_start"), 60, cascadeTimer: C.FRAME, destroyOnEmpty: true);
        _RedTrailPrefab = VFX.CreateTrailObject(ResMap.Get("subtractor_beam_red_mid")[0], new Vector2(20, 4), new Vector2(0, 0),
            ResMap.Get("subtractor_beam_red_mid"), 60, ResMap.Get("subtractor_beam_red_start"), 60, cascadeTimer: C.FRAME, destroyOnEmpty: true);
        _HitEffects = VFX.RegisterVFXObject("SubtractorHitEffects", ResMap.Get("subtractor_beam_hit_effect"), 12, loops: true,
            scale: 0.5f, anchor: tk2dBaseSprite.Anchor.MiddleCenter, emissivePower: 10f);

        ProjectileModule mod = gun.DefaultModule;
            mod.ammoCost            = 1;
            mod.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
            mod.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            mod.angleVariance       = 5.0f;
            mod.cooldownTime        = 0.25f;
            mod.numberOfShotsInClip = 4;

        Projectile beamProj = Lazy.PrefabProjectileFromGun(gun);
            beamProj.baseData.speed  = 300f;
            beamProj.baseData.range  = 300f;
            beamProj.baseData.damage = 0f;
            beamProj.baseData.force  = 0f;

        PierceProjModifier pierce = beamProj.gameObject.GetOrAddComponent<PierceProjModifier>();
            pierce.penetration            = 999;
            pierce.penetratesBreakables   = true;

        beamProj.gameObject.AddComponent<SubtractorProjectile>();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        AkSoundEngine.PostEvent("subtractor_beam_fire_sound_stop_all", this.Owner.gameObject);
        AkSoundEngine.PostEvent("subtractor_beam_fire_sound", this.Owner.gameObject);
    }

    protected override void OnPostDroppedByPlayer(PlayerController player)
    {
        base.OnPostDroppedByPlayer(player);
        WhoAreTheyAgain();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        WhoAreTheyAgain();
    }

    protected override void Update()
    {
        base.Update();
        YouShallKnowTheirNames();
    }

    private void WhoAreTheyAgain()
    {
        UpdateNametags(false);
    }

    private void UpdateNametags(bool enable)
    {
        List<int> deadEnemies = new();
        foreach(KeyValuePair<int, Nametag> entry in _Nametags)
        {
            if (!entry.Value.UpdateWhileParentAlive())
                deadEnemies.Add(entry.Key);
            else
                entry.Value.SetEnabled(enable);
        }
        foreach (int key in deadEnemies)
            _Nametags.Remove(key);
    }

    private void YouShallKnowTheirNames()
    {
        UpdateNametags(true);
        if (this.Owner?.GetAbsoluteParentRoom()?.GetActiveEnemies(RoomHandler.ActiveEnemyType.All) is not List<AIActor> activeEnemies)
            return;

        foreach (AIActor enemy in activeEnemies)
        {
            if (!enemy.IsHostile())
                continue;
            if (!enemy.gameObject.GetComponent<Nametag>())
                _Nametags[enemy.GetHashCode()] = enemy.gameObject.AddComponent<Nametag>();
            enemy.gameObject.GetComponent<Nametag>().SetName($"{enemy.healthHaver.GetCurrentHealth()}");
        }
    }
}

public class SubtractorProjectile : MonoBehaviour
{
    const int _NUM_HIT_PARTICLES = 5;

    private Projectile _projectile;
    private PlayerController _owner;

    private bool _hitFirstEnemy    = false;
    private float _damage          = 0f;
    private float _postHitDamage   = 0f;
    private TrailController _trail = null;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner      = this._projectile.Owner as PlayerController;
        this._trail      = this._projectile.AddTrailToProjectileInstance(SubtractorBeam._GreenTrailPrefab);
            this._trail.gameObject.SetGlowiness(100f);

        this._projectile.sprite.renderer.enabled                = false;
        this._projectile.OnHitEnemy                            += this.OnHitEnemy;
        this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
    }

    private void OnPreRigidbodyCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, SpeculativeRigidbody other, PixelCollider otherPixelCollider)
    {
        if ((other.specRigidbody.GetComponent<AIActor>() is not AIActor enemy) || !enemy.IsHostile(canBeNeutral: true))
        {
            PhysicsEngine.SkipCollision = true;
            return;
        }

        this._projectile.m_hasPierced = false; // reset pierce damange penalty from 0.5 to 1.0

        if (!this._hitFirstEnemy)
            return;

        this._postHitDamage = this._damage - enemy.healthHaver.GetCurrentHealth();
    }

    private void OnHitEnemy(Projectile p, SpeculativeRigidbody body, bool wasKiled)
    {
        if ((body.aiActor is not AIActor enemy) || (!enemy.IsHostile(canBeNeutral: true) && !wasKiled))
            return;

        if (!this._hitFirstEnemy)
        {
            this._hitFirstEnemy = true;
            this._damage = enemy.healthHaver.GetCurrentHealth();
            this._projectile.baseData.damage = this._damage;
            this._trail?.DisconnectFromSpecRigidbody(); // we want to have a red trail after hitting the enemy, but want the old green trail around as well
            this._trail = this._projectile.AddTrailToProjectileInstance(SubtractorBeam._RedTrailPrefab);
                this._trail.gameObject.SetGlowiness(100f);
            return;
        }

        Vector2 epos = enemy.sprite.WorldCenter;
        for (int i = 0; i < _NUM_HIT_PARTICLES; ++i)
        {
            float angle = Lazy.RandomAngle();
            Vector2 finalpos = epos + BraveMathCollege.DegreesToVector(angle);
            FancyVFX.Spawn(SubtractorBeam._HitEffects, finalpos, Quaternion.identity, velocity: Lazy.RandomVector(1f), lifetime: 0.5f, fadeOutTime: 0.5f);
        }
        AkSoundEngine.PostEvent("subtractor_beam_impact_sound_stop_all", enemy.gameObject);
        AkSoundEngine.PostEvent("subtractor_beam_impact_sound", enemy.gameObject);

        this._damage                     = this._postHitDamage;
        this._projectile.baseData.damage = this._damage;
        if (this._damage <= 0)
            this._projectile.DieInAir();
    }
}
