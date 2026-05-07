namespace CwaffingTheGungy;

public class RLTSRTSGTSBTSLB : CwaffGun
{
    public static string ItemName         = "RLTSRTSGTSBTSLB";
    public static string ShortDescription = "Surprisingly Underwhelming";
    public static string LongDescription  = "Fires a rocket that shoots guns that shoot bullets that shoot laser beams. Cannot move or dodge roll while reloading.";
    public static string Lore             = "This weapon's entire production budget was spent on ammo development. No funds remained for hiring a PR team to write a description.";

    public static void Init()
    {
        Lazy.SetupGun<RLTSRTSGTSBTSLB>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.D, gunClass: GunClass.SILLY, reloadTime: 2.75f, ammo: 6, shootFps: 30, smoothReload: 0.1f,
            fireAudio: "rocket_fire_sound", percentSpeedWhileReloading: 0.0f, carryOffset: new IntVector2(4, 0))
          .SetReloadAudio("rocket_plonk_sound", 17)
          .SetReloadAudio("rocket_wiggle_sound", 24, 26, 28, 30, 32, 34)
          .SetMuzzleVFX(Items.TheExotic, onlyCopyBasicEffects: false)
          .AddToShop(ItemBuilder.ShopType.Trorc)
          .InitProjectile(GunData.New(baseProjectile: Items.TheExotic.Projectile(), shootStyle: ShootStyle.SemiAutomatic, clipSize: 2,
            cooldown: 1.0f, recoil: 50f, customClip: true))
          .Assign(out Projectile pRocket);
        Projectile pLaser         = Items.FlashRay.Projectile().Clone();
          pLaser.gameObject.AddComponent<MasteryProcessor>();
        Projectile pBullet        = Items.Bullet.Projectile().gameObject.GetComponent<SpawnProjModifier>().projectileToSpawnInFlight.Clone();
        Projectile pGun           = Items.Bullet.Projectile().Clone();
        SpawnProjModifier sBullet = pBullet.AddComponent<SpawnProjModifier>();
          sBullet.spawnProjectilesOnCollision  = true;
          sBullet.projectileToSpawnOnCollision = pLaser;
          sBullet.numberToSpawnOnCollison      = 2;
          sBullet.spawnOnObjectCollisions      = true;
          sBullet.alignToSurfaceNormal         = true;
          sBullet.collisionSpawnStyle          = SpawnProjModifier.CollisionSpawnStyle.RADIAL;
        pGun.GetComponent<SpawnProjModifier>().projectileToSpawnInFlight = pBullet;
        pRocket.GetComponent<SpawnProjModifier>().projectileToSpawnOnCollision = pGun;
    }

    private class MasteryProcessor : MonoBehaviour
    {
      private void Start()
      {
        if (base.gameObject.GetComponent<Projectile>() is not Projectile proj)
          return;
        if (proj.Owner is not PlayerController player || !player.HasSynergy(Synergy.MASTERY_RLTSRTSGTSBTSLB))
          return;
        if (proj.gameObject.GetComponent<ExplosiveModifier>() is ExplosiveModifier e && e.doExplosion)
          return;
        e = proj.gameObject.GetOrAddComponent<ExplosiveModifier>();
        e.doExplosion = true;
        e.IgnoreQueues = true;
        e.explosionData = Explosions.DefaultSmall;
      }
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        if (this.PlayerOwner is not PlayerController player)
          return;
        player.forceAimPoint = null;
        player.ToggleRenderer(true, ItemName);
        player.ToggleHandRenderers(true, ItemName);
    }

    public override void Update()
    {
        base.Update();
        if (this.PlayerOwner is not PlayerController player)
          return;
        if (this.gun.spriteAnimator is not tk2dSpriteAnimator anim)
          return;
        if (anim.currentClip is not tk2dSpriteAnimationClip clip)
          return;
        bool playingReload = clip.name == this.gun.reloadAnimation;
        this.gun.preventRotation = playingReload;
        this.gun.sprite.HeightOffGround = 0.4f; // vanilla depth when preventRotation is true
        if (!playingReload || player.IsDodgeRolling)
        {
          player.forceAimPoint = null;
          player.ToggleRenderer(true, ItemName);
          player.ToggleHandRenderers(true, ItemName);
          this.gun.sprite.UpdateZDepth();
          return;
        }
        player.forceAimPoint = player.CenterPosition + Vector2.up;
        if (anim.CurrentFrame >= 43)
        {
          player.ToggleRenderer(true, ItemName);
          player.ToggleHandRenderers(true, ItemName);
        }
        else if (anim.CurrentFrame >= 14)
        {
          player.ToggleRenderer(false, ItemName);
          player.ToggleHandRenderers(false, ItemName);
        }
        this.gun.sprite.HeightOffGround = -0.075f; // vanilla back-facing depth when preventRotation is false
        this.gun.sprite.UpdateZDepth();
    }
}
