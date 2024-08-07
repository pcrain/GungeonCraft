﻿namespace CwaffingTheGungy;

public class DeathNote : CwaffGun
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
            gun.DefaultModule.ammoCost            = 1;
            gun.DefaultModule.shootStyle          = ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle       = ProjectileSequenceStyle.Random;
            gun.reloadTime                        = 1.1f;
            gun.InfiniteAmmo                      = true;
            gun.DefaultModule.angleVariance       = 15.0f;
            gun.quality                           = ItemQuality.D;

        Projectile projectile = gun.InitFirstProjectile(GunData.New());
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        WhoAreTheyAgain();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        WhoAreTheyAgain();
        base.OnSwitchedAwayFromThisGun();
    }

    public override void Update()
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
        if (!this.PlayerOwner)
            return;
        foreach (AIActor enemy in this.PlayerOwner.CurrentRoom.SafeGetEnemiesInRoom())
        {
            if (!enemy || !enemy.specRigidbody || enemy.IsGone || !enemy.healthHaver || enemy.healthHaver.IsDead)
                continue;
            if (!enemy.gameObject.GetComponent<Nametag>())
                _Nametags[enemy.GetHashCode()] = enemy.gameObject.AddComponent<Nametag>();
        }
    }
}
