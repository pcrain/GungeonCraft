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

        private static float kiReflectRange = 3.0f;

        public float nextKiBlastSign = 1;  //1 to deviate right, -1 to deviate left

        private static VFXPool vfx  = null;
        private PlayerController owner = null;
        private Vector2 currentTarget = Vector2.zero;

        public static void Add()
        {
            Gun gun = Lazy.InitGunFromStrings(gunName, spriteName, projectileName, shortDescription, longDescription);
            var comp = gun.gameObject.AddComponent<KiBlast>();
            comp.preventNormalReloadAudio = true;
            comp.preventNormalFireAudio = true;
            comp.overrideNormalFireAudio = "Play_WPN_Vorpal_Shot_Critical_01";

            gun.gunSwitchGroup                    = (PickupObjectDatabase.GetById(198) as Gun).gunSwitchGroup;
            gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.DefaultModule.cooldownTime        = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 99999;
            gun.reloadTime                        = 0f;
            gun.quality                           = PickupObject.ItemQuality.D;
            gun.InfiniteAmmo                      = true;
            gun.SetBaseMaxAmmo(99999);
            gun.SetAnimationFPS(gun.shootAnimation, 24);

            Projectile blast = Lazy.PrefabProjectileFromGun(gun);
            blast.AnimateProjectile(
                new List<string> {
                    "ki_blast_001",
                    "ki_blast_002",
                    "ki_blast_003",
                    "ki_blast_004",
                }, 12, true, new IntVector2(10, 10),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);
            blast.gameObject.AddComponent<KiBlastBehavior>();

            vfx = VFX.CreatePoolFromVFXGameObject((PickupObjectDatabase.GetById(0) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);
        }

        public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
        {
            base.OnReloadPressed(player, gun, manualReload);
            float closestDistance = 999f;
            KiBlastBehavior closestBlast = null;
            for (int i = 0; i < StaticReferenceManager.AllProjectiles.Count; i++)
            {
                Projectile p = StaticReferenceManager.AllProjectiles[i];
                KiBlastBehavior k = p.GetComponent<KiBlastBehavior>();
                if (k == null || (!k.reflected))
                    continue;
                float distanceToPlayer = Vector2.Distance(player.sprite.WorldCenter,p.sprite.WorldCenter);
                if (distanceToPlayer > kiReflectRange)
                    continue;
                if (distanceToPlayer > closestDistance)
                    continue;
                closestDistance = distanceToPlayer;
                closestBlast = k;
            }
            if (closestBlast != null)
                closestBlast.ReturnToPlayer(player);
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
        private static float defaultSecsToReachTarget = 0.5f;
        private static float maxAngleVariance  = 60f;
        private static float minSpeed = 15.0f;

        public bool reflected = false;

        private Projectile m_projectile;
        private PlayerController m_owner;
        private Vector2 targetPos;
        private float targetAngle;
        private float angleVariance;
        private float lifetime = 0;
        private float timeToReachTarget;
        private float actualTimeToReachTarget;

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

                this.m_projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;

                KiBlast k = this.m_owner.CurrentGun.GetComponent<KiBlast>();
                if (k != null)
                {
                    this.angleVariance = UnityEngine.Random.value*maxAngleVariance*k.nextKiBlastSign;
                    k.nextKiBlastSign *= -1;
                }
                else
                    ETGModConsole.Log("that should never happen o.o");

                SetNewTarget(this.targetPos, defaultSecsToReachTarget);
            }
        }

        public void SetNewTarget(Vector2 target, float secsToReachTarget)
        {
            this.lifetime = 0;
            this.targetPos = target;
            this.timeToReachTarget = secsToReachTarget;
            Vector2 curpos = this.m_projectile.specRigidbody.Position.GetPixelVector2();
            Vector2 delta  = (this.targetPos-curpos);
            this.targetAngle = delta.ToAngle();
            float distanceToTarget = Vector2.Distance(curpos,this.targetPos);
            this.m_projectile.baseData.speed = Mathf.Max(distanceToTarget / this.timeToReachTarget,minSpeed);
            this.actualTimeToReachTarget = distanceToTarget / this.m_projectile.baseData.speed;
            this.m_projectile.UpdateSpeed();
            this.m_projectile.SendInDirection(Lazy.AngleToVector(this.targetAngle-this.angleVariance), true);
        }

        private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            if (!this.reflected)
            {
                AIActor enemy = otherRigidbody.GetComponent<AIActor>();
                if (enemy == null)
                    return;
                PhysicsEngine.SkipCollision = true;

                Projectile p = this.m_projectile;
                // p.AdjustPlayerProjectileTint(Color.red, 2, 0.1f);
                p.Owner = enemy;
                p.collidesWithPlayer = true;
                p.collidesWithEnemies = false;
                this.reflected = true;

                AkSoundEngine.PostEvent("Play_WPN_Vorpal_Shot_Critical_01", enemy.gameObject);
                SetNewTarget(this.m_owner.sprite.WorldCenter, this.timeToReachTarget);
            }
        }

        public void ReturnToPlayer(PlayerController player)
        {
            if (!this.reflected)
                return;
            Projectile p = this.m_projectile;
            AIActor enemy = p.Owner as AIActor;
            if (enemy == null)
                return;
            p.Owner = player;
            // p.AdjustPlayerProjectileTint(Color.green, 2, 0.1f);
            p.collidesWithPlayer = false;
            p.collidesWithEnemies = true;
            this.reflected = false;
            AkSoundEngine.PostEvent("Play_WPN_Vorpal_Shot_Critical_01", enemy.gameObject);
            SetNewTarget(enemy.sprite.WorldCenter, this.timeToReachTarget);
        }

        private void Update()
        {
            this.lifetime += BraveTime.DeltaTime;
            float percentDoneTurning = this.lifetime / this.actualTimeToReachTarget;
            if (percentDoneTurning <= 1.0f)
            {
                float inflection = (2.0f*percentDoneTurning) - 1.0f;
                float newAngle = this.targetAngle + inflection * this.angleVariance;
                this.m_projectile.SendInDirection(Lazy.AngleToVector(newAngle), true);
            }
           //  this.m_projectile.HasDefaultTint = true;
           //  if (this.m_projectile.Owner == this.m_owner)
           //     this.m_projectile.DefaultTintColor = Color.green;
           // else
           //     this.m_projectile.DefaultTintColor = Color.red;
        }
    }
}
