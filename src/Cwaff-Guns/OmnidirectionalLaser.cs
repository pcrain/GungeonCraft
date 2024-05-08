namespace CwaffingTheGungy;

/*
   TODO
    - add extra pointer mechanics maybe
*/

public class OmnidirectionalLaser : CwaffGun
{
    public static string ItemName         = "Omnidirectional Laser";
    public static string ShortDescription = "Hula Hooplah";
    public static string LongDescription  = "Fires a strong laser in the direction of an oribting targeting reticle. The reticle orbits more quickly each time a laser is fired, and cools down after a short period of inactivity.";
    public static string Lore             = "Initially designed with hundreds of AI-guided lasers arounds its perimeter, budget cuts during production led to replacing the hundreds of lasers with a single laser that circled around the weapon's perimeter. Further budget cuts let to the removal of the AI targeting system, resulting in a final product that, while potent, is incredibly inconvenient to aim.";

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
        Gun gun = Lazy.SetupGun<OmnidirectionalLaser>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.SILLY, reloadTime: 0.0f, ammo: 250,
                idleFps: _BASE_FPS, shootFps: _BASE_FPS, loopFireAt: 0, preventRotation: true);
            gun.SetFireAudio("omni_spin_sound", 0, 1, 2, 3, 4, 5, 6, 7);
            gun.SuppressReloadAnimations();
            gun.AddFlippedCarryPixelOffsets(offset: new IntVector2(5, -4), flippedOffset: new IntVector2(4, -4),
                offsetPilot:       new IntVector2(5, -4), flippedOffsetPilot:       new IntVector2(5, -4),
                offsetConvict:     new IntVector2(5, -4), flippedOffsetConvict:     new IntVector2(5, -4),
                offsetRobot:       new IntVector2(5, -4), flippedOffsetRobot:       new IntVector2(4, -4),
                offsetSoldier:     new IntVector2(6, -4), flippedOffsetSoldier:     new IntVector2(6, -4),
                offsetGuide:       new IntVector2(7, -4), flippedOffsetGuide:       new IntVector2(7, -4),
                offsetCoopCultist: new IntVector2(5, -4), flippedOffsetCoopCultist: new IntVector2(5, -4),  //TODO: verify
                offsetBullet:      new IntVector2(8, -4), flippedOffsetBullet:      new IntVector2(8, -4),
                offsetEevee:       new IntVector2(5, -4), flippedOffsetEevee:       new IntVector2(5, -4),  //no one good offset for this character, so deal with a good average
                offsetGunslinger:  new IntVector2(5, -4), flippedOffsetGunslinger:  new IntVector2(5, -4)   //TODO: verify
                );
            gun.gunHandedness           = GunHandedness.NoHanded;
            gun.reloadAnimation         = gun.idleAnimation; // animation shouldn't automatically change when reloading
            gun.shootAnimation          = null; // animation shouldn't automatically change when firing
            gun.PreventOutlines         = true; // messes up with two-part rendering
            gun.OnlyUsesIdleInWeaponBox = true; // fixes half-sprite from appearing in weapon box

        for (int i = 1; i <= 8; ++i)
        {
            //NOTE: can't use gun.idleAnimation since it uses the trimmed variant by default
            foreach (tk2dSpriteDefinition.AttachPoint a in gun.AttachPointsForClip("omnidirectional_laser_idle", frame: i - 1).EmptyIfNull())
                if (a.name == "Casing")
                    _BarrelOffsets.Add(a.position);
            _BackSpriteIds.Add(gun.sprite.Collection.GetSpriteIdByName($"omnidirectional_laser_fire_back_00{i}"));
        }

        gun.InitProjectile(GunData.New(sprite: "omnilaser_projectile", clipSize: -1, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic,
            speed: 200f, damage: 16f, spawnSound: "omnilaser_shoot_sound", uniqueSounds: true));

        _OmniTrailPrefab = VFX.CreateTrailObject(ResMap.Get("omnilaser_projectile_trail")[0], new Vector2(23, 4), new Vector2(0, 0),
            ResMap.Get("omnilaser_projectile_trail"), 60, cascadeTimer: C.FRAME, softMaxLength: 1f, destroyOnEmpty: true);

        _OmniReticle = VFX.Create("omnilaser_reticle");
    }

    private void CreateRenderersIfNecessary()
    {
        if (!this.PlayerOwner)
            return;
        if (this.PlayerOwner.CurrentInputState != PlayerInputState.AllInput)
            return; // don't create a renderer until we're in full control of our character

        if (!this._reticle)
        {
            GameObject reticleObject = SpawnManager.SpawnVFX(_OmniReticle, this.gun.transform.position, Quaternion.identity);
            if (!reticleObject)
                return;  // SpawnManager is not active
            this._reticle = reticleObject.GetComponent<tk2dSprite>();
            this._reticle.SetAlphaImmediate(0.0f);
        }

        if (!this._backside)
        {
            ETGModConsole.Log($"new backside");
            this._backside = Lazy.SpriteObject(
                spriteColl: this.gun.sprite.Collection,
                spriteId: this.gun.sprite.Collection.GetSpriteIdByName($"{this.gun.InternalSpriteName()}_idle_back_001"));
            this._backside.transform.position = gun.transform.position;
            this._backside.transform.parent = gun.transform;
            this._backside.HeightOffGround = -0.5f;
            this._backside.UpdateZDepth();
            this.gun.sprite.AttachRenderer(this._backside);
        }
    }

    public override void OnDestroy()
    {
        if (this._backside)
        {
            this.gun.sprite.DetachRenderer(this._backside);
            this._backside.transform.parent = null;
            UnityEngine.Object.Destroy(this._backside.gameObject);
        }
        this._backside = null;
        if (this._reticle)
            this._reticle.gameObject.SafeDestroy();
        this._reticle = null;
        base.OnDestroy();
    }

    public override void Update()
    {
        base.Update();
        CreateRenderersIfNecessary();
    }

    private void LateUpdate()
    {
        if (!this._reticle || !this._backside)
            return; // nothing to do

        if (!this.PlayerOwner) // if we have no player, remove our render parent since it can be destroyed and cause ghost sprites when picked back up
        {
            this._backside.transform.position = this.gun.transform.position;
            this._backside.transform.parent = null;
            return;
        }

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
            this._backside.transform.parent = null;
            this._reticle.SetAlpha(0.0f);
            return;
        }

        this._reticle.SetAlpha(this.PlayerOwner ? 1.0f : 0.0f);
        if (!this._backside.transform.parent)
        {
            this._backside.transform.parent = this.gun.transform;
            this._backside.transform.position = this.gun.transform.position;
        }
        this._backside.HeightOffGround = -0.5f;
        this._backside.UpdateZDepth();
        int frame = this.gun.spriteAnimator.CurrentFrame;
        this._backside.sprite.SetSprite(_BackSpriteIds[frame]);
        this.gun.barrelOffset.localPosition = _BarrelOffsets[frame];  //NOTE: update the barrel offset for each specific frame

        if (!this.PlayerOwner)
            return;

        // play the fire animation at all times while the gun is being held
        tk2dSpriteAnimationClip clip = gun.spriteAnimator.GetClipByName($"{gun.InternalSpriteName()}_fire");
        if (gun.spriteAnimator.currentClip != clip)
        {
            gun.spriteAnimator.currentClip = clip;
            gun.spriteAnimator.Play();
        }

        //NOTE: using a 22.5 degree offset from straight down (270) so the middle of each animation frame corresponds to the visual angle,
        //      rather than the beginning of the animation frame corresponding to that angle
        this._laserAngle = (292.5f - (45f * this.gun.spriteAnimator.clipTime)).ToVector();
        this._reticle.transform.position = this.PlayerOwner.CenterPosition + 1.5f * this._laserAngle;
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        if (!this.PlayerOwner)
            return;
        if (this._backside && this._backside.renderer)
        {
            this._backside.transform.parent = null;
            this._backside.renderer.enabled = false;
        }
        if (this._reticle)
            this._reticle.SetAlpha(0.0f);
        // this._reticle.renderer.enabled = false;  //NOTE: doesn't work since it's parented
        this.PlayerOwner.forceAimPoint = null;
        base.OnSwitchedAwayFromThisGun();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        Vector2? targetPos = Lazy.NearestEnemyWithinConeOfVision(
            start                            : this.gun.barrelOffset.position,
            coneAngle                        : projectile.OriginalDirection(),
            maxDeviation                     : _LOCKON_FACTOR * this._currentFps, // between 16 and 64 degrees of lock-on
            useNearestAngleInsteadOfDistance : true,
            ignoreWalls                      : false
            );
        if (targetPos.HasValue)
            projectile.SendInDirection((targetPos.Value - this.gun.barrelOffset.position.XY()), true, true);
        projectile.AddTrailToProjectileInstance(_OmniTrailPrefab).gameObject.SetGlowiness(10f);
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        this._timeSinceLastShot = 0.0f;
        this._currentFps = Math.Min(this._currentFps + _FPS_STEP, _MAX_FPS);
        gun.spriteAnimator.ClipFps = this._currentFps;
    }

    private static float ForceGunAngle(Gun gun, float oldAngle)
    {
        return (gun.GetComponent<OmnidirectionalLaser>() is OmnidirectionalLaser omni)
            ? omni._laserAngle.ToAngle() : oldAngle;
    }

    [HarmonyPatch(typeof(Gun), nameof(Gun.HandleAimRotation))]
    private class OmnidirectionalLaserAimPatch
    {
        [HarmonyILManipulator]
        private static void OmnidirectionalLaserAimIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            int num2 = 0;
            if (!cursor.TryGotoNext(MoveType.Before,
                instr => instr.MatchLdloc(out num2), // num2 is unmodified aim angle
                instr => instr.MatchStfld<Gun>("prevGunAngleUnmodified")))
                return;

            ++cursor.Index; // move right before the store to prevGunAngleUnmodified (Gun is already on stack)
            cursor.Emit(OpCodes.Ldarg_0); // the gun itself
            cursor.Emit(OpCodes.Ldloc_S, (byte)num2); // num2 is unmodified aim angle
            cursor.Emit(OpCodes.Call, typeof(OmnidirectionalLaser).GetMethod("ForceGunAngle", BindingFlags.Static | BindingFlags.NonPublic));
            cursor.Emit(OpCodes.Stloc_S, (byte)num2); // num2 is unmodified aim angle
        }
    }
}
