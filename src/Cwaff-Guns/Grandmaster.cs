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
    public class Grandmaster : AdvancedGunBehavior
    {
        public static string ItemName         = "Grandmaster";
        public static string SpriteName       = "grandmaster";
        public static string ProjectileName   = "ak-47"; // no rotation
        public static string ShortDescription = "Magnum Carlsen";
        public static string LongDescription  = "(TBD)";

        internal static tk2dSpriteAnimationClip _PawnSprite;
        internal static tk2dSpriteAnimationClip _RookSprite;
        internal static tk2dSpriteAnimationClip _BishopSprite;
        internal static tk2dSpriteAnimationClip _KnightSprite;
        internal static tk2dSpriteAnimationClip _QueenSprite;
        internal static tk2dSpriteAnimationClip _KingSprite;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
            var comp = gun.gameObject.AddComponent<Grandmaster>();

            gun.gunSwitchGroup                    = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup;
            gun.DefaultModule.ammoCost            = 1;
            gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                        = 1.1f;
            gun.DefaultModule.cooldownTime        = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 20;
            gun.quality                           = PickupObject.ItemQuality.A;
            gun.SetBaseMaxAmmo(250);
            gun.SetAnimationFPS(gun.shootAnimation, 24);

            _PawnSprite   = AnimateBullet.CreateProjectileAnimation(new() { "chess_pawn", }, 12, true, new IntVector2(8, 12), false,
                tk2dBaseSprite.Anchor.MiddleCenter, true, true);
            _RookSprite   = AnimateBullet.CreateProjectileAnimation(new() { "chess_rook", }, 12, true, new IntVector2(8, 12), false,
                tk2dBaseSprite.Anchor.MiddleCenter, true, true);
            _BishopSprite = AnimateBullet.CreateProjectileAnimation(new() { "chess_bishop", }, 12, true, new IntVector2(8, 12), false,
                tk2dBaseSprite.Anchor.MiddleCenter, true, true);
            _KnightSprite = AnimateBullet.CreateProjectileAnimation(new() { "chess_knight", }, 12, true, new IntVector2(8, 12), false,
                tk2dBaseSprite.Anchor.MiddleCenter, true, true);
            _QueenSprite  = AnimateBullet.CreateProjectileAnimation(new() { "chess_queen", }, 12, true, new IntVector2(8, 12), false,
                tk2dBaseSprite.Anchor.MiddleCenter, true, true);
            _KingSprite   = AnimateBullet.CreateProjectileAnimation(new() { "chess_king", }, 12, true, new IntVector2(8, 12), false,
                tk2dBaseSprite.Anchor.MiddleCenter, true, true);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.AddAnimation(_PawnSprite);
                projectile.AddAnimation(_RookSprite);
                projectile.AddAnimation(_BishopSprite);
                projectile.AddAnimation(_KnightSprite);
                projectile.AddAnimation(_QueenSprite);
                projectile.AddAnimation(_KingSprite);
                projectile.SetAnimation(_PawnSprite);
                projectile.baseData.speed = 30f;
                EasyTrailBullet trail = projectile.gameObject.AddComponent<EasyTrailBullet>();
                    trail.TrailPos   = trail.transform.position;
                    trail.StartWidth = 0.2f;
                    trail.EndWidth   = 0f;
                    trail.LifeTime   = 0.1f;
                    trail.BaseColor  = Color.yellow;
                    trail.StartColor = Color.yellow;
                    trail.EndColor   = Color.yellow;
                PlayChessBehavior pop = projectile.gameObject.AddComponent<PlayChessBehavior>();

        }
    }

    public enum ChessPieces {
        Pawn   = 0,
        Rook   = 1,
        Bishop = 2,
        Knight = 3,
        Queen  = 4,
        King   = 5,
    };

    public class ChessPiece : MonoBehaviour
    {
        protected static float _BaseMoveDist  = 180f; // number of max pixels to travel each move
        protected static float _BaseMoveTime  = 0.1f; // seconds for a piece to move from one position to another
        protected static float _BaseMovePause = 0.4f; // seconds a piece waits after moving before moving again

        protected tk2dSpriteAnimationClip _sprite = null;
        protected Projectile _projectile  = null;
        protected PlayerController _owner = null;
        protected float _lifetime         = 0.0f;
        protected int _movePhase          = 1;     // 1 == moving, 2 == knight's second move, 0 == paused
        protected float _speed            = 0.0f;
        protected EasyTrailBullet _trail  = null;
        protected Vector2? _target        = null;  // the target position we're actually aiming at
        protected Vector2  _targetVec     = Vector2.zero;  // the vector we are traveling to reach that target position

        protected float _nextDir          = 0;     // the diretion we intend to move / scan for targets after our current move
        protected AIActor _targetEnemy    = null;  // which enemy, if any, we're currently targeting
        protected bool _twoPhaseMove      = false; // whether the piece moves in two phases; true only for knight

        private void Start()
        {
            // ETGModConsole.Log($"chose {this.GetType()}");
        }

        private void Update()
        {
            this._lifetime += BraveTime.DeltaTime;
            if (this._movePhase == 0)
            {
                if (this._lifetime >= _BaseMovePause)
                {
                    StartMoving();
                    this._trail.Enable();
                    this._projectile.collidesWithEnemies = true;
                    this._movePhase                      = 1;
                    this._lifetime                       = 0.0f;
                    if (this._twoPhaseMove)
                    {
                        // Get the magnitude of the both components of the vector
                        float xmag = Mathf.Abs(this._targetVec.x);
                        float ymag = Mathf.Abs(this._targetVec.y);
                        // Return the larger component on phase 1
                        Vector2 axisVec;
                        if (xmag > ymag)
                            axisVec = this._targetVec.WithY(0);
                        else
                            axisVec = this._targetVec.WithX(0);
                        // ETGModConsole.Log($"sending in major direction {axisVec}");
                        this._projectile.SendInDirection(axisVec, true);
                    }
                }
                else
                    UpdatePaused();
            }
            else if (this._twoPhaseMove && this._movePhase == 1)
            {
                if (this._lifetime >= _BaseMoveTime)
                {
                    // Get the magnitude of the both components of the vector
                    float xmag = Mathf.Abs(this._targetVec.x);
                    float ymag = Mathf.Abs(this._targetVec.y);
                    // Return the smaller component on phase 2
                    Vector2 axisVec;
                    if (xmag > ymag)
                        axisVec = this._targetVec.WithX(0);
                    else
                        axisVec = this._targetVec.WithY(0);
                    // ETGModConsole.Log($"sending in minor direction {axisVec}");
                    this._projectile.SendInDirection(axisVec, true);
                    this._movePhase = 2; // very intentionally not resetting lifetime here
                }
                else
                    UpdateMoving();
            }
            else
            {
                if (this._lifetime >= (_BaseMoveTime * this._movePhase)) // double the wait time for knight moves
                {
                    StopMoving();
                    this._trail.Disable();
                    this._projectile.collidesWithEnemies = false;
                    this._movePhase                      = 0;
                    this._lifetime                       = 0.0f;
                }
                else
                    UpdateMoving();
            }
            UpdateAlways();
        }

        protected float ComputeSpeed(float dist, float time)
        {
            return dist / (time * C.FPS);
        }

        public void Setup(Projectile projectile, PlayerController owner, EasyTrailBullet trail)
        {
            this._projectile = projectile;
            this._owner      = owner;
            this._trail      = trail;

            this._sprite     = GetSprite();
            this._speed      = ComputeSpeed(GetMoveDistance(), GetMoveTime());

            this._projectile.SetAnimation(this._sprite);
            UpdateCreate();
        }

        protected virtual tk2dSpriteAnimationClip GetSprite() => Grandmaster._PawnSprite;
        protected virtual float GetMoveDistance()             => _BaseMoveDist;
        protected virtual float GetMoveTime()                 => _BaseMoveTime;

        protected virtual Vector2? ChooseNewTarget()
        {
            return null; // don't change target by default
        }
        protected virtual void UpdateCreate() { /* do nothing by default */ }
        protected virtual void UpdatePaused() { /* do nothing by default */ }
        protected virtual void UpdateMoving() { /* do nothing by default */ }
        protected virtual void UpdateAlways() { /* do nothing by default */ }

        public void StartMoving()
        {
            this._target = ChooseNewTarget();
            if (this._target is Vector2 target)
            {
                this._targetVec = target - this._projectile.sprite.WorldCenter;
                this._projectile.SendInDirection(this._targetVec, true);
                float adjSpeed = this._targetVec.magnitude / _BaseMoveTime;
                this._projectile.baseData.speed = Mathf.Min(this._speed, adjSpeed);
            }
            else
            {
                this._projectile.baseData.speed = this._speed;
            }
            this._projectile.UpdateSpeed();
        }

        public void StopMoving()
        {
            AkSoundEngine.PostEvent("chess_move", this.gameObject);
            this._projectile.baseData.speed = 0.001f;
            this._projectile.UpdateSpeed();
        }

        protected float LockAngleToOneOf(float angle, List<float> angles)
        {
            float a = angle.Clamp360();

            float best      = 0.0f;
            float bestDelta = 999f;
            foreach(float c in angles)
            {
                float delta = Mathf.Abs(c - a);
                if (delta > bestDelta)
                    continue;
                bestDelta = delta;
                best = c;
            }
            return best;
        }

        protected virtual float GetBestValidAngleForPiece(float angle)
        {
            return angle;
        }

        protected Vector2? PointOrthognalTo(Vector2 start, Vector2 target, Vector2 dir, float projAmount = 1000f)
        {
            // Project a point outward from start in direction dir by amount projAmount
            Vector2 projection = start + (projAmount * dir);

            // Project a line orthogonal to dir through our target
            Vector2 ortho = projAmount * dir.Rotate(degrees: 90);
            Vector2 bbeg  = target + ortho;
            Vector2 bend  = target - ortho;

            // Find the orthogonal intersection point, or return null if no such point exists
            Vector2 ipoint;
            if (!BraveUtility.LineIntersectsLine(start, projection, bbeg, bend, out ipoint))
                return null;
            return ipoint;
        }

        protected Vector2? ScanForTarget()
        {
            // If we've already found a target previously, we have nothing else to do
            if (this._targetEnemy)
            {
                if (!this._targetEnemy.healthHaver.IsDead)
                {
                    float bestAngle = GetBestValidAngleForPiece((this._targetEnemy.sprite.WorldCenter - this._projectile.sprite.WorldCenter).ToAngle());
                    return PointOrthognalTo(this._projectile.sprite.WorldCenter, this._targetEnemy.sprite.WorldCenter, bestAngle.ToVector());
                }
                this._targetEnemy = null; // reset our target and march onward
            }

            // Rook should march forward until an enemy is in its line of sight on one axis
            List<AIActor> activeEnemies = this._projectile.transform.position.GetAbsoluteRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            if (activeEnemies == null || activeEnemies.Count == 0)
                return null;

            // Get our position and direction
            Vector2 abeg = this._projectile.sprite.WorldCenter;
            Vector2 dir  = this._nextDir.ToVector();

            // Project an imaginary line in our current direction
            Vector2 aend = abeg + (1000f * dir);

            // Get a line orthogonal to our current direction
            Vector2 ortho = 1000f * dir.Rotate(degrees: 90);

            // Find the closest viable enemy == one which we can move into the line of sight
            Vector2? closestViableEnemyPosition = null;
            float closestDistance = 999999f;
            foreach (AIActor enemy in activeEnemies)
            {
                if (!enemy.IsNormalEnemy || !enemy.healthHaver || enemy.IsHarmlessEnemy)
                    continue; // we only care about normal, alive, hostile enemies

                // Get the enemy's position and distance
                Vector2 epos = enemy.sprite.WorldCenter;
                float edist  = (epos - abeg).magnitude;
                if (edist >= closestDistance)
                    continue; // we only care about the closest enemy

                // Check if we can move orthogonal to the enemy in our current direction
                Vector2? ipoint = PointOrthognalTo(abeg, epos, dir);
                if (!ipoint.HasValue)
                    continue; // if we're not orthogonal to the enemy, we don't care

                // Check if there's an obstruction between us on the intersection point
                float dist = (ipoint.Value - abeg).magnitude;
                RaycastResult collision;
                if (PhysicsEngine.Instance.Raycast(abeg, dir, dist, out collision, true, false))
                    continue; // if we collide with a wall, we don't care

                // If there's no collision, it's a good position!
                closestDistance            = edist;
                closestViableEnemyPosition = ipoint;
                this._nextDir              = GetBestValidAngleForPiece((epos - abeg).ToAngle());
                this._targetEnemy          = enemy;
            }

            return closestViableEnemyPosition;
        }
    }

    public class PawnPiece   : ChessPiece { }

    public class RookPiece   : ChessPiece
    {
        protected override tk2dSpriteAnimationClip GetSprite() => Grandmaster._RookSprite;
        protected override float GetMoveDistance()             => 450f;
        protected override float GetMoveTime()                 => ChessPiece._BaseMoveTime;

        protected override void UpdateCreate()
        {
            // Rook should snap to 90 degree angles
            float angle = this._projectile.m_currentDirection.ToAngle();
            this._nextDir = GetBestValidAngleForPiece(angle);
            this._projectile.SendInDirection(this._nextDir.ToVector(), true);

            this._trail.BaseColor  = Color.magenta;
            this._trail.StartColor = Color.magenta;
            this._trail.EndColor   = Color.magenta;
        }

        private static List<float> _AnglesOf90 = new(){0f, 90f, 180f, 270f};
        protected override float GetBestValidAngleForPiece(float angle)
        {
            return LockAngleToOneOf(angle, _AnglesOf90);
        }

        protected override Vector2? ChooseNewTarget()
        {
            return ScanForTarget();
        }
    }

    public class BishopPiece : ChessPiece {
        protected override tk2dSpriteAnimationClip GetSprite() => Grandmaster._BishopSprite;
        protected override float GetMoveDistance()             => 350f;
        protected override float GetMoveTime()                 => ChessPiece._BaseMoveTime;

        protected override void UpdateCreate()
        {
            // Bishop should snap to 45 degree angles
            float angle = this._projectile.m_currentDirection.ToAngle();
            this._nextDir = GetBestValidAngleForPiece(angle);
            this._projectile.SendInDirection(this._nextDir.ToVector(), true);

            this._trail.BaseColor  = Color.cyan;
            this._trail.StartColor = Color.cyan;
            this._trail.EndColor   = Color.cyan;
        }

        private static List<float> _AnglesOf45 = new(){45f, 135f, 225f, 315f};
        protected override float GetBestValidAngleForPiece(float angle)
        {
            return LockAngleToOneOf(angle, _AnglesOf45);
        }

        protected override Vector2? ChooseNewTarget()
        {
            return ScanForTarget();
        }
    }

    public class KnightPiece : ChessPiece {
        protected override tk2dSpriteAnimationClip GetSprite() => Grandmaster._KnightSprite;
        protected override float GetMoveDistance()             => 250f;
        protected override float GetMoveTime()                 => ChessPiece._BaseMoveTime;

        protected override void UpdateCreate()
        {
            // Knight should snap to 30 degree angles
            float angle = this._projectile.m_currentDirection.ToAngle();
            this._nextDir = GetBestValidAngleForPiece(angle);
            this._targetVec = this._nextDir.ToVector();
            this._projectile.SendInDirection(this._nextDir.ToVector(), true);

            // Knights also have a two step move
            this._twoPhaseMove = true;
            this._movePhase = 2;

            this._trail.BaseColor  = Color.green;
            this._trail.StartColor = Color.green;
            this._trail.EndColor   = Color.green;
        }

        private static List<float> _AnglesOf30 = new(){30f, 60f, 120f, 150f, 210f, 240f, 300f, 330f};
        protected override float GetBestValidAngleForPiece(float angle)
        {
            return LockAngleToOneOf(angle, _AnglesOf30);
        }

        protected override Vector2? ChooseNewTarget()
        {
            return ScanForTarget();
        }
    }

    public class QueenPiece  : ChessPiece { }
    public class KingPiece   : ChessPiece { }

    public class PlayChessBehavior : MonoBehaviour
    {
        private Projectile _projectile  = null;
        private PlayerController _owner = null;
        private ChessPiece _piece       = null;

        private void Start()
        {
            this._projectile = base.GetComponent<Projectile>();
            if (this._projectile.Owner is not PlayerController pc)
                return;
            this._owner = pc;

            switch(Lazy.ChooseRandom<ChessPieces>())
            {
                case ChessPieces.Pawn:
                    this._piece = this._projectile.gameObject.AddComponent<PawnPiece>();
                    break;
                case ChessPieces.Rook:
                    this._piece = this._projectile.gameObject.AddComponent<RookPiece>();
                    break;
                case ChessPieces.Bishop:
                    this._piece = this._projectile.gameObject.AddComponent<BishopPiece>();
                    break;
                case ChessPieces.Knight:
                    this._piece = this._projectile.gameObject.AddComponent<KnightPiece>();
                    break;
                default:
                    this._piece = this._projectile.gameObject.AddComponent<PawnPiece>();
                    break;
                // case ChessPieces.Queen:
                //     this._piece = this._projectile.gameObject.AddComponent<QueenPiece>();
                //     break;
                // case ChessPieces.King:
                //     this._piece = this._projectile.gameObject.AddComponent<KingPiece>();
                //     break;
            }
            // this._piece = this._projectile.gameObject.AddComponent<KnightPiece>();
            // this._piece = this._projectile.gameObject.AddComponent<RookPiece>();
            // this._piece = this._projectile.gameObject.AddComponent<BishopPiece>();

            EasyTrailBullet trail = this._projectile.gameObject.GetComponent<EasyTrailBullet>();
            this._piece.Setup(this._projectile, pc, trail);
        }
    }
}
