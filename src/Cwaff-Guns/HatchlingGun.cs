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
    public class HatchlingGun : AdvancedGunBehavior
    {
        public static string ItemName         = "Hatchling Gun";
        public static string SpriteName       = "hatchling_gun";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "Yolked In";
        public static string LongDescription  = "TBD";

        internal static tk2dSpriteAnimationClip _BulletSprite;

        private const float _NATASHA_PROJECTILE_SCALE = 0.5f;
        private float _speedMult                      = 1.0f;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun<HatchlingGun>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.reloadTime                        = 1.1f;
                gun.quality                           = PickupObject.ItemQuality.D;
                gun.SetBaseMaxAmmo(2500);
                gun.CurrentAmmo = gun.GetBaseMaxAmmo(); // necessary iff gun basemaxammo > 1000
                gun.SetAnimationFPS(gun.shootAnimation, 30);
                gun.ClearDefaultAudio();

            ProjectileModule mod = gun.DefaultModule;
                mod.ammoCost            = 1;
                mod.shootStyle          = ProjectileModule.ShootStyle.Automatic;
                mod.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.angleVariance       = 15.0f;
                mod.cooldownTime        = 0.2f;
                mod.numberOfShotsInClip = -1;

            _BulletSprite = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("natascha_bullet").Base(),
                12, true, new IntVector2((int)(_NATASHA_PROJECTILE_SCALE * 15), (int)(_NATASHA_PROJECTILE_SCALE * 7)),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.AddDefaultAnimation(_BulletSprite);
                projectile.baseData.damage  = 3f;
                projectile.baseData.speed   = 20.0f;
                projectile.transform.parent = gun.barrelOffset;
                projectile.gameObject.AddComponent<HatchlingProjectile>();
        }
    }


    public class HatchlingProjectile : MonoBehaviour
    {
        private const float _HATCH_CHANCE = 1.0f;//0.1f;
        private const string _CHICKEN_GUID = "7bd9c670f35b4b8d84280f52a5cc47f6";

        private Projectile _projectile;
        private PlayerController _owner;
        private void Start()
        {
            this._projectile = base.GetComponent<Projectile>();
            this._owner = this._projectile.Owner as PlayerController;

            this._projectile.OnDestruction += this.Hatch;
        }

        // Code from CompanionItem::CreateCompanion()
        private void Hatch(Projectile p)
        {
            try
            {
                if (UnityEngine.Random.value > _HATCH_CHANCE)
                    return;

                // Create a baby chicken
                AIActor chickenActor = EnemyDatabase.GetOrLoadByGuid(_CHICKEN_GUID);
                GameObject extantCompanion2 = UnityEngine.Object.Instantiate(chickenActor.gameObject, p.transform.position, Quaternion.identity);
                CompanionController cc = extantCompanion2.GetOrAddComponent<CompanionController>();

                // From CompanionItem.Initialize()
                cc.m_owner                        = null; // original was player
                cc.aiActor.IsNormalEnemy          = false;
                cc.aiActor.CompanionOwner         = null; // original was player
                cc.aiActor.CanTargetPlayers       = false;
                cc.aiActor.CanTargetEnemies       = true;  // original was true
                // cc.aiActor.CustomPitDeathHandling += this.CustomPitDeathHandling;
                // cc.aiActor.PlayerTarget           = p.ProjectilePlayerOwner(); // only needed if using FleeFromTargetBehavior and fleeing from player
                // cc.aiActor.OverrideTarget         = <nearest AIActor.speculativeRigidBody> // only need if using FleeFromTargetBehavior and fleeing from enemy
                cc.aiActor.State                  = AIActor.ActorState.Normal;
                cc.healthHaver.OnDamaged += (float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection) => {
                    cc.healthHaver.FullHeal();
                    AkSoundEngine.PostEvent("Play_PET_chicken_cluck_01", cc.gameObject);
                };

                cc.aiActor.ParentRoom = p.transform.position.GetAbsoluteRoom(); // needed to avoid null deref for MoveErraticallyBehavior

                if (cc.specRigidbody)
                {
                    cc.specRigidbody.AddCollisionLayerIgnoreOverride(CollisionMask.LayerToMask(CollisionLayer.PlayerHitBox, CollisionLayer.PlayerCollider));
                    PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(cc.specRigidbody);
                }

                if (cc.behaviorSpeculator is BehaviorSpeculator bs)
                {
                    bs.m_aiActor = cc.aiActor;
                    bs._serializedStateKeys.Clear();
                    bs._serializedStateValues.Clear();
                    bs.TargetBehaviors.Clear();
                    bs.TargetBehaviors.Add(new TargetEnemiesBehavior/*Clone*/ {
                        LineOfSight = false, // must be false, as the default logic assumes the target is a player
                        ObjectPermanence = false,
                        SearchInterval = 1.0f,
                    });
                    bs.MovementBehaviors.Clear();
                    // bs.MovementBehaviors.Add(new FleeTargetBehaviorClone {
                    //     CloseDistance = 9f,
                    //     CloseTime = 0.1f,
                    //     TooCloseDistance = 6f,
                    //     DesiredDistance = 20f,
                    // });
                    bs.MovementBehaviors.Add(new MoveErraticallyBehavior/*Clone*/ {
                    });
                    // bs.m_behaviors.Clear();
                    bs.RegisterBehaviors(bs.TargetBehaviors);
                    bs.RegisterBehaviors(bs.MovementBehaviors);
                    bs.RefreshBehaviors();
                    // bs.StartBehaviors();
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log($"{e}");
            }
        }
    }

    public class TargetEnemiesBehaviorClone : TargetBehaviorBase
    {
        public bool LineOfSight = true;

        public bool ObjectPermanence = true;

        public float SearchInterval = 0.25f;

        private float m_losTimer;

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
            {
                return behaviorResult;
            }
            if (m_losTimer > 0f)
            {
                return BehaviorResult.Continue;
            }
            m_losTimer = SearchInterval;
            if ((bool)m_aiActor.PlayerTarget)
            {
                if (m_aiActor.PlayerTarget.IsFalling)
                {
                    m_aiActor.PlayerTarget = null;
                    m_aiActor.ClearPath();
                    return BehaviorResult.SkipRemainingClassBehaviors;
                }
                if ((bool)m_aiActor.PlayerTarget.healthHaver && m_aiActor.PlayerTarget.healthHaver.IsDead)
                {
                    m_aiActor.PlayerTarget = null;
                    m_aiActor.ClearPath();
                    return BehaviorResult.SkipRemainingClassBehaviors;
                }
            }
            else
            {
                m_aiActor.PlayerTarget = null;
            }
            if (!ObjectPermanence)
            {
                m_aiActor.PlayerTarget = null;
            }
            if (m_aiActor.PlayerTarget != null)
            {
                return BehaviorResult.Continue;
            }
            if (!m_aiActor.CanTargetEnemies)
            {
                return BehaviorResult.Continue;
            }
            List<AIActor> activeEnemies = GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(m_aiActor.GridPosition).GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            if (activeEnemies != null && activeEnemies.Count > 0)
            {
                AIActor playerTarget = null;
                float num = float.MaxValue;
                for (int i = 0; i < activeEnemies.Count; i++)
                {
                    AIActor aIActor = activeEnemies[i];
                    if (aIActor == m_aiActor)
                    {
                        continue;
                    }
                    float num2 = Vector2.Distance(m_aiActor.CenterPosition, aIActor.CenterPosition);
                    if (!(num2 < num))
                    {
                        continue;
                    }
                    if (LineOfSight)
                    {
                        int standardPlayerVisibilityMask = CollisionMask.StandardPlayerVisibilityMask;
                        RaycastResult result;
                        if (!PhysicsEngine.Instance.Raycast(m_aiActor.CenterPosition, aIActor.CenterPosition - m_aiActor.CenterPosition, num2, out result, true, true, standardPlayerVisibilityMask, null, false, null, m_aiActor.specRigidbody))
                        {
                            RaycastResult.Pool.Free(ref result);
                            continue;
                        }
                        // old code prevents LineOfSight from working with enemies
                        if (result.SpeculativeRigidbody == null /* || result.SpeculativeRigidbody.GetComponent<PlayerController>() == null*/)
                        {
                            RaycastResult.Pool.Free(ref result);
                            continue;
                        }
                        RaycastResult.Pool.Free(ref result);
                    }
                    playerTarget = aIActor;
                    num = num2;
                }
                m_aiActor.PlayerTarget = playerTarget;
            }
            if (m_aiShooter != null && m_aiActor.PlayerTarget != null)
            {
                m_aiShooter.AimAtPoint(m_aiActor.PlayerTarget.CenterPosition);
            }
            if (!m_aiActor.HasBeenEngaged)
            {
                m_aiActor.HasBeenEngaged = true;
                return BehaviorResult.SkipAllRemainingBehaviors;
            }
            return BehaviorResult.SkipRemainingClassBehaviors;
        }
    }

    public class FleeTargetBehaviorClone : MovementBehaviorBase
    {
        public float PathInterval = 0.25f;

        public float CloseDistance = 9f;

        public float CloseTime = 3f;

        public float TooCloseDistance = 6f;

        public bool TooCloseLOS = true;

        public float DesiredDistance = 20f;

        public int PlayerPersonalSpace;

        public bool CanAttackWhileMoving;

        public bool ManuallyDefineRoom;

        public Vector2 roomMin;

        public Vector2 roomMax;

        [NonSerialized]
        public bool ForceRun;

        private float m_repathTimer;

        private float m_closeTimer;

        private bool m_wasDamaged;

        private bool m_shouldRun;

        private SpeculativeRigidbody m_otherTargetRigidbody;

        private IntVector2? m_targetPos;

        private IntVector2 m_cachedPlayerCell;

        private IntVector2? m_cachedOtherPlayerCell;

        public override void Start()
        {
            if ((bool)m_aiActor && (bool)m_aiActor.healthHaver)
            {
                m_aiActor.healthHaver.OnDamaged += OnDamaged;
            }
        }

        public override void Upkeep()
        {
            try
            {
                base.Upkeep();
                DecrementTimer(ref m_repathTimer);
                DecrementTimer(ref m_closeTimer);
                if (m_aiActor.DistanceToTarget > CloseDistance)
                {
                    m_closeTimer = CloseTime;
                }
                m_shouldRun = false;
                if (m_wasDamaged)
                {
                    m_shouldRun = true;
                    m_wasDamaged = false;
                }
                m_otherTargetRigidbody = null;
                if (m_aiActor.PlayerTarget is PlayerController && GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
                {
                    PlayerController otherPlayer = GameManager.Instance.GetOtherPlayer(m_aiActor.PlayerTarget as PlayerController);
                    if ((bool)otherPlayer && (bool)otherPlayer.healthHaver && otherPlayer.healthHaver.IsAlive)
                    {
                        m_otherTargetRigidbody = otherPlayer.specRigidbody;
                    }
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log($"{e}");
            }
        }

        public override bool OverrideOtherBehaviors()
        {
            return ShouldRun();
        }

        public override BehaviorResult Update()
        {
            try
            {
                IntVector2? targetPos = m_targetPos;
                if (!targetPos.HasValue && m_repathTimer > 0f)
                {
                    return BehaviorResult.Continue;
                }
                IntVector2? targetPos2 = m_targetPos;
                if (targetPos2.HasValue && m_aiActor.PathComplete)
                {
                    m_targetPos = null;
                }
                IntVector2? targetPos3 = m_targetPos;
                if (!targetPos3.HasValue && (bool)m_aiActor.TargetRigidbody && ShouldRun() && m_aiActor.ParentRoom != null)
                {
                    RoomHandler parentRoom = m_aiActor.ParentRoom;
                    m_targetPos = parentRoom.GetRandomAvailableCell(m_aiActor.Clearance, m_aiActor.PathableTiles, false, CellValidator);
                    IntVector2? targetPos4 = m_targetPos;
                    if (!targetPos4.HasValue)
                    {
                        m_targetPos = parentRoom.GetRandomWeightedAvailableCell(m_aiActor.Clearance, m_aiActor.PathableTiles, false, CellValidator, CellWeighter);
                    }
                    m_repathTimer = 0f;
                    m_closeTimer = 0f;
                    ForceRun = false;
                }
                if (m_repathTimer <= 0f)
                {
                    IntVector2? targetPos5 = m_targetPos;
                    if (targetPos5.HasValue && (bool)m_aiActor.TargetRigidbody)
                    {
                        m_repathTimer = PathInterval;
                        m_cachedPlayerCell = m_aiActor.TargetRigidbody.UnitCenter.ToIntVector2(VectorConversions.Floor);
                        m_cachedOtherPlayerCell = ((!m_otherTargetRigidbody) ? null : new IntVector2?(m_otherTargetRigidbody.UnitCenter.ToIntVector2(VectorConversions.Floor)));
                        m_aiActor.PathfindToPosition(m_targetPos.Value.ToCenterVector2(), null, true, null, CellPathingWeighter);
                    }
                }
                IntVector2? targetPos6 = m_targetPos;
                if (!targetPos6.HasValue)
                {
                    return BehaviorResult.Continue;
                }
                return CanAttackWhileMoving ? BehaviorResult.SkipRemainingClassBehaviors : BehaviorResult.SkipAllRemainingBehaviors;
            }
            catch (Exception e)
            {
                ETGModConsole.Log($"{e}");
                return BehaviorResult.SkipAllRemainingBehaviors;
            }
        }

        private bool CellValidator(IntVector2 c)
        {
            if (ManuallyDefineRoom && ((float)c.x < roomMin.x || (float)c.x > roomMax.x || (float)c.y < roomMin.y || (float)c.y > roomMax.y))
            {
                return false;
            }
            for (int i = 0; i < m_aiActor.Clearance.x; i++)
            {
                for (int j = 0; j < m_aiActor.Clearance.y; j++)
                {
                    if (GameManager.Instance.Dungeon.data.isTopWall(c.x + i, c.y + j))
                    {
                        return false;
                    }
                }
            }
            if (Vector2.Distance(Pathfinding.Pathfinder.GetClearanceOffset(c, m_aiActor.Clearance), m_aiActor.TargetRigidbody.UnitCenter) < DesiredDistance)
            {
                return false;
            }
            if ((bool)m_otherTargetRigidbody && Vector2.Distance(Pathfinding.Pathfinder.GetClearanceOffset(c, m_aiActor.Clearance), m_otherTargetRigidbody.UnitCenter) < DesiredDistance)
            {
                return false;
            }
            return true;
        }

        private float CellWeighter(IntVector2 c)
        {
            for (int i = 0; i < m_aiActor.Clearance.x; i++)
            {
                for (int j = 0; j < m_aiActor.Clearance.y; j++)
                {
                    if (GameManager.Instance.Dungeon.data.isTopWall(c.x + i, c.y + j))
                    {
                        return 1000000f;
                    }
                }
            }
            float num = Vector2.Distance(Pathfinding.Pathfinder.GetClearanceOffset(c, m_aiActor.Clearance), m_aiActor.TargetRigidbody.UnitCenter);
            if ((bool)m_otherTargetRigidbody)
            {
                num = Mathf.Min(num, Vector2.Distance(Pathfinding.Pathfinder.GetClearanceOffset(c, m_aiActor.Clearance), m_otherTargetRigidbody.UnitCenter));
            }
            return num;
        }

        private int CellPathingWeighter(IntVector2 prevStep, IntVector2 thisStep)
        {
            if (IntVector2.Distance(thisStep, m_cachedPlayerCell) < (float)PlayerPersonalSpace)
            {
                return 100;
            }
            IntVector2? cachedOtherPlayerCell = m_cachedOtherPlayerCell;
            if (cachedOtherPlayerCell.HasValue && IntVector2.Distance(thisStep, m_cachedOtherPlayerCell.Value) < (float)PlayerPersonalSpace)
            {
                return 100;
            }
            return 0;
        }

        private void OnDamaged(float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection)
        {
            m_wasDamaged = true;
        }

        private bool ShouldRun()
        {
            if (m_shouldRun || ForceRun)
            {
                return true;
            }
            float num = m_aiActor.DistanceToTarget;
            if (m_aiActor.PlayerTarget is PlayerController && GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
            {
                PlayerController otherPlayer = GameManager.Instance.GetOtherPlayer(m_aiActor.PlayerTarget as PlayerController);
                if ((bool)otherPlayer && (bool)otherPlayer.healthHaver && otherPlayer.healthHaver.IsAlive)
                {
                    float b = Vector2.Distance(m_aiActor.specRigidbody.UnitCenter, otherPlayer.specRigidbody.GetUnitCenter(ColliderType.HitBox));
                    num = Mathf.Min(num, b);
                }
            }
            if (num < TooCloseDistance && (!TooCloseLOS || m_aiActor.HasLineOfSightToTarget))
            {
                return true;
            }
            if (num < CloseDistance && m_closeTimer <= 0f)
            {
                return true;
            }
            return false;
        }
    }

    public class MoveErraticallyBehaviorClone : MovementBehaviorBase
    {
        public float PathInterval = 0.25f;

        public float PointReachedPauseTime;

        public bool PreventFiringWhileMoving;

        public float InitialDelay;

        public bool StayOnScreen = true;

        public bool AvoidTarget = true;

        public bool UseTargetsRoom;

        private float m_repathTimer;

        private float m_pauseTimer;

        private IntVector2? m_targetPos;

        private IntVector2 m_cachedCameraBottomLeft;

        private IntVector2 m_cachedCameraBottomRight;

        private float m_cachedAngleFromTarget;

        private Vector2 m_cachedTargetPos;

        private float? m_cachedAngleFromOtherTarget;

        private Vector2? m_cachedOtherTargetPos;

        public override bool AllowFearRunState
        {
            get
            {
                return true;
            }
        }

        public override void Start()
        {
            base.Start();
            m_pauseTimer = InitialDelay;
        }

        public override void Upkeep()
        {
            try
            {
                base.Upkeep();
                DecrementTimer(ref m_repathTimer);
                DecrementTimer(ref m_pauseTimer);
                if (StayOnScreen)
                {
                    Vector2 vector = BraveUtility.ViewportToWorldpoint(new Vector2(0f, 0f), ViewportType.Gameplay);
                    Vector2 vector2 = BraveUtility.ViewportToWorldpoint(new Vector2(1f, 1f), ViewportType.Gameplay);
                    m_cachedCameraBottomLeft = vector.ToIntVector2(VectorConversions.Ceil);
                    m_cachedCameraBottomRight = vector2.ToIntVector2(VectorConversions.Floor) - IntVector2.One;
                }
                if (AvoidTarget && (bool)m_aiActor.TargetRigidbody)
                {
                    m_cachedTargetPos = m_aiActor.TargetRigidbody.GetUnitCenter(ColliderType.Ground);
                    m_cachedAngleFromTarget = (m_aiActor.specRigidbody.UnitCenter - m_cachedTargetPos).ToAngle();
                    PlayerController playerController = m_aiActor.PlayerTarget as PlayerController;
                    if ((bool)playerController && GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
                    {
                        PlayerController otherPlayer = GameManager.Instance.GetOtherPlayer(playerController);
                        m_cachedOtherTargetPos = otherPlayer.specRigidbody.GetUnitCenter(ColliderType.Ground);
                        m_cachedAngleFromOtherTarget = (m_aiActor.specRigidbody.UnitCenter - m_cachedTargetPos).ToAngle();
                    }
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log($"{e}");
            }
        }

        public override BehaviorResult Update()
        {
            try
            {
                BehaviorResult behaviorResult = base.Update();
                if (behaviorResult != 0)
                {
                    return behaviorResult;
                }
                IntVector2? targetPos = m_targetPos;
                if (!targetPos.HasValue && m_repathTimer > 0f)
                {
                    return BehaviorResult.Continue;
                }
                if (m_pauseTimer > 0f)
                {
                    return BehaviorResult.SkipRemainingClassBehaviors;
                }
                IntVector2? targetPos2 = m_targetPos;
                if (targetPos2.HasValue && m_aiActor.PathComplete)
                {
                    m_targetPos = null;
                    if (PointReachedPauseTime > 0f)
                    {
                        m_pauseTimer = PointReachedPauseTime;
                        return BehaviorResult.SkipAllRemainingBehaviors;
                    }
                }
                if (m_repathTimer <= 0f)
                {
                    m_repathTimer = PathInterval;
                    IntVector2? targetPos3 = m_targetPos;
                    if (targetPos3.HasValue && !SimpleCellValidator(m_targetPos.Value))
                    {
                        m_targetPos = null;
                    }
                    IntVector2? targetPos4 = m_targetPos;
                    ETGModConsole.Log($"e");
                    if (!targetPos4.HasValue)
                    {
                        ETGModConsole.Log($"e2: m_aiActor = {m_aiActor.name ?? "null"}");
                        RoomHandler roomHandler = m_aiActor.ParentRoom;
                        ETGModConsole.Log($"e3: room = {roomHandler?.GetRoomName() ?? "null"}");
                        if (UseTargetsRoom && (bool)m_aiActor.TargetRigidbody)
                        {
                            ETGModConsole.Log($"e4");
                            PlayerController playerController = ((!m_aiActor.TargetRigidbody.gameActor) ? null : (m_aiActor.TargetRigidbody.gameActor as PlayerController));
                            ETGModConsole.Log($"ee");
                            if ((bool)playerController)
                            {
                                roomHandler = playerController.CurrentRoom;
                            }
                        }
                        m_targetPos = roomHandler.GetRandomAvailableCell(m_aiActor.Clearance, m_aiActor.PathableTiles, false, FullCellValidator);
                    }
                    IntVector2? targetPos5 = m_targetPos;
                    if (!targetPos5.HasValue)
                    {
                        return BehaviorResult.Continue;
                    }
                    m_aiActor.PathfindToPosition(m_targetPos.Value.ToCenterVector2());
                }
                if (PreventFiringWhileMoving)
                {
                    return BehaviorResult.SkipAllRemainingBehaviors;
                }
                return BehaviorResult.SkipRemainingClassBehaviors;
            }
            catch (Exception e)
            {
                ETGModConsole.Log($"{e}");
                return BehaviorResult.SkipRemainingClassBehaviors;
            }
        }

        public void ResetPauseTimer()
        {
            m_pauseTimer = 0f;
        }

        private bool SimpleCellValidator(IntVector2 c)
        {
            for (int i = 0; i < m_aiActor.Clearance.x; i++)
            {
                for (int j = 0; j < m_aiActor.Clearance.y; j++)
                {
                    if (GameManager.Instance.Dungeon.data.isTopWall(c.x + i, c.y + j))
                    {
                        return false;
                    }
                }
            }
            if (StayOnScreen && (c.x < m_cachedCameraBottomLeft.x || c.y < m_cachedCameraBottomLeft.y || c.x + m_aiActor.Clearance.x - 1 > m_cachedCameraBottomRight.x || c.y + m_aiActor.Clearance.y - 1 > m_cachedCameraBottomRight.y))
            {
                return false;
            }
            return true;
        }

        private bool FullCellValidator(IntVector2 c)
        {
            if (!SimpleCellValidator(c))
            {
                return false;
            }
            if (AvoidTarget && (bool)m_aiActor.TargetRigidbody)
            {
                float a = (Pathfinding.Pathfinder.GetClearanceOffset(c, m_aiActor.Clearance) - m_cachedTargetPos).ToAngle();
                if (BraveMathCollege.AbsAngleBetween(a, m_cachedAngleFromTarget) > 90f)
                {
                    return false;
                }
                Vector2? cachedOtherTargetPos = m_cachedOtherTargetPos;
                if (cachedOtherTargetPos.HasValue)
                {
                    float? cachedAngleFromOtherTarget = m_cachedAngleFromOtherTarget;
                    if (cachedAngleFromOtherTarget.HasValue)
                    {
                        a = (Pathfinding.Pathfinder.GetClearanceOffset(c, m_aiActor.Clearance) - m_cachedOtherTargetPos.Value).ToAngle();
                        if (BraveMathCollege.AbsAngleBetween(a, m_cachedAngleFromOtherTarget.Value) > 90f)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}
