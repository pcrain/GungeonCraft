namespace CwaffingTheGungy;

public class Gadulo : CwaffGun
{
    public static string ItemName         = "Gadulo";
    public static string ShortDescription = "Authentic Subanese";
    public static string LongDescription  = "Fires shards that have very high base damage, but cannot damage enemies that they don't kill outright. Shards inflict 3 seconds of stun on enemies they don't kill.";
    public static string Lore             = "This weapon's crystals have a propensity for violently exploding when enough are embedded in the same organism. While incredibly effective in theory, inexperienced combatants often struggle getting them to stick to anything other than lightly-defended targets and their own sleeves.";

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
          .InitProjectile(GunData.New(sprite: "needle_rifle_projectile", clipSize: 3, cooldown: 0.45f, shootStyle: ShootStyle.SemiAutomatic, customClip: true,
            damage: 40.0f, speed: 200f, range: 99f, force: 10f, hitWallSound: "needle_rifle_impact_wall_sound", glowAmount: 20f,
            hitEnemySound: "needler_impact_enemy_sound", stunDuration: 3f))
          .Attach<GaduloDamageAdjuster>()
          .Attach<SuperCombiner>()
          .AttachTrail("needle_rifle_trail", fps: 60, glowAmount: 20f, destroyOnEmpty: true, cascadeTimer: 0.5f * C.FRAME, softMaxLength: 1f)
          .SetAllImpactVFX(VFX.CreatePool("needle_rifle_impact_vfx", fps: 60, loops: false, emissivePower: 30f))
          .StickToEnemies<NoKillHealthModificationBuff>(
            glowAmount: 10f, deathVFX: Items.CatClaw.DefaultProjectile().GetComponent<DelayedExplosiveBuff>().explosionData.effect.CreatePoolFromVFXGameObject(),
            setupFunc: b => { b.vfx.AddComponent<CrystalFlicker>(); });

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

    private class GaduloDamageAdjuster : DamageAdjuster
    {
        public override ApplyPriority Priority => ApplyPriority.Bottom; // this can 0 out damage, so give it minimum priority

        protected override float AdjustDamage(float currentDamage, Projectile proj, AIActor enemy)
          => (enemy.healthHaver is HealthHaver hh && hh.currentHealth <= currentDamage) ? currentDamage : 0f;
    }
}

public class NoKillHealthModificationBuff : HealthModificationBuff
{
  public override void AddSelfToTarget(GameObject target)
  {
    if (target.GetComponent<HealthHaver>() is not HealthHaver hh || hh.IsDead)
      return; // don't affect dead enemies
    base.AddSelfToTarget(target);
  }
}

public class CrystalFlicker : MonoBehaviour
{
    private Material _mat;

    private void Start()
    {
      tk2dSprite sprite = base.gameObject.GetComponent<tk2dSprite>();
      if (!sprite)
      {
        UnityEngine.Object.Destroy(this);
        return;
      }
      sprite.usesOverrideMaterial = true;
      this._mat = sprite.renderer.material;
      this._mat.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTiltedCutoutEmissive");
      this._mat.SetColor(CwaffVFX._EmissiveColorId, Color.magenta);
      this._mat.SetFloat(CwaffVFX._EmissiveColorPowerId, 0.65f);
    }

    private void Update()
    {
      this._mat.SetFloat(CwaffVFX._EmissivePowerId, 75f + 50f * Mathf.Sin(60f * BraveTime.ScaledTimeSinceStartup));
    }
}

public class SuperCombiner : MonoBehaviour
{
    private Projectile _proj;

    private void Start()
    {
      this._proj = base.gameObject.GetComponent<Projectile>();
      if (!this._proj || this._proj.m_owner is not PlayerController player || !player.HasSynergy(Synergy.MASTERY_GADULO))
      {
        if (base.gameObject.GetComponent<HealthModificationBuff>() is HealthModificationBuff buff)
          UnityEngine.Object.Destroy(buff); // don't stick to enemies unless mastered
        UnityEngine.Object.Destroy(this);
        return;
      }

      this._proj.OnHitEnemy += SuperCombiner.RegisterSuperCombine;
    }

    private static void RegisterSuperCombine(Projectile projectile, SpeculativeRigidbody rigidbody, bool killed)
    {
      if (!killed)
        rigidbody.gameObject.GetOrAddComponent<SuperCombineDamage>().AddDamage(projectile.baseData.damage);
    }
}

public class SuperCombineDamage : MonoBehaviour
{
  private HealthHaver _hh = null;
  private float _damage = 0f;
  private bool _setup = false;
  private int _count = 0;

  private void Start()
  {
    if (!this._setup && !Setup())
      UnityEngine.Object.Destroy(this);
  }

  private bool Setup()
  {
    this._hh = base.gameObject.GetComponent<HealthHaver>();
    if (!this._hh)
      return false;
    this._setup = true;
    return true;
  }

  public void AddDamage(float damage)
  {
    if (!this._setup && !Setup())
    {
      UnityEngine.Object.Destroy(this);
      return;
    }
    if (!this._hh || this._hh.IsDead)
    {
      UnityEngine.Object.Destroy(this);
      return;
    }
    // Lazy.DebugConsoleLog($"damage increase from {this._damage} to {this._damage + damage} out of {this._hh.currentHealth}");
    this._damage += damage;
    this._count += 1;
    if (this._damage < this._hh.currentHealth)
      return;
    if (this._hh.gameActor is not AIActor enemy)
    {
      UnityEngine.Object.Destroy(this);
      return;
    }
    base.gameObject.Play("supercombine_sound");
    this._hh.ApplyDamage(this._damage, Lazy.RandomVector(), "supercombine", CoreDamageTypes.None, DamageCategory.Unstoppable,
      ignoreInvulnerabilityFrames: true, ignoreDamageCaps: true);
    UnityEngine.Object.Destroy(this);
  }
}
