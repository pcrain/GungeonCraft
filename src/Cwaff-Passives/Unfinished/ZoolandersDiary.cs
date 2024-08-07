namespace CwaffingTheGungy;

public class ZoolandersDiary : CwaffPassive
{
    public static string ItemName         = "Zoolander's Diary";
    public static string ShortDescription = "Ambiturner No More";
    public static string LongDescription  = "(3x damage when shooting right; 1/3 damage when aiming left)";
    public static string Lore             = "TBD";

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<ZoolandersDiary>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        player.PostProcessProjectile += this.PostProcessProjectile;
    }

    public override DebrisObject Drop(PlayerController player)
    {
        player.PostProcessProjectile -= this.PostProcessProjectile;
        return base.Drop(player);
    }

    private void PostProcessProjectile(Projectile proj, float effectChanceScalar)
    {
        if (this.Owner == null)
            return;

        float gunAngle = BraveMathCollege.ClampAngle180(this.Owner.m_currentGunAngle);
        if (Math.Abs(gunAngle) < 45)
            proj.baseData.damage *= 3;
        else if (Math.Abs(gunAngle) > 135)
            proj.baseData.damage /= 3;
    }
}
