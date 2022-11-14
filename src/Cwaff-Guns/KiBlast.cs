using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod;
using MonoMod.RuntimeDetour;
using Gungeon;
using Alexandria.Misc;
using Alexandria.ItemAPI;

namespace CwaffingTheGungy
{
    public class KiBlast : AdvancedGunBehavior
    {
        public static string gunName          = "Ki Blast";
        public static string spriteName       = "fingerguns";
        public static string projectileName   = "38_special";
        public static string shortDescription = "Dragunball Z";
        public static string longDescription  = "(dakka)";

        public float nextKiBlastSign = 1;  //1 to deviate right, -1 to deviate left

        private static VFXPool vfx  = null;
        private PlayerController owner = null;
        private Vector2 currentTarget = Vector2.zero;

        public static void Add()
        {
            Gun gun = Lazy.InitGunFromStrings(gunName, spriteName, projectileName, shortDescription, longDescription);
            var comp = gun.gameObject.AddComponent<KiBlast>();
            comp.preventNormalFireAudio = true;
            comp.overrideNormalFireAudio = "Play_WPN_Vorpal_Shot_Critical_01";

            gun.gunSwitchGroup                    = (PickupObjectDatabase.GetById(198) as Gun).gunSwitchGroup;
            gun.DefaultModule.ammoCost            = 1;
            gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                        = 1.1f;
            gun.DefaultModule.cooldownTime        = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 250;
            gun.quality                           = PickupObject.ItemQuality.D;
            gun.SetBaseMaxAmmo(250);
            gun.SetAnimationFPS(gun.shootAnimation, 24);


            Projectile blast       = Lazy.PrefabProjectileFromGun(gun);
            blast.AnimateProjectile(
                new List<string> {
                    "ki_blast_001",
                    "ki_blast_002",
                    "ki_blast_003",
                    "ki_blast_004",
                }, 12, true, new IntVector2(16, 16),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);

            blast.gameObject.AddComponent<KiBlastBehavior>();

            vfx = VFX.CreatePoolFromVFXGameObject((PickupObjectDatabase.GetById(0) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);
        }

        protected override void Update()
        {
            base.Update();
            if (!(this.gun && this.gun.GunPlayerOwner()))
                return;
            PlayerController p = this.gun.GunPlayerOwner();
            this.currentTarget = RaycastToNearestWallOrEnemyOrObject(
                p.sprite.WorldCenter,p.CurrentGun.CurrentAngle);
            vfx.SpawnAtPosition(this.currentTarget.ToVector3ZisY(-1f),
                p.CurrentGun.CurrentAngle,null, null, null, -0.05f);
        }

        private static RaycastResult hit;

        private static bool ExcludePlayersAndProjectilesFromRaycasting(SpeculativeRigidbody s)
        {
            if (s.GetComponent<PlayerController>() != null)
                return true; //true == exclude players
            if (s.GetComponent<Projectile>() != null)
                return true; //true == exclude projectiles
            return false; //false == don't exclude
        }

        public static Vector2 RaycastToNearestWallOrEnemyOrObject(Vector2 pos, float angle, float minDistance = 1)
        {
            if (PhysicsEngine.Instance.Raycast(
              pos+Lazy.AngleToVector(angle,minDistance), Lazy.AngleToVector(angle), 200, out hit,
              rigidbodyExcluder: ExcludePlayersAndProjectilesFromRaycasting))
                return hit.Contact;
            return pos+Lazy.AngleToVector(angle,minDistance);
        }

        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
        }

    }

    public class KiBlastBehavior : MonoBehaviour
    {
        private static float secsToReachTarget = 0.5f;
        private static float maxAngleVariance  = 60f;
        private static float minSpeed = 5.0f;

        private Projectile m_projectile;
        private PlayerController m_owner;
        private Vector2 targetPos;
        private float targetAngle;
        private float angleVariance;
        private float lifetime = 0;

        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (this.m_projectile.Owner && this.m_projectile.Owner is PlayerController)
            {
                this.m_owner      = this.m_projectile.Owner as PlayerController;
                this.targetAngle  = this.m_owner.CurrentGun.CurrentAngle;
                this.targetPos    = KiBlast.RaycastToNearestWallOrEnemyOrObject(
                    this.m_owner.sprite.WorldCenter,
                    this.targetAngle);

                KiBlast k = this.m_owner.CurrentGun.GetComponent<KiBlast>();
                this.angleVariance = UnityEngine.Random.value*maxAngleVariance*k.nextKiBlastSign;
                k.nextKiBlastSign *= -1;

                // Make sure the projectile hits the wall in exactly 60 frames
                float distanceToTarget = Vector2.Distance(this.m_owner.sprite.WorldCenter,this.targetPos);
                this.m_projectile.baseData.speed = Mathf.Max(distanceToTarget / secsToReachTarget,minSpeed);
                this.m_projectile.UpdateSpeed();
                this.m_projectile.SendInDirection(Lazy.AngleToVector(this.targetAngle-this.angleVariance), true);
            }
        }

        private void Update()
        {
            this.lifetime += BraveTime.DeltaTime;
            float percentDoneTurning = this.lifetime / secsToReachTarget;
            if (percentDoneTurning <= 1.0f)
            {
                float inflection = (2.0f*percentDoneTurning) - 1.0f;
                float newAngle = this.targetAngle + inflection * this.angleVariance;
                this.m_projectile.SendInDirection(Lazy.AngleToVector(newAngle), true);
            }
        }
    }
}
