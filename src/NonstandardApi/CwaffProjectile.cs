namespace CwaffingTheGungy;

/// <summary>Extensions for improved projectile handling</summary>
public class CwaffProjectile : MonoBehaviour
{
    // sane defaults
    public string spawnSound         = null;
    public bool stopSoundOnDeath     = false;
    public string deathSound         = null;
    public bool uniqueSounds         = false;
    public GameObject shrapnelVFX    = null;
    public int shrapnelCount         = 10;
    public float shrapnelMinVelocity = 4f;
    public float shrapnelMaxVelocity = 8f;
    public float shrapnelLifetime    = 0.3f;
    public bool preventOrbiting      = false;

    private Projectile _projectile;
    private PlayerController _owner;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;

        this._projectile.OnDestruction += OnProjectileDestroy;

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
        if (!string.IsNullOrEmpty(deathSound))
        {
          if (uniqueSounds)
            base.gameObject.Play($"{deathSound}_stop_all");
          base.gameObject.Play(deathSound);
        }
      #endregion

      UnityEngine.Object.Destroy(this);  // clean up after ourselves when the projectile is destroyed
    }

    private void OnDestroy()
    {
      // enter destroy code here
    }

    private void SpawnShrapnel()
    {

    }

    /// <summary>Prevent projectiles from being affected by Orbital Bullets if preventOrbiting is set</summary>
    [HarmonyPatch(typeof(GunVolleyModificationItem), nameof(GunVolleyModificationItem.HandleStartOrbit))]
    private class PreventOrbitingPatch
    {
        static bool Prefix(GunVolleyModificationItem __instance, BounceProjModifier bouncer, SpeculativeRigidbody srb)
        {
            if (bouncer.projectile.GetComponent<CwaffProjectile>() is not CwaffProjectile c)
              return true; // call the original method
            return !c.preventOrbiting; // skip the original method iff we are supposed to prevent orbiting
        }
    }
}
