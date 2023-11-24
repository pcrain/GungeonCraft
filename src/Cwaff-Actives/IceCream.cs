namespace CwaffingTheGungy;

public class IceCream : PlayerItem
{
    public static string ItemName         = "Ice Cream";
    public static string SpritePath       = "ice_cream_icon";
    public static string ShortDescription = "Frozen Treat to Warm the Heart";
    public static string LongDescription  = "When used near an enemy with a gun, replaces their gun with ice cream. Enemies with ice cream are non-hostile, and will seek out other enemies with guns and try to share their ice cream.";
    public static string Lore             = "The ice cream sundae is happiness in dairy format -- an irresistible, timeless classic that needs no introduction or explanation. Getting it into the hands of a frenzied Gundead may prove difficult, but that difficulty is more than made up for by the friendships you'll make by going through the effort of doing it anyway. :>";

    internal static GameObject _HeartVFX;

    public static void Init()
    {
        IceCreamGun.Add(); // add the gun here because it's a pseudo-gun

        PlayerItem item = Lazy.SetupActive<IceCream>(ItemName, SpritePath, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;
        item.consumable   = false;
        item.CanBeDropped = true;
        item.SetCooldownType(ItemBuilder.CooldownType.Timed, 15f);

        _HeartVFX = VFX.RegisterVFXObject("Heart", ResMap.Get("heart_vfx"),
            fps: 18, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 1, emissiveColour: Color.magenta);
    }

    public override bool CanBeUsed(PlayerController user)
    {
        if (user.CurrentRoom?.GetActiveEnemies(RoomHandler.ActiveEnemyType.All) is not List<AIActor> roomEnemies)
            return false;

        Vector2 ppos = user.sprite.WorldCenter;
        foreach (AIActor enemy in roomEnemies)
            if (HappyIceCreamHaver.NeedsIceCream(enemy) && ((enemy.sprite.WorldCenter - ppos).sqrMagnitude <= HappyIceCreamHaver._SHARE_RANGE_SQUARED))
                return base.CanBeUsed(user);

        return false;
    }

    public override void DoEffect(PlayerController user)
    {
        if (user.CurrentRoom?.GetActiveEnemies(RoomHandler.ActiveEnemyType.All) is not List<AIActor> roomEnemies)
            return;

        Vector2 ppos = user.sprite.WorldCenter;
        foreach (AIActor enemy in roomEnemies)
            if (HappyIceCreamHaver.NeedsIceCream(enemy) && ((enemy.sprite.WorldCenter - ppos).sqrMagnitude <= HappyIceCreamHaver._SHARE_RANGE_SQUARED))
                HappyIceCreamHaver.ShareIceCream(enemy);
    }
}

public class IceCreamGun : AdvancedGunBehavior
{
    public static string ItemName         = "Ice Cream Gun";
    public static string SpriteName       = "ice_cream_gun";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = ":>";
    public static string LongDescription  = "EYE KEEM! 'v'";
    public static string Lore             = "EYYYYYEEEE KEEEEEMM";

    internal static int _IceCreamGunId;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<IceCreamGun>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore, hideFromAmmonomicon: true);
            gun.SetAttributes(quality: ItemQuality.SPECIAL, gunClass: GunClass.SILLY, reloadTime: 1.2f, ammo: 999, infiniteAmmo: true);
            gun.SetAnimationFPS(gun.chargeAnimation, 16);
            gun.muzzleFlashEffects = null;
            gun.preventRotation        = true; // make sure the ice cream is always standing up straight
            gun.sprite.HeightOffGround = 0.2f; // render in front of the player

        ProjectileModule mod = gun.DefaultModule;
            mod.shootStyle             = ShootStyle.SemiAutomatic;
            mod.sequenceStyle          = ProjectileSequenceStyle.Random;
            mod.numberOfShotsInClip    = -1;
            mod.ammoType               = GameUIAmmoType.AmmoType.BEAM;
            mod.cooldownTime           = 0.0f;
            mod.projectiles            = new(){ Lazy.NoProjectile() };

        // NOTE: sprites might need lots of padding for hands to render in right positions w.r.t. vanilla sprites, see bullet kin for example
        AIActor bulletKin = EnemyDatabase.GetOrLoadByGuid(Enemies.BulletKin);
            bulletKin.sprite.SetUpAnimation("bullet_smile_left", 2, tk2dSpriteAnimationClip.WrapMode.Loop, copyShaders: true);
            bulletKin.sprite.SetUpAnimation("bullet_smile_right", 2, tk2dSpriteAnimationClip.WrapMode.Loop, copyShaders: true);
            AIAnimator.NamedDirectionalAnimation newOtheranim = new AIAnimator.NamedDirectionalAnimation
            {
                name = "smile",
                anim = new DirectionalAnimation
                {
                    Prefix    = "smile",
                    AnimNames = new string[2]{"bullet_smile_right","bullet_smile_left"},
                    Type      = DirectionalAnimation.DirectionType.TwoWayHorizontal,
                    Flipped   = new DirectionalAnimation.FlipType[]{
                        DirectionalAnimation.FlipType.None,
                        DirectionalAnimation.FlipType.None,
                    },
                }
            };
            bulletKin.sprite.aiAnimator.OtherAnimations ??= new List<AIAnimator.NamedDirectionalAnimation>();
            bulletKin.sprite.aiAnimator.OtherAnimations.Add(newOtheranim);

        _IceCreamGunId = gun.PickupObjectId;
    }
}

public class HappyIceCreamHaver : MonoBehaviour
{
    internal const float _SHARE_RANGE_SQUARED = 6f;
    internal const float _SEEK_PLAYER_RANGE_SQUARED = 16f;

    private const float _TARGET_SWITCH_RATE = 1.00f;

    private AIActor _enemy;
    private float _lastTargetSwitch = 0f;

    private void Start()
    {
        this._enemy = base.GetComponent<AIActor>();
        this._enemy.CollisionDamage            = 0f;
        this._enemy.CollisionKnockbackStrength = 10f;
        this._enemy.IgnoreForRoomClear         = true;

        AdjustBehaviors();

        if (this._enemy.specRigidbody is SpeculativeRigidbody body)
        {
            // body.CanPush            = true;
            // body.CanBePushed        = true;
            // body.CanCarry           = true;
            // body.CanBeCarried       = true;
            // body.CollideWithTileMap = true;
            // body.CollideWithOthers  = true;

            // body.AddCollisionLayerOverride(CollisionMask.LayerToMask(CollisionLayer.Projectile | CollisionLayer.PlayerBlocker));
            // foreach(PixelCollider pc in body.PixelColliders)
            // {
            //     // CollisionMask.LayerToMask(CollisionLayer.Projectile | CollisionLayer.PlayerBlocker)
            //     pc.CollisionLayer = CollisionLayer.PlayerBlocker; // necessary to avoid getting stuck inside enemies
            // }
        }

        if (this._enemy.healthHaver is HealthHaver hh)
        {
            hh.IsVulnerable = false;
            hh.TriggerInvulnerabilityPeriod(999999f);
        }

        if (this._enemy.EnemyGuid == Enemies.BulletKin)
            this._enemy.aiAnimator.OverrideIdleAnimation = "smile";

        AkSoundEngine.PostEvent("ice_cream_shared", base.gameObject);
    }

    private void AdjustBehaviors()
    {
        if (this._enemy.aiShooter?.behaviorSpeculator is not BehaviorSpeculator bs)
            return;

        bs.AttackBehaviors   = new();
        bs.OverrideBehaviors = new();
        bs.OtherBehaviors    = new();

        TargetPourSoulsWithoutIceCreamBehavior targeter = new TargetPourSoulsWithoutIceCreamBehavior();
            targeter.Radius              = 100.0f;
            targeter.LineOfSight         = false;
            targeter.ObjectPermanence    = true;
            targeter.SearchInterval      = _TARGET_SWITCH_RATE;
            targeter.PauseOnTargetSwitch = false;
            targeter.PauseTime           = 0.0f;
            targeter.Init(this._enemy.gameObject, this._enemy.aiActor, this._enemy.aiShooter);
        bs.TargetBehaviors = new(){targeter};

        SeekTargetBehavior seeker = new SeekTargetBehavior();
            seeker.ExternalCooldownSource = false;
            seeker.SpecifyRange           = false;
            seeker.StopWhenInRange        = true;
            seeker.CustomRange            = 2.0f;
            seeker.LineOfSight            = false;
            seeker.ReturnToSpawn          = false;
            seeker.PathInterval           = 0.5f;
            seeker.Init(this._enemy.gameObject, this._enemy.aiActor, this._enemy.aiShooter);
        bs.MovementBehaviors = new(){seeker};

        bs.FullyRefreshBehaviors();
    }

    private void Update()
    {
        this._enemy.CurrentGun.preventRotation        = true;   // make sure the ice cream is always standing up straight
        this._enemy.CurrentGun.sprite.HeightOffGround = 0.2f;   // render in front of the enemy

        if (!this._enemy.CanTargetEnemies)
            this._enemy.CanTargetEnemies = true; // WARNING: calling these nullifies the PlayerTarget every time, so do it as little as possible
        if (!this._enemy.CanTargetPlayers)
            this._enemy.CanTargetPlayers = true; // WARNING: calling these nullifies the PlayerTarget every time, so do it as little as possible

        if (this._enemy.aiShooter is not AIShooter shooter)
            return;

        shooter.ForceGunOnTop = true;

        if (this._enemy.behaviorSpeculator.PlayerTarget is not AIActor iceCreamNeeder)
        {
            shooter.OverrideAimPoint = GameManager.Instance.BestActivePlayer.sprite.WorldCenter;
            return;
        }

        shooter.OverrideAimPoint = iceCreamNeeder.transform.position.XY();
        if ((this._enemy.sprite.WorldCenter - iceCreamNeeder.sprite.WorldCenter).sqrMagnitude < _SHARE_RANGE_SQUARED)
            if (NeedsIceCream(iceCreamNeeder))
                ShareIceCream(iceCreamNeeder);
    }

    internal static bool NeedsIceCream(AIActor enemy)
    {
        if (enemy?.aiShooter?.behaviorSpeculator?.AttackBehaviors == null)
            return false;
        if (enemy.GetComponent<HappyIceCreamHaver>())
            return false;
        if (!enemy.IsHostileAndNotABoss())
            return false;
        foreach (AttackBehaviorBase attack in enemy.aiShooter.behaviorSpeculator.AttackBehaviors)
            if (attack is ShootGunBehavior)
                return true;
        return false;
    }

    internal static void ShareIceCream(AIActor enemy)
    {
        enemy.ReplaceGun((Items)IceCreamGun._IceCreamGunId);
        enemy.gameObject.AddComponent<HappyIceCreamHaver>();
        GameObject vfx = SpawnManager.SpawnVFX(IceCream._HeartVFX, enemy.sprite.WorldTopCenter + new Vector2(0f, 1f), Quaternion.identity, ignoresPools: true);
            tk2dSprite sprite = vfx.GetComponent<tk2dSprite>();
                sprite.HeightOffGround = 1f;
            vfx.transform.parent = enemy.sprite.transform;
            vfx.AddComponent<GlowAndFadeOut>().Setup(
                fadeInTime: 0.25f, glowInTime: 0.50f, glowOutTime: 0.50f, fadeOutTime: 0.25f, maxEmit: 200f, destroy: true);
    }
}

public class TargetPourSoulsWithoutIceCreamBehavior : TargetBehaviorBase
{
    public float Radius           = 10f;
    public bool  LineOfSight      = true;
    public bool  ObjectPermanence = true;
    public float SearchInterval   = 0.25f;
    public bool  PauseOnTargetSwitch;
    public float PauseTime        = 0.25f;

    private float m_losTimer;
    private SpeculativeRigidbody m_specRigidbody;
    private BehaviorSpeculator m_behaviorSpeculator;

    public override void Init(GameObject gameObject, AIActor aiActor, AIShooter aiShooter)
    {
        base.Init(gameObject, aiActor, aiShooter);
        m_specRigidbody = gameObject.GetComponent<SpeculativeRigidbody>();
        m_behaviorSpeculator = gameObject.GetComponent<BehaviorSpeculator>();
    }

    public override void Start()
    {
    }

    public override void Upkeep()
    {
        base.Upkeep();
        DecrementTimer(ref m_losTimer);
    }

    public override BehaviorResult Update()
    {
        BehaviorResult behaviorResult = base.Update();
        if (behaviorResult != 0)
            return behaviorResult;
        if (m_losTimer > 0f)
            return BehaviorResult.Continue;

        m_losTimer = SearchInterval;
        m_behaviorSpeculator.PlayerTarget = NearestEnemyThatReallyNeedsIceCream(m_aiActor);
        if (m_behaviorSpeculator.PlayerTarget)
            m_aiShooter?.AimAtPoint(m_behaviorSpeculator.PlayerTarget.CenterPosition);

        return BehaviorResult.SkipRemainingClassBehaviors;
    }

    internal static GameActor NearestEnemyThatReallyNeedsIceCream(AIActor iceCreamHaver)
    {
        GameActor target = null;
        float bestDist = 9999f;
        Vector2 pos = iceCreamHaver.sprite.WorldCenter;
        foreach (AIActor other in pos.GetAbsoluteRoom()?.GetActiveEnemies(RoomHandler.ActiveEnemyType.All).EmptyIfNull())
        {
            if (other == iceCreamHaver)
                continue;
            if (!HappyIceCreamHaver.NeedsIceCream(other))
                continue;
            float dist = (pos - other.sprite.WorldCenter).sqrMagnitude;
            if (dist > bestDist)
                continue;
            if (dist < HappyIceCreamHaver._SHARE_RANGE_SQUARED)
            {
                HappyIceCreamHaver.ShareIceCream(other);
                continue;
            }
            bestDist = dist;
            target = other;
        }
        if (target)
            return target;

        PlayerController bestPlayer = GameManager.Instance.BestActivePlayer;
        Vector2 bestPlayerPos       = bestPlayer.sprite.WorldCenter;
        if ((pos-bestPlayerPos).sqrMagnitude < HappyIceCreamHaver._SEEK_PLAYER_RANGE_SQUARED)
            return bestPlayer; // target the player if we have no good enemy target and they're in range

        return iceCreamHaver; // target ourself if we have no better target
    }
}
