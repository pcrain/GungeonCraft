namespace CwaffingTheGungy;

public class Gadulo : CwaffGun
{
    public static string ItemName         = "Gadulo";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "Fires shards that have very high base damage, but cannot damage enemies that they don't kill outright. Shards inflict 3 seconds of stun on enemies they don't kill.";
    public static string Lore             = "TBD";

    internal static readonly List<string> _IdleAnimations = new(4);
    internal static readonly List<string> _FireAnimations = new(4);

    private const int _SHOOT_FPS = 60;

    public static void Init()
    {
        Lazy.SetupGun<Gadulo>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 1.25f, ammo: 90, smoothReload: 0.1f,
            fireAudio: "needle_rifle_fire_sound")
          .SetReloadAudio("needle_rifle_reload_hatch_sound")
          .SetReloadAudio("needle_rifle_reload_plant_sound", 5)
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(sprite: "needle_rifle_projectile", clipSize: 3, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic, customClip: true,
            damage: 40.0f, speed: 200f, range: 99f, force: 10f, hitWallSound: "needle_rifle_impact_wall_sound", glowAmount: 20f,
            hitEnemySound: "needler_impact_enemy_sound", stunChance: 1f, stunDuration: 3f))
          .Attach<AllOrNothingDamage>()
          .AttachTrail("needle_rifle_trail", fps: 60, glowAmount: 20f, destroyOnEmpty: true, cascadeTimer: 0.5f * C.FRAME, softMaxLength: 1f)
          .SetAllImpactVFX(VFX.CreatePool("needle_rifle_impact_vfx", fps: 60, loops: false, emissivePower: 30f));

        //REFACTOR: make this part of GunBuilder
        for (int i = 0; i < 4; ++i)
        {
            _IdleAnimations.Add(gun.QuickUpdateGunAnimation($"idle_{i}clip"));
            _FireAnimations.Add(gun.QuickUpdateGunAnimation(i < 3 ? $"fire_{i}clip" : "fire", fps: _SHOOT_FPS, returnToIdle: true));
            gun.SetGunAudio(_FireAnimations[i], "needle_rifle_fire_sound");
        }
        gun.idleAnimation = _IdleAnimations[3];
        gun.shootAnimation = _FireAnimations[3];
    }

    //REFACTOR: make this part of GunBuilder
    private void UpdateAnimations()
    {
        if (this.gun.IsReloading)
        {
            this.gun.idleAnimation = _IdleAnimations[_IdleAnimations.Count - 1];
            this.gun.shootAnimation = _FireAnimations[_FireAnimations.Count - 1];
        }
        else
        {
            this.gun.idleAnimation = _IdleAnimations[Mathf.Clamp(this.gun.ClipShotsRemaining, 0, _IdleAnimations.Count - 1)];
            this.gun.shootAnimation = _FireAnimations[Mathf.Clamp(this.gun.ClipShotsRemaining - 1, 0, _FireAnimations.Count - 1)];
        }
        this.gun.spriteAnimator.defaultClipId = this.gun.spriteAnimator.GetClipIdByName(this.gun.idleAnimation);
    }

    public override void Update()
    {
        base.Update();
        if (PlayerOwner && PlayerOwner.AcceptingNonMotionInput)
            UpdateAnimations();
    }

    private class AllOrNothingDamage : DamageAdjuster
    {
        protected override float AdjustDamage(float currentDamage, Projectile proj, AIActor enemy)
          => (enemy.healthHaver is not HealthHaver hh || hh.currentHealth > currentDamage) ? 0f : currentDamage;
    }
}
