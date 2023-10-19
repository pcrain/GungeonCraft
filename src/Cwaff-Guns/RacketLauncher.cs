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
    public class RacketLauncher : AdvancedGunBehavior
    {
        public static string ItemName         = "Racket Launcher";
        public static string SpriteName       = "racket_launcher";
        public static string ProjectileName   = "86"; //marine sidearm
        public static string ShortDescription = "Paddle to the Metal";
        public static string LongDescription  = "Launches a tennis ball that bounces off of walls, enemies, projectiles, and other obstructions. The ball can be volleyed repeatedly and increases in power, speed, and knockback with each successive volley.\n\nThe amount of speed, dexterity, and awareness required to play table tennis at the highest level is staggering to some when they first learn about it. The Racket takes patience and practice to wield to its full potential, but those willing to invest time honing their skills with it will be able to fearlessly return the most lethal of volleys with a Smile on their face.";

        internal const float _MAX_REFLECT_DISTANCE = 5f;
        internal const int   _IDLE_FPS             = 24;
        internal const int   _AMMO                 = 100; //100;

        internal static tk2dSpriteAnimationClip _BulletSprite;

        private List<TennisBall> _extantTennisBalls = new();

        public static void Add()
        {
            Gun gun = Lazy.SetupGun<RacketLauncher>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.SetAttributes(quality: PickupObject.ItemQuality.B, gunClass: GunClass.SILLY, reloadTime: 0.0f, ammo: _AMMO, canReloadNoMatterAmmo: true);
                gun.muzzleFlashEffects.type              = VFXPoolType.None;
                gun.SetAnimationFPS(gun.shootAnimation, 60);
                gun.SetAnimationFPS(gun.idleAnimation, _IDLE_FPS);
                gun.LoopAnimation(gun.idleAnimation, 0);

            ProjectileModule mod = gun.DefaultModule;
                mod.ammoCost               = 1;
                mod.shootStyle             = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle          = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime           = 0.1f;
                mod.ammoType               = GameUIAmmoType.AmmoType.SMALL_BULLET;
                mod.numberOfShotsInClip    = -1;

            _BulletSprite = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("tennis_ball").Base(),
                12, true, new IntVector2(9, 9),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);

            Projectile projectile              = Lazy.PrefabProjectileFromGun(gun);
                projectile.AddDefaultAnimation(_BulletSprite);
                projectile.baseData.damage         = 10f;
                projectile.baseData.speed          = 20f;
                projectile.baseData.range          = 300f;
                projectile.BulletScriptSettings.surviveRigidbodyCollisions = true;
                // projectile.DestroyMode = Projectile.ProjectileDestroyMode.DestroyComponent;  // must be set at creatoin time

            projectile.gameObject.AddComponent<TennisBall>();

            foreach (ProjectileModule pmod in gun.Volley.projectiles)
                pmod.ammoCost = 0;
        }

        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            base.OnPickedUpByPlayer(player);
            gun.SetAnimationFPS(gun.idleAnimation, _IDLE_FPS);
            gun.spriteAnimator.Play();
        }

        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            base.OnPostDroppedByPlayer(player);
            gun.SetAnimationFPS(gun.idleAnimation, 0);
            gun.spriteAnimator.StopAndResetFrameToDefault();
        }

        public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
        {
            if (this._extantTennisBalls.Count == 0 && gun.CurrentAmmo > 0)
            {
                mod.ammoCost = 1;
                // gun.LoseAmmo(1);
                gun.ClipShotsRemaining = gun.CurrentAmmo;
                return projectile;
            }
            Vector2 racketpos = gun.GunPlayerOwner().sprite.WorldCenter;
            foreach (TennisBall ball in this._extantTennisBalls)
            {
                if (!ball.Whackable())
                    continue;
                Vector2 delta = (ball.Position() - racketpos);
                float dist = delta.magnitude;
                float angle = delta.ToAngle();
                // make sure it's within range and not behind us
                if (dist < _MAX_REFLECT_DISTANCE && Mathf.Abs(angle - gun.CurrentAngle) < 90f)
                    ball.GotWhacked(gun.CurrentAngle.ToVector());
            }
            mod.ammoCost = 0;
            gun.ClipShotsRemaining = gun.CurrentAmmo + 1;
            return Lazy.NoProjectile();
        }

        public override void OnPostFired(PlayerController player, Gun gun)
        {
            base.OnPostFired(player, gun);
            // gun.IsGunBlocked()
        }

        public override void OnAmmoChangedSafe(PlayerController player, Gun gun)
        {
            gun.ClipShotsRemaining = gun.CurrentAmmo;
        }

        // Old reload behavior to clear all balls when reloading
        // public override void OnReloadPressedSafe(PlayerController player, Gun gun, bool manualReload)
        // {
        //     if (!manualReload)
        //         return;
        //     base.OnReloadPressedSafe(player, gun, manualReload);
        //     for (int i = this._extantTennisBalls.Count - 1; i >= 0; --i)
        //         this._extantTennisBalls[i]?.DieInAir();
        //     this._extantTennisBalls.Clear();
        // }

        public void AddExtantTennisBall(TennisBall tennisBall)
        {
            this._extantTennisBalls.Add(tennisBall);
        }

        public void RemoveExtantTennisBall(TennisBall tennisBall)
        {
            this._extantTennisBalls.Remove(tennisBall);
        }
    }

    public class TennisBall : MonoBehaviour
    {
        const float _RETURN_HOMING_STRENGTH = 0.1f;
        const float _SPREAD                 = 10f;
        const float _MAX_DEVIATION          = 30f; // max angle deviation we can be from player to home in
        const int   _MAX_VOLLEYS            = 16;
        const float _MAX_SPEED_BOOST        = 50f;
        const float _MAX_DAMAGE_BOOST       = 20f;
        const float _MAX_FORCE_BOOST        = 10f;

        private Projectile          _projectile    = null;
        private PlayerController    _owner         = null;
        private int                 _volleys       = 0;
        private bool                _returning     = false;
        private bool                _missedPlayer  = false;
        private bool                _dead          = false;
        private RacketLauncher        _parentGun     = null;
        private EasyTrailBullet     _trail         = null;
        private float               _baseSpeed     = 0f;
        private float               _baseDamage    = 0f;
        private float               _baseForce     = 0f;
        private BounceProjModifier  _bounce        = null;
        private Vector2             _deathVelocity = Vector2.zero;

        private void Start()
        {
            this._projectile = base.GetComponent<Projectile>();
            if (this._projectile.Owner is not PlayerController pc)
                return;
            this._owner = pc;
            // this._projectile.DestroyMode = Projectile.ProjectileDestroyMode.BecomeDebris;
            this._projectile.DestroyMode = Projectile.ProjectileDestroyMode.DestroyComponent;

            if (pc.CurrentGun.GetComponent<RacketLauncher>() is RacketLauncher tr)
                this._parentGun = tr;
            else foreach (Gun g in pc.inventory.AllGuns)
            {
                if (g.GetComponent<RacketLauncher>() is RacketLauncher tr2)
                {
                    this._parentGun = tr2;
                    break;
                }
            }
            if (this._parentGun)
            {
                this._parentGun.AddExtantTennisBall(this);
                this._projectile.BulletScriptSettings.surviveRigidbodyCollisions = true;
                this._projectile.collidesWithProjectiles = true;
                this._projectile.collidesOnlyWithPlayerProjectiles = false;
                this._projectile.UpdateCollisionMask();
                this._projectile.specRigidbody.OnPreRigidbodyCollision += this.ReflectProjectiles;
                this._projectile.specRigidbody.OnRigidbodyCollision += (CollisionData rigidbodyCollision) => {
                    this._projectile.SendInDirection(rigidbodyCollision.Normal, false);
                    ReturnToSender();
                };
                this._projectile.OnDestruction += (Projectile p) => {
                    if (p.GetComponent<TennisBall>() is TennisBall tc)
                        this._parentGun.RemoveExtantTennisBall(tc);
                };
                AkSoundEngine.PostEvent("monkey_tennis_hit_serve", this._projectile.gameObject);
            }

            this._bounce = this._projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
                this._bounce.numberOfBounces     = 9999;
                this._bounce.chanceToDieOnBounce = 0f;
                this._bounce.onlyBounceOffTiles  = false;
                this._bounce.ExplodeOnEnemyBounce = false;
                this._bounce.OnBounce += this.ReturnToSender;

            this._trail = this._projectile.gameObject.AddComponent<EasyTrailBullet>();
                this._trail.StartWidth = 0.2f;
                this._trail.EndWidth   = 0.1f;
                this._trail.LifeTime   = 0.1f;
                this._trail.BaseColor  = ExtendedColours.lime;
                this._trail.StartColor = ExtendedColours.lime;
                this._trail.EndColor   = Color.green;

            this._baseSpeed  = this._projectile.baseData.speed;
            this._baseDamage = this._projectile.baseData.damage;
            this._baseForce  = this._projectile.baseData.force;

            // AkSoundEngine.PostEvent("racket_hit", this._projectile.gameObject);
        }

        private void ReflectProjectiles(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherCollider)
        {
            if (otherRigidbody.GetComponent<Projectile>() is not Projectile p)
                return;
            if (this._returning || p.Owner is PlayerController)
            {
                PhysicsEngine.SkipCollision = true;
                return;
            }
            PassiveReflectItem.ReflectBullet(p, true, this._owner.gameActor, 10f, 1f, 1f, 0f);
            PhysicsEngine.SkipCollision = true;
            ReturnToSender();
        }

        public Vector2 Position()
        {
            return this._projectile.sprite.WorldCenter;
        }

        public void DieInAir()
        {
            this._deathVelocity = 0.5f * this._projectile.LastVelocity;

            // Make into debris
            DebrisObject debris            = base.gameObject.GetOrAddComponent<DebrisObject>();
            debris.angularVelocity         = 45;
            debris.angularVelocityVariance = 20;
            debris.decayOnBounce           = 0.5f;
            debris.bounceCount             = 4;
            debris.canRotate               = true;
            debris.shouldUseSRBMotion      = true;
            debris.sprite                  = this._projectile.sprite;
            debris.animatePitFall          = true;
            debris.audioEventName          = "monkey_tennis_bounce_first";
            debris.AssignFinalWorldDepth(-0.5f);
            debris.Trigger(this._deathVelocity, 0.5f);

            // Stop animating
            debris.spriteAnimator.Stop();
            // Destroy unused components that may interfere with rendering
            EasyTrailBullet tr = debris.GetComponent<EasyTrailBullet>();
                tr.Disable();
                UnityEngine.GameObject.Destroy(tr);
            UnityEngine.GameObject.Destroy(debris.GetComponent<TennisBall>()); // destroy the TennisBall component

            this._dead = true;
            AkSoundEngine.PostEvent("monkey_tennis_bounce_second", this._projectile.gameObject);
            this._projectile.DieInAir(suppressInAirEffects: true);
        }

        public bool Whackable()
        {
            return this._returning;
        }

        public void GotWhacked(Vector2 direction)
        {
            if (!this._returning)
                return;

            this._volleys                    = Mathf.Min(this._volleys + 1, _MAX_VOLLEYS);
            float percentPower               = (float)this._volleys / (float)_MAX_VOLLEYS;
            this._projectile.baseData.speed  = this._baseSpeed  + _MAX_SPEED_BOOST * percentPower;
            this._projectile.baseData.damage = this._baseDamage + _MAX_DAMAGE_BOOST * percentPower;
            this._projectile.baseData.force  = this._baseForce  + _MAX_FORCE_BOOST * percentPower;
            this._trail.LifeTime             = 0.1f + (this._volleys * 0.02f);
            Color newColor                   = Lazy.Blend(ExtendedColours.lime, Color.red, 0.1f * (float)this._volleys);
            this._trail.BaseColor            = newColor;
            this._trail.StartColor           = newColor;
            this._trail.UpdateTrail();

            this._returning = false;
            this._missedPlayer = false;
            this._projectile.Speed = this._projectile.baseData.speed;
            this._projectile.SendInDirection(direction, true);
            this._projectile.UpdateSpeed();
            // AkSoundEngine.PostEvent("racket_hit", this._projectile.gameObject);
            AkSoundEngine.PostEvent("monkey_tennis_hit_return_mid", this._projectile.gameObject);
            if (this._volleys > 6)
                AkSoundEngine.PostEvent("sonic_olympic_smash", this._projectile.gameObject);
            else if (this._volleys > 3)
                AkSoundEngine.PostEvent("sonic_olympic_sidespin"/*"monkey_tennis_hit_return_mid"*/, this._projectile.gameObject);
        }

        private IEnumerator DieNextFrame()
        {
            yield return null;
            this.DieInAir();
        }

        private void ReturnToSender()
        {
            if (this._dead)
                return;
            if (this._returning)
            {
                UnityEngine.Object.Destroy(this._bounce);
                StartCoroutine(DieNextFrame()); // avoid glitch with bounce modifier messing with debris object velocity
                return;
            }
            this._returning = true;
            float dirToOwner = (this._owner.sprite.WorldCenter - this._projectile.sprite.WorldCenter).ToAngle();
            float acc = this._owner.stats.GetStatValue(PlayerStats.StatType.Accuracy);
            this._projectile.SendInDirection(dirToOwner.AddRandomSpread(_SPREAD * Mathf.Sqrt(acc)).ToVector(), true);
            AkSoundEngine.PostEvent("racket_hit", this._projectile.gameObject);
        }

        private void HomeTowardsTarget(Vector2 targetPos, Vector2 curVelocity)
        {
            Vector2 targetVelocity = (targetPos - this._projectile.sprite.WorldCenter).normalized;
            if (this._returning && (Mathf.Abs(curVelocity.ToAngle().Clamp360() - targetVelocity.ToAngle().Clamp360()) > _MAX_DEVIATION))
            {
                this._missedPlayer = true;
                return;
            }
            Vector2 newVelocty = (_RETURN_HOMING_STRENGTH * targetVelocity) + ((1 - _RETURN_HOMING_STRENGTH) * curVelocity);
            this._projectile.SendInDirection(newVelocty, false);
        }

        private void Update()
        {
            if (this._dead || this._missedPlayer)
                return;

            Vector2 curVelocity = this._projectile.LastVelocity.normalized;

            // Returning to the player
            if (this._returning)
            {
                HomeTowardsTarget(this._owner.sprite.WorldCenter, curVelocity);
                return;
            }

            // Homing in on nearest enemy
            Vector2? maybeTarget = Lazy.NearestEnemyWithinConeOfVision(
                start                            : this._projectile.transform.position,
                coneAngle                        : curVelocity.ToAngle().Clamp360(),
                maxDeviation                     : _MAX_DEVIATION,
                useNearestAngleInsteadOfDistance : true,
                ignoreWalls                      : false
                );
            if (maybeTarget is Vector2 target)
                HomeTowardsTarget(target, curVelocity);
        }
    }
}
