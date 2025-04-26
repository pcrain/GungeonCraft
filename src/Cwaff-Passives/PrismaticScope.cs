namespace CwaffingTheGungy;

public class PrismaticScope : CwaffPassive
{
    public static string ItemName         = "Prismatic Scope";
    public static string ShortDescription = "A Colorful Sight";
    public static string LongDescription  = "Increases the damage of all beam weapons by 40%.";
    public static string Lore             = "A thick, multifaceted lens shaped to refract light into a full spectrum of colors. Astute Gungeoneers noticed that installing the lens backwards on their guns served to focus and amplify their light-based projectiles. Even when installed backwards, the smatterings of multicolored light emitted are still highly aesthetic.";

    private const float _BEAM_DAMAGE_MULT = 1.4f;
    private const float _PARTICLE_TIME = 0.05f;

    internal static GameObject _PrismaticVFX;

    private float _timer = 0.0f;

    public static void Init()
    {
        PassiveItem item = Lazy.SetupPassive<PrismaticScope>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality     = ItemQuality.C;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);

        _PrismaticVFX = VFX.Create("prismatic_scope");
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        player.PostProcessBeam += this.PostProcessBeam;
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        if (player)
            player.PostProcessBeam -= this.PostProcessBeam;
    }

    private void PostProcessBeam(BeamController beam)
    {
        if (beam.projectile is not Projectile p)
            return;
        if (p.PossibleSourceGun is not Gun gun)
            return;
        if (gun.gunClass == GunClass.BEAM || gun.DefaultModule.shootStyle == ShootStyle.Beam)
            p.baseData.damage *= _BEAM_DAMAGE_MULT;
    }

    public override void Update()
    {
        base.Update();
        if (this.Owner is not PlayerController pc)
            return;
        if (pc.CurrentGun is not Gun gun || !gun.IsFiring)
            return;
        if (gun.gunClass != GunClass.BEAM && gun.DefaultModule.shootStyle != ShootStyle.Beam)
            return;
        if ((this._timer += BraveTime.DeltaTime) < _PARTICLE_TIME)
            return;
        this._timer -= _PARTICLE_TIME;
        CwaffVFX.SpawnBurst(
            prefab           : _PrismaticVFX,
            numToSpawn       : 5,
            basePosition     : gun.barrelOffset.position,
            baseVelocity     : gun.gunAngle.ToVector(16f),
            velocityVariance : 4f,
            spread           : 30f,
            lifetime         : 0.25f + 0.5f * UnityEngine.Random.value,
            randomFrame      : true,
            emissiveColor    : Color.white,
            emissivePower    : 100f
          );
    }
}

