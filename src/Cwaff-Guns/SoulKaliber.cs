namespace CwaffingTheGungy;

public class SoulKaliber : CwaffGun
{
    public static string ItemName         = "Soul Kaliber";
    public static string ShortDescription = "Gundead or Alive";
    public static string LongDescription  = "Fires projectiles that give enemies the soul link status effect. All soul linked enemies receive damage when any one of them is hit.";
    public static string Lore             = "A traveling missionary of Kaliber was once rudely interrupted mid-sermon by a bandit army of sword-wielding heathens. With no weapons on hand to defend their congregation, the missionary prayed to the goddess for a firearm to deliver them from impending doom. Kaliber asked an acolyte to prepare and deliver one of her strongest guns; the acolyte, however, accidentally dropped the gun and its ammunition while loading it. The ammo rained down rather harmlessly on the bandits' heads, but by some miracle, the gun itself managed to bludgeon one of the bandits, knocking all of them out in the process.";

    internal static Color _SoulBlankColor = new Color(1f, 0.3f, 0.8f, 1f);

    public static void Init()
    {
        Lazy.SetupGun<SoulKaliber>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 1.1f, ammo: 444, shootFps: 24, reloadFps: 12,
            muzzleFrom: Items.BundleOfWands, fireAudio: "soul_kaliber_fire", reloadAudio: "soul_kaliber_reload")
          .InitProjectile(GunData.New(clipSize: 10, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic, damage: 1.0f, speed: 30.0f,
            sprite: "soul_kaliber_projectile", fps: 2, scale: 0.33f, anchor: Anchor.MiddleCenter, customClip: true))
          .Attach<SoulLinkProjectile>();
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        player.healthHaver.ModifyDamage += this.OnMightTakeDamage;
    }
    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        player.healthHaver.ModifyDamage -= this.OnMightTakeDamage;
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
            this.PlayerOwner.healthHaver.ModifyDamage -= this.OnMightTakeDamage;
        base.OnDestroy();
    }

    private static readonly List<AIActor> _Targets = new();
    private void OnMightTakeDamage(HealthHaver hh, HealthHaver.ModifyDamageEventArgs data)
    {
        const float _BASE_PROT_CHANCE      = 0.25f;
        const float _PER_CURSE_PROT_CHANCE = 0.05f;

        if (data == EventArgs.Empty || data.ModifiedDamage <= 0f || !hh.IsVulnerable)
            return; // if we weren't going to take damage anyway, nothing to do
        if (!this.PlayerOwner || this.PlayerOwner.CurrentRoom is not RoomHandler room)
            return; // no valid room to check for enemies
        if (!this.PlayerOwner.HasSynergy(Synergy.MASTERY_SOUL_KALIBER))
            return; // no mastery to trigger effect

        //NOTE: base chance for triggering == 1 - (1 - (0.25 + 0.05 * Curse))^(# of soul linked enemies)
        float protChance = _BASE_PROT_CHANCE + _PER_CURSE_PROT_CHANCE * Mathf.Clamp(PlayerStats.GetTotalCurse(), 0, 10);
        _Targets.Clear();
        foreach (AIActor enemy in room.SafeGetEnemiesInRoom())
            if (enemy && (UnityEngine.Random.value <= protChance) && enemy.gameObject.GetComponent<SoulLinkStatus>())
                _Targets.Add(enemy);
        if (_Targets.Count == 0)
            return;

        AIActor target = _Targets.ChooseRandom();
        target.healthHaver.ApplyDamage(100f, target.CenterPosition - this.PlayerOwner.CenterPosition, "Kaliber's Protection");
        data.ModifiedDamage = 0f;
        hh.TriggerInvulnerabilityPeriod();
        Lazy.DoDamagedFlash(hh);
        this.PlayerOwner.gameObject.Play("kaliber_protection_activate_sound");
        this.PlayerOwner.DoColorfulMiniBlank(_SoulBlankColor);
        this.PlayerOwner.DoColorfulMiniBlank(_SoulBlankColor, position: target.CenterPosition);

        for (int i = 0; i < 2; ++i)
            CwaffVFX.SpawnBurst(
                prefab           : SoulLinkStatus._SoulLinkOverheadVFX,
                numToSpawn       : 10,
                basePosition     : (i == 0) ? this.PlayerOwner.CenterPosition : target.CenterPosition,
                positionVariance : 1f,
                minVelocity      : 4f,
                velocityVariance : 1f,
                velType          : CwaffVFX.Vel.AwayRadial,
                lifetime         : 0.3f,
                startScale       : 0.5f,
                endScale         : 0.01f,
                emissiveColor    : Color.red,
                emissivePower    : 1f
              );
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
        if (!this._enemy || this._enemy.healthHaver is not HealthHaver hh || hh.IsDead)
            return false;

        hh.ApplyDamage(damage, Vector2.zero, "Soul Link", CoreDamageTypes.Magic, DamageCategory.Unstoppable, true, null, false);
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
