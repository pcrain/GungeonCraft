namespace CwaffingTheGungy;

/*
   TODO
    - add proper grounded animation without black outline in the middle
    - add proper projectile with trail / sound
    - add extra pointer mechanics
    - fix per character offsets if necessary
    - come up with a better name potentially
*/

public class OmnidirectionalLaser : AdvancedGunBehavior
{
    public static string ItemName         = "Omnidirectional Laser";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Spin to Win";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private static List<int> _BackSpriteIds = new();
    private static List<Vector3> _BarrelOffsets = new();
    private static GameObject _OmniReticle  = null;

    private tk2dSprite _backside = null;
    private tk2dSprite _reticle = null;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<OmnidirectionalLaser>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.SILLY, reloadTime: 0.0f, ammo: 150/*, defaultAudio: true*/);
            gun.SetAnimationFPS(gun.idleAnimation, 10);
            gun.SetIdleAudio("omni_spin_sound", 0, 1, 2, 3, 4, 5, 6, 7);
            gun.AddFlippedCarryPixelOffsets(offset: new IntVector2(5, -4), flippedOffset: new IntVector2(4, -4));
            gun.gunHandedness      = GunHandedness.NoHanded;
            gun.muzzleFlashEffects = null;
            gun.preventRotation    = true;
            gun.reloadAnimation    = null; // animation shouldn't change when reloading
            gun.shootAnimation     = null; // animation shouldn't change when firing
            gun.PreventOutlines    = true; // messes up with two-part rendering


        for (int i = 1; i <= 8; ++i)
        {
            foreach (tk2dSpriteDefinition.AttachPoint a in gun.AttachPointsForClip(gun.idleAnimation, frame: i - 1).EmptyIfNull())
                if (a.name == "Casing")
                {
                    ETGModConsole.Log($"  added barrel offset at {a.position}");
                    _BarrelOffsets.Add(a.position);
                }
            _BackSpriteIds.Add(gun.sprite.Collection.GetSpriteIdByName($"omnidirectional_laser_idle_back_00{i}"));
        }

        gun.InitProjectile(GunData.New(clipSize: -1, cooldown: 0.2f, shootStyle: ShootStyle.SemiAutomatic, speed: 200f));

        _OmniReticle = VFX.Create("omnilaser_reticle", fps: 2, /*scale: 2.0f, */loops: true, anchor: Anchor.MiddleCenter);
    }

    public override void Start()
    {
        base.Start();
        tk2dSprite backSprite = this._backside = new GameObject().AddComponent<tk2dSprite>();
        backSprite.SetSprite(this.gun.sprite.Collection, this.gun.sprite.Collection.GetSpriteIdByName("omnidirectional_laser_idle_back_001"));
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

        float laserAngle = (45f * (14 - this.gun.spriteAnimator.CurrentFrame)).Clamp360();
        this._reticle.transform.position = this.Player.CenterPosition + laserAngle.ToVector(1.5f);
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        if (!this.Player)
            return;
        this._backside.renderer.enabled = false;
        this._reticle.SetAlpha(0.0f);
        // this._reticle.renderer.enabled = false;
        this.Player.forceAimPoint = null;
        base.OnSwitchedAwayFromThisGun();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);

        //subtract the current frame since we're counterclockwise, and use 14 to start at a 270 degree angle
        float laserAngle = (45f * (14 - this.gun.spriteAnimator.CurrentFrame)).Clamp360();
        projectile.SendInDirection(laserAngle.ToVector(), true, true);
        projectile.AddTrailToProjectileInstance(ChekhovsGun._ChekhovTrailPrefab).gameObject.SetGlowiness(10f);

        this.Owner.gameObject.PlayUnique("chekhovs_gun_launch_sound_alt");
    }
}
