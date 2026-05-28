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

    public static void Init()
    {
        Lazy.SetupGun<Mtara>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.A, gunClass: GunClass.BEAM, reloadTime: 0.0f, ammo: 100,
            shootFps: 14, reloadFps: 4, doesScreenShake: false, percentSpeedWhileFiring: 0.1f)
          .Attach<MtaraAmmoDisplay>()
          .InitProjectile(GunData.New(baseProjectile: Items.Moonscraper.Projectile(), //NOTE: inherit from Moonscraper for hitscan
            clipSize: -1, cooldown: 0.25f, shootStyle: ShootStyle.Beam, damage: 50f, force: 0f, speed: -1f, ammoCost: 1, customClip: true,
            angleVariance: 0f, beamSprite: "mtara_beam", beamFps: 60, beamChargeFps: 30, beamImpactFps: 60, hitEnemySound: "mtara_impact_sound",
            beamLoopCharge: false, beamReflections: 0, beamChargeDelay: 0.225f, beamEmission: 40.0f, beamImpactEmission: 50f,
            beamEmissionColor: new Color(1.0f, 1.0f, 0.5f), beamEmissionColorPower: 10.0f, beamEmissionSensitivity: 0.6f));
    }

    public override void OnTriedToInitiateAttack(PlayerController player)
    {
      base.OnTriedToInitiateAttack(player);
      if (this._overheated)
        player.SuppressThisClick = true;
    }

    public override void Update()
    {
        base.Update();
        if (this.PlayerOwner is not PlayerController player)
            return;
        bool isActuallyFiring = this.gun && this.gun.IsFiring && !player.IsDodgeRolling;
        UpdateHeat(isActuallyFiring);
        this.gun.LoopSoundIf(isActuallyFiring, "focus_rifle_fire_sound", loopPointMs: 800, rewindAmountMs: 800 - 601);
        if (GetExtantBeam() is BasicBeamController beam && beam.State == BeamState.Firing)
            UpdateLights();
    }

    private float HeatPercent() => this._heat / _OVERHEAT_TIME;

    private void UpdateHeat(bool firing)
    {
      float dtime = BraveTime.DeltaTime;
      if (this._overheated)
      {
        this._heat -= _COOLDOWN_RATE_OVERHEATED * dtime;
        if (this._heat <= 0.0f)
        {
          this._heat = 0.0f;
          this.ToggleOverheat(false);
        }
        return;
      }
      if (!firing)
      {
        this._heat -= _COOLDOWN_RATE_NORMAL * dtime;
        if (this._heat < 0)
          this._heat = 0.0f;
        return;
      }
      this._heat += dtime;
      if (this._heat < _OVERHEAT_TIME)
        return;

      this._heat = _OVERHEAT_TIME;
      this.ToggleOverheat(true);
      this.gun.CeaseAttack();
    }

    private void ToggleOverheat(bool isOverheated)
    {
      this._overheated = isOverheated;
      this.PlayerOwner.inventory.GunLocked.SetOverride(ItemName, isOverheated);
      this.gun.CanBeDropped = !isOverheated;
      if (isOverheated)
      {
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
            for (int i = heatBars; i > 0; --i)
              _SB.AppendFormat("[sprite \"mtara_heat_ui{0}\"]", (this._mtara._overheated ? 8 : i).ToString());
            _SB.Append("\n");
            _SB.Append(this._owner.VanillaAmmoDisplay());
            uic.GunAmmoCountLabel.Text = _SB.ToString();
            return true;
        }
    }
}
