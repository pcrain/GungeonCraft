namespace CwaffingTheGungy;

public class SubMachineGun : CwaffGun
{
    public static string ItemName         = "Sub Machine Gun";
    public static string ShortDescription = "The Hero You Deserve";
    public static string LongDescription  = "Fires harmless sandwiches that have a chance to permacharm enemies equal to twice the percent of health they have lost, with enemies at 50% health or less being 100% susceptible to charm. Enemies are fully healed upon being charmed. Can be eaten when out of ammo to restore 1.5 hearts.";
    public static string Lore             = "Calling this object a gun stretches the definition of the word to its absolute limits, but it evidently scrapes by just enough to get a pass from both the legal system and the Breach's marketing division. Even so, this 'gun' doesn't seem to be particularly useful as a conventional firearm.";

    private const float _HEAL_AMOUNT = 1.5f;

    internal static GameObject _NourishVFX;
    internal static GameObject _NourishSelfVFX;

    public static void Init()
    {
        Lazy.SetupGun<SubMachineGun>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.CHARM, reloadTime: 1.5f, ammo: 200, shootFps: 20, reloadFps: 10,
            muzzleVFX: "muzzle_sub_machine_gun", muzzleFps: 30, muzzleScale: 0.5f, muzzleAnchor: Anchor.MiddleCenter,
            fireAudio: "sub_machine_gun_fire_sound", reloadAudio: "sub_machine_gun_reload_sound", banFromBlessedRuns: true)
          .InitProjectile(GunData.New(sprite: "sandwich_projectile", clipSize: 5, cooldown: 0.2f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 0.0f, shouldRotate: false, customClip: true))
          .Attach<NourishingProjectile>();

        _NourishVFX = VFX.Create("nourish_vfx", fps: 18, emissivePower: 1, emissiveColour: Color.Lerp(Color.green, Color.white, 0.5f));
        _NourishSelfVFX = Items.Ration.AsActive().gameObject.GetComponent<RationItem>().healVFX;
    }

    public override bool OnZeroAmmoAttack()
    {
        base.OnZeroAmmoAttack();
        if (this.PlayerOwner is not PlayerController pc)
            return true;

        pc.gameObject.Play("sub_machine_gun_reload_sound");
        pc.gameObject.Play("nourished_sound");
        pc.healthHaver.ApplyHealing(_HEAL_AMOUNT);
        pc.PlayEffectOnActor(_NourishSelfVFX, Vector3.zero);
        UnityEngine.Object.Destroy(pc.ForceDropGun(this.gun).gameObject);
        return false;
    }
}

public class NourishingProjectile : MonoBehaviour
{
    private Projectile _proj = null;

    private void Start()
    {
        this._proj = base.GetComponent<Projectile>();
        this._proj.OnHitEnemy += this.OnHitEnemy;
    }

    private void OnHitEnemy(Projectile p, SpeculativeRigidbody body, bool killed)
    {
        if ((body.healthHaver is not HealthHaver hh) || !hh.IsAlive || killed)
            return; // already dead
        if ((body.aiActor is not AIActor enemy) || enemy.CanTargetEnemies)
            return; // already charmed
        if ((1f - hh.GetCurrentHealthPercentage()) <= (0.5f * UnityEngine.Random.value))
            return; // percent chance to charm is equal to twice the percent of depleted health

        enemy.ApplyEffect((ItemHelper.Get(Items.YellowChamber) as YellowChamberItem).CharmEffect);
        if (enemy.CanTargetPlayers || !enemy.CanTargetEnemies)
            return; // failed to apply charm

        if (this._proj.Owner is PlayerController player && player.HasSynergy(Synergy.I_NEED_A_HERO))
            enemy.ReplaceGun(Items.Heroine);

        hh.FullHeal();
        GameObject vfx = SpawnManager.SpawnVFX(SubMachineGun._NourishVFX, enemy.sprite.WorldTopCenter + new Vector2(0f, 1f), Quaternion.identity, ignoresPools: true);
        vfx.GetComponent<tk2dSprite>().HeightOffGround = 1f;
        vfx.transform.parent = enemy.sprite.transform;
        vfx.AddComponent<GlowAndFadeOut>().Setup(
            fadeInTime: 0.15f, glowInTime: 0.20f, holdTime: 0.0f, glowOutTime: 0.20f, fadeOutTime: 0.15f, maxEmit: 5f, destroy: true);
        enemy.gameObject.Play("nourished_sound");
    }
}
