namespace CwaffingTheGungy;

public class PaintballCannon : AdvancedGunBehavior
{
    public static string ItemName         = "Paintball Cannon";
    public static string ProjectileName   = "86"; //marine sidearm
    public static string ShortDescription = "The T is Silent";
    public static string LongDescription  = "Shoots various colored projectiles that stain enemies and leave colored goop in their wake.";
    public static string Lore             = "Paintball guns are traditionally known for their usage in niche sporting events moreso than their viability in actual combat. A product of executive meddling and rebranding, the paintball cannon is a slightly beefed-up paintball gun with the potential to do at least a passable amount of damage. The increased projectile size has led to the leakage of paint as the gun's projectiles are in transit. Ironically, many Gungeoneers find the resulting paint streaks charming and therapeutic, making this design flaw the gun's primary selling point that sets it apart from otherwise more functional weapons.";

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<PaintballCannon>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.PISTOL, reloadTime: 0.9f, ammo: 600);
            gun.SetAnimationFPS(gun.shootAnimation, 14);
            gun.SetAnimationFPS(gun.reloadAnimation, 4);
            gun.SetMuzzleVFX(Items.Mailbox); // innocuous muzzle flash effects
            gun.SetFireAudio("paintball_shoot_sound");
            gun.SetReloadAudio("paintball_reload_sound");
            gun.AddToSubShop(ItemBuilder.ShopType.Goopton);

        gun.InitProjectile(new(clipSize: 12, cooldown: 0.18f, shootStyle: ShootStyle.SemiAutomatic, damage: 9.0f)
          ).Attach<PaintballColorizer>(
          ).Attach<GoopModifier>(g => {
            g.SpawnGoopOnCollision   = true;
            g.CollisionSpawnRadius   = 1f;
            g.SpawnGoopInFlight      = true;
            g.InFlightSpawnRadius    = 0.4f;
            g.InFlightSpawnFrequency = 0.01f;
          });
    }
}

public class PaintballColorizer : MonoBehaviour
{
    private Color _tint;

    private void Start()
    {
        Projectile p = base.GetComponent<Projectile>();

        int i = UnityEngine.Random.Range(0, EasyGoopDefinitions.ColorGoopColors.Count);
        this._tint = EasyGoopDefinitions.ColorGoopColors[i];
        p.GetComponent<GoopModifier>().goopDefinition = EasyGoopDefinitions.ColorGoops[i];

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
        enemy.aiActor?.RemoveEffect("Paintballed");
        enemy.aiActor?.ApplyEffect(tint);
    }
}
