namespace CwaffingTheGungy;

/* TODO:
    - figure out nicely drawing while out of bounds
*/

public class Gunbrella : AdvancedGunBehavior
{
    public static string ItemName         = "Gunbrella";
    public static string SpriteName       = "gunbrella";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Cloudy with a Chance of Pain";
    public static string LongDescription  = "Charging and releasing fires projectiles that hail from the sky at the cursor's position.";
    public static string Lore             = "A normal umbrella that was genetically modified to fire bullets, older models fired projectiles from the front much like a traditional firearm. Gungeoneers quickly grew frustrated at being unable to actually see where they were shooting at due to the Gunbrella's large frame. With modern advances in technology and magic, newer models include a touchscreen and GPS that allows the user to target enemies directly with projectiles summoned from the sky itself.";

    private const float _MIN_CHARGE_TIME   = 0.75f;
    private const int   _BARRAGE_SIZE      = 16;
    private const float _BARRAGE_DELAY     = 0.04f;
    private const float _PROJ_DAMAGE       = 16f;
    private const float _MAX_RETICLE_RANGE = 10f;
    private const float _MAX_ALPHA         = 0.5f;

    internal static VFXPool _HailParticle;
    internal static GameObject _RainReticle;

    private GameObject _targetingReticle = null;
    private float _curChargeTime         = 0.0f;
    private Vector2 _chargeStartPos      = Vector2.zero;
    private float _chargeStartAngle      = 0.0f;
    private int _nextProjectileNumber    = 0;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Gunbrella>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.A, gunClass: GunClass.CHARGE, reloadTime: 1.0f, ammo: 60);
            gun.SetAnimationFPS(gun.shootAnimation, 60);
            gun.SetAnimationFPS(gun.chargeAnimation, 16);
            gun.LoopAnimation(gun.chargeAnimation, 17);
            gun.SetMuzzleVFX("muzzle_gunbrella", fps: 30, scale: 0.5f, anchor: Anchor.MiddleCenter);

        gun.SetupDefaultModule(clipSize: 1, shootStyle: ShootStyle.Charged, customClip: SpriteName);

        for (int i = 1; i < _BARRAGE_SIZE; i++) // start from 1 since we already have a default module
        {
            // use ak47 so our sprite doesn't rotate and mess up our transform calculations when launching / falling
            gun.AddProjectileModuleFrom(ItemHelper.Get(Items.Ak47) as Gun, true, false);
            gun.DefaultModule.ammoCost = 1;
        }

        _HailParticle = VFX.RegisterVFXPool("icicle_crash_particles", fps: 30, loops: false, anchor: Anchor.MiddleCenter, scale: 0.35f);

        GameActorFreezeEffect freeze = ItemHelper.Get(Items.FrostBullets).GetComponent<BulletStatusEffectItem>().FreezeModifierEffect;
        tk2dSpriteAnimationClip projAnimation = AnimatedBullet.Create(name: "gunbrella_projectile", fps: 16, anchor: Anchor.MiddleLeft);
        for (int i = 0; i < _BARRAGE_SIZE; i++)
        {
            ProjectileModule pmod = gun.Volley.projectiles[i];
            Projectile projectile = (i == 0) ? gun.InitFirstProjectile(damage: _PROJ_DAMAGE) : gun.CloneProjectile(damage: _PROJ_DAMAGE);
                projectile.AddDefaultAnimation(projAnimation);
                projectile.SetAllImpactVFX(_HailParticle);
                projectile.onDestroyEventName   = "icicle_crash";
                projectile.AppliesFreeze        = true;
                projectile.FreezeApplyChance    = 0.33f;
                projectile.freezeEffect         = freeze;
                projectile.BossDamageMultiplier = 0.6f; // bosses are big and this does a lot of damage, so tone it down
            GunbrellaProjectile gp = projectile.gameObject.AddComponent<GunbrellaProjectile>();

            pmod.angleVariance = 10f;
            if (i >= 1)
                pmod.ammoCost = 0;
            pmod.shootStyle = ShootStyle.Charged;
            pmod.sequenceStyle = ProjectileSequenceStyle.Random;
            pmod.chargeProjectiles = new(){ new(){
                Projectile = projectile,
                ChargeTime = _MIN_CHARGE_TIME,
            }};
        }

        _RainReticle = VFX.RegisterVFXObject("gunbrella_target_reticle",
            fps: 12, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 10, emissiveColour: Color.cyan, scale: 0.75f);
    }

    protected override void Update()
    {
        base.Update();
        if (BraveTime.DeltaTime == 0.0f)
            return;

        if (!this.gun.IsCharging)
        {
            EndCharge();
            return;
        }

        Lazy.PlaySoundUntilDeathOrTimeout(soundName: "gunbrella_charge_sound", source: this.gun.gameObject, timer: 0.05f);

        if (this._curChargeTime == 0.0f)
            BeginCharge();
        UpdateCharge();
        this._curChargeTime += BraveTime.DeltaTime;
    }

    // Using LateUpdate() here so alpha is updated correctly
    private void LateUpdate()
    {
        this._targetingReticle?.SetAlpha(_MAX_ALPHA * Mathf.Min(1.0f, this._curChargeTime / _MIN_CHARGE_TIME));
    }

    private void BeginCharge()
    {
        this._nextProjectileNumber = 0;
        this._chargeStartPos   = this.gun.barrelOffset.PositionVector2();
        this._chargeStartAngle = this.gun.CurrentAngle;
        if (this._targetingReticle)
            return;

        this._targetingReticle = Instantiate<GameObject>(_RainReticle, base.transform.position, Quaternion.identity);
        this._targetingReticle.SetAlphaImmediate(0.0f); // avoid bug where setting alpha on newly created object is delayed by one frame
    }

    private void UpdateCharge()
    {
        if (this.Owner is not PlayerController player)
            return;

        if (this._curChargeTime == 0.0f)
            BeginCharge();

        // smoothly handle reticle postion, compensating extra distance for controller users
        Vector2 gunPos       = this.gun.barrelOffset.PositionVector2();
        Vector2 newTargetPos =
            player.IsKeyboardAndMouse() ? player.unadjustedAimPoint.XY() : player.sprite.WorldCenter + (1f + _MAX_RETICLE_RANGE) * player.m_activeActions.Aim.Vector;
        Vector2 gunDelta     = (newTargetPos - gunPos);
        if (gunDelta.magnitude > _MAX_RETICLE_RANGE)
            newTargetPos = gunPos + _MAX_RETICLE_RANGE * gunDelta.normalized;

        // if (!GameManager.Instance.Dungeon.data.CheckInBoundsAndValid(newTargetPos.ToVector3ZUp().IntXY(VectorConversions.Floor)))
        //     return;

        if (newTargetPos.GetAbsoluteRoom() != gunPos.GetAbsoluteRoom())
            return; // aiming outside the room

        this._chargeStartPos = newTargetPos;
        this._targetingReticle.transform.position = this._chargeStartPos;
    }

    private void EndCharge()
    {
        this._curChargeTime = 0.0f;
        DestroyReticle();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        DestroyReticle();
    }

    public override void OnDropped()
    {
        base.OnDropped();
        DestroyReticle();
    }

    private void DestroyReticle()
    {
        if (!this._targetingReticle)
            return;
        UnityEngine.Object.Destroy(this._targetingReticle);
        this._targetingReticle = null;
    }

    public Vector2 GetReticleCenter()
    {
        return this._chargeStartPos;
    }

    public int GetProjectileNumber()
    {
        return this._nextProjectileNumber++;
    }
}


public class GunbrellaProjectile : MonoBehaviour
{
    private const float _SPREAD               = 1.5f;   // max distance from the target an individual projectile can land
    private const float _LAUNCH_SPEED         = 80.0f;  // speed at which projectiles rise / fall
    private const float _LAUNCH_TIME          = 0.35f;  // time spent rising
    private const float _HANG_TIME            = 0.05f;  // time spent between rising and falling
    private const float _FALL_TIME            = 0.3f;   // time spent falling
    private const float _HOME_STRENGTH        = 0.1f;   // amount we adjust our velocity each frame when launching
    private const float _DELAY                = 0.03f;  // delay between firing projectiles
    private const float _TIME_TO_REACH_TARGET = _LAUNCH_TIME + _HANG_TIME + _FALL_TIME;

    private static float _LastFireSound = 0.0f;

    private Projectile _projectile   = null;
    private PlayerController _owner  = null;
    private float _lifetime          = 0.0f;
    private bool _intangible         = true;
    private Vector2 _exactTarget     = Vector2.zero;
    private Vector2 _startVelocity   = Vector2.zero;

    private bool _launching          = false;
    private bool _falling            = false;
    private float _extraDelay          = 0.0f; // must be public so unity serializes it properly with the prefab

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;

        if (this._owner.CurrentGun.GetComponent<Gunbrella>() is not Gunbrella gun)
        {
            ETGModConsole.Log($"shooting a gunbrella projectile without a reticle, uh-oh o.o");
            return;
        }

        this._extraDelay   = _DELAY * gun.GetProjectileNumber();
        this._exactTarget = gun.GetReticleCenter();

        this._projectile.damageTypes &= (~CoreDamageTypes.Electric); // remove robot's electric damage type from the projectile
        this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
        this._projectile.specRigidbody.OnPreTileCollision += this.OnPreTileCollision;

        this._startVelocity = this._owner.m_currentGunAngle.AddRandomSpread(10f).ToVector(1f);

        if (_LastFireSound < BraveTime.ScaledTimeSinceStartup)
        {
            _LastFireSound = BraveTime.ScaledTimeSinceStartup;
            AkSoundEngine.PostEvent("gunbrella_fire_sound", this._projectile.gameObject);
        }

        this._projectile.collidesWithEnemies = false;
        this._projectile.specRigidbody.CollideWithOthers = false;
        this._projectile.specRigidbody.CollideWithTileMap = false;
        // this._projectile.renderer.sortingOrder = -10;
        StartCoroutine(TakeToTheSkies());
    }

    private void OnPreCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, SpeculativeRigidbody other, PixelCollider otherPixelCollider)
    {
        if (this._intangible)
            PhysicsEngine.SkipCollision = true;
    }

    private void OnPreTileCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, PhysicsEngine.Tile other, PixelCollider otherPixelCollider)
    {
        if (this._intangible)
            PhysicsEngine.SkipCollision = true;
    }

    private IEnumerator TakeToTheSkies()
    {
        // Phase 1 / 4 -- become intangible and launch to the skies
        this._projectile.sprite.HeightOffGround = 10f; // max, 100 doesn't render
        this._projectile.sprite.UpdateZDepth();
        this._projectile.sprite.renderLayer = 3; // 2 is same as Mourning Star laser, 3 is Gatling Gull outro doer
        DepthLookupManager.ProcessRenderer(
            this._projectile.sprite.renderer, DepthLookupManager.GungeonSortingLayer.FOREGROUND);

        Vector2 targetLaunchVelocity = (85f + 10f*UnityEngine.Random.value).ToVector(1f);
        this._projectile.IgnoreTileCollisionsFor(_TIME_TO_REACH_TARGET);
        this._projectile.baseData.speed = _LAUNCH_SPEED;
        this._projectile.baseData.range = float.MaxValue;
        this._launching = true;
        while (this._lifetime < _LAUNCH_TIME)
        {
            this._startVelocity = ((1f - _HOME_STRENGTH) * this._startVelocity) + (_HOME_STRENGTH * targetLaunchVelocity);
            this._projectile.SendInDirection(this._startVelocity, true);
            this._projectile.UpdateSpeed();
            yield return null;
            this._lifetime += BraveTime.DeltaTime;
        }
        this._lifetime -= _LAUNCH_TIME;

        // Phase 2 / 4 -- slight delay
        this._launching = false;
        this._projectile.baseData.speed = 0.01f;
        while (this._lifetime < (_HANG_TIME + this._extraDelay))
        {
            yield return null;
            this._lifetime += BraveTime.DeltaTime;
        }
        this._lifetime -= (_HANG_TIME + this._extraDelay);

        // Phase 3 / 4 -- fall from the skies
        this._falling = true;
        Vector2 targetFallVelocity = (250f + 40f*UnityEngine.Random.value).ToVector(1f);
        this._projectile.baseData.speed = _LAUNCH_SPEED;
        Vector2 offsetTarget = this._exactTarget + Lazy.RandomVector(_SPREAD * UnityEngine.Random.value);
        this._projectile.specRigidbody.Position = new Position(offsetTarget + (_FALL_TIME * _LAUNCH_SPEED) * (-targetFallVelocity));
        this._projectile.specRigidbody.UpdateColliderPositions();
        this._projectile.SendInDirection(targetFallVelocity, true);
        this._projectile.UpdateSpeed();
        while (this._lifetime + BraveTime.DeltaTime < _FALL_TIME) // stop a frame early so we can collide with enemies on our last frame
        {
            this._lifetime += BraveTime.DeltaTime;
            yield return null;
        }
        this._lifetime -= _FALL_TIME;

        // Phase 4 / 4 -- become tangible, wait a frame to collide with enemies, then die
        this._projectile.collidesWithEnemies = true;
        this._projectile.specRigidbody.CollideWithOthers = true;
        this._projectile.specRigidbody.CollideWithTileMap = true;
        this._intangible = false;
        yield return null;

        this._projectile.DieInAir();
    }
}
