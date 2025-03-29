
namespace CwaffingTheGungy;

public class Empath : CwaffGun
{
    public static string ItemName         = "Empath";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject _PsychicVFX = null;

    private bool _setupAnimator = false;

    public static void Init()
    {
        Lazy.SetupGun<Empath>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.PISTOL, reloadTime: 2.1f, ammo: 480, shootFps: 14, reloadFps: 1,
            fireAudio: "empath_fire_sound", smoothReload: 0.0f, muzzleVFX: "muzzle_empath", muzzleFps: 30, muzzleAnchor: Anchor.MiddleCenter)
          .SetReloadAudio("empath_reload_sound_2", 7)
          .InitProjectile(GunData.New(sprite: "empath_projectile", clipSize: 15, cooldown: 0.13f, shootStyle: ShootStyle.SemiAutomatic, fps: 11,
            damage: 15.0f, speed: 18f, range: 999f, force: 12f, collidesWithProjectiles: true, bossDamageMult: 0.6f, scale: 2.0f, customClip: true))
          .SetAllImpactVFX(VFX.CreatePool("empath_impact_vfx", fps: 30, loops: false));

        _PsychicVFX = VFX.Create("empath_psychic_damage_vfx", fps: 30, loops: false);
    }

    public override void Update()
    {
        base.Update();
        if (!this._setupAnimator)
        {
          base.gameObject.GetComponent<tk2dSpriteAnimator>().AnimationEventTriggered += this.AnimationEventTriggered;
          this._setupAnimator = true;
        }
    }

    private void AnimationEventTriggered(tk2dSpriteAnimator animator, tk2dSpriteAnimationClip clip, int frame)
    {
        if (frame != 7 || !this.PlayerOwner || clip.name != "empath_reload")
            return;
        Vector2 pos = this.gun.sprite.WorldCenter + (base.transform.rotation * (new Vector2(-6f/16f, -2f/16f))).XY();
        CwaffVFX.Spawn(_PsychicVFX, position: pos, lifetime: 0.5f, startScale: 0.5f, endScale: 3.0f, fadeOutTime: 0.4f);
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
        projectile.specRigidbody.OnRigidbodyCollision += this.OnCollision;
    }

    private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (!otherRigidbody || otherRigidbody.gameObject.GetComponent<Projectile>() is not Projectile p || !p.isActiveAndEnabled)
            PhysicsEngine.SkipCollision = true;
    }

    private void OnCollision(CollisionData collision)
    {
        if (!collision.OtherRigidbody || collision.OtherRigidbody.gameObject is not GameObject otherObject)
            return;
        if (otherObject.GetComponent<Projectile>() is not Projectile other)
            return;

        Projectile p = collision.MyRigidbody.gameObject.GetComponent<Projectile>();
        if (other.Owner is not AIActor enemy || !enemy.specRigidbody || !enemy.healthHaver || !enemy.healthHaver.IsAlive)
        {
            p.DieInAir();
            other.DieInAir();
            return;
        }

        Vector2 enemyPos = enemy.specRigidbody.UnitCenter;
        enemy.gameObject.Play("empath_collide_sound");
        CwaffVFX.SpawnBurst(prefab: _PsychicVFX, numToSpawn: 4, basePosition: enemyPos, positionVariance: 1f,
          minVelocity: 2f, uniform: true, velType: CwaffVFX.Vel.AwayRadial, lifetime: 0.4f);
        p.HandleHitEffectsMidair();  // play hit effects at position of projectile before moving
        p.specRigidbody.Position = new Position(enemyPos);
        p.specRigidbody.UpdateColliderPositions();
        LinearCastResult lcr = LinearCastResult.Pool.Allocate();
        p.ForceCollision(enemy.specRigidbody, lcr);
        LinearCastResult.Pool.Free(ref lcr);
        other.DieInAir();
    }
}
