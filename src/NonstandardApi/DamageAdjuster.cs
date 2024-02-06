namespace CwaffingTheGungy;

/// <summary>Class that allows projectiles to adjust their damage upon colliding with an enemy.</summary>
public abstract class DamageAdjuster : MonoBehaviour
{
    public static void Init()
    {
        new ILHook(
          typeof(Projectile).GetMethod("HandleDamage", BindingFlags.Instance | BindingFlags.NonPublic),
          HandleDamageIL
          );
    }

    private static void HandleDamageIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.Before,instr => instr.MatchStloc(4)))  // V_4 == damage -> float damage = num; in source
            return;
                                       // float damage is already on stack
        cursor.Emit(OpCodes.Ldarg_0);  // load Projectile this onto stack
        cursor.Emit(OpCodes.Ldarg_1);  // load SpeculativeRigidbody rigidbody onto stack
        cursor.Emit(OpCodes.Call, typeof(DamageAdjuster).GetMethod("AdjustDamageStatic", BindingFlags.Static | BindingFlags.NonPublic));
    }

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
