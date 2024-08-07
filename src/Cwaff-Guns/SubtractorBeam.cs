namespace CwaffingTheGungy;

public class SubtractorBeam : CwaffGun
{
    public static string ItemName         = "Subtractor Beam";
    public static string ShortDescription = "What's the Difference?";
    public static string LongDescription  = "Fires a fast piercing beam that uses the health of the first enemy it hits as its damage. The beam's damage is reduced by the health of each subsequent enemy it hits, and dissipates once its damage reaches zero. Cannot damage the first enemy it hits. Reveals enemies' health while active.";
    public static string Lore             = "In a time and place where weaponry seems to have an ever-increasing disregard for the biology, chemistry, and physics that govern our universe, the Subtractor Beam spits in the face of something even more fundamental: math. Invented by someone who wasn't a mad scientist so much as a moderately irritable elementary school professor, the destructive potential of this gun has been calibrated to vary with the resilience of the first object it passes through. As to why a weapon with near limitless potential would be designed with such arbitrary limitations, the creator has claimed it is 'to show the kids that subtraction tables ARE useful in real life! Now if only I could do something for multiplication tables....'";

    internal static Dictionary<int, Nametag> _Nametags = new();

    internal static TrailController _GreenTrailPrefab;
    internal static TrailController _RedTrailPrefab;
    internal static GameObject _HitEffects;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<SubtractorBeam>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.D, gunClass: GunClass.FULLAUTO, reloadTime: 1.25f, ammo: 300, idleFps: 10, shootFps: 24, reloadFps: 30,
                muzzleVFX: "muzzle_subtractor_beam", muzzleFps: 30, muzzleScale: 0.3f, muzzleAnchor: Anchor.MiddleCenter,
                reloadAudio: "subtractor_beam_reload_sound", banFromBlessedRuns: true);

        _GreenTrailPrefab = VFX.CreateTrailObject("subtractor_beam_mid", fps: 60, startAnim: "subtractor_beam_start",
            softMaxLength: 1f, cascadeTimer: C.FRAME, destroyOnEmpty: true);
        _RedTrailPrefab = VFX.CreateTrailObject("subtractor_beam_red_mid", fps: 60, startAnim: "subtractor_beam_red_start",
            softMaxLength: 1f, cascadeTimer: C.FRAME, destroyOnEmpty: true);
        _HitEffects = VFX.Create("subtractor_beam_hit_effect", 12, loops: true,
            scale: 0.5f, anchor: Anchor.MiddleCenter, emissivePower: 10f);

        gun.InitProjectile(GunData.New(clipSize: 4, cooldown: 0.25f, angleVariance: 5.0f, shootStyle: ShootStyle.SemiAutomatic,
          damage: 0.0f, speed: 300.0f, force: 0.0f, range: 300.0f, customClip: true, spawnSound: "subtractor_beam_fire_sound", uniqueSounds: true
          )).Attach<PierceProjModifier>(pierce => {
            pierce.penetration            = 999;
            pierce.penetratesBreakables   = true;
          }).Attach<SubtractorProjectile>();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        WhoAreTheyAgain();
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        WhoAreTheyAgain();
        base.OnDestroy();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        WhoAreTheyAgain();
        base.OnSwitchedAwayFromThisGun();
    }

    public override void Update()
    {
        base.Update();
        if ((this.PlayerOwner is not PlayerController player) || !player.healthHaver || player.healthHaver.IsDead)
            WhoAreTheyAgain();
        else
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
        if (!this.PlayerOwner)
            return;

        foreach (AIActor enemy in this.PlayerOwner.CurrentRoom.SafeGetEnemiesInRoom())
        {
            if (!enemy.IsHostile(canBeNeutral: true))
                continue;
            if (!enemy.gameObject.GetComponent<Nametag>())
            {
                Nametag nametag = enemy.gameObject.AddComponent<Nametag>();
                nametag.Setup();
                _Nametags[enemy.GetHashCode()] = nametag;
            }
            enemy.GetComponent<Nametag>().SetName($"{enemy.healthHaver.GetCurrentHealth()}");
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
            if (this._trail)
                this._trail.DisconnectFromSpecRigidbody(); // we want to have a red trail after hitting the enemy, but want the old green trail around as well
            this._trail = this._projectile.AddTrailToProjectileInstance(SubtractorBeam._RedTrailPrefab);
                this._trail.gameObject.SetGlowiness(100f);
            return;
        }

        CwaffVFX.SpawnBurst(prefab: SubtractorBeam._HitEffects, numToSpawn: _NUM_HIT_PARTICLES, basePosition: enemy.CenterPosition,
            positionVariance: 1f, baseVelocity: Vector2.zero, velocityVariance: 1f, velType: CwaffVFX.Vel.Radial,
            lifetime: 0.5f, fadeOutTime: 0.5f);

        enemy.gameObject.PlayUnique("subtractor_beam_impact_sound");

        this._damage                     = this._postHitDamage;
        this._projectile.baseData.damage = this._damage;
        if (this._damage <= 0)
            this._projectile.DieInAir();
    }
}
