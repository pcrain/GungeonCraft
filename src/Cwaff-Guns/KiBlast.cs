using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;

using Gungeon;
using ItemAPI;

namespace CwaffingTheGungy
{
    public class KiBlast : AdvancedGunBehavior
    {
        public static string GunName          = "Ki Blast";
        public static string SpriteName       = "fingerguns";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "Dragunball Z";
        public static string LongDescription  = "(dakka)";

        public static tk2dSpriteAnimationClip kisprite;
        public static tk2dSpriteAnimationClip kispritered;

        public float nextKiBlastSign = 1;  //1 to deviate right, -1 to deviate left

        private static float kiReflectRange = 3.0f;
        private static VFXPool vfx  = null;
        private PlayerController owner = null;
        private Vector2 currentTarget = Vector2.zero;

        public static void Add()
        {
            Gun gun = Lazy.InitGunFromStrings(GunName, SpriteName, ProjectileName, ShortDescription, LongDescription);
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
            kisprite = AnimateBullet.CreateProjectileAnimation(
                new List<string> {
                    "ki_blast_001",
                    "ki_blast_002",
                    "ki_blast_003",
                    "ki_blast_004",
                }, 12, true, new IntVector2(8, 8),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);
            blast.AddAnimation(kisprite);
            kispritered = AnimateBullet.CreateProjectileAnimation(
                new List<string> {
                    "ki_blast_red_001",
                    "ki_blast_red_002",
                    "ki_blast_red_003",
                    "ki_blast_red_004",
                }, 12, true, new IntVector2(8, 8),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);
            blast.AddAnimation(kispritered);
            blast.SetAnimation(kisprite);
            blast.baseData.damage = 4f;
            blast.ignoreDamageCaps = true;

            blast.gameObject.AddComponent<KiBlastBehavior>();

            EasyTrailBullet trail = blast.gameObject.AddComponent<EasyTrailBullet>();
            trail.TrailPos = trail.transform.position;
            trail.StartWidth = 0.2f;
            trail.EndWidth = 0f;
            trail.LifeTime = 0.1f;
            trail.BaseColor = Color.cyan;
            trail.EndColor = Color.cyan;

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
            closestBlast?.ReturnToPlayer(player);
        }

        protected override void Update()
        {
            base.Update();
            if (!this.Player)
                return;
            this.currentTarget = Raycast.ToNearestWallOrEnemyOrObject(
                this.Player.sprite.WorldCenter,this.Player.CurrentGun.CurrentAngle);
            vfx.SpawnAtPosition(this.currentTarget.ToVector3ZisY(-1f),
                this.Player.CurrentGun.CurrentAngle,null, null, null, -0.05f);
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
        private static float minReflectableLifetime = 0.4f;
        private static SlashData basicSlashData = null;

        private Projectile m_projectile;
        private PlayerController m_owner;
        private Vector2 targetPos;
        private float targetAngle;
        private float angleVariance;
        private float lifetime = 0;
        private float timeSinceLastReflect = 0;
        private float timeToReachTarget;
        private float actualTimeToReachTarget;
        private int numReflections = 0;
        private float startingDamage;
        private float scaling = 1.5f;

        public bool reflected = false;

        private void Start()
        {
            basicSlashData ??= new SlashData();
            this.m_projectile = base.GetComponent<Projectile>();
            this.startingDamage = this.m_projectile.baseData.damage;
            if (this.m_projectile.Owner && this.m_projectile.Owner is PlayerController)
            {
                this.m_owner      = this.m_projectile.Owner as PlayerController;
                this.targetAngle  = this.m_owner.CurrentGun.CurrentAngle;
                this.targetPos    = Raycast.ToNearestWallOrEnemyOrObject(
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

                AkSoundEngine.PostEvent("ki_blast_return_sound_stop_all", this.m_projectile.gameObject);
                AkSoundEngine.PostEvent("ki_blast_sound_stop_all", this.m_projectile.gameObject);
                AkSoundEngine.PostEvent("ki_blast_sound", this.m_projectile.gameObject);
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
            this.m_projectile.SendInDirection(BraveMathCollege.DegreesToVector(this.targetAngle-this.angleVariance), true);
        }

        private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            AIActor enemy = otherRigidbody.GetComponent<AIActor>();
            // Leaving this out for now because I think it's funny if enemies can team kill
            // if (this.reflected && enemy != null)
            // {
            //     PhysicsEngine.SkipCollision = true;
            //     return;
            // }
            if (!this.reflected)
            {
                if (enemy == null)
                    return;

                if (this.m_projectile.baseData.damage >= enemy.healthHaver.GetCurrentHealth())
                    return;

                if (this.timeSinceLastReflect < minReflectableLifetime)
                    return; //don't want enemies to just be able to spam reflect

                // Apply damage to the enemy
                enemy.healthHaver.ApplyDamage(this.m_projectile.baseData.damage, this.m_projectile.Direction, "Ki Blast",
                    CoreDamageTypes.None, DamageCategory.Collision,
                    false, null, true);
                enemy.healthHaver.knockbackDoer.ApplyKnockback(this.m_projectile.Direction, this.m_projectile.baseData.force);

                // Skip the normal collision
                PhysicsEngine.SkipCollision = true;

                // Make the projectile belong to the enemy and return it towards the player
                Projectile p = this.m_projectile;
                p.Owner = enemy;
                p.collidesWithPlayer = true;
                p.collidesWithEnemies = false;
                this.reflected = true;

                // Update sounds and animations
                p.SetAnimation(KiBlast.kispritered);
                EasyTrailBullet trail = p.gameObject.GetComponent<EasyTrailBullet>();
                trail.BaseColor = Color.red;
                trail.EndColor = Color.red;
                trail.UpdateTrail();
                // AkSoundEngine.PostEvent("Play_WPN_Vorpal_Shot_Critical_01", enemy.gameObject);
                AkSoundEngine.PostEvent("ki_blast_return_sound_stop_all", this.m_projectile.gameObject);
                AkSoundEngine.PostEvent("ki_blast_sound_stop_all", this.m_projectile.gameObject);
                AkSoundEngine.PostEvent("ki_blast_return_sound", this.m_projectile.gameObject);
                SetNewTarget(this.m_owner.sprite.WorldCenter, this.timeToReachTarget);
            }
        }

        // TODO: misnomer, we're returning *from* the player
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

            p.SetAnimation(KiBlast.kisprite);
            EasyTrailBullet trail = p.gameObject.GetComponent<EasyTrailBullet>();
            trail.BaseColor = Color.cyan;
            trail.EndColor = Color.cyan;
            trail.UpdateTrail();
            ++this.numReflections;
            this.timeSinceLastReflect = 0.0f;
            this.m_projectile.baseData.damage = this.startingDamage*Mathf.Pow(this.scaling,this.numReflections);
            AkSoundEngine.PostEvent("ki_blast_return_sound_stop_all", this.m_projectile.gameObject);
            AkSoundEngine.PostEvent("ki_blast_sound_stop_all", this.m_projectile.gameObject);
            AkSoundEngine.PostEvent("ki_blast_return_sound", this.m_projectile.gameObject);
            int enemiesToCheck = 10;
            while (enemy.healthHaver.currentHealth <= 0 && --enemiesToCheck >= 0)
                enemy = enemy.GetAbsoluteParentRoom().GetRandomActiveEnemy(false);
            SetNewTarget(enemy.sprite.WorldCenter, this.timeToReachTarget);
            SlashDoer.DoSwordSlash(
                player.sprite.WorldCenter,
                (enemy.sprite.WorldCenter-player.sprite.WorldCenter).ToAngle(),
                p.Owner,
                basicSlashData);
        }

        private void Update()
        {
            float deltatime = BraveTime.DeltaTime;
            this.lifetime += deltatime;
            this.timeSinceLastReflect += deltatime;
            float percentDoneTurning = this.lifetime / this.actualTimeToReachTarget;
            if (percentDoneTurning <= 1.0f)
            {
                float inflection = (2.0f*percentDoneTurning) - 1.0f;
                float newAngle = this.targetAngle + inflection * this.angleVariance;
                this.m_projectile.SendInDirection(BraveMathCollege.DegreesToVector(newAngle), true);
            }
        }
    }
}
