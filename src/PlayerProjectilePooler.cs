namespace CwaffingTheGungy;

/* TODO:
    - fix proxy indexoutofrange exceptions on floor load and reenable pooling across floors
    - figure out how to re-add removed transforms / components
*/

[HarmonyPatch]
internal class PlayerProjectilePooler
{
  internal static readonly Dictionary<GameObject, PlayerProjectilePooler> _Poolers = new();

  private GameObject _prefab;
  private string _name;
  private LinkedList<GameObject> _despawned = new();
  private LinkedList<GameObject> _spawned = new();
  private HashSet<Transform> _startingChildren = new();

  internal static void RegisterAsPoolable(Projectile p)
  {
    GameObject prefab = p.gameObject;
    PlayerProjectilePooler pooler = PlayerProjectilePooler._Poolers[prefab] = new();
    pooler._prefab = prefab;
    pooler._name = prefab.name;
  }

  private static void ClearPoolsForNextFloor()
  {
    Lazy.DebugLog($"cleaning up projectile pools!");
    foreach (PlayerProjectilePooler pooler in _Poolers.Values)
    {
      Lazy.DebugLog($"  {pooler._name} pool had {pooler._spawned.Count} spawned and {pooler._despawned.Count} despawned projectiles");
      pooler._despawned.Clear();
      pooler._spawned.Clear();
    }
  }

  private void RegisterStartingTransforms(Transform t)
  {
      for (int i = t.childCount - 1; i >= 0; --i)
      {
        Transform child = t.GetChild(i);
        RegisterStartingTransforms(child);
        this._startingChildren.Add(child);
      }
  }

  private void PurgeNonStartingTransforms(Transform t)
  {
      for (int i = t.childCount - 1; i >= 0; --i)
      {
        Transform child = t.GetChild(i);
        PurgeNonStartingTransforms(child);
        if (!this._startingChildren.Contains(child))
        {
          System.Console.WriteLine($"removing transform {child.gameObject.name}");
          UnityEngine.Object.Destroy(child.gameObject);
        }
      }
  }

  // [HarmonyPatch(typeof(Projectile), nameof(Projectile.HandleHitEffectsTileMap))]
  // [HarmonyPrefix]
  // private static void DebugPatch(Projectile __instance)
  // {
  //   System.Console.WriteLine($"doing hit effects");
  // }

  private GameObject Spawn(Vector3 position, Quaternion rotation)
  {
    if (this._despawned.Count == 0)
    {
      GameObject newInstance = UnityEngine.Object.Instantiate(this._prefab, position, rotation);
      newInstance.AddComponent<PlayerProjectilePoolInfo>().pooler = this;
      RegisterStartingTransforms(newInstance.transform);
      this._spawned.AddLast(new LinkedListNode<GameObject>(newInstance));
      // System.Console.WriteLine($"spawned {this._spawned.Count} of {this._name}");
      return newInstance;
    }

    // System.Console.WriteLine($"spawning {this._prefab.name} from pool");
    LinkedListNode<GameObject> pooledProjNode = this._despawned.Last;
    this._despawned.RemoveLast();
    GameObject pooledProjObj = pooledProjNode.Value;
    pooledProjNode.Value = null;
    this._spawned.AddLast(pooledProjNode);

    pooledProjObj.transform.position = position;
    pooledProjObj.transform.rotation = rotation;
    pooledProjObj.SetActive(true);

    Projectile pooledProj = pooledProjObj.GetComponent<Projectile>();
    pooledProj.Owner = GameManager.Instance.PrimaryPlayer; //HACK: for testing purposes, make this more robust later with a patch
    pooledProj.RegenerateCache();
    pooledProj.Awake(); //NOTE: needs to happen after RegenerateCache() so sprite is properly set up -> also resets SRB delegates
    pooledProj.Start();
    pooledProj.OnSpawned();
    foreach (Component c in pooledProjObj.GetComponents<Component>())
      if (c is IPPPComponent ippp)
        ippp.PPPRespawn();

    return pooledProjObj;
  }

  private void Despawn(GameObject projInstance)
  {
    // purge unwanted Components
    // projInstance.SetActive(true); // activate so we can actually find the components
    foreach (Component c in projInstance.GetComponents<Component>())
    {
      if (c is Transform)                continue; // don't tamper with this
      if (c is PlayerProjectilePoolInfo) continue; // don't tamper with this
      if (c is Projectile)               continue; // handled later
      if (c is SpeculativeRigidbody)     continue; // handled later
      if (c is IPPPComponent ippp) // custom handler
      {
        ippp.PPPReset(this._prefab);
        continue;
      }
      // everything else needs to be nuked
      System.Console.WriteLine($"  destroying {c.GetType()}");
      UnityEngine.Object.Destroy(c);
    }

    // purge unwanted child objects
    //NOTE: done after IPPPComponent checks to give them a chance to clean these up themselves
    PurgeNonStartingTransforms(projInstance.transform);

    // reset default SRB settings
    SpeculativeRigidbody body = projInstance.GetComponent<SpeculativeRigidbody>();
    SpeculativeRigidbody baseBody = this._prefab.GetComponent<SpeculativeRigidbody>();
    ResetSpeculativeRigidbody(body, baseBody);

    // reset to default projectile settings
    Projectile proj = projInstance.GetComponent<Projectile>();
    Projectile baseProj = this._prefab.GetComponent<Projectile>();
    ResetProjectile(proj, baseProj);

    //reset to default sprite settings
    tk2dBaseSprite sprite = proj.sprite;
    tk2dBaseSprite baseSprite = this._prefab.GetComponentInChildren<tk2dBaseSprite>(); //TOOD: might be more than one valid sprite object
    ResetSprite(sprite, baseSprite);

    // disable the projectile
    projInstance.transform.parent = null;
    projInstance.SetActive(false);
    // GameObject.DontDestroyOnLoad(projInstance); //NOTE: can't figure out how to preserve projectiles across floor loads with proxy tree corruption

    // return it to the pool
    if (this._spawned.Count == 0) //TODO: figure out how this happens
    {
      // System.Console.WriteLine($"how did this happen o.o");
      this._spawned.AddLast(new LinkedListNode<GameObject>(null));
    }
    LinkedListNode<GameObject> pooledProjNode = this._spawned.Last;
    this._spawned.RemoveLast();
    pooledProjNode.Value = projInstance;
    this._despawned.AddLast(pooledProjNode);
  }

  private static void ResetSprite(tk2dBaseSprite sprite, tk2dBaseSprite baseSprite)
  {
    sprite.OverrideMaterialMode = baseSprite.OverrideMaterialMode;
    sprite.renderer.material.shader = baseSprite.renderer.material.shader;
    sprite.renderer.material.CopyPropertiesFromMaterial(baseSprite.renderer.material);
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
    // proj.m_cachedInitialDamage = -1f; // [NonSerialized] // NOTE: called in Awake()

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
    // proj.m_transform = null; // NOTE: called in Awake()
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
    proj.m_cachedCollidesWithPlayer = false;
    proj.m_cachedCollidesWithProjectiles = false;
    proj.m_cachedCollidesWithEnemies = false;
    proj.m_cachedDamagesWalls = false;

    proj.m_cachedBaseData = null;
    proj.m_cachedBulletScriptSettings = null;
    proj.m_cachedCollideWithTileMap = false;
    proj.m_cachedCollideWithOthers = false;

    proj.m_cachedSpriteId = -1; // private, maybe should be 0?
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

    //TODO: having trouble resetting all of these properly
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
  [HarmonyPatch(typeof(SpawnManager), nameof(SpawnManager.Despawn), typeof(GameObject), typeof(PathologicalGames.PrefabPool))]
  [HarmonyPrefix]
  private static bool SpawnManagerDespawnPatch(GameObject instance, PathologicalGames.PrefabPool prefabPool, ref bool __result)
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

internal class PlayerProjectilePoolInfo : MonoBehaviour
{
  public PlayerProjectilePooler pooler = null;
}

internal static class PlayerProjectilePoolerHelpers
{
  public static Projectile RegisterAsPoolable(this Projectile p)
  {
    PlayerProjectilePooler.RegisterAsPoolable(p);
    return p;
  }
}

internal interface IPPPComponent
{
  public void PPPReset(GameObject prefab);
  public void PPPRespawn();
}
