using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace CwaffingTheGungy
{
    public class SpinCycle : AdvancedGunBehavior
    {
        public static string gunName          = "Spin Cycle";
        public static string spriteName       = "ranger";
        public static string projectileName   = "86"; //marine sidearm
        public static string shortDescription = "Bring it Around Town";
        public static string longDescription  = "(ball and chain)";

        private static VFXPool vfx = null;
        private static Projectile theProtoBall;
        private Projectile theCurBall = null;

        public static void Add()
        {
            Gun gun = Lazy.InitGunFromStrings(gunName, spriteName, projectileName, shortDescription, longDescription);
            var comp = gun.gameObject.AddComponent<SpinCycle>();
            comp.preventNormalFireAudio = true;

            gun.gunSwitchGroup                    = (PickupObjectDatabase.GetById(198) as Gun).gunSwitchGroup;
            gun.DefaultModule.ammoCost            = 1;
            gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                        = 5f;
            gun.DefaultModule.angleVariance       = 0f;
            gun.DefaultModule.numberOfShotsInClip = 1;
            gun.quality                           = PickupObject.ItemQuality.A;
            gun.InfiniteAmmo                      = true;
            gun.SetAnimationFPS(gun.shootAnimation, 0);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);

            Projectile ball = Lazy.PrefabProjectileFromGun(gun,false);
            ball.BulletScriptSettings.surviveTileCollisions = true;
            ball.BulletScriptSettings.surviveRigidbodyCollisions = true;
            ball.baseData.speed = 0.0001f;
            ball.baseData.force  = 10f;
            ball.baseData.damage = 10f;
            ball.baseData.range = 1000000f;
            ball.PenetratesInternalWalls = true;
            ball.pierceMinorBreakables   = true;

            PierceProjModifier pierce     = ball.gameObject.GetOrAddComponent<PierceProjModifier>();
            pierce.penetration            = 100000;
            pierce.penetratesBreakables   = true;

            theProtoBall = ball;

            vfx = VFX.CreatePoolFromVFXGameObject((PickupObjectDatabase.GetById(0) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);
        }

        private void SetupBallAndChain(PlayerController p)
        {
            theCurBall = SpawnManager.SpawnProjectile(
                theProtoBall.gameObject, p.sprite.WorldCenter, Quaternion.Euler(0f, 0f, 0f), true
                ).GetComponent<Projectile>();
            theCurBall.Owner = p;

            this.forcedDirection = p.FacingDirection;
            this.facingLast      = p.FacingDirection;
            // this.curTurnSpeed    = minTurnSpeed;
            this.curMomentum     = 0;
            p.m_overrideGunAngle = this.forcedDirection;
            base.OnSwitchedToThisGun();
        }

        public override void OnSwitchedToThisGun()
        {
            SetupBallAndChain(this.gun.CurrentOwner as PlayerController);
        }

        protected override void OnPickup(GameActor owner)
        {
            if (!(owner is PlayerController))
                return;
            if (this.gun == owner.CurrentGun)
                SetupBallAndChain(owner as PlayerController);
        }

        public override void OnSwitchedAwayFromThisGun()
        {
            if (theCurBall != null)
            {
                this.theCurBall.DieInAir(true,false,false,true);
                theCurBall = null;
            }
            if (!(this.gun.CurrentOwner && this.gun.CurrentOwner is PlayerController))
                return;
            PlayerController p = this.gun.CurrentOwner as PlayerController;
            p.m_overrideGunAngle = null;
            base.OnSwitchedAwayFromThisGun();
        }

        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
            return;
            // projectile.angularVelocity = 10f;
            // projectile.UpdateSpeed();
            // projectile.SendInDirection(Lazy.AngleToVector(90f), true);
            // projectile.UpdateSpeed();
        }

        private static float maxMomentum      = 18f;  //3.0 rotations per second @60FPS
        private static float ballWeight       = 30f;  //full speed in 0.5 seconds @60FPS
        private static float maxChainLength   = 6.0f;
        private static float maxAccel         = maxMomentum/ballWeight;
        private static float airFriction      = 0.99f;
        private static bool relativeToReticle = false; //if false, ...need better documentation

        private float facingLast      = 0f;
        private float forcedDirection = 0f;
        private float curMomentum     = 0f;

        protected override void Update()
        {
            base.Update();
            if (!(this.gun.CurrentOwner && this.gun.CurrentOwner is PlayerController))
                return;

            // prevent the gun from firing entirely
            this.gun.RuntimeModuleData[this.gun.DefaultModule].onCooldown = true;

            // get the owner of the gun
            PlayerController p = this.gun.CurrentOwner as PlayerController;

            // determine the angle delta between the reticle and our current forced direction
            float deltaToTarget =
                p.FacingDirection - (relativeToReticle ? this.forcedDirection : this.facingLast);
            if (deltaToTarget > 180)
                deltaToTarget -= 360f;
            else if (deltaToTarget < -180)
                deltaToTarget += 360f;
            this.facingLast = p.FacingDirection;

            // determine the actual change in momentum, then update momentum and direction accordingly
            float accel = Mathf.Sign(deltaToTarget)*Mathf.Min(Math.Abs(deltaToTarget)/ballWeight,maxAccel);
            this.curMomentum += accel;
            if (Mathf.Abs(this.curMomentum) > maxMomentum)
                this.curMomentum = Mathf.Sign(this.curMomentum)*maxMomentum;
            this.curMomentum *= airFriction;
            this.forcedDirection += this.curMomentum;
            if (this.forcedDirection > 360f)
                this.forcedDirection -= 360f;
            else if (this.forcedDirection < 0f)
                this.forcedDirection += 360f;

            // force player to face targeting reticle
            p.m_overrideGunAngle = this.forcedDirection;

            // update chain length
            float curChainLength = maxChainLength * (this.curMomentum / maxMomentum);

            // draw VFX showing the ball's current momentum
            DrawVFXWithRespectToPlayerAngle(p,this.forcedDirection,curChainLength);

            // draw the ball itself
            if (theCurBall != null)
            {
                theCurBall.collidesWithEnemies = true;
                theCurBall.collidesWithPlayer = false;
                Vector2 ppos = (p.sprite.WorldCenter+Lazy.AngleToVector(this.forcedDirection,curChainLength)
                    ).ToVector3ZisY(-1f);
                theCurBall.specRigidbody.Position = new Position(ppos);
            }
        }

        private void DrawVFXWithRespectToPlayerAngle(PlayerController p, float angle, float mag)
        {
            Vector2 ppos = p.sprite.WorldCenter;
            vfx.SpawnAtPosition((ppos+Lazy.AngleToVector(angle,mag)).ToVector3ZisY(-1f),
                0,null, null, null, -0.05f);
        }
    }
}
