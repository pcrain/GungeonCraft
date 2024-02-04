namespace CwaffingTheGungy;

public class Vladimir : AdvancedGunBehavior
{
    public static string ItemName         = "Vladimir";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal const float _LAUNCH_FORCE = 150f;

    internal static List<Vector3> _IdleBarrelOffsets   = new();
    internal static List<Vector3> _ShootBarrelOffsets  = new();
    internal static List<Vector3> _ChargeBarrelOffsets = new();

    private List<AIActor> _skeweredEnemies = new();

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Vladimir>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.SILLY, reloadTime: 0.1f, ammo: 100, infiniteAmmo: true, canReloadNoMatterAmmo: true);
            gun.SetMuzzleVFX("muzzle_vladimir", fps: 30, scale: 0.3f, anchor: Anchor.MiddleCenter);
            gun.SetFireAudio("vladimir_fire_sound");

        gun.InitProjectile(new(ammoCost: 0, clipSize: -1, cooldown: 0.18f, shootStyle: ShootStyle.SemiAutomatic,
          damage: 7.0f, speed: 100f, range: 0.1f, sprite: "vladimir_hitbox")  // range ensures it dissipates swiftly
        ).SetAllImpactVFX(VFX.CreatePool("whip_particles", fps: 20, loops: false, anchor: Anchor.MiddleCenter, scale: 0.5f)
        ).Attach<VladimirProjectile>(
        );

        _IdleBarrelOffsets   = gun.GetBarrelOffsetsForAnimation(gun.idleAnimation);
        _ShootBarrelOffsets  = gun.GetBarrelOffsetsForAnimation(gun.shootAnimation);
        _ChargeBarrelOffsets = gun.GetBarrelOffsetsForAnimation(gun.chargeAnimation);
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
            hh.ApplyDamage(damage: 10f, direction: launchDir, sourceName: ItemName,
                damageTypes: CoreDamageTypes.None, damageCategory: DamageCategory.Normal);
            if (!hh.IsAlive)
                TossOff(enemy: enemy, launchDir: launchDir);
        }
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
        // enemy.specRigidbody.CollideWithTileMap = false;
        // enemy.specRigidbody.CollideWithOthers = false;
        this.gun.m_unswitchableGun = true;
        this.gun.CanBeDropped = false;
    }

    public void TossOff(AIActor enemy, Vector2 launchDir)
    {
        this._skeweredEnemies.Remove(enemy);
        if (enemy.behaviorSpeculator is BehaviorSpeculator bs)
        {
            bs.EndStun();
            bs.Stun(duration: 1f, createVFX: true);
        }
        enemy.GetComponent<ImpaledOnGunBehaviour>().SafeDestroy();
        if (enemy.specRigidbody is SpeculativeRigidbody body)
        {
            // if (enemy.healthHaver is HealthHaver hh && hh.IsAlive)
            // {
            //     // body.CollideWithTileMap = true;
            //     // body.CollideWithOthers  = true;
            //     // ensure we're in bounds when we're tossed off
            // }
            MoveRigidBodyTowardsTargetOrWall(body: body, start: this.Owner.CenterPosition, target: this.gun.barrelOffset.position.XY());
        }
        if (enemy.knockbackDoer is KnockbackDoer kb)
            kb.ApplyKnockback(direction: launchDir, force: _LAUNCH_FORCE);
        if (enemy.gameObject.AddComponent<ImpaledOnGunBehaviour>() is ImpaledOnGunBehaviour impaled)
            UnityEngine.Object.Destroy(impaled);
    }

    // returns true if we reach our target
    public static bool MoveRigidBodyTowardsTargetOrWall(SpeculativeRigidbody body, Vector2 start, Vector2 target, int steps = 10)
    {
        Vector2 delta        = (target - start);
        Vector2 step         = delta / (float)steps;
        Vector2 lastCheckPos = start;
        tk2dSprite sprite = body.GetComponent<tk2dSprite>();
        for (int i = 0; i < steps; ++i)
        {
            Vector2 checkPos = start + (i * step);
            // body.gameObject.transform.position = checkPos;
            sprite.PlaceAtPositionByAnchor(checkPos, Anchor.MiddleCenter);
            body.Reinitialize();
            if (PhysicsEngine.Instance.OverlapCast(
              rigidbody              : body,
              overlappingCollisions  : null,
              collideWithTiles       : true,
              collideWithRigidbodies : false,
              overrideCollisionMask  : null,
              ignoreCollisionMask    : null,
              collideWithTriggers    : false,
              overridePosition       : null,
              rigidbodyExcluder      : null,
              ignoreList             : new SpeculativeRigidbody[0])
              )
            {
                body.gameObject.transform.position = lastCheckPos;
                body.Reinitialize();
                return false;
            }
            lastCheckPos = checkPos;
        }
        return true;
    }
}

public class VladimirProjectile : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private Vladimir _gun = null;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._projectile.sprite.renderer.enabled = false;
        this._owner = this._projectile.Owner as PlayerController;
        if (this._owner?.CurrentGun?.GetComponent<Vladimir>() is not Vladimir v)
            return;

        this._gun = v;
        this._projectile.OnHitEnemy += this.OnHitEnemy;
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
