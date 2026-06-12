namespace CwaffingTheGungy;

public class Mtara : CwaffGun
{
    public static string ItemName         = "M'tara";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const float _OVERHEAT_TIME = 3.0f;
    private const float _COOLDOWN_RATE_NORMAL = 3.0f;
    private const float _COOLDOWN_RATE_OVERHEATED = 1.0f;

    private float _heat = 0.0f;
    private bool _overheated = false;
    private bool _allowOverheatFire = false;

    public static void Init()
    {
        Lazy.SetupGun<Mtara>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.A, gunClass: GunClass.BEAM, reloadTime: 0.0f, ammo: 100, modulesAreTiers: true,
            shootFps: 14, reloadFps: 4, doesScreenShake: false, percentSpeedWhileFiring: 0.1f)
          .Attach<MtaraAmmoDisplay>()
          .AssignGun(out Gun gun);

        for (int i = 0; i < 2; ++i)
        {
          bool mastered = i == 1;
          GunData gunData = GunData.New(gun: gun, baseProjectile: Items.Moonscraper.Projectile(), //NOTE: inherit from Moonscraper for hitscan
            clipSize: -1, cooldown: 0.25f, shootStyle: ShootStyle.Beam, damage: 50f, force: 0f, speed: -1f, ammoCost: 1, customClip: true,
            angleVariance: 0f, beamSprite: mastered ? "mtara_overheat_beam" : "mtara_beam", beamFps: 60, beamChargeFps: 120, beamImpactFps: 60,
            hitEnemySound: mastered ? "mtara_overheat_impact_sound" : "mtara_impact_sound", beamLoopCharge: false, beamReflections: 0,
            beamChargeDelay: mastered ? 0.0f : 0.05f, beamEmission: 40.0f, beamImpactEmission: 50f, beamEmissionColorPower: 10.0f,
            beamEmissionSensitivity: 0.6f, beamStatusDelay: 0.0f, beamEmissionColor: mastered ? new Color(0.75f, 1.0f, 0.75f) : new Color(1.0f, 1.0f, 0.5f),
            fire: mastered ? 1.0f : 0.0f, greenFire: true);
          if (i == 0)
            gun.InitProjectile(gunData);
          else
            gun.DuplicateDefaultModule(cloneProjectiles: false).InitSingleProjectileModule(gunData);
        }
    }

    public override void OnTriedToInitiateAttack(PlayerController player)
    {
      base.OnTriedToInitiateAttack(player);
      if (this._overheated && !this.Mastered)
        player.SuppressThisClick = true;
    }

    private void Start()
    {
        gun.sprite.MakeGlowyBetter(0.9f, new Color(0.0f, 0.7f, 0.5f), 20.0f, 0.1f);
    }

    public override void Update()
    {
        base.Update();
        bool mastered = this.Mastered;
        if (mastered)
          this.percentSpeedWhileFiring = 1.0f;
        Material mat = gun.sprite.renderer.material;
        mat.SetFloat(CwaffVFX._EmissivePowerId, 0.1f);
        if (this.PlayerOwner is not PlayerController player)
            return;
        bool isActuallyFiring = this.gun && this.gun.IsFiring && !player.IsDodgeRolling;
        if (this._allowOverheatFire && !isActuallyFiring)
        {
          if (this.gun.IsFiring)
            this.gun.CeaseAttack();
          this._allowOverheatFire = false; // force cooldown once we actually let go of the button
          this.gun.CurrentStrengthTier = 0;
          base.gameObject.Play("mtara_overheat_sound");
        }
        UpdateHeat(isActuallyFiring);
        bool overheatFire = this._overheated && mastered;
        if (this._allowOverheatFire)
          this.gun.LoopSoundIf(isActuallyFiring, "focus_rifle_overheat_fire_sound", loopPointMs: 800, rewindAmountMs: 800 - 601);
        else
          this.gun.LoopSoundIf(isActuallyFiring, "focus_rifle_fire_sound", loopPointMs: 800, rewindAmountMs: 800 - 601);
        if (GetExtantBeam() is BasicBeamController beam && beam.State == BeamState.Firing)
        {
            UpdateLights();
            mat.SetFloat(CwaffVFX._EmissivePowerId, 0.9f + 0.8f * Mathf.Abs(Mathf.Sin(15f * BraveTime.ScaledTimeSinceStartup)));
        }
    }

    private float HeatPercent() => this._heat / _OVERHEAT_TIME;

    private void UpdateHeat(bool firing)
    {
      if (this._allowOverheatFire)
        return;
      float dtime = BraveTime.DeltaTime;
      if (!firing)
      {
        this._heat -= (this._overheated ? _COOLDOWN_RATE_OVERHEATED : _COOLDOWN_RATE_NORMAL) * dtime;
        if (this._heat <= 0.0f)
        {
          this._heat = 0.0f;
          if (this._overheated)
            this.ToggleOverheat(false);
        }
        return;
      }
      if (this._heat >= _OVERHEAT_TIME)
        return;
      this._heat += dtime;
      if (this._heat < _OVERHEAT_TIME)
        return;

      this._heat = _OVERHEAT_TIME;
      this.ToggleOverheat(true);
    }

    private void ToggleOverheat(bool isOverheated)
    {
      this._overheated = isOverheated;
      this.PlayerOwner.inventory.GunLocked.SetOverride(ItemName, isOverheated);
      this.gun.CanBeDropped = !isOverheated;
      if (!isOverheated)
        return;
      if (this.Mastered)
      {
        this._allowOverheatFire = true;
        this.gun.CurrentStrengthTier = 1;
        bool wasFiring = this.gun.IsFiring;
        if (wasFiring)
        {
          this.gun.gameObject.Stop("focus_rifle_fire_sound");
          this.gun.CeaseAttack();
        }
        ClearCachedShootData(); // reset particle effects
        if (wasFiring)
          this.gun.Attack();
        return;
      }
      this.gun.CeaseAttack();
      base.gameObject.Play("mtara_overheat_sound");
      GameObject vfxPrefab = Jugglernaut._ImpactVFX[5].effects[0].effects[0].effect;
      Vector2 barrelPos = this.gun.barrelOffset.transform.position;
      Vector2 playerPos = this.PlayerOwner.CenterPosition;
      for (int i = 0; i < 4; ++i)
      {
        tk2dSprite vfxSprite = SpawnManager.SpawnVFX(vfxPrefab, Vector2.Lerp(barrelPos, playerPos, 0.25f * i), Quaternion.identity).GetComponent<tk2dSprite>();
        vfxSprite.HeightOffGround = 10f;
        vfxSprite.UpdateZDepth();
      }
    }

    private void UpdateLights()
    {
        float emission = 40f + 20f * Mathf.Sin(40f * BraveTime.ScaledTimeSinceStartup);
        for (int ibeam = this.gun.m_activeBeams.Count - 1; ibeam >= 0; --ibeam)
            for (BasicBeamController beam = this.gun.m_activeBeams[ibeam].beam as BasicBeamController; beam; beam = beam.m_reflectedBeam)
                beam.sprite.renderer.material.SetFloat(CwaffVFX._EmissivePowerId, emission);
    }

    private class MtaraAmmoDisplay : CustomAmmoDisplay
    {
        private const int NUM_BARS = 8;

        private static StringBuilder _SB = new StringBuilder("", 1000);

        private Mtara _mtara;
        private PlayerController _owner;

        private void Start()
        {
            Gun gun     = base.GetComponent<Gun>();
            this._mtara = gun.GetComponent<Mtara>();
            this._owner = gun.CurrentOwner as PlayerController;
        }

        public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
        {
            if (!this._owner)
                return false;

            //TODO: double check this isn't a performance issue
            _SB.Length = 0;
            int heatBars = Mathf.CeilToInt(NUM_BARS * this._mtara.HeatPercent());
            if (this._mtara._overheated && this._mtara._allowOverheatFire)
            {
              for (int i = heatBars; i > 0; --i)
                _SB.AppendFormat("[sprite \"mtara_heat_max_ui{0}\"]", UnityEngine.Random.Range(1, 5).ToString());
            }
            else
            {
              for (int i = heatBars; i > 0; --i)
                _SB.AppendFormat("[sprite \"mtara_heat_ui{0}\"]", (this._mtara._overheated ? 8 : i).ToString());
            }
            _SB.Append("\n");
            _SB.Append(this._owner.VanillaAmmoDisplay());
            uic.GunAmmoCountLabel.Text = _SB.ToString();
            return true;
        }
    }
}
