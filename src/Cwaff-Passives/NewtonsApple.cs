namespace CwaffingTheGungy;

public class NewtonsApple : CwaffPassive
{
    public static string ItemName         = "Newton's Apple";
    public static string ShortDescription = "Doesn't Fall Far from the Tree";
    public static string LongDescription  = "Enemies home in on nearby player projectiles.";
    public static string Lore             = "Before an apple helped Isaac Newton invent gravity, everyone was stuck floating around everywhere all the time. After gravity eliminated this burden from all but a select few, scientists got to work synthesizing even stronger gravity apples to alleviate the burden of independent motion from the rest of the population.";

    private static readonly Color _AttractColor = new Color(81f/255f, 178f/255f, 242f/255f);

    private List<Projectile> _gravityProjectiles = new();
    private Dictionary<AIActor, ActiveKnockbackData> _knockbackDict = new();

    public static void Init()
    {
        PassiveItem item = Lazy.SetupPassive<NewtonsApple>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality     = ItemQuality.D;
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        player.PostProcessProjectile += this.PostProcessProjectile;
        player.OnRoomClearEvent += this.OnRoomClearEvent;
    }

    private void OnRoomClearEvent(PlayerController controller)
    {
        this._knockbackDict.CleanupKnockbackData();
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        if (!player)
          return;
        player.PostProcessProjectile -= this.PostProcessProjectile;
        player.OnRoomClearEvent -= this.OnRoomClearEvent;
        foreach (Projectile p in this._gravityProjectiles)
          if (p)
            p.OnDestruction -= this.OnProjectileDestruction;
        this._gravityProjectiles.Clear();
        this._knockbackDict.Clear();
    }

    private void PostProcessProjectile(Projectile projectile, float arg2)
    {
        this._gravityProjectiles.Add(projectile);
        projectile.OnDestruction += this.OnProjectileDestruction;
    }

    private void OnProjectileDestruction(Projectile projectile)
    {
        if (!this)
          return;
        projectile.OnDestruction -= this.OnProjectileDestruction;
        this._gravityProjectiles.Remove(projectile);
    }

    public override void Update()
    {
        const float MAX_SQR_INFLUENCE   = 36f;
        const float MIN_SQR_INFLUENCE   = 4f;
        const float DELTA_SQR_INFLUENCE = MAX_SQR_INFLUENCE - MIN_SQR_INFLUENCE;
        const float PULL_FORCE          = 20f;
        const float PARTICLE_CHANCE     = 3.0f;

        base.Update();
        if (!this.Owner)
            return;

        float dtime = BraveTime.DeltaTime;
        foreach (AIActor enemy in Lazy.GetAllNearbyEnemies(this.Owner.CenterPosition, ignoreWalls: true))
        {
          if (enemy.knockbackDoer is not KnockbackDoer kbd || kbd.m_isImmobile.Value)
            continue;

          Vector2 epos = enemy.CenterPosition;
          Projectile nearestProj = null;
          float nearestSqrMag = MAX_SQR_INFLUENCE;
          foreach (Projectile proj in this._gravityProjectiles)
          {
            if (!proj)
              continue;
            float sqrMag = (proj.SafeCenter - epos).sqrMagnitude;
            if (sqrMag > nearestSqrMag)
              continue;
            nearestSqrMag = sqrMag;
            nearestProj = proj;
          }
          if (!nearestProj)
            continue;

          if (UnityEngine.Random.value < dtime * PARTICLE_CHANCE)
            CwaffVFX.SpawnBurst(
              prefab           : FluxFist._AttractParticle,
              numToSpawn       : 2,
              basePosition     : 0.5f * (epos + nearestProj.SafeCenter),
              positionVariance : 0.5f,
              velocityVariance : 1.0f,
              velType          : CwaffVFX.Vel.AwayRadial,
              rotType          : CwaffVFX.Rot.Random,
              lifetime         : 0.5f,
              emissivePower    : 100f,
              emissiveColor    : _AttractColor,
              height           : 8f
              );
          float pullStrength = 1f - Mathf.Clamp01((nearestSqrMag - MIN_SQR_INFLUENCE) / DELTA_SQR_INFLUENCE);
          Vector2 pullVector = (PULL_FORCE * pullStrength) * (nearestProj.SafeCenter - epos).normalized;
          enemy.ApplyContinuousSourcedKnockback(base.gameObject, this._knockbackDict, pullVector);
        }
    }
}
