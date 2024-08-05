namespace CwaffingTheGungy;

public class Gunflower : CwaffGun
{
    public static string ItemName         = "Gunflower";
    public static string ShortDescription = "Petal to the Metal";
    public static string LongDescription  = "Fires a highly concentrated beam of sunlight. Replenishes ammo when exposed to water and water-like goops. Depletes ammo when exposed to fire and various unhealthy goops.";
    public static string Lore             = "TBD";

    private const float _LIGHT_SPACING = 2f;

    internal static GameObject _GrowthSparkles;
    internal static GameObject _DecayVFX;

    private List<AdditionalBraveLight> _lights = new();
    private ModuleShootData _cachedShootData = null;
    private uint _soundId = 0;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Gunflower>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.A, gunClass: GunClass.BEAM, reloadTime: 0.0f, ammo: 100, shootFps: 4, reloadFps: 4,
              muzzleFrom: Items.Mailbox, dynamicBarrelOffsets: true);
            gun.LoopAnimation(gun.shootAnimation, 4);

        //NOTE: inherit from Moonscraper for hitscan
        Projectile projectile = gun.InitProjectile(GunData.New(baseProjectile: Items.Moonscraper.Projectile(), clipSize: -1, cooldown: 0.18f,
          shootStyle: ShootStyle.Beam, damage: 100f, speed: -1f, ammoType: GameUIAmmoType.AmmoType.BEAM, ammoCost: 10, angleVariance: 0f));

        BasicBeamController beamComp = projectile.SetupBeamSprites(spriteName: "gunflower_beam", fps: 17, chargeFps: 8,
          dims: new Vector2(32, 7), impactDims: new Vector2(15, 7), impactFps: 14, loopCharge: false);
        beamComp.reflections = 0;
        beamComp.chargeDelay = 0.8f;
        beamComp.sprite.usesOverrideMaterial = true;
        beamComp.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTiltedCutoutEmissive");
        beamComp.sprite.renderer.material.SetFloat("_EmissivePower", 40f);

        _GrowthSparkles = VFX.Create("gunflower_growth_sparkles", 2, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 100f);
        _DecayVFX = VFX.Create("gunflower_decay_vfx", 2, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 100f);
    }

    private bool _revved = false;
    public override void Update()
    {
        base.Update();
        UpdateNutrients();
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

    private void UpdateNutrients()
    {
        if (this.PlayerOwner is not PlayerController player)
            return;
        GoopDefinition currentGoop = player.CurrentGoop;
        if (!currentGoop)
            return;
        GoopPositionData goopData = player.specRigidbody.UnitCenter.GoopData();
        if (goopData == null)
            return;
        int nutrition;
        bool consumesGoop;
        if (currentGoop.DrainsAmmo)
            { consumesGoop = false; nutrition = 0; }  // no double jeopardy from ammo draining goop
        else if (player.m_currentGoopFrozen)
            { consumesGoop = false; nutrition = 0; }  // do nothing on ice
        else if (goopData.IsOnFire && currentGoop.fireEffect is GameActorFireEffect fire && fire.AffectsPlayers)
            { consumesGoop = true; nutrition = -1; }  // fire burns us
        else if (currentGoop.AppliesDamageOverTime)
            { consumesGoop = true; nutrition = -1; }  // poison is toxic
        else if (currentGoop.isOily)
            { consumesGoop = true; nutrition = -1; }  // oil is toxic
        else if (currentGoop.usesWaterVfx)
            { consumesGoop = true; nutrition = 1; }   // water is nutritious
        else if (currentGoop.SpeedModifierEffect is GameActorSpeedEffect)
            { consumesGoop = false; nutrition = 0; }  // do nothing on webs
        else
            { consumesGoop = true; nutrition = -1; }  // mystery goops are assumed toxic
        bool nutritious = nutrition > 0;
        if (nutrition > 0)
        {
            this.gun.GainAmmo(nutrition);
            this.gun.gameObject.PlayOnce("starmageddon_bullet_impact_sound_2");
        }
        else if (nutrition < 0)
        {
            this.gun.LoseAmmo(-nutrition);
            this.gun.gameObject.PlayOnce("lightwing_impact_sound");
        }
        if (nutrition != 0)
            CwaffVFX.SpawnBurst(
                prefab           : nutritious ? _GrowthSparkles : _DecayVFX,
                numToSpawn       : nutritious ? 2 : 1,
                basePosition     : this.gun.barrelOffset.position,
                positionVariance : 1f,
                velocityVariance : nutritious ? 3f : 5f,
                velType          : CwaffVFX.Vel.Radial,
                rotType          : CwaffVFX.Rot.None,
                lifetime         : 0.5f,
                fadeOutTime      : 0.5f,
                emissivePower    : (nutrition > 0) ? 100f : 0f,
                emissiveColor    : Color.yellow
                );
        if (consumesGoop)
            DeadlyDeadlyGoopManager.DelayedClearGoopsInRadius(player.CenterPosition, 1f);
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
            {
                this._lights[i].gameObject.transform.position = barrelPos + mag * deltaNorm;
                this._lights[i].LightIntensity = 100f;
            }
            else
            {
                this._lights[i].gameObject.transform.position = barrelPos + i * gap;
                this._lights[i].LightIntensity = 10f;
            }
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
