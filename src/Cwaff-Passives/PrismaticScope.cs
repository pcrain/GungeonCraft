namespace CwaffingTheGungy;

public class PrismaticScope : CwaffPassive
{
    public static string ItemName         = "Prismatic Scope";
    public static string ShortDescription = "A Colorful Sight";
    public static string LongDescription  = "Increases the damage of all beam weapons by 40%.";
    public static string Lore             = "TBD";

    private const float _BEAM_DAMAGE_MULT = 1.4f;

    internal static GameObject _PrismaticVFX;

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
        if (beam.projectile is Projectile p && p.PossibleSourceGun && p.PossibleSourceGun.gunClass == GunClass.BEAM)
        {
            p.baseData.damage *= _BEAM_DAMAGE_MULT;
            beam.gameObject.AddComponent<Sparklyboi>();
        }
    }

    private class Sparklyboi : MonoBehaviour
    {
        private const float _PARTICLE_TIME = 0.05f;

        private BeamController _beam;
        private BasicBeamController _basicBeam;
        private float _timer;

        private void Start()
        {
            this._beam = base.gameObject.GetComponent<BeamController>();
            this._basicBeam = this._beam as BasicBeamController;
        }

        private void Update()
        {
            if (this._basicBeam && this._basicBeam.State == BeamState.Charging)
                return;
            if ((this._timer += BraveTime.DeltaTime) < _PARTICLE_TIME)
                return;

            this._timer -= _PARTICLE_TIME;
            CwaffVFX.SpawnBurst(
                prefab           : _PrismaticVFX,
                numToSpawn       : 3,
                basePosition     : this._beam.Origin,
                baseVelocity     : 6f * this._beam.Direction.normalized,
                velocityVariance : 2f,
                spread           : 60f,
                lifetime         : 0.75f,
                fadeOutTime      : 0.75f,
                randomFrame      : true
              );
        }
    }
}

