namespace CwaffingTheGungy;

/*
    Need to prevent:
        - dodge rolling
        - stopping firing

        m_player.inventory.GunLocked.SetOverride("spren gun", true);
*/

public class Commitment : CwaffGun
{
    public static string ItemName         = "Commitment";
    public static string ShortDescription = "Going Until It's Gone";
    public static string LongDescription  = "(cannot switch weapons or stop firing until out of ammo)";
    public static string Lore             = "TBD";

    private bool committed = false;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Commitment>(ItemName, ShortDescription, LongDescription, Lore);
            gun.DefaultModule.ammoCost            = 1;
            gun.DefaultModule.shootStyle          = ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle       = ProjectileSequenceStyle.Random;
            gun.reloadTime                        = 1.1f;
            gun.DefaultModule.angleVariance       = 15.0f;
            gun.DefaultModule.cooldownTime        = 0.15f;
            gun.DefaultModule.numberOfShotsInClip = 10000;
            gun.CanBeDropped                      = false;
            gun.CanGainAmmo                       = false;
            gun.quality                           = ItemQuality.C;
            gun.SetBaseMaxAmmo(500);
            gun.SetAnimationFPS(gun.shootAnimation, 24);

        Projectile projectile       = gun.InitFirstProjectile(GunData.New(damage: 16.0f, speed: 24.0f));
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        // if (gun.ammo <= 1)
        if (gun.ammo <= 0)
        {
            if (this.committed)
                ETGModConsole.Log("Uncommitted!");
            // player.inventory.DestroyCurrentGun();
            this.committed                = false;
            // player.m_preventItemSwitching = false;
            player.forceFireDown          = false;
            player.forceFireUp            = true;
        }
        else
        {
            if (!this.committed)
                ETGModConsole.Log("Committed!");
            this.committed                = true;
            // player.m_preventItemSwitching = true;
            player.forceFireUp            = false;
            player.forceFireDown          = true;
        }
    }

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner)
            return;
        bool forceOn             = this.committed && (this.gun.CurrentAmmo > 0);
        this.PlayerOwner.m_preventItemSwitching = forceOn;
        this.PlayerOwner.forceFireDown          = forceOn;

        // p.forceFire = true;
        // p.m_handleDodgeRollStartThisFrame = false;
        // p.m_disableInput = new OverridableBool(true);
        // p.m_inputState   = PlayerInputState.OnlyMovement;
        // p.m_dodgeRollTimer = 100f;
        // if (p.IsDodgeRolling)
        // {
        //     p.ForceStopDodgeRoll();
        //     // p.ClearDodgeRollState();
        // }
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        if (this.committed)
        {
            this.committed = false;
            player.forceFireDown = false;
        }
        base.OnDroppedByPlayer(player);
        ETGModConsole.Log("Dropped gun");
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        if (this.committed && this.PlayerOwner)
        {
            while (this.PlayerOwner.inventory.CurrentGun.PickupObjectId != this.gun.PickupObjectId)
                this.PlayerOwner.inventory.ChangeGun(1, false, false);
            ETGModConsole.Log("Forcing gun back to Commitment");
        }
        base.OnSwitchedAwayFromThisGun();
    }
}
