namespace CwaffingTheGungy;

public class PaintballCannon : AdvancedGunBehavior
{
    public static string ItemName         = "Paintball Cannon";
    public static string SpriteName       = "paintball_cannon";
    public static string ProjectileName   = "86"; //marine sidearm
    public static string ShortDescription = "The T is Silent";
    public static string LongDescription  = "Shoots various colored projectiles that stain enemies and leave colored goop in their wake.";
    public static string Lore             = "Paintball guns are traditionally known for their usage in niche sporting events moreso than their viability in actual combat. A product of executive meddling and rebranding, the paintball cannon is a slightly beefed-up paintball gun with the potential to do at least a passable amount of damage. The increased projectile size has led to the leakage of paint as the gun's projectiles are in transit. Ironically, many Gungeoneers find the resulting paint streaks charming and therapeutic, making this design flaw the gun's primary selling point that sets it apart from otherwise more functional weapons.";

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<PaintballCannon>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.PISTOL, reloadTime: 0.9f, ammo: 600);
            gun.SetAnimationFPS(gun.shootAnimation, 14);
            gun.SetAnimationFPS(gun.reloadAnimation, 4);
            gun.SetMuzzleVFX(Items.Mailbox); // innocuous muzzle flash effects
            gun.SetFireAudio("paintball_shoot_sound");
            gun.SetReloadAudio("paintball_reload_sound");
            gun.AddToSubShop(ItemBuilder.ShopType.Goopton);

        gun.DefaultModule.SetAttributes(clipSize: 12, cooldown: 0.18f, shootStyle: ShootStyle.SemiAutomatic);

        Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
            projectile.baseData.damage = 9f;

        PaintballColorizer paintballController = projectile.gameObject.AddComponent<PaintballColorizer>();
            paintballController.ApplyColourToHitEnemies = true;
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        PaintballColorizer pbc =
            projectile.gameObject.GetComponent<PaintballColorizer>();

        GoopModifier goopmod           = projectile.gameObject.AddComponent<GoopModifier>();
        goopmod.SpawnGoopOnCollision   = true;
        goopmod.CollisionSpawnRadius   = 1f;
        goopmod.SpawnGoopInFlight      = true;
        goopmod.InFlightSpawnRadius    = 0.4f;
        goopmod.InFlightSpawnFrequency = 0.01f;
        goopmod.goopDefinition         = pbc.setColorAndGetGoop();

        base.PostProcessProjectile(projectile);
    }
}

public class PaintballColorizer : MonoBehaviour
{
    public  bool       ApplyColourToHitEnemies;
    public  int        tintPriority;
    public  Color      selectedColour;
    private Projectile m_projectile;
    public PaintballColorizer()
    {
        ApplyColourToHitEnemies = false;
        tintPriority            = 1;
    }
    public GoopDefinition setColorAndGetGoop() {
        int selectedIndex = UnityEngine.Random.Range(0, EasyGoopDefinitions.ColorGoopColors.Count);
        selectedColour = EasyGoopDefinitions.ColorGoopColors[selectedIndex];
        return EasyGoopDefinitions.ColorGoops[selectedIndex];
    }
    private void Start()
    {
        this.m_projectile = base.GetComponent<Projectile>();
        this.m_projectile.AdjustPlayerProjectileTint(selectedColour, tintPriority);
        if (ApplyColourToHitEnemies)
            this.m_projectile.OnHitEnemy += this.OnHitEnemy;
    }
    private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool what)
    {
        GameActorHealthEffect tint = new GameActorHealthEffect()
        {
            TintColor                = selectedColour,
            DeathTintColor           = selectedColour,
            AppliesTint              = true,
            AppliesDeathTint         = true,
            AffectsEnemies           = true,
            DamagePerSecondToEnemies = 0f,
            duration                 = 10000000,
            effectIdentifier         = "Paintballed",
        };
        enemy.aiActor.RemoveEffect("Paintballed");
        enemy.aiActor.ApplyEffect(tint);
    }
}
