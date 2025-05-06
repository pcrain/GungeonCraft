namespace CwaffingTheGungy;

public class KiBlast : CwaffGun
{
    public static string ItemName         = "Ki Blast";
    public static string ShortDescription = "Dragunball Z";
    public static string LongDescription  = "Fires alternating ki blasts that may be reflected by sufficiently strong enemies. Reloading reflects the nearest ki blast back at the enemy, amplifying the damage after every successive reflect. Reflected projectiles are not affected by DPS caps.";
    public static string Lore             = "Harnessing one's ki is an art form that has been taught for millennia, yet mastered by exceptionally few. Among the already small number of those able to effectively harness ki, even fewer have successfully weaponized it, and among them, only one has brought that power to the Gungeon. That Gungeoneer unfortunately got absolutely incinerated by a flamethrower they didn't see jutting out of the wall, but to this very day, the ki they released upon their untimely demise occasionally manifests itself as a weapon for others passing through the Gungeon.";

    internal static string _FireLeftAnim;
    internal static string _FireRightAnim;
    internal static string _KameAnim;
    internal static string _HameAnim;

    internal const float _DAMAGE_MULT_CAP = 8.0f;

    private const float _KI_REFLECT_RANGE = 3.0f;
    private const float _KI_REFLECT_RANGE_SQR = _KI_REFLECT_RANGE * _KI_REFLECT_RANGE;
    private const float _MAX_RECHARGE_TIME = 0.5f;
    private const float _MIN_RECHARGE_TIME = 0.05f;
    private const float _RECHARGE_DECAY = 0.9f;
    private const float _CHARGE_START_TIME = 0.5f;
    private const float _CHARGE_FINISH_TIME = 4.5f;
    private const float _CHARGE_TOTAL_TIME = _CHARGE_FINISH_TIME - _CHARGE_START_TIME;
    private const float _CHARGE_SOUND_DELAY = 0.25f;

    private Vector2 _currentTarget = Vector2.zero;

    public float nextKiBlastSign = 1;  //1 to deviate right, -1 to deviate left
    private float _rechargeTimer = 0.0f;
    private float _nextRecharge = 0.0f;
    private int _nextChargeSound = 1;
    private float _timeCharging = 0.0f;

    public static void Init()
    {
        Lazy.SetupGun<KiBlast>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.BEAM, reloadTime: 0.0f, ammo: 20, canGainAmmo: false, idleFps: 10, shootFps: 24,
            muzzleVFX: "muzzle_ki_blast", muzzleFps: 30, muzzleScale: 0.5f, muzzleAnchor: Anchor.MiddleLeft)
          .Attach<KiBlastAmmoDisplay>()
          .Attach<Unthrowable>()
          .AddToShop(ModdedShopType.Boomhildr)
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(clipSize: -1, cooldown: 0.1f, shootStyle: ShootStyle.Charged, chargeTime: 0.0f,
            customClip: true, damage: 4.0f, range: 1000.0f, speed: 50.0f, sprite: "ki_blast", fps: 12, scale: 0.25f,
            ignoreDamageCaps: true, hitSound: "ki_blast_explode_sound", spawnSound: "ki_blast_sound",
            glowAmount: 50.0f, lightStrength: 2.1f, lightRange: 2.4f, lightColor: Color.cyan))
          .SetAllImpactVFX(VFX.CreatePool("ki_explosion", fps: 20, loops: false, scale: 0.5f, emissivePower: 10f))
          .Attach<EasyTrailBullet>(trail => {
            trail.TrailPos   = trail.transform.position;
            trail.StartWidth = 0.2f;
            trail.EndWidth   = 0f;
            trail.LifeTime   = 0.1f;
            trail.BaseColor  = Color.cyan;
            trail.EndColor   = Color.cyan; })
          .Attach<ArcTowardsTargetBehavior>()
          .Attach<KiBlastBehavior>(); //NOTE: KiBlastBehavior must init before ArcTowardsTargetBehavior so we can call Setup() before Start()

        // Kamehameha projectile
        gun.AddSynergyModules(Synergy.MASTERY_KI_BLAST, (new ProjectileModule().InitSingleProjectileModule(GunData.New(
          gun: gun, baseProjectile: Items.Moonscraper.Projectile(), clipSize: -1, cooldown: 0.18f, //NOTE: inherit from Moonscraper for hitscan
          shootStyle: ShootStyle.Beam, damage: 100f, speed: -1f, customClip: true, ammoCost: 1, angleVariance: 0f,
          beamSprite: "kamehameha", beamFps: 30, beamChargeFps: 20, beamImpactFps: 30, beamDissipateFps: 30,
          beamLoopCharge: true, beamReflections: 0, beamChargeDelay: _CHARGE_FINISH_TIME, beamEmission: 160f, ignoreDamageCaps: true))));

        _FireLeftAnim  = gun.shootAnimation;
        _FireRightAnim = gun.QuickUpdateGunAnimation("fire_alt", returnToIdle: true, fps: 24);
        _KameAnim = gun.QuickUpdateGunAnimation("kame");
        _HameAnim = gun.QuickUpdateGunAnimation("hameha");
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        gun.shootAnimation = (this.nextKiBlastSign > 0 ? _FireRightAnim : _FireLeftAnim);
        this._rechargeTimer = 0.0f;
        this._nextRecharge = _MAX_RECHARGE_TIME;
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        if (this.PlayerOwner is not PlayerController player)
            return;
        player.ToggleGunRenderers(false, ItemName);
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        if (this.PlayerOwner is not PlayerController player)
            return;
        player.ToggleGunRenderers(true, ItemName);
    }

    public override void OnInitializedWithOwner(GameActor actor)
    {
        base.OnInitializedWithOwner(actor);
        if (actor is not PlayerController player)
            return;
        player.ToggleGunRenderers(false, ItemName);
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        player.ToggleGunRenderers(true, ItemName);
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
            this.PlayerOwner.ToggleGunRenderers(true, ItemName);
        base.OnDestroy();
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        float closestDistanceSqr = _KI_REFLECT_RANGE_SQR;
        KiBlastBehavior closestBlast = null;
        foreach (Projectile p in StaticReferenceManager.AllProjectiles)
        {
            if (!p || !p.isActiveAndEnabled)
                continue;
            KiBlastBehavior k = p.gameObject.GetComponent<KiBlastBehavior>();
            if (!k || !k.reflected)
                continue;
            float sqrDist = (player.CenterPosition - p.SafeCenter).sqrMagnitude;
            if (sqrDist > closestDistanceSqr)
                continue;
            closestDistanceSqr = sqrDist;
            closestBlast = k;
        }
        if (closestBlast)
            closestBlast.ReturnFromPlayer(player);
    }

    private void UpdateIdleAnimation(string idleAnimation = null, int frame = -1)
    {
        string curClipName = this.gun.spriteAnimator.CurrentClip.name;
        if (curClipName != this.gun.idleAnimation && curClipName != _KameAnim && curClipName != _HameAnim)
            return;
        this.gun.spriteAnimator.PlayIfNotPlaying(idleAnimation ?? this.gun.idleAnimation);
        if (frame >= 0)
            this.gun.spriteAnimator.PlayFromFrame(frame);
    }

    private bool HandleKamehameha()
    {
        this.percentSpeedWhileFiring = 1f;
        if (!this.gun.IsFiring)
            _nextChargeSound = 1;
        if (this.gun.Volley.projectiles.Count < 2
          || this.gun.m_moduleData == null
          || !this.gun.m_moduleData.TryGetValue(this.gun.Volley.projectiles[1], out ModuleShootData msd))
            return false;
        if (msd == null || msd.beam is not BasicBeamController beam)
            return false;

        bool isChargingOrFiring = false;
        if (beam.State == BeamState.Charging)
        {
            this._timeCharging += BraveTime.DeltaTime;
            this.gun.LoopSoundIf(this._timeCharging >= _CHARGE_SOUND_DELAY, "kamehameha_charge_sound");
             //NOTE: first frame of charge animation is blank to avoid 1-frame delay on making it invisible
            bool preCharge = beam.m_chargeTimer < _CHARGE_START_TIME;
            if (beam.m_beamMuzzleAnimator && beam.m_beamMuzzleAnimator.sprite)
                beam.m_beamMuzzleAnimator.sprite.renderer.enabled = !preCharge;
            if (preCharge)
                _nextChargeSound = 1;
            else
            {
                isChargingOrFiring = true;
                // evenly space voice lines
                int chargeSound = Mathf.FloorToInt(5f * (beam.m_chargeTimer - _CHARGE_START_TIME) / _CHARGE_TOTAL_TIME);
                if (chargeSound >= _nextChargeSound && _nextChargeSound <= 5)
                    base.gameObject.Play($"kamehameha_charge_{_nextChargeSound++}_sound");
                UpdateIdleAnimation(_KameAnim, _nextChargeSound - 1);
            }
            this.percentSpeedWhileFiring = 1f - beam.m_chargeTimer / beam.chargeDelay;
            if (beam.m_beamMuzzleAnimator && beam.m_beamMuzzleAnimator.sprite)
            {
                beam.m_beamMuzzleAnimator.sprite.SetGlowiness(500f * beam.m_chargeTimer / beam.chargeDelay);
                beam.m_beamMuzzleAnimator.ClipFps = 20f + 8f * beam.m_chargeTimer;
            }
        }
        else
        {
            bool isFiring = isChargingOrFiring = beam.State == BeamState.Firing;
            if (isFiring && _nextChargeSound == 5)
                base.gameObject.Play($"kamehameha_charge_{_nextChargeSound++}_sound");
            this.gun.LoopSoundIf(isFiring, "kamehameha_fire_sound",
              loopPointMs: 1050, rewindAmountMs: 300, finishNaturally: true);
            this.percentSpeedWhileFiring = 0f;
            _nextChargeSound = 1;
            UpdateIdleAnimation(isFiring ? _HameAnim : gun.idleAnimation);
        }

        if (isChargingOrFiring && this.gun.m_moduleData.TryGetValue(this.gun.Volley.projectiles[0], out ModuleShootData kiBlastMsd))
            kiBlastMsd.chargeFired = true; // disable the normal ki blast projectile now that the beam is active
        return true;
    }

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner || !this.gun)
            return;
        if (!HandleKamehameha())
        {
            this._timeCharging = 0.0f;
            UpdateIdleAnimation(); // reset idle animation to default if we're not actively charging or firing a kamehameha
        }
        this.PlayerOwner.ToggleGunRenderers(!this.gun.isActiveAndEnabled, ItemName);
        if (this.gun.CurrentAmmo >= this.gun.AdjustedMaxAmmo)
            return;
        this._rechargeTimer += BraveTime.DeltaTime;
        if (this._rechargeTimer < this._nextRecharge)
            return;
        this._rechargeTimer -= this._nextRecharge;
        this._nextRecharge = Mathf.Max(this._nextRecharge * _RECHARGE_DECAY, _MIN_RECHARGE_TIME);
        this.gun.ammo = Math.Min(this.gun.ammo + 1, this.gun.AdjustedMaxAmmo);
    }
}

public class KiBlastAmmoDisplay : CustomAmmoDisplay
{
    private Gun              _gun     = null;
    private KiBlast          _kiblast = null;
    private PlayerController _owner   = null;

    private void Start()
    {
        this._gun       = base.GetComponent<Gun>();
        this._kiblast   = this._gun.GetComponent<KiBlast>();
        this._owner     = this._gun.CurrentOwner as PlayerController;
    }

    public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
    {
        if (!this._owner)
            return false;

        uic.SetAmmoCountLabelColor(Color.cyan);
        uic.GunAmmoCountLabel.Text = $"{this._gun.CurrentAmmo} Ki";
        return true;
    }
}

public class KiBlastBehavior : MonoBehaviour
{
    private const float _SCALING = 1.5f;

    private static float _MinAngleVariance       = 10f;
    private static float _MaxAngleVariance       = 60f;
    private static float _MinReflectableLifetime = 0.15f;
    private static SlashData _BasicSlashData     = null;

    private Projectile _projectile        = null;
    private PlayerController _owner       = null;
    private float _timeSinceLastReflect   = 0;
    private int _numReflections           = 0;
    private float _startingDamage         = 0;
    private ArcTowardsTargetBehavior _arc = null;

    public bool reflected = false;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        if (this._projectile.Owner is not PlayerController pc)
            return;
        this._owner = pc;

        _BasicSlashData ??= new SlashData();
        this._startingDamage = this._projectile.baseData.damage;
        this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;

        float angle = 0;
        if (this._owner.CurrentGun.GetComponent<KiBlast>() is KiBlast k)
        {
            angle = Mathf.Max(UnityEngine.Random.value * pc.AccuracyMult() * _MaxAngleVariance, _MinAngleVariance) * k.nextKiBlastSign;
            k.nextKiBlastSign *= -1;
        }
        this._arc = base.GetComponent<ArcTowardsTargetBehavior>();
        this._arc.Setup(arcAngle: angle, maxSecsToReachTarget: 0.5f / pc.ProjSpeedMult(), minSpeed: 15.0f);

        this._projectile.gameObject.Play("ki_blast_return_sound_stop_all");
        this._projectile.gameObject.PlayUnique("ki_blast_sound");
    }

    private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (this.reflected || this._timeSinceLastReflect < _MinReflectableLifetime)
            return; //don't want enemies to just be able to spam reflect

        AIActor enemy = otherRigidbody.GetComponent<AIActor>();
        if (!enemy || !enemy.healthHaver || this._projectile.baseData.damage >= enemy.healthHaver.GetCurrentHealth())
            return; //don't reflect if our target is not an enemy or if the blast is stronger than them

        // Apply damage to the enemy
        enemy.healthHaver.ApplyDamage(this._projectile.baseData.damage, this._projectile.Direction, "Ki Blast",
            CoreDamageTypes.None, DamageCategory.Collision, false, null, true);
        if (enemy.healthHaver.knockbackDoer is KnockbackDoer kbd)
            kbd.ApplyKnockback(this._projectile.Direction, this._projectile.baseData.force);

        // Skip the normal collision
        PhysicsEngine.SkipCollision = true;

        // Make the projectile belong to the enemy and return it towards the player
        this.reflected        = true;
        Projectile p          = this._projectile;
            p.Owner               = enemy;
            p.collidesWithPlayer  = true;
            p.collidesWithEnemies = false;
        this._arc.SetNewTarget(this._owner.CenterPosition);

        // Update sounds and animations
        EasyTrailBullet trail = p.gameObject.GetComponent<EasyTrailBullet>();
            trail.BaseColor = Color.yellow;
            trail.EndColor = Color.yellow;
            trail.UpdateTrail();

        this._projectile.gameObject.Play("ki_blast_sound_stop_all");
        this._projectile.gameObject.PlayUnique("ki_blast_return_sound");
    }

    public void ReturnFromPlayer(PlayerController player)
    {
        if (!this.reflected || !this._projectile)
            return;
        if (this._projectile.Owner is not AIActor enemy)
            return;
        if (!player || player.CurrentRoom is not RoomHandler room)
            return;

        ++this._numReflections;
        this.reflected = false;
        this._timeSinceLastReflect = 0.0f;
        this._projectile.baseData.damage = this._startingDamage * Mathf.Min(KiBlast._DAMAGE_MULT_CAP, Mathf.Pow(_SCALING, this._numReflections));

        this._projectile.Owner = player;
        this._projectile.collidesWithPlayer = false;
        this._projectile.collidesWithEnemies = true;

        EasyTrailBullet trail = this._projectile.gameObject.GetComponent<EasyTrailBullet>();
            trail.BaseColor = Color.cyan;
            trail.EndColor = Color.cyan;
            trail.UpdateTrail();

        this._projectile.gameObject.Play("ki_blast_sound_stop_all");
        this._projectile.gameObject.PlayUnique("ki_blast_return_sound");
        int enemiesToCheck = 10;
        bool success = true;
        while (!enemy || !enemy.isActiveAndEnabled || !enemy.healthHaver || enemy.healthHaver.IsDead)
        {
            if (--enemiesToCheck < 0)
            {
                success = false;
                break;
            }
            enemy = room.GetRandomActiveEnemy(true);
        }
        float angle = success ? (enemy.CenterPosition - player.CenterPosition).ToAngle() : Lazy.RandomAngle();
        this._arc.SetNewTarget(success ? enemy.CenterPosition : (
            player.CenterPosition + player.m_currentGunAngle.ToVector(UnityEngine.Random.Range(1f, 5f))));
        SlashDoer.DoSwordSlash(
            position        : player.CenterPosition,
            angle           : angle,
            owner           : this._projectile.Owner,
            slashParameters : _BasicSlashData);
    }

    private void Update()
    {
        this._timeSinceLastReflect += BraveTime.DeltaTime;
    }
}
