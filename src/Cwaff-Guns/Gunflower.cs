namespace CwaffingTheGungy;

public class Gunflower : CwaffGun
{
    public static string ItemName         = "Gunflower";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const float _LIGHT_SPACING = 2f;

    private List<AdditionalBraveLight> _lights = new();
    private ModuleShootData _cachedShootData = null;
    private uint _soundId = 0;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Gunflower>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.PISTOL, reloadTime: 0.0f, ammo: 1200, shootFps: 4, reloadFps: 4,
                muzzleFrom: Items.Mailbox);
            gun.LoopAnimation(gun.shootAnimation, 4);

        //NOTE: inherit from Moonscraper for hitscan
        Projectile projectile = gun.InitProjectile(GunData.New(baseProjectile: Items.Moonscraper.Projectile(), clipSize: -1, cooldown: 0.18f, shootStyle: ShootStyle.Beam,
            speed: -1f, ammoType: GameUIAmmoType.AmmoType.BEAM, ammoCost: 5, angleVariance: 0f));

        BasicBeamController beamComp = projectile.SetupBeamSprites(spriteName: "gunflower_beam", fps: 17, chargeFps: 8,
            dims: new Vector2(32, 7), impactDims: new Vector2(15, 7), impactFps: 14, loopCharge: false);
        beamComp.reflections = 0;
        beamComp.chargeDelay = 0.8f; // <gun shoot animation loop point> / <shootFps>
        beamComp.sprite.usesOverrideMaterial = true;
        beamComp.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTiltedCutoutEmissive");
        beamComp.sprite.renderer.material.SetFloat("_EmissivePower", 40f);

        // Projectile proj = gun.InitProjectile(GunData.New(sprite: null, clipSize: 60, cooldown: 0.05f, shootStyle: ShootStyle.Automatic,
        //     damage: 3.0f, speed: 30f, range: 1000f, force: 2f, hitEnemySound: "paintball_impact_enemy_sound", hitWallSound: "paintball_impact_wall_sound",
        //     invisibleProjectile: true, preventSparks: true))
        //   .Attach<LightProjectile>();
        // proj.AddLight(color: Color.yellow, radius: 2f, brightness: 100f);

        // gun.AddLight(useCone: true, fadeInTime: 0.5f, fadeOutTime: 0.25f, color: Color.yellow, grownIn: true, turnOnImmediately: false);
    }

    private bool _revved = false;
    public override void Update()
    {
        base.Update();
        bool shouldPlaySound = this.gun && this.gun.IsFiring;
        if (shouldPlaySound && this._soundId == 0)
            this._soundId = this.gun.LoopSound("gunflower_fire_sound", loopPointMs: 1750, rewindAmountMs: 1750 - 1177);
        else if (!shouldPlaySound && this._soundId > 0)
        {
            AkSoundEngine.StopPlayingID(this._soundId);
            this._soundId = 0;
        }
        if (GetExtantBeam() is BasicBeamController beam && beam.State == BeamState.Firing)
        {
            if (!this._revved)
            {
                this.gun.SetAnimationFPS(this.gun.shootAnimation, 20);
                this.gun.spriteAnimator.PlayFromFrame(this.gun.shootAnimation, 4);
                this._revved = true;
            }
            UpdateLights(beam);
        }
        else
        {
            if (this._revved)
            {
                this.gun.SetAnimationFPS(this.gun.shootAnimation, 4);
                this._revved = false;
            }
            DismissLights();
        }
    }

    private BeamController GetExtantBeam()
    {
        if (_cachedShootData == null)
        {
            if (!this.gun || !this.gun.IsFiring || this.gun.m_moduleData == null || this.gun.DefaultModule == null)
                return null;
            if (!this.gun.m_moduleData.TryGetValue(this.gun.DefaultModule, out ModuleShootData data))
                return null;
            this._cachedShootData = data;
        }
        return this._cachedShootData.beam;
    }

    private void DismissLights(int startIndex = 0)
    {
        for (int i = startIndex; i < this._lights.Count; ++i)
            if (this._lights[i])
                this._lights[i].LightIntensity = 0f;
    }

    private void UpdateLights(BasicBeamController beam)
    {
        Vector2 barrelPos = this.gun.barrelOffset.position;
        Vector2 deltaNorm = beam.Direction.normalized;
        Vector2 gap = _LIGHT_SPACING * deltaNorm;
        float mag = beam.m_currentBeamDistance;
        int steps = Mathf.CeilToInt(mag / _LIGHT_SPACING);
        for (int i = 0; i <= steps; ++i)
        {
            if (this._lights.Count < i + 1)
                this._lights.Add(null);
            if (!this._lights[i])
            {
                this._lights[i] = new GameObject().AddComponent<AdditionalBraveLight>();
                this._lights[i].LightColor = Color.white;
                this._lights[i].LightRadius = 2f;
                this._lights[i].Initialize();
            }
            if (i == steps)
                this._lights[i].gameObject.transform.position = barrelPos + mag * deltaNorm;
            else
                this._lights[i].gameObject.transform.position = barrelPos + i * gap;
            this._lights[i].LightIntensity = 10f;
        }
        for (int i = steps + 1; i < this._lights.Count; ++i)
            if (this._lights[i])
                this._lights[i].LightIntensity = 0f;
    }

    // public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    // {
    //     base.OnReloadPressed(player, gun, manualReload);
    //     if (gun.GetComponentInChildren<EasyLight>() is EasyLight light)
    //         light.Toggle();
    // }
}

public class LightProjectile : MonoBehaviour
{
    // private GameActor _owner;
    // private EasyLight _light;
    // private void Start()
    // {
    //     this._light = base.gameObject.GetComponentInChildren<EasyLight>();
    //     this._owner = base.gameObject.GetComponent<Projectile>().Owner;
    // }

    // private void Update()
    // {
    //     if (this._light && this._owner)
    //         this._light.PointAt(this._owner.CenterPosition);
    // }
}
