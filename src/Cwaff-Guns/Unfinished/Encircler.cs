namespace CwaffingTheGungy;

public class Encircler : AdvancedGunBehavior
{
    public static string ItemName         = "Encircler";
    public static string SpriteName       = "doublegun";
    public static string ProjectileName   = "ak-47";
    public static string ShortDescription = "Sir Cumference's Own";
    public static string LongDescription  = "(circles)";
    public static string Lore             = "TBD";

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Encircler>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);

        gun.gunSwitchGroup                    = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup;
        gun.DefaultModule.ammoCost            = 1;
        gun.DefaultModule.shootStyle          = ShootStyle.SemiAutomatic;
        gun.DefaultModule.sequenceStyle       = ProjectileSequenceStyle.Random;
        gun.reloadTime                        = 1.1f;
        gun.DefaultModule.cooldownTime        = 0.1f;
        gun.DefaultModule.numberOfShotsInClip = 20;
        gun.quality                           = ItemQuality.D;
        gun.SetBaseMaxAmmo(250);
        gun.SetAnimationFPS(gun.shootAnimation, 24);

        int numProjectiles = 7;

        // Lazy.InitGunFromStrings already handles the first one
        for (int i = 1; i < numProjectiles; i++)
            gun.AddProjectileModuleFrom("ak-47", true, false);

        int iterator = 0;
        //GUN STATS
        foreach (ProjectileModule mod in gun.Volley.projectiles)
        {
            Projectile projectile = mod.projectiles[0].Clone(new(damage: 12.0f, range: 15.0f));
            projectile.hitEffects.alwaysUseMidair = true;
            EncirclerBehavior pop = projectile.gameObject.AddComponent<EncirclerBehavior>();
            pop.angle = iterator*360/numProjectiles;

            mod.ammoCost                          = 1;
            mod.shootStyle                        = ShootStyle.SemiAutomatic;
            mod.sequenceStyle                     = ProjectileSequenceStyle.Random;
            mod.cooldownTime                      = 0.5f;
            mod.angleVariance                     = 11.25f;
            mod.numberOfShotsInClip               = 4;
            mod.angleFromAim                      = iterator*360/numProjectiles;
            mod.projectiles[0]                   = projectile;
            if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
            iterator++;
        }
    }
}

public class EncirclerBehavior : MonoBehaviour
{
    private Projectile m_projectile;
    private PlayerController m_owner;

    public float currentAngle;
    public int   popCurrent   = 0;
    public float launchTimer  = 0.6f;
    public float rotateSpeed  = 60f;
    public float launchSpeed  = 20;
    public float driftSpeed   = 10f;
    public float angularSpeed = 720f;  //in degrees per second

    public float angle;
    public float offsetAngle;
    public float targetAngle;

    private float lifetime = 0;
    private bool runningInCircles = true;

    private void Start()
    {
        this.m_projectile = base.GetComponent<Projectile>();
        if (this.m_projectile.Owner && this.m_projectile.Owner is PlayerController)
        {
            this.m_owner      = this.m_projectile.Owner as PlayerController;
            this.targetAngle  = this.m_owner.CurrentGun.CurrentAngle;
            this.offsetAngle  = this.targetAngle + this.angle;

            this.m_projectile.baseData.speed = 0;
            this.m_projectile.UpdateSpeed();
        }
        Invoke("DoLaunch",launchTimer+0.02f*UnityEngine.Random.Range(0, 30));
    }

    private void Update()
    {
        if (this.runningInCircles)
        {
            this.lifetime += BraveTime.DeltaTime;
            float newspeed = Math.Min(this.rotateSpeed,this.rotateSpeed * this.lifetime);

            // NOTE: SendInDirection doesn't account for vector magnitude, so calculating speed before
            //   calculating vectors leads to some janky, non-circular movement,
            //   but I'm leaving it in because it looks neat :D
            this.m_projectile.baseData.speed = driftSpeed;
            this.m_projectile.UpdateSpeed();

            Vector2 circularComponent =
                BraveMathCollege.DegreesToVector(this.offsetAngle+angularSpeed*(this.lifetime - Mathf.Floor(this.lifetime)),newspeed);
            Vector2 straightComponent =
                BraveMathCollege.DegreesToVector(this.targetAngle,this.driftSpeed);

            this.m_projectile.SendInDirection(circularComponent+straightComponent, true);
        }
    }

    private void DoLaunch()
    {
        this.runningInCircles = false;
        this.m_projectile.baseData.speed = this.launchSpeed;
        this.m_projectile.UpdateSpeed();
        this.m_projectile.SendInDirection(BraveMathCollege.DegreesToVector(this.targetAngle), true);
        AkSoundEngine.PostEvent("Play_WPN_blasphemy_shot_01", this.m_projectile.gameObject);
    }
}
