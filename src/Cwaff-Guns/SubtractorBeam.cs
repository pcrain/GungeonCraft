namespace CwaffingTheGungy;

public class SubtractorBeam : CwaffGun
{
    public static string ItemName         = "Subtractor Beam";
    public static string ShortDescription = "What's the Difference?";
    public static string LongDescription  = "Fires a fast piercing beam that uses the health of the first enemy it hits as its damage. The beam's damage is reduced by the health of each subsequent enemy it hits, and dissipates once its damage reaches zero. Cannot damage the first enemy it hits. Reveals enemies' health while active.";
    public static string Lore             = "In a time and place where weaponry seems to have an ever-increasing disregard for the biology, chemistry, and physics that govern our universe, the Subtractor Beam spits in the face of something even more fundamental: math. Invented by someone who wasn't a mad scientist so much as a moderately irritable elementary school professor, the destructive potential of this gun has been calibrated to vary with the resilience of the first object it passes through. As to why a weapon with near limitless potential would be designed with such arbitrary limitations, the creator has claimed it is 'to show the kids that subtraction tables ARE useful in real life! Now if only I could do something for multiplication tables....'";

    internal static CwaffTrailController _GreenTrailPrefab;
    internal static CwaffTrailController _RedTrailPrefab;
    internal static GameObject _HitEffects;
    private static readonly LinkedList<dfLabel> _PooledNametags = new();
    private static readonly LinkedList<dfLabel> _ActiveNametags = new();

    internal HealthHaver _lastHitEnemy = null;

    public static void Init()
    {
        Lazy.SetupGun<SubtractorBeam>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.D, gunClass: GunClass.RIFLE, reloadTime: 1.25f, ammo: 300, idleFps: 10, shootFps: 24, reloadFps: 30,
            muzzleVFX: "muzzle_subtractor_beam", muzzleFps: 30, muzzleScale: 0.3f, muzzleAnchor: Anchor.MiddleCenter, smoothReload: 0.1f,
            reloadAudio: "subtractor_beam_reload_sound", banFromBlessedRuns: true)
          .Attach<SubtractorBeamAmmoDisplay>()
          .InitProjectile(GunData.New(clipSize: 4, cooldown: 0.25f, angleVariance: 5.0f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 0.0f, speed: 300.0f, force: 0.0f, range: 300.0f, customClip: true, spawnSound: "subtractor_beam_fire_sound", uniqueSounds: true))
          .Attach<PierceProjModifier>(pierce => {
            pierce.penetration            = 999;
            pierce.penetratesBreakables   = true; })
          .Attach<SubtractorProjectile>();

        _GreenTrailPrefab = VFX.CreateSpriteTrailObject("subtractor_beam_mid", fps: 60, startAnim: "subtractor_beam_start",
            softMaxLength: 1f, cascadeTimer: C.FRAME, destroyOnEmpty: true);
        _RedTrailPrefab = VFX.CreateSpriteTrailObject("subtractor_beam_red_mid", fps: 60, startAnim: "subtractor_beam_red_start",
            softMaxLength: 1f, cascadeTimer: C.FRAME, destroyOnEmpty: true);
        _HitEffects = VFX.Create("subtractor_beam_hit_effect", 12, scale: 0.5f, emissivePower: 10f);
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        ClearNametags();
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        ClearNametags();
        base.OnDestroy();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        ClearNametags();
        base.OnSwitchedAwayFromThisGun();
    }

    public override void Update()
    {
        base.Update();
        ClearNametags();
        if (this.PlayerOwner is PlayerController player && player.healthHaver && player.healthHaver.IsAlive)
            YouShallKnowTheirNames();
    }

    private void ClearNametags()
    {
        while (_ActiveNametags.Last is LinkedListNode<dfLabel> current)
        {
            _ActiveNametags.RemoveLast();
            if (current.Value is not dfLabel label)
                continue;
            label.IsVisible = false;
            _PooledNametags.AddLast(current);
        }
    }

    private void YouShallKnowTheirNames()
    {
        foreach (AIActor enemy in this.PlayerOwner.CurrentRoom.SafeGetEnemiesInRoom())
        {
            if (!enemy.IsHostile(canBeNeutral: true) || !enemy.sprite)
                continue;
            if (_PooledNametags.Count == 0)
                _PooledNametags.AddLast(CwaffLabel.MakeNewLabel(unicode: false, outline: true));
            LinkedListNode<dfLabel> current = _PooledNametags.Last;
            _PooledNametags.RemoveLast();
            _ActiveNametags.AddLast(current);
            if (current.Value is not dfLabel label)
                label = current.Value = CwaffLabel.MakeNewLabel(unicode: false, outline: true);
            label.Text = $"{Mathf.CeilToInt(enemy.healthHaver.currentHealth)}[sprite \"mini_heart_ui\"]";
            label.Place(enemy.sprite.WorldBottomCenter + new Vector2(0, -1f));
        }
    }

    internal void SetLastEnemy(HealthHaver enemy)
    {
        if (this.Mastered)
            this._lastHitEnemy = enemy;
    }

    internal HealthHaver GetLastEnemy()
    {
        HealthHaver next = this._lastHitEnemy;
        this._lastHitEnemy = null;
        return next;
    }
}

public class SubtractorBeamAmmoDisplay : CustomAmmoDisplay
{
    private Gun _gun;
    private SubtractorBeam _sub;
    private PlayerController _owner;
    private void Start()
    {
        this._gun = base.GetComponent<Gun>();
        this._sub = this._gun.gameObject.GetComponent<SubtractorBeam>();
        this._owner = this._gun.CurrentOwner as PlayerController;
    }

    public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
    {
        if (!this._sub || !this._sub.Mastered)
            return false;

        float damage = this._sub._lastHitEnemy ? this._sub._lastHitEnemy.currentHealth : 0f;
        uic.GunAmmoCountLabel.Text = $"[color #66dd66]{damage}[/color]\n{this._owner.VanillaAmmoDisplay()}";
        return true;
    }
}

public class SubtractorProjectile : MonoBehaviour
{
    const int _NUM_HIT_PARTICLES = 5;

    private Projectile _projectile;
    private PlayerController _owner;

    private HealthHaver _hitFirstEnemy  = null;
    private bool _hitTwoEnemies         = false;
    private float _damage               = 0f;
    private float _postHitDamage        = 0f;
    private CwaffTrailController _trail = null;
    private SubtractorBeam _gun         = null;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;

        if (this._owner.CurrentGun is Gun gun && gun.gameObject.GetComponent<SubtractorBeam>() is SubtractorBeam sub)
        {
            this._gun = sub;
            this._hitFirstEnemy = sub.GetLastEnemy();
            this._damage = Mathf.Max(0f, this._hitFirstEnemy ? this._hitFirstEnemy.currentHealth : 0f);
            this._projectile.baseData.damage = this._damage;
        }

        this._trail = this._projectile.AddTrail(
          this._hitFirstEnemy ? SubtractorBeam._RedTrailPrefab : SubtractorBeam._GreenTrailPrefab);
        this._trail.gameObject.SetGlowiness(100f);

        this._projectile.sprite.renderer.enabled                = false;
        this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
        this._projectile.OnHitEnemy                            += this.OnHitEnemy;
        this._projectile.OnDestruction                         += this.OnDestruction;
    }

    private void OnDestruction(Projectile proj)
    {
        if (this._gun && !this._hitTwoEnemies)
            this._gun.SetLastEnemy(this._hitFirstEnemy);
    }

    private void OnPreRigidbodyCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, SpeculativeRigidbody other, PixelCollider otherPixelCollider)
    {
        if ((other.specRigidbody.GetComponent<AIActor>() is not AIActor enemy) || !enemy.IsHostile(canBeNeutral: true) || this._hitFirstEnemy == enemy.healthHaver)
        {
            PhysicsEngine.SkipCollision = true;
            return;
        }
        if (!this._hitFirstEnemy)
            return;

        this._projectile.ResetPiercing();
        this._postHitDamage = this._damage - enemy.healthHaver.GetCurrentHealth();
    }

    private void OnHitEnemy(Projectile p, SpeculativeRigidbody body, bool wasKiled)
    {
        if ((body.aiActor is not AIActor enemy) || (!enemy.IsHostile(canBeNeutral: true) && !wasKiled))
            return;

        if (!this._hitFirstEnemy)
        {
            this._hitFirstEnemy = enemy.healthHaver;
            this._damage = Mathf.Ceil(this._hitFirstEnemy.GetCurrentHealth());
            this._projectile.baseData.damage = this._damage;
            if (this._trail)
                this._trail.DisconnectFromSpecRigidbody(); // we want to have a red trail after hitting the enemy, but want the old green trail around as well
            this._trail = this._projectile.AddTrail(SubtractorBeam._RedTrailPrefab);
                this._trail.gameObject.SetGlowiness(100f);
            return;
        }

        CwaffVFX.SpawnBurst(prefab: SubtractorBeam._HitEffects, numToSpawn: _NUM_HIT_PARTICLES, basePosition: enemy.CenterPosition,
            positionVariance: 1f, baseVelocity: Vector2.zero, velocityVariance: 1f, velType: CwaffVFX.Vel.Radial,
            lifetime: 0.5f, fadeOutTime: 0.5f);

        enemy.gameObject.PlayUnique("subtractor_beam_impact_sound");

        this._hitTwoEnemies              = true;
        this._damage                     = this._postHitDamage;
        this._projectile.baseData.damage = this._damage;
        if (this._damage <= 0)
            this._projectile.DieInAir();
    }
}
