namespace CwaffingTheGungy;

public class GunBuildData
{
  public int? clipSize;
  public float? cooldown;
  public float? angleVariance;
  public ShootStyle shootStyle;
  public ProjectileSequenceStyle sequenceStyle;
  public float chargeTime;
  public int ammoCost;
  public GameUIAmmoType.AmmoType? ammoType;
  public string customClip;
  public float? damage;
  public float? speed;
  public float? force;
  public float? range;
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
  public Vector3? manualOffsets;
  public IntVector2? overrideColliderPixelSizes;
  public IntVector2? overrideColliderOffsets;
  public Projectile overrideProjectilesToCopyFrom;
  public float bossDamageMult;
  public string destroySound;
  public bool? shouldRotate;
  public int barrageSize;
  public bool? shouldFlipHorizontally;
  public bool? shouldFlipVertically;
  public bool  useDummyChargeModule;

  /// <summary>Helper class containing setup information for a single module, single projectile gun.</summary>
  /// <param name="clipSize">The number of shots the gun can fired before reloading.</param>
  /// <param name="cooldown">The minimum number of seconds between shots</param>
  /// <param name="angleVariance">Maximum deviation from shooting angle (in degrees) a bullet may actually be fired.</param>
  /// <param name="shootStyle">How bullets are actually fired from the gun.</param>
  /// <param name="sequenceStyle">In what order bullets are actually fired from the gun.</param>
  /// <param name="chargeTime">If shootStyle is Charged, how long the projectile must charge for.</param>
  /// <param name="ammoCost">How much ammo is depleted per shot fired from a module.</param>
  /// <param name="ammoType">If using base game ammo clips, the type of ammo clip to use.</param>
  /// <param name="customClip">If using custom ammo clips, the base name of the sprite of the clip to use.</param>

  /// <param name="damage">The damage of the projectile.</param>
  /// <param name="speed">The speed of the projectile.</param>
  /// <param name="force">The force of the projectile.</param>
  /// <param name="range">The range of the projectile.</param>
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
  /// <param name="manualOffsets">[Unknown]</param>
  /// <param name="overrideColliderPixelSizes">If set, manually adjusts the size of the projectile's hitbox.</param>
  /// <param name="overrideColliderOffsets">If set, manually adjusts the offset of the projectile's hitbox.</param>
  /// <param name="overrideProjectilesToCopyFrom">[Unknown]</param>

  /// <param name="bossDamageMult"></param>
  /// <param name="destroySound"></param>
  /// <param name="shouldRotate"></param>
  /// <param name="barrageSize"></param>
  /// <param name="shouldFlipHorizontally"></param>
  /// <param name="shouldFlipVertically"></param>
  /// <param name="useDummyChargeModule"></param>
  public GunBuildData(int? clipSize = null, float? cooldown = null, float? angleVariance = null,
    ShootStyle shootStyle = ShootStyle.Automatic, ProjectileSequenceStyle sequenceStyle = ProjectileSequenceStyle.Random, float chargeTime = 0.0f, int ammoCost = 1, GameUIAmmoType.AmmoType? ammoType = null,
    string customClip = null, float? damage = null, float? speed = null, float? force = null, float? range = null, float poison = 0.0f, float fire = 0.0f, float freeze = 0.0f, float slow = 0.0f,
    bool? collidesWithEnemies = null, bool? ignoreDamageCaps = null, bool? collidesWithProjectiles = null, bool? surviveRigidbodyCollisions = null, bool? collidesWithTilemap = null,
    string sprite = null, int fps = 2, Anchor anchor = Anchor.MiddleCenter, float scale = 1.0f, bool anchorsChangeColliders = true, bool fixesScales = true, Vector3? manualOffsets = null, IntVector2? overrideColliderPixelSizes = null,
    IntVector2? overrideColliderOffsets = null, Projectile overrideProjectilesToCopyFrom = null, float bossDamageMult = 1.0f, string destroySound = null, bool? shouldRotate = null, int barrageSize = 1,
    bool? shouldFlipHorizontally = null, bool? shouldFlipVertically = null, bool useDummyChargeModule = false)
  {
      this.clipSize                      = clipSize;
      this.cooldown                      = cooldown;
      this.angleVariance                 = angleVariance;
      this.shootStyle                    = shootStyle;
      this.sequenceStyle                 = sequenceStyle;
      this.chargeTime                    = chargeTime;
      this.ammoCost                      = ammoCost;
      this.ammoType                      = ammoType;
      this.customClip                    = customClip;
      this.damage                        = damage;
      this.speed                         = speed;
      this.force                         = force;
      this.range                         = range;
      this.poison                        = poison;
      this.fire                          = fire;
      this.freeze                        = freeze;
      this.slow                          = slow;
      this.collidesWithEnemies           = collidesWithEnemies;
      this.ignoreDamageCaps              = ignoreDamageCaps;
      this.collidesWithProjectiles       = collidesWithProjectiles;
      this.surviveRigidbodyCollisions    = surviveRigidbodyCollisions;
      this.collidesWithTilemap           = collidesWithTilemap;
      this.sprite                        = sprite;
      this.fps                           = fps;
      this.anchor                        = anchor;
      this.scale                         = scale;
      this.anchorsChangeColliders        = anchorsChangeColliders;
      this.fixesScales                   = fixesScales;
      this.manualOffsets                 = manualOffsets;
      this.overrideColliderPixelSizes    = overrideColliderPixelSizes;
      this.overrideColliderOffsets       = overrideColliderOffsets;
      this.overrideProjectilesToCopyFrom = overrideProjectilesToCopyFrom;
      this.bossDamageMult                = bossDamageMult;
      this.destroySound                  = destroySound;
      this.shouldRotate                  = shouldRotate;
      this.barrageSize                   = barrageSize;
      this.shouldFlipHorizontally        = shouldFlipHorizontally;
      this.shouldFlipVertically          = shouldFlipVertically;
      this.useDummyChargeModule          = useDummyChargeModule;
  }

  public static GunBuildData Default = new GunBuildData();
}

public static class GunBuilder
{
  // General-purpose starting point for setting up most guns -- sets up default module, default projectile, and default animation for projectile
  public static ProjectileType InitSpecialProjectile<ProjectileType>(this Gun gun, GunBuildData b = null)
    where ProjectileType : Projectile
  {
    // If we haven't passed any GunBuildData in, use sane defaults
    b ??= GunBuildData.Default;

    // Set up the gun's default module, default projectile, and default projectile animation
    ProjectileModule mod = gun.SetupDefaultModule(b);
    ProjectileType proj = gun.InitFirstProjectileOfType<ProjectileType>(b);
      proj.AddDefaultAnimation(b);

    // Need to set up charge projectiles after both module and base projectile have been set up
    if (b.shootStyle == ShootStyle.Charged)
    {
      mod.chargeProjectiles = new();
      if (b.chargeTime > 0)
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

  // Generic version of the above, assuming we just want a normal projectile
  public static Projectile InitProjectile(this Gun gun, GunBuildData b = null)
  {
    return gun.InitSpecialProjectile<Projectile>(b);
  }

  // Clone, modify, and return a specific projectile
  public static ProjectileType CloneSpecial<ProjectileType>(this ProjectileType projectile, GunBuildData b = null)
    where ProjectileType : Projectile
  {
    b ??= GunBuildData.Default;
    ProjectileType p = projectile.ClonePrefab();

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
    if (p.specRigidbody)
      p.specRigidbody.CollideWithTileMap              = b.collidesWithTilemap ?? p.specRigidbody.CollideWithTileMap;  // doesn't work!

    // Non-defaulted
    p.BossDamageMultiplier                            = b.bossDamageMult;
    p.onDestroyEventName                              = b.destroySound;

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
      p.speedEffect = (ItemHelper.Get(Items.TripleCrossbow) as Gun).DefaultModule.projectiles[0].speedEffect;

    return p;
  }

  // Generic version of the above, assuming we just want a normal projectile
  public static Projectile Clone(this Projectile projectile, GunBuildData b = null)
  {
    return projectile.CloneSpecial<Projectile>(b);
  }

  // Copy fields from a Projectile to a subclass of that Projectile, then attach it to the base projectile's gameObject and destroy the base projectile
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

  // Initializes and returns the first projectile from the default module of a gun
  public static ProjectileType InitFirstProjectileOfType<ProjectileType>(this Gun gun, GunBuildData b)
    where ProjectileType : Projectile
  {
    Projectile clone = gun.DefaultModule.projectiles[0].Clone(b);
    ProjectileType p = clone as ProjectileType;
    if (p == null)
      p = clone.ConvertToSpecialtyType<ProjectileType>();
    gun.DefaultModule.projectiles[0] = p;
    p.transform.parent = gun.barrelOffset;
    return p;
  }

  // Generic version of the above, assuming we just want a normal projectile
  public static Projectile InitFirstProjectile(this Gun gun, GunBuildData b = null)
  {
    return gun.InitFirstProjectileOfType<Projectile>(b);
  }

  // Clone and return a projectile from a specific gun (Gun version)
  public static Projectile CloneProjectile(this Gun gun, GunBuildData b = null)
  {
      return gun.DefaultModule.projectiles[0].Clone(b);
  }

  // Clone and return a projectile from a specific gun (Items version)
  public static Projectile CloneProjectile(this Items gunItem, GunBuildData b = null)
  {
      return (ItemHelper.Get(gunItem) as Gun).DefaultModule.projectiles[0].Clone(b);
  }

  // Set basic attributes for a projectile module and return it
  public static ProjectileModule SetAttributes(this ProjectileModule mod, GunBuildData b = null)
  {
    b ??= GunBuildData.Default;
    if (b.clipSize.HasValue)
      mod.numberOfShotsInClip = b.clipSize.Value;
    if (b.cooldown.HasValue)
      mod.cooldownTime        = b.cooldown.Value;
    mod.ammoCost            = b.ammoCost;
    mod.shootStyle          = b.shootStyle;
    mod.sequenceStyle       = b.sequenceStyle;
    if (b.angleVariance.HasValue)
      mod.angleVariance = b.angleVariance.Value;
    if (!string.IsNullOrEmpty(b.customClip))
      mod.SetupCustomAmmoClip(b.customClip);
    else if (b.ammoType.HasValue)
      mod.ammoType = b.ammoType.Value;

    return mod;
  }

  // Set basic attributes for a gun's default projectile module and return it
  public static ProjectileModule SetupDefaultModule(this Gun gun, GunBuildData b = null)
  {
    return gun.DefaultModule.SetAttributes(b);
  }

  // Add a component to a projectile's GameObject, perform setup if necessary, and return the projectile
  public static Projectile Attach<T>(this Projectile projectile, Action<T> predicate = null, bool allowDuplicates = false) where T : MonoBehaviour
  {
    T component = allowDuplicates ? projectile.AddComponent<T>() : projectile.GetOrAddComponent<T>();
    if (predicate != null)
      predicate(component);
    return projectile;
  }

  // Add each animation from a list in turn to a projectile and return that projectile
  public static Projectile AddAnimations(this Projectile proj, params tk2dSpriteAnimationClip[] animations)
  {
    foreach(tk2dSpriteAnimationClip clip in animations)
      proj.AddAnimation(clip);
    return proj;
  }

  // Get a dummy charge module and add it to a gun (useful for weapons that need to check if they're being charged)
  private static ProjectileModule _DummyChargeModule = null;
  public static void AddDummyChargeModule(this Gun gun)
  {
    if (_DummyChargeModule == null)
    {
        _DummyChargeModule = new();
          _DummyChargeModule.shootStyle = ShootStyle.Charged;
          _DummyChargeModule.ammoCost   = 0;  // hides from the UI when duct-taped
          _DummyChargeModule.chargeProjectiles = new();
          _DummyChargeModule.chargeProjectiles.Add(new ProjectileModule.ChargeProjectile {
            Projectile = Lazy.NoProjectile(),
            ChargeTime = float.MaxValue,
          });
          _DummyChargeModule.numberOfShotsInClip = 1;
          _DummyChargeModule.ammoType            = GameUIAmmoType.AmmoType.CUSTOM;
          _DummyChargeModule.customAmmoType      = "white";
    }
    gun.Volley.projectiles.Add(ProjectileModule.CreateClone(_DummyChargeModule, false, gun.Volley.projectiles.Count));
  }
}
