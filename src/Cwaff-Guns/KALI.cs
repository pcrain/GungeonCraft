namespace CwaffingTheGungy;

public class KALI : AdvancedGunBehavior
{
    public static string ItemName         = "KALI";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static Projectile _KaliProjectile = null;
    internal static string _ChargeBase = null;
    internal static string _ChargeMore = null;
    internal static string _ChargeMost = null;
    internal static List<string> _ChargeAnimations = null;

    internal GameObject _timeShifter = null;

    private int _chargeLevel = -1;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<KALI>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.CHARGE, reloadTime: 0.1f, ammo: 100, audioFrom: Items.Banana /* silent charge */);
            // gun.SetFireAudio("alligator_shoot_sound");
            gun.SetFireAudio("kali_shoot_sound");

        _KaliProjectile = gun.InitProjectile(new(damage: 100f, speed: 700f, range: 9999f, force: 3f, cooldown: 0.1f,
          clipSize: 1, sprite: "kali_projectile", shootStyle: ShootStyle.Charged)
        ).Attach<KaliProjectile>(
        ).Attach<PierceProjModifier>(pierce => { pierce.penetration = 999; pierce.penetratesBreakables = true; }
        // ).Attach<BounceProjModifier>(bounce => { bounce.numberOfBounces = Mathf.Max(bounce.numberOfBounces, 99); }
        );
            TrailController tc = _KaliProjectile.AddTrailToProjectilePrefab(ResMap.Get("kali_trail")[0], new Vector2(15, 6), new Vector2(7, 3),
                ResMap.Get("kali_trail"), 60, cascadeTimer: C.FRAME, softMaxLength: 1f, destroyOnEmpty: false);
                tc.UsesDispersalParticles = true;
                tc.DispersalParticleSystemPrefab = (ItemHelper.Get(Items.FlashRay) as Gun).DefaultModule.projectiles[0].GetComponentInChildren<TrailController>().DispersalParticleSystemPrefab;
            // p.SetAllImpactVFX(VFX.CreatePool("gaster_beam_impact", fps: 20, loops: false, scale: 1.0f, anchor: Anchor.MiddleCenter));

            // TrailController tc = (ItemHelper.Get(Items.Railgun) as Gun)
            //     .DefaultModule
            //     .chargeProjectiles[1]
            //     .Projectile
            //     .GetComponentInChildren<TrailController>()
            //     .gameObject
            //     .ClonePrefab()
            //     .GetComponent<TrailController>();
            // p.AddTrailToProjectilePrefab(tc);

        ProjectileModule mod = gun.DefaultModule;
        mod.projectiles.Clear();
        mod.chargeProjectiles = new List<ProjectileModule.ChargeProjectile>(){
            new(){
                Projectile = _KaliProjectile.Clone(),
                ChargeTime = 1f,
            },
            new(){
                Projectile = _KaliProjectile.Clone(),
                ChargeTime = 2f,
            },
            new(){
                Projectile = _KaliProjectile.Clone(),
                ChargeTime = 3f,
            },
        };

        _ChargeBase = gun.UpdateAnimation("charge", returnToIdle: false);
            gun.SetAnimationFPS(_ChargeBase, 20);
            // gun.SetGunAudio(_ChargeBase, "kali_charge_sound", 0);
        _ChargeMore = gun.UpdateAnimation("charge_more", returnToIdle: false);
            gun.SetAnimationFPS(_ChargeMore, 40);
            // gun.SetGunAudio(_ChargeMore, "kali_charge_sound", 0);
        _ChargeMost = gun.UpdateAnimation("charge_most", returnToIdle: false);
            gun.SetAnimationFPS(_ChargeMost, 60);
            gun.SetGunAudio(_ChargeMost, "kali_charge_sound", 0);
        _ChargeAnimations = new(){
            _ChargeBase,
            _ChargeMore,
            _ChargeMost,
        };
    }

    protected override void Update()
    {
        base.Update();
        if (BraveTime.DeltaTime == 0.0f)
            return;
        if (this.Owner is not PlayerController)
            return;

        if (!this.gun.IsCharging)
        {
            this._chargeLevel = -1;
            this.gun.chargeAnimation = _ChargeAnimations[0];
            this.gun.sprite.usesOverrideMaterial = false;
            this.gun.sprite.renderer.material.SetFloat("_EmissivePower", 0f);
            this.gun.sprite.UpdateMaterial();
            return;
        }

        int newChargeLevel = 1 + this.gun.GetChargeLevel();
        if (newChargeLevel == this._chargeLevel)
            return;

        this._chargeLevel = newChargeLevel;
        this.gun.chargeAnimation = _ChargeAnimations[Math.Max(newChargeLevel - 1, 0)];
        this.gun.spriteAnimator.currentClip = this.gun.spriteAnimator.GetClipByName(this.gun.chargeAnimation);
        if (newChargeLevel == 0)
        {
            AkSoundEngine.PostEvent("kali_activate_sound", base.gameObject);
            this.gun.spriteAnimator.Play();
        }
        else if (newChargeLevel < 3)
        {
            AkSoundEngine.PostEvent("kali_charge_sound", base.gameObject);
            this.gun.spriteAnimator.Play();
        }
        else
            this.gun.spriteAnimator.PlayFromFrame(0); // level 3 sound handled automatically by the animation
        this.gun.sprite.gameObject.SetGlowiness(25f + 25f * newChargeLevel);
    }

    protected override void OnPickedUpByPlayer(PlayerController player)
    {
        base.OnPickedUpByPlayer(player);
        this._timeShifter.SafeDestroy();
        this._timeShifter = new GameObject();
        this._timeShifter.AddComponent<KaliTimeshifter>();
        this._timeShifter.transform.parent = player.transform;
        this.gun.sprite.gameObject.SetGlowiness(0f);
    }

    protected override void OnPostDroppedByPlayer(PlayerController player)
    {
        base.OnPostDroppedByPlayer(player);
        this._timeShifter.SafeDestroy();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (projectile.GetComponent<KaliProjectile>() is not KaliProjectile kp)
            return;

        projectile.Attach<SpawnProjModifier>(s => {
          s.spawnProjectilesOnCollision  = true;
          s.numberToSpawnOnCollison      = 9;
          s.startAngle                   = 180;
          s.projectileToSpawnOnCollision = KALI._KaliProjectile;
          s.collisionSpawnStyle          = SpawnProjModifier.CollisionSpawnStyle.RADIAL;
        });

        this._timeShifter.GetComponent<KaliTimeshifter>().Reset();
        projectile.transform.DoMovingDistortionWave(distortionIntensity: 1.5f, distortionRadius: 0.5f, maxRadius: 0.75f, duration: 0.25f);
        // projectile.transform.DoMovingDistortionWave(distortionIntensity: 1.5f, distortionRadius: 0.05f, maxRadius: 0.75f, duration: 0.25f);
        base.gameObject.Play("magunet_launch_sound");
    }
}

public class KaliProjectile : MonoBehaviour
{
    private void Start()
    {
        base.GetComponentInChildren<TrailController>().gameObject.SetGlowiness(100f);
    }
}

public class KaliTimeshifter : MonoBehaviour
{
    private const float _BASE_TIME_SCALE = 0.1f;
    private const float _TIME_SCALE_FACTOR = 2.0f;

    private float _timeScale = 1.0f;

    private void Start()
    {
        this._timeScale = 1.0f;
    }

    private void Update()
    {
        this._timeScale = Mathf.Min(this._timeScale + _TIME_SCALE_FACTOR * BraveTime.DeltaTime, 1.0f);
        if (this._timeScale >= 1f)
        {
            BraveTime.ClearMultiplier(base.gameObject);
            return;
        }

        BraveTime.SetTimeScaleMultiplier(this._timeScale, base.gameObject);
    }

    public void Reset()
    {
        this._timeScale = _BASE_TIME_SCALE;
    }

    private void OnDestroy()
    {
        BraveTime.ClearMultiplier(base.gameObject);
    }
}
