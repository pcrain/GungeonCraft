namespace CwaffingTheGungy;

public class HatchlingGun : CwaffGun
{
    public static string ItemName         = "Hatchling Gun";
    public static string ShortDescription = "Yolked In";
    public static string LongDescription  = "Fires eggs which spawn chicks on impact. Chicks randomly wander the room, blocking enemies and their projectiles until taking damage.";
    public static string Lore             = "The age-old question \"which came first, the chicken or the egg?\" is mostly of academic interest. Questions of more practical interest to gunsmiths include \"what is the fastest an egg can be fired out of a gun without it breaking in transit?\" and \"how much damage can a singular egg inflict on the Gundead?\" The answers to these questions turn out to be \"not very fast\" and \"not very much,\" respectively. As such, most gunsmiths have no interest in forging guns that fire eggs as projectiles, and the {ItemName}'s existence can be largely attributed to an excessive supply of eggs moreso than an excessive demand of egg-shooting firearms.";

    public static void Init()
    {
        Lazy.SetupGun<HatchlingGun>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.D, gunClass: GunClass.RIFLE, reloadTime: 1.25f, ammo: 500,
            shootFps: 40, reloadFps: 20, muzzleFrom: Items.Mailbox)
          .SetReloadAudio("hatchling_gun_bounce_sound", 0, 6, 14)
          .InitProjectile(GunData.New(clipSize: 12, cooldown: 0.2f, angleVariance: 15.0f, shootStyle: ShootStyle.SemiAutomatic, customClip: true,
            damage: 3.0f, speed: 24.0f, sprite: "egg", fps: 12, scale: 1.5f, anchor: Anchor.MiddleCenter, spawnSound: "hatchling_gun_shoot_sound"))
          .SetAllImpactVFX(VFX.CreatePool("egg_break", fps: 16, loops: false, scale: 0.75f, anchor: Anchor.MiddleCenter))
          .Attach<HatchlingProjectile>();
    }
}

public class HatchlingProjectile : MonoBehaviour
{
    private const float _HATCH_CHANCE = 1.0f;
    private const float _PATH_INTERVAL = 10.0f;
    private const float _COLLISION_DAMAGE = 10.0f; // when jammed

    private Projectile _projectile;
    private PlayerController _owner;
    private GameObject _hatchling;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;

        if (this._owner)
        {
            this._projectile.OnDestruction += this.Hatch;
            this._projectile.GetComponent<SpeculativeRigidbody>().OnRigidbodyCollision += this.OnRigidbodyCollision;
        }
    }

    private void OnRigidbodyCollision(CollisionData obj)
    {
        if (!this._hatchling)
            return;
        if (!obj.OtherRigidbody || obj.OtherRigidbody.GetComponent<AIActor>() is not AIActor)
            return;
        this._hatchling.GetComponent<SpeculativeRigidbody>().RegisterTemporaryCollisionException(obj.OtherRigidbody, 0.5f);
    }

    // Code adapted from CompanionItem::CreateCompanion()
    private void Hatch(Projectile p)
    {
        if (UnityEngine.Random.value > _HATCH_CHANCE)
            return;

        // Create a baby chicken
        GameObject chickum = this._hatchling = AIActor.Spawn(EnemyDatabase.GetOrLoadByGuid(Enemies.Cucco), (Vector2)p.LastPosition, p.transform.position.GetAbsoluteRoom(), true).gameObject;
        CompanionController cc = chickum.GetOrAddComponent<CompanionController>();

        bool ownerHasMastery = this._owner.HasSynergy(Synergy.MASTERY_HATCHLING_GUN);

        // From CompanionItem.Initialize()
        cc.m_owner                        = this._owner; // original was player
        cc.aiActor.ForceBlackPhantom      = ownerHasMastery;
        cc.aiActor.CollisionDamage        = ownerHasMastery ? _COLLISION_DAMAGE : 0f;
        cc.aiActor.IsHarmlessEnemy        = true;
        cc.aiActor.IsWorthShootingAt      = false;
        cc.aiActor.IsNormalEnemy          = false;
        cc.aiActor.CompanionOwner         = this._owner; // original was player
        cc.aiActor.CanTargetPlayers       = false;
        cc.aiActor.CanTargetEnemies       = true;  // original was true
        cc.aiActor.State                  = AIActor.ActorState.Normal;
        cc.aiActor.ParentRoom = p.transform.position.GetAbsoluteRoom(); // needed to avoid null deref for MoveErraticallyBehavior

        if (cc.GetComponent<SpeculativeRigidbody>() is SpeculativeRigidbody srb)
        {
            srb.AddCollisionLayerIgnoreOverride(CollisionMask.LayerToMask(CollisionLayer.PlayerHitBox, CollisionLayer.PlayerCollider));
            PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(srb);
        }

        // Remove normal behavior speculators that follow the player and use our own
        if (cc.behaviorSpeculator is BehaviorSpeculator bs)
        {
            bs.m_aiActor = cc.aiActor;
            bs._serializedStateKeys.Clear();
            bs._serializedStateValues.Clear();
            bs.TargetBehaviors.Clear();
            bs.MovementBehaviors.Clear();

            bs.MovementBehaviors.Add(new MoveErraticallyBehavior {
                PathInterval = _PATH_INTERVAL,
                StayOnScreen = false,
                UseTargetsRoom = false,
                AvoidTarget = false,
            });
            // bs.RegisterBehaviors(bs.TargetBehaviors);
            // bs.RegisterBehaviors(bs.MovementBehaviors);
            bs.FullyRefreshBehaviors();
        }

        // Make it smol
        cc.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        cc.sprite.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        cc.aiActor.procedurallyOutlined = false; // procedural outlining doesn't respect scale, so remove it
        cc.aiActor.HasShadow = false; // don't cast a blob shadow on the ground to save some rendering juice

        // Make it yellow (if it's not jammed)
        cc.sprite.usesOverrideMaterial = true;
        if (!ownerHasMastery)
        {
            cc.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
            cc.aiActor.RegisterOverrideColor(new Color(1.0f, 1.0f, 0.0f, 0.5f) , "little chicky");
        }
        else
            cc.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitCutoutUberPhantom");

        // Add HatchlingBehavior
        cc.gameObject.AddComponent<HatchlingBehavior>().Setup(this._owner);
    }
}

public class HatchlingBehavior : MonoBehaviour
{
    private const float _CHECK_INTERVAL = 1.0f;

    private RoomHandler _startRoom = null;
    private PlayerController _owner = null;
    private AIActor _actor = null;
    private float _lastCheck = 0.0f;

    private void Start()
    {
        this._startRoom = this.gameObject.transform.position.GetAbsoluteRoom();
        this._actor = base.gameObject.GetComponent<AIActor>();
        base.gameObject.Play("bird_chirp");
        base.GetComponent<HealthHaver>().OnDamaged += this.OnDamaged;
        base.GetComponent<SpeculativeRigidbody>().OnCollision += this.OnCollision;
    }

    private void OnCollision(CollisionData obj)
    {
        if (!obj.OtherRigidbody || obj.OtherRigidbody.GetComponent<AIActor>() is not AIActor enemy)
            return;
        base.gameObject.Play("bird_chirp");
        UnityEngine.Object.Destroy(base.gameObject);
    }

    private void OnDamaged(float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection)
    {
        base.gameObject.Play("bird_chirp");
        UnityEngine.Object.Destroy(base.gameObject);
    }

    public void Setup(PlayerController pc)
    {
        this._owner = pc;
    }

    private void Update()
    {
        if ((this._lastCheck += BraveTime.DeltaTime) < _CHECK_INTERVAL)
            return;
        this._lastCheck = 0.0f;

        if (this._owner.CurrentRoom == _startRoom)
            return; // don't despawn even if we're offscreen, so long as the player is in the room we spawned in


        // Check if we're offscreen, and destroy if so
        if (!this._actor.Position.OnScreen())
            UnityEngine.Object.Destroy(base.gameObject);
    }
}
