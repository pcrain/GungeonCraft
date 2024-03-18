namespace CwaffingTheGungy;

/// <summary>Extensions for improved projectile handling</summary>
public class CwaffProjectile : MonoBehaviour
{
    public string spawnSound;
    public bool stopSoundOnDeath;
    public string deathSound;
    public bool uniqueSounds;

    public GameObject shrapnelVFX;
    public int shrapnelCount;
    public float shrapnelMinVelocity;
    public float shrapnelMaxVelocity;
    public float shrapnelLifetime;

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
}
