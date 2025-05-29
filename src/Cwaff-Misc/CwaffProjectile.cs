namespace CwaffingTheGungy;

/// <summary>Extensions for improved projectile handling</summary>
[HarmonyPatch]
public class CwaffProjectile : MonoBehaviour
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

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;

        this._projectile.OnDestruction += OnProjectileDestroy;

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
        if (this.lightStrength > 0)
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
        }
        #endregion
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
      if (newChargeProjectile == null || newChargeProjectile.Projectile.gameObject.GetComponent<CwaffProjectile>() is not CwaffProjectile cp)
        return;
      if (!string.IsNullOrEmpty(cp.chargeSound))
        __instance.gameObject.Play(cp.chargeSound);
    }
}
