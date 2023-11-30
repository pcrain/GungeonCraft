namespace CwaffingTheGungy;

public class Crapshooter : AdvancedGunBehavior
{
    public static string ItemName         = "Crapshooter";
    public static string SpriteName       = "crapshooter";
    public static string ProjectileName   = "38_special"; // 19 / grenade_launcher
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static Projectile _BaseCrapshooterProjectile;
    internal static List<string> _DiceSounds = new(){
        "dice_sound_1",
        "dice_sound_2",
        "dice_sound_3",
        "dice_sound_4",
        "dice_sound_5"
    };

    private int _nextRoll = 0; // 1 lower than the die face value

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Crapshooter>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.D, gunClass: GunClass.POISON, reloadTime: 1.5f, ammo: 300);
            gun.SetIdleAnimationFPS(6);
            gun.SetAnimationFPS(gun.shootAnimation, 24);
            gun.SetAnimationFPS(gun.reloadAnimation, 24);
            gun.SetMuzzleVFX(Items.Mailbox); // innocuous muzzle flash effects
            gun.SetFireAudio("crapshooter_shoot_sound");
            gun.SetReloadAudio(_DiceSounds[0], 0, 6, 11, 14, 17, 19, 28);
            gun.SetReloadAudio(_DiceSounds[1], 3, 7, 12, 15, 21, 27);
            gun.SetReloadAudio(_DiceSounds[2], 2, 18, 23, 25, 31);

        _BaseCrapshooterProjectile = gun.InitSpecialProjectile<GrenadeProjectile>(clipSize: 12, cooldown: 0.16f, shootStyle: ShootStyle.Automatic, scale: 2.0f,
          damage: 3f, speed: 24f, force: 10f, range: 30f, sprite: "crapshooter_projectile", fps: 12, anchor: Anchor.MiddleCenter,
          shouldRotate: false
          ).Attach<GrenadeProjectile>(g => {
            g.startingHeight = 0.5f;
          }).Attach<BounceProjModifier>(bounce => {
            bounce.percentVelocityToLoseOnBounce = 0.5f;
            bounce.numberOfBounces = Mathf.Max(bounce.numberOfBounces, 0) + 3;
            bounce.OnBounce += () => {
                AkSoundEngine.PostEvent(_DiceSounds.ChooseRandom(), bounce.gameObject);
            };
          }).Attach<DiceProjectile>(
          );
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        AkSoundEngine.PostEvent(_DiceSounds.ChooseRandom(), base.gameObject);
        projectile.SetFrame(this._nextRoll);
        projectile.DestroyMode = Projectile.ProjectileDestroyMode.BecomeDebris;
        switch (this._nextRoll + 1)
        {
            case 1: // crappy dice
                projectile.baseData.speed  *= 0.5f;
                break;
            case 2: // standard dice
                projectile.baseData.damage *= 2f;
                break;
            case 3: // explosive dice
                projectile.baseData.damage *= 3f;
                projectile.OnDestruction += (Projectile p) => Exploder.Explode(
                    p.sprite.WorldCenter, Bouncer._MiniExplosion, p.Direction);
                break;
            case 4: // homing dice
                projectile.baseData.damage *= 4f;
                projectile.Attach<HomingModifier>(homing => {
                    homing.HomingRadius    = 8f;
                    homing.AngularVelocity = 540f;
                });
                break;
            case 5: // flak dice
                projectile.baseData.damage *= 5f;
                projectile.Attach<SpawnProjModifier>(s => {
                  s.spawnProjectilesOnCollision  = true;
                  s.numberToSpawnOnCollison      = 5;
                  s.startAngle                   = 180;
                  s.projectileToSpawnOnCollision = _BaseCrapshooterProjectile;
                  s.collisionSpawnStyle          = SpawnProjModifier.CollisionSpawnStyle.RADIAL;
                });
                break;
            case 6: // pierce dice
                projectile.baseData.range  *= 1.5f;
                projectile.baseData.speed  *= 1.5f;
                projectile.baseData.damage *= 10f;
                projectile.Attach<PierceProjModifier>(pierce => {
                    pierce.penetration = 6;
                    pierce.penetratesBreakables = true;
                });
                projectile.Attach<EasyTrailBullet>(trail => {
                    trail.StartWidth = 0.1f;
                    trail.EndWidth   = 0f;
                    trail.LifeTime   = 0.25f;
                    trail.BaseColor  = Color.white;
                    trail.StartColor = Color.white;
                    trail.EndColor   = Color.white;
                });
                break;
        }
    }

    private void LateUpdate()
    {
        if (this.gun.spriteAnimator.currentClip.name == this.gun.idleAnimation)
            this._nextRoll = this.gun.spriteAnimator.CurrentFrame;
        else
            this._nextRoll = 0;
        this.gun.RenderInFrontOfPlayer();
    }
}

public class DiceProjectile : MonoBehaviour
{
    private const float _AIR_FRICTION   = 0.985f;
    private const float _MIN_SPEED      = 1.00f;
    private const float _ROTATION_COEFF = 20f;
    private const float _ROT_RATE       = _ROTATION_COEFF * 360f;

    private Projectile _projectile     = null;
    private PlayerController _owner    = null;
    private GrenadeProjectile _grenade = null;
    private float _rotation            = 0.0f;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        this._grenade = base.GetComponent<GrenadeProjectile>();

        this._projectile.spriteAnimator.Stop(); // use our default animation frame
        this._projectile.baseData.speed *= 0.95f + 1.05f * UnityEngine.Random.value; // randomize the velocity slightly
        this._rotation = 360f * UnityEngine.Random.value; // randomize the starting rotation
    }

    private void Update()
    {
        if (!this._projectile || !this._grenade)
            return;

        float newHeight = this._grenade.m_currentHeight + (this._grenade.m_current3DVelocity.z + this._projectile.LocalDeltaTime * -10f) * this._projectile.LocalDeltaTime;
        if (newHeight < 0) // we just bounced, so play some nice dice sounds
            AkSoundEngine.PostEvent(Crapshooter._DiceSounds.ChooseRandom(), base.gameObject);

        this._projectile.baseData.speed *= _AIR_FRICTION;
        if (this._projectile.baseData.speed < _MIN_SPEED)
            this._projectile.DieInAir(suppressInAirEffects: true);
        this._projectile.UpdateSpeed();

        if (!this._projectile.specRigidbody)
            return;

        this._rotation += Mathf.Sign(this._projectile.specRigidbody.Velocity.x) * _ROT_RATE * this._projectile.baseData.speed * BraveTime.DeltaTime;
        this._projectile.transform.localRotation = this._rotation.EulerZ();
    }
}
