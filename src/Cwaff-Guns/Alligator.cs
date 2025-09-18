namespace CwaffingTheGungy;

public class Alligator : CwaffGun
{
    public static string ItemName         = "Alligator";
    public static string ShortDescription = "Shockingly Effective";
    public static string LongDescription  = "Fires clips that clamp onto enemies and periodically channel electricity from the player. Each clip's energy output is proportional to the player's damage stat, and increases further while rolling, walking over carpet, or standing in electrified goop. Up to 8 clips can be attached to each enemy. Passively grants electric immunity while in inventory.";
    public static string Lore             = "Most of the Gundead are either made of metal or carrying metal weaponry on them, making them rather hilariously susceptible to contact with live wires. Thanks to some fancy electrical engineering far beyond your comprehension, the Alligator allows you to channel the ambient static electricity you passively collect directly into the bodies of anything you can clip onto. Outside the Gungeon, it also doubles as an extremely handy tool for do-it-yourself home wiring projects.";

    private const float _ELECTRIFIED_ENERGY_BONUS    = 4.0f;
    private const float _ROLLING_ENERGY_BONUS        = 3.0f;
    private const float _CARPET_ENERGY_BONUS         = 1.5f;
    private const float _ELECTRIC_SLIDE_ENERGY_BONUS = 3.0f;
    private const float _ELECTRIC_DECAY_FACTOR       = 1.0f;
    private const float _ENERGY_MULT                 = 10.0f;

    internal static GameObject _SparkVFX           = null;
    internal static GameObject _ClipVFX            = null;
    internal static readonly Color _RedClipColor   = Color.Lerp(Color.red, Color.magenta, 0.25f);
    internal static readonly Color _BlackClipColor = Color.Lerp(Color.black, Color.blue, 0.25f);

    public float energyProduction                   = 0f;

    internal HashSet<AlligatorCableHandler> _cables = new();

    private DamageTypeModifier _electricImmunity    = null;
    private bool _ownerElectrified                  = false;
    private float _lastElectrifyCheck               = 0.0f;
    private Material _mat                           = null;

    public static void Init()
    {
        Lazy.SetupGun<Alligator>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.CHARGE, reloadTime: 2.0f, ammo: 300, shootFps: 20, reloadFps: 16,
            muzzleVFX: "muzzle_alligator", muzzleFps: 60, muzzleScale: 0.5f, muzzleAnchor: Anchor.MiddleCenter, muzzleEmission: 50f,
            fireAudio: "alligator_shoot_sound", reloadAudio: "alligator_reload_sound", dynamicBarrelOffsets: true)
          .Attach<AlligatorAmmoDisplay>()
          .InitProjectile(GunData.New(clipSize: 8, cooldown: 0.4f, angleVariance: 15.0f, shootStyle: ShootStyle.Automatic, customClip: true,
            damage: 1.0f, speed: 50.0f, sprite: "alligator_projectile", fps: 2, anchor: Anchor.MiddleCenter, electric: true))
          .Attach<AlligatorProjectile>();

        _SparkVFX = VFX.Create("spark_vfx", fps: 16, scale: 0.35f, emissivePower: 50f);
        _ClipVFX  = VFX.Create("alligator_clamped_vfx");
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        this._electricImmunity ??= new DamageTypeModifier {
            damageType = CoreDamageTypes.Electric,
            damageMultiplier = 0f,
        };
        base.OnPlayerPickup(player);
        AdjustGunShader(on: true);
        player.healthHaver.damageTypeModifiers.AddUnique(this._electricImmunity);
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        AdjustGunShader(on: false);
        player.healthHaver.damageTypeModifiers.TryRemove(this._electricImmunity);
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
            this.PlayerOwner.healthHaver.damageTypeModifiers.TryRemove(this._electricImmunity);
        base.OnDestroy();
    }

    public void AdjustGunShader(bool on)
    {
        Material m = this._mat = this.gun.sprite.renderer.material;
        if (!on)
        {
            this.gun.sprite.usesOverrideMaterial = false;
            m.shader = ShaderCache.Acquire("Brave/PlayerShader");
            return;
        }
        this.gun.sprite.usesOverrideMaterial = true;
        m.shader = CwaffShaders.ElectricShader;
        m.SetTexture("_ShaderTex", CwaffShaders.NoiseTexture);
        m.SetFloat("_Strength", 6.0f);
        m.SetFloat(CwaffVFX._EmissivePowerId, 400f);
    }

    public override void OwnedUpdate(GameActor owner, GunInventory inventory)
    {
        base.OwnedUpdate(owner, inventory);
        if (!this.PlayerOwner)
            return;
        CheckIfOwnerIsElectrified();
        CalculateEnergyProduction();
    }

    public override void Update()
    {
        base.Update();
        if (!this._mat)
            return;
        //NOTE: higher strength == lower brightness
        float s = Mathf.Max(2.0f, 6.0f - Mathf.Log(Mathf.Max(_ENERGY_MULT * (this.energyProduction - 1f), 1f), 4));  // max brightness at 4^4 == 256 energy
        this._mat.SetFloat("_Strength", s);
    }

    private void CalculateEnergyProduction()
    {
        float newProduction = 1.0f;

        float now = BraveTime.ScaledTimeSinceStartup;
        newProduction *= this.PlayerOwner.DamageMult();
        if (this.PlayerOwner.IsDodgeRolling)
            newProduction *= _ROLLING_ENERGY_BONUS;
        if (this._ownerElectrified)
            newProduction *= _ELECTRIFIED_ENERGY_BONUS;
        else if (this.PlayerOwner.specRigidbody.Velocity.sqrMagnitude > 0.1f // else to avoid stacking with water tiles
          && GameManager.Instance.Dungeon.GetFloorTypeFromPosition(this.PlayerOwner.specRigidbody.UnitBottomCenter) == CellVisualData.CellFloorType.Carpet)
        {
            newProduction *= _CARPET_ENERGY_BONUS;
            if (this.PlayerOwner.HasSynergy(Synergy.ELECTRIC_SLIDE))
                newProduction *= _ELECTRIC_SLIDE_ENERGY_BONUS;
        }

        if (newProduction >= this.energyProduction)
            this.energyProduction = newProduction;
        else
        {
            float decay = _ELECTRIC_DECAY_FACTOR;
            if (this.Mastered)
                decay *= 0.1f;
            this.energyProduction = Mathf.Max(newProduction, Lazy.SmoothestLerp(this.energyProduction, 0, decay));
        }
    }

    private void CheckIfOwnerIsElectrified()
    {
        float now = BraveTime.ScaledTimeSinceStartup;
        if (now - this._lastElectrifyCheck < 0.1f)
            return;

        this._lastElectrifyCheck = now;
        this._ownerElectrified = false;
        RoomHandler room = this.PlayerOwner.CurrentRoom;
        Vector2 pos = this.PlayerOwner.SpriteBottomCenter;
        if (room == null || room.RoomGoops == null)
            return;
        foreach (DeadlyDeadlyGoopManager goopManager in room.RoomGoops)
            if (goopManager.IsPositionElectrified(pos))
            {
                this._ownerElectrified = true;
                break;
            }
    }

    private class AlligatorAmmoDisplay : CustomAmmoDisplay
    {
        private Gun _gun;
        private Alligator _alligator;
        private PlayerController _owner;

        private void Start()
        {
            this._gun       = base.GetComponent<Gun>();
            this._alligator = this._gun.GetComponent<Alligator>();
            this._owner     = this._gun.CurrentOwner as PlayerController;
        }

        public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
        {
            if (!this._owner)
                return false;

            string energy = Mathf.RoundToInt(_ENERGY_MULT * this._alligator.energyProduction).ToString();
            uic.GunAmmoCountLabel.Text = $"[color #00ffff]{energy}[/color][sprite \"charge_ui\"]\n{this._owner.VanillaAmmoDisplay()}";
            return true;
        }
    }
}

public class AlligatorProjectile : MonoBehaviour
{
    const int _MAX_CLIPS_PER_ENEMY = 8;

    private void Start()
    {
        Projectile p = base.gameObject.GetComponent<Projectile>();
            p.OnHitEnemy += HandleHitEnemy;
        base.gameObject.AddComponent<AlligatorCableHandler>()
            .Initialize(p.Owner as PlayerController, null, p.transform, p.specRigidbody.HitboxPixelCollider.UnitCenter - p.transform.position.XY());
    }

    private void HandleHitEnemy(Projectile projectile, SpeculativeRigidbody body, bool _)
    {
        if (body.aiActor is not AIActor aiActor)
            return;
        if (body.healthHaver is not HealthHaver hh)
            return;
        if (!aiActor.IsHostile(canBeNeutral: true) || aiActor.gameObject.GetComponents<AlligatorCableHandler>().Length >= _MAX_CLIPS_PER_ENEMY)
            return;
        new GameObject().AddComponent<AlligatorCableHandler>()
            .Initialize(projectile.Owner as PlayerController, aiActor, aiActor.transform, aiActor.CenterPosition - aiActor.transform.position.XY());
    }
}

// modified from basegame ArbitraryCableDrawer
public class AlligatorCableHandler : MonoBehaviour
{
    private const int _SEGMENTS            = 10;
    private const float _SPARK_TRAVEL_TIME = 0.3f;
    private const float _MIN_DROP_SPEED    = 5f;
    private const float _MAX_DROP_SPEED    = 20f;
    private const float _DROP_FRICTION     = 0.9f;
    private const float _SPARK_DAMAGE      = 2.0f;

    private MeshFilter _stringFilter;
    private Transform _startTransform;
    private Transform _endTransform;
    private Mesh _mesh;
    private Vector3[] _vertices;
    private PlayerController _owner     = null;
    private AIActor _enemy              = null;
    private int _ownerId                = -1;
    private bool _targetingEnemy        = false;
    private GameObject _clippyboi       = null;
    private Vector2 _ownerOffset        = Vector2.zero;
    private Vector2 _endTransformOffset = Vector2.zero;
    private float _energyProduced       = 0f;
    private Alligator _alligator        = null;
    private bool _fallen                = false;
    private bool _settled               = false;
    private Vector3 _dropVelocity       = Vector3.zero;
    private Vector3 _dropOffsetVector   = Vector3.zero;
    private Vector3 _lastGoodEndPos     = Vector3.zero;

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
            meshRenderer.material.SetColor(CwaffVFX._OverrideColorId, Alligator._RedClipColor);

        if (this._owner && this._targetingEnemy)
        {
            float now = BraveTime.ScaledTimeSinceStartup;
            this._extantSparks.Add(SpawnManager.SpawnVFX(Alligator._SparkVFX, this._startTransform.position, Quaternion.identity));
            this._extantSpawnTimes.Add(now);

            Vector3 spriteSize = this._enemy.sprite.GetBounds().size;
                float randomXOffset = 0.25f * spriteSize.x * UnityEngine.Random.value * BraveUtility.RandomSign();
                float randomYOffset = 0.25f * spriteSize.y * UnityEngine.Random.value * BraveUtility.RandomSign();
            this._endTransformOffset += new Vector2(randomXOffset, randomYOffset);

            this._clippyboi = SpawnManager.SpawnVFX(Alligator._ClipVFX, clipTransform.position, Quaternion.identity);
                this._clippyboi.transform.parent = clipTransform;
                this._clippyboi.transform.localPosition = _endTransformOffset;
                tk2dSprite clippySprite = this._clippyboi.GetComponent<tk2dSprite>();
                    clippySprite.HeightOffGround = 10f;

            if (this._owner.CurrentGun.GetComponent<Alligator>() is Alligator a1)
                this._alligator = a1;
            else if (this._owner.GetGun<Alligator>() is Alligator a2)
                this._alligator = a2;
            if (this._alligator)
                this._alligator._cables.Add(this);
        }
    }

    private void FallOnFloor()
    {
        if (this._alligator)
        {
            this._alligator._cables.Remove(this);
            this._alligator = null;
        }

        for (int i = this._extantSparks.Count - 1; i >= 0; --i)
            if (this._extantSparks[i])
                UnityEngine.Object.Destroy(this._extantSparks[i]);
        this._extantSparks.Clear();

        base.gameObject.SetLayerRecursively(LayerMask.NameToLayer("BG_Critical")); // render below most objects
        base.transform.position = this._startTransform.position.WithZ(10f);
        this._startTransform = base.transform;

        this._endTransform = new GameObject().transform;
        this._endTransform.position = this._lastGoodEndPos;
        if (this._clippyboi)
        {
            this._clippyboi.transform.parent = this._endTransform;
            this._clippyboi.transform.localRotation = Lazy.RandomEulerZ();
        }

        this._dropVelocity     = Lazy.RandomVector(UnityEngine.Random.Range(_MIN_DROP_SPEED, _MAX_DROP_SPEED)).ToVector3ZUp();
        this._dropOffsetVector = Lazy.RandomVector(0.5f + 1.5f * UnityEngine.Random.value).ToVector3ZUp();
        this._enemy            = null;
        this._targetingEnemy   = false;
        this._fallen           = true;
        this._settled          = false;
    }

    private void HandleFallenOnFloor()
    {
        if (this._settled)
            return;

        float dtime = BraveTime.DeltaTime;
        this._endTransform.position += dtime * this._dropVelocity;
        this._startTransform.position = Lazy.SmoothestLerp(this._startTransform.position, this._endTransform.position, 10f);
        this._dropVelocity *= Mathf.Pow(_DROP_FRICTION, dtime * C.FPS);
        if (this._dropVelocity.sqrMagnitude < 1f)
            this._settled = true;

        Vector3 vector = this._startTransform.position;
        Vector3 vector2 = this._endTransform.position + this._endTransformOffset.ToVector3ZisY();
        BuildMeshAlongCurveAndUpdateSparks(vector, vector, vector2 + this._dropOffsetVector, vector2);
        this._mesh.vertices = this._vertices;
        this._mesh.RecalculateBounds();
        this._mesh.RecalculateNormals();
    }

    private void LateUpdate()
    {
        if (this._fallen)
        {
            HandleFallenOnFloor();
            return;
        }
        if (this._targetingEnemy && !(this._enemy && this._enemy.healthHaver && this._enemy.healthHaver.IsAlive))
        {
            FallOnFloor();
            return;
        }
        if (!this._owner || !this._owner.CurrentGun)
        {
            UnityEngine.Object.Destroy(base.gameObject);
            return;
        }

        if (!this._startTransform)
            this._startTransform = this._owner.CurrentGun.barrelOffset;

        if (!this._startTransform || !this._endTransform)
            return;

        Vector3 vector;
        Gun gun = this._owner.CurrentGun;
        if (!gun.GetComponent<Alligator>() || !gun.renderer.enabled)
            vector = this._owner.CenterPosition.ToVector3ZisY();
        else
            vector = this._startTransform.position.XY().ToVector3ZisY(-3f);

        this._lastGoodEndPos = this._endTransform.position;
        Vector3 vector2 = this._lastGoodEndPos.XY().ToVector3ZisY(-3f) + this._endTransformOffset.ToVector3ZisY();
        BuildMeshAlongCurveAndUpdateSparks(vector, vector, vector2 + new Vector3(0f, -2f, -2f), vector2);
        this._mesh.vertices = this._vertices;
        this._mesh.RecalculateBounds();
        this._mesh.RecalculateNormals();
    }

    private void OnDestroy()
    {
        if (this._alligator)
            this._alligator._cables.Remove(this);
        for (int i = this._extantSparks.Count - 1; i >= 0; --i)
            if (this._extantSparks[i])
                UnityEngine.Object.Destroy(this._extantSparks[i]);
        if (this._clippyboi)
            UnityEngine.Object.Destroy(this._clippyboi);
        if (this._stringFilter)
            UnityEngine.Object.Destroy(this._stringFilter.gameObject);
        if (this._fallen && this._endTransform)
            UnityEngine.Object.Destroy(this._endTransform.gameObject);
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
            vertices[i * 2] = (vector2 + offset * meshWidth).ToVector3ZisY(this._fallen ? 10f : -10f);
            vertices[i * 2 + 1] = (vector2 + -offset * meshWidth).ToVector3ZisY(this._fallen ? 10f : -10f);
            vector = vector2;
        }

        if (!this._targetingEnemy)
            return;

        float curTime = BraveTime.ScaledTimeSinceStartup;
        if (this._alligator && this._alligator.energyProduction > 0f)
        {
            this._energyProduced += this._alligator.energyProduction * BraveTime.DeltaTime;
            if (this._energyProduced > 1f)
            {
                this._energyProduced -= 1f;
                this._extantSparks.Add(SpawnManager.SpawnVFX(Alligator._SparkVFX, _startTransform.position, Quaternion.identity));
                this._extantSpawnTimes.Add(curTime);
            }
        }

        for (int i = _extantSparks.Count - 1; i >= 0; --i)
        {
            if (!this._enemy || !this._enemy.healthHaver)
                break;  // can happen if a previous spark killed the enemy
            float percentDone = (curTime - _extantSpawnTimes[i]) / _SPARK_TRAVEL_TIME;
            if (percentDone > 1f)
            {
                this._enemy.healthHaver.ApplyDamage(_SPARK_DAMAGE, Vector2.zero, Alligator.ItemName, CoreDamageTypes.Electric, DamageCategory.Normal);
                base.gameObject.PlayUnique("electrocution_sound");
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
