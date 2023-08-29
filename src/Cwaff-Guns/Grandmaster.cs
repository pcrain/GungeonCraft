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

            _PawnSprite = AnimateBullet.CreateProjectileAnimation(
                new List<string> {
                    "pawn",
                }, 12, true, new IntVector2(8, 8),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.AddAnimation(_PawnSprite);
                projectile.SetAnimation(_PawnSprite);
                projectile.baseData.speed = 30f;
                EasyTrailBullet trail = projectile.gameObject.AddComponent<EasyTrailBullet>();
                    trail.TrailPos   = trail.transform.position;
                    trail.StartWidth = 0.2f;
                    trail.EndWidth   = 0f;
                    trail.LifeTime   = 0.1f;
                    trail.BaseColor  = Color.red;
                    trail.EndColor   = Color.red;
                ChessPieceBehavior pop = projectile.gameObject.AddComponent<ChessPieceBehavior>();

        }
    }

    public class ChessPieceBehavior : MonoBehaviour
    {
        private static float _MoveDist  = 180f;
        private static float _MoveTime  = 6.0f/60.0f;
        private static float _MovePause = 24.0f/60.0f;
        private static float _MoveSpeed = _MoveDist / (_MoveTime * C.FPS);

        private Projectile _projectile  = null;
        private PlayerController _owner = null;
        private float _lifetime         = 0.0f;
        private bool _paused            = false;
        private float _speed            = 0.0f;
        private EasyTrailBullet _trail  = null;

        private void Start()
        {
            this._projectile = base.GetComponent<Projectile>();
            if (this._projectile.Owner is not PlayerController pc)
                return;

            this._trail = this._projectile.gameObject.GetComponent<EasyTrailBullet>();

            this._owner = pc;
            this._speed = this._projectile.baseData.speed;
            this._speed = _MoveSpeed;
        }

        private void ContinuePause()
        {
            if (this._lifetime < _MovePause)
                return;

            this._projectile.collidesWithEnemies = true;
            this._projectile.baseData.speed = this._speed;
            this._projectile.UpdateSpeed();
            this._trail.Enable();

            this._paused = false;
            this._lifetime = 0.0f;
        }

        private void ContinueMove()
        {
            if (this._lifetime < _MoveTime)
                return;

            AkSoundEngine.PostEvent("chess_move", this.gameObject);
            this._projectile.collidesWithEnemies = false;
            this._projectile.baseData.speed = 0.001f;
            this._projectile.UpdateSpeed();
            this._trail.Disable();

            this._paused = true;
            this._lifetime = 0.0f;
        }

        private void Update()
        {
            this._lifetime += BraveTime.DeltaTime;
            if (this._paused)
                ContinuePause();
            else
                ContinueMove();

            // if (UnityEngine.Random.Range(0f, 1f) > 0.1f)
            //     return;
            // this._projectile.SendInDirection(Lazy.RandomVector(), true);
            // if (this.runningInCircles)
            // {
            //     this.lifetime += BraveTime.DeltaTime;
            //     float newspeed = Math.Min(this.rotateSpeed,this.rotateSpeed * this.lifetime);

            //     // NOTE: SendInDirection doesn't account for vector magnitude, so calculating speed before
            //     //   calculating vectors leads to some janky, non-circular movement,
            //     //   but I'm leaving it in because it looks neat :D
            //     this._projectile.baseData.speed = driftSpeed;
            //     this._projectile.UpdateSpeed();

            //     Vector2 circularComponent =
            //         BraveMathCollege.DegreesToVector(this.offsetAngle+angularSpeed*(this.lifetime - Mathf.Floor(this.lifetime)),newspeed);
            //     Vector2 straightComponent =
            //         BraveMathCollege.DegreesToVector(this.targetAngle,this.driftSpeed);

            //     this._projectile.SendInDirection(circularComponent+straightComponent, true);
            // }
        }

        // private void DoLaunch()
        // {
        //     this.runningInCircles = false;
        //     this._projectile.baseData.speed = this.launchSpeed;
        //     this._projectile.UpdateSpeed();
        //     this._projectile.SendInDirection(BraveMathCollege.DegreesToVector(this.targetAngle), true);
        //     AkSoundEngine.PostEvent("Play_WPN_blasphemy_shot_01", this._projectile.gameObject);
        // }
    }
}
