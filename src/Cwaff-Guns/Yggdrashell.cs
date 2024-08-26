namespace CwaffingTheGungy;

public class Yggdrashell : CwaffGun
{
    public static string ItemName         = "Yggdrashell";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Yggdrashell>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.BEAM, reloadTime: 0.9f, ammo: 600, shootFps: 14, reloadFps: 4,
                doesScreenShake: false);

        Projectile proj = gun.InitProjectile(GunData.New(clipSize: -1, cooldown: 0.1f, shootStyle: ShootStyle.Beam, doBeamSetup: false));
        CwaffRaidenBeamController beam = proj.AddRaidenBeamPrefab("yggdrashell_beam", 20);
        //TODO: replace later
        beam.ImpactRenderer = (Items.RaidenCoil).AsGun().singleModule.projectiles[0].GetComponent<RaidenBeamController>().ImpactRenderer;
        beam.maxTargets = 1;
        beam.targetType = CwaffRaidenBeamController.TargetType.Room;
    }
}
