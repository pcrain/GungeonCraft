namespace CwaffingTheGungy;

/// <summary>Class that allows projectiles to adjust their damage upon colliding with an enemy.</summary>
public abstract class DamageAdjuster : MonoBehaviour
{
    public enum ApplyPriority
    {
      Top,
      High,
      Normal, // the default priority, suitable for most things
      Low,
      Bottom  // minimum priority, useful for damage types that set damage conditionally based on other DamageAdjusters
    }

    public virtual ApplyPriority Priority => ApplyPriority.Normal;

    // NOTE: called by patch in CwaffPatches
    internal static float AdjustDamageStatic(float currentDamage, Projectile proj, SpeculativeRigidbody body)
    {
        if (body.GetComponent<AIActor>() is not AIActor enemy)
            return currentDamage;
        float adjustedDamage = currentDamage;
        foreach (DamageAdjuster adj in proj.GetComponents<DamageAdjuster>().OrderBy(d => d.Priority))
            adjustedDamage = adj.AdjustDamage(adjustedDamage, proj, enemy);
        return adjustedDamage;
    }

    protected abstract float AdjustDamage(float currentDamage, Projectile proj, AIActor enemy);
}
