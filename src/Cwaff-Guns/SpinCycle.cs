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

/*
    - figure out projectile interpolation so it doesn't whiff when moving too fast
        (hack for now, just make projectile bigger)
    - add visuals for the chains
*/

namespace CwaffingTheGungy
{
    public class SpinCycle : AdvancedGunBehavior
    {
        public static string gunName          = "Spin Cycle";
        public static string spriteName       = "ranger";
        public static string projectileName   = "86"; //marine sidearm
        public static string shortDescription = "Bring it Around Town";
        public static string longDescription  = "(ball and chain)";

        private static VFXPool vfx  = null;
        private static VFXPool vfx2 = null;
        private static Projectile theProtoBall;
        private static Projectile theProtoChain;
        private Projectile theCurBall = null;
        private BasicBeamController theCurChain = null;

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
            ball.BulletScriptSettings.surviveTileCollisions      = true;
            ball.BulletScriptSettings.surviveRigidbodyCollisions = true;
            ball.baseData.speed          = 0.0001f;
            ball.baseData.force          = 100f;
            ball.baseData.damage         = 100f;
            ball.baseData.range          = 1000000f;
            ball.PenetratesInternalWalls = true;
            ball.pierceMinorBreakables   = true;

            PierceProjModifier pierce    = ball.gameObject.GetOrAddComponent<PierceProjModifier>();
            pierce.penetration           = 100000;
            pierce.penetratesBreakables  = true;

            theProtoBall = ball;

            Projectile chain = Lazy.PrefabProjectileFromGun(gun,false);
            chain.baseData.force                                  = 1f;
            chain.baseData.damage                                 = 1f;
            chain.baseData.speed                                  = 100f;
            chain.BulletScriptSettings.surviveTileCollisions      = true;
            chain.BulletScriptSettings.surviveRigidbodyCollisions = true;
            chain.PenetratesInternalWalls                         = true;
            chain.pierceMinorBreakables                           = true;

            PierceProjModifier pierce2    = chain.gameObject.GetOrAddComponent<PierceProjModifier>();
            pierce2.penetration           = 100000;
            pierce2.penetratesBreakables  = true;

            List<string> BeamAnimPaths = new List<string>()
            {
                "CwaffingTheGungy/Resources/BeamSprites/alphabeam_mid_001",
                "CwaffingTheGungy/Resources/BeamSprites/alphabeam_mid_002",
                "CwaffingTheGungy/Resources/BeamSprites/alphabeam_mid_003",
                "CwaffingTheGungy/Resources/BeamSprites/alphabeam_mid_004",
            };
            BasicBeamController chainBeam = chain.GenerateBeamPrefab(
                "CwaffingTheGungy/Resources/BeamSprites/alphabeam_mid_001",
                new Vector2(15, 7),
                new Vector2(0, 4),
                BeamAnimPaths,
                13,glowAmount:100,emissivecolouramt:100);
            chainBeam.boneType                         = BasicBeamController.BeamBoneType.Projectile;
            chainBeam.interpolateStretchedBones        = true;
            chainBeam.ContinueBeamArtToWall            = true;

            theProtoChain = chain;

            vfx = VFX.CreatePoolFromVFXGameObject((PickupObjectDatabase.GetById(0) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);
            vfx2 = (PickupObjectDatabase.GetById(33) as Gun).muzzleFlashEffects;
        }

        private void SetupBallAndChain(PlayerController p)
        {
            theCurBall = SpawnManager.SpawnProjectile(
                theProtoBall.gameObject, p.sprite.WorldCenter, Quaternion.Euler(0f, 0f, 0f), true
                ).GetComponent<Projectile>();
            theCurBall.Owner = p;
            theCurBall.Shooter = p.specRigidbody;
            theCurBall.RuntimeUpdateScale(3.0f);

            if (tieProjectilePositionToBeam)
            {
                theCurChain = BeamAPI.FreeFireBeamFromAnywhere(
                    theProtoChain, p, p.gameObject,
                    Vector2.zero, this.forcedDirection, 1000000.0f, true, true
                    ).GetComponent<BasicBeamController>();
            }

            this.forcedDirection = p.FacingDirection;
            this.facingLast      = p.FacingDirection;
            this.curMomentum     = 0;
            p.m_overrideGunAngle = this.forcedDirection;
        }

        private void DestroyBallAndChain()
        {
            if (this.theCurBall != null)
            {
                this.theCurBall.DieInAir(true,false,false,true);
                this.theCurBall = null;
            }
            if (tieProjectilePositionToBeam)
            {
                if (this.theCurChain != null)
                {
                    this.theCurChain.DestroyBeam();
                    this.theCurChain = null;
                }
            }
        }

        public override void OnSwitchedToThisGun()
        {
            SetupBallAndChain(this.gun.CurrentOwner as PlayerController);
            base.OnSwitchedToThisGun();
        }

        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            base.OnPickedUpByPlayer(player);
            player.OnRollStarted += this.OnDodgeRoll;
            if (this.gun == player.CurrentGun)
                SetupBallAndChain(player);
        }

        private void OnDodgeRoll(PlayerController player, Vector2 dirVec)
        {
            DestroyBallAndChain();
        }

        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            base.OnPostDroppedByPlayer(player);
            player.OnRollStarted -= this.OnDodgeRoll;
            DestroyBallAndChain();
        }

        public override void OnSwitchedAwayFromThisGun()
        {
            DestroyBallAndChain();
            if (!(this.gun.CurrentOwner && this.gun.CurrentOwner is PlayerController))
                return;
            PlayerController p = this.gun.CurrentOwner as PlayerController;
            p.m_overrideGunAngle = null;
            RecomputePlayerSpeed(p,1.0f);
            base.OnSwitchedAwayFromThisGun();
        }

        private static float maxMomentum         = 18f;  //3.0 rotations per second @60FPS
        private static float ballWeight          = 75f;  //full speed in 0.5 seconds @60FPS
        private static float minChainLengh       = 1.0f;
        private static float maxChainLength      = 6.0f;
        private static float airFriction         = 0.995f;
        private static bool relativeToLastFacing = true;
        private static float maxAccel            = maxMomentum/ballWeight;

        private static bool influencePlayerMomentum     = true;
        private static float minPlayerMomentum          = 0.5f;
        private static float maxPlayerMomentum          = 1.5f;
        private static float playerMomentumDelta        = maxPlayerMomentum - minPlayerMomentum;
        private static int maxChainSegments             = 5;
        private static float minGapBetweenChainSegments = 1.4f;

        // Very important variable, determines behavior dramatically
        private static bool tieProjectilePositionToBeam = true;  //makes projectile hug walls since beams collide with them

        private float facingLast      = 0f;
        private float forcedDirection = 0f;
        private float curMomentum     = 0f;

        private void RecomputePlayerSpeed(PlayerController p, float speed)
        {
            StatModifier m = new StatModifier
            {
                amount      = speed,
                statToBoost = PlayerStats.StatType.MovementSpeed,
                modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE
            };
            this.gun.passiveStatModifiers = (new StatModifier[] { m }).ToArray();
            p.stats.RecalculateStats(p, false, false);
        }

        protected override void Update()
        {
            base.Update();
            if (!(this.gun.CurrentOwner && this.gun.CurrentOwner is PlayerController))
                return;

            // get the owner of the gun
            PlayerController p = this.gun.CurrentOwner as PlayerController;

            if (theCurBall == null)
            {
                SetupBallAndChain(p);
            }

            // prevent the gun from normal firing entirely
            this.gun.RuntimeModuleData[this.gun.DefaultModule].onCooldown = true;

            // determine the angle delta between the reticle and our current forced direction
            float deltaToTarget =
                p.FacingDirection - (relativeToLastFacing ? this.facingLast : this.forcedDirection);
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
            float curChainLength
                = Mathf.Max(minChainLengh,maxChainLength * (Mathf.Abs(this.curMomentum) / maxMomentum));
            BasicBeamController.BeamBone lastBone = null;
            if (tieProjectilePositionToBeam)
            {
                // theCurChain.m_currentBeamDistance = curChainLength;
                // fancy computations to compute direction based on momentum
                float chainTargetDirection = this.forcedDirection +
                    this.curMomentum * curChainLength;  //TODO: 0.75 is magic, do real math later
                // theCurChain.Direction = Lazy.AngleToVector(chainTargetDirection);
                foreach (BasicBeamController.BeamBone b in theCurChain.m_bones)
                {
                    b.Velocity = Lazy.AngleToVector(chainTargetDirection,curChainLength*C.PIXELS_PER_TILE);
                    lastBone   = b;
                }
            }

            // update speed of owner as appropriate
            if (influencePlayerMomentum)
            {
                RecomputePlayerSpeed(p,minPlayerMomentum+playerMomentumDelta*(curChainLength/maxChainLength));
            }

            // draw VFX showing the ball's current momentum
            if (!tieProjectilePositionToBeam)
                DrawVFXWithRespectToPlayerAngle(p,this.forcedDirection,curChainLength);

            // update and draw the ball itself
            theCurBall.collidesWithEnemies = true;
            theCurBall.collidesWithPlayer = false;
            Vector2 ppos = (p.sprite.WorldCenter+Lazy.AngleToVector(this.forcedDirection,curChainLength+15f/C.PIXELS_PER_TILE) // 15 == beam sprite length
                ).ToVector3ZisY(-1f);

            Vector2 oldPos = theCurBall.specRigidbody.Position.GetPixelVector2();
            if (tieProjectilePositionToBeam && (lastBone != null))
                ppos = lastBone.Position;

            theCurBall.specRigidbody.Position = new Position(ppos);
            theCurBall.SendInDirection(ppos-oldPos,true,true);
        }

        private void DrawVFXWithRespectToPlayerAngle(PlayerController p, float angle, float mag)
        {
            Vector2 ppos   = p.sprite.WorldCenter;
            float segments = Mathf.Floor(Mathf.Min(maxChainSegments,mag/minGapBetweenChainSegments));
            float gap      = mag/segments;
            for(int i = 0 ; i < segments; ++i )
                vfx2.SpawnAtPosition((ppos+Lazy.AngleToVector(angle,i*gap)).ToVector3ZisY(-1f),
                    angle,null, null, null, -0.05f);
            vfx.SpawnAtPosition((ppos+Lazy.AngleToVector(angle,mag)).ToVector3ZisY(-1f),
                angle,null, null, null, -0.05f);
        }
    }
}
