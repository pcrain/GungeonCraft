namespace CwaffingTheGungy;

public class PhotonCannon : CwaffGun
{
    public static string ItemName         = "Photon Cannon";
    public static string ShortDescription = "20 Minute Battery Life";
    public static string LongDescription  = "Fires a beam that continuously inflicts sunburn. Sunburn deals increasing damage over time the longer an enemy is afflicted. Enemies not afflicted with sunburn within the last second recover from the status completely.";
    public static string Lore             = "Via the controlled tunneling of massless particles through a cylindrical polymer at light speed, and the redirecting of those particles to a single point of contact through an amorphous silica-based substrate, this weapon is capable of transferring millions of femtojoules of raw energy to a designated target in seconds.";

    private const float _SOUND_TIMER         = 0.25f;

    internal const float _BRIGHTNESS          = 10.0f;
    internal const float _BRIGHTNESS_MASTERED = 50.0f;

    internal static GameObject _SweatVFX = null;

    private float _nextSoundTime = 0f;
    private EasyLight _glassLight = null;

    public static void Init()
    {
        Lazy.SetupGun<PhotonCannon>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.D, gunClass: GunClass.BEAM, reloadTime: 0.0f, ammo: 1200, continuousFire: true)
          .InitProjectile(GunData.New(baseProjectile: Items.Moonscraper.Projectile(), clipSize: -1, cooldown: 0.18f, //NOTE: inherit from Moonscraper for hitscan
            shootStyle: ShootStyle.Beam, damage: 3.0f, force: 0f, speed: -1f, ammoCost: 1, angleVariance: 0f,
            beamSprite: "photon_beam", beamFps: 60, beamChargeFps: 8, beamImpactFps: 30,
            beamLoopCharge: false, beamReflections: 0, beamChargeDelay: 0f, beamEmission: 50f))
          .Attach<MagnifyingRay>();
        _SweatVFX = VFX.Create("sweat_particle", fps: 16, anchor: Anchor.LowerCenter, emissivePower: 1f);
    }

    public override void Update()
    {
        base.Update();
        if (this.gun.IsFiring && this.gun.renderer.enabled)
        {
          if (!this._glassLight)
            this._glassLight = EasyLight.Create(parent: this.gun.barrelOffset, color: ExtendedColours.honeyYellow, radius: 1f,
              brightness: this.Mastered ? _BRIGHTNESS_MASTERED : _BRIGHTNESS);
        }
        else if (this._glassLight)
        {
          UnityEngine.Object.Destroy(this._glassLight.gameObject);
          this._glassLight = null;
        }
    }

    private void LateUpdate()
    {
        if (!this.gun.IsFiring)
        {
          base.gameObject.Play("photon_beam_sound_stop");
          return;
        }
        float now = BraveTime.ScaledTimeSinceStartup;
        if (this._nextSoundTime > now)
          return;
        this._nextSoundTime = now + _SOUND_TIMER;
        base.gameObject.Play("photon_beam_sound");
    }
}

public class MagnifyingRay : MonoBehaviour
{
    private BasicBeamController _beam;
    private EasyLight _light;
    private bool _mastered;

    public float magStrength = 0f;

    private void Start()
    {
        Projectile proj      = base.GetComponent<Projectile>();
        this._beam           = proj.gameObject.GetComponent<BasicBeamController>();
        this.magStrength     = proj.baseData.damage;
        proj.baseData.damage = 0f;
        this._mastered       = proj.Owner is PlayerController pc && pc.HasSynergy(Synergy.MASTERY_PHOTON_CANNON);
        proj.statusEffectsToApply.Add(this._mastered ? AntBurningEffect.DefaultPermanent : AntBurningEffect.Default);
    }

    private void Update()
    {
      if (!this._beam)
        return;
      if (!this._light)
        this._light = EasyLight.Create(
          parent     : this._beam.transform,
          pos        : this._beam.Origin + this._beam.Direction.normalized * this._beam.m_currentBeamDistance,
          color      : ExtendedColours.honeyYellow,
          radius     : 4f,
          brightness : this._mastered ? PhotonCannon._BRIGHTNESS_MASTERED : PhotonCannon._BRIGHTNESS);
      this._light.gameObject.transform.position = this._beam.Origin + this._beam.Direction.normalized * this._beam.m_currentBeamDistance;
    }
}

public class AntBurningEffect : GameActorEffect
{
    private const float _PERSIST_TIME = 1.0f;

    public static readonly AntBurningEffect Default = new AntBurningEffect() {
        duration         = _PERSIST_TIME,
        effectIdentifier = "AntBurningEffect",
        PlaysVFXOnActor  = true,
        OverheadVFX      = PhotonCannon._SweatVFX,
        stackMode        = GameActorEffect.EffectStackingMode.DarkSoulsAccumulate,
      };

    public static readonly AntBurningEffect DefaultPermanent = new AntBurningEffect() {
        duration         = 10000000,
        effectIdentifier = "AntBurningEffect",
        PlaysVFXOnActor  = true,
        OverheadVFX      = PhotonCannon._SweatVFX,
        stackMode        = GameActorEffect.EffectStackingMode.DarkSoulsAccumulate,
      };

    public override void OnDarkSoulsAccumulate(GameActor actor, RuntimeGameActorEffectData effectData, float partialAmount = 1f, Projectile sourceProjectile = null)
    {
        float magStrength = 1.0f;
        if (sourceProjectile && sourceProjectile.gameObject.GetComponent<MagnifyingRay>() is MagnifyingRay mr)
          magStrength = mr.magStrength;
        effectData.accumulator += partialAmount * magStrength * BraveTime.DeltaTime;
        effectData.elapsed = 0.0f; // reset duration
    }

    public override void EffectTick(GameActor actor, RuntimeGameActorEffectData effectData)
    {
        if (actor is not AIActor enemy)
            return;
        enemy.healthHaver.ApplyDamage(
            damage           : Mathf.Ceil(effectData.accumulator) * BraveTime.DeltaTime,
            direction        : Vector2.zero,
            sourceName       : this.effectIdentifier,
            damageTypes      : CoreDamageTypes.Fire,
            damageCategory   : DamageCategory.DamageOverTime,
            ignoreDamageCaps : true);
    }

    public override void OnEffectRemoved(GameActor actor, RuntimeGameActorEffectData effectData)
    {
        base.OnEffectRemoved(actor, effectData);
    }
}
