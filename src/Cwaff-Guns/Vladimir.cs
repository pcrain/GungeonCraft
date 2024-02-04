namespace CwaffingTheGungy;

public class Vladimir : AdvancedGunBehavior
{
    public static string ItemName         = "Vladimir";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal const float _LAUNCH_FORCE                 = 150f;
    internal const float _SKEWER_DAMAGE                = 10f;

    internal static List<Vector3> _IdleBarrelOffsets   = new();
    internal static List<Vector3> _ShootBarrelOffsets  = new();
    internal static List<Vector3> _ChargeBarrelOffsets = new();
    internal static GameObject _AbsorbVFX              = null;

    private List<AIActor> _skeweredEnemies             = new();
    private int _power                                 = 0;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Vladimir>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.SILLY, reloadTime: 0.1f, ammo: 100,
              infiniteAmmo: true, canReloadNoMatterAmmo: true);
            gun.SetMuzzleVFX("muzzle_vladimir", fps: 30, scale: 0.3f, anchor: Anchor.MiddleCenter);
            gun.SetFireAudio("vladimir_fire_sound");

        // TODO: make our own impact vfx
        gun.InitProjectile(new(ammoCost: 0, clipSize: -1, cooldown: 0.18f, shootStyle: ShootStyle.SemiAutomatic,
          damage: 7.0f, speed: 100f, range: 0.1f, sprite: "vladimir_hitbox")  // low range ensures the projectile dissipates swiftly
        ).SetAllImpactVFX(VFX.CreatePool("whip_particles", fps: 20, loops: false, anchor: Anchor.MiddleCenter, scale: 0.5f)
        ).Attach<VladimirProjectile>(
        );

        _IdleBarrelOffsets   = gun.GetBarrelOffsetsForAnimation(gun.idleAnimation);
        _ShootBarrelOffsets  = gun.GetBarrelOffsetsForAnimation(gun.shootAnimation);
        _ChargeBarrelOffsets = gun.GetBarrelOffsetsForAnimation(gun.chargeAnimation);

        _AbsorbVFX = VFX.Create("vladimir_impale_projectile_vfx", fps: 2, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 1f);
    }

    protected override void Update()
    {
        base.Update();
        if (this.Owner is not PlayerController pc)
            return;

        UpdateOffsets();

        Vector2 gunPos = this.gun.barrelOffset.position.XY();
        for (int i = this._skeweredEnemies.Count - 1; i >=0; --i)
        {
            AIActor enemy = this._skeweredEnemies[i];
            if (enemy?.healthHaver?.IsAlive ?? false)
            {
                enemy.sprite.PlaceAtPositionByAnchor(gunPos.ToVector3ZisY(), Anchor.MiddleCenter);
                enemy.specRigidbody?.Reinitialize();
            }
            else
                this._skeweredEnemies.RemoveAt(i);
        }
        this.gun.m_unswitchableGun = (this._skeweredEnemies.Count > 0);
        this.gun.CanBeDropped = !this.gun.m_unswitchableGun;
    }

    private void UpdateOffsets()
    {
        tk2dSpriteAnimator anim = gun.spriteAnimator;
        if (anim.IsPlaying(gun.shootAnimation))
            gun.barrelOffset.localPosition = _ShootBarrelOffsets[anim.CurrentFrame];
        else if (anim.IsPlaying(gun.idleAnimation))
            gun.barrelOffset.localPosition = _IdleBarrelOffsets[anim.CurrentFrame];
        else if (anim.IsPlaying(gun.chargeAnimation))
            gun.barrelOffset.localPosition = _ChargeBarrelOffsets[anim.CurrentFrame];
        else
            gun.barrelOffset.localPosition = _ShootBarrelOffsets[0];

        if (gun.sprite.FlipY)
            gun.barrelOffset.localPosition = gun.barrelOffset.localPosition.WithY(-gun.barrelOffset.localPosition.y);
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        Vector2 launchDir = gun.CurrentAngle.ToVector();
        for (int i = this._skeweredEnemies.Count - 1; i >=0; --i)
        {
            AIActor enemy = this._skeweredEnemies[i];
            if (enemy?.healthHaver is not HealthHaver hh)
                continue;
            hh.ApplyDamage(damage: _SKEWER_DAMAGE, direction: launchDir, sourceName: ItemName,
                damageTypes: CoreDamageTypes.None, damageCategory: DamageCategory.Normal);
            if (!hh.IsAlive)
                TossOff(enemy: enemy, launchDir: launchDir);
        }

        if (this._power <= 0)
            return;

        Vector2 gunPos  = this.gun.barrelOffset.position.XY();
        //TODO: use our own projectile here
        Projectile proj = SpawnManager.SpawnProjectile(PistolWhip._PistolWhipProjectile.gameObject, gunPos, this.gun.CurrentAngle.EulerZ()
          ).GetComponent<Projectile>();
        proj.Owner               = player;
        proj.collidesWithEnemies = true;
        proj.collidesWithPlayer  = false;
        foreach (AIActor enemy in this._skeweredEnemies)
        {
            if (enemy?.specRigidbody)
                proj.specRigidbody.RegisterSpecificCollisionException(enemy.specRigidbody);
        }
        AkSoundEngine.PostEvent("whip_crack_sound", player.gameObject);
        --this._power;
    }

    public void Impale(AIActor enemy)
    {
        if (this.Owner is not PlayerController pc)
            return;
        if (!(enemy?.IsHostileAndNotABoss() ?? false) || (enemy.behaviorSpeculator?.ImmuneToStun ?? true))
            return;
        if (enemy.GetComponent<ImpaledOnGunBehaviour>())
            return;

        this._skeweredEnemies.Add(enemy);
        enemy.gameObject.AddComponent<ImpaledOnGunBehaviour>();
        enemy.behaviorSpeculator.Stun(duration: 36000f, createVFX: true);
        this.gun.m_unswitchableGun = true;
        this.gun.CanBeDropped = false;
    }

    public void TossOff(AIActor enemy, Vector2 launchDir)
    {
        this._skeweredEnemies.Remove(enemy);
        enemy.GetComponent<ImpaledOnGunBehaviour>().SafeDestroy();
        enemy.behaviorSpeculator?.ResetStun(duration: 1f, createVFX: true);
        enemy.specRigidbody?.MoveTowardsTargetOrWall(start: this.Owner.CenterPosition, target: this.gun.barrelOffset.position.XY());
        enemy.knockbackDoer?.ApplyKnockback(direction: launchDir, force: _LAUNCH_FORCE);
    }

    public void AbsorbProjectile(Projectile p)
    {
        AkSoundEngine.PostEvent("subtractor_beam_fire_sound", this.gun.gameObject);
        FancyVFX.SpawnBurst(prefab: _AbsorbVFX, numToSpawn: 8, basePosition: p.SafeCenter, positionVariance: 0.2f,
            baseVelocity: null, velocityVariance: 2f, velType: FancyVFX.Vel.AwayRadial, rotType: FancyVFX.Rot.None,
            lifetime: 0.5f, fadeOutTime: 0.5f, parent: null, emissivePower: 0f, emissiveColor: null, fadeIn: false,
            uniform: true, startScale: 1.0f, endScale: 1.0f, height: null);
        p.DieInAir(suppressInAirEffects: true, allowActorSpawns: false, allowProjectileSpawns: false, killedEarly: true);
        ++this._power;
    }
}

public class VladimirProjectile : MonoBehaviour
{
    private const float _PROJ_GRAB_RANGE_SQR = 9f;

    private Projectile _projectile;
    private PlayerController _owner;
    private Vladimir _gun = null;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._projectile.sprite.renderer.enabled = false; // projectile shouldn't be visible since it's just a hitbox
        this._owner = this._projectile.Owner as PlayerController;
        if (this._owner?.CurrentGun?.GetComponent<Vladimir>() is not Vladimir v)
            return;

        this._gun = v;
        this._projectile.OnHitEnemy += this.OnHitEnemy;
    }

    private void Update()
    {
        if (!this._gun)
            return;

        Vector2 myPos = this._projectile.SafeCenter;
        foreach (Projectile p in StaticReferenceManager.AllProjectiles)
        {
            if (!p.isActiveAndEnabled)
                continue;
            if (p.Owner is not AIActor)
                continue;
            if ((myPos - p.SafeCenter).sqrMagnitude > _PROJ_GRAB_RANGE_SQR)
                continue;
            this._gun.AbsorbProjectile(p);
        }
    }

    private void OnHitEnemy(Projectile p, SpeculativeRigidbody enemy, bool killed)
    {
        if (!killed)
            this._gun.Impale(enemy?.aiActor);
    }
}

public class ImpaledOnGunBehaviour : MonoBehaviour
{
    private void Start()
    {
        SpeculativeRigidbody body = base.GetComponent<SpeculativeRigidbody>();
        body.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
        body.OnPreTileCollision += this.OnPreTileCollision;
    }

    private void OnDestroy()
    {
        SpeculativeRigidbody body = base.GetComponent<SpeculativeRigidbody>();
        body.OnPreRigidbodyCollision -= this.OnPreRigidbodyCollision;
        body.OnPreTileCollision -= this.OnPreTileCollision;
    }

    private void OnPreRigidbodyCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, SpeculativeRigidbody other, PixelCollider otherPixelCollider)
    {
        PhysicsEngine.SkipCollision = true;
    }

    private void OnPreTileCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, PhysicsEngine.Tile other, PixelCollider otherPixelCollider)
    {
        PhysicsEngine.SkipCollision = true;
    }
}
