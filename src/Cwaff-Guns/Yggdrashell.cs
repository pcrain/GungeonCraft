namespace CwaffingTheGungy;

public class Yggdrashell : CwaffGun
{
    public static string ItemName         = "Yggdrashell";
    public static string ShortDescription = "The Gun of Life";
    public static string LongDescription  = "Fires vines that constrict enemies and absorb their life force, with vines increasing in strength as more life absorbed. Max absorption scales with the player's current number of hearts (or armor, for zero-health characters). At max absorption, provides one hit of Earth Armor that negates damage once. Absorbed life force is reset to zero upon getting hit or triggering Earth Armor.";
    public static string Lore             = "Crafted from the wood of the World Tree by Reloadin himself, this gun thrives off the life forces of those at both ends of its barrel. Its true strength manifests only when wielded by one possessing both great might and great fortitude.";

    private const float _PARTICLE_RATE          = 0.03f;
    private const float _LIFEFORCE_VALUE        = 100f;
    private const float _ACCUM_RATE             = 2f;
    private static readonly float[] _Thresholds = [0f, 1f * _LIFEFORCE_VALUE, 3f * _LIFEFORCE_VALUE, 6f * _LIFEFORCE_VALUE, 10f * _LIFEFORCE_VALUE];

    internal static Color _EarthBlankColor         = new Color(0.75f, 1.0f, 0.75f, 1f);
    internal static tk2dBaseSprite _HeartVFXSprite = null;
    internal static tk2dBaseSprite _ArmorVFXSprite = null;
    internal static GameObject _LeafVFX            = null;

    public float lifeForce                 = 0f;

    private bool _protectionActive           = false;
    private float _lastParticleTime          = 0f;

    public static void Init()
    {
        Lazy.SetupGun<Yggdrashell>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.S, gunClass: GunClass.BEAM, reloadTime: 0.0f, ammo: 600, shootFps: 14, reloadFps: 4,
            doesScreenShake: false, modulesAreTiers: true, attacksThroughWalls: true)
          .AssignGun(out Gun gun)
          .Volley.projectiles = new(){
            SetupMod(level: 1, gun: gun),
            SetupMod(level: 2, gun: gun),
            SetupMod(level: 3, gun: gun),
            SetupMod(level: 4, gun: gun),
            SetupMod(level: 5, gun: gun)
          };

        _HeartVFXSprite = VFX.Create("yggdrashell_heart_vfx").GetComponent<tk2dBaseSprite>();
        _ArmorVFXSprite = VFX.Create("yggdrashell_armor_vfx").GetComponent<tk2dBaseSprite>();
        _LeafVFX = VFX.Create("leaf_vfx", emissivePower: 100f, emissiveColour: Color.green);
    }

    [HarmonyPatch(typeof(GameUIHeartController), nameof(GameUIHeartController.ProcessHeartSpriteModifications))]
    private class YggdrashellShieldPatch
    {
        static void Postfix(GameUIHeartController __instance, PlayerController associatedPlayer)
        {
            if (associatedPlayer.GetGun<Yggdrashell>() is not Yggdrashell y)
                return;
            if (!y._protectionActive)
                return;

            __instance.m_currentFullHeartName  = "yggdrashell_heart_full_ui";
            __instance.m_currentHalfHeartName  = "yggdrashell_heart_half_ui";
            __instance.m_currentEmptyHeartName = "yggdrashell_heart_empty_ui";
            __instance.m_currentArmorName      = "yggdrashell_armor_ui";
        }
    }

    private static ProjectileModule SetupMod(int level, Gun gun)
    {
        Projectile projectile = Items._38Special.CloneProjectile(GunData.New(clipSize: -1, cooldown: 0.1f, shootStyle: ShootStyle.Beam,
          doBeamSetup: false, damage: 15f * level));
            projectile.AddRaidenBeamPrefab($"yggdrashell_beam_{level}", fps: 20, maxTargets: 1, targetOffscreen: true);
        ProjectileModule mod = new ProjectileModule().SetAttributes(GunData.New(gun: gun, clipSize: -1, cooldown: 0.1f, shootStyle: ShootStyle.Beam, ammoCost: 5, customClip: true));
        mod.projectiles = new(){projectile};
        return mod;
    }

    public void UpdateDamageDealt(float damageThisTick)
    {
        if (this.gun.GunPlayerOwner() is not PlayerController player)
            return;
        int effectiveHealth = Mathf.Max(1, Mathf.FloorToInt(player.ForceZeroHealthState ? player.healthHaver.currentArmor : player.healthHaver.currentHealth));
        float maxLifeForce = effectiveHealth * _LIFEFORCE_VALUE;
        this.lifeForce = Mathf.Min(this.lifeForce + damageThisTick * _ACCUM_RATE, maxLifeForce);
        if (!this._protectionActive && this.lifeForce >= maxLifeForce)
        {
            this._protectionActive = true;
            player.DoGenericItemActivation(this.PlayerOwner.ForceZeroHealthState ? _ArmorVFXSprite : _HeartVFXSprite, playSound: "yggdrashell_protection_ready_sound");
            player.gameObject.Play("yggdrashell_protection_ready_sound");
            CwaffVFX.SpawnBurst(
                prefab           : _LeafVFX,
                numToSpawn       : 50,
                anchorTransform  : player.sprite.transform,
                basePosition     : player.CenterPosition,
                positionVariance : 8f,
                velType          : CwaffVFX.Vel.InwardToCenter,
                rotType          : CwaffVFX.Rot.Random,
                lifetime         : 0.5f,
                startScale       : 1.0f,
                endScale         : 0.1f,
                randomFrame      : true,
                emissiveColor    : Color.green,
                emissivePower    : 100f
              );
        }
        int oldTier = this.gun.CurrentStrengthTier;
        this.gun.CurrentStrengthTier = _Thresholds.FirstLT(this.lifeForce) - 1;
        if (oldTier < this.gun.CurrentStrengthTier)
        {
            ClearCachedShootData(); // reset particle effects
            this.gun.gameObject.Play("yggdrashell_power_up_sound");
            this.gun.CeaseAttack();
            this.gun.Attack();
        }
    }

    public override void Update()
    {
        base.Update();
        bool shouldPlaySound = this.gun && this.gun.IsFiring;
        this.gun.LoopSoundIf(shouldPlaySound, "entangle_loop", loopPointMs: 1500, rewindAmountMs: 1500 - 1000);
        if (this.gun.IsFiring && (BraveTime.ScaledTimeSinceStartup - this._lastParticleTime) > _PARTICLE_RATE)
        {
            this._lastParticleTime = BraveTime.ScaledTimeSinceStartup;
            if (this.GetExtantBeam() is not CwaffRaidenBeamController beam)
                return;
            Vector2 gunAngle = this.gun.CurrentAngle.ToVector();
            CwaffVFX.SpawnBurst(
                prefab           : _LeafVFX,
                numToSpawn       : 2 + 2 * this.gun.CurrentStrengthTier,
                basePosition     : beam.GetPointOnMainBezier(UnityEngine.Random.value),
                positionVariance : 1f,
                baseVelocity     : 4f * gunAngle,
                velocityVariance : 4f,
                velType          : CwaffVFX.Vel.Random,
                rotType          : CwaffVFX.Rot.Random,
                lifetime         : 0.5f,
                startScale       : 1.0f,
                endScale         : 0.1f,
                randomFrame      : true
              );
        }
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        this.gun.CurrentStrengthTier = 0;
        player.OnReceivedDamage += this.OnReceivedDamage;
        player.healthHaver.ModifyDamage += this.OnMightTakeDamage;
    }

    private void OnReceivedDamage(PlayerController player)
    {
        this.lifeForce = 0;
        int oldTier = this.gun.CurrentStrengthTier;
        this.gun.CurrentStrengthTier = 0;
        if (oldTier == this.gun.CurrentStrengthTier)
            return;

        ClearCachedShootData(); // reset particle effects
        if (!this.gun.IsFiring)
            return;

        this.gun.CeaseAttack();
        this.gun.Attack();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        player.OnReceivedDamage -= this.OnReceivedDamage;
        player.healthHaver.ModifyDamage -= this.OnMightTakeDamage;
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
        {
            this.PlayerOwner.OnReceivedDamage -= this.OnReceivedDamage;
            this.PlayerOwner.healthHaver.ModifyDamage -= this.OnMightTakeDamage;
        }
        base.OnDestroy();
    }

    private void OnMightTakeDamage(HealthHaver hh, HealthHaver.ModifyDamageEventArgs data)
    {
        if (!this._protectionActive || !this.PlayerOwner)
            return;
        if (data == EventArgs.Empty || data.ModifiedDamage <= 0f || !hh.IsVulnerable)
            return; // if we weren't going to take damage anyway, nothing to do
        data.ModifiedDamage = 0f;
        hh.TriggerInvulnerabilityPeriod();
        Lazy.DoDamagedFlash(hh);
        this._protectionActive = false;
        this.PlayerOwner.DoColorfulBlank(_EarthBlankColor);
        this.PlayerOwner.gameObject.Play("yggdrashell_protection_activate_sound");
        CwaffVFX.SpawnBurst(
            prefab           : _LeafVFX,
            numToSpawn       : 50,
            basePosition     : this.PlayerOwner.CenterPosition,
            positionVariance : 1f,
            minVelocity      : 6f,
            velocityVariance : 2f,
            velType          : CwaffVFX.Vel.AwayRadial,
            rotType          : CwaffVFX.Rot.Random,
            lifetime         : 0.8f,
            startScale       : 1.0f,
            endScale         : 0.1f,
            randomFrame      : true,
            emissiveColor    : Color.green,
            emissivePower    : 10f
          );

        this.OnReceivedDamage(this.PlayerOwner);
    }

    public override void MidGameSerialize(List<object> data, int i)
    {
        base.MidGameSerialize(data, i);
        data.Add(this.lifeForce);
    }

    public override void MidGameDeserialize(List<object> data, ref int i)
    {
        base.MidGameDeserialize(data, ref i);
        this.lifeForce = (float)data[i++];
        this.gun.CurrentStrengthTier = _Thresholds.FirstLT(this.lifeForce) - 1;

        PlayerController player = this.PlayerOwner;
        int effectiveHealth = Mathf.Max(1, Mathf.FloorToInt(
            player.ForceZeroHealthState ? player.healthHaver.currentArmor : player.healthHaver.currentHealth));
        float maxLifeForce = effectiveHealth * _LIFEFORCE_VALUE;
        this._protectionActive = this.lifeForce >= maxLifeForce;
    }
}
