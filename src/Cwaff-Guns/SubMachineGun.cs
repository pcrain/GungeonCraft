namespace CwaffingTheGungy;

public class SubMachineGun : AdvancedGunBehavior
{
    public static string ItemName         = "Sub Machine Gun";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "Calling this object a gun stretches the definition of the word to its absolute limits, but it evidently scrapes by just enough to get a pass from both the legal system and the Breach's marketing division. Even so, this 'gun' doesn't seem to be particularly useful as a conventional firearm.";

    internal static GameObject _NourishVFX;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<SubMachineGun>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.CHARM, reloadTime: 1.5f, ammo: 200);
            gun.SetAnimationFPS(gun.shootAnimation, 20);
            gun.SetAnimationFPS(gun.reloadAnimation, 10);
            gun.SetMuzzleVFX("muzzle_sub_machine_gun", fps: 30, scale: 0.5f, anchor: Anchor.MiddleCenter);
            gun.SetFireAudio("sub_machine_gun_fire_sound");
            gun.SetReloadAudio("sub_machine_gun_reload_sound");

        gun.InitProjectile(GunData.New(sprite: "sandwich_projectile", clipSize: 5, cooldown: 0.2f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 0.0f, shouldRotate: false)).Attach<NourishingProjectile>();

        _NourishVFX = VFX.Create("nourish_vfx",
            fps: 18, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 1, emissiveColour: Color.Lerp(Color.green, Color.white, 0.5f));
    }
}

public class NourishingProjectile : MonoBehaviour
{
    private void Start()
    {
        base.GetComponent<Projectile>().OnHitEnemy += this.OnHitEnemy;
    }

    private void OnHitEnemy(Projectile p, SpeculativeRigidbody body, bool killed)
    {
        if ((body.healthHaver is not HealthHaver hh) || !hh.IsAlive || killed)
            return; // already dead
        if ((body.aiActor is not AIActor enemy) || enemy.CanTargetEnemies)
            return; // already charmed
        if (hh.GetCurrentHealthPercentage() >= UnityEngine.Random.value)
            return; // percent chance to charm is equal to percent of depleted health

        enemy.ApplyEffect((ItemHelper.Get(Items.YellowChamber) as YellowChamberItem).CharmEffect);
        if (enemy.CanTargetPlayers || !enemy.CanTargetEnemies)
            return; // failed to apply charm

        hh.FullHeal();
        GameObject vfx = SpawnManager.SpawnVFX(SubMachineGun._NourishVFX, enemy.sprite.WorldTopCenter + new Vector2(0f, 1f), Quaternion.identity, ignoresPools: true);
        vfx.GetComponent<tk2dSprite>().HeightOffGround = 1f;
        vfx.transform.parent = enemy.sprite.transform;
        vfx.AddComponent<GlowAndFadeOut>().Setup(
            fadeInTime: 0.15f, glowInTime: 0.20f, glowOutTime: 0.20f, fadeOutTime: 0.15f, maxEmit: 5f, destroy: true);
        enemy.gameObject.Play("nourished_sound");
    }
}
