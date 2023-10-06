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
    public class TennisRocket : AdvancedGunBehavior
    {
        public static string ItemName         = "Tennis Rocket";
        public static string SpriteName       = "paddle";
        public static string ProjectileName   = "86"; //marine sidearm
        public static string ShortDescription = "TBD";
        public static string LongDescription  = "TBD";

        internal const float _MAX_REFLECT_DISTANCE = 5f;
        internal const int   _IDLE_FPS             = 24;

        internal static tk2dSpriteAnimationClip _BulletSprite;

        private List<TennisBall> _extantTennisBalls = new();

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
            var comp = gun.gameObject.AddComponent<TennisRocket>();
                comp.SetFireAudio();

            gun.DefaultModule.ammoCost               = 0;
            gun.DefaultModule.shootStyle             = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle          = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                           = 0;
            gun.DefaultModule.cooldownTime           = 0.3f;
            gun.DefaultModule.cooldownTime           = 0.1f;
            gun.muzzleFlashEffects.type              = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip    = 100;
            gun.barrelOffset.transform.localPosition = new Vector3(1.0f, 1.875f, 0);
            gun.quality                              = PickupObject.ItemQuality.D;
            gun.gunClass                             = GunClass.SILLY;
            gun.gunSwitchGroup                       = (ItemHelper.Get(Items.Blasphemy) as Gun).gunSwitchGroup;
            gun.CanReloadNoMatterAmmo                = true;
            gun.CurrentAmmo                          = 100;
            gun.SetBaseMaxAmmo(100);
            gun.SetAnimationFPS(gun.shootAnimation, 60);
            gun.SetAnimationFPS(gun.idleAnimation, _IDLE_FPS);
            gun.LoopAnimation(gun.idleAnimation, 0);

            _BulletSprite = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("tennis_ball").Base(),
                12, true, new IntVector2(9, 9),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);

            Projectile projectile              = Lazy.PrefabProjectileFromGun(gun);
                projectile.AddAnimation(_BulletSprite);
                projectile.SetAnimation(_BulletSprite);
                projectile.baseData.damage         = 9f;
                projectile.baseData.speed          = 30f;
                projectile.baseData.range          = 300f;
                projectile.BulletScriptSettings.surviveRigidbodyCollisions = true;

            projectile.gameObject.AddComponent<TennisBall>();

            foreach (ProjectileModule mod in gun.Volley.projectiles)
                mod.ammoCost = 0;
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
                gun.LoseAmmo(1);
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
            gun.ClipShotsRemaining = gun.CurrentAmmo;
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

        public override void OnReloadPressedSafe(PlayerController player, Gun gun, bool manualReload)
        {
            if (!manualReload)
                return;
            base.OnReloadPressedSafe(player, gun, manualReload);
            for (int i = this._extantTennisBalls.Count - 1; i >= 0; --i)
                this._extantTennisBalls[i]?.DieInAir();
            this._extantTennisBalls.Clear();
        }

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
        const float _SPREAD = 10f;
        const float _MAX_DEVIATION = 30f; // max angle deviation we can be from player to home in
        const int   _MAX_VOLLEYS = 10;

        private Projectile       _projectile;
        private PlayerController _owner;
        private int              _volleys = 0;
        private bool             _returning = false;
        private bool             _missedPlayer = false;
        private TennisRocket     _parentGun = null;
        private EasyTrailBullet  _trail = null;
        private float            _baseSpeed  = 0f;
        private float            _baseDamage = 0f;
        private float            _baseForce  = 0f;

        private void Start()
        {
            this._projectile = base.GetComponent<Projectile>();
            if (this._projectile.Owner is not PlayerController pc)
                return;
            this._owner = pc;

            if (pc.CurrentGun.GetComponent<TennisRocket>() is TennisRocket tr)
                this._parentGun = tr;
            else foreach (Gun g in pc.inventory.AllGuns)
            {
                if (g.GetComponent<TennisRocket>() is TennisRocket tr2)
                {
                    this._parentGun = tr2;
                    break;
                }
            }
            if (this._parentGun)
            {
                this._parentGun.AddExtantTennisBall(this);
                this._projectile.BulletScriptSettings.surviveRigidbodyCollisions = true;
                this._projectile.specRigidbody.OnRigidbodyCollision += (CollisionData rigidbodyCollision) => {
                    this._projectile.SendInDirection(rigidbodyCollision.Normal, false);
                    ReturnToSender();
                };
                this._projectile.OnDestruction += (Projectile p) => {
                    if (p.GetComponent<TennisBall>() is TennisBall tc)
                        this._parentGun.RemoveExtantTennisBall(tc);
                };
                AkSoundEngine.PostEvent("racket_hit", this._projectile.gameObject);
            }

            BounceProjModifier bounce = this._projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
                bounce.numberOfBounces     = 9999;
                bounce.chanceToDieOnBounce = 0f;
                bounce.onlyBounceOffTiles  = false;
                bounce.ExplodeOnEnemyBounce = true;
                bounce.OnBounce += this.ReturnToSender;

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

        public Vector2 Position()
        {
            return this._projectile.sprite.WorldCenter;
        }

        public void DieInAir()
        {
            this._projectile.DieInAir();
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
            this._projectile.baseData.speed  = this._baseSpeed  + 40f * percentPower;
            this._projectile.baseData.damage = this._baseDamage + 20f * percentPower;
            this._projectile.baseData.force  = this._baseForce  + 10f * percentPower;
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
            AkSoundEngine.PostEvent("racket_hit", this._projectile.gameObject);
        }

        private void ReturnToSender()
        {
            if (this._returning)
            {
                this._projectile.DieInAir();
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
            if (this._missedPlayer)
                return;

            Vector2 ppos = this._projectile.sprite.WorldCenter;
            Vector2 curVelocity = this._projectile.LastVelocity.normalized;

            // Returning to the player
            if (this._returning)
            {
                HomeTowardsTarget(this._owner.sprite.WorldCenter, curVelocity);
                return;
            }

            // Homing in on nearest enemy
            Vector2? maybeTarget = Lazy.NearestEnemyWithinConeOfVision(
                start: this._projectile.transform.position,
                coneAngle: curVelocity.ToAngle().Clamp360(),
                maxDeviation: _MAX_DEVIATION,
                useNearestAngleInsteadOfDistance: true,
                ignoreWalls: false
                );
            if (maybeTarget is Vector2 target)
                HomeTowardsTarget(target, curVelocity);
        }
    }
}
