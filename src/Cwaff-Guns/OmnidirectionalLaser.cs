namespace CwaffingTheGungy;

/*
   TODO
    - #BUG: fix per character carry offsets (only works with robot for now)
    - add extra pointer mechanics maybe
*/

public class OmnidirectionalLaser : AdvancedGunBehavior
{
    public static string ItemName         = "Omnidirectional Laser";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Hula Hooplah";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const int _BASE_FPS          = 8;
    private const int _MAX_FPS           = 32;
    private const int _FPS_STEP          = 1;
    private const float _FPS_RESET_TIME  = 1.0f; // how long after firing we start slowing the reticle down
    private const float _FPS_RESET_SPEED = 0.125f; // time increment between stepping down the reticle's speed
    private const float _LOCKON_FACTOR   = 2f; // if we fire a laser within this many (FPS * _LOCKON_FACTOR) degrees of an enemy, snap to the enemy

    private static List<int> _BackSpriteIds = new();
    private static List<Vector3> _BarrelOffsets = new();
    private static GameObject _OmniReticle  = null;
    private static TrailController _OmniTrailPrefab  = null;

    private tk2dSprite _backside = null;
    private tk2dSprite _reticle = null;
    private Vector2 _laserAngle = Vector2.zero;
    private int _currentFps = _BASE_FPS;
    private float _timeSinceLastShot = 0.0f;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<OmnidirectionalLaser>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.SILLY, reloadTime: 0.0f, ammo: 250/*, defaultAudio: true*/);
            gun.SetAnimationFPS(gun.idleAnimation, _BASE_FPS);
            gun.SetAnimationFPS(gun.shootAnimation, _BASE_FPS);
            gun.LoopAnimation(gun.shootAnimation);
            gun.SetFireAudio("omni_spin_sound", 0, 1, 2, 3, 4, 5, 6, 7);
            gun.AddFlippedCarryPixelOffsets(offset: new IntVector2(5, -4), flippedOffset: new IntVector2(4, -4));
            gun.gunHandedness      = GunHandedness.NoHanded;
            gun.muzzleFlashEffects = null;
            gun.preventRotation    = true;
            gun.reloadAnimation    = null; // animation shouldn't automatically change when reloading
            gun.shootAnimation     = null; // animation shouldn't automatically change when firing
            gun.PreventOutlines    = true; // messes up with two-part rendering

        for (int i = 1; i <= 8; ++i)
        {
            foreach (tk2dSpriteDefinition.AttachPoint a in gun.AttachPointsForClip(gun.idleAnimation, frame: i - 1).EmptyIfNull())
                if (a.name == "Casing")
                    _BarrelOffsets.Add(a.position);
            _BackSpriteIds.Add(gun.sprite.Collection.GetSpriteIdByName($"omnidirectional_laser_fire_back_00{i}"));
        }

        gun.InitProjectile(GunData.New(sprite: "omnilaser_projectile", clipSize: -1, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic,
            speed: 200f, damage: 16f));

        _OmniTrailPrefab = VFX.CreateTrailObject(ResMap.Get("omnilaser_projectile_trail")[0], new Vector2(23, 4), new Vector2(0, 0),
            ResMap.Get("omnilaser_projectile_trail"), 60, cascadeTimer: C.FRAME, destroyOnEmpty: true);

        _OmniReticle = VFX.Create("omnilaser_reticle");
    }

    public override void Start()
    {
        base.Start();
        tk2dSprite backSprite = this._backside = new GameObject().AddComponent<tk2dSprite>();
        backSprite.SetSprite(this.gun.sprite.Collection, this.gun.sprite.Collection.GetSpriteIdByName($"{gun.InternalName()}_idle_back_001"));
        backSprite.transform.position = gun.transform.position;
        // backSprite.transform.parent = gun.transform;  //NOTE: prevents renderer from disabling properly
        backSprite.HeightOffGround = -0.5f;
        backSprite.UpdateZDepth();
        gun.sprite.AttachRenderer(backSprite);

        this._reticle = SpawnManager.SpawnVFX(_OmniReticle, this.gun.transform.position, Quaternion.identity).GetComponent<tk2dSprite>();
        this._reticle.SetAlphaImmediate(0.0f);
    }

    public override void OnDestroy()
    {
        this._backside.gameObject.SafeDestroy(); //WARNING: verify this works when loading new levels due to being unparented
        this._backside = null;
        this._reticle.gameObject.SafeDestroy();
        this._reticle = null;
        base.OnDestroy();
    }

    private void LateUpdate()
    {
        this.gun.m_prepThrowTime = -999f; //HACK: prevent the gun from being thrown (the sprite looks ridiculous when rotated)

        if ((this._timeSinceLastShot += BraveTime.DeltaTime) > _FPS_RESET_TIME && (this._currentFps >= _BASE_FPS))
        {
            this._timeSinceLastShot -= _FPS_RESET_SPEED;
            this._currentFps = Math.Max(this._currentFps - _FPS_STEP, _BASE_FPS);
            gun.spriteAnimator.ClipFps = this._currentFps;
        }

        bool shouldRender
            = this._backside.renderer.enabled
            = this.gun.m_meshRenderer.enabled;
        if (!shouldRender)
        {
            this._reticle.SetAlpha(0.0f);
            return;
        }

        this._reticle.SetAlpha(this.Player ? 1.0f : 0.0f);
        this._backside.transform.position = this.gun.transform.position;
        this._backside.HeightOffGround = -0.5f;
        this._backside.UpdateZDepth();
        int frame = this.gun.spriteAnimator.CurrentFrame;
        this._backside.sprite.SetSprite(_BackSpriteIds[frame]);
        this.gun.barrelOffset.localPosition = _BarrelOffsets[frame];  //NOTE: update the barrel offset for each specific frame

        if (!this.Player)
            return;

        // play the fire animation at all times while the gun is being held
        tk2dSpriteAnimationClip clip = gun.spriteAnimator.GetClipByName($"{gun.InternalName()}_fire");
        if (gun.spriteAnimator.currentClip != clip)
        {
            gun.spriteAnimator.currentClip = clip;
            gun.spriteAnimator.Play();
        }

        //NOTE: using a 22.5 degree offset from straight down (270) so the middle of each animation frame corresponds to the visual angle,
        //      rather than the beginning of the animation frame corresponding to that angle
        this._laserAngle = (292.5f - (45f * this.gun.spriteAnimator.clipTime)).ToVector();
        this._reticle.transform.position = this.Player.CenterPosition + 1.5f * this._laserAngle;
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        if (!this.Player)
            return;
        this._backside.renderer.enabled = false;
        this._reticle.SetAlpha(0.0f);
        // this._reticle.renderer.enabled = false;  //NOTE: doesn't work since it's parented
        this.Player.forceAimPoint = null;
        base.OnSwitchedAwayFromThisGun();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);

        Vector2? targetPos = Lazy.NearestEnemyWithinConeOfVision(
            start                            : this.gun.barrelOffset.position,
            coneAngle                        : this._laserAngle.ToAngle().Clamp360(),
            maxDeviation                     : _LOCKON_FACTOR * this._currentFps,  // between 16 and 64 degrees of lock-on
            useNearestAngleInsteadOfDistance : true,
            ignoreWalls                      : false
            );
        Vector2 angle =  targetPos.HasValue
            ? (targetPos.Value - this.gun.barrelOffset.position.XY())
            : this._laserAngle;
        projectile.SendInDirection(angle, true, true);
        projectile.AddTrailToProjectileInstance(_OmniTrailPrefab).gameObject.SetGlowiness(10f);
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        gun.gameObject.PlayUnique("omnilaser_shoot_sound");
        this._timeSinceLastShot = 0.0f;
        this._currentFps = Math.Min(this._currentFps + _FPS_STEP, _MAX_FPS);
        gun.spriteAnimator.ClipFps = this._currentFps;
    }
}
