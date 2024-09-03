namespace CwaffingTheGungy;

public class Gunflower : CwaffGun
{
    public static string ItemName         = "Gunflower";
    public static string ShortDescription = "Petal to the Metal";
    public static string LongDescription  = "Fires a highly concentrated beam of sunlight. Replenishes ammo when exposed to water and water-like goops. Depletes ammo when exposed to fire and various unhealthy goops.";
    public static string Lore             = "A living sunflower that has been cybernetically enhanced with a generator and a refraction chamber. The botanist who invented it had originally been trying to produce a flower capable of growing itself, a million-dollar idea so it seemed. Several failed attempts and a refresher on the first law of thermodynamics later, they eventually tossed all of their prototypes into the wind, which carried some of them all the way into the Gungeon.";

    private const float _LIGHT_SPACING = 2f;

    internal static GameObject _GrowthSparkles;
    internal static GameObject _DecayVFX;

    private List<AdditionalBraveLight> _lights = new();
    private ModuleShootData _cachedShootData = null;
    private bool _revved = false;

    public static void Init()
    {
        Lazy.SetupGun<Gunflower>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.A, gunClass: GunClass.BEAM, reloadTime: 0.0f, ammo: 100, shootFps: 4, reloadFps: 4,
            muzzleFrom: Items.Mailbox, dynamicBarrelOffsets: true, loopFireAt: 4)
          .InitProjectile(GunData.New(baseProjectile: Items.Moonscraper.Projectile(), clipSize: -1, cooldown: 0.18f, //NOTE: inherit from Moonscraper for hitscan
            shootStyle: ShootStyle.Beam, damage: 100f, speed: -1f, customClip: true, ammoCost: 10, angleVariance: 0f,
            beamSprite: "gunflower_beam", beamFps: 17, beamChargeFps: 8, beamImpactFps: 14,
            beamLoopCharge: false, beamReflections: 0, beamChargeDelay: 0.8f, beamEmission: 40f));

        _GrowthSparkles = VFX.Create("gunflower_growth_sparkles", emissivePower: 100f);
        _DecayVFX = VFX.Create("gunflower_decay_vfx", emissivePower: 100f);
    }

    public override void Update()
    {
        base.Update();
        UpdateNutrients();
        bool shouldPlaySound = this.gun && this.gun.IsFiring;
        this.gun.LoopSoundIf(shouldPlaySound, "gunflower_fire_sound", loopPointMs: 1750, rewindAmountMs: 1750 - 1177);
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

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        DismissLights();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        DismissLights();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (this && this._lights != null)
            for (int i = 0; i < this._lights.Count; ++i)
                if (this._lights[i] && this._lights[i].gameObject)
                    UnityEngine.Object.Destroy(this._lights[i].gameObject);
    }
}
