namespace CwaffingTheGungy;

/* TODO:
    - properly reset animators
    - [maybe] fix proxy indexoutofrange exceptions on floor load and reenable pooling across floors
*/

public class PlayerProjectilePoolInfo : MonoBehaviour
{
  // cached starting invocation lists
  public Action<Projectile> OnDestruction;
  public SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate OnPreRigidbodyCollision;
  public Action<CollisionData> OnCollision;
  public SpeculativeRigidbody.OnPreTileCollisionDelegate OnPreTileCollision;
  public SpeculativeRigidbody.OnTileCollisionDelegate OnTileCollision;
  public SpeculativeRigidbody.OnRigidbodyCollisionDelegate OnRigidbodyCollision;

  // internal stuff
  internal Projectile projectile = null;
  internal PlayerProjectilePooler pooler = null;
  internal LinkedListNode<GameObject> node = null;
  internal List<Transform> startingTransforms = new();
  internal bool spawned = false;

  // stupid events ):<
  private static FieldInfo _OnDestructionBackingField
    = typeof(Projectile).GetField("OnDestruction", BindingFlags.Instance | BindingFlags.NonPublic);

  internal LinkedListNode<GameObject> Register(PlayerProjectilePooler pooler)
  {
    this.pooler = pooler;
    this.node = new LinkedListNode<GameObject>(base.gameObject);
    RegisterStartingTransforms(base.gameObject.transform);
    this.spawned = true;
    return this.node;
  }

  internal void RestoreDelegates()
  {
    _OnDestructionBackingField.SetValue(this.projectile, this.OnDestruction);
    SpeculativeRigidbody body    = this.projectile.specRigidbody;
    body.OnPreRigidbodyCollision = this.OnPreRigidbodyCollision;
    body.OnCollision             = this.OnCollision;
    body.OnPreTileCollision      = this.OnPreTileCollision;
    body.OnTileCollision         = this.OnTileCollision;
    body.OnRigidbodyCollision    = this.OnRigidbodyCollision;
  }

  private void RegisterStartingTransforms(Transform root)
  {
      this.startingTransforms.Add(root);
      // System.Console.WriteLine($"  we have {root.name} vs {this.pooler._prefabTransforms[this.startingTransforms.Count - 1].name}");
      foreach (Transform child in root)
        RegisterStartingTransforms(child);
  }
}

[HarmonyPatch]
internal class PlayerProjectilePooler
{
  internal static readonly List<Type> _NoPurgeWhitelist = [
    typeof(Transform),                // special
    typeof(PlayerProjectilePoolInfo), // special
    typeof(Projectile),               // unique handling
    typeof(SpeculativeRigidbody),     // unique handling
    typeof(tk2dSprite),               // unique handling
    typeof(MeshFilter),               // required and handled by tk2dSprite
    typeof(MeshRenderer),             // required and handled by tk2dSprite
  ];

  /// <summary>List of pooled player projectiles that have been spawned in, but haven't been assigned an owner yet. Need to defer calling Start() until owner has been assigned.</summary>
  private static readonly List<Projectile> _AwaitingNewOwner = new();
  private static bool _DoingFloorCleanup = false;
  private static List<Component> _Components = new();

  /// <summary>Map of all poolable projectile prefabs to their pooler.</summary>
  internal static readonly Dictionary<GameObject, PlayerProjectilePooler> _Poolers = new();

  private GameObject _prefab;
  private string _name;
  private List<Transform> _prefabTransforms = new();
  private LinkedList<GameObject> _despawned = new();
  private LinkedList<GameObject> _spawned = new();

  internal static void RegisterAsPoolable(Projectile p)
  {
    GameObject prefab = p.gameObject;
    PlayerProjectilePooler pooler = new();
    pooler._prefab = prefab;
    pooler._name = prefab.name;
    if (pooler.RegisterPrefabTransforms(prefab.transform))
      PlayerProjectilePooler._Poolers[prefab] = pooler;
  }

  /// <summary>Returns false if this projectile cannot be pooled.</summary>
  private bool RegisterPrefabTransforms(Transform root)
  {
    this._prefabTransforms.Add(root);
    root.gameObject.GetComponents<Component>(_Components);
    foreach (Component c in _Components)
    {
      if (c is IPPPComponent || _NoPurgeWhitelist.Contains(c.GetType()))
        continue;
      Lazy.DebugWarn($"Attempted to register poolable projectile with non-poolable component {c.GetType().Name}.");
      return false;
    }
    int numChildren = root.childCount;
    for (int i = 0; i < numChildren; ++i)
      if (!RegisterPrefabTransforms(root.GetChild(i)))
        return false;
    return true;
  }

  private static void ClearPoolsForNextFloor()
  {
    try
    {
      _DoingFloorCleanup = true;
      Lazy.DebugLog($"cleaning up projectile pools!");
      foreach (PlayerProjectilePooler pooler in _Poolers.Values)
      {
        Lazy.DebugLog($"  {pooler._name} pool has {pooler._spawned.Count} spawned and {pooler._despawned.Count} despawned projectiles");
        while (pooler._spawned.Count > 0) // critical to make sure poolable components clean up their transforms properly (e.g., EasyTrailBullet returning its CustomTrailRenderer)
          pooler.Despawn(pooler._spawned.Last.Value, destroy: true);
        while (pooler._despawned.Count > 0)
        {
          UnityEngine.Object.Destroy(pooler._despawned.Last.Value);
          pooler._despawned.RemoveLast();
        }
      }
    }
    finally
    {
      _DoingFloorCleanup = false;
    }
  }

  #region Debug patches
  #if DEBUG
  // [HarmonyPatch(typeof(UnityEngine.Object), nameof(UnityEngine.Object.Destroy), typeof(UnityEngine.Object))]
  // [HarmonyPrefix]
  // private static void DebugPatch(UnityEngine.Object obj)
  // {
  //   if (!_DoingFloorCleanup && obj is GameObject go && go.GetComponent<PlayerProjectilePoolInfo>())
  //   {
  //     System.Console.WriteLine($"destroying our projectile, that doesn't seem good");
  //     UnityEngine.Debug.LogError($"destroying our projectile, that doesn't seem good");
  //   }
  // }

  // [HarmonyPatch(typeof(BitArray2D), MethodType.Constructor, typeof(bool))]
  // [HarmonyPrefix]
  // private static void DebugPatch()
  // {
  //   const string MSG = "new BitArray2D";
  //   System.Console.WriteLine(MSG);
  //   UnityEngine.Debug.LogError(MSG);
  // }

  #endif
  #endregion

  /// <summary>Make sure projectile is set up after its Owner has been assigned</summary>
  [HarmonyPatch(typeof(Projectile), nameof(Projectile.Owner), MethodType.Setter)]
  [HarmonyPatch(typeof(Projectile), nameof(Projectile.SetOwnerSafe))]
  [HarmonyPostfix]
  private static void OnProjectileOwnerAssignedPatch(Projectile __instance)
  {
    if (!_AwaitingNewOwner.Remove(__instance))
      return;

    // __instance.Start(); // OnSpawned calls Start
    __instance.OnSpawned(); //NOTE: this requires Owner to be set, necessitating this patch in the first place
    __instance.gameObject.GetComponents<Component>(_Components);
    foreach (Component c in _Components)
      if (c is IPPPComponent ippp)
        ippp.PPPRespawn();
  }

  private GameObject Spawn(Vector3 position, Quaternion rotation)
  {
    // check if we need to instantiate a brand new projectile
    if (this._despawned.Count == 0)
    {
      GameObject newInstance = UnityEngine.Object.Instantiate(this._prefab, position, rotation);
      PlayerProjectilePoolInfo newPppi = newInstance.AddComponent<PlayerProjectilePoolInfo>();

      // set up initial SRB delegates (copied from Awake())
      Projectile newProj = newPppi.projectile = newInstance.GetComponent<Projectile>();
      newPppi.OnPreTileCollision += newProj.OnPreTileCollision;
      newPppi.OnPreRigidbodyCollision += newProj.OnPreCollision;
      newPppi.OnTileCollision += newProj.OnTileCollision;
      newPppi.OnRigidbodyCollision += newProj.OnRigidbodyCollision;

      // initialize IPPPComponents and additional delegates
      newInstance.GetComponents<Component>(_Components);
      //TODO: currently assuming all IPPPComponents are top level
      foreach (Component c in _Components)
        if (c is IPPPComponent ippp)
          ippp.PPPInit(newPppi);
      this._spawned.AddLast(newPppi.Register(this));
      newPppi.RestoreDelegates();
      // Lazy.DebugLog($"spawned new {this._name}, {this._spawned.Count} total");
      return newInstance;
    }

    LinkedListNode<GameObject> pooledProjNode = this._despawned.Last;
    this._despawned.RemoveLast();
    this._spawned.AddLast(pooledProjNode);

    GameObject pooledProjObj = pooledProjNode.Value;
    pooledProjObj.transform.position = position;
    pooledProjObj.transform.rotation = rotation;
    pooledProjObj.SetActive(true);

    Projectile pooledProj = pooledProjObj.GetComponent<Projectile>();
    // pooledProj.RegenerateCache(); //shouldn't be necessary unless we're ever swapping out particle systems
    pooledProj.Reawaken();
    _AwaitingNewOwner.Add(pooledProj); // need to wait for the Owner to be assigned before finishing the respawning process

    PlayerProjectilePoolInfo pppi = pooledProjObj.GetComponent<PlayerProjectilePoolInfo>();
    pppi.spawned = true;
    pppi.RestoreDelegates();

    return pooledProjObj;
  }

  private void Sanitize(Transform root, PlayerProjectilePoolInfo pppi, ref int savedTransforms)
  {
    // purge unwanted Transforms we didn't spawn with
    int ti = pppi.startingTransforms.IndexOf(root);
    if (ti == -1)
    {
      // Lazy.DebugLog($"  destroying transform {root.gameObject.name}");
      UnityEngine.Object.Destroy(root.gameObject);
      return;
    }

    // get our corresponding prefab transform
    Transform baseT = this._prefabTransforms[ti];

    // purge unwanted Components
    root.gameObject.GetComponents<Component>(_Components);
    foreach (Component c in _Components)
    {
      if (_NoPurgeWhitelist.Contains(c.GetType()))
        continue;
      if (c is IPPPComponent ippp)
      {
        ippp.PPPReset(baseT.gameObject);
        continue;
      }
      // Lazy.DebugLog($"  destroying component {c.GetType()}");
      UnityEngine.Object.Destroy(c); // everything else needs to be nuked
    }

    // count our saved transforms and recurse
    ++savedTransforms;
    int numChildren = root.childCount;
    for (int i = 0; i < numChildren; ++i)
      Sanitize(root.GetChild(i), pppi, ref savedTransforms);
  }

  private void Despawn(GameObject projInstance, bool destroy = false)
  {
    // get PlayerProjectilePoolInfo and sanity check it's actually spawned
    PlayerProjectilePoolInfo pppi = projInstance.GetComponent<PlayerProjectilePoolInfo>();
    pppi.spawned = false;

    // purge unwanted child transforms and components
    int savedTransforms = 0;
    Sanitize(projInstance.transform, pppi, ref savedTransforms);

    // sanity check we have the same transforms we started with. if not, there's big issues
    if (savedTransforms != pppi.startingTransforms.Count)
    {
      Lazy.DebugWarn("at least one starting transform destroyed from projectile, giving up on pooling");
      UnityEngine.Object.Destroy(projInstance);
      return;
    }

    // reset default SRB settings
    SpeculativeRigidbody body = projInstance.GetComponent<SpeculativeRigidbody>();
    SpeculativeRigidbody baseBody = this._prefab.GetComponent<SpeculativeRigidbody>();
    ResetSpeculativeRigidbody(body, baseBody);

    // reset to default projectile settings
    Projectile proj = projInstance.GetComponent<Projectile>();
    Projectile baseProj = this._prefab.GetComponent<Projectile>();
    ResetProjectile(proj, baseProj);
    StaticReferenceManager.RemoveProjectile(proj);
    //TODO: look into Cleanup() method for basegame despawning (specifically ReturnFromBlackBullet() and baseData resetting)

    //reset to default sprite settings
    tk2dBaseSprite sprite = proj.sprite;
    tk2dBaseSprite baseSprite = this._prefab.GetComponentInChildren<tk2dBaseSprite>(); //TOOD: might be more than one valid sprite object
    ResetSprite(sprite, baseSprite);

    // disable the projectile
    projInstance.transform.parent = null;
    projInstance.SetActive(false);
    // GameObject.DontDestroyOnLoad(projInstance); //WARN: can't figure out how to preserve projectiles across floor loads without proxy tree corruption

    // return it to the pool or destroy it as necessary
    this._spawned.Remove(pppi.node);
    if (!destroy)
      this._despawned.AddLast(pppi.node);
    else
      UnityEngine.Object.Destroy(projInstance);
  }

  private static void ResetSprite(tk2dBaseSprite sprite, tk2dBaseSprite baseSprite)
  {
    sprite.OverrideMaterialMode = baseSprite.OverrideMaterialMode;
    sprite.renderer.material.shader = baseSprite.renderer.material.shader;
    sprite.renderer.material.CopyPropertiesFromMaterial(baseSprite.renderer.material);
    sprite.SetSprite(baseSprite.collection, baseSprite.spriteId);
    //TODO: reset animator as necessary
  }

  private static void ResetProjectile(Projectile proj, Projectile baseProj)
  {
    proj.PossibleSourceGun = null; // [NonSerialized]
    proj.SpawnedFromOtherPlayerProjectile = false; // [NonSerialized]
    proj.PlayerProjectileSourceGameTimeslice = -1f; // [NonSerialized]
    proj.m_owner = null; // [NonSerialized]
    proj.BulletScriptSettings.SetAll(baseProj.BulletScriptSettings);
    proj.damageTypes = baseProj.damageTypes;
    proj.allowSelfShooting = baseProj.allowSelfShooting;
    proj.collidesWithPlayer = baseProj.collidesWithPlayer;
    proj.collidesWithProjectiles = baseProj.collidesWithProjectiles;
    proj.collidesOnlyWithPlayerProjectiles = baseProj.collidesOnlyWithPlayerProjectiles;
    proj.projectileHitHealth = baseProj.projectileHitHealth;
    proj.collidesWithEnemies = baseProj.collidesWithEnemies;
    proj.shouldRotate = baseProj.shouldRotate;
    proj.shouldFlipVertically = baseProj.shouldFlipVertically;
    proj.shouldFlipHorizontally = baseProj.shouldFlipHorizontally;
    proj.ignoreDamageCaps = baseProj.ignoreDamageCaps;
    // proj.m_cachedInitialDamage = -1f; // [NonSerialized] // can stay cached

    proj.baseData.damage = baseProj.baseData.damage;
    proj.baseData.speed = baseProj.baseData.speed;
    proj.baseData.range = baseProj.baseData.range;
    proj.baseData.force = baseProj.baseData.force;
    proj.baseData.damping = baseProj.baseData.damping;
    proj.baseData.UsesCustomAccelerationCurve = baseProj.baseData.UsesCustomAccelerationCurve;
    proj.baseData.AccelerationCurve = baseProj.baseData.AccelerationCurve;
    proj.baseData.CustomAccelerationCurveDuration = baseProj.baseData.CustomAccelerationCurveDuration;

    proj.AppliesPoison = baseProj.AppliesPoison;
    proj.PoisonApplyChance = baseProj.PoisonApplyChance;
    // proj.healthEffect = baseProj.healthEffect;
    proj.AppliesSpeedModifier = baseProj.AppliesSpeedModifier;
    proj.SpeedApplyChance = baseProj.SpeedApplyChance;
    // proj.speedEffect = baseProj.speedEffect;
    proj.AppliesCharm = baseProj.AppliesCharm;
    proj.CharmApplyChance = baseProj.CharmApplyChance;
    // proj.charmEffect = baseProj.charmEffect;
    proj.AppliesFreeze = baseProj.AppliesFreeze;
    proj.FreezeApplyChance = baseProj.FreezeApplyChance;
    // proj.freezeEffect = baseProj.freezeEffect;
    proj.AppliesFire = baseProj.AppliesFire;
    proj.FireApplyChance = baseProj.FireApplyChance;
    // proj.fireEffect = baseProj.fireEffect;
    proj.AppliesStun = baseProj.AppliesStun;
    proj.StunApplyChance = baseProj.StunApplyChance;
    proj.AppliedStunDuration = baseProj.AppliedStunDuration;
    proj.AppliesBleed = baseProj.AppliesBleed;
    // proj.bleedEffect = baseProj.bleedEffect;
    proj.AppliesCheese = baseProj.AppliesCheese;
    proj.CheeseApplyChance = baseProj.CheeseApplyChance;
    // proj.cheeseEffect = baseProj.cheeseEffect;
    proj.BleedApplyChance = baseProj.BleedApplyChance;
    proj.CanTransmogrify = baseProj.CanTransmogrify;
    proj.ChanceToTransmogrify = baseProj.ChanceToTransmogrify;

    if (proj.TransmogrifyTargetGuids != null && proj.TransmogrifyTargetGuids.Length > 0) // [EnemyIdentifier]
      Array.Resize(ref proj.TransmogrifyTargetGuids, 0);
    proj.BossDamageMultiplier = 1f; // [NonSerialized]
    proj.SpawnedFromNonChallengeItem = false; // [NonSerialized]
    proj.TreatedAsNonProjectileForChallenge = false; // [NonSerialized]

    // public ProjectileImpactVFXPool hitEffects;
    proj.CenterTilemapHitEffectsByProjectileVelocity = baseProj.CenterTilemapHitEffectsByProjectileVelocity;
    // public VFXPool wallDecals;
    proj.damagesWalls = baseProj.damagesWalls;
    proj.persistTime = baseProj.persistTime;
    proj.angularVelocity = baseProj.angularVelocity;
    proj.angularVelocityVariance = baseProj.angularVelocityVariance;
    proj.spawnEnemyGuidOnDeath = baseProj.spawnEnemyGuidOnDeath; // [EnemyIdentifier]
    proj.HasFixedKnockbackDirection = baseProj.HasFixedKnockbackDirection;
    proj.FixedKnockbackDirection = baseProj.FixedKnockbackDirection;
    proj.pierceMinorBreakables = baseProj.pierceMinorBreakables;
    proj.objectImpactEventName = baseProj.objectImpactEventName;
    proj.enemyImpactEventName = baseProj.enemyImpactEventName;
    proj.onDestroyEventName = baseProj.onDestroyEventName;
    proj.additionalStartEventName = baseProj.additionalStartEventName;

    proj.IsRadialBurstLimited = baseProj.IsRadialBurstLimited;
    proj.MaxRadialBurstLimit = baseProj.MaxRadialBurstLimit;
    // public SynergyBurstLimit[] AdditionalBurstLimits;
    proj.AppliesKnockbackToPlayer = baseProj.AppliesKnockbackToPlayer;
    proj.PlayerKnockbackForce = baseProj.PlayerKnockbackForce;
    proj.HasDefaultTint = baseProj.HasDefaultTint;
    proj.DefaultTintColor = baseProj.DefaultTintColor;
    proj.IsCritical = false; // [NonSerialized]
    proj.BlackPhantomDamageMultiplier = 1f; // [NonSerialized]
    proj.PenetratesInternalWalls = baseProj.PenetratesInternalWalls;
    proj.neverMaskThis = baseProj.neverMaskThis;
    proj.isFakeBullet = baseProj.isFakeBullet;
    proj.CanBecomeBlackBullet = baseProj.CanBecomeBlackBullet;

    // public TrailRenderer TrailRenderer;
    // public CustomTrailRenderer CustomTrailRenderer;
    // public ParticleSystem ParticleTrail;
    proj.DelayedDamageToExploders = baseProj.DelayedDamageToExploders;


    proj.IsBlackBullet = false; // [NonSerialized]
    proj.m_forceBlackBullet = false;
    (proj.statusEffectsToApply ??= new List<GameActorEffect>()).Clear(); // [NonSerialized]

    proj.m_initialized = false; //TODO: double check
    // proj.m_transform = null; // can stay cached
    proj.m_cachedHasBeamController = null;
    proj.AdditionalScaleMultiplier = baseProj.AdditionalScaleMultiplier;
    proj.m_cachedLayer = 0;
    proj.m_currentTintPriority = -1; //TODO: private and not serialized, so maybe should be 0?


    proj.CurseSparks = false; // [NonSerialized]
    proj.m_lastSparksPoint = null;

    proj.OverrideMotionModule = null; // [NonSerialized]
    proj.m_usesNormalMoveRegardless = false; // [NonSerialized]
    proj.m_isInWall = false;
    proj.m_shooter = null;
    proj.m_currentSpeed = 0f;
    proj.m_currentDirection = default;
    // proj.m_renderer = null;  // NOTE: called in Awake()
    proj.m_timeElapsed = 0f;
    proj.m_distanceElapsed = 0f;
    proj.m_lastPosition = default;
    proj.m_hasImpactedObject = false;
    proj.m_hasImpactedEnemy = false;
    proj.m_hasDiedInAir = false;
    proj.m_hasPierced = false;
    proj.m_healthHaverHitCount = 0;

    //NOTE: these are all cached and reset by OnSpawned
    // proj.m_cachedCollidesWithPlayer = false;
    // proj.m_cachedCollidesWithProjectiles = false;
    // proj.m_cachedCollidesWithEnemies = false;
    // proj.m_cachedDamagesWalls = false;
    // proj.m_cachedBaseData = null;
    // proj.m_cachedBulletScriptSettings = null;
    // proj.m_cachedCollideWithTileMap = false;
    // proj.m_cachedCollideWithOthers = false;
    // proj.m_cachedSpriteId = -1; // private, maybe should be 0?

    proj.m_spawnPool = null;
    proj.m_isRamping = false;
    proj.m_rampTimer = 0f;
    proj.m_rampDuration = 0f;
    proj.m_currentRampHeight = 0f;
    proj.m_startRampHeight = 0f;
    proj.m_ignoreTileCollisionsTimer = 0f;
    proj.m_outOfBoundsCounter = 0f;
    proj.m_isExitClippingTiles = false;
    proj.m_exitClippingDistance = 0f;

    proj.OnHitEnemy = null;
    proj.OnWillKillEnemy = null;
    proj.OnBecameDebris = null;
    proj.OnBecameDebrisGrounded = null;
    proj.ModifyVelocity = null;
    proj.PreMoveModifiers = null;
  }

  private static void ResetSpeculativeRigidbody(SpeculativeRigidbody body, SpeculativeRigidbody baseBody)
  {
    body.CollideWithTileMap = baseBody.CollideWithTileMap;
    body.CollideWithOthers = baseBody.CollideWithOthers;
    body.Velocity = baseBody.Velocity;
    body.CapVelocity = baseBody.CapVelocity;
    body.MaxVelocity = baseBody.MaxVelocity;
    body.ForceAlwaysUpdate = baseBody.ForceAlwaysUpdate;
    body.CanPush = baseBody.CanPush;
    body.CanBePushed = baseBody.CanBePushed;
    body.PushSpeedModifier = baseBody.PushSpeedModifier;
    body.CanCarry = baseBody.CanCarry;
    body.CanBeCarried = baseBody.CanBeCarried;

    body.ForceCarriesRigidbodies = false; // [NonSerialized]
    body.PreventPiercing = baseBody.PreventPiercing;
    body.SkipEmptyColliders = baseBody.SkipEmptyColliders;
    body.TK2DSprite = baseBody.TK2DSprite;


    body.RegenerateColliders = false; // [NonSerialized]
    body.RecheckTriggers = baseBody.RecheckTriggers;
    body.UpdateCollidersOnRotation = baseBody.UpdateCollidersOnRotation;
    body.UpdateCollidersOnScale = baseBody.UpdateCollidersOnScale;
    body.AxialScale = baseBody.AxialScale;
    // public DebugSettings DebugParams = new DebugSettings();
    body.IgnorePixelGrid = baseBody.IgnorePixelGrid;
    // public List<PixelCollider> PixelColliders;

    body.SortHash = -1; // [NonSerialized]
    // body.proxyId = -1; // [NonSerialized] //NOTE: set via DeregisterWhenAvailable() below
    // body.PhysicsRegistration = SpeculativeRigidbody.RegistrationState.Deregistered; //NOTE: set via DeregisterWhenAvailable() below

    body.m_cachedIsSimpleProjectile = null;
    body.PathMode = false; // [NonSerialized]
    body.PathTarget = default; // [NonSerialized]
    body.PathSpeed = 0f; // [NonSerialized]

    (body.PreviousPositions ??= new LinkedList<Vector3>()).Clear(); // [NonSerialized]
    body.LastVelocity = default; // [NonSerialized]
    body.LastRotation = 0f; // [NonSerialized]
    body.LastScale = default; // [NonSerialized]
    body.m_position = new Position(0, 0);
    (body.m_specificCollisionExceptions ??= new()).Clear(); // [NonSerialized]
    (body.m_temporaryCollisionExceptions ??= new()).Clear(); // [NonSerialized]
    (body.m_ghostCollisionExceptions ??= new()).Clear(); // [NonSerialized]
    (body.m_pushedRigidbodies ??= new()).Clear(); // [NonSerialized]
    (body.m_carriedRigidbodies ??= new()).Clear(); // [NonSerialized]
    body.m_initialized = false;
    PhysicsEngine.Instance.DeregisterWhenAvailable(body);

    body.OnPreMovement = null;
    body.OnPreRigidbodyCollision = null;
    body.OnPreTileCollision = null;
    body.OnCollision = null;
    body.OnRigidbodyCollision = null;
    body.OnBeamCollision = null;
    body.OnTileCollision = null;

    body.OnEnterTrigger = null;
    body.OnTriggerCollision = null;
    body.OnExitTrigger = null;
    body.OnPathTargetReached = null;
    body.OnPostRigidbodyMovement = null;
    body.MovementRestrictor = null;
    body.OnHitByBeam = null;
    body.ReflectProjectilesNormalGenerator = null;
    body.ReflectBeamsNormalGenerator = null;
  }

  /// <summary>Handle spawning pooled player projectiles</summary>
  [HarmonyPatch(typeof(SpawnManager), nameof(SpawnManager.SpawnProjectile), typeof(GameObject), typeof(Vector3), typeof(Quaternion), typeof(bool))]
  [HarmonyPrefix]
  private static bool SpawnManagerSpawnProjectilePatch(GameObject prefab, Vector3 position, Quaternion rotation, bool ignoresPools, ref GameObject __result)
  {
      // System.Console.WriteLine($"spawning a projectile");
      if (!_Poolers.TryGetValue(prefab, out PlayerProjectilePooler ppp))
      {
        // System.Console.WriteLine($"  not poolable");
        return true; // call the original method
      }
      __result = ppp.Spawn(position, rotation);
      return __result == null; // skip the original method unless our own Spawn() returns null
  }

  /// <summary>Handle cleaning up pooled player projectiles.</summary>
  [HarmonyPatch(typeof(SpawnManager), nameof(SpawnManager.Despawn), typeof(GameObject))]
  [HarmonyPatch(typeof(SpawnManager), nameof(SpawnManager.Despawn), typeof(GameObject), typeof(PathologicalGames.PrefabPool))]
  [HarmonyPrefix]
  private static bool SpawnManagerDespawnPatch(GameObject instance, ref bool __result)
  {
    if (instance.GetComponent<PlayerProjectilePoolInfo>() is not PlayerProjectilePoolInfo pppInfo)
      return true; // call the original method

    pppInfo.pooler.Despawn(instance);
    __result = true; // return true because we were pooled //TODO: double check this later
    return false;
  }

  /// <summary>Clear pools between floors.</summary>
  [HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadNextLevelAsync_CR))]
  [HarmonyPrefix]
  private static void LoadNextLevelAsync_CRPatch(GameManager __instance)
  {
    ClearPoolsForNextFloor();
  }

  /// <summary>Deactivate particles systems when the projectile transform is no longer active</summary>
  [HarmonyPatch(typeof(ParticleKiller), nameof(ParticleKiller.Update))]
  [HarmonyILManipulator]
  private static void ParticleKillerUpdatePatchIL(ILContext il)
  {
      ILCursor cursor = new ILCursor(il);
      if (!cursor.TryGotoNext(MoveType.After,
        instr => instr.MatchLdfld<ParticleKiller>("m_parentTransform"),
        instr => instr.MatchCall<UnityEngine.Object>("op_Implicit")
        ))
        return;

      cursor.Emit(OpCodes.Ldarg_0);
      cursor.CallPrivate(typeof(PlayerProjectilePooler), nameof(IsPKParentActive));
  }

  private static bool IsPKParentActive(bool wasActive, ParticleKiller pk) => wasActive && pk.m_parentTransform.gameObject.activeSelf;
}

internal static class PlayerProjectilePoolerHelpers
{
  /// <summary>Tell the PlayerProjectilePooler that this projectile is poolable.</summary>
  public static Projectile RegisterAsPoolable(this Projectile p)
  {
    PlayerProjectilePooler.RegisterAsPoolable(p);
    return p;
  }
}

internal interface IPPPComponent
{
  // called upon insantitating a pooled player projectile
  public void PPPInit(PlayerProjectilePoolInfo pppi);
  // called upon despawning a pooled player projectile
  public void PPPReset(GameObject prefab);
  // called upon respawning a pooled player projectile
  public void PPPRespawn();
}
