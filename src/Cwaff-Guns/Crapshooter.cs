namespace CwaffingTheGungy;

public class Crapshooter : CwaffGun
{
    public static string ItemName         = "Crapshooter";
    public static string ShortDescription = "Reloaded Dice";
    public static string LongDescription  = "Shoots a die whose face value corresponds to the one currently shown on the gun. Dice have different effects corresponding to their face value: 1) weak + slow, 2) normal, 3) explosive, 4) homing, 5) flak, 6) strong + piercing. The face shown on the gun cycles from 1 to 6, and shooting immediately resets the value to 1, so shots may be timed to repeatedly roll the desired value.";
    public static string Lore             = "Dice are the core component of several games, ranging from family-friendly ones (like Monopoly) to those that inevitably end in homicide (like Monopoly). The dice themselves are rarely the vessels used to carry out these acts of violence, but when stuffed inside a firearm, they prove to be at least somewhat effective projectiles, if not unpredictable ones.";

    internal static Projectile _BaseCrapshooterProjectile;
    internal static readonly List<string> _DiceSounds = new(){
        "dice_sound_1",
        "dice_sound_2",
        "dice_sound_3",
        "dice_sound_4",
        "dice_sound_5"
    };

    private int _nextRoll = 0; // 1 lower than the die face value
    private float _freezeTimer = 0.0f;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Crapshooter>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.D, gunClass: GunClass.PISTOL, reloadTime: 1.5f, ammo: 300, idleFps: 6, shootFps: 24,
                reloadFps: 24, muzzleFrom: Items.Mailbox, fireAudio: "crapshooter_shoot_sound");
            gun.SetReloadAudio(_DiceSounds[0], 0, 6, 11, 14, 17, 19, 28);
            gun.SetReloadAudio(_DiceSounds[1], 3, 7, 12, 15, 21, 27);
            gun.SetReloadAudio(_DiceSounds[2], 2, 18, 23, 25, 31);

        _BaseCrapshooterProjectile = gun.InitSpecialProjectile<GrenadeProjectile>(GunData.New(clipSize: 12, cooldown: 0.16f,
            shootStyle: ShootStyle.Automatic, scale: 2.0f, damage: 3f, speed: 24f, force: 10f, range: 30f, customClip: true,
            sprite: "crapshooter_projectile", fps: 12, anchor: Anchor.MiddleCenter, shouldRotate: false
          )).Attach<GrenadeProjectile>(g => {
            g.startingHeight = 0.5f;
          }).Attach<BounceProjModifier>(bounce => {
            bounce.percentVelocityToLoseOnBounce = 0.5f;
            bounce.numberOfBounces = Mathf.Max(bounce.numberOfBounces, 0) + 3;
            bounce.OnBounce += () => {
                bounce.gameObject.Play(_DiceSounds.ChooseRandom());
            };
          }).Attach<DiceProjectile>(
          );
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);

        if (this.PlayerOwner && this.PlayerOwner.PlayerHasActiveSynergy(Synergy.MASTERY_CRAPSHOOTER))
            this._freezeTimer = 0.25f; //NOTE: needs to be long enough that idle animation doesn't play in between shots

        base.gameObject.Play(_DiceSounds.ChooseRandom());
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
        this.gun.RenderInFrontOfPlayer();
        if ((this._freezeTimer -= BraveTime.DeltaTime) > 0.0f)
            return;

        if (this.gun.spriteAnimator.currentClip.name == this.gun.idleAnimation)
            this._nextRoll = this.gun.spriteAnimator.CurrentFrame;
        else
            this._nextRoll = 0;
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
            base.gameObject.Play(Crapshooter._DiceSounds.ChooseRandom());

        this._projectile.ApplyFriction(_AIR_FRICTION);
        if (this._projectile.baseData.speed < _MIN_SPEED)
            this._projectile.DieInAir(suppressInAirEffects: true);

        if (!this._projectile.specRigidbody)
            return;

        this._rotation += Mathf.Sign(this._projectile.specRigidbody.Velocity.x) * _ROT_RATE * this._projectile.baseData.speed * BraveTime.DeltaTime;
        this._projectile.transform.localRotation = this._rotation.EulerZ();
    }
}
