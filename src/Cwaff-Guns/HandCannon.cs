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
    public class HandCannon : AdvancedGunBehavior
    {
        public static string ItemName         = "Hand Cannon";
        public static string SpriteName       = "hand_cannon";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "Fire Arms";
        public static string LongDescription  = "Fires a high-powered glove that slaps enemies perpendicular to the glove's trajectory with extreme force.\n\nSecond only to guns, hands are widely considered to be one of the most effective weapons ever brought to the battlefield. In ancient times, combatants would often throw the severed hands of their fallen comrades at their enemies to simultaneously inflict physical and emotional damage, ergo the modern expression \"tossing hands\". The venerable Gun Tzu is thought to be the first to marry guns and hands with his legendary Finger Gun, known for inflicting panic and fear in all who opposed his army. The Hand Cannon is a direct descendant and natural evolution of Gun Tzu's original Finger Gun, packing enough force to make Vasilii Kamotskii blush.";

        internal static GameObject _SlapppAnimation;
        internal static tk2dSpriteAnimationClip _BulletSprite;
        internal static int                     _FireAnimationFrames = 8;

        private const float _CHARGE_TIME       = 0.5f;
        private const int   _CHARGE_LOOP_FRAME = 11;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun<HandCannon>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.SetAttributes(quality: PickupObject.ItemQuality.B, gunClass: GunClass.CHARGE, reloadTime: 0.75f, ammo: 100);
                gun.SetAnimationFPS(gun.shootAnimation, 30);
                gun.SetAnimationFPS(gun.reloadAnimation, (int)(gun.spriteAnimator.GetClipByName(gun.reloadAnimation).frames.Length / gun.reloadTime));
                gun.SetAnimationFPS(gun.chargeAnimation, (int)((1.0f / _CHARGE_TIME) * _CHARGE_LOOP_FRAME));
                gun.LoopAnimation(gun.chargeAnimation, _CHARGE_LOOP_FRAME);
                gun.SetFireAudio("hand_cannon_shoot_sound");
                gun.SetReloadAudio("hand_cannon_reload_sound");
                gun.SetChargeAudio("hand_cannon_charge_sound", frame: 0);
                gun.SetChargeAudio("hand_cannon_charge_sound", frame: 10);

            ProjectileModule mod = gun.DefaultModule;
                mod.ammoCost            = 1;
                mod.shootStyle          = ProjectileModule.ShootStyle.Charged;
                mod.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.angleVariance       = 15.0f;
                mod.cooldownTime        = 0.1f;
                mod.numberOfShotsInClip = 2;

            _BulletSprite = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("slappp").Base(),
                30, true, new IntVector2(46, 70), // 0.5x scale
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true,
                overrideColliderPixelSize: new IntVector2(8,8) // small collider near the center of the sprite
                );

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.AddDefaultAnimation(_BulletSprite);
                projectile.baseData.damage  = 40f;
                projectile.baseData.speed   = 40f;
                projectile.transform.parent = gun.barrelOffset;
                projectile.gameObject.AddComponent<SlappProjectile>();

            gun.DefaultModule.chargeProjectiles = new(){
                new ProjectileModule.ChargeProjectile
                {
                    Projectile = projectile,
                    ChargeTime = _CHARGE_TIME,
                }
            };

            _SlapppAnimation = VFX.RegisterVFXObject(
                "Slappp", ResMap.Get("slappp"), fps: 30, loops: false, anchor: tk2dBaseSprite.Anchor.MiddleCenter, scale: 0.5f);
        }
    }

    public class SlappProjectile : MonoBehaviour
    {
        private const int   _SLAPPP_FRAME         = 8;   // frame 8 is the meat of the slappp animation
        private const float _SLAPPP_FORCE         = 300f;
        private const float _SLAPPP_STUN          = 2f;
        private const float _SLAPP_RADIUS_SQUARED = 3f;

        private Projectile _projectile;
        private PlayerController _owner;
        private AIActor _slapVictim = null;
        private float _slapAngle = 0f;
        private bool _flipped = false;
        private float _slapDamage = 0f;
        private FancyVFX _vfx = null;
        private void Start()
        {
            this._projectile = base.GetComponent<Projectile>();
            this._owner = this._projectile.Owner as PlayerController;

            this._projectile.spriteAnimator.Stop(); // 0 FPS for now since we only care about the first frame
            this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;

            this._flipped = this._owner.sprite.FlipX;
            this._slapDamage = this._projectile.baseData.damage;
        }

        private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            if (otherRigidbody?.healthHaver?.gameActor is not AIActor enemy)
                return;

            if (!enemy.IsHostile())
                return;

            // Set up SLAPPP parameters
            this._slapVictim = enemy;
            this._slapAngle = (this._projectile.Direction.ToAngle() + (this._flipped ? -90f : 90f)).Clamp180();

            this._vfx = FancyVFX.SpawnUnpooled( //NOTE: absolutely MUST ignore pools or VFX objects with preexisting FancyVFX components might get reused
                prefab       : HandCannon._SlapppAnimation,
                position     : this._projectile.sprite.transform.position,
                rotation     : this._projectile.sprite.transform.rotation,
                velocity     : Vector2.zero,
                lifetime     : 0.5f,
                fadeOutTime  : 0.20f,
                parent       : enemy.sprite.transform);
            this._vfx.sprite.FlipY = this._flipped;  //smack in the opposite direction by flipping vertically, not horizontally
            if (this._vfx.GetComponent<tk2dSpriteAnimator>() is tk2dSpriteAnimator animator)
            {
                animator.AnimationEventTriggered += SlapppEvent;
                animator.DefaultClip.frames[_SLAPPP_FRAME].triggerEvent = true;
                animator.DefaultClip.frames[_SLAPPP_FRAME].eventAudio = "slappp_sound";
                animator.Play(animator.DefaultClip);
            }
            PhysicsEngine.SkipCollision = true;
            this._projectile.DieInAir(suppressInAirEffects: true, allowActorSpawns: false, allowProjectileSpawns: false, killedEarly: false);
        }

        private /*static*/ void SlapppEvent(tk2dSpriteAnimator animator, tk2dSpriteAnimationClip clip, int frame)
        {
            this._vfx.transform.parent = null;  // don't follow the enemy after we've followed through on the slap
            if (!this._slapVictim?.sprite)
                return;

            Vector2 victimPos = this._slapVictim.sprite.WorldCenter;
            foreach (AIActor enemy in StaticReferenceManager.AllEnemies)
            {
                if (!enemy.IsHostileAndNotABoss(canBeNeutral: true))
                    continue;
                if (enemy?.healthHaver is not HealthHaver hh)
                    continue;
                if ((enemy.sprite.WorldCenter - victimPos).magnitude > _SLAPP_RADIUS_SQUARED)
                    continue;
                enemy.behaviorSpeculator?.Stun(_SLAPPP_STUN);
                hh.ApplyDamage(this._slapDamage, Vector2.zero, "SLAPPP", CoreDamageTypes.None, DamageCategory.Collision, true);
                if (!hh.IsBoss && !hh.IsSubboss)
                    hh.knockbackDoer?.ApplyKnockback(this._slapAngle.ToVector(), _SLAPPP_FORCE);
            }
            UnityEngine.Object.Destroy(this);
        }
    }
}
