

namespace CwaffingTheGungy;

public class Vladimir : CwaffGun
{
    public static string ItemName         = "Vladimir";
    public static string ShortDescription = "Poke 'em On";
    public static string LongDescription  = "Attacking performs a swift melee stab that impales enemies within range. While impaled, enemies are vulnerable to other enemies' projectiles and take damage from subsequent stabs. Each stab can destroy a single enemy projectile. Cannot be dropped or switched while an enemy is impaled. Increases curse by 1 while in inventory.";
    public static string Lore             = "Wielded by and named after a mad warrior who was addicted to impaling his enemies -- enough so that he would frequently count the number of times he was able to bounce them on his trident before they gave out on him. They say some of his madness still lingers within the weapon, but what that means is up to interpretation. On an unrelated note, you find yourself wondering how many stabs a Gun Nut can withstand.";

    internal const float _LAUNCH_FORCE                 = 150f;
    internal const float _SKEWER_DAMAGE                = 14.0f;
    internal const float _CURSE_DAMAGE_SCALING         = 4.0f;
    internal const int   _ENEMIES_PER_CURSE            = 10;

    internal static List<Vector3> _IdleBarrelOffsets   = new();
    internal static List<Vector3> _ShootBarrelOffsets  = new();
    internal static List<Vector3> _ChargeBarrelOffsets = new();
    internal static GameObject _AbsorbVFX              = null;

    internal int _enemiesKilled                        = 0;

    private List<AIActor> _skeweredEnemies             = new();
    private int _power                                 = 0;

    public static void Init()
    {
        Lazy.SetupGun<Vladimir>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.SILLY, reloadTime: 0.1f, ammo: 100,
            infiniteAmmo: true, canReloadNoMatterAmmo: true, fireAudio: "vladimir_fire_sound", muzzleVFX: "muzzle_vladimir",
            muzzleFps: 30, muzzleScale: 0.3f, muzzleAnchor: Anchor.MiddleCenter, curse: 1f, dynamicBarrelOffsets: true)
          .AddToShop(ItemBuilder.ShopType.Cursula)
          .InitProjectile(GunData.New(ammoCost: 0, clipSize: -1, cooldown: 0.3f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 7.0f, speed: 1f, range: 0.01f, sprite: "vladimir_hitbox", hideAmmo: true))  // low range ensures the projectile dissipates swiftly
          .SetAllImpactVFX(VFX.CreatePool("vladimir_particles", fps: 20, loops: false, anchor: Anchor.MiddleCenter, scale: 0.5f))
          .Attach<PierceProjModifier>(pierce => { pierce.penetration = 100; pierce.penetratesBreakables = true; })
          .Attach<VladimirProjectile>();

        _AbsorbVFX = VFX.Create("vladimir_impale_projectile_vfx", emissivePower: 1f);
    }

    public override void Update()
    {
        base.Update();
        if (this.PlayerOwner is not PlayerController pc)
            return;

        Vector2 gunPos = this.gun.barrelOffset.position.XY();
        Vector2 gunVec = this.gun.CurrentAngle.ToVector(0.5f);
        int skewerCount = 0;
        for (int i = this._skeweredEnemies.Count - 1; i >=0; --i)
        {
            AIActor enemy = this._skeweredEnemies[i];
            if (!enemy || !enemy.healthHaver || !enemy.healthHaver.IsAlive)
            {
                this._skeweredEnemies.RemoveAt(i);
                continue;
            }

            enemy.sprite.PlaceAtPositionByAnchor((gunPos - (skewerCount * gunVec)).ToVector3ZisY(), Anchor.MiddleCenter);
            ++skewerCount;

            if (!enemy.specRigidbody)
                continue;

            enemy.specRigidbody.Reinitialize();
            PixelCollider collider = enemy.specRigidbody.PrimaryPixelCollider;
            IntVector2 epos = enemy.sprite.WorldBottomLeft.ToIntVector2();
            IntVector2 edim = (enemy.sprite.WorldTopRight - enemy.sprite.WorldBottomLeft).ToIntVector2();
            for (int j = StaticReferenceManager.AllProjectiles.Count - 1; j >= 0; --j) //REFACTOR: make enemies collide with projectiles properly
            {
                Projectile proj = StaticReferenceManager.AllProjectiles[j];
                if (!proj | !proj.isActiveAndEnabled)
                    continue;
                if (proj.specRigidbody is not SpeculativeRigidbody body)
                    continue;
                // if (!collider.AABBOverlaps(body.PrimaryPixelCollider))
                if (!proj.sprite.Overlaps(enemy.sprite))
                    continue;

                // forces projectile to collide with enemies even if it normally wouldn't
                LinearCastResult lcr   = LinearCastResult.Pool.Allocate();
                lcr.Contact            = enemy.CenterPosition;
                lcr.Normal             = Vector2.right;
                lcr.OtherPixelCollider = body.PrimaryPixelCollider;
                lcr.MyPixelCollider    = collider;
                proj.ForceCollision(enemy.specRigidbody, lcr);
                LinearCastResult.Pool.Free(ref lcr);
            }
        }
        bool gunLocked = (this._skeweredEnemies.Count > 0);
        pc.inventory.GunLocked.SetOverride(ItemName, gunLocked);
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        Vector2 launchDir = gun.CurrentAngle.ToVector();
        float damage = _SKEWER_DAMAGE * player.DamageMult();
        for (int i = this._skeweredEnemies.Count - 1; i >=0; --i)
        {
            AIActor enemy = this._skeweredEnemies[i];
            if (!enemy || enemy.healthHaver is not HealthHaver hh)
                continue;
            hh.ApplyDamage(damage: damage, direction: launchDir, sourceName: ItemName,
                damageTypes: CoreDamageTypes.None, damageCategory: DamageCategory.Normal);
            if (!hh.IsAlive)
            {
                enemy.specRigidbody.RegisterSpecificCollisionException(player.specRigidbody);
                enemy.specRigidbody.AddCollisionLayerIgnoreOverride(CollisionMask.LayerToMask(CollisionLayer.Projectile));
                TossOff(enemy: enemy, launchDir: launchDir);
            }
        }

        // Old code for being able to launch projectiles back, not using for now
        // if (this._power <= 0)
        //     return;

        // Vector2 gunPos  = this.gun.barrelOffset.position.XY();
        // // use our own projectile here if we ever decide to do this for real
        // Projectile proj = SpawnManager.SpawnProjectile(PistolWhip._PistolWhipProjectile.gameObject, gunPos, this.gun.CurrentAngle.EulerZ()
        //   ).GetComponent<Projectile>();
        // proj.Owner               = player;
        // proj.collidesWithEnemies = true;
        // proj.collidesWithPlayer  = false;
        // foreach (AIActor enemy in this._skeweredEnemies)
        // {
        //     if (enemy && enemy.specRigidbody)
        //         proj.specRigidbody.RegisterSpecificCollisionException(enemy.specRigidbody);
        // }
        // player.gameObject.Play("whip_crack_sound");
        // if ((--this._power) == 0)
        //     this.gun.sprite.gameObject.SetGlowiness(0f);
    }

    public void Impale(AIActor enemy)
    {
        if (this.PlayerOwner is not PlayerController pc)
            return;
        if (!enemy || !enemy.IsHostileAndNotABoss() || !enemy.behaviorSpeculator || enemy.behaviorSpeculator.ImmuneToStun)
            return;
        if (enemy.GetComponent<ImpaledOnGunBehaviour>())
            return;

        this._skeweredEnemies.Add(enemy);
        enemy.gameObject.AddComponent<ImpaledOnGunBehaviour>();
        enemy.behaviorSpeculator.Stun(duration: 36000f, createVFX: true);
        pc.inventory.GunLocked.SetOverride(ItemName, true);
    }

    public void TossOff(AIActor enemy, Vector2 launchDir)
    {
        this._skeweredEnemies.Remove(enemy);
        enemy.GetComponent<ImpaledOnGunBehaviour>().SafeDestroy();
        if (enemy.behaviorSpeculator)
            enemy.behaviorSpeculator.ResetStun(duration: 1f, createVFX: true);
        if (enemy.specRigidbody)
        {
            enemy.specRigidbody.MoveTowardsTargetOrWall(start: this.PlayerOwner.CenterPosition, target: this.gun.barrelOffset.position.XY());
            enemy.specRigidbody.AddCollisionLayerOverride(CollisionMask.LayerToMask(CollisionLayer.EnemyHitBox));
            enemy.specRigidbody.OnPreRigidbodyCollision += DealDamageWhenTossedAtEnemies;
        }
        if (enemy.knockbackDoer)
        {
            enemy.knockbackDoer.m_activeKnockbacks.Clear();
            enemy.knockbackDoer.ClearContinuousKnockbacks();
            enemy.knockbackDoer.ApplyKnockback(direction: launchDir, force: _LAUNCH_FORCE);
        }
        if (!this.PlayerOwner || !this.PlayerOwner.HasSynergy(Synergy.MASTERY_VLADIMIR))
            return;
        if (++this._enemiesKilled < _ENEMIES_PER_CURSE)
            return;
        this._enemiesKilled -= _ENEMIES_PER_CURSE;
        this.PlayerOwner.IncreaseCurse();
    }

    private void DealDamageWhenTossedAtEnemies(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (!otherRigidbody || !otherRigidbody.aiActor || !myRigidbody || !myRigidbody.healthHaver)
            return;
        myRigidbody.OnPreRigidbodyCollision -= DealDamageWhenTossedAtEnemies;
        AIActor aIActor = otherRigidbody.aiActor;
        if (aIActor.IsNormalEnemy && aIActor.healthHaver)
            aIActor.healthHaver.ApplyDamage(myRigidbody.healthHaver.GetMaxHealth(), myRigidbody.Velocity, ItemName);
    }

    public void AbsorbProjectile(Projectile p)
    {
        this.gun.gameObject.Play("subtractor_beam_fire_sound");
        CwaffVFX.SpawnBurst(prefab: _AbsorbVFX, numToSpawn: 8, basePosition: p.SafeCenter, positionVariance: 0.2f,
            baseVelocity: Vector2.zero, velocityVariance: 2f, velType: CwaffVFX.Vel.AwayRadial, rotType: CwaffVFX.Rot.None,
            lifetime: 0.5f, fadeOutTime: 0.5f, emissivePower: 0f, emissiveColor: null, fadeIn: false,
            uniform: true, startScale: 1.0f, endScale: 1.0f, height: null);
        p.DieInAir(suppressInAirEffects: true, allowActorSpawns: false, allowProjectileSpawns: false, killedEarly: true);

        // Old code for being able to launch projectiles back, not using for now
        // ++this._power;
        // this.gun.sprite.gameObject.SetGlowiness(250f);
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (this.PlayerOwner && this.PlayerOwner.HasSynergy(Synergy.MASTERY_VLADIMIR))
            projectile.baseData.damage += _CURSE_DAMAGE_SCALING * Mathf.Max(0f, this.PlayerOwner.Curse());
    }

    internal class ImpaledOnGunBehaviour : SkipAllCollisionsBehavior {}

    [HarmonyPatch(typeof(Dungeon), nameof(Dungeon.SpawnCurseReaper))]
    private class PreventLotJSpawnWhenMasteredPatch
    {
        static bool Prefix(Dungeon __instance)
        {
            if (GameManager.Instance.PrimaryPlayer.HasSynergy(Synergy.MASTERY_VLADIMIR))
                return false;
            if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
                if (GameManager.Instance.SecondaryPlayer.HasSynergy(Synergy.MASTERY_VLADIMIR))
                    return false;
            return true;
        }
    }
}

public class VladimirProjectile : MonoBehaviour
{
    private const float _PROJ_GRAB_RANGE_SQR = 9f;

    private Projectile _projectile;
    private PlayerController _owner;
    private Vladimir _gun = null;
    private bool _absorbedProjectile = false;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._projectile.sprite.renderer.enabled = false; // projectile shouldn't be visible since it's just a hitbox
        this._owner = this._projectile.Owner as PlayerController;
        if (!this._owner || !this._owner.CurrentGun || this._owner.CurrentGun.GetComponent<Vladimir>() is not Vladimir v)
            return;

        this._gun = v;
        this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
        this._projectile.OnHitEnemy += this.OnHitEnemy;
    }

    private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (otherRigidbody.gameObject.GetComponent<Vladimir.ImpaledOnGunBehaviour>())
            PhysicsEngine.SkipCollision = true;
    }

    private void Update()
    {
        if (!this._gun || this._absorbedProjectile)
            return;

        Vector2 myPos = this._projectile.SafeCenter;
        for (int i = StaticReferenceManager.AllProjectiles.Count - 1; i >= 0; --i)
        {
            Projectile p = StaticReferenceManager.AllProjectiles[i];
            if (!p.isActiveAndEnabled)
                continue;
            if (p.Owner is not AIActor)
                continue;
            if ((myPos - p.SafeCenter).sqrMagnitude > _PROJ_GRAB_RANGE_SQR)
                continue;
            this._gun.AbsorbProjectile(p);
            this._absorbedProjectile = true;
            break;
        }
    }

    private void OnHitEnemy(Projectile p, SpeculativeRigidbody enemy, bool killed)
    {
        if (!enemy)
            return;
        if (!killed)
        {
            this._gun.Impale(enemy.aiActor);
            return;
        }

        if (!this._gun || this._gun.PlayerOwner is not PlayerController player)
            return;
        if (!player.HasSynergy(Synergy.MASTERY_VLADIMIR))
            return;
        if (++this._gun._enemiesKilled < Vladimir._ENEMIES_PER_CURSE)
            return;
        this._gun._enemiesKilled -= Vladimir._ENEMIES_PER_CURSE;
        player.IncreaseCurse();
    }
}
