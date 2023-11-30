namespace CwaffingTheGungy;

public class Telefragger : AdvancedGunBehavior
{
    public static string ItemName         = "Telefragger";
    public static string SpriteName       = "multiplicator";
    public static string ProjectileName   = "ak-47";
    public static string ShortDescription = "Voip";
    public static string LongDescription  = "(teleports to projectile upon wall collision and creates a blank)";
    public static string Lore             = "TBD";

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Telefragger>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);

        gun.gunSwitchGroup                    = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup;
        gun.DefaultModule.ammoCost            = 1;
        gun.DefaultModule.shootStyle          = ShootStyle.Automatic;
        gun.DefaultModule.sequenceStyle       = ProjectileSequenceStyle.Random;
        gun.reloadTime                        = 1.1f;
        gun.DefaultModule.angleVariance       = 15.0f;
        gun.DefaultModule.cooldownTime        = 0.7f;
        gun.DefaultModule.numberOfShotsInClip = 1000;
        gun.quality                           = ItemQuality.D;
        gun.SetBaseMaxAmmo(1000);
        gun.SetAnimationFPS(gun.shootAnimation, 24);

        Projectile projectile       = gun.InitFirstProjectile(new(damage: 20.0f, speed: 30.0f));

        projectile.gameObject.AddComponent<HeadCannonBullets>();
    }

}

public class HeadCannonBullets : MonoBehaviour
{
    private Projectile m_projectile;
    private PlayerController m_owner;

    private static VFXPool vfx = null;

    private void Start()
    {
        vfx ??= VFX.CreatePoolFromVFXGameObject((ItemHelper.Get(Items.MagicLamp) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);

        this.m_projectile = base.GetComponent<Projectile>();
        if (this.m_projectile?.Owner is PlayerController)
            this.m_owner = this.m_projectile.Owner as PlayerController;

        SpeculativeRigidbody specRigidBody = this.m_projectile.specRigidbody;
        // this.m_projectile.BulletScriptSettings.surviveTileCollisions = true;
        specRigidBody.OnCollision += this.OnCollision;
    }

    private void OnCollision(CollisionData tileCollision)
    {

        if (tileCollision?.OtherRigidbody?.gameObject?.GetComponent<AIActor>() != null)
            return; //ignore collisions with enemies, we only care about walls

        PhysicsEngine.PostSliceVelocity     = new Vector2?(default(Vector2));
        SpeculativeRigidbody specRigidbody  = this.m_projectile.specRigidbody;
        specRigidbody.OnCollision          -= this.OnCollision;

        this.TeleportPlayerToPosition(this.m_owner, tileCollision.PostCollisionUnitCenter);
        this.m_projectile.DieInAir();
    }

    private void TeleportPlayerToPosition(PlayerController player, Vector2 position)
    {
        Vector2 startPosition = player.transform.position;
        Lazy.MovePlayerTowardsPositionUntilHittingWall(player, position);
        Vector2 endPosition = player.transform.position;
        player.StartCoroutine(DoPlayerTeleport(player, startPosition, endPosition, 0.125f));
    }

    private IEnumerator DoPlayerTeleport(PlayerController player, Vector2 start, Vector2 end, float duration)
    {
        float timer = 0;

        GameManager.Instance.MainCameraController.SetManualControl(true, true);
        Vector2 startCamPos = GameManager.Instance.MainCameraController.GetIdealCameraPosition();
        Vector2 endCamPos   = end;
        Vector2 deltaPos    = (endCamPos - startCamPos);

        player.transform.position = start;
        player.specRigidbody.Reinitialize();
        player.sprite.renderer.enabled = false;
        player.specRigidbody.enabled = false;

        AkSoundEngine.PostEvent("Play_OBJ_chestwarp_use_01", player.gameObject);
        vfx.SpawnAtPosition(start.ToVector3ZisY(-1f), 0, null, null, null, -0.05f);

        while (timer < duration)
        {
            timer += BraveTime.DeltaTime;
            Vector2 interPos = startCamPos + (timer/duration)*deltaPos;
            GameManager.Instance.MainCameraController.OverridePosition = interPos;
            yield return null;
        }

        AkSoundEngine.PostEvent("Play_OBJ_chestwarp_use_01", player.gameObject);
        vfx.SpawnAtPosition(end.ToVector3ZisY(-1f), 0, null, null, null, -0.05f);
        this.m_owner.DoEasyBlank(end, EasyBlankType.MINI);

        GameManager.Instance.MainCameraController.SetManualControl(false, true);
        player.transform.position = end;
        player.sprite.renderer.enabled = true;
        player.specRigidbody.enabled = true;
        player.specRigidbody.Reinitialize();

        yield break;
    }
}
