namespace CwaffingTheGungy;

public class PaintballCannon : CwaffGun
{
    public static string ItemName         = "Paintball Cannon";
    public static string ShortDescription = "The T is Silent";
    public static string LongDescription  = "Shoots various colored projectiles that stain enemies and leave colored goop in their wake.";
    public static string Lore             = "Paintball guns are traditionally known for their usage in niche sporting events moreso than their viability in actual combat. A product of executive meddling and rebranding, the paintball cannon is a slightly beefed-up paintball gun with the potential to do at least a passable amount of damage. The increased projectile size has led to the leakage of paint as the gun's projectiles are in transit. Ironically, many Gungeoneers find the resulting paint streaks charming and therapeutic, making this design flaw the gun's primary selling point that sets it apart from otherwise more functional weapons.";

    public static void Init()
    {
        Lazy.SetupGun<PaintballCannon>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.PISTOL, reloadTime: 0.9f, ammo: 600, shootFps: 14, reloadFps: 4,
            muzzleFrom: Items.Mailbox, fireAudio: "paintball_shoot_sound", reloadAudio: "paintball_reload_sound")
          .AddToShop(ItemBuilder.ShopType.Goopton)
          .InitProjectile(GunData.New(sprite: "paintball_cannon_projectile", scale: 0.9f, clipSize: 12, cooldown: 0.18f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 9.0f, speed: 25f, range: 18f, force: 12f, hitEnemySound: "paintball_impact_enemy_sound", hitWallSound: "paintball_impact_wall_sound"))
          .Attach<PaintballColorizer>()
          .Attach<GoopModifier>(g => {
            g.SpawnGoopOnCollision   = true;
            g.CollisionSpawnRadius   = 1f;
            g.SpawnGoopInFlight      = true;
            g.InFlightSpawnRadius    = 0.4f;
            g.InFlightSpawnFrequency = 0.01f;})
          .SetAllImpactVFX(VFX.CreatePool("paint_splatter_vfx", fps: 60, loops: false));
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        if (this.PlayerOwner && this.PlayerOwner.HasSynergy(Synergy.MASTERY_PAINTBALL_CANNON))
            projectile.GetComponent<PaintballColorizer>().mastered = true;
    }
}

public class PaintballColorizer : MonoBehaviour
{
    private enum Goop { CHARM, FIRE, CHEESE, ELECTRIC, POISON, WATER, WEB, ICE };

    private Color _tint;

    public bool mastered = false;

    private void Start()
    {
        Projectile p = base.GetComponent<Projectile>();

        int i = UnityEngine.Random.Range(0, EasyGoopDefinitions.ColorGoopColors.Count);
        Goop g = (Goop)i;
        this._tint = EasyGoopDefinitions.ColorGoopColors[i];
        GoopModifier gm = p.GetComponent<GoopModifier>();
        if (this.mastered)
        {
            gm.CollisionSpawnRadius *= 2f;
            gm.goopDefinition = g switch {
                Goop.CHARM    => EasyGoopDefinitions.CharmGoopDef, // pink   == charm
                Goop.FIRE     => EasyGoopDefinitions.FireDef,      // red    == fire
                Goop.CHEESE   => EasyGoopDefinitions.CheeseDef,    // orange == cheese
                Goop.ELECTRIC => EasyGoopDefinitions.WaterGoop,    // yellow == electrified water
                Goop.POISON   => EasyGoopDefinitions.PoisonDef,    // green  == poison
                Goop.WATER    => EasyGoopDefinitions.WaterGoop,    // blue   == water
                Goop.WEB      => EasyGoopDefinitions.WebGoop,      // purple == web
                Goop.ICE      => EasyGoopDefinitions.WaterGoop,    // cyan   == ice
                _ => throw new NotSupportedException(),
            };
            if (g == Goop.ELECTRIC)
                p.damageTypes |= CoreDamageTypes.Electric;
            else if (g == Goop.ICE)
                p.damageTypes |= CoreDamageTypes.Ice;
        }
        else
            gm.goopDefinition = EasyGoopDefinitions.ColorGoops[i];

        p.AdjustPlayerProjectileTint(_tint, priority: 1);
        p.OnHitEnemy += this.OnHitEnemy;
    }

    private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool killed)
    {
        GameActorHealthEffect tint = new GameActorHealthEffect() {
            TintColor                = this._tint,
            DeathTintColor           = this._tint,
            AppliesTint              = true,
            AppliesDeathTint         = true,
            AffectsEnemies           = true,
            DamagePerSecondToEnemies = 0f,
            duration                 = 10000000,
            effectIdentifier         = "Paintballed",
            };
        if (!enemy.aiActor)
            return;
        enemy.aiActor.RemoveEffect("Paintballed");
        enemy.aiActor.ApplyEffect(tint);
    }
}
