namespace CwaffingTheGungy;

public class Gunflower : CwaffGun
{
    public static string ItemName         = "Gunflower";
    public static string ShortDescription = "Petal to the Metal";
    public static string LongDescription  = "Fires a highly concentrated beam of sunlight. Replenishes ammo when exposed to water and water-like goops. Depletes ammo when exposed to fire and various unhealthy goops.";
    public static string Lore             = "A living sunflower that has been cybernetically enhanced with a generator and a refraction chamber. The botanist who invented it had originally been trying to produce a flower capable of growing itself, a million-dollar idea so it seemed. Several failed attempts and a refresher on the first law of thermodynamics later, they eventually tossed all of their prototypes into the wind, which carried some of them all the way into the Gungeon.";

    private const float _LIGHT_SPACING = 2f;
    private const float _PASSIVE_AMMO_REGEN_TIME = 5f;
    private const float _PASSIVE_AMMO_REGEN_PERCENT = 0.1f;

    internal static GameObject _GrowthSparkles;
    internal static GameObject _DecayVFX;

    private List<AdditionalBraveLight> _lights = new();
    private bool _revved = false;

    public static void Init()
    {
        Lazy.SetupGun<Gunflower>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.A, gunClass: GunClass.BEAM, reloadTime: 0.0f, ammo: 100, shootFps: 4, reloadFps: 4,
            muzzleFrom: Items.Mailbox, dynamicBarrelOffsets: true, loopFireAt: 4)
          .InitProjectile(GunData.New(baseProjectile: Items.Moonscraper.Projectile(), clipSize: -1, cooldown: 0.18f, //NOTE: inherit from Moonscraper for hitscan
            shootStyle: ShootStyle.Beam, damage: 100f, speed: -1f, customClip: true, ammoCost: 3, angleVariance: 0f,
            beamSprite: "gunflower_beam", beamFps: 17, beamChargeFps: 8, beamImpactFps: 14,
            beamLoopCharge: false, beamReflections: 0, beamChargeDelay: 0.8f, beamEmission: 40f));

        _GrowthSparkles = VFX.Create("gunflower_growth_sparkles", emissivePower: 100f);
        _DecayVFX = VFX.Create("gunflower_decay_vfx", emissivePower: 100f);
    }

    public override void Update()
    {
        base.Update();
        if (this.PlayerOwner is not PlayerController player)
            return;
        if (this.gun.CurrentAmmo > 0 && this.gun.spriteAnimator.IsPlaying(this.gun.outOfAmmoAnimation))
            this.gun.spriteAnimator.PlayFromFrame(this.gun.idleAnimation, 0);
        if (this.Mastered && !this.gun.IsFiring)
            DoPassiveAmmoRegen();
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
            UpdateLights();
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

    private float _lastRegenTime = 0;
    private void DoPassiveAmmoRegen()
    {
        float ammoPercent = (float)this.gun.CurrentAmmo / this.gun.AdjustedMaxAmmo;
        if (ammoPercent >= _PASSIVE_AMMO_REGEN_PERCENT)
            return;

        float now = BraveTime.ScaledTimeSinceStartup;
        float timeToRestoreOneAmmo = _PASSIVE_AMMO_REGEN_TIME / (_PASSIVE_AMMO_REGEN_PERCENT * this.gun.AdjustedMaxAmmo);
        float timeSinceLastRegen = now - _lastRegenTime;
        if (timeSinceLastRegen < timeToRestoreOneAmmo)
            return;

        this._lastRegenTime = now;
        this.gun.GainAmmo(1);
        this.gun.gameObject.PlayOnce("starmageddon_bullet_impact_sound_2");
        SpawnParticles(true);
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
        if (this.Mastered)
            { consumesGoop = true; nutrition = 1; }   // all goop is nutritious if we're mastered
        else if (currentGoop.DrainsAmmo)
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
        if (nutrition > 0)
        {
            this.gun.GainAmmo(nutrition);
            if (player.HasSynergy(Synergy.PHOTOSYNTHESIS) && player.GetGun((int)Items.Camera) is Gun camera)
                camera.GainAmmo(nutrition);
            this.gun.gameObject.PlayOnce("starmageddon_bullet_impact_sound_2");
            SpawnParticles(true);
        }
        else if (nutrition < 0)
        {
            this.gun.LoseAmmo(-nutrition);
            this.gun.gameObject.PlayOnce("lightwing_impact_sound");
            SpawnParticles(false);
        }
        if (consumesGoop)
            DeadlyDeadlyGoopManager.DelayedClearGoopsInRadius(player.CenterPosition, 1f);
    }

    private void SpawnParticles(bool nutritious)
    {
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
            emissivePower    : nutritious ? 100f : 0f,
            emissiveColor    : Color.yellow
            );
    }

    private void DismissLights(int startIndex = 0)
    {
        for (int i = startIndex; i < this._lights.Count; ++i)
            if (this._lights[i])
                this._lights[i].LightIntensity = 0f;
    }

    private void UpdateLightsForBeam(BasicBeamController beam, ref int i)
    {
        Vector2 origin = beam.Origin;
        Vector2 deltaNorm = beam.Direction.normalized;
        Vector2 gap = _LIGHT_SPACING * deltaNorm;
        float mag = beam.m_currentBeamDistance;
        int steps = Mathf.CeilToInt(mag / _LIGHT_SPACING);
        for (int s = 0; s <= steps; ++s)
        {
            ++i;
            if (this._lights.Count < i + 1)
                this._lights.Add(null);
            if (!this._lights[i])
            {
                this._lights[i] = new GameObject().AddComponent<AdditionalBraveLight>();
                this._lights[i].LightColor = Color.white;
                this._lights[i].LightRadius = 2f;
                this._lights[i].Initialize();
            }
            if (s == steps)
            {
                this._lights[i].gameObject.transform.position = origin + mag * deltaNorm;
                this._lights[i].LightIntensity = 100f;
            }
            else
            {
                this._lights[i].gameObject.transform.position = origin + s * gap;
                this._lights[i].LightIntensity = 10f;
            }
        }
    }

    private void UpdateLights()
    {
        int i = -1;
        for (int ibeam = this.gun.m_activeBeams.Count - 1; ibeam >= 0; --ibeam)
            for (BasicBeamController beam = this.gun.m_activeBeams[ibeam].beam as BasicBeamController; beam; beam = beam.m_reflectedBeam)
                UpdateLightsForBeam(beam, ref i);
        while (++i < this._lights.Count)
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
