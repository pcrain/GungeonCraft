namespace CwaffingTheGungy;

public class BBGun : AdvancedGunBehavior
{
    public static string ItemName         = "B. B. Gun";
    public static string SpriteName       = "b_b_gun";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Spare No One";
    public static string LongDescription  = "Fires a single large projectile that bounces off walls and knocks enemies around with extreme force. Ammo can only be regained by interacting with the projectiles once they have come to a halt.\n\nThis gun was originally used in the mid-18th century for hunting turkeys, as they were the only birds slow enough to actually hit with any degree of reliability. While hunters quickly decided that using a large, slow, rolling projectile wasn't ideal for hunting, the gun's legacy lives on today in shooting arenas known as \"alleys\", where sporting enthusiasts roll similar projectiles against red and white wooden objects in hopes of scoring a \"turkey\" themselves.";

    private static readonly float[] _CHARGE_LEVELS  = {0.25f,0.5f,1.0f,2.0f};
    private static Projectile       _FakeProjectile = null;
    private float                   _lastCharge     = 0.0f;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<BBGun>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.CHARGE, reloadTime: 0.5f, ammo: 3, canGainAmmo: false);
            gun.CanGainAmmo                          = false;
            gun.muzzleFlashEffects                   = (ItemHelper.Get(Items.SeriousCannon) as Gun).muzzleFlashEffects;
            gun.SetAnimationFPS(gun.shootAnimation, 10);
            gun.SetAnimationFPS(gun.chargeAnimation, 16);
            gun.LoopAnimation(gun.chargeAnimation, 32);
            gun.SetMuzzleVFX("muzzle_b_b_gun", fps: 30, scale: 0.5f, anchor: Anchor.MiddleCenter);
            gun.SetFireAudio("Play_WPN_seriouscannon_shot_01");
            gun.SetReloadAudio("Play_ENM_flame_veil_01");

        ProjectileModule mod = gun.DefaultModule;
            mod.ammoCost            = 1;
            mod.numberOfShotsInClip = 3;
            mod.shootStyle          = ShootStyle.Charged;
            mod.sequenceStyle       = ProjectileSequenceStyle.Ordered;
            mod.cooldownTime        = 0.70f;
            mod.angleVariance       = 10f;
            mod.SetupCustomAmmoClip(SpriteName);

        Projectile projectile = mod.projectiles[0].ClonePrefab();
            projectile.baseData.range = 999999f;
            projectile.baseData.speed = 20f;
            projectile.AddDefaultAnimation(AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("bball").Base(),
                20, true, new IntVector2(24, 22),
                false, Anchor.MiddleCenter,
                anchorsChangeColliders: false,
                fixesScales: true,
                overrideColliderPixelSize: new IntVector2(2, 2))); // prevent uneven colliders from glitching into walls
            projectile.gameObject.AddComponent<TheBB>();

        mod.chargeProjectiles = new();
        for (int i = 0; i < _CHARGE_LEVELS.Length; i++)
        {
            Projectile p = projectile.ClonePrefab();
            p.gameObject.GetComponent<TheBB>().chargeLevel = i+1;
            mod.chargeProjectiles.Add(new ProjectileModule.ChargeProjectile {
                Projectile = p,
                ChargeTime = _CHARGE_LEVELS[i],
            });
        }

        _FakeProjectile = Lazy.PrefabProjectileFromGun(gun);
        _FakeProjectile.gameObject.AddComponent<FakeProjectileComponent>(); // disable VFX like robot's lightning (TODO: this doesn't work here; need to apply to each projectile)
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        projectile.baseData.speed = 100 * this._lastCharge;
        base.PostProcessProjectile(projectile);
    }

    protected override void Update()
    {
        base.Update();
        if (!this.Player)
            return;
        if (this.gun.IsCharging)
            this._lastCharge = this.gun.GetChargeFraction();
    }
}

public class TheBB : MonoBehaviour
{
    private const float _BB_DAMAGE_SCALE    = 2.0f;
    private const float _BB_FORCE_SCALE     = 2.0f;
    private const float _BB_SPEED_DECAY     = 3.0f;
    private const float _BASE_EMISSION      = 3.0f;
    private const float _EXTRA_EMISSION     = 30.0f;
    private const float _BASE_ANIM_SPEED    = 2.0f;
    private const float _BOUNCE_SPEED_DECAY = 0.9f;

    public int chargeLevel = 0;

    private Projectile _projectile;
    private PlayerController _owner;
    private float _lifetime = 0f;
    private float _maxSpeed = 0f;
    private float _lastBounceTime = 0f;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        if (this._projectile.Owner is PlayerController pc)
            this._owner = pc;

        this._projectile.collidesWithPlayer = true;
        // this._projectile.DestroyMode = Projectile.ProjectileDestroyMode.DestroyComponent;
        this._projectile.OnDestruction += CreateInteractible;
        this._maxSpeed = this._projectile.baseData.speed;

        this._projectile.sprite.usesOverrideMaterial = true;
        Material m = this._projectile.sprite.renderer.material;
            m.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
            m.SetFloat("_EmissivePower", 1000f);
            m.SetFloat("_EmissiveColorPower", 1.55f);
            m.SetColor("_EmissiveColor", Color.magenta);

        BounceProjModifier bounce = this._projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            bounce.numberOfBounces     = Mathf.Max(bounce.numberOfBounces, 999);
            bounce.chanceToDieOnBounce = 0f;
            bounce.onlyBounceOffTiles  = true;
            bounce.OnBounce += OnBounce;

        PierceProjModifier pierce = this._projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            pierce.penetration = Mathf.Max(pierce.penetration, 999);
            pierce.penetratesBreakables = true;
    }

    private void OnBounce()
    {
        this._projectile.baseData.speed *= _BOUNCE_SPEED_DECAY;
    }

    private void CreateInteractible(Projectile p)
    {
        MiniInteractable mi = MiniInteractable.CreateInteractableAtPosition(
          this._projectile.sprite,
          this._projectile.sprite.WorldCenter,
          BBInteractScript);
            mi.doHover = true;
            mi.sprite.usesOverrideMaterial = true;
            Material mat = mi.sprite.renderer.material;
                mat.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
                mat.SetFloat("_EmissivePower", _BASE_EMISSION);
                mat.SetFloat("_EmissiveColorPower", 1.55f);
                mat.SetColor("_EmissiveColor", Color.magenta);
        // UnityEngine.Object.Destroy(p.gameObject);
    }

    private void Update()
    {
        float deltatime = BraveTime.DeltaTime;
        this._lifetime += deltatime;
        this._projectile.UpdateSpeed();
        float newSpeed = Mathf.Max(this._projectile.baseData.speed-_BB_SPEED_DECAY*deltatime,0.0001f);
        this._projectile.baseData.speed = newSpeed;
        this._projectile.UpdateSpeed();

        this._projectile.sprite.renderer.material.SetFloat(
            "_EmissivePower", _BASE_EMISSION+_EXTRA_EMISSION*(newSpeed/_maxSpeed));
        this._projectile.sprite.renderer.material.SetFloat(
            "_Cutoff", 0.1f);

        if (newSpeed > 1)
        {
            this._projectile.baseData.damage = _BB_DAMAGE_SCALE * newSpeed;
            this._projectile.baseData.force = _BB_FORCE_SCALE * newSpeed;
            this._projectile.spriteAnimator.ClipFps = Mathf.Min(_BASE_ANIM_SPEED*newSpeed, 60f);
            Lazy.PlaySoundUntilDeathOrTimeout("bb_rolling", this._projectile.gameObject, 0.1f);
            return;
        }

        // CreateInteractible(this);
        this._projectile.DieInAir(suppressInAirEffects: true);
        return;
    }

    public static IEnumerator BBInteractScript(MiniInteractable i, PlayerController p)
    {
        foreach (Gun gun in p.inventory.AllGuns)
        {
            if (!gun.GetComponent<BBGun>())
                continue;
            if (gun.CurrentAmmo >= gun.AdjustedMaxAmmo)
                break;
            gun.CurrentAmmo += 1;
            gun.ForceImmediateReload();
            Lazy.DoPickupAt(i.sprite.WorldCenter);
            UnityEngine.Object.Destroy(i.gameObject);
            break;
        }
        i.interacting = false;
        yield break;
    }
}
