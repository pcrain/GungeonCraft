﻿namespace CwaffingTheGungy;

public class Wavefront : CwaffGun
{
    public static string ItemName         = "Wavefront";
    public static string ShortDescription = "Particle-larly Interesting";
    public static string LongDescription  = "Fires bullets that persistently orbit and gravitate towards the player for up to 15 seconds until they collide with an enemy.";
    public static string Lore             = "The primary difficulty of working with projectiles that gravitate towards you is, hopefully unsurprisingly, the fact that those projectiles can hit you. The Gungineer in charge of redesigning this gun to meet modern safety standards came up with a rather ingenious workaround for this issue: do nothing, but claim that you have incorporated proprietary technology that reduces the likelihood of shooting yourself so people will buy it anyway. The redesigned gun received 100% approval from those who survived using it, and the Gungineer received an employee of the year award from management shortly after the redesign went live.";

    internal static GameObject _LinkVFXPrefab = null;

    public static void Init()
    {
        Lazy.SetupGun<Wavefront>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.A, gunClass: GunClass.RIFLE, reloadTime: 1.0f, ammo: 320, idleFps: 12, shootFps: 50, reloadFps: 16,
            muzzleVFX: "muzzle_wavefront", muzzleFps: 30, muzzleAnchor: Anchor.MiddleCenter, muzzleEmission: 10f, fireAudio: "wavefront_fire_sound",
            attacksThroughWalls: true, smoothReload: 0.1f)
          .SetReloadAudio("wavefront_reload_sound", 0, 6, 12, 18)
          .InitProjectile(GunData.New(damage: 12f, clipSize: 12, cooldown: 0.125f, shootStyle: ShootStyle.Automatic, range: 999999f, speed: 60f, shouldRotate: true,
            customClip: true, sprite: "wavefront_projectile_alt", scale: 0.25f, fps: 24, glowAmount: 4f, glowColor: Color.cyan, lightStrength: 10f, lightRange: 1f,
            lightColor: Color.white, hitEnemySound: "wavefront_projectile_zap_sound", uniqueSounds: true))
          .SetEnemyImpactVFX(VFX.CreatePool("wavefront_impact_particles", fps: 24, loops: false, anchor: Anchor.MiddleCenter, lightColor: Color.cyan,
            lightRange: 1.5f, lightStrength: 3.0f, emissivePower: 0.5f, emissiveColorPower: 0.5f, emissiveColour: ExtendedColours.purple))
          .Attach<TeslaProjectileBehavior>();

      _LinkVFXPrefab = VFX.Create("wavefront_lightning", fps: 60, loops: true, anchor: Anchor.MiddleLeft).MakeChainLightingVFX();
    }
}

public class TeslaProjectileBehavior : MonoBehaviour
{
    private const float _LIFESPAN   =  15.0f;
    private const float _ACCEL      = 350.0f;
    private const float _MIN_SPEED  =  50.0f;
    private const float _MAX_SPEED  =  70.0f;
    private const float _PRECESSION =   1.0f; // speed at which projectiles change their angle of orbit around the player
    private const float _MIN_ION_TIME = 0.5f;
    private const float _ION_VARIANCE = 0.5f;
    private const float _ION_DAMAGE   = 1.5f;

    private static readonly Color _TrailColor = new Color(0.35f, 1.0f, 1.0f, 0.35f);
    private static readonly List<TeslaProjectileBehavior> _ExtantTeslas = new();
    private static List<AIActor> _ZappableEnemies = new();

    private Projectile _projectile;
    private PlayerController _owner;
    private float _myMaxSpeed;
    private float _lifespan;
    private bool _mastered;
    private float _ionizationTimer;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner      = this._projectile.Owner as PlayerController;
        if (!this._owner)
            return;
        this._myMaxSpeed = UnityEngine.Random.Range(_MIN_SPEED, _MAX_SPEED) * this._owner.ProjSpeedMult();
        this._lifespan   = _LIFESPAN;

        this._projectile.specRigidbody.CollideWithTileMap = false;
        this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
        EasyTrailBullet trail = this._projectile.AddComponent<EasyTrailBullet>();
            trail.StartWidth = 0.1f;
            trail.EndWidth   = 0f;
            trail.LifeTime   = 0.15f;
            trail.BaseColor  = _TrailColor;
            trail.StartColor = _TrailColor;
            trail.EndColor   = _TrailColor;

        this._mastered = this._owner.HasSynergy(Synergy.MASTERY_WAVEFRONT);
        if (this._mastered)
        {
            this._ionizationTimer = _MIN_ION_TIME + _ION_VARIANCE * UnityEngine.Random.value;
            _ExtantTeslas.Add(this);
        }
    }

    private void OnDestroy()
    {
        _ExtantTeslas.TryRemove(this);
    }

    private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (otherRigidbody.GetComponent<AIActor>() is not AIActor enemy || !enemy.IsHostile(canBeNeutral: true))
            PhysicsEngine.SkipCollision = true;
        else
            base.gameObject.Play("wavefront_projectile_impact_sound");
    }

    private void Update()
    {
        if (!this._owner || !this._projectile)
        {
            UnityEngine.Object.Destroy(this);
            return;
        }

        float dtime = BraveTime.DeltaTime;
        if ((this._lifespan -= dtime) <= 0.0f)
        {
            this._projectile.DieInAir();
            return;
        }

        this._projectile.OverrideMotionModule = null; // get rid of Helix Bullets and the like

        Vector2 oldVel    = this._projectile.m_currentSpeed * this._projectile.m_currentDirection;
        Vector2 playerVec = (this._owner.CenterPosition - this._projectile.transform.position.XY());
        Vector2 newVel    = oldVel + _ACCEL * BraveTime.DeltaTime * playerVec.normalized.Rotate(_PRECESSION);

        this._projectile.SetSpeed(Mathf.Min(this._myMaxSpeed, newVel.magnitude));
        this._projectile.SendInDirection(newVel, true);

        if (!this._mastered)
            return;
        if ((this._ionizationTimer -= dtime) > 0)
            return;

        this._ionizationTimer = _MIN_ION_TIME + _ION_VARIANCE * UnityEngine.Random.value;
        bool didZap = false;
        TeslaProjectileBehavior t = _ExtantTeslas.ChooseRandom();
        if (t && t != this && t._projectile && t._projectile.specRigidbody)
        {
            DoTeslaZaps(this._projectile, t._projectile.specRigidbody);
            didZap = true;
        }
        AIActor nearestEnemy = Lazy.NearestEnemy(this._projectile.specRigidbody.UnitCenter);
        if (nearestEnemy && nearestEnemy.specRigidbody is SpeculativeRigidbody enemyBody)
        {
            DoTeslaZaps(this._projectile, enemyBody);
            didZap = true;
        }
        if (didZap)
            this._projectile.gameObject.PlayUnique("wavefront_zap_sound");
    }

    private static void DoTeslaZaps(Projectile proj, SpeculativeRigidbody target)
    {
        OwnerConnectLightningModifier zap = proj.gameObject.AddComponent<OwnerConnectLightningModifier>();
        zap.owner         = proj.Owner;
        zap.targetBody    = target;
        zap.originPos     = proj.specRigidbody.UnitCenter;
        zap.targetPos     = target.UnitCenter;
        zap.linkPrefab    = Wavefront._LinkVFXPrefab;
        zap.disownTimer   = 0.1f;
        zap.fadeTimer     = 0.1f;
        zap.color         = Color.cyan;
        zap.emissivePower = 20f;
        zap.baseDamage    = _ION_DAMAGE;
        zap.MakeGlowy();
    }
}
