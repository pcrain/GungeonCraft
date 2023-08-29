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
                    trail.BaseColor  = Color.red;
                    trail.EndColor   = Color.red;
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
        protected static float _MoveDist  = 180f; // number of pixels to travel each move
        protected static float _MoveTime  = 0.1f; // seconds for a piece to move from one position to another
        protected static float _MovePause = 0.4f; // seconds a piece waits after moving before moving again
        protected static float _MoveSpeed = _MoveDist / (_MoveTime * C.FPS);

        protected tk2dSpriteAnimationClip _sprite = null;
        protected Projectile _projectile  = null;
        protected PlayerController _owner = null;
        protected float _lifetime         = 0.0f;
        protected bool _paused            = false;
        protected float _speed            = 0.0f;
        protected EasyTrailBullet _trail  = null;

        private void Start()
        {
            ETGModConsole.Log($"chose {this.GetType()}");
        }

        public void Setup(Projectile projectile, PlayerController owner, EasyTrailBullet trail)
        {
            this._projectile = projectile;
            this._owner      = owner;
            this._trail      = trail;

            this._sprite     = Grandmaster._PawnSprite;
            this._speed      = _MoveSpeed;
        }

        private void Update()
        {
            this._lifetime += BraveTime.DeltaTime;
            if (this._paused)
            {
                if (this._lifetime >= _MovePause)
                {
                    StartMoving();
                    this._trail.Enable();
                    this._projectile.collidesWithEnemies = true;
                    this._paused                         = false;
                    this._lifetime                       = 0.0f;
                }
            }
            else
            {
                if (this._lifetime >= _MoveTime)
                {
                    StopMoving();
                    this._trail.Disable();
                    this._projectile.collidesWithEnemies = false;
                    this._paused                         = true;
                    this._lifetime                       = 0.0f;
                }
            }
            // this._projectile.SendInDirection(Lazy.RandomVector(), true);
        }

        protected Vector2? ChooseNewTarget()
        {
            return null;
        }

        public void StartMoving()
        {
            if (ChooseNewTarget() is Vector2 target)
                this._projectile.SendInDirection(target - this._projectile.sprite.WorldCenter, true);
            this._projectile.baseData.speed = this._speed;
            this._projectile.UpdateSpeed();
        }

        public void StopMoving()
        {
            AkSoundEngine.PostEvent("chess_move", this.gameObject);
            this._projectile.baseData.speed = 0.001f;
            this._projectile.UpdateSpeed();
        }
    }

    public class PawnPiece   : ChessPiece { }
    public class RookPiece   : ChessPiece { }
    public class BishopPiece : ChessPiece { }
    public class KnightPiece : ChessPiece { }
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
                case ChessPieces.Queen:
                    this._piece = this._projectile.gameObject.AddComponent<QueenPiece>();
                    break;
                case ChessPieces.King:
                    this._piece = this._projectile.gameObject.AddComponent<KingPiece>();
                    break;
            }

            this._owner = pc;
            EasyTrailBullet trail = this._projectile.gameObject.GetComponent<EasyTrailBullet>();
            this._piece.Setup(this._projectile, this._owner, trail);
        }
    }
}
