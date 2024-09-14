namespace CwaffingTheGungy;

public class KiBlast : CwaffGun
{
    public static string ItemName         = "Ki Blast";
    public static string ShortDescription = "Dragunball Z";
    public static string LongDescription  = "Fires alternating ki blasts that may be reflected by sufficiently strong enemies. Reloading reflects the nearest ki blast back at the enemy, amplifying the damage after every successive reflect. Reflected projectiles are not affected by DPS caps.";
    public static string Lore             = "Harnessing one's ki is an art form that has been taught for millennia, yet mastered by exceptionally few. Among the already small number of those able to effectively harness ki, even fewer have successfully weaponized it, and among them, only one has brought that power to the Gungeon. That Gungeoneer unfortunately got absolutely incinerated by a flamethrower they didn't see jutting out of the wall, but to this very day, the ki they released upon their untimely demise occasionally manifests itself as a weapon for others passing through the Gungeon.";

    internal static string _FireLeftAnim;
    internal static string _FireRightAnim;

    private const float _KI_REFLECT_RANGE = 3.0f;
    private const float _KI_REFLECT_RANGE_SQR = _KI_REFLECT_RANGE * _KI_REFLECT_RANGE;
    private const float _MAX_RECHARGE_TIME = 0.5f;
    private const float _MIN_RECHARGE_TIME = 0.05f;
    private const float _RECHARGE_DECAY = 0.9f;

    private Vector2 _currentTarget = Vector2.zero;

    public float nextKiBlastSign = 1;  //1 to deviate right, -1 to deviate left
    private float _rechargeTimer = 0.0f;
    private float _nextRecharge = 0.0f;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<KiBlast>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.BEAM, reloadTime: 0.0f, ammo: 20, canGainAmmo: false, idleFps: 10,
            fireAudio: "ki_blast_sound", muzzleVFX: "muzzle_ki_blast", muzzleFps: 30, muzzleScale: 0.5f, muzzleAnchor: Anchor.MiddleLeft)
          .Attach<KiBlastAmmoDisplay>()
          .AddToShop(ModdedShopType.Boomhildr);

        _FireLeftAnim  = gun.shootAnimation;
        _FireRightAnim = gun.QuickUpdateGunAnimation("fire_alt", returnToIdle: true);
        gun.SetAnimationFPS(_FireLeftAnim, 24);
        gun.SetAnimationFPS(_FireRightAnim, 24);

        gun.InitProjectile(GunData.New(clipSize: -1, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic,
          customClip: true, damage: 4.0f, range: 1000.0f, speed: 50.0f, sprite: "ki_blast", fps: 12, scale: 0.25f,
          anchor: Anchor.MiddleCenter, ignoreDamageCaps: true, hitSound: "ki_blast_explode_sound"))
        .SetAllImpactVFX(VFX.CreatePool("ki_explosion", fps: 20, loops: false, scale: 0.5f))
        .Attach<EasyTrailBullet>(trail => {
          trail.TrailPos   = trail.transform.position;
          trail.StartWidth = 0.2f;
          trail.EndWidth   = 0f;
          trail.LifeTime   = 0.1f;
          trail.BaseColor  = Color.cyan;
          trail.EndColor   = Color.cyan; })
        .Attach<ArcTowardsTargetBehavior>()
        .Attach<KiBlastBehavior>(); //TODO: KiBlastBehavior must init before ArcTowardsTargetBehavior so we can call Setup() before Start()
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
            KiBlastBehavior k = p.GetComponent<KiBlastBehavior>();
            if (k == null || (!k.reflected))
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

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner)
            return;
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
        uic.GunAmmoCountLabel.AutoHeight = true; // enable multiline text
        uic.GunAmmoCountLabel.ProcessMarkup = true; // enable multicolor text
        uic.GunAmmoCountLabel.Text = $"{this._gun.CurrentAmmo} Ki";
        return true;
    }
}

public class KiBlastBehavior : MonoBehaviour
{
    private static float _MinAngleVariance       = 10f;
    private static float _MaxAngleVariance       = 60f;
    private static float _MinReflectableLifetime = 0.15f;
    private static float _Scaling                = 1.5f;
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
        if (!this.reflected)
            return;
        if (this._projectile.Owner is not AIActor enemy)
            return;
        if (player.CurrentRoom is not RoomHandler room)
            return;

        ++this._numReflections;
        this.reflected = false;
        this._timeSinceLastReflect = 0.0f;
        this._projectile.baseData.damage = this._startingDamage * Mathf.Pow(_Scaling, this._numReflections);

        this._projectile.Owner = player;
        // p.AdjustPlayerProjectileTint(Color.green, 2, 0.1f);
        this._projectile.collidesWithPlayer = false;
        this._projectile.collidesWithEnemies = true;
        this._arc.SetNewTarget(enemy.CenterPosition);

        // this._projectile.SetAnimation(KiBlast._KiSprite);
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
