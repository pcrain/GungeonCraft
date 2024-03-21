namespace CwaffingTheGungy;

public class DeathNote : AdvancedGunBehavior
{
    public static string ItemName         = "Death Note";
    public static string ShortDescription = "Notably Dangerous";
    public static string LongDescription  = "(TBD)";
    public static string Lore             = "TBD";

    internal static Dictionary<int, Nametag> _Nametags = new();
    internal static string _Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<DeathNote>(ItemName, ShortDescription, LongDescription, Lore);
            gun.gunSwitchGroup                    = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup;
            gun.DefaultModule.ammoCost            = 1;
            gun.DefaultModule.shootStyle          = ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle       = ProjectileSequenceStyle.Random;
            gun.reloadTime                        = 1.1f;
            gun.InfiniteAmmo                      = true;
            gun.DefaultModule.angleVariance       = 15.0f;
            gun.quality                           = ItemQuality.D;

        Projectile projectile = gun.InitFirstProjectile(GunData.New());
    }

    protected override void OnPostDroppedByPlayer(PlayerController player)
    {
        base.OnPostDroppedByPlayer(player);
        WhoAreTheyAgain();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        WhoAreTheyAgain();
        base.OnSwitchedAwayFromThisGun();
    }

    protected override void Update()
    {
        base.Update();
        YouShallKnowTheirNames();
    }

    private void WhoAreTheyAgain()
    {
        UpdateNametags(false);
    }

    private void UpdateNametags(bool enable)
    {
        List<int> deadEnemies = new();
        foreach(KeyValuePair<int, Nametag> entry in _Nametags)
        {
            if (!entry.Value.UpdateWhileParentAlive())
                deadEnemies.Add(entry.Key);
            else
                entry.Value.SetEnabled(enable);
        }
        foreach (int key in deadEnemies)
            _Nametags.Remove(key);
    }

    private void YouShallKnowTheirNames()
    {
        UpdateNametags(true);

        List<AIActor> activeEnemies = this.Owner.GetAbsoluteParentRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
        if (activeEnemies == null)
            return;

        foreach (AIActor enemy in activeEnemies)
        {
            if (!enemy || !enemy.specRigidbody || enemy.IsGone || !enemy.healthHaver || enemy.healthHaver.IsDead)
                continue;
            if (!enemy.gameObject.GetComponent<Nametag>())
                _Nametags[enemy.GetHashCode()] = enemy.gameObject.AddComponent<Nametag>();
        }
    }
}
