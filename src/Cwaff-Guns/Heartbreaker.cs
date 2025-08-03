namespace CwaffingTheGungy;

/* TODO:
    - make heart absorption effects nicer
    - add heart counter to ammo display
    - add midgame save serialization
    - animate gun
*/

public class Heartbreaker : CwaffGun
{
    public static string ItemName         = "Heartbreaker";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const int _MAX_LEVEL = 12;
    private const int _MIN_BULLETS = 4;
    private const int _BULLETS_PER_LEVEL = 2;
    private const int _BURSTS_PER_CLIP = 3;
    private const int _BASE_MAX_AMMO = 400;
    private const int _AMMO_PER_LEVEL = _BULLETS_PER_LEVEL * 100;

    public int storedHalfHearts = 0;

    public static void Init()
    {
        Lazy.SetupGun<Heartbreaker>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.A, gunClass: GunClass.SILLY, reloadTime: 1.4f, ammo: _BASE_MAX_AMMO, shootFps: 16, reloadFps: 16,
            smoothReload: 0.1f, fireAudio: "heartbreaker_fire_sound", modulesAreTiers: true)
          .SetReloadAudio("heartbeat_sound", 0, 2, 4)
          .Attach<HeartbreakerAmmoDisplay>()
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(sprite: "heartbreaker_projectile", fps: 15, clipSize: 5, cooldown: 0.25f, burstCooldown: 0.04f,
            angleVariance: 30f, damage: 5.5f, speed: 25f, range: 1000f, force: 9f, shootStyle: ShootStyle.Burst, hitEnemySound: "heartbreaker_impact_sound",
            lightStrength: 8f, lightRange: 0.75f, lightColor: ExtendedColours.vibrantOrange))
          .SetAllImpactVFX(VFX.CreatePool("heartbreak_projectile_impact_vfx", fps: 60, loops: false, anchor: Anchor.MiddleCenter,
            emissivePower: 0.5f, emissiveColorPower: 1.5f, emissiveColour: ExtendedColours.vibrantOrange,
            lightColor: ExtendedColours.vibrantOrange, lightRange: 1.5f, lightStrength: 7.0f))
          .AttachTrail("heartbreaker_trail", fps: 60, cascadeTimer: 0.5f * C.FRAME, softMaxLength: 0.25f);

        ProjectileModule mod = gun.DefaultModule;
        gun.Volley.projectiles = new(_MAX_LEVEL + 1);
        for (int i = 0; i <= _MAX_LEVEL; ++i)
        {
            ProjectileModule newMod = ProjectileModule.CreateClone(mod, inheritGuid: false);
            newMod.burstShotCount = _MIN_BULLETS + i * _BULLETS_PER_LEVEL;
            newMod.numberOfShotsInClip = newMod.burstShotCount * _BURSTS_PER_CLIP;
            newMod.burstCooldownTime = i switch {  // faster cooldown the more shots we have
                < 3 => 0.04f,
                < 6 => 0.03f,
                < 9 => 0.02f,
                _   => 0.01f
            };
            gun.Volley.projectiles.Add(newMod);
        } //REFACTOR: burst builder
    }

    private void Start()
    {
        // gun.sprite.SetGlowiness(glowAmount: 5f, glowColor: ExtendedColours.vibrantOrange, glowColorPower: 2.0f);
        gun.sprite.SetGlowiness(glowAmount: 1f, glowColor: Color.red, glowColorPower: 2.0f, clampBrightness: false);
    }

    public override void Update()
    {
        base.Update();
        if (this.gun.IsReloading)
            return;

        float phase = Mathf.Abs(Mathf.Sin(9f * BraveTime.ScaledTimeSinceStartup));
        // this.gun.sprite.renderer.material.SetFloat(CwaffVFX._EmissivePowerId, 5f + 5f * phase);
        this.gun.sprite.renderer.material.SetFloat(CwaffVFX._EmissivePowerId, 1f + 2f * phase);
        // this.gun.sprite.renderer.material.SetFloat(CwaffVFX._EmissivePowerId, 2f + 8f * phase);
        // this.gun.sprite.renderer.material.SetFloat(CwaffVFX._EmissiveColorPowerId, 2f + 2f * phase);
        this.gun.sprite.renderer.material.SetFloat(CwaffVFX._EmissiveColorPowerId, 2f + 8f * phase);
        // this.gun.sprite.renderer.material.SetFloat(CwaffVFX._EmissiveColorPowerId, 1f + 0.5f * phase);
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        projectile.OverrideMotionModule = new HeartbreakerProjectileMotionModule();
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        this.gun.spriteAnimator.AnimationEventTriggered -= ProduceLight;
        if (this.PlayerOwner)
            this.gun.spriteAnimator.AnimationEventTriggered += ProduceLight;
        UpdateGunStrength();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        this.gun.spriteAnimator.AnimationEventTriggered -= ProduceLight;
    }

    public override void OnFullClipReload(PlayerController player, Gun gun)
    {
        for (int health = ConsumeNearbyHeart(player.CenterPosition); health > 0; --health)
        {
            if (gun.CurrentStrengthTier >= _MAX_LEVEL)
                return;
            ++storedHalfHearts;
            UpdateGunStrength();
            if ((storedHalfHearts % 2) == 0)
                gun.GainAmmo(_AMMO_PER_LEVEL);
        }
    }

    private int ConsumeNearbyHeart(Vector2 pos)
    {
        const float MAX_SQR_RADIUS = 10f;

        IPlayerInteractable targetIx = null;
        HealthPickup targetHeart = null;
        float nearest = MAX_SQR_RADIUS;
        foreach (var ix in RoomHandler.unassignedInteractableObjects)
        {
            if (ix == null || ix is not HealthPickup heart || !heart || !heart.isActiveAndEnabled || !heart.sprite)
                continue;
            float dist = (pos - heart.sprite.WorldCenter).sqrMagnitude;
            if (dist > nearest)
                continue;
            targetIx = ix;
            targetHeart = heart;
            nearest = dist;
        }
        if (!targetHeart)
            return 0;

        RoomHandler.unassignedInteractableObjects.Remove(targetIx);
        int health = Mathf.RoundToInt(2f * targetHeart.healAmount);
        targetHeart.sprite.DuplicateInWorldAsMesh().Dissipate(time: 0.75f, amplitude: 5f, progressive: true);
        UnityEngine.Object.Destroy(targetHeart.gameObject);
        base.gameObject.Play("materialize_sound");
        base.gameObject.Play("vaporized_sound");
        return health;
    }

    private void ProduceLight(tk2dSpriteAnimator animator, tk2dSpriteAnimationClip clip, int frame)
    {
        if (clip.name != "heartbreaker_reload" || ((frame % 2) == 1))
            return;
        EasyLight.Create(parent: this.gun.PrimaryHandAttachPoint, color: Color.Lerp(ExtendedColours.vibrantOrange, Color.white, 0.35f),
          fadeInTime: 0.05f, fadeOutTime: 0.05f, radius: 2f, brightness: 3f, maxLifeTime: 0.1f);
    }

    private void UpdateGunStrength()
    {
        int newTier = (storedHalfHearts / 2);
        if (gun.CurrentStrengthTier == newTier)
            return;

        gun.CurrentStrengthTier = newTier;
        gun.SetBaseMaxAmmo(_BASE_MAX_AMMO + _AMMO_PER_LEVEL * newTier);
    }

    private class HeartbreakerAmmoDisplay : CustomAmmoDisplay
    {
        private Gun _gun;
        private PlayerController _owner;

        private void Start()
        {
            this._gun = base.GetComponent<Gun>();
            this._owner = this._gun.CurrentOwner as PlayerController;
        }

        public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
        {
            if (!this._owner)
                return false;

            uic.GunAmmoCountLabel.Text = $"{this._gun.m_currentStrengthTier}\n{this._owner.VanillaAmmoDisplay()}";
            return true;
        }
    }
}

public class HeartbreakerProjectileMotionModule : ProjectileMotionModule
{
    private const float _DECEL_START        = 0.15f;
    private const float _LERP_RATE          = 13f;
    private const float _LOCKON_SPEED       = 5f;
    private const float _LOCKON_SPEED_SCALE = 2f;
    private const float _HOME_SPEED_SCALE   = 4f;
    private const float _MIN_START_SPEED    = 5f;
    private const float _HOME_THRES         = 10f;
    private const float _HOME_THRES_SQR     = _HOME_THRES * _HOME_THRES;

    private float _stateTime            = 0f;
    private State _state                = State.START;
    private Vector2 _initialRightVector = default;
    private Vector2 _initialUpVector    = default;
    private Vector2 _targetDir          = default;
    private float _startSpeed           = 0f;

    private enum State
    {
        START,
        DECEL,
        LOCKON,
        HOME,
    }

    private Vector2 DetermineTargetDir(Projectile source, SpeculativeRigidbody specRigidbody)
    {
        AIActor nearestEnemy = Lazy.NearestEnemy(source.SafeCenter);
        return nearestEnemy ? (nearestEnemy.CenterPosition - source.SafeCenter).normalized : Lazy.RandomVector();
    }

    public override void Move(Projectile source, Transform projectileTransform, tk2dBaseSprite projectileSprite, SpeculativeRigidbody specRigidbody, ref float m_timeElapsed, ref Vector2 m_currentDirection, bool Inverted, bool shouldRotate)
    {
        float now = BraveTime.ScaledTimeSinceStartup;
        float dtime = BraveTime.DeltaTime;
        float newSpeed;
        this._stateTime += dtime;

        switch (this._state)
        {
            case State.START:
                if (this._stateTime >= _DECEL_START)
                {
                    this._startSpeed = source.baseData.speed;
                    if (this._startSpeed < _MIN_START_SPEED)
                        this._startSpeed = _MIN_START_SPEED;
                    this._state = State.DECEL;
                }
                specRigidbody.Velocity = source.m_currentDirection * source.m_currentSpeed;
                break;
            case State.DECEL:
                newSpeed = Lazy.SmoothestLerp(source.baseData.speed, 0f, _LERP_RATE);
                if (newSpeed < 1f || source.m_currentDirection == Vector2.zero)
                {
                    newSpeed = 1f;
                    this._targetDir = DetermineTargetDir(source, specRigidbody);
                    this._state = State.LOCKON;
                    this._stateTime = 0f;
                }
                source.baseData.speed = newSpeed;
                source.UpdateSpeed();
                specRigidbody.Velocity = source.m_currentDirection * newSpeed;
                break;
            case State.LOCKON:
                specRigidbody.Velocity += (this._startSpeed * _LOCKON_SPEED_SCALE * dtime) * this._targetDir;
                if (specRigidbody.Velocity.sqrMagnitude > _HOME_THRES_SQR)
                {
                    newSpeed = _HOME_SPEED_SCALE * this._startSpeed;
                    source.baseData.speed = newSpeed;
                    source.UpdateSpeed();
                    specRigidbody.Velocity = newSpeed * this._targetDir;
                    this._state = State.HOME;
                    source.gameObject.Play("heartbreaker_home_sound");
                }
                break;
            case State.HOME:
                specRigidbody.Velocity = (_HOME_SPEED_SCALE * this._startSpeed) * this._targetDir;
                break;
        }
        projectileTransform.localRotation = specRigidbody.Velocity.EulerZ();
    }

    private void ResetAngle(float angleDiff)
    {
        if (float.IsNaN(angleDiff))
            return;

        Quaternion q        = Quaternion.Euler(0f, 0f, angleDiff);
        _initialUpVector    = q * _initialUpVector;
        _initialRightVector = q * _initialRightVector;
    }

    public override void UpdateDataOnBounce(float angleDiff) => ResetAngle(angleDiff);
    public override void AdjustRightVector(float angleDiff) => ResetAngle(angleDiff);
}
