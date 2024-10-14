namespace CwaffingTheGungy;

public class Detergent : CwaffActive
{
    public static string ItemName         = "Detergent";
    public static string ShortDescription = "Next to Godliness";
    public static string LongDescription  = "Sets curse to 0 upon use.";
    public static string Lore             = "An ordinary bottle of liquid laundry detergent, often used to repel the Jammed by disguising the scent of contraband inventory. The fresh non-specifically clean scent reminds you of home and provides with you an additional false sense of security.";

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<Detergent>(ItemName, ShortDescription, LongDescription, Lore);
        item.AddToSubShop(ItemBuilder.ShopType.Goopton);
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
