namespace CwaffingTheGungy;

public class PopcornGun : AdvancedGunBehavior
{
    public static string ItemName         = "Popcorn Gun";
    public static string SpriteName       = "arcpistol";
    public static string ProjectileName   = "ak-47";
    public static string ShortDescription = "The Weasel";
    public static string LongDescription  = "(split split split again)";

    public static Projectile gunprojectile;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<PopcornGun>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);

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

        Projectile projectile       = Lazy.PrefabProjectileFromGun(gun);
        projectile.baseData.damage  = 5f;
        projectile.baseData.speed   = 20.0f;
        projectile.transform.parent = gun.barrelOffset;

        PopcornBehavior pop = projectile.gameObject.AddComponent<PopcornBehavior>();

        gunprojectile = projectile;
    }
}

public class PopcornBehavior : MonoBehaviour
{
    private Projectile m_projectile;
    private PlayerController m_owner;
    private float m_angle;

    public float currentAngle;
    public int   popCurrent   = 0;
    public int   popMax       = 4;
    public float popTimer     = 0.2f;
    public float popAngleMin  = 20;
    public float popAngleMax  = 60;
    public float speed        = 20;
    public float speedFalloff = 0.8f;

    private void Start()
    {
        this.m_projectile = base.GetComponent<Projectile>();
        if (this.m_projectile.Owner && this.m_projectile.Owner is PlayerController)
        {
            this.m_owner      = this.m_projectile.Owner as PlayerController;
            this.m_angle      = this.m_owner.CurrentGun.CurrentAngle;
            currentAngle      = this.m_angle;
            this.m_projectile.baseData.speed = this.speed;
            this.m_projectile.UpdateSpeed();
        }
        Invoke("DoPop",popTimer);
    }
    private void DoPop()
    {
        if (this.popCurrent < this.popMax)
        {
            ++this.popCurrent;
            float otherAngle = currentAngle - UnityEngine.Random.Range(this.popAngleMin,this.popAngleMax);
            currentAngle     = currentAngle + UnityEngine.Random.Range(this.popAngleMin,this.popAngleMax);
            this.m_projectile.SendInDirection(BraveMathCollege.DegreesToVector(currentAngle), true);

            Projectile other = SpawnManager.SpawnProjectile(
                PopcornGun.gunprojectile.gameObject,
                this.m_projectile.sprite.WorldCenter,
                Quaternion.Euler(0f, 0f, otherAngle),
                true).GetComponent<Projectile>();
            if (!other)
                return;

            other.Owner = this.m_owner;
            other.Shooter = this.m_owner.specRigidbody;
            this.m_owner.DoPostProcessProjectile(other);

            PopcornBehavior otherPop         = other.GetComponent<PopcornBehavior>();
            otherPop.currentAngle            = otherAngle;
            otherPop.popCurrent              = this.popCurrent;
            otherPop.speedFalloff            = this.speedFalloff;
            float newspeed                   = this.speed*this.speedFalloff;
            this.speed                       = newspeed;
            otherPop.speed                   = newspeed;
            this.m_projectile.baseData.speed = newspeed;
            other.baseData.speed             = newspeed;
            this.m_projectile.UpdateSpeed();
            other.UpdateSpeed();

            AkSoundEngine.PostEvent("Play_WPN_smileyrevolver_shot_01", this.m_projectile.gameObject);
            Invoke("DoPop",popTimer);
        }
    }
}
