
namespace CwaffingTheGungy;

/* TODO:
    - maybe add helix projectiles compatibility (currently just ignores it)
*/

public class Heartbreaker : CwaffGun
{
    public static string ItemName         = "Heartbreaker";
    public static string ShortDescription = "</3";
    public static string LongDescription  = "Fires a burst of projectiles that rapidly home towards nearby enemies after a short delay. Reloading with a full clip while near a heart pickup will consume it, increasing the gun's current and max ammo, clip size, and burst size.";
    public static string Lore             = "TBD";

    private const float _VFX_GLOW        = 10f;
    private const int _MAX_LEVEL         = 26;
    private const int _MIN_BULLETS       = 4;
    private const int _BURSTS_PER_CLIP   = 3;
    private const int _BASE_MAX_AMMO     = 800;
    private const int _AMMO_PER_LEVEL    = _BASE_MAX_AMMO / _MIN_BULLETS;

    private List<PlayerOrbital> _extantOrbitals = new();

    internal static GameObject _AbsorbVFX = null;
    internal static GameObject _EmptyHeartGuon = null;

    public int storedHalfHearts = 0;

    public static void Init()
    {
        Lazy.SetupGun<Heartbreaker>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.A, gunClass: GunClass.SILLY, reloadTime: 1.4f, ammo: _BASE_MAX_AMMO, shootFps: 30, reloadFps: 16,
            smoothReload: 0.1f, fireAudio: "heartbreaker_fire_sound", modulesAreTiers: true)
          .SetReloadAudio("heartbeat_sound", 0, 2, 4)
          .Attach<HeartbreakerAmmoDisplay>()
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(sprite: "heartbreaker_projectile", fps: 15, clipSize: 5, cooldown: 0.25f, burstCooldown: 0.04f, spawnSound: "heartbreaker_fire_sound",
            angleVariance: 30f, damage: 5.5f, speed: 25f, range: 1000f, force: 9f, shootStyle: ShootStyle.Burst, hitEnemySound: "heartbreaker_impact_sound",
            lightStrength: 8f, lightRange: 0.75f, lightColor: ExtendedColours.vibrantOrange, glowAmount: 30f, customClip: true))
          .SetAllImpactVFX(VFX.CreatePool("heartbreak_projectile_impact_vfx", fps: 60, loops: false, anchor: Anchor.MiddleCenter,
            emissivePower: 0.5f, emissiveColorPower: 1.5f, emissiveColour: ExtendedColours.vibrantOrange,
            lightColor: ExtendedColours.vibrantOrange, lightRange: 1.5f, lightStrength: 7.0f))
          .AttachTrail("heartbreaker_trail", fps: 60, cascadeTimer: 0.5f * C.FRAME, softMaxLength: 0.25f);

        ProjectileModule mod = gun.DefaultModule;
        gun.Volley.projectiles = new(_MAX_LEVEL + 1);
        for (int i = 0; i <= _MAX_LEVEL; ++i)
        {
            ProjectileModule newMod = ProjectileModule.CreateClone(mod, inheritGuid: false);
            newMod.burstShotCount = _MIN_BULLETS + i;
            newMod.numberOfShotsInClip = newMod.burstShotCount * _BURSTS_PER_CLIP;
            newMod.burstCooldownTime = i switch {  // faster cooldown the more shots we have
                < 6  => 0.04f,
                < 12 => 0.03f,
                < 18 => 0.02f,
                _    => 0.01f
            };
            gun.Volley.projectiles.Add(newMod);
        } //REFACTOR: burst builder

        _AbsorbVFX = VFX.Create("hearbreaker_absorb_vfx", emissivePower: _VFX_GLOW, emissiveColour: Color.Lerp(Color.red, Color.white, 0.5f));

        _EmptyHeartGuon = (Items.GlassGuonStone.AsPassive() as IounStoneOrbitalItem).OrbitalPrefab.gameObject.ClonePrefab();
        _EmptyHeartGuon.GetComponentInChildren<tk2dSprite>().SetSprite(VFX.Collection, VFX.Collection.GetSpriteIdByName("heartbreaker_shield"));
        PixelCollider collider = _EmptyHeartGuon.GetComponent<SpeculativeRigidbody>().PixelColliders[0];
          collider.ManualWidth = 13;
          collider.ManualHeight = 11;
          collider.ManualOffsetX = 6;
          collider.ManualOffsetY = 5;
        _EmptyHeartGuon.GetComponent<PlayerOrbital>().orbitRadius = 4f;
    }

    private void Start()
    {
        // gun.sprite.SetGlowiness(glowAmount: 5f, glowColor: ExtendedColours.vibrantOrange, glowColorPower: 2.0f);
        gun.sprite.SetGlowiness(glowAmount: 1f, glowColor: Color.red, glowColorPower: 2.0f, clampBrightness: false);
    }

    private void UpdateOrbitals()
    {
        HealthHaver hh = this.PlayerOwner.healthHaver;
        int curOrbitals = this._extantOrbitals.Count;
        int nextOrbitals = Mathf.Max(0, Mathf.FloorToInt(hh.GetMaxHealth() - hh.GetCurrentHealth()));
        while (this._extantOrbitals.Count < nextOrbitals)
        {
            PlayerOrbital newOrbital = PlayerOrbitalItem.CreateOrbital(this.PlayerOwner, _EmptyHeartGuon, false).GetComponent<PlayerOrbital>();
            EasyLight.Create(pos: newOrbital.gameObject.GetComponentInChildren<tk2dSprite>().WorldCenter, parent: newOrbital.gameObject.transform,
              color: Color.Lerp(ExtendedColours.vibrantOrange, Color.white, 0.35f), fadeInTime: 0.05f, fadeOutTime: 0.05f, radius: 2f, brightness: 3f);
            this._extantOrbitals.Add(newOrbital);
        }
        while (this._extantOrbitals.Count > nextOrbitals)
        {
            int last = this._extantOrbitals.Count - 1;
            if (this._extantOrbitals[last])
                UnityEngine.Object.Destroy(this._extantOrbitals[last].gameObject);
            this._extantOrbitals.RemoveAt(last);
        }
    }

    private void DestroyOrbitals()
    {
        for (int i = this._extantOrbitals.Count - 1; i >=0; --i)
            if (this._extantOrbitals[i])
                UnityEngine.Object.Destroy(this._extantOrbitals[i].gameObject);
        this._extantOrbitals.Clear();
    }

    public override void Update()
    {
        base.Update();
        if (this.gun.IsReloading)
            return;

        if (this.Mastered && this.PlayerOwner)
            UpdateOrbitals();

        float phase = Mathf.Abs(Mathf.Sin(9f * BraveTime.ScaledTimeSinceStartup));
        Material mat = this.gun.sprite.renderer.material;
        mat.SetFloat(CwaffVFX._EmissivePowerId, 1f + 2f * phase);
        mat.SetFloat(CwaffVFX._EmissiveColorPowerId, 2f + 8f * phase);
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
        DestroyOrbitals();
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        GameManager.Instance.OnNewLevelFullyLoaded += this.OnNewFloor;
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
        DestroyOrbitals();
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
        base.OnDestroy();
    }

    private void OnNewFloor()
    {
        if (this)
            DestroyOrbitals();
    }

    public override void OnFullClipReload(PlayerController player, Gun gun)
    {
        if (gun.CurrentStrengthTier >= _MAX_LEVEL)
            return;
        int health = ConsumeNearbyHeart(player.CenterPosition);
        if (health == 0)
            return;

        health = Mathf.Min(health, _MAX_LEVEL - storedHalfHearts);
        storedHalfHearts += health;
        UpdateGunStrength();
        gun.GainAmmo(health * _AMMO_PER_LEVEL);
        CwaffVFX.SpawnBurst(
            prefab           : _AbsorbVFX,
            numToSpawn       : 30,
            anchorTransform  : gun.sprite.transform,
            basePosition     : gun.sprite.WorldCenter,
            positionVariance : 5f,
            velType          : CwaffVFX.Vel.InwardToCenter,
            rotType          : CwaffVFX.Rot.Random,
            lifetime         : 0.35f,
            startScale       : 1.0f,
            endScale         : 0.1f,
            emissivePower    : _VFX_GLOW,
            emitColorPower   : 1.55f,
            emissiveColor    : Color.red
          );
    }

    private int ConsumeNearbyHeart(Vector2 pos)
    {
        const float MAX_SQR_RADIUS = 25f;

        IPlayerInteractable targetIx = null;
        HealthPickup targetHeart = null;
        float nearest = MAX_SQR_RADIUS;
        foreach (var ix in RoomHandler.unassignedInteractableObjects)
        {
            if (ix == null || ix is not HealthPickup heart || !heart || !heart.isActiveAndEnabled || !heart.sprite || heart.healAmount <= 0)
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
        targetHeart.sprite.DuplicateInWorldAsMesh().Dissipate(time: 0.4f, amplitude: 5f, progressive: true);
        UnityEngine.Object.Destroy(targetHeart.gameObject);
        base.gameObject.Play("heartbreaker_absorb_sound");
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
        if (gun.CurrentStrengthTier == storedHalfHearts)
            return;

        gun.CurrentStrengthTier = storedHalfHearts;
        gun.SetBaseMaxAmmo(_BASE_MAX_AMMO + _AMMO_PER_LEVEL * storedHalfHearts);
    }

    public override void MidGameSerialize(List<object> data, int i)
    {
        base.MidGameSerialize(data, i);
        data.Add(storedHalfHearts);
    }

    public override void MidGameDeserialize(List<object> data, ref int i)
    {
        base.MidGameDeserialize(data, ref i);
        storedHalfHearts = ((int)data[i++]);
        UpdateGunStrength();
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

            uic.GunAmmoCountLabel.Text = $"[sprite \"heartbreaker_heart_ui\"]x{this._gun.m_currentStrengthTier}\n{this._owner.VanillaAmmoDisplay()}";
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
