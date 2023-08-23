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
    public class KiBlast : AdvancedGunBehavior
    {
        public static string ItemName         = "Ki Blast";
        public static string SpriteName       = "fingerguns";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "Dragunball Z";
        public static string LongDescription  = "(dakka)";

        public static tk2dSpriteAnimationClip KiSprite;
        public static tk2dSpriteAnimationClip KiSpriteRed;

        private static float _KiReflectRange = 3.0f;
        private static VFXPool _Vfx  = null;

        private PlayerController _owner = null;
        private Vector2 _currentTarget = Vector2.zero;

        public float nextKiBlastSign = 1;  //1 to deviate right, -1 to deviate left

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
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
                gun.SetFireAudio("Play_WPN_Vorpal_Shot_Critical_01");

            var comp = gun.gameObject.AddComponent<KiBlast>();
                comp.preventNormalReloadAudio = true;

            KiSprite = AnimateBullet.CreateProjectileAnimation(
                new List<string> {
                    "ki_blast_001",
                    "ki_blast_002",
                    "ki_blast_003",
                    "ki_blast_004",
                }, 12, true, new IntVector2(8, 8),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);

            KiSpriteRed = AnimateBullet.CreateProjectileAnimation(
                new List<string> {
                    "ki_blast_red_001",
                    "ki_blast_red_002",
                    "ki_blast_red_003",
                    "ki_blast_red_004",
                }, 12, true, new IntVector2(8, 8),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);

            Projectile blast = Lazy.PrefabProjectileFromGun(gun);
                blast.AddAnimation(KiSprite);
                blast.AddAnimation(KiSpriteRed);
                blast.SetAnimation(KiSprite);
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

            _Vfx = VFX.CreatePoolFromVFXGameObject((PickupObjectDatabase.GetById(0) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);
        }

        public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
        {
            base.OnReloadPressed(player, gun, manualReload);
            float closestDistance = _KiReflectRange;
            KiBlastBehavior closestBlast = null;
            foreach (Projectile p in StaticReferenceManager.AllProjectiles)
            {
                KiBlastBehavior k = p.GetComponent<KiBlastBehavior>();
                if (k == null || (!k.reflected))
                    continue;
                float distanceToPlayer = Vector2.Distance(player.sprite.WorldCenter,p.sprite.WorldCenter);
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
            this._currentTarget = Raycast.ToNearestWallOrEnemyOrObject(
                this.Player.sprite.WorldCenter,this.Player.CurrentGun.CurrentAngle);
            _Vfx.SpawnAtPosition(this._currentTarget.ToVector3ZisY(-1f),
                this.Player.CurrentGun.CurrentAngle,null, null, null, -0.05f);
        }

        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
        }

    }

    public class KiBlastBehavior : MonoBehaviour
    {
        private static float _DefaultSecsToReachTarget = 0.5f;
        private static float _MaxAngleVariance  = 60f;
        private static float _MinSpeed = 15.0f;
        private static float _MinReflectableLifetime = 0.4f;
        private static SlashData _BasicSlashData = null;

        private Projectile _projectile;
        private PlayerController _owner;
        private Vector2 _targetPos;
        private float _targetAngle;
        private float _angleVariance;
        private float _lifetime = 0;
        private float _timeSinceLastReflect = 0;
        private float _timeToReachTarget;
        private float _actualTimeToReachTarget;
        private int _numReflections = 0;
        private float _startingDamage;
        private float _scaling = 1.5f;

        public bool reflected = false;

        private void Start()
        {
            _BasicSlashData ??= new SlashData();
            this._projectile = base.GetComponent<Projectile>();
            this._startingDamage = this._projectile.baseData.damage;
            if (this._projectile.Owner is not PlayerController pc)
                return;

            this._owner      = pc;
            this._targetAngle  = this._owner.CurrentGun.CurrentAngle;
            this._targetPos    = Raycast.ToNearestWallOrEnemyOrObject(
                this._owner.sprite.WorldCenter,
                this._targetAngle);

            this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;

            KiBlast k = this._owner.CurrentGun.GetComponent<KiBlast>();
            if (k != null)
            {
                this._angleVariance = UnityEngine.Random.value*_MaxAngleVariance*k.nextKiBlastSign;
                k.nextKiBlastSign *= -1;
            }
            else
                ETGModConsole.Log("that should never happen o.o");

            AkSoundEngine.PostEvent("ki_blast_return_sound_stop_all", this._projectile.gameObject);
            AkSoundEngine.PostEvent("ki_blast_sound_stop_all", this._projectile.gameObject);
            AkSoundEngine.PostEvent("ki_blast_sound", this._projectile.gameObject);
            SetNewTarget(this._targetPos, _DefaultSecsToReachTarget);
        }

        public void SetNewTarget(Vector2 target, float secsToReachTarget)
        {
            this._lifetime = 0;
            this._targetPos = target;
            this._timeToReachTarget = secsToReachTarget;
            Vector2 curpos = this._projectile.specRigidbody.Position.GetPixelVector2();
            Vector2 delta  = (this._targetPos-curpos);
            this._targetAngle = delta.ToAngle();
            float distanceToTarget = Vector2.Distance(curpos,this._targetPos);
            this._projectile.baseData.speed = Mathf.Max(distanceToTarget / this._timeToReachTarget,_MinSpeed);
            this._actualTimeToReachTarget = distanceToTarget / this._projectile.baseData.speed;
            this._projectile.UpdateSpeed();
            this._projectile.SendInDirection(BraveMathCollege.DegreesToVector(this._targetAngle-this._angleVariance), true);
        }

        private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            if (this.reflected)
                return;

            AIActor enemy = otherRigidbody.GetComponent<AIActor>();
            if (enemy == null)
                return;

            if (this._projectile.baseData.damage >= enemy.healthHaver.GetCurrentHealth())
                return;

            if (this._timeSinceLastReflect < _MinReflectableLifetime)
                return; //don't want enemies to just be able to spam reflect

            // Apply damage to the enemy
            enemy.healthHaver.ApplyDamage(this._projectile.baseData.damage, this._projectile.Direction, "Ki Blast",
                CoreDamageTypes.None, DamageCategory.Collision,
                false, null, true);
            enemy.healthHaver.knockbackDoer.ApplyKnockback(this._projectile.Direction, this._projectile.baseData.force);

            // Skip the normal collision
            PhysicsEngine.SkipCollision = true;

            // Make the projectile belong to the enemy and return it towards the player
            Projectile p = this._projectile;
            p.Owner = enemy;
            p.collidesWithPlayer = true;
            p.collidesWithEnemies = false;
            this.reflected = true;

            // Update sounds and animations
            p.SetAnimation(KiBlast.KiSpriteRed);
            EasyTrailBullet trail = p.gameObject.GetComponent<EasyTrailBullet>();
                trail.BaseColor = Color.red;
                trail.EndColor = Color.red;
                trail.UpdateTrail();

            // AkSoundEngine.PostEvent("Play_WPN_Vorpal_Shot_Critical_01", enemy.gameObject);
            AkSoundEngine.PostEvent("ki_blast_return_sound_stop_all", this._projectile.gameObject);
            AkSoundEngine.PostEvent("ki_blast_sound_stop_all", this._projectile.gameObject);
            AkSoundEngine.PostEvent("ki_blast_return_sound", this._projectile.gameObject);
            SetNewTarget(this._owner.sprite.WorldCenter, this._timeToReachTarget);
        }

        // TODO: misnomer, we're returning *from* the player
        public void ReturnToPlayer(PlayerController player)
        {
            if (!this.reflected)
                return;

            Projectile p = this._projectile;
            if (p.Owner is not AIActor enemy)
                return;

            ++this._numReflections;
            this.reflected = false;
            this._timeSinceLastReflect = 0.0f;
            this._projectile.baseData.damage = this._startingDamage*Mathf.Pow(this._scaling,this._numReflections);

            p.Owner = player;
            // p.AdjustPlayerProjectileTint(Color.green, 2, 0.1f);
            p.collidesWithPlayer = false;
            p.collidesWithEnemies = true;
            p.SetAnimation(KiBlast.KiSprite);

            EasyTrailBullet trail = p.gameObject.GetComponent<EasyTrailBullet>();
                trail.BaseColor = Color.cyan;
                trail.EndColor = Color.cyan;
                trail.UpdateTrail();

            AkSoundEngine.PostEvent("ki_blast_return_sound_stop_all", this._projectile.gameObject);
            AkSoundEngine.PostEvent("ki_blast_sound_stop_all", this._projectile.gameObject);
            AkSoundEngine.PostEvent("ki_blast_return_sound", this._projectile.gameObject);
            int enemiesToCheck = 10;
            while (enemy.healthHaver.currentHealth <= 0 && --enemiesToCheck >= 0)
                enemy = enemy.GetAbsoluteParentRoom().GetRandomActiveEnemy(false);
            SetNewTarget(enemy.sprite.WorldCenter, this._timeToReachTarget);
            SlashDoer.DoSwordSlash(
                player.sprite.WorldCenter,
                (enemy.sprite.WorldCenter-player.sprite.WorldCenter).ToAngle(),
                p.Owner,
                _BasicSlashData);
        }

        private void Update()
        {
            float deltatime = BraveTime.DeltaTime;
            this._lifetime += deltatime;
            this._timeSinceLastReflect += deltatime;
            float percentDoneTurning = this._lifetime / this._actualTimeToReachTarget;
            if (percentDoneTurning > 1.0f)
                return;

            float inflection = (2.0f*percentDoneTurning) - 1.0f;
            float newAngle = this._targetAngle + inflection * this._angleVariance;
            this._projectile.SendInDirection(BraveMathCollege.DegreesToVector(newAngle), true);
        }
    }
}
