namespace CwaffingTheGungy;

public class Xelsior : CwaffGun
{
    public static string ItemName         = "X-elsior";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject _XelsiorReticle  = null;
    internal static GameObject _HoverGunPrefab  = null;
    internal static GameObject _ShootVFXPrefab  = null;
    internal static GameObject _HoverProjectile = null;
    internal static float      _ReticleRadius   = 1.75f;

    private const float _TARGET_COOLDOWN = 0.5f;

    private List<XelsiorHoveringGun> _extantGuns = new();
    private AIActor _target = null;
    private float _cooldown = 0.0f;
    private GameObject _reticle = null;

    public int maxGuns = 0;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Xelsior>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.S, gunClass: GunClass.RIFLE, reloadTime: 0.9f, ammo: 600, shootFps: 14, reloadFps: 4,
                fireAudio: null, reloadAudio: null);

        gun.InitProjectile(GunData.New(baseProjectile: Items.Moonscraper.Projectile(), clipSize: -1, cooldown: 0.18f, //NOTE: inherit from Moonscraper for hitscan
            shootStyle: ShootStyle.Beam, damage: 2f, force: 0f, speed: -1f, ammoCost: 3, angleVariance: 0f,
            beamSprite: "xelsior_beam", beamFps: 60, beamChargeFps: 8, beamImpactFps: 30,
            beamLoopCharge: false, beamReflections: 0, beamChargeDelay: 0f, beamEmission: 50f));

        _XelsiorReticle = VFX.Create("xelsior_reticle");
        _HoverGunPrefab = VFX.Create("xelsior_hover_gun");
        _ShootVFXPrefab = VFX.Create("xelsior_shoot_vfx", fps: 60, loops: false);
        _HoverGunPrefab.AddComponent<XelsiorHoveringGun>();

        _HoverProjectile = Items.Ak47.CloneProjectile(GunData.New(/*sprite: "widowmaker_laser_projectile", */angleVariance: 0.0f,
            speed: 100f, damage: 2f, range: 4f, force: 0f, shouldRotate: true, pierceBreakables: true, ignoreDamageCaps: true,
            collidesWithTilemap: false)).gameObject;
    }

    public override void PostProcessBeam(BeamController beam)
    {
        base.PostProcessBeam(beam);
        beam.projectile.OnHitEnemy += this.SetTargets;
    }

    private void SetTargets(Projectile projectile, SpeculativeRigidbody rigidbody, bool arg3)
    {
        SetTargets(rigidbody.aiActor);
    }

    private void SetTargets(AIActor enemy, bool allowNull = false)
    {
        if (!allowNull)
        {
            if (!enemy)
                return;
            if (enemy.healthHaver is not HealthHaver hh)
                return;
            if (hh.IsDead)
                return;
        }
        this._target = enemy;
        int numGuns = this._extantGuns.Count;
        for (int i = 0; i < numGuns; ++i)
            this._extantGuns[i].SetTarget(enemy);
        this._cooldown = _TARGET_COOLDOWN;
    }

    private void UpdateReticle()
    {
        if (this._reticle)
        {
            this._reticle.GetComponent<tk2dSprite>().renderer.enabled = this._target;
            this._reticle.transform.rotation = (XelsiorHoveringGun._CIRCLE_SPEED * 360f * BraveTime.ScaledTimeSinceStartup).EulerZ();
            this._reticle.SetAlpha(0.5f * this._cooldown / _TARGET_COOLDOWN);
        }
        if (!this._target)
            return;
        if (!this._reticle)
        {
            this._reticle = SpawnManager.SpawnVFX(_XelsiorReticle, this._target.CenterPosition, Quaternion.identity, ignoresPools: true);
            this._reticle.SetAlphaImmediate(0.5f);
        }
        tk2dSprite sigilSprite = this._reticle.GetComponent<tk2dSprite>();
        sigilSprite.HeightOffGround = -5f;
        sigilSprite.UpdateZDepth();
        this._reticle.transform.position = this._target.CenterPosition;
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        StartCoroutine(SpawnGunsOnceWeCanMove());

        #if DEBUG
        Commands._OnDebugKeyPressed -= AddNewGun;
        Commands._OnDebugKeyPressed += AddNewGun;
        #endif
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        DestroyExtantGuns();
        this._target = null;
    }

    private void CheckForDroppedGuns()
    {
        if (this.GetExtantBeam() is not BasicBeamController beam)
            return;

        Vector2 start = beam.Origin;
        if (start.GetAbsoluteRoom() is not RoomHandler room)
            return;
        Vector2 end = start + beam.Direction.normalized * beam.m_currentBeamDistance;
        Gun targetGun = null;
        foreach (var ix in RoomHandler.unassignedInteractableObjects)
        {
            if (ix is not Gun gun || !gun.isActiveAndEnabled || !gun.sprite)
                continue;
            Vector2 v = default;
            if (!BraveMathCollege.LineSegmentRectangleIntersection(start, end, gun.sprite.WorldBottomLeft, gun.sprite.WorldTopRight, ref v))
                continue;
            targetGun = gun;
            break;
        }
        if (!targetGun)
            return;

        RoomHandler.unassignedInteractableObjects.Remove(targetGun);
        int quality = Mathf.Max(targetGun.QualityGrade(), 1);
        tk2dMeshSprite ms = Lazy.CreateMeshSpriteObject(targetGun.sprite, targetGun.sprite.WorldCenter, pointMesh: true);
        ms.PlaceAtPositionByAnchor(targetGun.sprite.WorldCenter, Anchor.MiddleCenter);
        ms.StartCoroutine(XelsiorHoveringGun.DoDissipate(ms, 0.5f));
        UnityEngine.Object.Destroy(targetGun.gameObject);
        for (int i = 0; i < quality; ++i)
            AddNewGun();
        base.gameObject.Play("assimilate_sound");
        base.gameObject.Play("vaporized_sound");
    }

    public override void Update()
    {
        base.Update();

        UpdateReticle();
        CheckForDroppedGuns();
        this.gun.LoopSoundIf(this.gun.IsFiring, "xelsior_fire_loop", loopPointMs: 1300, rewindAmountMs: 500);
        if (this._cooldown <= 0.0f)
            return;
        this._cooldown -= BraveTime.DeltaTime;
        if (this._cooldown > 0.0f)
            return;
        this._cooldown = 0.0f;
        SetTargets(null, allowNull: true);
    }

    private void AddNewGun()
    {
        ++this.maxGuns;
        SpawnNewGun();
        UpdateGunOffsets();
    }

    private void SpawnNewGun()
    {
        XelsiorHoveringGun xg = _HoverGunPrefab.Instantiate(base.transform.position).GetComponent<XelsiorHoveringGun>();
        xg.Setup(this);
        xg.SetTarget(this._target);
        this._extantGuns.Add(xg);
    }

    private void UpdateGunOffsets()
    {
        int numGuns = this._extantGuns.Count;
        float maxOffset = (numGuns - 1) / 2.0f;
        bool oddGuns = (numGuns % 2) == 1;
        float offset = 0;
        for (int i = 0; i < numGuns; ++i)
        {
            offset = maxOffset - (i / 2);
            if (i % 2 == 1)
                offset = -offset;
            if (oddGuns && i == numGuns - 1)
                offset = 0;
            this._extantGuns[i].offset = offset;
            this._extantGuns[i].numGuns = numGuns;
        }
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        GameManager.Instance.OnNewLevelFullyLoaded += this.OnNewFloor;
        base.OnPlayerPickup(player);
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
        base.OnDroppedByPlayer(player);
        StopAllCoroutines();
        DestroyExtantGuns();
        this._target = null;
    }

    private void DestroyExtantGuns(bool doVFX = true)
    {
        if (this._extantGuns.Count > 0)
            base.gameObject.Play("vaporized_sound");
        for (int i = 0; i < this._extantGuns.Count; ++i)
            if (this._extantGuns[i])
            {
                this._extantGuns[i].doVFX = doVFX;
                UnityEngine.Object.Destroy(this._extantGuns[i].gameObject);
            }
        this._extantGuns.Clear();
    }

    public override void OnDestroy()
    {
        GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
        StopAllCoroutines();
        DestroyExtantGuns();
        base.OnDestroy();

        #if DEBUG
        Commands._OnDebugKeyPressed -= AddNewGun;
        #endif
    }

    private void OnNewFloor()
    {
        if (!this)
            return;
        DestroyExtantGuns(doVFX: false);
        StartCoroutine(SpawnGunsOnceWeCanMove());
    }

    private IEnumerator SpawnGunsOnceWeCanMove()
    {
        if (this.maxGuns <= 0)
            yield break;
        while (this.PlayerOwner && !this.PlayerOwner.AcceptingNonMotionInput)
            yield return null;
        if (!this.PlayerOwner)
            yield break;

        base.gameObject.Play("assimilate_sound");
        for (int i = 0; i < this.maxGuns; ++i)
            SpawnNewGun();
        UpdateGunOffsets();
    }

    public override void MidGameSerialize(List<object> data, int i)
    {
        base.MidGameSerialize(data, i);
        data.Add(this.maxGuns);
    }

    public override void MidGameDeserialize(List<object> data, ref int i)
    {
        base.MidGameDeserialize(data, ref i);
        this.maxGuns = (int)data[i++];
    }
}

public class XelsiorHoveringGun : MonoBehaviour
{
    internal const float _CIRCLE_SPEED = 0.5f;

    private const float _KICKBACK_TIME = 0.5f;
    private const float _MATERIALIZE_TIME = 0.5f;
    private const float _MAX_TRANSITION_TIME = 1.0f;
    private const float _BASE_DELAY = 0.1f;
    private const float _MAX_DELAY = 0.5f;
    private const float _GUN_DIST = 3.5f;

    private static VFXPool _MuzzleVFX = null;

    public float curAngle    = 90f;
    public float curOffset   = 0f;
    public float offset      = 0f;
    public int numGuns       = 0;
    public Vector2 curPos    = default;
    public tk2dSprite sprite = null;
    public bool doVFX        = true;

    private Xelsior _parentGun = null;
    private PlayerController _owner = null;
    private AIActor _target = null;
    private Vector2 _lastTargetPos = default;
    private HealthHaver _targetHH = null;
    private bool _transitionToPlayer = false;
    private float _kickback = 0f;
    private float _transitionTimer = 0f;
    private float _retargetTime = 0f;
    private float _nextShot = 0f;
    private float _targetHoverDist = 0f;
    private tk2dMeshSprite _mesh = null;

    public XelsiorHoveringGun Setup(Xelsior parentGun)
    {
        this._parentGun = parentGun;
        this._owner = parentGun.PlayerOwner;

        this.sprite = base.gameObject.GetComponent<tk2dSprite>();

        this._mesh = Lazy.CreateMeshSpriteObject(this.sprite, this.sprite.WorldCenter, pointMesh: true);
        this._mesh.gameObject.transform.position = this.sprite.WorldCenter;
        this._mesh.gameObject.transform.rotation = base.transform.rotation;
        this._mesh.renderer.material.shader = CwaffShaders.ShatterShader;
        this._mesh.renderer.material.SetFloat("_Progressive", 1f);
        this._mesh.renderer.material.SetFloat("_Fade", 1f);
        this._mesh.renderer.material.SetFloat("_Amplitude", 4f);
        this._mesh.renderer.material.SetFloat("_RandomSeed", UnityEngine.Random.value);
        this.StartCoroutine(DoMaterialize(this, this._mesh, _MATERIALIZE_TIME));

        // this.sprite.ForceUnlit();
        this.sprite.usesOverrideMaterial = true;
        this.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/PlayerShader");
        this.sprite.renderer.enabled = false;

        return this;
    }

    public void SetTarget(AIActor enemy)
    {
        if (enemy == this._target)
            return;

        if (!this._target) // reset shot timer if we didn't already have a target
        {
            float myDelay = this.offset >= 0 ? this.offset : (this.numGuns + this.offset);
            float delay = _BASE_DELAY + (_MAX_DELAY - _BASE_DELAY) * (myDelay / this.numGuns);
            this._nextShot = BraveTime.ScaledTimeSinceStartup + delay;
        }

        this._target = enemy;
        this._targetHH = enemy ? enemy.healthHaver : null;
        this._targetHoverDist = Mathf.Max(_GUN_DIST, enemy.SpriteRadius() + 2.5f);
    }

    private void HandleQueuedShots(bool targetIsAlive)
    {
        if (this._nextShot >= BraveTime.ScaledTimeSinceStartup)
            return;

        this._nextShot = BraveTime.ScaledTimeSinceStartup + _MAX_DELAY;
        if (!targetIsAlive)
            return;

        if (this.numGuns <= 6)
            base.gameObject.PlayOnce("xelsior_shoot_sound");
        else
            base.gameObject.PlayOnce("xelsior_shoot_sound_short");
        this._kickback = _KICKBACK_TIME;

        Vector2 pos =
            this.sprite.transform.position.XY() + this.sprite.GetRelativePositionFromAnchor(Anchor.MiddleRight).Rotate(this.sprite.transform.eulerAngles.z);
        Vector2 delta = (this._target.CenterPosition - pos);
        GameObject po = SpawnManager.SpawnProjectile(Xelsior._HoverProjectile, pos, delta.EulerZ());
        Projectile proj = po.GetComponent<Projectile>();
        proj.SetOwnerAndStats(this._owner);
        this._owner.DoPostProcessProjectile(proj);
        proj.AddTrail(ChekhovsGun._ChekhovTrailPrefab).gameObject.SetGlowiness(10.0f);

        if (_MuzzleVFX == null)
            _MuzzleVFX = Items.Mailbox.AsGun().muzzleFlashEffects;
        _MuzzleVFX.SpawnAtPosition(pos, zRotation: delta.ToAngle());
    }

    private void Update()
    {
        const float SQR_SNAP_THRES     = 0.01f;
        const float SQR_UNTARGET_THRES = 0.04f;
        const float GUN_SPACING        = 0.75f;
        const float HOVER_AMP          = 0.5f;
        const float HOVER_FREQ         = 3.0f;
        const float LERP_RATE          = 7.5f;
        const float ANGULAR_LERP_RATE  = 9f;
        const float KICKBACK_STRENGTH  = 1.25f;
        const float RETARGET_DELAY      = 0.5f;

        if (!this._owner || !this._parentGun)
            return;

        this._kickback = Mathf.Max(this._kickback - BraveTime.DeltaTime, 0f);

        Vector2 basePos, backVec, tangent, targetPos;
        float targetAngle;

        float now = BraveTime.ScaledTimeSinceStartup;
        bool targetIsAlive = this._target && this._targetHH && this._targetHH.IsAlive;
        if (targetIsAlive || (this._retargetTime > now))
        {
            if (targetIsAlive)
            {
                this._lastTargetPos = this._target.CenterPosition;
                this._retargetTime = now + RETARGET_DELAY;
            }
            basePos = this._lastTargetPos;
            float offAngle = 360f * _CIRCLE_SPEED * BraveTime.ScaledTimeSinceStartup;
            targetAngle = (offAngle + 360f * this.offset / this.numGuns).Clamp360();
            targetPos = basePos + targetAngle.ToVector(this._targetHoverDist);
            this._transitionToPlayer = true;
            this._transitionTimer = 0f;
            HandleQueuedShots(targetIsAlive); // tick timer down even if we don't have a valid target
        }
        else
        {
            basePos = this._owner.CenterPosition;
            targetAngle = (180f + this._owner.m_currentGunAngle).Clamp360();
            backVec = basePos + targetAngle.ToVector(_GUN_DIST);
            tangent = (targetAngle + 90f).ToVector(GUN_SPACING);
            targetPos = backVec + this.offset * tangent;
            this._target = null;
            this._targetHH = null;
        }

        // HandleLaser(targetIsAlive, this._lastTargetPos);

        Vector2 targetDelta = (this.curPos - targetPos);
        float dMag = targetDelta.sqrMagnitude;

        if (dMag < SQR_UNTARGET_THRES)  // check if we need to transition from lerping magnitude to lerping angle
            this._transitionToPlayer = this._target;

        if (dMag < SQR_SNAP_THRES) // snap to target
        {
            this.curAngle = targetAngle;
            this.curPos = targetPos;
            this.curOffset = this.offset;
        }
        else if (this._target || this._transitionToPlayer) // lerp position to target
        {
            this.curOffset = this.offset;
            float lerpRate = LERP_RATE;
            if (!this._target) // extra adjustment to make sure we eventually reach the player
            {
                this._transitionTimer += BraveTime.DeltaTime;
                float transitionProgress = this._transitionTimer / _MAX_TRANSITION_TIME;
                lerpRate += 10f * transitionProgress;
            }
            this.curPos = Lazy.SmoothestLerp(this.curPos, targetPos, lerpRate);
            float relAngle = this.curAngle.RelAngleTo(targetAngle);
            this.curAngle = (this.curAngle + Lazy.SmoothestLerp(0, relAngle, LERP_RATE)).Clamp360();
        }
        else // snap to magnitude from target and lerp angle
        {
            this.curOffset = Lazy.SmoothestLerp(this.curOffset, this.offset, 5f);
            float relAngle = this.curAngle.RelAngleTo(targetAngle);
            this.curAngle = (this.curAngle + Lazy.SmoothestLerp(0, relAngle, ANGULAR_LERP_RATE)).Clamp360();
            Vector2 localBackVec = basePos + this.curAngle.ToVector(_GUN_DIST);
            Vector2 localTangent = (this.curAngle + 90f).ToVector(GUN_SPACING);
            this.curPos = localBackVec + this.curOffset * localTangent;
        }
        Vector2 kickbackVec = Vector2.zero;
        if (this._kickback > 0)
        {
            float kickbackLeft = this._kickback / _KICKBACK_TIME;
            float kickbackDist = KICKBACK_STRENGTH * Mathf.Sin(Mathf.PI * kickbackLeft * kickbackLeft);
            kickbackVec = this.curAngle.ToVector(kickbackDist);
        }
        Vector2 finalPos = this.curPos + kickbackVec + new Vector2(0f, HOVER_AMP * Mathf.Sin(HOVER_FREQ * BraveTime.ScaledTimeSinceStartup));
        base.transform.position = finalPos.Quantize(0.0625f);
        base.transform.localRotation = (180f + this.curAngle).EulerZ();
        this.sprite.FlipY = this.curAngle < 90f || this.curAngle > 270f;
    }

    // private tk2dTiledSprite _laserSight = null;
    // private void HandleLaser(bool draw, Vector2 target)
    // {
    //     const float _SIGHT_WIDTH = 1.0f;

    //     if (this._laserSight)
    //         this._laserSight.renderer.enabled = draw;
    //     if (!draw)
    //         return;

    //     Vector2 start = this.sprite.WorldCenter;
    //     Vector2 delta = target - start;
    //     float angle = delta.ToAngle();
    //     float mag   = C.PIXELS_PER_TILE * (delta.magnitude - Xelsior._ReticleRadius);
    //     bool hadLaserSight = this._laserSight;
    //     if (!hadLaserSight)
    //     {
    //         this._laserSight = VFX.CreateLaserSight(start, 1f, _SIGHT_WIDTH, angle, colour: Color.red/*, power: 50f*/)
    //             .GetComponent<tk2dTiledSprite>();
    //         this._laserSight.gameObject.transform.parent = this.sprite.transform;
    //         this._laserSight.usesOverrideMaterial = true;
    //         this._laserSight.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/SimpleAlphaFadeUnlit");
    //         this._laserSight.renderer.material.SetFloat("_Fade", 0.25f);
    //     }

    //     if (mag < 1f)
    //     {
    //         this._laserSight.renderer.enabled = false;
    //         return;
    //     }
    //     this._laserSight.dimensions = new Vector2(mag, _SIGHT_WIDTH);
    //     this._laserSight.gameObject.transform.localPosition = Vector2.zero;
    //     this._laserSight.gameObject.transform.rotation = angle.EulerZ();
    //     if (hadLaserSight)
    //     {
    //         this._laserSight.ForceRotationRebuild();
    //         this._laserSight.UpdateZDepth();
    //     }
    // }

    private void OnDestroy()
    {
        // if (this._laserSight)
        // {
        //     UnityEngine.Object.Destroy(this._laserSight);
        //     this._laserSight = null;
        // }
        if (this._mesh)
        {
            UnityEngine.Object.Destroy(this._mesh.gameObject);
            this._mesh = null;
        }

        if (!this.doVFX)
            return;

        tk2dMeshSprite ms = Lazy.CreateMeshSpriteObject(this.sprite, this.sprite.WorldCenter, pointMesh: true);
        ms.gameObject.transform.rotation = base.transform.rotation;
        ms.StartCoroutine(DoDissipate(ms, 0.5f));
    }

    private static IEnumerator DoMaterialize(XelsiorHoveringGun xg, tk2dMeshSprite ms, float v)
    {
        Material mat = ms.renderer.material;
        for (float elapsed = 0f; elapsed < v; elapsed += BraveTime.DeltaTime)
        {
            ms.transform.position = xg.transform.position;
            ms.transform.rotation = xg.transform.rotation;
            ms.FlipY = xg.sprite.FlipY;
            float percentLeft = 1f - elapsed / v;
            mat.SetFloat("_Fade", percentLeft);
            yield return null;
        }
        UnityEngine.Object.Destroy(xg._mesh.gameObject);
        xg._mesh = null;
        xg.sprite.renderer.enabled = true;
        yield break;
    }

    internal static IEnumerator DoDissipate(tk2dMeshSprite ms, float v)
    {
        ms.renderer.material.shader = CwaffShaders.ShatterShader;
        ms.renderer.material.SetFloat("_Progressive", 0f);
        ms.renderer.material.SetFloat("_Amplitude", 10f);
        ms.renderer.material.SetFloat("_RandomSeed", UnityEngine.Random.value);

        Material mat = ms.renderer.material;
        for (float elapsed = 0f; elapsed < v; elapsed += BraveTime.DeltaTime)
        {
            float percentLeft = 1f - elapsed / v;
            mat.SetFloat("_Fade", 1f - percentLeft * percentLeft);
            yield return null;
        }
        UnityEngine.Object.Destroy(ms.gameObject);
        yield break;
    }
}
