namespace CwaffingTheGungy;

public class Xelsior : CwaffGun
{
    public static string ItemName         = "X-elsior";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject _HoverGunPrefab = null;
    internal static GameObject _HoverProjectile = null;

    private List<XelsiorHoveringGun> _extantGuns = new();
    private AIActor _target = null;

    public int maxGuns = 0;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Xelsior>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 0.9f, ammo: 600, shootFps: 14, reloadFps: 4,
                muzzleFrom: Items.Mailbox, fireAudio: "platinum_fire_sound", reloadAudio: null);

        gun.InitProjectile(GunData.New(sprite: null, clipSize: 12, cooldown: 0.18f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 3.0f, speed: 25f, range: 18f, force: 12f, hitEnemySound: null, hitWallSound: null))
          // .Attach<XelsiorProjectile>()
          ;

        // _HoverGunPrefab = new GameObject("xelsior hovergun", typeof(tk2dSprite), typeof(XelsiorHoveringGun)).RegisterPrefab();
        _HoverGunPrefab = VFX.Create("xelsior_hover_gun");
        _HoverGunPrefab.AddComponent<XelsiorHoveringGun>();

        _HoverProjectile = Items.Ak47.AsGun().DefaultModule.projectiles[0].projectile.gameObject.ClonePrefab();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        projectile.OnHitEnemy += this.SetTargets;
    }

    private void SetTargets(Projectile projectile, SpeculativeRigidbody rigidbody, bool arg3)
    {
        if (rigidbody.aiActor is not AIActor enemy)
            return;
        if (enemy.healthHaver is not HealthHaver hh)
            return;
        if (hh.IsDead)
            return;
        this._target = enemy;
        int numGuns = this._extantGuns.Count;
        for (int i = 0; i < numGuns; ++i)
            this._extantGuns[i].SetTarget(enemy);
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
    }

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner)
            return;
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
    }

    private void DestroyExtantGuns(bool doVFX = true)
    {
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
        while (this.PlayerOwner && !this.PlayerOwner.AcceptingNonMotionInput)
            yield return null;
        if (!this.PlayerOwner)
            yield break;

        for (int i = 0; i < this.maxGuns; ++i)
            SpawnNewGun();
        UpdateGunOffsets();
    }
}

public class XelsiorHoveringGun : MonoBehaviour
{
    private const float _KICKBACK_TIME = 0.5f;
    private const float _MATERIALIZE_TIME = 0.5f;
    private const float _MAX_TRANSITION_TIME = 1.0f;

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
    private float _extraDist = 0f;
    private float _kickback = 0f;
    private float _transitionTimer = 0f;
    private tk2dMeshSprite _mesh = null;

    private LinkedList<float> _queuedShots = new();

    public XelsiorHoveringGun Setup(Xelsior parentGun)
    {
        this._parentGun = parentGun;
        this._owner = parentGun.PlayerOwner;

        this.sprite = base.gameObject.GetComponent<tk2dSprite>();
        // this.sprite.SetSprite(parentGun.sprite.collection, parentGun.sprite.spriteId);

        this._mesh = Lazy.CreateMeshSpriteObject(this.sprite, this.sprite.WorldCenter, pointMesh: true);
        // this._mesh.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Unfaded"));
        this._mesh.gameObject.transform.position = this.sprite.WorldCenter;
        this._mesh.gameObject.transform.rotation = base.transform.rotation;
        this._mesh.renderer.material.shader = CwaffShaders.ShatterShader;
        this._mesh.renderer.material.SetFloat("_Progressive", 1f);
        this._mesh.renderer.material.SetFloat("_Fade", 1f);
        this._mesh.renderer.material.SetFloat("_Amplitude", 4f);
        this._mesh.renderer.material.SetFloat("_RandomSeed", UnityEngine.Random.value + BraveTime.ScaledTimeSinceStartup);
        this.StartCoroutine(DoMaterialize(this, this._mesh, _MATERIALIZE_TIME));

        // Lazy.DebugLog($"have {this._mesh.renderer.materials.Length} materials");
        // foreach (Material m in this._mesh.renderer.materials)
        //     Lazy.DebugLog($"  {m.shader.name}");

        // Lazy.DebugLog($"have {this._mesh.renderer.sharedMaterials.Length} materials");
        // foreach (Material m in this._mesh.renderer.sharedMaterials)
        //     Lazy.DebugLog($"  {m.shader.name}");

        this.sprite.ForceUnlit();
        this.sprite.renderer.enabled = false;

        return this;
    }

    public void SetTarget(AIActor enemy)
    {
        if (enemy != this._target)
        {
            this._target = enemy;
            this._targetHH = enemy ? enemy.healthHaver : null;
            // this._extraDist = UnityEngine.Random.Range(-1f, 1f);
        }
        if (this._targetHH && this._targetHH.IsAlive)
            QueueShotAgainstTarget();
    }

    private void QueueShotAgainstTarget()
    {
        const float BASE_DELAY = 0.125f;
        const float MAX_DELAY = 0.4f;

        float myDelay = this.offset >= 0 ? this.offset : (this.numGuns + this.offset);
        float delay = BASE_DELAY + MAX_DELAY * (myDelay / this.numGuns);
        this._queuedShots.AddLast(BraveTime.ScaledTimeSinceStartup + delay);
    }

    private void HandleQueuedShots()
    {
        if (this._queuedShots.Count == 0)
            return;
        if (this._queuedShots.First.Value >= BraveTime.ScaledTimeSinceStartup)
            return;

        this._queuedShots.RemoveFirst();
        this._kickback = _KICKBACK_TIME;

        Vector2 pos = this.sprite.WorldCenter;
        GameObject po = SpawnManager.SpawnProjectile(Xelsior._HoverProjectile, pos, (this._target.CenterPosition - pos).EulerZ());
        po.PlayOnce("xelsior_fire_sound");

        Projectile proj = po.GetComponent<Projectile>();
        proj.SetOwnerAndStats(this._owner);
        proj.SetSpeed(60f);
        proj.ignoreDamageCaps = true;
        proj.baseData.damage = 2f;
        proj.baseData.range = 4f;
        proj.specRigidbody.CollideWithTileMap = false;
        proj.AddTrail(ChekhovsGun._ChekhovTrailPrefab).gameObject.SetGlowiness(10.0f);
    }

    private void Update()
    {
        const float SQR_SNAP_THRES     = 0.01f;
        const float SQR_UNTARGET_THRES = 0.04f;
        const float GUN_SPACING        = 0.75f;
        const float GUN_DIST           = 3.5f;
        const float HOVER_AMP          = 0.5f;
        const float HOVER_FREQ         = 3.0f;
        const float CIRCLE_SPEED       = 0.5f;
        const float LERP_RATE          = 7.5f;
        const float ANGULAR_LERP_RATE  = 9f;
        const float KICKBACK_STRENGTH  = 1.25f;

        if (!this._owner || !this._parentGun)
            return;

        this._kickback = Mathf.Max(this._kickback - BraveTime.DeltaTime, 0f);

        Vector2 basePos, backVec, tangent, targetPos;
        float targetAngle;

        if (this._target && this._targetHH && this._targetHH.IsAlive)
        {
            basePos = this._target.CenterPosition;
            this._lastTargetPos = basePos;
            float offAngle = 360f * CIRCLE_SPEED * BraveTime.ScaledTimeSinceStartup;
            targetAngle = (offAngle + 360f * this.offset / this.numGuns).Clamp360();
            targetPos = basePos + targetAngle.ToVector(GUN_DIST + this._extraDist);
            this._transitionToPlayer = true;
            this._transitionTimer = 0f;
            HandleQueuedShots();
        }
        else
        {
            basePos = this._owner.CenterPosition;
            targetAngle = (180f + this._owner.m_currentGunAngle).Clamp360();
            backVec = basePos + targetAngle.ToVector(GUN_DIST);
            tangent = (targetAngle + 90f).ToVector(GUN_SPACING);
            targetPos = backVec + this.offset * tangent;
            this._target = null;
            this._targetHH = null;
            this._queuedShots.Clear();
        }

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
        // else if (this._transitionToPlayer) // lerp magnitude and angle to target
        // {
        //     float curAngle = this.curAngle;
        //     float relAngle = this.curAngle.RelAngleTo(targetAngle);

        //     float curMag = (this.curPos - this._lastTargetPos).magnitude;
        //     float targetMag = GUN_DIST;

        //     Vector2 curCenter = this._lastTargetPos;
        //     Vector2 targetCenter = basePos;

        //     float newAngle = (curAngle + Lazy.SmoothestLerp(0, relAngle, LERP_RATE)).Clamp360();
        //     float newMag = Lazy.SmoothestLerp(curMag, targetMag, LERP_RATE);
        //     Vector2 newCenter = Lazy.SmoothestLerp(curCenter, targetCenter, LERP_RATE);
        //     this.curOffset = Lazy.SmoothestLerp(this.curOffset, this.offset, LERP_RATE);

        //     this._lastTargetPos = newCenter;
        //     this.curAngle = newAngle;
        //     Vector2 localTangent = (targetAngle + 90f).ToVector(GUN_SPACING);
        //     this.curPos = newCenter + newAngle.ToVector(newMag) + this.curOffset * localTangent;

        //     // Vector2 deltaToPlayer = this.curPos - basePos;
        //     // float magToPlayer = deltaToPlayer.magnitude;
        //     // float newMag = Lazy.SmoothestLerp(magToPlayer, GUN_DIST - 0.5f, LERP_RATE);
        //     // this.curPos = basePos + newMag * deltaToPlayer.normalized;
        //     // this.curAngle = deltaToPlayer.ToAngle();
        //     // if (newMag <= GUN_DIST)
        //     //     this._transitionToPlayer = false;
        // }
        else // snap to magnitude from target and lerp angle
        {
            this.curOffset = Lazy.SmoothestLerp(this.curOffset, this.offset, 5f);
            float relAngle = this.curAngle.RelAngleTo(targetAngle);
            this.curAngle = (this.curAngle + Lazy.SmoothestLerp(0, relAngle, ANGULAR_LERP_RATE)).Clamp360();
            Vector2 localBackVec = basePos + this.curAngle.ToVector(GUN_DIST);
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

    private void OnDestroy()
    {
        if (this._mesh)
        {
            UnityEngine.Object.Destroy(this._mesh.gameObject);
            this._mesh = null;
        }

        if (!this.doVFX)
            return;

        tk2dMeshSprite ms = Lazy.CreateMeshSpriteObject(this.sprite, this.sprite.WorldCenter, pointMesh: true);
        ms.gameObject.transform.rotation = base.transform.rotation;
        ms.renderer.material.shader = CwaffShaders.ShatterShader;
        ms.renderer.material.SetFloat("_Progressive", 0f);
        ms.renderer.material.SetFloat("_Amplitude", 10f);
        ms.renderer.material.SetFloat("_RandomSeed", this.offset + BraveTime.ScaledTimeSinceStartup);
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

    private static IEnumerator DoDissipate(tk2dMeshSprite ms, float v)
    {
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
