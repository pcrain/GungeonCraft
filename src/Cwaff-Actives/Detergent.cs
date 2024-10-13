namespace CwaffingTheGungy;

public class Detergent : CwaffActive
{
    public static string ItemName         = "Detergent";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<Detergent>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality    = ItemQuality.B;
    }

    public override bool CanBeUsed(PlayerController user) => user.Curse() > 0;

    public override void DoEffect(PlayerController user)
    {
        user.ownerlessStatModifiers.Add(StatType.Curse.Add(-user.Curse()));
        user.stats.RecalculateStats(user);
        user.gameObject.Play("detergent_cleanse_sound");
        CwaffVFX.SpawnBurst(prefab: AllayCompanion._AllaySparkles, numToSpawn: 20,
            basePosition: user.CenterPosition, baseVelocity: new Vector2(0, 3f), positionVariance: 1f,
            minVelocity: 1f, velocityVariance: 1f, velType: CwaffVFX.Vel.AwayRadial,
            rotType: CwaffVFX.Rot.Random, lifetime: 1.0f, startScale: 0.75f,
            endScale: 0.5f, fadeOutTime: 0.5f, emissivePower: 5f);
    }
}
