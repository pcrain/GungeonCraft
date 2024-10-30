namespace CwaffingTheGungy;

/// <summary>Singleton class containing reusable data for GunBuilder</summary>
/// <remarks>NOTE: operates under the assumption that you are only ever using one GunData at a time.</remarks>
public sealed class GunData
{
  private static readonly GunData _Instance = new();

  static GunData() {} // explicit static constructor to prevent premature initialization
  private GunData() {} // private default constructor to prevent explicit instantiation
  public static GunData Default { get { return GunData.New(); } } // get default settings for GunBuildData

  /// <summary>Special flags used for simplifying setup</summary>
  public const int MATCH_ANIM = -2;

  public Gun gun;
  public Projectile baseProjectile;
  public int? clipSize;
  public float? cooldown;
  public float? angleVariance;
  public ShootStyle shootStyle;
  public ProjectileSequenceStyle sequenceStyle;
  public float chargeTime;
  public int ammoCost;
  public GameUIAmmoType.AmmoType? ammoType;
  public bool customClip;
  public float? damage;
  public float? speed;
  public float? force;
  public float? range;
  public float? recoil;
  public float poison;
  public float fire;
  public float freeze;
  public float slow;
  public bool? collidesWithEnemies;
  public bool? ignoreDamageCaps;
  public bool? collidesWithProjectiles;
  public bool? surviveRigidbodyCollisions;
  public bool? collidesWithTilemap;
  public string sprite;
  public int fps;
  public Anchor anchor;
  public float scale;
  public bool anchorsChangeColliders;
  public bool fixesScales;
  public IntVector2? overrideColliderPixelSizes;
  public IntVector2? overrideColliderOffsets;
  public float bossDamageMult;
  public float jammedDamageMult;
  public string destroySound;
  public bool? shouldRotate;
  public int barrageSize;
  public bool? shouldFlipHorizontally;
  public bool? shouldFlipVertically;
  public bool  useDummyChargeModule;
  public bool  invisibleProjectile;
  public float angleFromAim;
  public bool ignoredForReloadPurposes;
  public bool mirror;
  public string spawnSound;
  public bool? stopSoundOnDeath;
  public bool? uniqueSounds;
  public GameObject shrapnelVFX;
  public int? shrapnelCount;
  public float? shrapnelMinVelocity;
  public float? shrapnelMaxVelocity;
  public float? shrapnelLifetime;
  public bool? preventOrbiting;
  public string hitSound;
  public string hitEnemySound;
  public string hitWallSound;
  public bool? becomeDebris;
  public bool? electric;
  public float? burstCooldown;
  public bool? preventSparks;
  public bool? pierceBreakables;
  public bool? collidesOnlyWithPlayerProjectiles;
  public bool? pierceInternalWalls;

  public bool? doBeamSetup;
  public string beamSprite;
  public int beamFps;
  public int beamStartFps;
  public int beamEndFps;
  public int beamChargeFps;
  public int beamImpactFps;
  public bool beamLoopCharge;
  public float beamEmission;
  public int beamReflections;
  public float beamChargeDelay;
  public float beamStatusDelay;
  public GoopDefinition beamGoop;
  public bool? beamInterpolate;
  public int beamPiercing;
  public bool? beamPiercesCover;
  public bool? beamContinueToWall;
  public bool? beamIsRigid;
  public float beamKnockback;
  public BasicBeamController.BeamTileType? beamTiling;
  public BasicBeamController.BeamEndType? beamEndType;
  public bool? beamSeparation;
  public bool beamStartIsMuzzle;
  public bool hideAmmo;
  public float spinupTime;
  public string spinupSound;

  /// <summary>Pseudo-constructor holding most setup information required for a single projectile gun.</summary>
  /// <param name="gun">The gun we're attaching to (can be null, only used for custom clip sprite name resolution for now).</param>
  /// <param name="baseProjectile">The projectile we're using as a base (if null, reuses the first projectile of the gun's default module).</param>
  /// <param name="clipSize">The number of shots the gun can fired before reloading.</param>
  /// <param name="cooldown">The minimum number of seconds between shots</param>
  /// <param name="angleVariance">Maximum deviation from shooting angle (in degrees) a bullet may actually be fired.</param>
  /// <param name="shootStyle">How bullets are actually fired from the gun.</param>
  /// <param name="sequenceStyle">In what order bullets are actually fired from the gun.</param>
  /// <param name="chargeTime">If shootStyle is Charged, how long the projectile must charge for.</param>
  /// <param name="ammoCost">How much ammo is depleted per shot fired from a module.</param>
  /// <param name="ammoType">If using base game ammo clips, the type of ammo clip to use.</param>
  /// <param name="customClip">Whether to use a custom ammo clip</param>
  /// <param name="damage">The damage of the projectile.</param>
  /// <param name="speed">The speed of the projectile.</param>
  /// <param name="force">The force of the projectile.</param>
  /// <param name="range">The range of the projectile.</param>
  /// <param name="recoil">The recoil force of the projectile on the owner.</param>
  /// <param name="poison">The chance for the projectile to apply poison.</param>
  /// <param name="fire">The chance for the projectile to apply fire.</param>
  /// <param name="freeze">The chance for the projectile to apply freeze.</param>
  /// <param name="slow">The chance for the projectile to apply slow.</param>
  /// <param name="collidesWithEnemies">If false, projectile won't collide with enemies.</param>
  /// <param name="ignoreDamageCaps">If true, ignores DPS caps on bosses.</param>
  /// <param name="collidesWithProjectiles">If true, projectile will collide with other projectiles.</param>
  /// <param name="surviveRigidbodyCollisions">If true, projectile will not die upon colliding with an enemy or other rigid body.</param>
  /// <param name="collidesWithTilemap">If true, projectile will not die upon colliding with an enemy or other rigid body.</param>
  /// <param name="sprite">The base name of the sprite to use for the projectile.</param>
  /// <param name="fps">The number of frames per second for the projectile's default animation.</param>
  /// <param name="anchor">Where the projectile's effective center of mass is for rotation and muzzle offset purposes.</param>
  /// <param name="scale">The scale of the sprite in-game relative to its pixel size on disk.</param>
  /// <param name="anchorsChangeColliders">If true, colliders are adjusted to account for the sprite's anchor.</param>
  /// <param name="fixesScales">If true, colliders are adjusted to account for scaling.</param>
  /// <param name="overrideColliderPixelSizes">If set, manually adjusts the size of the projectile's hitbox.</param>
  /// <param name="overrideColliderOffsets">If set, manually adjusts the offset of the projectile's hitbox.</param>
  /// <param name="bossDamageMult">Multiplier for damage done to bosses.</param>
  /// <param name="jammedDamageMult">Multiplier for damage done to jammed enemies.</param>
  /// <param name="destroySound">Sound event for when projectile is destroyed.</param>
  /// <param name="shouldRotate">Whether the projectile should rotate based on its velocity.</param>
  /// <param name="barrageSize">The number of projectiles to fire simultaenously.</param>
  /// <param name="shouldFlipHorizontally">Whether the projectile sprite should flip horizontally.</param>
  /// <param name="shouldFlipVertically">Whether the projectile sprite should flip vertically.</param>
  /// <param name="useDummyChargeModule">Cannot be the only projectile for a gun / not compatible with modules are tiers.</param>
  /// <param name="invisibleProjectile">If true, the projectile is not rendered.</param>
  /// <param name="spawnSound">The sound to play when the projectile is created.</param>
  /// <param name="stopSoundOnDeath">If true, spawnSound will be stopped when the projectile is destroyed.</param>
  /// <param name="uniqueSounds">If true, spawnSound will be stopped if another projectile is spawned.</param>
  /// <param name="shrapnelVFX">The VFX object used for the projectile's shrapnel.</param>
  /// <param name="shrapnelCount">The number of pieces of shrapnel to emit when the projectile is destroyed.</param>
  /// <param name="shrapnelMinVelocity">Minimum magnitude of launched shrapnel velocity.</param>
  /// <param name="shrapnelMaxVelocity">Maximum magnitude of launched shrapnel velocity.</param>
  /// <param name="shrapnelLifetime">The amount of time shrapnel lasts before fading out.</param>
  /// <param name="preventOrbiting">If true, the projectile will not be affected by Orbital Bullets or similar effects.</param>
  /// <param name="hitSound">Fallback sound to play on collision if hitEnemySound and/or hitWallSound is not specified.</param>
  /// <param name="hitEnemySound">The sound to play when the projectile collids with an enemy.</param>
  /// <param name="hitWallSound">The sound to play when the projectile collids with a wall.</param>
  /// <param name="becomeDebris">If true, the projectile will drop to the ground as a DebrisObject when destroyed.</param>
  /// <param name="angleFromAim">The angle from the player's computed aim direction at which the projectile will launch.</param>
  /// <param name="ignoredForReloadPurposes">If true, prevents reload notifications when this projectile is created (useful for synergy bonus projectiles).</param>
  /// <param name="mirror">If true, the current projectile module will spawn another identical projectile with an opposite angleFromAim.</param>
  /// <param name="electric">If true, adds the Electric damage type to the projectile.</param>
  /// <param name="burstCooldown">The minimum number of seconds between shots in a burst.</param>
  /// <param name="preventSparks">If true, prevents sparks from being created for electric / cursed projectiles.</param>
  /// <param name="pierceBreakables">If true, the projectile will pierce minor breakables such as crates, barrels, etc.</param>
  /// <param name="collidesOnlyWithPlayerProjectiles">If true and if collidesWithProjectiles is true, the projectile will only collide with player projectiles.</param>
  /// <param name="pierceInternalWalls">If true, the projectile will pierce internal walls within rooms.</param>
  /// <param name="doBeamSetup'">If non-null, forcibly enables / disables additional beam setup regardless of whether projectile is part of a ShootStyle.Beam module.</param>
  /// <param name="beamSprite">The base sprite name to use for beam start, end, impact, etc. sprites.</param>
  /// <param name="beamFps">The default framerate for beam sprite animators.</param>
  /// <param name="beamStartFps">The framerate for the beam's start animation.</param>
  /// <param name="beamEndFps">The framerate for the beam's end animation.</param>
  /// <param name="beamChargeFps">The framerate for the beam's charge animation.</param>
  /// <param name="beamImpactFps">The framerate for the beam's impact animation.</param>
  /// <param name="beamLoopCharge">Whether the beam's charge amimation should be looped.</param>
  /// <param name="beamEmission">The emissive power of the beam sprites.</param>
  /// <param name="beamReflections">The number of times the beam projectile should reflect.</param>
  /// <param name="beamChargeDelay">How long the beam charges before firing.</param>
  /// <param name="beamStatusDelay">How long the beam must contact an enemy before applying any status effects.</param>
  /// <param name="beamGoop">Which goop, if any, the beam should spawn.</param>
  /// <param name="beamInterpolate">If true, beam bones interpolate sprites (can fix / cause visual jankiness depending on the situation).</param>
  /// <param name="beamPiercing">How much the beam can pierce.</param>
  /// <param name="beamPiercesCover">If true, the beam instantly breaks tables and flippables (I think).</param>
  /// <param name="beamContinueToWall">If false, the beam sprite will not continue all the way to the wall with partial tiling.</param>
  /// <param name="beamIsRigid">If true, the beam will always be completely straight. If false, beam segments can curve and bend.</param>
  /// <param name="beamKnockback">The knockback strength of the beam.</param>
  /// <param name="beamTiling">Determines how segments of the beam grow / attach to each other.</param>
  /// <param name="beamEndType">Determines how the end of the beam dissipates.</param>
  /// <param name="beamSeparation">If true, the beam can separate from its linked nodes upon collision.</param>
  /// <param name="beamStartIsMuzzle">If true, uses the beam's start animation as a muzzle animation instead (fixes some graphical glitches).</param>
  /// <param name="hideAmmo">If true, no ammo is displayed for the module (uses Blasphemy's ammo).</param>
  /// <param name="spinupTime">The amount of time a (semi-)automatic gun must spin up before firing.</param>
  /// <param name="spinupSound">The sound to play while the gun is spinning up.</param>
  public static GunData New(Gun gun = null, Projectile baseProjectile = null, int? clipSize = null, float? cooldown = null, float? angleVariance = null,
    ShootStyle shootStyle = ShootStyle.Automatic, ProjectileSequenceStyle sequenceStyle = ProjectileSequenceStyle.Random, float chargeTime = 0.0f, int ammoCost = 1,
    GameUIAmmoType.AmmoType? ammoType = null, bool customClip = false, float? damage = null, float? speed = null, float? force = null, float? range = null, float? recoil = null,
    float poison = 0.0f, float fire = 0.0f, float freeze = 0.0f, float slow = 0.0f, bool? collidesWithEnemies = null, bool? ignoreDamageCaps = null,
    bool? collidesWithProjectiles = null, bool? surviveRigidbodyCollisions = null, bool? collidesWithTilemap = null, string sprite = null, int fps = 2,
    Anchor anchor = Anchor.MiddleCenter, float scale = 1.0f, bool anchorsChangeColliders = true, bool fixesScales = true, IntVector2? overrideColliderPixelSizes = null,
    IntVector2? overrideColliderOffsets = null, float bossDamageMult = 1.0f, float jammedDamageMult = 1.0f, string destroySound = null, bool? shouldRotate = null, int barrageSize = 1,
    bool? shouldFlipHorizontally = null, bool? shouldFlipVertically = null, bool useDummyChargeModule = false, bool invisibleProjectile = false, string spawnSound = null,
    bool? stopSoundOnDeath = null, bool? uniqueSounds = null, GameObject shrapnelVFX = null, int? shrapnelCount = null, float? shrapnelMinVelocity = null,
    float? shrapnelMaxVelocity = null, float? shrapnelLifetime = null, bool? preventOrbiting = null, string hitSound = null, string hitEnemySound = null, string hitWallSound = null,
    bool? becomeDebris = null, float angleFromAim = 0.0f, bool ignoredForReloadPurposes = false, bool mirror = false, bool? electric = null, float? burstCooldown = null,
    bool? preventSparks = null, bool? pierceBreakables = null, bool? collidesOnlyWithPlayerProjectiles = null, bool? pierceInternalWalls = null, bool? doBeamSetup = null,
    string beamSprite = null, int beamFps = -1, int beamStartFps = -1, int beamEndFps = -1, int beamChargeFps = -1, int beamImpactFps = -1, bool beamLoopCharge = true,
    float beamEmission = -1f, int beamReflections = -1, float beamChargeDelay = -1f, float beamStatusDelay = -1f, GoopDefinition beamGoop = null, bool? beamInterpolate = null,
    int beamPiercing = -1, bool? beamPiercesCover = null, bool? beamContinueToWall = null, bool? beamIsRigid = null, float beamKnockback = -1f,
    BasicBeamController.BeamTileType? beamTiling = null, BasicBeamController.BeamEndType? beamEndType = null, bool? beamSeparation = null, bool beamStartIsMuzzle = false,
    bool hideAmmo = false, float spinupTime = 0.0f, string spinupSound = null)
  {
      _Instance.gun                               = gun; // set by InitSpecialProjectile()
      _Instance.baseProjectile                    = baseProjectile;
      _Instance.clipSize                          = clipSize;
      _Instance.cooldown                          = cooldown;
      _Instance.angleVariance                     = angleVariance;
      _Instance.shootStyle                        = shootStyle;
      _Instance.sequenceStyle                     = sequenceStyle;
      _Instance.chargeTime                        = chargeTime;
      _Instance.ammoCost                          = ammoCost;
      _Instance.ammoType                          = ammoType;
      _Instance.customClip                        = customClip;
      _Instance.damage                            = damage;
      _Instance.speed                             = speed;
      _Instance.force                             = force;
      _Instance.range                             = range;
      _Instance.recoil                            = recoil;
      _Instance.poison                            = poison;
      _Instance.fire                              = fire;
      _Instance.freeze                            = freeze;
      _Instance.slow                              = slow;
      _Instance.collidesWithEnemies               = collidesWithEnemies;
      _Instance.ignoreDamageCaps                  = ignoreDamageCaps;
      _Instance.collidesWithProjectiles           = collidesWithProjectiles;
      _Instance.surviveRigidbodyCollisions        = surviveRigidbodyCollisions;
      _Instance.collidesWithTilemap               = collidesWithTilemap;
      _Instance.sprite                            = sprite;
      _Instance.fps                               = fps;
      _Instance.anchor                            = anchor;
      _Instance.scale                             = scale;
      _Instance.anchorsChangeColliders            = anchorsChangeColliders;
      _Instance.fixesScales                       = fixesScales;
      _Instance.overrideColliderPixelSizes        = overrideColliderPixelSizes;
      _Instance.overrideColliderOffsets           = overrideColliderOffsets;
      _Instance.bossDamageMult                    = bossDamageMult;
      _Instance.jammedDamageMult                  = jammedDamageMult;
      _Instance.destroySound                      = destroySound;
      _Instance.shouldRotate                      = shouldRotate;
      _Instance.barrageSize                       = barrageSize;
      _Instance.shouldFlipHorizontally            = shouldFlipHorizontally;
      _Instance.shouldFlipVertically              = shouldFlipVertically;
      _Instance.useDummyChargeModule              = useDummyChargeModule;
      _Instance.invisibleProjectile               = invisibleProjectile;
      _Instance.spawnSound                        = spawnSound;
      _Instance.stopSoundOnDeath                  = stopSoundOnDeath;
      _Instance.uniqueSounds                      = uniqueSounds;
      _Instance.shrapnelVFX                       = shrapnelVFX;
      _Instance.shrapnelCount                     = shrapnelCount;
      _Instance.shrapnelMinVelocity               = shrapnelMinVelocity;
      _Instance.shrapnelMaxVelocity               = shrapnelMaxVelocity;
      _Instance.shrapnelLifetime                  = shrapnelLifetime;
      _Instance.preventOrbiting                   = preventOrbiting;
      _Instance.hitSound                          = hitSound;
      _Instance.hitEnemySound                     = hitEnemySound;
      _Instance.hitWallSound                      = hitWallSound;
      _Instance.becomeDebris                      = becomeDebris;
      _Instance.angleFromAim                      = angleFromAim;
      _Instance.mirror                            = mirror;
      _Instance.electric                          = electric;
      _Instance.burstCooldown                     = burstCooldown;
      _Instance.preventSparks                     = preventSparks;
      _Instance.pierceBreakables                  = pierceBreakables;
      _Instance.collidesWithProjectiles           = collidesWithProjectiles;
      _Instance.collidesOnlyWithPlayerProjectiles = collidesOnlyWithPlayerProjectiles;
      _Instance.pierceInternalWalls               = pierceInternalWalls;
      _Instance.doBeamSetup                       = doBeamSetup;
      _Instance.beamSprite                        = beamSprite;
      _Instance.beamFps                           = beamFps;
      _Instance.beamStartFps                      = beamStartFps;
      _Instance.beamEndFps                        = beamEndFps;
      _Instance.beamChargeFps                     = beamChargeFps;
      _Instance.beamImpactFps                     = beamImpactFps;
      _Instance.beamLoopCharge                    = beamLoopCharge;
      _Instance.beamEmission                      = beamEmission;
      _Instance.beamReflections                   = beamReflections;
      _Instance.beamChargeDelay                   = beamChargeDelay;
      _Instance.beamStatusDelay                   = beamStatusDelay;
      _Instance.beamGoop                          = beamGoop;
      _Instance.beamInterpolate                   = beamInterpolate;
      _Instance.beamPiercing                      = beamPiercing;
      _Instance.beamPiercesCover                  = beamPiercesCover;
      _Instance.beamContinueToWall                = beamContinueToWall;
      _Instance.beamIsRigid                       = beamIsRigid;
      _Instance.beamKnockback                     = beamKnockback;
      _Instance.beamTiling                        = beamTiling;
      _Instance.beamEndType                       = beamEndType;
      _Instance.beamSeparation                    = beamSeparation;
      _Instance.beamStartIsMuzzle                 = beamStartIsMuzzle;
      _Instance.hideAmmo                          = hideAmmo;
      _Instance.spinupTime                        = spinupTime;
      _Instance.spinupSound                       = spinupSound;
      return _Instance;
  }
}

public static class GunBuilder
{
  /// <summary>General-purpose starting point for setting up most guns -- sets up default module, default projectile, and default animation for projectile</summary>
  public static ProjectileType InitSpecialProjectile<ProjectileType>(this Gun gun, GunData b = null)
    where ProjectileType : Projectile
  {
    // If we haven't passed any GunBuildData in, use sane defaults
    b ??= GunData.Default;
    // Add a reference to our current gun to the gun build data (needed for custom projectile setup)
    b.gun = gun;

    // Set up the gun's default module, default projectile, and default projectile animation
    ProjectileModule mod = gun.SetupDefaultModule(b);
    ProjectileType proj = gun.InitFirstProjectileOfType<ProjectileType>(b);

    // Determine whether we have an invisible projectile
    proj.sprite.renderer.enabled = !b.invisibleProjectile;

    // Need to set up charge projectiles after both module and base projectile have been set up
    if (b.shootStyle == ShootStyle.Charged)
    {
      mod.chargeProjectiles = new();
      if (b.chargeTime >= 0) //WARNING: recently changed this from > to >=, verify nothing breaks
      {
        mod.chargeProjectiles.Add(new ProjectileModule.ChargeProjectile {
          Projectile = proj,
          ChargeTime = b.chargeTime,
        });
      }
    }

    // Need to duplicate volley projectile after all other setup is done, including charge projectiles
    for (int i = 1; i < b.barrageSize; ++i)
      gun.RawSourceVolley.projectiles.Add(ProjectileModule.CreateClone(mod, inheritGuid: false, sourceIndex: i));

    // Add a dummy charge module if we request it (probably mutually exclusive with a ShootStyle.Charged default module)
    if (b.useDummyChargeModule)
        gun.AddDummyChargeModule();

    return proj;
  }

  /// <summary>Generic version of InitSpecialProjectile, assuming we just want a normal projectile</summary>
  public static Projectile InitProjectile(this Gun gun, GunData b = null)
  {
    return gun.InitSpecialProjectile<Projectile>(b);
  }

  /// <summary>Creates a module for standalone use without attaching it to a gun</summary>
  public static ProjectileModule InitSpecialSingleProjectileModule<ProjectileType>(this ProjectileModule mod, GunData b = null)
    where ProjectileType : Projectile
  {
    if (b == null || b.baseProjectile == null)
    {
      Lazy.RuntimeWarn($"Tried to run InitModule() with a valid base projectile, backing out.");
      return mod;
    }

    // Set up the mod's default projectile and default projectile animation
    mod.SetAttributes(b);
    Projectile clone = b.baseProjectile.Clone(b);
    if (clone is not ProjectileType proj)
      proj = clone.ConvertToSpecialtyType<ProjectileType>();
    mod.projectiles = new(){proj};

    // Determine whether we have an invisible projectile
    proj.sprite.renderer.enabled = !b.invisibleProjectile;

    // Need to set up charge projectiles after both module and base projectile have been set up
    if (b.shootStyle == ShootStyle.Charged)
    {
      mod.chargeProjectiles = new();
      if (b.chargeTime >= 0) //WARNING: recently changed this from > to >=, verify nothing breaks
      {
        mod.chargeProjectiles.Add(new ProjectileModule.ChargeProjectile {
          Projectile = proj,
          ChargeTime = b.chargeTime,
        });
      }
    }

    return mod;
  }

  /// <summary>Generic version of InitModule, assuming we just want a normal projectile</summary>
  public static ProjectileModule InitSingleProjectileModule(this ProjectileModule mod, GunData b = null)
  {
    return mod.InitSpecialSingleProjectileModule<Projectile>(b);
  }

  /// <summary>Clone, modify, and return a specific projectile</summary>
  public static ProjectileType CloneSpecial<ProjectileType>(this ProjectileType projectile, GunData b = null)
    where ProjectileType : Projectile
  {
    b ??= GunData.Default;
    ProjectileType p = projectile.ClonePrefab();
    p.AddDefaultAnimation(b);

    // Defaulted
    p.baseData.damage                                 = b.damage                     ?? p.baseData.damage;
    p.baseData.speed                                  = b.speed                      ?? p.baseData.speed;
    p.baseData.force                                  = b.force                      ?? p.baseData.force;
    p.baseData.range                                  = b.range                      ?? p.baseData.range;
    p.shouldRotate                                    = b.shouldRotate               ?? p.shouldRotate;
    p.shouldFlipHorizontally                          = b.shouldFlipHorizontally     ?? p.shouldFlipHorizontally;
    p.shouldFlipVertically                            = b.shouldFlipVertically       ?? p.shouldFlipVertically;
    p.collidesWithEnemies                             = b.collidesWithEnemies        ?? p.collidesWithEnemies;
    p.ignoreDamageCaps                                = b.ignoreDamageCaps           ?? p.ignoreDamageCaps;
    p.collidesWithProjectiles                         = b.collidesWithProjectiles    ?? p.collidesWithProjectiles;
    p.BulletScriptSettings.surviveRigidbodyCollisions = b.surviveRigidbodyCollisions ?? p.BulletScriptSettings.surviveRigidbodyCollisions;
    p.pierceMinorBreakables                           = b.pierceBreakables           ?? p.pierceMinorBreakables;
    p.collidesWithProjectiles                         = b.collidesWithProjectiles    ?? p.collidesWithProjectiles;
    p.collidesOnlyWithPlayerProjectiles               = b.collidesOnlyWithPlayerProjectiles ?? p.collidesOnlyWithPlayerProjectiles;
    p.PenetratesInternalWalls                         = b.pierceInternalWalls ?? p.PenetratesInternalWalls;
    if (p.specRigidbody)
      p.specRigidbody.CollideWithTileMap              = b.collidesWithTilemap ?? p.specRigidbody.CollideWithTileMap;  // doesn't work!
    if (b.recoil.HasValue && b.recoil.Value != 0f)
    {
      p.AppliesKnockbackToPlayer = true;
      p.PlayerKnockbackForce = b.recoil.Value;
    }
    if (b.electric.HasValue)
    {
      if (b.electric.Value)
        p.damageTypes |= CoreDamageTypes.Electric;
      else
        p.damageTypes &= (~CoreDamageTypes.Electric);
    }

    CwaffProjectile c = p.GetOrAddComponent<CwaffProjectile>();
      c.spawnSound          = b.spawnSound          ?? c.spawnSound;
      c.stopSoundOnDeath    = b.stopSoundOnDeath    ?? c.stopSoundOnDeath;
      c.uniqueSounds        = b.uniqueSounds        ?? c.uniqueSounds;
      c.shrapnelVFX         = b.shrapnelVFX         ?? c.shrapnelVFX;
      c.shrapnelCount       = b.shrapnelCount       ?? c.shrapnelCount;
      c.shrapnelMinVelocity = b.shrapnelMinVelocity ?? c.shrapnelMinVelocity;
      c.shrapnelMaxVelocity = b.shrapnelMaxVelocity ?? c.shrapnelMaxVelocity;
      c.shrapnelLifetime    = b.shrapnelLifetime    ?? c.shrapnelLifetime;
      c.preventOrbiting     = b.preventOrbiting     ?? c.preventOrbiting;
      c.becomeDebris        = b.becomeDebris        ?? c.becomeDebris;
      c.preventSparks       = b.preventSparks       ?? c.preventSparks;

    // Non-defaulted
    p.BossDamageMultiplier         = b.bossDamageMult;
    p.BlackPhantomDamageMultiplier = b.jammedDamageMult;
    p.onDestroyEventName           = b.destroySound;
    p.enemyImpactEventName         = b.hitEnemySound ?? b.hitSound;
    p.objectImpactEventName        = b.hitWallSound ?? b.hitSound;

    p.PoisonApplyChance = b.poison;
    p.AppliesPoison     = b.poison > 0.0f;
    if (p.AppliesPoison)
      p.healthEffect = ItemHelper.Get(Items.IrradiatedLead).GetComponent<BulletStatusEffectItem>().HealthModifierEffect;

    p.FireApplyChance = b.fire;
    p.AppliesFire     = b.fire > 0.0f;
    if (p.AppliesFire)
      p.fireEffect = ItemHelper.Get(Items.HotLead).GetComponent<BulletStatusEffectItem>().FireModifierEffect;

    p.FreezeApplyChance = b.freeze;
    p.AppliesFreeze     = b.freeze > 0.0f;
    if (p.AppliesFreeze)
      p.freezeEffect = ItemHelper.Get(Items.FrostBullets).GetComponent<BulletStatusEffectItem>().FreezeModifierEffect;

    p.SpeedApplyChance     = b.slow;
    p.AppliesSpeedModifier = b.slow > 0.0f;
    if (p.AppliesSpeedModifier)
      p.speedEffect = Items.TripleCrossbow.AsGun().DefaultModule.projectiles[0].speedEffect;

    if (b.doBeamSetup ?? (b.shootStyle == ShootStyle.Beam))
      p.InternalSetupBeam(b);

    return p;
  }

  /// <summary>Sets up all of the necessary stuff for beams</summary>
  private static void InternalSetupBeam(this Projectile p, GunData b)
  {
      BasicBeamController beamComp = p.SetupBeamSprites(
          spriteName : b.beamSprite,
          fps        : b.beamFps,
          impactFps  : b.beamImpactFps,
          endFps     : b.beamEndFps,
          startFps   : b.beamStartFps,
          chargeFps  : b.beamChargeFps,
          loopCharge : b.beamLoopCharge);
      if (b.beamChargeDelay >= 0f)
      {
        beamComp.usesChargeDelay = b.beamChargeDelay > 0;
        beamComp.chargeDelay = b.beamChargeDelay;
      }
      if (b.beamEmission > 0f)
      {
        beamComp.sprite.usesOverrideMaterial = true;
        //TODO: verify this change doesn't break things
        // beamComp.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTiltedCutoutEmissive");
        beamComp.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
        beamComp.sprite.renderer.material.SetFloat("_EmissivePower", b.beamEmission);
      }
      if (b.beamReflections >= 0f)
        beamComp.reflections = b.beamReflections;
      if (b.beamStatusDelay >= 0f)
        beamComp.TimeToStatus = b.beamStatusDelay;
      if (b.beamPiercing >= 0f)
        beamComp.penetration = b.beamPiercing;
      if (b.beamKnockback >= 0f)
        beamComp.knockbackStrength = b.beamKnockback;
      if (b.beamIsRigid is bool rigid)
        beamComp.boneType = rigid ? BasicBeamController.BeamBoneType.Straight : BasicBeamController.BeamBoneType.Projectile;
      if (b.beamGoop != null)
        beamComp.GetOrAddComponent<GoopModifier>().goopDefinition = b.beamGoop;

      beamComp.interpolateStretchedBones = b.beamInterpolate    ?? beamComp.interpolateStretchedBones;
      beamComp.ContinueBeamArtToWall     = b.beamContinueToWall ?? beamComp.ContinueBeamArtToWall;
      beamComp.PenetratesCover           = b.beamPiercesCover   ?? beamComp.PenetratesCover;
      beamComp.collisionSeparation       = b.beamSeparation     ?? beamComp.collisionSeparation;
      beamComp.TileType                  = b.beamTiling         ?? beamComp.TileType;
      beamComp.endType                   = b.beamEndType        ?? beamComp.endType;

      if (b.beamStartIsMuzzle)
      {
          // fix some animation glitches (don't blindly copy paste; need to be set on a case by case basis depending on your beam's needs)
          beamComp.muzzleAnimation = beamComp.beamStartAnimation;  //use start animation for muzzle animation, make start animation null
          beamComp.beamStartAnimation = null;
      }
  }

  /// <summary>Generic version of Clone, assuming we just want a normal projectile</summary>
  public static Projectile Clone(this Projectile projectile, GunData b = null)
  {
    return projectile.CloneSpecial<Projectile>(b);
  }

  /// <summary>Copy fields from a Projectile to a subclass of that Projectile, then attach it to the base projectile's gameObject and destroy the base projectile</summary>
  public static ProjectileType ConvertToSpecialtyType<ProjectileType>(this Projectile baseProj) where ProjectileType : Projectile
  {
    ProjectileType p = baseProj.gameObject.AddComponent<ProjectileType>();
    p.BulletScriptSettings                        = baseProj.BulletScriptSettings;
    p.damageTypes                                 = baseProj.damageTypes;
    p.allowSelfShooting                           = baseProj.allowSelfShooting;
    p.collidesWithPlayer                          = baseProj.collidesWithPlayer;
    p.collidesWithProjectiles                     = baseProj.collidesWithProjectiles;
    p.collidesOnlyWithPlayerProjectiles           = baseProj.collidesOnlyWithPlayerProjectiles;
    p.projectileHitHealth                         = baseProj.projectileHitHealth;
    p.collidesWithEnemies                         = baseProj.collidesWithEnemies;
    p.shouldRotate                                = baseProj.shouldRotate;
    p.shouldFlipVertically                        = baseProj.shouldFlipVertically;
    p.shouldFlipHorizontally                      = baseProj.shouldFlipHorizontally;
    p.ignoreDamageCaps                            = baseProj.ignoreDamageCaps;
    p.baseData                                    = baseProj.baseData;
    p.AppliesPoison                               = baseProj.AppliesPoison;
    p.PoisonApplyChance                           = baseProj.PoisonApplyChance;
    p.healthEffect                                = baseProj.healthEffect;
    p.AppliesSpeedModifier                        = baseProj.AppliesSpeedModifier;
    p.SpeedApplyChance                            = baseProj.SpeedApplyChance;
    p.speedEffect                                 = baseProj.speedEffect;
    p.AppliesCharm                                = baseProj.AppliesCharm;
    p.CharmApplyChance                            = baseProj.CharmApplyChance;
    p.charmEffect                                 = baseProj.charmEffect;
    p.AppliesFreeze                               = baseProj.AppliesFreeze;
    p.FreezeApplyChance                           = baseProj.FreezeApplyChance;
    p.freezeEffect                                = baseProj.freezeEffect;
    p.AppliesFire                                 = baseProj.AppliesFire;
    p.FireApplyChance                             = baseProj.FireApplyChance;
    p.fireEffect                                  = baseProj.fireEffect;
    p.AppliesStun                                 = baseProj.AppliesStun;
    p.StunApplyChance                             = baseProj.StunApplyChance;
    p.AppliedStunDuration                         = baseProj.AppliedStunDuration;
    p.AppliesBleed                                = baseProj.AppliesBleed;
    p.bleedEffect                                 = baseProj.bleedEffect;
    p.AppliesCheese                               = baseProj.AppliesCheese;
    p.CheeseApplyChance                           = baseProj.CheeseApplyChance;
    p.cheeseEffect                                = baseProj.cheeseEffect;
    p.BleedApplyChance                            = baseProj.BleedApplyChance;
    p.CanTransmogrify                             = baseProj.CanTransmogrify;
    p.ChanceToTransmogrify                        = baseProj.ChanceToTransmogrify;
    p.TransmogrifyTargetGuids                     = baseProj.TransmogrifyTargetGuids;
    p.hitEffects                                  = baseProj.hitEffects;
    p.CenterTilemapHitEffectsByProjectileVelocity = baseProj.CenterTilemapHitEffectsByProjectileVelocity;
    p.wallDecals                                  = baseProj.wallDecals;
    p.damagesWalls                                = baseProj.damagesWalls;
    p.persistTime                                 = baseProj.persistTime;
    p.angularVelocity                             = baseProj.angularVelocity;
    p.angularVelocityVariance                     = baseProj.angularVelocityVariance;
    p.spawnEnemyGuidOnDeath                       = baseProj.spawnEnemyGuidOnDeath;
    p.HasFixedKnockbackDirection                  = baseProj.HasFixedKnockbackDirection;
    p.FixedKnockbackDirection                     = baseProj.FixedKnockbackDirection;
    p.pierceMinorBreakables                       = baseProj.pierceMinorBreakables;
    p.objectImpactEventName                       = baseProj.objectImpactEventName;
    p.enemyImpactEventName                        = baseProj.enemyImpactEventName;
    p.onDestroyEventName                          = baseProj.onDestroyEventName;
    p.additionalStartEventName                    = baseProj.additionalStartEventName;
    p.IsRadialBurstLimited                        = baseProj.IsRadialBurstLimited;
    p.MaxRadialBurstLimit                         = baseProj.MaxRadialBurstLimit;
    p.AdditionalBurstLimits                       = baseProj.AdditionalBurstLimits;
    p.AppliesKnockbackToPlayer                    = baseProj.AppliesKnockbackToPlayer;
    p.PlayerKnockbackForce                        = baseProj.PlayerKnockbackForce;
    p.HasDefaultTint                              = baseProj.HasDefaultTint;
    p.DefaultTintColor                            = baseProj.DefaultTintColor;
    p.PenetratesInternalWalls                     = baseProj.PenetratesInternalWalls;
    p.neverMaskThis                               = baseProj.neverMaskThis;
    p.isFakeBullet                                = baseProj.isFakeBullet;
    p.CanBecomeBlackBullet                        = baseProj.CanBecomeBlackBullet;
    p.TrailRenderer                               = baseProj.TrailRenderer;
    p.CustomTrailRenderer                         = baseProj.CustomTrailRenderer;
    p.ParticleTrail                               = baseProj.ParticleTrail;
    p.DelayedDamageToExploders                    = baseProj.DelayedDamageToExploders;
    p.AdditionalScaleMultiplier                   = baseProj.AdditionalScaleMultiplier;
    UnityEngine.Object.DestroyImmediate(baseProj);  // we don't want two projectiles attached to the same gameObject
    return p;
  }

  /// <summary>Initializes and returns the first projectile from the default module of a gun</summary>
  public static ProjectileType InitFirstProjectileOfType<ProjectileType>(this Gun gun, GunData b)
    where ProjectileType : Projectile
  {
    Projectile clone = (b.baseProjectile ?? gun.DefaultModule.projectiles[0]).Clone(b);
    if (clone is not ProjectileType p)
      p = clone.ConvertToSpecialtyType<ProjectileType>();
    gun.DefaultModule.projectiles[0] = p;
    p.transform.parent = gun.barrelOffset;
    return p;
  }

  /// <summary>Generic version of InitFirstProjectileOfType, assuming we just want a normal projectile</summary>
  public static Projectile InitFirstProjectile(this Gun gun, GunData b = null)
  {
    return gun.InitFirstProjectileOfType<Projectile>(b);
  }

  /// <summary>Clone and return a projectile from a specific gun (Gun version)</summary>
  public static Projectile CloneProjectile(this Gun gun, GunData b = null)
  {
      return gun.DefaultModule.projectiles[0].Clone(b);
  }

  /// <summary>Clone and return a projectile from a specific gun (Items version)</summary>
  public static Projectile CloneProjectile(this Items gunItem, GunData b = null)
  {
      return gunItem.AsGun().DefaultModule.projectiles[0].Clone(b);
  }

  /// <summary>Returns a projectile from a specific gun (Items version)</summary>
  public static Projectile Projectile(this Items gunItem)
  {
      return gunItem.AsGun().DefaultModule.projectiles[0];
  }

  /// <summary>Set basic attributes for a projectile module and return it</summary>
  public static ProjectileModule SetAttributes(this ProjectileModule mod, GunData b = null)
  {
    b ??= GunData.Default;
    CwaffGun cg = b.gun.gameObject.GetComponent<CwaffGun>();

    if (b.clipSize.HasValue)
      mod.numberOfShotsInClip = b.clipSize.Value;
    if (b.cooldown.HasValue)
      mod.cooldownTime        = b.cooldown.Value;
    if (b.burstCooldown.HasValue)
      mod.burstCooldownTime   = b.burstCooldown.Value;
    mod.ammoCost                 = b.ammoCost;
    mod.shootStyle               = b.shootStyle;
    mod.sequenceStyle            = b.sequenceStyle;
    mod.angleFromAim             = b.angleFromAim;
    mod.ignoredForReloadPurposes = b.ignoredForReloadPurposes;
    mod.mirror                   = b.mirror;
    if (b.angleVariance.HasValue)
      mod.angleVariance = b.angleVariance.Value;
    if (b.hideAmmo)
      cg.hideAmmo = true;
    else if (b.customClip)
      mod.SetupCustomAmmoClip(b);
    else if (b.ammoType.HasValue)
      mod.ammoType = b.ammoType.Value;

    cg.spinupTime  = b.spinupTime;
    cg.spinupSound = b.spinupSound;

    return mod;
  }

  /// <summary>Set basic attributes for a gun's default projectile module and return it</summary>
  public static ProjectileModule SetupDefaultModule(this Gun gun, GunData b = null)
  {
    b ??= GunData.Default;
    b.gun = gun;
    return gun.DefaultModule.SetAttributes(b);
  }

  /// <summary>Add a component to a gun's GameObject, perform setup if necessary, and return the gun</summary>
  public static Gun Attach<T>(this Gun gun, Action<T> predicate = null, bool allowDuplicates = false) where T : MonoBehaviour
  {
    T component = allowDuplicates ? gun.gameObject.AddComponent<T>() : gun.gameObject.GetOrAddComponent<T>();
    if (predicate != null)
      predicate(component);
    return gun;
  }

  /// <summary>Add a component to a projectile's GameObject, perform setup if necessary, and return the projectile</summary>
  public static Projectile Attach<T>(this Projectile projectile, Action<T> predicate = null, bool allowDuplicates = false) where T : MonoBehaviour
  {
    T component = allowDuplicates ? projectile.gameObject.AddComponent<T>() : projectile.gameObject.GetOrAddComponent<T>();
    if (predicate != null)
      predicate(component);
    return projectile;
  }

  /// <summary>Assign a projectile by reference</summary>
  public static T Assign<T>(this T projectile, out T projectileRef) where T : Projectile
  {
    return projectileRef = projectile;
  }

  /// <summary>Assign a gun by reference</summary>
  public static T AssignGun<T>(this T gun, out T gunRef) where T : Gun
  {
    return gunRef = gun;
  }

  /// <summary>Add each animation from a list in turn to a projectile and return that projectile</summary>
  public static Projectile AddAnimations(this Projectile proj, params tk2dSpriteAnimationClip[] animations)
  {
    foreach(tk2dSpriteAnimationClip clip in animations)
      proj.AddAnimation(clip);
    return proj;
  }

  private static ProjectileModule _DummyChargeModule = null;
  /// <summary>Get a dummy charge module and add it to a gun (useful for weapons that need to check if they're being charged)</summary>
  public static void AddDummyChargeModule(this Gun gun)
  {
    _DummyChargeModule ??= new(){
      shootStyle = ShootStyle.Charged,
      ammoCost   = 0,  // hides from the UI when duct-taped
      chargeProjectiles = new(){ new(){
        Projectile = Lazy.NoProjectile(),
        ChargeTime = float.MaxValue,
      }},
      numberOfShotsInClip = 1,
      ammoType            = GameUIAmmoType.AmmoType.CUSTOM,
      customAmmoType      = "white",
    };
    gun.Volley.projectiles.Add(ProjectileModule.CreateClone(_DummyChargeModule, false, gun.Volley.projectiles.Count));
  }

  /// <summary>Duplicates a gun's default module, optionally clones the projectiles and/or adds it to the gun, and returns the new module</summary>
  public static ProjectileModule DuplicateDefaultModule(this Gun gun, bool cloneProjectiles = true, bool add = true)
  {
    ProjectileModule mod = ProjectileModule.CreateClone(gun.DefaultModule, inheritGuid: false);
    if (add)
      gun.Volley.projectiles.Add(mod);
    if (!cloneProjectiles)
      return mod;

    //WARNING: doesn't clone charge / final projectiles yet
    if (mod.projectiles != null)
      for (int i = 0; i < mod.projectiles.Count; ++i)
        mod.projectiles[i] = mod.projectiles[i].Clone();

    return mod;
  }

  /// <summary>Dummy class for suppressing reload animations on guns with reload times of 0</summary>
  public class ReloadAnimationSuppressor : MonoBehaviour {}

  /// <summary>Prevent a gun from ever playing reload animations when it's reload time is 0</summary>
  public static void SuppressReloadAnimations(this Gun gun)
    => gun.gameObject.GetOrAddComponent<ReloadAnimationSuppressor>();

  /// <summary>Helper patch for enabling ReloadAnimationSuppressor's functionality</summary>
  [HarmonyPatch(typeof(Gun), nameof(Gun.FinishReload))]
  private class ReloadAnimationSuppressorPatch
  {
      static void Prefix(Gun __instance, bool activeReload, ref bool silent, bool isImmediate)
      {
          if (__instance.GetComponent<ReloadAnimationSuppressor>())
              silent = true;
      }
  }
}
