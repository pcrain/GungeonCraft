namespace CwaffingTheGungy;

/// <summary>Extensions for improved projectile handling</summary>
[HarmonyPatch]
public class CwaffProjectile : MonoBehaviour, IPPPComponent
{
    // sane defaults
    public string spawnSound         = null;
    public string chargeSound        = null; //handled in a patch
    public bool stopSoundOnDeath     = false;
    public bool uniqueSounds         = false;
    public GameObject shrapnelVFX    = null;
    public int shrapnelCount         = 10;
    public float shrapnelMinVelocity = 4f;
    public float shrapnelMaxVelocity = 8f;
    public float shrapnelLifetime    = 0.3f;
    public bool preventOrbiting      = false;
    public bool firedForFree         = true;
    public bool becomeDebris         = false;
    public bool preventSparks        = false;
    public float spinRate            = 0f;
    public float lightStrength       = 0f;
    public float lightRange          = 0f;
    public Color lightColor          = Color.white;

    private Projectile _projectile;
    private PlayerController _owner;
    private bool _setupLight;
    private bool _checkedPooled;
    private bool _isPooled;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;

        if (!this._checkedPooled)
        {
          this._isPooled = base.gameObject.GetComponent<PlayerProjectilePoolInfo>();
          this._checkedPooled = true;
        }
        if (!this._isPooled) // only set this up here if PPPInit() is not called on our prefab
          this._projectile.OnDestruction += this.OnProjectileDestroy;

        if (becomeDebris)
          this._projectile.DestroyMode = Projectile.ProjectileDestroyMode.BecomeDebris;

        #region Sound Handling
          if (!string.IsNullOrEmpty(spawnSound))
          {
            if (uniqueSounds)
              base.gameObject.Play($"{spawnSound}_stop_all");
            base.gameObject.Play(spawnSound);
          }
        #endregion

        #region Light Handling
        if (!this._setupLight && this.lightStrength > 0)
        {
          Light light = new GameObject().AddComponent<Light>();
          light.color = this.lightColor;
          light.intensity = this.lightStrength;
          light.range = this.lightRange;
          light.type = LightType.Point;
          light.bounceIntensity = 1f;
          light.renderMode = LightRenderMode.Auto;
          light.shadows = LightShadows.None;
          light.gameObject.transform.parent = base.transform;
          light.gameObject.transform.localPosition = new Vector3(0, 0, -0.8f);
          light.gameObject.AddComponent<ObjectHeightController>().heightOffGround = -0.8f;
          this._setupLight = true;
        }
        #endregion
    }

    public void PPPInit(PlayerProjectilePoolInfo pppi)
    {
        pppi.OnDestruction += this.OnProjectileDestroy;
    }

    public void PPPRespawn()
    {
        Start();
    }

    public void PPPReset(GameObject prefab)
    {
      CwaffProjectile baseCp = prefab.GetComponent<CwaffProjectile>();
      this.spawnSound = baseCp.spawnSound;
      this.chargeSound = baseCp.chargeSound;
      this.stopSoundOnDeath = baseCp.stopSoundOnDeath;
      this.uniqueSounds = baseCp.uniqueSounds;
      this.shrapnelVFX = baseCp.shrapnelVFX;
      this.shrapnelCount = baseCp.shrapnelCount;
      this.shrapnelMinVelocity = baseCp.shrapnelMinVelocity;
      this.shrapnelMaxVelocity = baseCp.shrapnelMaxVelocity;
      this.shrapnelLifetime = baseCp.shrapnelLifetime;
      this.preventOrbiting = baseCp.preventOrbiting;
      this.firedForFree = baseCp.firedForFree;
      this.becomeDebris = baseCp.becomeDebris;
      this.preventSparks = baseCp.preventSparks;
      this.spinRate = baseCp.spinRate;
      this.lightStrength = baseCp.lightStrength;
      this.lightRange = baseCp.lightRange;
      this.lightColor = baseCp.lightColor;

      this._projectile = null;
      this._owner = null;
      //NOTE: not resetting this._setupLight since we don't want to create it twice
    }

    private void Update()
    {
      if (this.spinRate != 0f && this._projectile.m_transform)
        this._projectile.m_transform.rotation *= Quaternion.Euler(0f, 0f, BraveTime.DeltaTime * this.spinRate);
    }

    private void OnProjectileDestroy(Projectile p)
    {
      #region Shrapnel Handling
        if (shrapnelVFX)
        {
          p.SpawnShrapnel(
              shrapnelVFX         : shrapnelVFX,
              shrapnelCount       : shrapnelCount,
              shrapnelMinVelocity : shrapnelMinVelocity,
              shrapnelMaxVelocity : shrapnelMaxVelocity,
              shrapnelLifetime    : shrapnelLifetime);
        }
      #endregion

      #region Sound Handling
        if (stopSoundOnDeath && !string.IsNullOrEmpty(spawnSound))
          base.gameObject.Play($"{spawnSound}_stop");
      #endregion

      if (!this._isPooled)
        UnityEngine.Object.Destroy(this);  // clean up after ourselves when the projectile is destroyed
    }

    private void OnDestroy()
    {
      // enter destroy code here
    }

    /// <summary>Vanilla code only plays events with the format "Play_WPN_" + enemyImpactEventName + "_impact_01" , so just play the raw events here</summary>
    internal static void PlayCollisionSounds(Projectile p, bool hitEnemy)
    {
      if (!GameManager.AUDIO_ENABLED)
        return;
      if (hitEnemy && !string.IsNullOrEmpty(p.enemyImpactEventName))
        AkSoundEngine.PostEvent(p.enemyImpactEventName, p.gameObject);
      else if (!hitEnemy && !string.IsNullOrEmpty(p.objectImpactEventName))
        AkSoundEngine.PostEvent(p.objectImpactEventName, p.gameObject);
    }

    // NOTE: called by patch in CwaffPatches
    internal static void DetermineIfFiredForFree(Projectile p, Gun gun, ProjectileModule module)
    {
      if (p.gameObject.GetComponent<CwaffProjectile>() is not CwaffProjectile cp)
        return;
      if (gun.InfiniteAmmo)
      {
        cp.firedForFree = true;
        return;
      }

      if (gun.modifiedFinalVolley != null && module == gun.modifiedFinalVolley.projectiles[0])
        module = gun.DefaultModule;
      int cost = module.ammoCost;
      if (module.shootStyle == ProjectileModule.ShootStyle.Charged)
      {
        ProjectileModule.ChargeProjectile chargeProjectile = module.GetChargeProjectile(gun.m_moduleData[module].chargeTime);
        if (chargeProjectile.UsesAmmo)
          cost = chargeProjectile.AmmoCost;
      }
      cp.firedForFree = (cost == 0);
    }

    /// <summary>Prevent projectiles from being affected by Orbital Bullets if preventOrbiting is set</summary>
    [HarmonyPatch(typeof(GunVolleyModificationItem), nameof(GunVolleyModificationItem.HandleStartOrbit))]
    [HarmonyPrefix]
    private static bool PreventOrbitingProjectilePatch(GunVolleyModificationItem __instance, BounceProjModifier bouncer, SpeculativeRigidbody srb)
    {
        if (bouncer.projectile.GetComponent<CwaffProjectile>() is not CwaffProjectile c)
          return true; // call the original method
        return !c.preventOrbiting; // skip the original method iff we are supposed to prevent orbiting
    }

    /// <summary>Prevent beams from being affected by Orbital Bullets if preventOrbiting is set</summary>
    [HarmonyPatch(typeof(GunVolleyModificationItem), nameof(GunVolleyModificationItem.PostProcessProjectileOrbitBeam))]
    [HarmonyPrefix]
    private static bool PreventOrbitingBeamPatch(GunVolleyModificationItem __instance, BeamController beam)
    {
        if (beam.projectile.GetComponent<CwaffProjectile>() is not CwaffProjectile c)
          return true; // call the original method
        return !c.preventOrbiting; // skip the original method iff we are supposed to prevent orbiting
    }

    /// <summary>Prevent electric damage bullets from emitting sparks when they're electric / cursed</summary>
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.HandleSparks))]
    [HarmonyPrefix]
    private static bool ProjectileHandleSparksPatch(Projectile __instance, Vector2? overridePoint)
    {
      if (__instance.GetComponent<CwaffProjectile>() is CwaffProjectile cp)
        return !cp.preventSparks; // skip original method if preventSparks is true
      if (__instance.GetComponent<FakeProjectileComponent>())
        return false; // skip original method if we're a fake projectile
      return true;     // call the original method
    }

    /// <summary>Play charge sounds for individual charge projectiles</summary>
    [HarmonyPatch(typeof(Gun), nameof(Gun.HandleChargeEffects))]
    [HarmonyPostfix]
    private static void GunHandleChargeEffectsPatch(Gun __instance, ProjectileModule.ChargeProjectile oldChargeProjectile, ProjectileModule.ChargeProjectile newChargeProjectile)
    {
      if (newChargeProjectile == null || newChargeProjectile.Projectile is not Projectile proj || proj.gameObject.GetComponent<CwaffProjectile>() is not CwaffProjectile cp)
        return;
      if (!string.IsNullOrEmpty(cp.chargeSound))
        __instance.gameObject.Play(cp.chargeSound);
    }
}

/// <summary>Special class for projectiles that do something weird when spawned in and die immediately.</summary>
[HarmonyPatch]
public class WeirdProjectile : Projectile
{
  public enum FiredBy
  {
    FREEFIRE,      // free fired by, e.g., Ring of Triggers, Chance Bullets, etc.
    PRIMARYGUN,    // fired from a human player's primary gun
    SECONDARYGUN,  // fired from a human player's secondary (dual wield) gun
    ENEMYGUN,      // fired from an AIActor's gun
  }

  internal static Stack<Gun>              _CurrentProjectileGun = new(); //NOTE: used by a patch in CwaffPatches
  internal static Stack<ProjectileModule> _CurrentProjectileMod = new(); //NOTE: used by a patch in CwaffPatches

  public Gun sourceGun              { get; private set; } // gun that spawned the projectile
  public ProjectileModule sourceMod { get; private set; } // projectile module that spawned the projectile
  public FiredBy firedBy            { get; private set; } // means by which the projectile was spawned

  private void OnEnable()
  {
    DetermineFiringSource();
    this.damageTypes         = CoreDamageTypes.None;
    this.collidesWithEnemies = false;
    this.collidesWithPlayer  = false;
    if (base.gameObject.GetComponent<tk2dBaseSprite>() is tk2dBaseSprite sprite)
      UnityEngine.Object.Destroy(sprite);
    base.gameObject.AddComponent<FakeProjectileComponent>();
  }

  public override sealed void Move()
  {
    if (this.firedBy switch
    {
      FiredBy.FREEFIRE     => OnFreeFired(),
      FiredBy.PRIMARYGUN   => OnFiredByPlayer(primaryGun: true),
      FiredBy.SECONDARYGUN => OnFiredByPlayer(primaryGun: false),
      FiredBy.ENEMYGUN     => OnFiredByEnemy(),
      _                    => true,
    })
      OnFiredByAnything();
    DieInAir(suppressInAirEffects: true, allowActorSpawns: false, allowProjectileSpawns: false, killedEarly: false);
  }

  /// <summary>Called when a projectile is fired from a non-gun source.</summary>
  protected virtual bool OnFreeFired()                    => true;

  /// <summary>Called when a projectile is fired from a player's gun.</summary>
  protected virtual bool OnFiredByPlayer(bool primaryGun) => true;

  /// <summary>Called when a projectile is fired from an enemy's gun.</summary>
  protected virtual bool OnFiredByEnemy()                 => true;

  /// <summary>Called when a projectile is fired by any source, provided the source-specific method returned true.</summary>
  protected virtual void OnFiredByAnything()
  {

  }

  /// <summary>Effectively transforms the Gun into a different projectile entirely</summary>
  protected Projectile SpawnDifferentProjectile(Projectile proj)
  {
    GameObject newProjObject = SpawnManager.SpawnProjectile(proj.gameObject, base.transform.position, base.transform.rotation);
    Projectile newProj = newProjObject.GetComponent<Projectile>();
    newProj.Owner = this.Owner;
    newProj.Shooter = this.Shooter;
    if (this.Owner is PlayerController player)
      player.DoPostProcessProjectile(newProj);
    return newProj;
  }

  /// <summary>Effectively transforms the Gun into a different projectile entirely</summary>
  protected Projectile SpawnDifferentProjectile(GameObject proj)
  {
    GameObject newProjObject = SpawnManager.SpawnProjectile(proj, base.transform.position, base.transform.rotation);
    Projectile newProj = newProjObject.GetComponent<Projectile>();
    newProj.Owner = this.Owner;
    newProj.Shooter = this.Shooter;
    if (this.Owner is PlayerController player)
      player.DoPostProcessProjectile(newProj);
    return newProj;
  }


  private void DetermineFiringSource()
  {
    if (_CurrentProjectileGun.Count == 0 || _CurrentProjectileGun.Peek() is not Gun gun || gun.m_owner is not GameActor actor)
    {
      this.sourceGun = null;
      this.sourceMod = null;
      this.firedBy = FiredBy.FREEFIRE;
      return;
    }

    this.sourceGun = gun;
    this.sourceMod = _CurrentProjectileMod.Peek();
    if (actor is AIActor enemy)
      this.firedBy = FiredBy.ENEMYGUN;
    else if (actor is not PlayerController player)
      this.firedBy = FiredBy.FREEFIRE;
    else if (gun == player.CurrentGun)
      this.firedBy = FiredBy.PRIMARYGUN;
    else if (gun == player.CurrentSecondaryGun)
      this.firedBy = FiredBy.SECONDARYGUN;
    else
      this.firedBy = FiredBy.FREEFIRE;
  }
}
