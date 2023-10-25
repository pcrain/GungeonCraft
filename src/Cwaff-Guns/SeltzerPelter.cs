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
    public class SeltzerPelter : AdvancedGunBehavior
    {
        public static string ItemName         = "Seltzer Pelter";
        public static string SpriteName       = "seltzer_pelter";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "TBD";
        public static string LongDescription  = "TBD";

        internal static tk2dSpriteAnimationClip _BulletSprite;
        internal static BasicBeamController _BubbleBeam;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun<SeltzerPelter>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.SetAttributes(quality: PickupObject.ItemQuality.B, gunClass: GunClass.CHARGE, reloadTime: 1.0f, ammo: 800);
                gun.SetAnimationFPS(gun.reloadAnimation, 45);
                gun.SetReloadAudio("seltzer_shake_sound", frame: 0);
                gun.SetReloadAudio("seltzer_shake_sound", frame: 10);
                gun.SetReloadAudio("seltzer_shake_sound", frame: 22);
                gun.SetReloadAudio("seltzer_shake_sound", frame: 29);
                gun.SetReloadAudio("seltzer_shake_sound", frame: 35);
                gun.SetReloadAudio("seltzer_shake_sound", frame: 42);

            ProjectileModule mod = gun.DefaultModule;
                mod.ammoCost            = 1;
                mod.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime        = 0.75f;
                mod.numberOfShotsInClip = 4;

            _BulletSprite = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("can_projectile_a").Base(),
                12, true, new IntVector2(16, 12),
                false, tk2dBaseSprite.Anchor.MiddleCenter,
                anchorsChangeColliders: false/*true*/,
                fixesScales: true,
                overrideColliderPixelSize: new IntVector2(2, 2) // prevent uneven colliders from glitching into walls
                );

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.AddDefaultAnimation(_BulletSprite);
                projectile.transform.parent = gun.barrelOffset;
                projectile.baseData.range = 999f;
                projectile.gameObject.AddComponent<SeltzerProjectile>();

            Projectile beamProjectile = Lazy.PrefabProjectileFromGun(ItemHelper.Get(Items.MarineSidearm) as Gun, false);
                beamProjectile.baseData.range  = 3f;   // the perfect seltzer stats, do not tweak without testing!
                beamProjectile.baseData.speed  = 20f;  // the perfect seltzer stats, do not tweak without testing!
                beamProjectile.baseData.force  = 100f;
                beamProjectile.baseData.damage = 20f;

            _BubbleBeam = beamProjectile.SetupBeamSprites(spriteName: "bubble_beam", fps: 8, dims: new Vector2(16, 8));
                _BubbleBeam.sprite.usesOverrideMaterial = true;
                _BubbleBeam.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTiltedCutoutEmissive");
                _BubbleBeam.sprite.renderer.material.SetFloat("_EmissivePower", 5f);

                _BubbleBeam.chargeDelay         = 0f;
                _BubbleBeam.usesChargeDelay     = false;
                _BubbleBeam.HitsPlayers         = false;
                _BubbleBeam.HitsEnemies         = true;
                _BubbleBeam.collisionSeparation = true;
                _BubbleBeam.knockbackStrength   = 100f;
                _BubbleBeam.boneType            = BasicBeamController.BeamBoneType.Projectile;
                _BubbleBeam.TileType            = BasicBeamController.BeamTileType.Flowing;
                _BubbleBeam.endType             = BasicBeamController.BeamEndType.Persist;
                _BubbleBeam.interpolateStretchedBones = false; // causes weird graphical glitches whether it's enabled or not, but enabled is worse

            GoopModifier gmod = _BubbleBeam.gameObject.AddComponent<GoopModifier>();
                gmod.SpawnAtBeamEnd         = true;
                gmod.BeamEndRadius          = 0.5f;
                gmod.SpawnGoopInFlight      = true;
                gmod.InFlightSpawnRadius    = 0.5f;
                gmod.InFlightSpawnFrequency = 0.01f;
                gmod.goopDefinition         = EasyGoopDefinitions.SeltzerGoop;
        }
    }

    public class SeltzerProjectile : MonoBehaviour
    {
        private Projectile _canProjectile;
        private PlayerController _owner;
        private BounceProjModifier  _bounce        = null;
        private BasicBeamController _beam = null;
        private float _rotationRate = 0f;
        private bool _startedSpraying = false;

        private void Start()
        {
            this._canProjectile = base.GetComponent<Projectile>();
            this._owner = this._canProjectile.Owner as PlayerController;
            this._canProjectile.BulletScriptSettings.surviveRigidbodyCollisions = true;
            this._canProjectile.DestroyMode = Projectile.ProjectileDestroyMode.BecomeDebris;
            this._canProjectile.shouldRotate = false; // prevent automatic rotation after creation
            this._canProjectile.specRigidbody.OnRigidbodyCollision += OnRigidbodyCollision;
            this._bounce = this._canProjectile.gameObject.GetOrAddComponent<BounceProjModifier>();
                this._bounce.numberOfBounces      = 9999;
                this._bounce.chanceToDieOnBounce  = 0f;
                this._bounce.onlyBounceOffTiles   = false;
                this._bounce.ExplodeOnEnemyBounce = false;
                this._bounce.bouncesTrackEnemies  = true;
                this._bounce.bounceTrackRadius    = 3f;
                this._bounce.OnBounce += this.StartSprayingSoda;

            AkSoundEngine.PostEvent("seltzer_shoot_sound_alt", base.gameObject);
        }

        private void OnRigidbodyCollision(CollisionData rigidbodyCollision)
        {
            if (!this?._canProjectile)
                return;
            this._canProjectile.SendInDirection(rigidbodyCollision.Normal, false);

            if (!this._startedSpraying)
            {
                StartSprayingSoda();
                return;
            }

            this._canProjectile.baseData.speed *= 0.5f;
            this._canProjectile.UpdateSpeed();
        }

        private void StartSprayingSoda()
        {
            this._startedSpraying = true;
            this._bounce.OnBounce -= this.StartSprayingSoda;
            this._bounce.OnBounce += this.RestartBeamOnBounce;
            this._canProjectile.baseData.speed *= 0.5f;
            this._canProjectile.UpdateSpeed();
            this._canProjectile.OnDestruction += this.DestroyBeam;
            this._canProjectile.StartCoroutine(SpraySoda_CR(this, this._canProjectile));

            AkSoundEngine.PostEvent("seltzer_shoot_sound", base.gameObject);
        }

        private void CreateBeam()
        {
            GameObject theBeamObject = SpawnManager.SpawnProjectile(SeltzerPelter._BubbleBeam.gameObject, this._canProjectile.sprite.WorldCenter, Quaternion.identity);
            Projectile beamProjectile = theBeamObject.GetComponent<Projectile>();
                beamProjectile.Owner = this._owner;

            this._beam = theBeamObject.GetComponent<BasicBeamController>();
                this._beam.Owner       = this._owner;
                this._beam.HitsPlayers = false;
                this._beam.HitsEnemies = true;
                this._beam.Origin      = this._canProjectile.sprite.WorldCenter;
                this._beam.Direction   = -this._canProjectile.sprite.transform.rotation.z.ToVector();
        }

        private void RestartBeamOnBounce()
        {
            if (this?._canProjectile)
                this._canProjectile.baseData.speed *= 0.5f;
            this._beam?.CeaseAttack();
            this._beam = null;
        }

        private void UpdateRotationRate()
        {
            this._rotationRate = UnityEngine.Random.Range(-5f, 5f);
        }

        private void DestroyBeam(Projectile p)
        {
            this._beam?.CeaseAttack();
        }

        private const float SPRAY_TIME = 2f;
        private const float SPIN_TIME  = 4f;
        private const float ACCEL      = 40f;
        private const float _AIR_DRAG  = 0.20f;
        private const float _SOUND_RATE = 0.2f;

        private static IEnumerator SpraySoda_CR(SeltzerProjectile seltzer, Projectile p)
        {
            yield return null;
            float startAngle = p.LastVelocity.ToAngle();
            float curAngle = startAngle;
            seltzer.UpdateRotationRate();

            AkSoundEngine.PostEvent("seltzer_spray_sound", p.gameObject);
            float lastSoundTime = BraveTime.ScaledTimeSinceStartup;

            #region The Ballistics
                for (float elapsed = 0f; elapsed < SPRAY_TIME; elapsed += BraveTime.DeltaTime)
                {
                    while (BraveTime.DeltaTime == 0)
                        yield return null;
                    if (!p.isActiveAndEnabled || p.HasDiedInAir)
                        break;
                    if (!seltzer._beam)
                        seltzer.CreateBeam();

                    if (lastSoundTime + _SOUND_RATE < BraveTime.ScaledTimeSinceStartup)
                    {
                        lastSoundTime = BraveTime.ScaledTimeSinceStartup;
                        AkSoundEngine.PostEvent("seltzer_spray_sound", p.gameObject);
                    }

                    Vector2 oldSpeed = p.LastVelocity;
                    curAngle += seltzer._rotationRate;
                    Vector2 newSpeed = oldSpeed + curAngle.ToVector(ACCEL * BraveTime.DeltaTime);
                    p.baseData.speed = newSpeed.magnitude;
                    p.SendInDirection(newSpeed, false, false);
                    p.UpdateSpeed();
                    p.SetRotation(newSpeed.ToAngle());
                    seltzer._beam.Origin = p.sprite.WorldCenter;
                    seltzer._beam.Direction = -p.LastVelocity;
                    seltzer._beam.LateUpdatePosition(seltzer._beam.Origin);
                    yield return null;
                }
            #endregion

            #region The Rapid Spin
                curAngle = p.LastVelocity.ToAngle(); // reset this to match the actual sprite
                float rotIncrease = 5f * Mathf.Sign(seltzer._rotationRate);
                for (float elapsed = 0f; elapsed < SPIN_TIME; elapsed += BraveTime.DeltaTime)
                {
                    while (BraveTime.DeltaTime == 0)
                        yield return null;
                    if (!p.isActiveAndEnabled || p.HasDiedInAir)
                        break;
                    if (!seltzer._beam)
                        seltzer.CreateBeam();

                    if (lastSoundTime + _SOUND_RATE < BraveTime.ScaledTimeSinceStartup)
                    {
                        lastSoundTime = BraveTime.ScaledTimeSinceStartup;
                        AkSoundEngine.PostEvent("seltzer_spray_sound", p.gameObject);
                    }

                    if (p.baseData.speed > 0.1f)
                    {
                        p.baseData.speed *= Mathf.Pow(_AIR_DRAG, BraveTime.DeltaTime);
                        p.UpdateSpeed();
                    }
                    seltzer._rotationRate += rotIncrease * BraveTime.DeltaTime;
                    curAngle += seltzer._rotationRate * C.FPS * BraveTime.DeltaTime;
                    p.SetRotation(curAngle);
                    seltzer._beam.Origin = p.sprite.WorldCenter;
                    seltzer._beam.Direction = -curAngle.ToVector();
                    seltzer._beam.LateUpdatePosition(seltzer._beam.Origin);
                    yield return null;
                }
            #endregion

            #region Die Down
                seltzer._beam.CeaseAttack();
                p?.DieInAir();
            #endregion

            yield break;
        }

        private void Update()
        {

        }
    }
}
