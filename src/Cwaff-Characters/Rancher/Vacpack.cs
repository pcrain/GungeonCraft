namespace CwaffingTheGungy;

public class Vacpack : CwaffGun
{
    public static string ItemName         = "Vacpack";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject _VacpackVFX = null;

    internal const float _REACH            =  9.00f; // how far (in tiles) the gun reaches
    internal const float _ABSORB_RANGE     =  1.50f; // how far (in tiles) the gun absorbs slimes
    internal const float _SPREAD           =    15f; // radius (in degrees) of suction cone at the end of our reach
    internal const float _ACCEL_SEC        =  3.50f; // speed (in tiles per second) at which debris accelerates towards the gun near the end of the gun's reach
    internal const float _SUCK_FORCE       = 100.0f; // force with which slimes are sucked towards the vacuum

    internal const float _SQR_REACH        = _REACH * _REACH;
    internal const float _SQR_ABSORB_RANGE = _ABSORB_RANGE * _ABSORB_RANGE;

    private Dictionary<AIActor, ActiveKnockbackData> _KnockbackDict = new();
    private List<SlimyboiController> _vacSlimes = new();

    public static void Init()
    {
        Lazy.SetupGun<Vacpack>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.EXCLUDED, gunClass: CwaffGunClass.UTILITY, reloadTime: 1.2f, ammo: 999, infiniteAmmo: true,
            chargeFps: 16, banFromBlessedRuns: true)
          .InitProjectile(GunData.New(clipSize: -1, shootStyle: ShootStyle.Charged, hideAmmo: true, chargeTime: float.MaxValue)); // absurdly high charge value so we never actually shoot

        _VacpackVFX = VFX.Create("vacpack_wind_sprite_a", fps: 30, loopStart: 6, scale: 0.5f);
    }

    public override void Update()
    {
        base.Update();
        if (this.PlayerOwner is not PlayerController player || BraveTime.DeltaTime == 0.0f)
        {
          this.gun.LoopSoundIf(false, "vacpack_fire_sound");
          return;
        }
        bool isCharging = this.gun.IsCharging;
        this.gun.LoopSoundIf(isCharging, "vacpack_fire_sound", loopPointMs: 3898, rewindAmountMs: 3898 - 1855);
        if (!isCharging)
        {
            this._vacSlimes.Clear();
            return;
        }

        Vector2 gunpos = this.gun.barrelOffset.position;

        // Particle effect creation logic should not be tied to framerate
        bool isParticleFrame = UnityEngine.Random.value < 0.66f * (BraveTime.DeltaTime * C.FPS);
        if (isParticleFrame)
        {
            float angleFromGun = this.gun.CurrentAngle + UnityEngine.Random.Range(-_SPREAD, _SPREAD);
            //WARNING: verify this doesn't cause pooling issues
            GameObject o = SpawnManager.SpawnVFX(_VacpackVFX, (gunpos + angleFromGun.ToVector(_REACH)).ToVector3ZUp(), Lazy.RandomEulerZ());
            o.AddComponent<VacpackParticle>().Setup(this.gun, _REACH);
        }

        float towardsGunAngle = (180f + this.gun.CurrentAngle).Clamp180();
        foreach (AIActor enemy in Lazy.GetAllNearbyEnemies(gunpos, radius: _REACH, limitToCurrentRoom: false,
          includeInvulnerable: true, includeHarmless: true, ignoreWalls: false))
        {
          if (!enemy || enemy.gameObject.GetComponent<SlimyboiController>() is not SlimyboiController sloim)
            continue;

          Vector2 towardsGun = gunpos - enemy.CenterPosition;
          float sqrMagnitude = towardsGun.sqrMagnitude;
          if (!this._vacSlimes.Contains(sloim)) // we can skip checks for slimes that have been in range at least once
          {
            if (sqrMagnitude >= _SQR_REACH)
              continue;
            if (towardsGunAngle.AbsAngleTo(towardsGun.ToAngle()) > _SPREAD)
              continue;
            this._vacSlimes.Add(sloim);
          }
          if (sqrMagnitude < _SQR_ABSORB_RANGE)
          {
            ProcessSlime(sloim);
            continue;
          }
          enemy.behaviorSpeculator.Stun(0.1f, false);
          float influence = Mathf.Max(0.1f, 1f - sqrMagnitude / _SQR_REACH);
          enemy.ApplyContinuousSourcedKnockback(base.gameObject, _KnockbackDict, (_SUCK_FORCE * influence) * towardsGun.normalized);
          if (isParticleFrame)
            CwaffVFX.SpawnBurst(
              prefab           : _VacpackVFX,
              numToSpawn       : 1,
              basePosition     : enemy.CenterPosition,
              positionVariance : 0.5f,
              baseVelocity     : null,
              minVelocity      : 2f,
              velocityVariance : 2f,
              velType          : CwaffVFX.Vel.Away,
              rotType          : CwaffVFX.Rot.Random,
              lifetime         : 0.5f,
              fadeOutTime      : 0.5f,
              startScale       : 1.0f,
              endScale         : 0.1f
              );
        }
    }

    public bool IsVacuumingSlime(SlimyboiController sloim) => this._vacSlimes.Contains(sloim);

    public void ProcessSlime(SlimyboiController sloim)
    {
        base.gameObject.PlayUnique("slime_vacuum_sound");
        sloim.aiActor.EraseFromExistence(suppressDeathSounds: true);
        CwaffVFX.SpawnBurst(
          prefab           : _VacpackVFX,
          numToSpawn       : 8,
          basePosition     : this.gun.barrelOffset.position,
          positionVariance : 0.5f,
          baseVelocity     : null,
          minVelocity      : 4f,
          velocityVariance : 4f,
          velType          : CwaffVFX.Vel.Away,
          rotType          : CwaffVFX.Rot.Random,
          lifetime         : 0.5f,
          fadeOutTime      : 0.5f,
          startScale       : 1.0f,
          endScale         : 0.1f
          );
    }
}

public class VacpackParticle : MonoBehaviour
{
    private const float _MAX_LIFE           = 1.0f;
    private const float _MIN_DIST_TO_VACUUM = 0.5f;
    private const float _MIN_VAC_DIST_SQR   = _MIN_DIST_TO_VACUUM * _MIN_DIST_TO_VACUUM;
    private const float _MAX_ALPHA          = 0.5f;
    private const float _DLT_ALPHA          = 0.01f;

    private Vacpack  _vac          = null;
    private Gun _gun               = null;
    private tk2dBaseSprite _sprite = null;
    private Vector2 _velocity      = Vector2.zero;
    private float _lifetime        = 0.0f;
    private float _startDistance   = 0.0f;
    private float _startScaleX     = 1.0f;
    private float _startScaleY     = 1.0f;
    private Vector2 _spriteCenter  = Vector2.zero;

    public void Setup(Gun gun, float startDistance = 0.0f)
    {
        this._gun           = gun;
        this._vac           = gun.gameObject.GetComponent<Vacpack>();
        this._startDistance = startDistance;
        this._sprite        = base.gameObject.GetComponent<tk2dSprite>();
        this._startScaleX   = 1.0f;
        this._startScaleY   = 1.0f;
        this._spriteCenter  = this._sprite.WorldCenter;
    }

    // Using LateUpdate() here so alpha is updated correctly
    private void LateUpdate()
    {
        if (BraveTime.DeltaTime == 0.0f)
            return; // nothing to do if time isn't passing

        // handle particle fading logic exclusive to the vacuum particles
        this._lifetime += BraveTime.DeltaTime;
        if (!this._gun || this._lifetime > _MAX_LIFE)
        {
            UnityEngine.GameObject.Destroy(base.gameObject);
            return;
        }
        this._sprite.renderer.SetAlpha(_MAX_ALPHA * (1f - this._lifetime / _MAX_LIFE));

        Vector2 towardsVacuum = (this._gun.barrelOffset.position.XY() - this._sprite.WorldCenter);
        if (towardsVacuum.sqrMagnitude < _MIN_VAC_DIST_SQR)
        {
            UnityEngine.GameObject.Destroy(base.gameObject);
            return;
        }

        // Shrink on our way to the vacuum
        float scale = towardsVacuum.magnitude / this._startDistance;
        this._sprite.scale = new Vector3(this._startScaleX * scale, this._startScaleY * scale, 1f);
        this._velocity = this._sprite.WorldCenter.LerpDirectAndNaturalVelocity(
            target          : this._gun.barrelOffset.position,
            naturalVelocity : this._velocity,
            accel           : VacuumCleaner._ACCEL_SEC * BraveTime.DeltaTime,
            lerpFactor      : 1f);
        this._spriteCenter += (this._velocity * C.FPS * BraveTime.DeltaTime);
        this._sprite.PlaceAtRotatedPositionByAnchor(this._spriteCenter, Anchor.MiddleCenter);
    }
}
