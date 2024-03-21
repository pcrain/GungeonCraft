namespace CwaffingTheGungy;

public class KALI : AdvancedGunBehavior
{
    public static string ItemName         = "K.A.L.I.";
    public static string ShortDescription = "Fission";
    public static string LongDescription  = "Fires insanely high-velocity piercing particles with high recoil. Projectile damage, velocity, and recoil are doubled at level 2 charge and again at level 3. Destroys enemy projectiles at level 3 charge. Continuously drains ammo while charging. Killed enemies are obliterated and do not drop casings or pickups. ";
    public static string Lore             = "Developed by the nation of Gundia's Department of the Best Defense is a Good Offense, this high-powered handheld electron accelerator can instantaneously ionize most targets and anything in direct contact with them. Various ongoing attempts by the government to market the device to the nation's citizens as a children's toy, a water purifier, and a flashlight have all been criticized by major media outlets as 'irresponsible and asinine', although surveys suggest high market interest from the age 12-17 demographic.";

    private const float _AMMO_DRAIN_RATE = 1.0f;

    internal static GameObject _IonizeVFX = null;
    internal static Projectile _KaliProjectile = null;
    internal static List<string> _ChargeAnimations = null;

    internal GameObject _timeShifter = null;

    private int _chargeLevel = -1;
    private float _timeCharging = 0f;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<KALI>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.CHARGE, reloadTime: 0.1f, ammo: 200, fireAudio: "kali_shoot_sound");

        _KaliProjectile = gun.InitProjectile(GunData.New(range: 9999f, force: 3f, cooldown: 0.1f, collidesWithProjectiles: true,
          clipSize: 1, sprite: "kali_projectile", shootStyle: ShootStyle.Charged)
        ).Attach<PierceProjModifier>(pierce => { pierce.penetration = 999; pierce.penetratesBreakables = true; }
        // ).Attach<BounceProjModifier>(bounce => { bounce.numberOfBounces = Mathf.Max(bounce.numberOfBounces, 99); }
        );
            TrailController tc = _KaliProjectile.AddTrailToProjectilePrefab(ResMap.Get("kali_trail")[0], new Vector2(15, 6), new Vector2(7, 3),
                ResMap.Get("kali_trail"), 60, cascadeTimer: C.FRAME, softMaxLength: 1f, destroyOnEmpty: false);
                tc.UsesDispersalParticles = true;
                tc.DispersalParticleSystemPrefab = (ItemHelper.Get(Items.FlashRay) as Gun).DefaultModule.projectiles[0].GetComponentInChildren<TrailController>().DispersalParticleSystemPrefab;
                tc.gameObject.AddComponent<TrailControllerHotfix.Fix>(); //NOTE: high speed projectiles don't always collide with walls cleanly in vanilla, so patch that
            // p.SetAllImpactVFX(VFX.CreatePool("gaster_beam_impact", fps: 20, loops: false, scale: 1.0f, anchor: Anchor.MiddleCenter));

        ProjectileModule mod = gun.DefaultModule;
        mod.projectiles.Clear();
        mod.chargeProjectiles = new List<ProjectileModule.ChargeProjectile>(){
            new(){
                Projectile = _KaliProjectile.Clone(GunData.New(damage: 25f, speed: 175f, recoil: 100f)
                  ).Attach<KaliProjectile>(k => k.SetChargeLevel(1)
                  ),
                ChargeTime = 1f,
            },
            new(){
                Projectile = _KaliProjectile.Clone(GunData.New(damage: 50f, speed: 350f, recoil: 200f)
                  ).Attach<KaliProjectile>(k => k.SetChargeLevel(2)
                  ),
                ChargeTime = 2.5f,
            },
            new(){
                Projectile = _KaliProjectile.Clone(GunData.New(damage: 100f, speed: 700f, recoil: 400f)
                  ).Attach<KaliProjectile>(k => k.SetChargeLevel(3)
                  ),
                ChargeTime = 4.5f,
            },
        };

        string chargeAnim1 = gun.QuickUpdateGunAnimation("charge", returnToIdle: false);
            gun.SetAnimationFPS(chargeAnim1, 20);
            // gun.SetGunAudio(_ChargeBase, "kali_charge_sound", 0);
        string chargeAnim2 = gun.QuickUpdateGunAnimation("charge_more", returnToIdle: false);
            gun.SetAnimationFPS(chargeAnim2, 40);
            // gun.SetGunAudio(_ChargeMore, "kali_charge_sound", 0);
        string chargeAnim3 = gun.QuickUpdateGunAnimation("charge_most", returnToIdle: false);
            gun.SetAnimationFPS(chargeAnim3, 60);
            gun.SetGunAudio(chargeAnim3, "kali_charge_sound", 0);
        _ChargeAnimations = new(){chargeAnim1, chargeAnim2, chargeAnim3};

        _IonizeVFX = VFX.Create("kali_ionize_particle", fps: 7, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 100f);
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
            this._timeCharging = 0f;
            this.gun.chargeAnimation = _ChargeAnimations[0];
            this.gun.sprite.usesOverrideMaterial = false;
            this.gun.sprite.renderer.material.SetFloat("_EmissivePower", 0f);
            this.gun.sprite.UpdateMaterial();
            return;
        }

        if ((this._timeCharging += BraveTime.DeltaTime) > _AMMO_DRAIN_RATE)
        {
            this._timeCharging -= _AMMO_DRAIN_RATE;
            this.gun.LoseAmmo(1);
        }

        int newChargeLevel = 1 + this.gun.GetChargeLevel();
        if (newChargeLevel == this._chargeLevel)
            return;

        this._chargeLevel = newChargeLevel;
        this.gun.chargeAnimation = _ChargeAnimations[Math.Max(newChargeLevel - 1, 0)];
        this.gun.spriteAnimator.currentClip = this.gun.spriteAnimator.GetClipByName(this.gun.chargeAnimation);
        if (newChargeLevel == 0)
        {
            base.gameObject.Play("kali_activate_sound");
            this.gun.spriteAnimator.Play();
        }
        else if (newChargeLevel < 3)
        {
            base.gameObject.Play("kali_charge_sound");
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

    public override void OnDestroy()
    {
        this._timeShifter.SafeDestroy();
        base.OnDestroy();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (projectile.GetComponent<KaliProjectile>() is not KaliProjectile kp)
            return;

        if (kp.GetChargeLevel() == 3)
            this._timeShifter.GetComponent<KaliTimeshifter>().Reset();
        projectile.transform.DoMovingDistortionWave(distortionIntensity: 2.5f, distortionRadius: 0.25f, maxRadius: 0.25f, duration: 0.75f);
    }
}

public class KaliProjectile : MonoBehaviour
{
    [SerializeField]
    private int _chargeLevel = 0;

    private void Start()
    {
        base.GetComponentInChildren<TrailController>().gameObject.SetGlowiness(100f);
        Projectile p = base.GetComponent<Projectile>();
        p.OnWillKillEnemy += OnWillKillEnemy;
        p.OnHitEnemy += OnHitEnemy;
        p.specRigidbody.OnPreRigidbodyCollision += this.MaybeVaporizeProjectiles;
        p.specRigidbody.OnRigidbodyCollision += this.VaporizeProjectiles;
    }

    private void MaybeVaporizeProjectiles(SpeculativeRigidbody me, PixelCollider myPixelCollider, SpeculativeRigidbody other, PixelCollider otherPixelCollider)
    {
        if (other.GetComponent<Projectile>() is not Projectile p)
            return; // do nothing if we didn't run into a projectile
        if (this._chargeLevel != 3)
            PhysicsEngine.SkipCollision = true; // don't vaporize except at final charge level
        else if (p.Owner is PlayerController)
            PhysicsEngine.SkipCollision = true; // doin't vaporize player projectiles
    }

    private void VaporizeProjectiles(CollisionData rigidbodyCollision)
    {
        if (this._chargeLevel != 3)
            return;
        if (rigidbodyCollision.OtherRigidbody.GetComponent<Projectile>() is not Projectile other)
            return;
        if (other.Owner is PlayerController)
            return;
        FancyVFX glowyBoi = FancyVFX.FromCurrentFrame(other.sprite);
        glowyBoi.sprite.SetGlowiness(300f, overrideColor: Color.cyan, glowColor: Color.cyan, clampBrightness: false);
        glowyBoi.sprite.StartCoroutine(CriticalGlow(glowyBoi.sprite));
        other.DieInAir(suppressInAirEffects: true, allowActorSpawns: false, allowProjectileSpawns: false, killedEarly: true);
    }

    public void SetChargeLevel(int level) => this._chargeLevel = level;
    public int GetChargeLevel() => this._chargeLevel;

    private static void OnHitEnemy(Projectile p, SpeculativeRigidbody enemy, bool killed)
    {
        enemy.gameObject.Play("kali_impact_sound");
    }

    private static void OnWillKillEnemy(Projectile p, SpeculativeRigidbody body)
    {
        if (!body.aiActor || body.aiActor.IsABoss(canBeDead: true))
            return;
        FancyVFX glowyBoi = FancyVFX.FromCurrentFrame(body.aiActor.sprite);
        glowyBoi.sprite.SetGlowiness(300f, overrideColor: Color.cyan, glowColor: Color.cyan, clampBrightness: false);
        glowyBoi.sprite.StartCoroutine(CriticalGlow(glowyBoi.sprite));
        body.aiActor.EraseFromExistence(true);
    }

    private static IEnumerator CriticalGlow(tk2dSprite sprite)
    {
        Material m = sprite.renderer.material;
        for (float elapsed = 0f; elapsed < 0.5f; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / 0.5f;
            m.SetFloat("_EmissivePower", 300f + 2700f * percentDone);
            yield return null;
        }
        FancyVFX.SpawnBurst(
            prefab           : KALI._IonizeVFX,
            numToSpawn       : 50,
            basePosition     : sprite.WorldCenter,
            positionVariance : 1f,
            baseVelocity     : null,
            minVelocity      : 40f,
            velocityVariance : 40f,
            velType          : FancyVFX.Vel.Away,
            rotType          : FancyVFX.Rot.None,
            lifetime         : 0.5f,
            parent           : null,
            emissivePower    : 300f,
            emissiveColor    : Color.cyan,
            fadeIn           : false,
            uniform          : false,
            startScale       : 1.0f,
            endScale         : 1.0f,
            height           : null
          );
        sprite.gameObject.PlayUnique("kali_explode_sound");
        UnityEngine.Object.Destroy(sprite.gameObject);
    }
}

public class KaliTimeshifter : MonoBehaviour
{
    private const float _MIN_TIME_SCALE = 0.1f;
    private const float _DLT_TIME_SCALE = 1f - _MIN_TIME_SCALE;
    private const float _EASE_TIME = 0.7f;

    private float _curTime = 0.0f;

    private void Start()
    {
        this._curTime = _EASE_TIME;
    }

    private void Update()
    {
        if (BraveTime.DeltaTime == 0.0f)
            return;

        this._curTime += Time.unscaledDeltaTime; // NOTE: specifically not using BraveTime here because that would slow down the game to crawl
        if (this._curTime >= _EASE_TIME)
        {
            BraveTime.ClearMultiplier(base.gameObject);
            return;
        }

        float percentDone = this._curTime / _EASE_TIME;
        float cubicEase = percentDone * percentDone * percentDone;
        BraveTime.SetTimeScaleMultiplier(_MIN_TIME_SCALE + _DLT_TIME_SCALE * cubicEase, base.gameObject);
    }

    public void Reset()
    {
        this._curTime = 0.0f;
    }

    private void OnDestroy()
    {
        BraveTime.ClearMultiplier(base.gameObject);
    }
}
