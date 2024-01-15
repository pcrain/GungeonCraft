namespace CwaffingTheGungy;

public class Alligator : AdvancedGunBehavior
{
    public static string ItemName         = "Alligator";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Shockingly Effective";
    public static string LongDescription  = "Fires clips that clamp onto enemies and periodically channel electricity from the player. Energy output is proportional to the player's damage stat, and increases further while rolling or while in electrified goop. Each clip channels the full energy output, and up to 8 clips can be attached to each enemy. Passively grants electric immunity while in inventory.";
    public static string Lore             = "Most of the Gundead are either made of metal or carrying metal weaponry on them, making them rather hilariously susceptible to contact with live wires. Thanks to some fancy electrical engineering far beyond your comprehension, the Alligator allows you to channel the ambient static electricity you passively collect directly into the bodies of anything you can clip onto. Outside the Gungeon, it also doubles as an extremely handy tool for do-it-yourself home wiring projects.";

    internal static GameObject _SparkVFX               = null;
    internal static GameObject _ClipVFX                = null;
    internal static readonly Color _RedClipColor       = Color.Lerp(Color.red, Color.magenta, 0.25f);
    internal static readonly Color _BlackClipColor     = Color.Lerp(Color.black, Color.blue, 0.25f);
    internal static List<Vector3> _ShootBarrelOffsets  = new();
    internal static List<Vector3> _ReloadBarrelOffsets = new();

    private DamageTypeModifier _electricImmunity = null;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Alligator>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.CHARGE, reloadTime: 2.0f, ammo: 300);
            gun.SetAnimationFPS(gun.shootAnimation, 20);
            gun.SetAnimationFPS(gun.reloadAnimation, 16);
            gun.SetMuzzleVFX("muzzle_alligator", fps: 60, scale: 0.5f, anchor: Anchor.MiddleCenter, emissivePower: 50f);
            gun.SetFireAudio("alligator_shoot_sound");
            gun.SetReloadAudio("alligator_reload_sound");

        gun.InitProjectile(new(clipSize: 8, cooldown: 0.4f, angleVariance: 15.0f, shootStyle: ShootStyle.Automatic, customClip: true,
          damage: 1.0f, speed: 36.0f, sprite: "alligator_projectile", fps: 2, anchor: Anchor.MiddleCenter
          )).Attach<AlligatorProjectile>();

        _ShootBarrelOffsets  = gun.GetBarrelOffsetsForAnimation(gun.shootAnimation);
        _ReloadBarrelOffsets = gun.GetBarrelOffsetsForAnimation(gun.reloadAnimation);
        _SparkVFX            = VFX.Create("spark_vfx", fps: 16, loops: true, anchor: Anchor.MiddleCenter, scale: 0.35f, emissivePower: 50f);
        _ClipVFX             = VFX.Create("alligator_projectile_clamped", fps: 2, loops: true, anchor: Anchor.MiddleCenter);
    }

    protected override void OnPickedUpByPlayer(PlayerController player)
    {
        if (!everPickedUpByPlayer)
            this._electricImmunity = new DamageTypeModifier {
                damageType = CoreDamageTypes.Electric,
                damageMultiplier = 0f,
            };
        base.OnPickedUpByPlayer(player);

        if (!player.healthHaver.damageTypeModifiers.Contains(this._electricImmunity))
            player.healthHaver.damageTypeModifiers.Add(this._electricImmunity);
    }

    protected override void OnPostDroppedByPlayer(PlayerController player)
    {
        base.OnPostDroppedByPlayer(player);

        if (player.healthHaver.damageTypeModifiers.Contains(this._electricImmunity))
            player.healthHaver.damageTypeModifiers.Remove(this._electricImmunity);
    }

    protected override void Update()
    {
        base.Update();

        // ETGModConsole.Log($"    CURRENT ID {gun.sprite.spriteId}");

        tk2dSpriteAnimator anim = gun.spriteAnimator;
        if (anim.IsPlaying(gun.shootAnimation))
            gun.barrelOffset.localPosition = _ShootBarrelOffsets[anim.CurrentFrame];
        else if (anim.IsPlaying(gun.reloadAnimation))
            gun.barrelOffset.localPosition = _ReloadBarrelOffsets[anim.CurrentFrame];
        else
            gun.barrelOffset.localPosition = _ShootBarrelOffsets[0];

        if (gun.sprite.FlipY)
            gun.barrelOffset.localPosition = gun.barrelOffset.localPosition.WithY(-gun.barrelOffset.localPosition.y);
    }
}

public class AlligatorProjectile : MonoBehaviour
{
    const int _MAX_CLIPS_PER_ENEMY = 8;

    private void Start()
    {
        Projectile p = GetComponent<Projectile>();
            p.OnHitEnemy += HandleHitEnemy;
        AlligatorCableHandler cable = base.gameObject.AddComponent<AlligatorCableHandler>();
            cable.Initialize(p.Owner as PlayerController, null, p.transform, p.specRigidbody.HitboxPixelCollider.UnitCenter - p.transform.position.XY());
            cable._stringFilter.GetComponent<MeshRenderer>().material.SetColor("_OverrideColor", Alligator._RedClipColor);
    }

    private void HandleHitEnemy(Projectile projectile, SpeculativeRigidbody body, bool _)
    {
        if (body.aiActor is not AIActor aiActor)
            return;
        if (!aiActor.IsHostile(canBeNeutral: true) || aiActor.gameObject.GetComponents<AlligatorCableHandler>().Count() >= _MAX_CLIPS_PER_ENEMY)
            return;
        AlligatorCableHandler cable = aiActor.gameObject.AddComponent<AlligatorCableHandler>();
            cable.Initialize(projectile.Owner as PlayerController, aiActor, aiActor.transform, aiActor.CenterPosition - aiActor.transform.position.XY());
            cable._stringFilter.GetComponent<MeshRenderer>().material.SetColor("_OverrideColor", Alligator._RedClipColor);
    }
}

// modified from basegame ArbitraryCableDrawer
public class AlligatorCableHandler : MonoBehaviour
{
    const int _SEGMENTS                   = 10;
    const float _SPARK_TRAVEL_TIME        = 0.3f;
    const float _ELECTRIFIED_ENERGY_BONUS = 4.0f;
    const float _ROLLING_ENERGY_BONUS     = 3.0f;

    private static bool[] _PlayerElectrified                            = {false, false};
    private static float[] _LastElectrifiedCheck                        = {0f, 0f};
    private static float[] _PlayerEnergyProductionRate                  = {1f, 1f};
    private static float[] _LastEnergyCheck                             = {0f, 0f};
    private static HashSet<AlligatorCableHandler>[] _PlayerExtantCables = {new(), new()};

    public Transform _startTransform;
    public Transform _endTransform;

    public Mesh _mesh;
    public Vector3[] _vertices;
    public MeshFilter _stringFilter;

    private PlayerController _owner     = null;
    private AIActor _enemy              = null;
    private int _ownerId                = -1;
    private float _energyProduced       = 0f;
    private bool _targetingEnemy        = false;
    private GameObject _clippyboi       = null;
    private Vector2 _ownerOffset        = Vector2.zero;
    private Vector2 _endTransformOffset = Vector2.zero;

    internal List<GameObject> _extantSparks = new();
    internal List<float> _extantSpawnTimes  = new();

    public void Initialize(PlayerController owner, AIActor target, Transform clipTransform, Vector2 clipTransformOffset)
    {
        this._owner              = owner;
        this._ownerOffset        = owner.CenterPosition - owner.transform.position.XY();
        this._ownerId            = owner.PlayerIDX;
        this._enemy              = target;
        this._targetingEnemy     = target != null;
        this._startTransform     = owner.CurrentGun.barrelOffset;
        this._endTransform       = clipTransform;
        this._endTransformOffset = clipTransformOffset;
        this._mesh               = new Mesh();
        this._vertices           = new Vector3[2 * _SEGMENTS];
        this._mesh.vertices      = this._vertices;
        int[] array              = new int[6 * (_SEGMENTS - 1)];
        Vector2[] uv             = new Vector2[2 * _SEGMENTS];
        int num                  = 0;
        for (int i = 0; i < (_SEGMENTS - 1); i++)
        {
            array[i * 6]     = num;
            array[i * 6 + 1] = num + 2;
            array[i * 6 + 2] = num + 1;
            array[i * 6 + 3] = num + 2;
            array[i * 6 + 4] = num + 3;
            array[i * 6 + 5] = num + 1;
            num += 2;
        }
        this._mesh.triangles          = array;
        this._mesh.uv                 = uv;
        GameObject gameObject         = new GameObject("cableguy");
        this._stringFilter            = gameObject.AddComponent<MeshFilter>();
        this._stringFilter.mesh       = _mesh;
        MeshRenderer meshRenderer     = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.material     = BraveResources.Load("Global VFX/WhiteMaterial", ".mat") as Material;
            // meshRenderer.material.SetColor("_OverrideColor", Color.black);

        if (this._owner && this._targetingEnemy)
        {
            float now = BraveTime.ScaledTimeSinceStartup;
            this._extantSparks.Add(SpawnManager.SpawnVFX(Alligator._SparkVFX, this._startTransform.position, Quaternion.identity));
            this._extantSpawnTimes.Add(now);
            this._energyProduced = 0;

            Vector3 spriteSize = this._enemy.sprite.GetBounds().size;
                float randomXOffset = 0.25f * spriteSize.x * UnityEngine.Random.value * BraveUtility.RandomSign();
                float randomYOffset = 0.25f * spriteSize.y * UnityEngine.Random.value * BraveUtility.RandomSign();
            this._endTransformOffset += new Vector2(randomXOffset, randomYOffset);

            this._clippyboi = SpawnManager.SpawnVFX(Alligator._ClipVFX, clipTransform.position, Quaternion.identity);
                this._clippyboi.transform.parent = clipTransform;
                this._clippyboi.transform.localPosition = _endTransformOffset;
                tk2dSprite clippySprite = this._clippyboi.GetComponent<tk2dSprite>();
                    clippySprite.HeightOffGround = 10f;

            _PlayerExtantCables[this._ownerId].Add(this);
        }
    }

    private void CheckIfOwnerIsElectrified()
    {
        float now = BraveTime.ScaledTimeSinceStartup;
        if (now - _LastElectrifiedCheck[this._ownerId] < 0.1f)
            return;

        _LastElectrifiedCheck[this._ownerId] = now;
        _PlayerElectrified[this._ownerId]    = false;
        RoomHandler absoluteRoomFromPosition = GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(this._owner.specRigidbody.UnitCenter.ToIntVector2());
        foreach (DeadlyDeadlyGoopManager goopManager in absoluteRoomFromPosition?.RoomGoops.EmptyIfNull())
            if (goopManager.IsPositionElectrified(this._owner.specRigidbody.UnitCenter))
            {
                _PlayerElectrified[this._ownerId] = true;
                break;
            }
    }

    private void CalculateEnergyProduction()
    {
        float now = BraveTime.ScaledTimeSinceStartup;
        if ((now - _LastEnergyCheck[this._ownerId]) < 0.01f)
            return;
        _LastEnergyCheck[this._ownerId] = now;

        float energyOutput = this._owner.stats.GetStatValue(PlayerStats.StatType.Damage);
        if (this._owner.IsDodgeRolling)
            energyOutput *= _ROLLING_ENERGY_BONUS;
        if (_PlayerElectrified[this._ownerId])
            energyOutput *= _ELECTRIFIED_ENERGY_BONUS;

        _PlayerEnergyProductionRate[this._ownerId] = energyOutput;
    }

    private void LateUpdate()
    {
        if (!this._owner || (this._targetingEnemy && !(this._enemy?.healthHaver?.IsAlive ?? false)))
            UnityEngine.Object.Destroy(this);

        if (!this._startTransform || !this._endTransform)
            return;

        CheckIfOwnerIsElectrified();
        CalculateEnergyProduction();

        Vector3 vector;
        Gun gun = this._owner.CurrentGun;
        if (!gun.GetComponent<Alligator>() || !gun.renderer.enabled)
            vector = this._owner.CenterPosition.ToVector3ZisY();
        else
            vector = this._startTransform.position.XY().ToVector3ZisY(-3f);

        Vector3 vector2 = this._endTransform.position.XY().ToVector3ZisY(-3f) + this._endTransformOffset.ToVector3ZisY();
        BuildMeshAlongCurveAndUpdateSparks(vector, vector, vector2 + new Vector3(0f, -2f, -2f), vector2);
        this._mesh.vertices = this._vertices;
        this._mesh.RecalculateBounds();
        this._mesh.RecalculateNormals();
    }

    private void OnDestroy()
    {
        _PlayerExtantCables[this._ownerId].Remove(this);
        this._clippyboi.SafeDestroy();
        if (this._stringFilter)
            UnityEngine.Object.Destroy(this._stringFilter.gameObject);
        for (int i = 0; i < _extantSparks.Count(); ++i)
            UnityEngine.Object.Destroy(this._extantSparks[i]);
    }

    private void BuildMeshAlongCurveAndUpdateSparks(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float meshWidth = 1f / 32f)
    {
        Vector3[] vertices = this._vertices;
        Vector2? vector    = null;
        Quaternion euler90 = Quaternion.Euler(0f, 0f, 90f);

        for (int i = 0; i < _SEGMENTS; i++)
        {
            Vector2 vector2 = BraveMathCollege.CalculateBezierPoint((float)i / 9f, p0, p1, p2, p3);
            Vector2? vector3 = ((i != (_SEGMENTS - 1)) ? new Vector2?(BraveMathCollege.CalculateBezierPoint((float)i / (float)(_SEGMENTS - 1), p0, p1, p2, p3)) : null);
            Vector2 offset = Vector2.zero;
            if (vector.HasValue)
                offset += (euler90 * (vector2 - vector.Value)).XY().normalized;
            if (vector3.HasValue)
                offset += (euler90 * (vector3.Value - vector2)).XY().normalized;
            offset = offset.normalized;
            vertices[i * 2] = (vector2 + offset * meshWidth).ToVector3ZisY(-10f);
            vertices[i * 2 + 1] = (vector2 + -offset * meshWidth).ToVector3ZisY(-10f);
            vector = vector2;
        }

        if (!this._targetingEnemy)
            return;

        float curTime = BraveTime.ScaledTimeSinceStartup;
        this._energyProduced += _PlayerEnergyProductionRate[this._ownerId] * BraveTime.DeltaTime;
        if (this._energyProduced >= 1f)
        {
            this._energyProduced -= 1f;
            this._extantSparks.Add(SpawnManager.SpawnVFX(Alligator._SparkVFX, _startTransform.position, Quaternion.identity));
            this._extantSpawnTimes.Add(curTime);
        }

        for (int i = _extantSparks.Count() - 1; i >= 0; --i)
        {
            if (!this._enemy?.healthHaver)
                break;  // can happen if a previous spark killed the enemy
            float percentDone = (curTime - _extantSpawnTimes[i]) / _SPARK_TRAVEL_TIME;
            if (percentDone > 1f)
            {
                this._enemy.healthHaver.ApplyDamage(1f, Vector2.zero, Alligator.ItemName, CoreDamageTypes.Electric, DamageCategory.Normal);
                AkSoundEngine.PostEvent("electrocution_sound_stop_all", base.gameObject);
                AkSoundEngine.PostEvent("electrocution_sound", base.gameObject);
                UnityEngine.Object.Destroy(_extantSparks[i]);
                this._extantSparks.RemoveAt(i);
                this._extantSpawnTimes.RemoveAt(i);
                continue;
            }
            Vector2 sparkPos = BraveMathCollege.CalculateBezierPoint(percentDone, p0, p1, p2, p3);
            this._extantSparks[i].transform.position = sparkPos;
            if (UnityEngine.Random.value < 0.2f)
                GlobalSparksDoer.DoRandomParticleBurst(5, sparkPos, sparkPos, Lazy.RandomVector(4f), 360f, 0f,
                    startLifetime: 0.2f,
                    startColor: Color.cyan,
                    systemType: GlobalSparksDoer.SparksType.SPARKS_ADDITIVE_DEFAULT);
        }
    }
}
