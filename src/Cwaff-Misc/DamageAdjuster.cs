namespace CwaffingTheGungy;

/// <summary>Class that allows projectiles to adjust their damage upon colliding with an enemy.</summary>
public abstract class DamageAdjuster : MonoBehaviour
{
    private static float AdjustDamageStatic(float currentDamage, Projectile proj, SpeculativeRigidbody body)
    {
        if (body.GetComponent<AIActor>() is not AIActor enemy)
            return currentDamage;
        float adjustedDamage = currentDamage;
        foreach (DamageAdjuster adj in proj.GetComponents<DamageAdjuster>())
            adjustedDamage = adj.AdjustDamage(adjustedDamage, proj, enemy);
        return adjustedDamage;
    }

    protected abstract float AdjustDamage(float currentDamage, Projectile proj, AIActor enemy);
}
