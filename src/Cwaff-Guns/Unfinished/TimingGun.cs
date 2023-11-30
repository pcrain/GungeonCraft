namespace CwaffingTheGungy;

public class TimingGun : AdvancedGunBehavior
{
    public static string ItemName         = "Timing Gun";
    public static string SpriteName       = "agargun";
    public static string ProjectileName   = "ak-47";
    public static string ShortDescription = "One You Can Count On";
    public static string LongDescription  = "(charge 1-10, different effects depending on charge)";
    public static string Lore             = "TBD";

    public static List<string> timingLevelSprites;
    public static List<Projectile> timingProjectiles;
    public PlayerController owner;

    private static int maxCharge = 10;
    private static int spinSpeed = 6;
    private int curCharge = 0;
    private int curSpin = 0;
    private GameObject theCounter = null;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<TimingGun>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);

        gun.gunSwitchGroup                    = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup;
        gun.DefaultModule.shootStyle          = ShootStyle.Automatic;
        gun.DefaultModule.sequenceStyle       = ProjectileSequenceStyle.Random;
        gun.quality                           = ItemQuality.C;
        gun.DefaultModule.ammoCost            = 1;
        gun.SetBaseMaxAmmo(300);
        gun.SetAnimationFPS(gun.shootAnimation, 24);

        gun.reloadTime                        = 2.0f;
        gun.DefaultModule.cooldownTime        = 0.2f;
        gun.DefaultModule.numberOfShotsInClip = 12;

        timingProjectiles = new List<Projectile>();
        timingLevelSprites = new List<string>();

        Projectile projectile       = gun.InitFirstProjectile(new(damage: 0.0f, speed: 5.0f, range: 5.0f));

        // No guns without ammo (base stats)
        Projectile p0 = projectile.Clone(new(damage: 1.0f, speed: 2.0f, range: 2.0f));
        timingProjectiles.Add(p0);

        // TODO: antiquated sprite registration, figure out something better if I ever revamp this gun
        for (int i = 0; i < 10; ++i)
        {
            string istring = i.ToString();
            VFX.RegisterVFX(istring, new List<string>() {
                    "CwaffingTheGungy/Resources/MiscVFX/Numbers/"+istring,
                }, 1, loops: false, anchor: Anchor.LowerCenter, persist: true);
        }

        // 1+ guns without ammo (scale stats from last projectile)
        for(int i = 1; i < maxCharge; ++i)
        {
            Projectile po      = timingProjectiles[i-1];
            Projectile pi      = po.Clone(new(damage: po.baseData.damage * 1.4f, speed: po.baseData.speed * 1.4f, range: po.baseData.range * 1.4f));
            timingProjectiles.Add(pi);
            timingLevelSprites.Add(i.ToString());
        }
    }

    public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
    {
        return timingProjectiles[this.curCharge];
    }

    protected override void Update()
    {
        base.Update();
        if (!this.Player)
            return;

        if (++this.curSpin != spinSpeed)
            return;

        this.curSpin = 0;
        this.curCharge = (this.curCharge+1) % maxCharge;
        if (theCounter != null)
            UnityEngine.Object.Destroy(theCounter);
        // theCounter = Instantiate<GameObject>(
        //                 VFX.animations[curCharge.ToString()],
        //                 this.Player.specRigidbody.sprite.WorldTopCenter + new Vector2(0f,0.5f),
        //                 Quaternion.identity,
        //                 this.Player.specRigidbody.transform);
        theCounter.transform.localScale = new Vector3(0.2f,0.2f,0.2f);
    }
}
