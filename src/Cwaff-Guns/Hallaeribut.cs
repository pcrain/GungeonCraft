namespace CwaffingTheGungy;

using static Hallaeribut.State;

public class Hallaeribut : CwaffGun
{
    public static string ItemName         = "Hallaeribut";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private static readonly float[] _AmmoThresholds = [1.0f, 0.75f, 0.5f, 0.25f];
    internal enum State { SATIATED, PECKISH, HUNGRY, FAMISHED }

    internal static GameObject _BiteVFX;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Hallaeribut>(ItemName, ShortDescription, LongDescription, Lore, hideFromAmmonomicon: true)
          .SetAttributes(quality: ItemQuality.SPECIAL, gunClass: CwaffGunClass.UTILITY, reloadTime: 0f, ammo: 80, shootFps: 30, reloadFps: 40,
            muzzleVFX: "muzzle_hallaeribut", muzzleFps: 30, muzzleScale: 0.5f, fireAudio: "chomp_small_sound", infiniteAmmo: true, modulesAreTiers: true);

        Projectile proj = gun.InitProjectile(GunData.New(sprite: "hallaeribut_projectile", fps: 24, scale: 0.75f, clipSize: 32, cooldown: 0.33f,
            shootStyle: ShootStyle.Burst, angleVariance: 20f, damage: 4.0f, speed: 75f, range: 1000f, force: 12f, burstCooldown: 0.04f))
          .AudioEvent("snap_sound", 0)
          .Attach<HallaeributProjectile>();

        proj.AddTrailToProjectilePrefab("hallaeribut_trail", fps: 24, cascadeTimer: C.FRAME, softMaxLength: 1f, destroyOnEmpty: false);

        ProjectileModule mod = gun.DefaultModule;
        gun.Volley.projectiles = new(4);
        for (int i = 1; i <= 4; ++i)
        {
            ProjectileModule newMod = ProjectileModule.CreateClone(mod, inheritGuid: false);
            newMod.numberOfShotsInClip = i * 3 * 5;
            newMod.burstShotCount = i * 3;
            newMod.projectiles = Enumerable.Repeat<Projectile>(proj, i).ToList();
            gun.Volley.projectiles.Add(newMod);
        } //REFACTOR: burst builder

        _BiteVFX = VFX.Create("bite_vfx", fps: 40, loops: false, scale: 0.33f);
    }

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner || !this.PlayerOwner.AcceptingNonMotionInput)
            return;
        // if (this.gun.CurrentStrengthTier != 1)
        //   this.gun.CurrentStrengthTier = 1;
    }
}

public class HallaeributProjectile : MonoBehaviour
{
    private const float _DECEL_START = 0.05f;
    private const float _HALT_START  = 0.25f;
    private const float _RELAUNCH_START  = 0.5f;
    private const float _LERP_RATE = 10f;

    private Projectile _projectile;
    private float _lifetime = 0f;
    private State _state = State.START;
    private float _startSpeed;
    private AIActor _target;

    private enum State
    {
        START,
        DECEL,
        HALT,
        RELAUNCH,
    }

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._projectile.OnHitEnemy += this.OnHitEnemy;
        // this._projectile.spriteAnimator.Pause();

        this._startSpeed = this._projectile.baseData.speed;
    }

    private void OnHitEnemy(Projectile arg1, SpeculativeRigidbody arg2, bool arg3)
    {
        Vector2 center = (arg2.sprite is tk2dBaseSprite sprite) ? sprite.WorldCenter : arg2.UnitCenter;
        CwaffVFX.Spawn(prefab: Hallaeribut._BiteVFX, position: center, lifetime: 0.4f, fadeOutTime: 0.1f);
        arg2.gameObject.Play("chomp_large_sound");
    }

    private void Update()
    {
        this._lifetime += BraveTime.DeltaTime;
        switch (this._state)
        {
            case State.START:
                if (this._lifetime >= _DECEL_START)
                    this._state = State.DECEL;
                break;
            case State.DECEL:
                if (this._lifetime >= _HALT_START)
                {
                    this._projectile.baseData.speed = 0.01f;
                    this._target = Lazy.NearestEnemy(this._projectile.SafeCenter);
                    this._state = State.HALT;
                }
                else
                  this._projectile.baseData.speed = Lazy.SmoothestLerp(this._projectile.baseData.speed, 0f, _LERP_RATE);
                this._projectile.UpdateSpeed();
                break;
            case State.HALT:
                if (this._lifetime >= _RELAUNCH_START)
                {
                    if (this._target)
                        this._projectile.SendInDirection(this._target.CenterPosition - this._projectile.SafeCenter, true);
                    this._projectile.UpdateSpeed();
                    this._state = State.RELAUNCH;
                    // this._projectile.spriteAnimator.Resume();
                }
                break;
            case State.RELAUNCH:
                this._projectile.baseData.speed = Lazy.SmoothestLerp(this._projectile.baseData.speed, this._startSpeed, _LERP_RATE);
                this._projectile.UpdateSpeed();
                break;
        }
    }
}
