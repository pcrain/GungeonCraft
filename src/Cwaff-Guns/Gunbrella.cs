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
    public class Gunbrella : AdvancedGunBehavior
    {
        public static string ItemName         = "Gunbrella";
        public static string SpriteName       = "gunbrella";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "Cloudy with a Chance of Pain";
        public static string LongDescription  = "(Charging and releasing fires projectiles that rain from the sky)";

        private const float _RETICLE_ACCEL     = 24.0f;
        private const float _RETICLE_MAX_SPEED = 24.0f;
        private const float _FADE_IN_SPEED     = 5.0f;
        private const float _MIN_CHARGE_TIME   = 0.25f;
        private const int   _BARRAGE_SIZE      = 16;
        private const float _BARRAGE_DELAY     = 0.04f;
        private const float _PROJ_DAMAGE       = 16f;

        internal static tk2dSpriteAnimationClip _BulletSprite;

        private GameObject _targetingReticle              = null;
        private float _curChargeTime                      = 0.0f;
        private Vector2 _chargeStartPos                   = Vector2.zero;
        private float _chargeStartAngle                   = 0.0f;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.gunClass                             = GunClass.CHARGE;
                gun.gunSwitchGroup                       = (ItemHelper.Get(Items.MarineSidearm) as Gun).gunSwitchGroup;
                gun.DefaultModule.shootStyle             = ProjectileModule.ShootStyle.Charged;
                gun.DefaultModule.sequenceStyle          = ProjectileModule.ProjectileSequenceStyle.Random;
                gun.DefaultModule.numberOfShotsInClip    = 1;
                gun.quality                              = PickupObject.ItemQuality.A;
                gun.InfiniteAmmo                         = false;
                gun.barrelOffset.transform.localPosition = new Vector3(1.6875f, 0.6875f, 0f); // should match "Casing" in JSON file
                gun.SetAnimationFPS(gun.chargeAnimation, 16);
                gun.SetBaseMaxAmmo(100);
                gun.CurrentAmmo = 100;

            _BulletSprite = AnimateBullet.CreateProjectileAnimation(
                new List<string> {
                    "gunbrella-projectile1",
                    "gunbrella-projectile2",
                    "gunbrella-projectile3",
                }, 16, true, new IntVector2(9, 9),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);


            for (int i = 0; i < _BARRAGE_SIZE; i++)
            {
                // use ak47 so our sprite doesn't rotate and mess up our transform calculations when launching / falling
                gun.AddProjectileModuleFrom(ItemHelper.Get(Items.Ak47) as Gun, true, false);
            }


            int index = 0;
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                Projectile projectile = Lazy.PrefabProjectileFromGun(gun, setGunDefaultProjectile: false);
                    projectile.AddAnimation(_BulletSprite);
                    projectile.SetAnimation(_BulletSprite);
                    projectile.baseData.damage = _PROJ_DAMAGE;
                GunbrellaProjectile gp = projectile.gameObject.AddComponent<GunbrellaProjectile>();
                    gp.extraDelay = _BARRAGE_DELAY * (index++);

                mod.angleVariance = 10f;
                mod.ammoCost = (mod != gun.DefaultModule) ? 0 : 1;
                mod.shootStyle = ProjectileModule.ShootStyle.Charged;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.chargeProjectiles = new(){ new(){
                    Projectile = projectile,
                    ChargeTime = _MIN_CHARGE_TIME,
                }};
            }

            var comp = gun.gameObject.AddComponent<Gunbrella>();
                comp.SetFireAudio(); // prevent fire audio, as it's handled in Update()
        }

        protected override void Update()
        {
            base.Update();
            if (BraveTime.DeltaTime == 0.0f)
                return;

            if (!this.gun.IsCharging)
            {
                EndCharge();
                return;
            }

            if (this._curChargeTime == 0.0f)
                BeginCharge();
            UpdateCharge();
            this._curChargeTime += BraveTime.DeltaTime;
        }

        // Using LateUpdate() here so alpha is updated correctly
        private void LateUpdate()
        {
            this._targetingReticle?.SetAlpha(Mathf.Min(1.0f, _FADE_IN_SPEED * this._curChargeTime));
        }

        private void BeginCharge()
        {
            this._chargeStartPos   = this.gun.barrelOffset.PositionVector2();
            this._chargeStartAngle = this.gun.CurrentAngle;
            if (this._targetingReticle)
                return;

            this._targetingReticle = Instantiate<GameObject>(VFX.animations["RainReticle"], base.transform.position, Quaternion.identity);
            this._targetingReticle.SetAlphaImmediate(0.0f); // avoid bug where setting alpha on newly created object is delayed by one frame
        }

        private void UpdateCharge()
        {
            if (this._curChargeTime == 0.0f)
                BeginCharge();

            float velocity = Mathf.Min(_RETICLE_MAX_SPEED, _RETICLE_ACCEL * this._curChargeTime);
            this._chargeStartPos += this._chargeStartAngle.ToVector(velocity * BraveTime.DeltaTime);
            this._targetingReticle.transform.position = this._chargeStartPos;
        }

        private void EndCharge()
        {
            this._curChargeTime = 0.0f;
            if (!this._targetingReticle)
                return;
            UnityEngine.Object.Destroy(this._targetingReticle);
            this._targetingReticle = null;
        }

        public Vector2 GetReticleCenter()
        {
            return this._chargeStartPos;
        }
    }


    public class GunbrellaProjectile : MonoBehaviour
    {
        private const float _SPREAD               = 1.5f;   // max distance from the target an individual projectile can land
        private const float _LAUNCH_SPEED         = 80.0f;  // speed at which projectiles rise / fall
        private const float _LAUNCH_TIME          = 0.35f;  // time spent rising
        private const float _HANG_TIME            = 0.15f;  // time spent between rising and falling
        private const float _FALL_TIME            = 0.2f;   // time spent falling
        private const float _TIME_TO_REACH_TARGET = _LAUNCH_TIME + _HANG_TIME + _FALL_TIME;

        private Projectile _projectile   = null;
        private PlayerController _owner  = null;
        private float _lifetime          = 0.0f;
        private bool _intangible         = true;
        private Vector2 _exactTarget     = Vector2.zero;

        private bool _launching          = false;
        private bool _falling            = false;

        public float extraDelay          = 0.0f; // must be public so unity serializes it properly with the prefab

        private void Start()
        {
            this._projectile = base.GetComponent<Projectile>();
            this._owner = this._projectile.Owner as PlayerController;

            if (this._owner.CurrentGun.GetComponent<Gunbrella>() is not Gunbrella gun)
            {
                ETGModConsole.Log($"shooting a gunbrella projectile without a reticle, uh-oh o.o");
                return;
            }

            this._exactTarget = gun.GetReticleCenter();

            this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
            this._projectile.specRigidbody.OnPreTileCollision += this.OnPreTileCollision;
            StartCoroutine(TakeToTheSkies());
        }

        private void OnPreCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, SpeculativeRigidbody other, PixelCollider otherPixelCollider)
        {
            if (this._intangible)
                PhysicsEngine.SkipCollision = true;
        }

        private void OnPreTileCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, PhysicsEngine.Tile other, PixelCollider otherPixelCollider)
        {
            if (this._intangible)
                PhysicsEngine.SkipCollision = true;
        }

        private IEnumerator TakeToTheSkies()
        {
            // Phase 1 / 4 -- become intangible and launch to the skies
            this._projectile.IgnoreTileCollisionsFor(_TIME_TO_REACH_TARGET);
            this._projectile.collidesWithEnemies = false;
            this._projectile.baseData.speed = _LAUNCH_SPEED;
            this._projectile.baseData.range = float.MaxValue;
            this._projectile.SendInDirection((85f + 10f*UnityEngine.Random.value).ToVector(), true);
            this._projectile.UpdateSpeed();
            this._launching = true;
            while (this._lifetime < _LAUNCH_TIME)
            {
                yield return null;
                this._lifetime += BraveTime.DeltaTime;
            }
            this._lifetime -= _LAUNCH_TIME;

            // Phase 2 / 4 -- slight delay
            this._launching = false;
            this._projectile.baseData.speed = 0.01f;
            while (this._lifetime < (_HANG_TIME + this.extraDelay))
            {
                yield return null;
                this._lifetime += BraveTime.DeltaTime;
            }
            this._lifetime -= (_HANG_TIME + this.extraDelay);

            // Phase 3 / 4 -- fall from the skies
            this._falling = true;
            this._projectile.baseData.speed = _LAUNCH_SPEED;
            this._projectile.SendInDirection(Vector2.down, true);
            this._projectile.UpdateSpeed();
            this._projectile.specRigidbody.Position = new Position(this._exactTarget + (_FALL_TIME * _LAUNCH_SPEED) * Vector2.up + Lazy.RandomVector(_SPREAD * UnityEngine.Random.value));
            this._projectile.specRigidbody.UpdateColliderPositions();
            while (this._lifetime + BraveTime.DeltaTime < _FALL_TIME) // stop a frame early so we can collide with enemies on our last frame
            {
                this._lifetime += BraveTime.DeltaTime;
                yield return null;
            }
            this._lifetime -= _FALL_TIME;

            // Phase 4 / 4 -- become tangible, wait a frame to collide with enemies, then die
            this._projectile.collidesWithEnemies = true;
            this._intangible = false;
            yield return null;

            this._projectile.DieInAir();
        }
    }
}
