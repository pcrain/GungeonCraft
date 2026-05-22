namespace CwaffingTheGungy;

public class HolyWaterGun : CwaffGun
{
    public static string ItemName         = "Holy Water Gun";
    public static string ShortDescription = "Water, Gun, & Holy Soak";
    public static string LongDescription  = "Deals 8x damage to the Jammed. Killing a Jammed enemy reduces curse by 0.5.";
    public static string Lore             = "Rumored to have been used in exorcisms by the High Priest back while he was still the Low Priest. While the exact composition of the holy water is unknown, scientists have been able to reasonably ascertain the fluid contains koi pond water, primer, rat saliva, and moonshine. In any case, it has proven extremely effective at exorcising the Jammed and nauseating everyone else.";

    internal static GameObject _ExorcismParticleVFX = null;

    public static void Init()
    {
        Lazy.SetupGun<HolyWaterGun>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.BEAM, reloadTime: 1.0f, ammo: 100, audioFrom: Items.MegaDouser, defaultAudio: true)
          .AddToShop(ItemBuilder.ShopType.Cursula)
          .AddToShop(ItemBuilder.ShopType.Goopton)
          .InitProjectile(GunData.New(baseProjectile: Items.MegaDouser.Projectile(), clipSize: -1, shootStyle: ShootStyle.Beam,
            customClip: true, damage: 15.0f, speed: 50.0f, force: 15.0f, beamSprite: "holy_water_gun",
            beamFps: 20, beamEmission: 15f, beamInterpolate: false, beamStartIsMuzzle: true))
          .Attach<ExorcismJuice>();

        _ExorcismParticleVFX = VFX.Create("exorcism_particles", fps: 12, loops: false, emissivePower: 2);
    }
}

public class ExorcismJuice : MonoBehaviour
{
    private const float _HOLY_GOOP_RADIUS = 5f;
    private const float _JAMMED_DAMAGE_MULT = 4f;
    private const float _MASTERY_JAMMED_DAMAGE_MULT = 16f;

    private PlayerController _owner;
    private bool _mastered;

    private void Start()
    {
        Projectile proj = base.GetComponent<Projectile>();
        this._owner = proj.Owner as PlayerController;
        this._mastered = this._owner && this._owner.HasSynergy(Synergy.MASTERY_HOLY_WATER_GUN);
        proj.BlackPhantomDamageMultiplier = this._mastered ? _MASTERY_JAMMED_DAMAGE_MULT : _JAMMED_DAMAGE_MULT;
        proj.OnHitEnemy += ExorciseTheJammed;
    }

    private void ExorciseTheJammed(Projectile proj, SpeculativeRigidbody body, bool willKill)
    {
        if (!body || body.GetComponent<AIActor>() is not AIActor enemy)
            return;

        Lazy.PlaySoundUntilDeathOrTimeout(soundName: "exorcism_noises_intensify", source: enemy.gameObject, timer: 0.2f);
        if (UnityEngine.Random.value < 0.25f)
        {
            Vector2 finalpos = enemy.CenterPosition + Lazy.RandomVector();
            CwaffVFX.Spawn(HolyWaterGun._ExorcismParticleVFX, finalpos.ToVector3ZisY(-1f), Lazy.RandomEulerZ(),
                velocity: Lazy.RandomVector(0.5f), lifetime: 0.34f, fadeOutTime: 0.34f);
        }

        if (!willKill || !enemy.IsBlackPhantom || proj.GetComponent<BasicBeamController>() is not BasicBeamController beam)
            return;

        this._owner.ownerlessStatModifiers.Add(StatType.Curse.Add(-0.5f));
        this._owner.stats.RecalculateStats(this._owner);

        tk2dBaseSprite sprite = enemy.sprite.DuplicateInWorld(enemy.optionalPalette);
        sprite.ApplyShader(CwaffShaders.DesatShader, enemy.optionalPalette);
        sprite.renderer.material.SetFloat(CwaffVFX._SaturationId, 0f);
        sprite.renderer.material.SetFloat(CwaffVFX._FadeId, 1f);
        sprite.StartCoroutine(GhostlyDeath(sprite, beam.Direction));

        if (!this._mastered)
            return;

        this._owner.gameObject.Play("holy_sound");
        if (DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.HolyGoop) is DeadlyDeadlyGoopManager holyGooper)
            holyGooper.AddGoopCircle(enemy.CenterPosition, _HOLY_GOOP_RADIUS);
    }

    private static IEnumerator GhostlyDeath(tk2dBaseSprite sprite, Vector2 direction)
    {
        const float _FADE_TIME   = 2.5f;
        const float _DRIFT_SPEED = 1.5f;

        Vector3 velocity = (_DRIFT_SPEED * direction.normalized).ToVector3ZUp();
        sprite.gameObject.Play("ghost_soul_sound");
        for (float elapsed = 0f; elapsed < _FADE_TIME; elapsed += BraveTime.DeltaTime)
        {
            sprite.transform.position += velocity * BraveTime.DeltaTime;
            sprite.renderer.material.SetFloat(CwaffVFX._FadeId, 1f - (elapsed / _FADE_TIME));
            yield return null;
        }
        UnityEngine.Object.Destroy(sprite.gameObject);
    }
}

public class GameActorHolyGoopEffect : GameActorSpeedEffect
{
    public override void OnEffectApplied(GameActor actor, RuntimeGameActorEffectData effectData, float partialAmount = 1f)
    {
        base.OnEffectApplied(actor, effectData, partialAmount);
        if (actor is not PlayerController player)
            return;
        player.InfiniteAmmo.AddOverride("Holy Goop");
        Material[] array = player.SetOverrideShader(ShaderCache.Acquire("Brave/Internal/RainbowChestShader"));
        for (int i = 0; i < array.Length; i++)
            if (array[i] != null)
                array[i].SetFloat("_AllColorsToggle", 1f);
        player.healthHaver.IsVulnerable = false;
    }

    public override void OnEffectRemoved(GameActor actor, RuntimeGameActorEffectData effectData)
    {
        base.OnEffectRemoved(actor, effectData);
        if (actor is not PlayerController player)
            return;
        player.InfiniteAmmo.RemoveOverride("Holy Goop");
        player.ClearOverrideShader();
        player.healthHaver.IsVulnerable = true;
    }
}
