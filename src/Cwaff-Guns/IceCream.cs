using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace CwaffingTheGungy
{
    public class IceCream : AdvancedGunBehavior
    {
        public static string ItemName         = "Ice Cream";
        public static string SpriteName       = "ice_cream";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = ":>";
        public static string LongDescription  = "TBD";

        internal const float _SHARE_RANGE_SQUARED = 4f;

        internal static int _IceCreamId;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun<IceCream>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.SetAttributes(quality: PickupObject.ItemQuality.C, gunClass: GunClass.SILLY, reloadTime: 1.2f, ammo: 999, infiniteAmmo: true);
                gun.SetAnimationFPS(gun.chargeAnimation, 16);
                gun.muzzleFlashEffects = null;
                gun.preventRotation        = true; // make sure the ice cream is always standing up straight
                gun.sprite.HeightOffGround = 0.2f; // render in front of the player

            ProjectileModule mod = gun.DefaultModule;
                mod.shootStyle             = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle          = ProjectileModule.ProjectileSequenceStyle.Random;
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
                            // DirectionalAnimation.FlipType.Mirror,
                            // DirectionalAnimation.FlipType.Mirror,
                            DirectionalAnimation.FlipType.None,
                        },
                    }
                };
                bulletKin.sprite.aiAnimator.OtherAnimations ??= new List<AIAnimator.NamedDirectionalAnimation>();
                bulletKin.sprite.aiAnimator.OtherAnimations.Add(newOtheranim);
                // Copy attach points from idle sprites
                // tk2dSpriteCollectionData collection = bulletKin.sprite.collection;
                // int bestIdleId = CwaffToolbox.GetIdForBestIdleAnimation(bulletKin);
                // ETGModConsole.Log($"  best idle animation is {bestIdleId} -> {collection.spriteDefinitions[bestIdleId].name}");
                // tk2dSpriteDefinition.AttachPoint[] idleAttachPoints = collection.GetAttachPoints(bestIdleId);
                // if (idleAttachPoints != null)
                // {
                //     ETGModConsole.Log($"  found {idleAttachPoints.Length} attach points");
                // }

            _IceCreamId = gun.PickupObjectId;
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

        internal static void GiveIceCream(AIActor enemy)
        {
            enemy.ReplaceGun((Items)_IceCreamId);
            enemy.gameObject.AddComponent<HappyIceCreamHaver>();
        }

        protected override void Update()
        {
            base.Update();
            if (this.Owner is not PlayerController player)
                return;
            if (player.CurrentRoom == null)
                return;
            if (BraveTime.DeltaTime == 0.0f)
                return;
            List<AIActor> roomEnemies = player.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            if (roomEnemies == null)
                return;

            Vector2 ppos = this.gun.barrelOffset.transform.position.XY();
            foreach (AIActor enemy in roomEnemies)
                if (NeedsIceCream(enemy) && ((enemy.sprite.WorldCenter - ppos).sqrMagnitude <= _SHARE_RANGE_SQUARED))
                {
                    // if (enemy.aiShooter is AIShooter shooter)
                    // {
                    //     ETGModConsole.Log($" before shooter.attachPointCachedPosition = {shooter.attachPointCachedPosition}");
                    //     ETGModConsole.Log($" before shooter.attachPointCachedFlippedPosition = {shooter.attachPointCachedFlippedPosition}");
                    // }
                    GiveIceCream(enemy);
                }
        }
    }

    public class HappyIceCreamHaver : MonoBehaviour
    {
        private const float _TARGET_SWITCH_RATE = 0.05f;

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
            {
                // ETGModConsole.Log($"  TRYING!");//
                this._enemy.aiAnimator.OverrideIdleAnimation = "smile";
            }

            // if (this._enemy.aiShooter is AIShooter shooter)
            // {
            //     ETGModConsole.Log($" after shooter.attachPointCachedPosition = {shooter.attachPointCachedPosition}");
            //     ETGModConsole.Log($" after shooter.attachPointCachedFlippedPosition = {shooter.attachPointCachedFlippedPosition}");
            // }
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
                // targeter.SearchInterval      = 0.01f;
                targeter.SearchInterval      = 1.25f;
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
                seeker.PathInterval           = 0.25f;
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

            // if (this._lastTargetSwitch + _TARGET_SWITCH_RATE < BraveTime.ScaledTimeSinceStartup)
            // {
            //     this._lastTargetSwitch                      = BraveTime.ScaledTimeSinceStartup;
            //     GameActor target                            = NearestEnemyThatReallyNeedsIceCream(this._enemy);
            //     this._enemy.behaviorSpeculator.PlayerTarget = target;
            //     this._enemy.PlayerTarget                    = target;
            // }

            if (this._enemy.aiShooter is AIShooter shooter)
            {
                shooter.ForceGunOnTop = true;

                // ETGModConsole.Log($"gunangle {shooter.GunAngle}, manual {shooter.ManualGunAngle}");
                // Bounds untrimmedBounds = shooter.sprite.GetUntrimmedBounds();
                // shooter.attachPointCachedPosition = shooter.gunAttachPoint.localPosition + (Vector3)PhysicsEngine.PixelToUnit(shooter.overallGunAttachOffset);
                // shooter.attachPointCachedFlippedPosition = shooter.gunAttachPoint.localPosition.WithX(untrimmedBounds.center.x + (untrimmedBounds.center.x - shooter.gunAttachPoint.localPosition.x)) + (Vector3)PhysicsEngine.PixelToUnit(shooter.flippedGunAttachOffset) + (Vector3)PhysicsEngine.PixelToUnit(shooter.overallGunAttachOffset);

                // Fix hands appearing in weird positions (BAD, need something better)
                // if (shooter.gunAttachPoint.localPosition == shooter.attachPointCachedPosition)
                //     shooter.attachPointCachedFlippedPosition = shooter.attachPointCachedPosition;
                // else
                //     shooter.attachPointCachedPosition = shooter.attachPointCachedFlippedPosition;

                if (this._enemy.behaviorSpeculator.PlayerTarget is GameActor target)
                {
                    shooter.OverrideAimPoint = target.transform.position.XY();
                    if (target is AIActor iceCreamNeeder)
                    {
                        if ((this._enemy.sprite.WorldCenter - iceCreamNeeder.sprite.WorldCenter).sqrMagnitude < IceCream._SHARE_RANGE_SQUARED)
                        {
                            if (IceCream.NeedsIceCream(iceCreamNeeder))
                                IceCream.GiveIceCream(iceCreamNeeder);
                        }
                    }
                }
                else
                    shooter.OverrideAimPoint = GameManager.Instance.BestActivePlayer.sprite.WorldCenter;
            }
        }

        internal static GameActor NearestEnemyThatReallyNeedsIceCream(AIActor iceCreamHaver)
        {
            GameActor target = null;
            float bestDist = 9999f;
            Vector2 pos = iceCreamHaver.sprite.WorldCenter;
            foreach (AIActor other in pos.GetAbsoluteRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All))
            {
                if (other == iceCreamHaver)
                    continue;
                if (!IceCream.NeedsIceCream(other))
                    continue;
                float dist = (pos - other.sprite.WorldCenter).sqrMagnitude;
                if (dist > bestDist)
                    continue;
                if (dist < IceCream._SHARE_RANGE_SQUARED)
                {
                    IceCream.GiveIceCream(other);
                    continue;
                }
                bestDist = dist;
                target = other;
            }
            return target ?? GameManager.Instance.BestActivePlayer; // target the player if we have no better target
            // return target ?? this._enemy; // target ourself if we have no better target
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
            if (m_behaviorSpeculator.PlayerTarget)
                return BehaviorResult.Continue;

            m_behaviorSpeculator.PlayerTarget = HappyIceCreamHaver.NearestEnemyThatReallyNeedsIceCream(m_aiActor);
            // m_aiActor.PlayerTarget            = m_behaviorSpeculator.PlayerTarget;
            // m_aiActor.OverrideTarget          = m_behaviorSpeculator.PlayerTarget?.specRigidbody; // TODO: why is this necessary to get everything functioning properly?

            if (m_aiShooter != null && m_behaviorSpeculator.PlayerTarget != null)
                m_aiShooter.AimAtPoint(m_behaviorSpeculator.PlayerTarget.CenterPosition);

            if ((bool)m_aiActor && !m_aiActor.HasBeenEngaged)
            {
                m_aiActor.HasBeenEngaged = true;
                return BehaviorResult.SkipAllRemainingBehaviors;
            }
            return BehaviorResult.SkipRemainingClassBehaviors;
        }
    }

}
