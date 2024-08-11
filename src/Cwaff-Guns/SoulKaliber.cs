namespace CwaffingTheGungy;

public class SoulKaliber : CwaffGun
{
    public static string ItemName         = "Soul Kaliber";
    public static string ShortDescription = "Gundead or Alive";
    public static string LongDescription  = "Fires projectiles that give enemies the soul link status effect. All soul linked enemies receive damage when any one of them is hit.";
    public static string Lore             = "A traveling missionary of Kaliber was once rudely interrupted mid-sermon by a bandit army of sword-wielding heathens. With no weapons on hand to defend their congregation, the missionary prayed to the goddess for a firearm to deliver them from impending doom. Kaliber asked an acolyte to prepare and deliver one of her strongest guns; the acolyte, however, accidentally dropped the gun and its ammunition while loading it. The ammo rained down rather harmlessly on the bandits' heads, but by some miracle, the gun itself managed to bludgeon one of the bandits, knocking all of them out in the process.";

    public static void Init()
    {
        Lazy.SetupGun<SoulKaliber>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 1.1f, ammo: 250, shootFps: 24, reloadFps: 12,
            muzzleFrom: Items.BundleOfWands, fireAudio: "soul_kaliber_fire", reloadAudio: "soul_kaliber_reload")
          .InitProjectile(GunData.New(clipSize: 10, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic, damage: 1.0f, speed: 30.0f,
            sprite: "soul_kaliber_projectile", fps: 2, scale: 0.33f, anchor: Anchor.MiddleCenter))
          .Attach<SoulLinkProjectile>();
    }
}

public class SoulLinkProjectile : MonoBehaviour
{
    private void Start()
    {
        Projectile proj = base.GetComponent<Projectile>();
        proj.sprite.SetGlowiness(glowAmount: 100f, glowColor: Color.magenta);
        proj.OnHitEnemy += this.OnHitEnemy;
    }

    private void OnHitEnemy(Projectile p, SpeculativeRigidbody enemy, bool _)
    {
        p.gameObject.Play("soul_kaliber_impact");
        enemy.aiActor.gameObject.GetOrAddComponent<SoulLinkStatus>();
    }
}

public class SoulLinkStatus : MonoBehaviour
{
    public static GameActorHealthEffect StandardSoulLinkEffect;

    private const int   _NUM_HIT_PARTICLES = 12;
    private const float _SOUL_PART_SPEED   = 3f;
    private const float _MAX_VFX_RATE      = 0.10f;

    internal static VFXPool _SoulLinkHitVFXPool      = null;
    internal static GameObject _SoulLinkHitVFX       = null;
    internal static GameObject _SoulLinkOverheadVFX  = null;
    internal static GameObject _SoulLinkSoulVFX      = null;

    private static bool _SoulLinkEffectHappening = false;

    private AIActor _enemy;
    private OrbitalEffect _orbitalEffect = null;
    private float _lastVfxTime = 0f;

    public static void Init()
    {
        _SoulLinkHitVFXPool    = VFX.CreatePoolFromVFXGameObject(Items.MagicLamp.AsGun().DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);
        _SoulLinkHitVFX        = _SoulLinkHitVFXPool.effects[0].effects[0].effect.gameObject;
        _SoulLinkOverheadVFX   = VFX.Create("soul_link_particle",
            fps: 16, loops: true, anchor: Anchor.LowerCenter, scale: 0.3f, emissivePower: 100);
        _SoulLinkSoulVFX       = VFX.Create("soul_link_soul",
            fps: 5, loops: true, anchor: Anchor.MiddleCenter, scale: 0.3f, emissivePower: 200);
        StandardSoulLinkEffect = new GameActorHealthEffect
        {
            duration                 = 60,
            effectIdentifier         = "SoulLink",
            resistanceType           = EffectResistanceType.None,
            DamagePerSecondToEnemies = 0,
            ignitesGoops             = false,
            OverheadVFX              = null,
            AffectsEnemies           = true,
            AffectsPlayers           = false,
            AppliesOutlineTint       = true,
            PlaysVFXOnActor          = false,
            AppliesTint              = false,
            AppliesDeathTint         = false,
        };
    }

    private void Start()
    {
        this._enemy         = base.GetComponent<AIActor>();
        this._orbitalEffect = this._enemy.gameObject.AddComponent<OrbitalEffect>();
            this._orbitalEffect.SetupOrbitals(vfx: _SoulLinkOverheadVFX, numOrbitals: 3, rps: 0.5f, isEmissive: true);

        this._enemy.ApplyEffect(SoulLinkStatus.StandardSoulLinkEffect);
        this._enemy.healthHaver.ModifyDamage += this.OnTakeDamage;
        this._enemy.healthHaver.OnPreDeath +=
            (_) => this._orbitalEffect.HandleEnemyDied();  // deal with some despawn gliches
    }

    private void OnTakeDamage(HealthHaver hh, HealthHaver.ModifyDamageEventArgs data)
    {
        // prevent ourselves from taking damage in an infinite loop from other soul-linked enemies
        if (_SoulLinkEffectHappening)
            return;
        try
        {
            _SoulLinkEffectHappening = true;

            AIActor enemy = hh.aiActor;
            if (!enemy)
                return;
            List<AIActor> activeEnemies = enemy.GetAbsoluteParentRoom().SafeGetEnemiesInRoom();

            bool shouldPlaySound = false;
            for (int i = activeEnemies.Count - 1; i >=0; --i)
            {
                AIActor otherEnemy = activeEnemies[i];
                if (!(otherEnemy.IsHostileAndNotABoss()))
                    continue; // we don't care about harmless enemies
                if (enemy == otherEnemy)
                    continue; // don't apply damage to ourselves
                if (otherEnemy.gameObject.GetComponent<SoulLinkStatus>() is not SoulLinkStatus soulLink)
                    continue;
                shouldPlaySound = soulLink.ShareThePain(data.ModifiedDamage);
            }
            if (shouldPlaySound)
                hh.gameObject.Play("soul_kaliber_drain");
        }
        finally
        {
            _SoulLinkEffectHappening = false;
        }
    }

    public bool ShareThePain(float damage)
    {
        HealthHaver hh = this._enemy.healthHaver;
        float curHealth = hh.currentHealth;
        float maxHealth = hh.maximumHealth;
        hh.ApplyDamage(damage, new Vector2(0f,0f), "Soul Link",
            CoreDamageTypes.Magic, DamageCategory.Unstoppable,
            true, null, false);
        hh.knockbackDoer.ApplyKnockback(new Vector2(0f,0f), 2f);

        if (this._lastVfxTime + _MAX_VFX_RATE > BraveTime.ScaledTimeSinceStartup)
            return false; // don't play any sounds

        this._lastVfxTime = BraveTime.ScaledTimeSinceStartup;
        CwaffVFX.SpawnBurst(prefab: _SoulLinkSoulVFX, numToSpawn: _NUM_HIT_PARTICLES, basePosition: this._enemy.CenterPosition,
            positionVariance: 1f, baseVelocity: _SOUL_PART_SPEED * Vector2.up, lifetime: 0.5f, fadeOutTime: 0.5f,
            emissivePower: 50f, emissiveColor: Color.white);
        return true; // now we can play sounds
    }
}
