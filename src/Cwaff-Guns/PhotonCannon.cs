namespace CwaffingTheGungy;

public class PhotonCannon : CwaffGun
{
    public static string ItemName         = "Photon Cannon";
    public static string ShortDescription = "20 Minute Battery Life";
    public static string LongDescription  = "Fires a light beam that inflicts ever-increasing damage the longer it is focused on an enemy. Damage accumluation is reset after 1 second of not hitting an enemy.";
    public static string Lore             = "TBD";

    private const float _SOUND_TIMER = 0.25f;

    internal static GameObject _SweatVFX = null;

    private float _nextSoundTime = 0f;

    public static void Init()
    {
        Lazy.SetupGun<PhotonCannon>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.D, gunClass: GunClass.BEAM, reloadTime: 0.0f, ammo: 1200, continuousFire: true)
          .InitProjectile(GunData.New(baseProjectile: Items.Moonscraper.Projectile(), clipSize: -1, cooldown: 0.18f, //NOTE: inherit from Moonscraper for hitscan
            shootStyle: ShootStyle.Beam, damage: 2.5f, force: 0f, speed: -1f, ammoCost: 1, angleVariance: 0f,
            beamSprite: "photon_beam", beamFps: 60, beamChargeFps: 8, beamImpactFps: 30,
            beamLoopCharge: false, beamReflections: 0, beamChargeDelay: 0f, beamEmission: 50f))
          .Attach<MagnifyingRay>();
        _SweatVFX = VFX.Create("sweat_particle");
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
    private void Start()
    {
        Projectile proj = base.GetComponent<Projectile>();
        proj.statusEffectsToApply.Add(AntBurningEffect.Create(proj.baseData.damage));
        proj.baseData.damage = 0f;
    }
}

public class AntBurningEffect : GameActorEffect
{
    private const float _PERSIST_TIME = 1.0f;

    public float effectStrength = 0.0f;

    public static AntBurningEffect Create(float strength)
    {
      return new AntBurningEffect() {
        effectStrength   = strength,
        duration         = _PERSIST_TIME,
        effectIdentifier = "AntBurningEffect",
        stackMode        = GameActorEffect.EffectStackingMode.DarkSoulsAccumulate,
      };
    }

    public override void OnDarkSoulsAccumulate(GameActor actor, RuntimeGameActorEffectData effectData, float partialAmount = 1f, Projectile sourceProjectile = null)
    {
        int oldAccum = Mathf.CeilToInt(effectData.accumulator);
        effectData.accumulator += partialAmount * BraveTime.DeltaTime;
        if (Mathf.CeilToInt(effectData.accumulator) > oldAccum)
          CwaffVFX.SpawnBurst(
            prefab           : PhotonCannon._SweatVFX,
            numToSpawn       : oldAccum,
            basePosition     : actor.CenterPosition,
            positionVariance : 1.5f,
            baseVelocity     : new Vector2(0f, 2.5f),
            velocityVariance : 4f,
            velType          : CwaffVFX.Vel.AwayRadial,
            rotType          : CwaffVFX.Rot.Random,
            lifetime         : 0.3f,
            lifetimeVariance : 0.2f,
            emissivePower    : 1f,
            emissiveColor    : Color.cyan,
            startScale       : 1.0f,
            endScale         : 0.75f,
            height           : 4.0f
            );
        effectData.elapsed = 0.0f; // reset duration
    }

    public override void EffectTick(GameActor actor, RuntimeGameActorEffectData effectData)
    {
        if (actor is not AIActor enemy)
            return;
        float dosage = effectStrength * effectData.accumulator;
        enemy.healthHaver.ApplyDamage(
            damage           : dosage * BraveTime.DeltaTime,
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
