namespace CwaffingTheGungy;

public class LastResort : CwaffGun
{
    public static string ItemName         = "Last Resort";
    public static string ShortDescription = "Way Past Plan B";
    public static string LongDescription  = "(Gains stats for every ammo-less gun you have in your inventory.)";
    public static string Lore             = "TBD";

    public static List<string> lastResortLevelSprites;
    public static List<Projectile> lastResortProjectiles;
    public static Projectile lastResortBaseProjectile;

    internal static string _PumpChargeAnimationName = "PumpChargeAnimated";

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<LastResort>(ItemName, ShortDescription, LongDescription, Lore);

        gun.gunSwitchGroup                    = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup;
        gun.DefaultModule.shootStyle          = ShootStyle.Automatic;
        gun.DefaultModule.sequenceStyle       = ProjectileSequenceStyle.Random;
        gun.quality                           = ItemQuality.C;
        gun.DefaultModule.ammoCost            = 1;
        gun.SetBaseMaxAmmo(300);
        gun.SetAnimationFPS(gun.shootAnimation, 24);

        gun.reloadTime                        = 2.0f;
        gun.DefaultModule.cooldownTime        = 0.4f;
        gun.DefaultModule.numberOfShotsInClip = 10;

        lastResortProjectiles = new List<Projectile>();
        lastResortLevelSprites = new List<string>();

        Projectile projectile = gun.InitFirstProjectile(GunData.New(damage: 0.0f, speed: 5.0f, range: 5.0f));

        // No guns without ammo (base stats)
        Projectile p0 = projectile.Clone(GunData.New(damage: 2.0f));
        lastResortProjectiles.Add(p0);

        VFX.RegisterVFX(_PumpChargeAnimationName, ResMap.Get("PumpChargeMeter"),
            fps: 4, loops: true, anchor: Anchor.LowerCenter);

        // 1+ guns without ammo (scale stats from last projectile)
        for(int i = 1; i < 5; ++i)
        {
            Projectile po      = lastResortProjectiles[i-1];
            Projectile pi      = po.Clone(GunData.New(damage: po.baseData.damage * 2, speed: po.baseData.speed * 2, range: po.baseData.range * 2));
            lastResortProjectiles.Add(pi);
            lastResortLevelSprites.Add("PumpChargeMeter"+i);
        }

        lastResortBaseProjectile = projectile;
    }

    public override bool CollectedAmmoPickup(PlayerController player, Gun self, AmmoPickup pickup)
    {
        pickup.ForcePickupWithoutGainingAmmo(player);
        return false;
    }

    protected override void Update()
    {
        base.Update();
        if (!this.Player)
            return;
        // TODO: hack to detect reset gun stat changes, find a better way later
        if (this.gun.DefaultModule.projectiles[0].baseData.damage == 0f)
            ComputeLastResortStats();
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        ComputeLastResortStats();

        Gun g = (ItemHelper.Get(Items.MagicLamp) as Gun);
        // overrideMidairDeathVFX will make implicit use of CreatePoolFromVFXGameObject
        VFXPool v = VFX.CreatePoolFromVFXGameObject((ItemHelper.Get(Items.MagicLamp) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);

        Vector2 ppos = this.Player.sprite.WorldCenter;
        float pangle = this.Player.CurrentGun.gunAngle;
        int numInCircle = 7;
        for (int i = 0; i < numInCircle; ++i)
        {
            Vector2 finalpos = ppos + BraveMathCollege.DegreesToVector(pangle+(360/numInCircle)*i,3);
            VFX.SpawnVFXPool(v,finalpos, degAngle: pangle);
        }
    }

    protected override void OnPickedUpByPlayer(PlayerController player)
    {
        base.OnPickedUpByPlayer(player);
        ComputeLastResortStats();
    }

    private void ComputeLastResortStats()
    {
        if (!this.Player)
            return;
        int ammoless = 0;
        foreach (Gun gun in this.Player.inventory.AllGuns)
        {
            if (!(gun == this.gun || gun.InfiniteAmmo || gun.ammo > 0))
                ++ammoless;
        }
        this.gun.DefaultModule.projectiles[0] =
            lastResortProjectiles[Math.Min(ammoless,lastResortProjectiles.Count)];
        this.gun.reloadTime                        = 2.0f / (float)Math.Pow(1.5,ammoless);
        this.gun.DefaultModule.cooldownTime        = 0.4f / (float)Math.Pow(1.5,ammoless);
        this.gun.DefaultModule.numberOfShotsInClip = 4 * (1+ammoless);
        this.overrideNormalFireAudio = "Play_WPN_blasphemy_shot_01";
        if (ammoless > 0)
        {
            this.gameObject.Play("Play_OBJ_silenceblank_small_01");
            this.Player.ShowOverheadVFX(lastResortLevelSprites[ammoless-1], 1);
        }
        this.Player.ShowOverheadAnimatedVFX(_PumpChargeAnimationName, 2);
    }
}
