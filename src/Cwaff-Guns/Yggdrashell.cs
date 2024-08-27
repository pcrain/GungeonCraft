namespace CwaffingTheGungy;

public class Yggdrashell : CwaffGun
{
    public static string ItemName         = "Yggdrashell";
    public static string ShortDescription = "The Gun of Life";
    public static string LongDescription  = "Fires vines that constrict enemies and absorb their life force, with vines increasing in strength as more life absorbed. Max absorption scales with the player's current number of hearts (or armor, for zero-health characters). At max absorption, provides one hit of Earth Armor that negates damage once. Absorbed life force is reset to zero upon getting hit or triggering Earth Armor.";
    public static string Lore             = "Crafted from the wood of the World Tree by Reloadin himself, this gun thrives off the life forces of those at both ends of its barrel. Its true strength manifests only when wielded by one possessing both great might and great fortitude.";

    private const float _PARTICLE_RATE          = 0.03f;
    private const float _LIFEFORCE_VALUE        = 100f;
    private static readonly float[] _Thresholds = [0f, 1f * _LIFEFORCE_VALUE, 3f * _LIFEFORCE_VALUE, 6f * _LIFEFORCE_VALUE, 10f * _LIFEFORCE_VALUE];

    internal static tk2dBaseSprite _HeartVFXSprite = null;
    internal static tk2dBaseSprite _ArmorVFXSprite = null;
    internal static GameObject _LeafVFX            = null;

    private uint _soundId                    = 0;
    private int _kills                       = 0;
    private float _lifeForce                 = 0f;
    private bool _protectionActive           = false;
    private float _lastParticleTime          = 0f;
    private ModuleShootData _cachedShootData = null;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Yggdrashell>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.S, gunClass: GunClass.BEAM, reloadTime: 0.0f, ammo: 600, shootFps: 14, reloadFps: 4,
                doesScreenShake: false, modulesAreTiers: true);
            gun.CanAttackThroughObjects = true;

        gun.Volley.projectiles = new(){
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
          doBeamSetup: false, damage: 10f * level));
            projectile.AddRaidenBeamPrefab($"yggdrashell_beam_{level}", fps: 20, maxTargets: 1, targetOffscreen: true);
        ProjectileModule mod = new ProjectileModule().SetAttributes(GunData.New(gun: gun, clipSize: -1, cooldown: 0.1f, shootStyle: ShootStyle.Beam, ammoCost: 3, customClip: true));
        mod.projectiles = new(){projectile};
        return mod;
    }

    public void UpdateDamageDealt(float damageThisTick)
    {
        if (this.gun.GunPlayerOwner() is not PlayerController player)
            return;
        int effectiveHealth = Mathf.Max(1, Mathf.FloorToInt(player.ForceZeroHealthState ? player.healthHaver.currentArmor : player.healthHaver.currentHealth));
        float maxLifeForce = effectiveHealth * _LIFEFORCE_VALUE;
        this._lifeForce = Mathf.Min(this._lifeForce + damageThisTick, maxLifeForce);
        if (!this._protectionActive && this._lifeForce >= maxLifeForce)
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
        this.gun.CurrentStrengthTier = _Thresholds.FirstLT(this._lifeForce) - 1;
        if (oldTier < this.gun.CurrentStrengthTier)
        {
            this._cachedShootData = null; // reset particle effects
            this.gun.gameObject.Play("yggdrashell_power_up_sound");
            this.gun.CeaseAttack();
            this.gun.Attack();
        }
    }

    public override void Update()
    {
        base.Update();
        bool shouldPlaySound = this.gun && this.gun.IsFiring;
        if (shouldPlaySound && this._soundId == 0)
            this._soundId = this.gun.LoopSound("entangle_loop", loopPointMs: 1500, rewindAmountMs: 1500 - 1000);
        else if (!shouldPlaySound && this._soundId > 0)
        {
            AkSoundEngine.StopPlayingID(this._soundId);
            this._soundId = 0;
        }
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

    private CwaffRaidenBeamController GetExtantBeam()
    {
        if (_cachedShootData == null)
        {
            if (!this.gun || !this.gun.IsFiring || this.gun.m_moduleData == null || this.gun.DefaultModule == null)
                return null;
            if (!this.gun.m_moduleData.TryGetValue(this.gun.DefaultModule, out ModuleShootData data))
                return null;
            this._cachedShootData = data;
        }
        return this._cachedShootData.beam as CwaffRaidenBeamController;
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
        this._lifeForce = 0;
        int oldTier = this.gun.CurrentStrengthTier;
        this.gun.CurrentStrengthTier = 0;
        if (oldTier == this.gun.CurrentStrengthTier)
            return;

        this._cachedShootData = null; // reset particle effects
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
        data.ModifiedDamage = 0f;
        hh.TriggerInvulnerabilityPeriod();
        Lazy.DoDamagedFlash(hh);
        this._protectionActive = false;
        this.PlayerOwner.ForceBlank();
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
            emissivePower    : 100f
          );

        this.OnReceivedDamage(this.PlayerOwner);
    }
}
