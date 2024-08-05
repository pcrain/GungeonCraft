namespace CwaffingTheGungy;

/// <summary>Extensions for improved projectile handling</summary>
public class CwaffProjectile : MonoBehaviour
{
    // sane defaults
    public string spawnSound         = null;
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
    }

    private void Update()
    {
      // enter update code here
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
    private class PreventOrbitingProjectilePatch
    {
        static bool Prefix(GunVolleyModificationItem __instance, BounceProjModifier bouncer, SpeculativeRigidbody srb)
        {
            if (bouncer.projectile.GetComponent<CwaffProjectile>() is not CwaffProjectile c)
              return true; // call the original method
            return !c.preventOrbiting; // skip the original method iff we are supposed to prevent orbiting
        }
    }

    /// <summary>Prevent beams from being affected by Orbital Bullets if preventOrbiting is set</summary>
    [HarmonyPatch(typeof(GunVolleyModificationItem), nameof(GunVolleyModificationItem.PostProcessProjectileOrbitBeam))]
    private class PreventOrbitingBeamPatch
    {
        static bool Prefix(GunVolleyModificationItem __instance, BeamController beam)
        {
            if (beam.projectile.GetComponent<CwaffProjectile>() is not CwaffProjectile c)
              return true; // call the original method
            return !c.preventOrbiting; // skip the original method iff we are supposed to prevent orbiting
        }
    }

    /// <summary>Prevent electric damage bullets from emitting sparks when they're electric / cursed</summary>
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.HandleSparks))]
    private class ProjectileHandleSparksPatch
    {
        static bool Prefix(Projectile __instance, Vector2? overridePoint)
        {
          if (__instance.GetComponent<CwaffProjectile>() is not CwaffProjectile cp)
            return true;     // call the original method
          return !cp.preventSparks; // skip original method if preventSparks is true
        }
    }
}
