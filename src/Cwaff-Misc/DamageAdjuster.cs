namespace CwaffingTheGungy;

/// <summary>Class that allows projectiles to adjust their damage upon colliding with an enemy.</summary>
public abstract class DamageAdjuster : MonoBehaviour
{
    private static float AdjustDamageStatic(float currentDamage, Projectile proj, SpeculativeRigidbody body)
    {
        if (proj.GetComponent<DamageAdjuster>() is not DamageAdjuster adj)
            return currentDamage;
        if (body.GetComponent<AIActor>() is not AIActor enemy)
            return currentDamage;
        return adj.AdjustDamage(currentDamage, proj, enemy);
    }

    protected abstract float AdjustDamage(float currentDamage, Projectile proj, AIActor enemy);
}
