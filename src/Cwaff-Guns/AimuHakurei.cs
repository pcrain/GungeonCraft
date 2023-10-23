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
    public class AimuHakurei : AdvancedGunBehavior
    {
        public static string ItemName         = "Aimu Hakurei";
        public static string SpriteName       = "aimu_hakurei";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "TBD";
        public static string LongDescription  = "TBD";

        public static void Add()
        {
            Gun gun = Lazy.SetupGun<AimuHakurei>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.SetAttributes(quality: PickupObject.ItemQuality.B, gunClass: GunClass.FULLAUTO, reloadTime: 1.2f, ammo: 700);
                gun.Volley.ModulesAreTiers = true;
                // gun.SetAnimationFPS(gun.shootAnimation, 30);
                // gun.SetAnimationFPS(gun.reloadAnimation, 40);
                gun.SetFireAudio("aimu_shoot_sound");
                // gun.SetReloadAudio("blowgun_reload_sound");

            tk2dSpriteAnimationClip proj1Sprite = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("soul_kaliber_projectile").Base(),
                2, true, new IntVector2(10, 10),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);

            Projectile projBase = Lazy.PrefabProjectileFromGun(gun, setGunDefaultProjectile: false);

            Projectile proj1a = projBase.ClonePrefab<Projectile>();
                proj1a.AddDefaultAnimation(proj1Sprite);
                proj1a.transform.parent = gun.barrelOffset;
                proj1a.gameObject.AddComponent<AimuHakureiProjectileBehavior>()
                    .Setup(invert: false, amplitude: 0.75f);
                AddTrail(proj1a);

            Projectile proj1b = projBase.ClonePrefab<Projectile>();
                proj1b.AddDefaultAnimation(proj1Sprite);
                proj1b.transform.parent = gun.barrelOffset;
                proj1b.gameObject.AddComponent<AimuHakureiProjectileBehavior>()
                    .Setup(invert: true, amplitude: 0.75f);
                AddTrail(proj1b);

            Projectile proj1c = projBase.ClonePrefab<Projectile>();
                proj1c.AddDefaultAnimation(proj1Sprite);
                proj1c.transform.parent = gun.barrelOffset;
                proj1c.gameObject.AddComponent<AimuHakureiProjectileBehavior>()
                    .Setup(invert: false, amplitude: 2.25f);
                AddTrail(proj1c);

            Projectile proj1d = projBase.ClonePrefab<Projectile>();
                proj1d.AddDefaultAnimation(proj1Sprite);
                proj1d.transform.parent = gun.barrelOffset;
                proj1d.gameObject.AddComponent<AimuHakureiProjectileBehavior>()
                    .Setup(invert: true, amplitude: 2.25f);
                AddTrail(proj1d);

            Projectile proj1e = Lazy.PrefabProjectileFromGun(ItemHelper.Get(Items._38Special) as Gun, setGunDefaultProjectile: false);
                proj1e.baseData.speed = 300f;
                TrailController tc = proj1e.AddTrailToProjectile(ResMap.Get("aimu_beam_mid")[0], new Vector2(25, 39), new Vector2(0, 0),
                    ResMap.Get("aimu_beam_mid"), 60, ResMap.Get("aimu_beam_start"), 60, /*timeTillAnimStart: 0.01f,*/ cascadeTimer: C.FRAME, destroyOnEmpty: true);
                    tc.UsesDispersalParticles = true;
                    tc.DispersalParticleSystemPrefab = (ItemHelper.Get(Items.FlashRay) as Gun).DefaultModule.projectiles[0].GetComponentInChildren<TrailController>().DispersalParticleSystemPrefab;

            ProjectileModule mod1 = gun.DefaultModule;
                mod1.ammoCost            = 1;
                mod1.shootStyle          = ProjectileModule.ShootStyle.Burst;
                mod1.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Ordered;
                mod1.burstShotCount      = 5;
                mod1.burstCooldownTime   = C.FRAME * 3;
                mod1.cooldownTime        = C.FRAME * 3;
                mod1.numberOfShotsInClip = 10 * mod1.burstShotCount;
                mod1.angleVariance       = 5f;
                mod1.angleFromAim        = 0f;
                // mod1.alternateAngle      = true;
                mod1.projectiles         = new(){ proj1a, proj1b, proj1c, proj1d, proj1e, };

            VFXPool impactFVX = VFX.RegisterVFXPool(ItemName+" Impact", ResMap.Get("gaster_beam_impact"), fps: 20, loops: false, scale: 1.0f, anchor: tk2dBaseSprite.Anchor.MiddleCenter);
                proj1e.SetHorizontalImpactVFX(impactFVX);
                proj1e.SetVerticalImpactVFX(impactFVX);
                proj1e.SetEnemyImpactVFX(impactFVX);
                proj1e.SetAirImpactVFX(impactFVX);
        }

        private static void AddTrail(Projectile p)
        {
            EasyTrailBullet trail = p.gameObject.AddComponent<EasyTrailBullet>();
                trail.TrailPos   = trail.transform.position;
                trail.StartWidth = 0.2f;
                trail.EndWidth   = 0.05f;
                trail.LifeTime   = 0.1f;
                trail.BaseColor  = Color.magenta;
                trail.EndColor   = Color.magenta;
        }

        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
            // TrailController tc = projectile.GetComponentInChildren<TrailController>();
            // if (tc)
            // {
            //     ETGModConsole.Log($"found TrailController with dispersal {tc.UsesDispersalParticles}");
            //     Dissect.DumpFieldsAndProperties<TrailController>(tc);
            // }

            // tk2dSpriteAnimator animator = projectile.GetComponentInChildren<tk2dSpriteAnimator>();
            // if (animator)
            //     ETGModConsole.Log($"found animator with fps {animator.ClipFps}");

            // if (projectile.GetComponentInChildren<TrailController>())
            //     Dissect.CompareFieldsAndProperties<TrailController>(
            //         projectile.GetComponentInChildren<TrailController>(),
            //         (ItemHelper.Get(Items.FlashRay) as Gun).DefaultModule.projectiles[0].GetComponentInChildren<TrailController>());
        }
    }


    public class AimuHakureiProjectileBehavior : MonoBehaviour
    {
        private Projectile _projectile;
        private PlayerController _owner;
        private AimuHakureiProjectileMotionModule _aimu;

        // must be public or it won't serialize in prefab
        public bool invert;
        public float amplitude;

        public void Setup(bool invert, float amplitude)
        {
            this.invert = invert;
            this.amplitude = amplitude;
        }

        private void Start()
        {
            this._projectile = base.GetComponent<Projectile>();
            this._owner = this._projectile.Owner as PlayerController;
            this._aimu = new AimuHakureiProjectileMotionModule();
                this._aimu.ForceInvert = this.invert;
                this._aimu.amplitude = this.amplitude;
            this._projectile.OverrideMotionModule = this._aimu;
        }
    }

    public class AimuHakureiProjectileMotionModule : ProjectileMotionModule
    {
        public float wavelength = 8f;
        public float amplitude = 0f;
        public bool ForceInvert;

        private bool _initialized;
        private Vector2 _initialRightVector;
        private Vector2 _initialUpVector;
        private Vector2 _privateLastPosition;
        private float _xDisplacement;
        private float _yDisplacement;

        public override void UpdateDataOnBounce(float angleDiff)
        {
            if (!float.IsNaN(angleDiff))
            {
                _initialUpVector = Quaternion.Euler(0f, 0f, angleDiff) * _initialUpVector;
                _initialRightVector = Quaternion.Euler(0f, 0f, angleDiff) * _initialRightVector;
            }
        }

        public override void AdjustRightVector(float angleDiff)
        {
            if (!float.IsNaN(angleDiff))
            {
                _initialUpVector = Quaternion.Euler(0f, 0f, angleDiff) * _initialUpVector;
                _initialRightVector = Quaternion.Euler(0f, 0f, angleDiff) * _initialRightVector;
            }
        }

        private void Initialize(Vector2 lastPosition, Transform projectileTransform, float m_timeElapsed, Vector2 m_currentDirection, bool shouldRotate)
        {
            _privateLastPosition = lastPosition;
            _initialRightVector = ((!shouldRotate) ? m_currentDirection : projectileTransform.right.XY());
            _initialUpVector    = ((!shouldRotate) ? (Quaternion.Euler(0f, 0f, 90f) * m_currentDirection) : projectileTransform.up);
            _initialized   = true;
            _xDisplacement       = 0f;
            _yDisplacement      = 0f;
            m_timeElapsed        = 0f;
        }

        public override void Move(Projectile source, Transform projectileTransform, tk2dBaseSprite projectileSprite, SpeculativeRigidbody specRigidbody, ref float m_timeElapsed, ref Vector2 m_currentDirection, bool Inverted, bool shouldRotate)
        {
            ProjectileData baseData = source.baseData;
            Vector2 oldPos = ((!projectileSprite) ? projectileTransform.position.XY() : projectileSprite.WorldCenter);
            if (!_initialized)
                Initialize(oldPos, projectileTransform, m_timeElapsed, m_currentDirection, shouldRotate);
            m_timeElapsed   += BraveTime.DeltaTime;
            int invertSign           = ((!(Inverted ^ ForceInvert)) ? 1 : (-1));
            float phaseAngle         = (float)Math.PI * baseData.speed / wavelength;
            float newDisplacementX   = m_timeElapsed * baseData.speed;
            float newDisplacementY   = (float)invertSign * amplitude * Mathf.Sin(m_timeElapsed * phaseAngle);
            float deltaDisplacementX = newDisplacementX - _xDisplacement;
            float deltaDisplacementY = newDisplacementY - _yDisplacement;
            Vector2 newPos           = (_privateLastPosition = _privateLastPosition + _initialRightVector * deltaDisplacementX + _initialUpVector * deltaDisplacementY);
            if (shouldRotate)
            {
                float futureDisplacementY = (float)invertSign * amplitude * Mathf.Sin((m_timeElapsed + 0.01f) * phaseAngle);
                float angleFromStart = BraveMathCollege.Atan2Degrees(futureDisplacementY - newDisplacementY, 0.01f * baseData.speed);
                projectileTransform.localRotation = Quaternion.Euler(0f, 0f, angleFromStart + _initialRightVector.ToAngle());
            }
            Vector2 velocity = (newPos - oldPos) / BraveTime.DeltaTime;
            if (!float.IsNaN(BraveMathCollege.Atan2Degrees(velocity)))
                m_currentDirection = velocity.normalized;
            _xDisplacement        = newDisplacementX;
            _yDisplacement        = newDisplacementY;
            specRigidbody.Velocity = velocity;
        }

        public override void SentInDirection(ProjectileData baseData, Transform projectileTransform, tk2dBaseSprite projectileSprite, SpeculativeRigidbody specRigidbody, ref float m_timeElapsed, ref Vector2 m_currentDirection, bool shouldRotate, Vector2 dirVec, bool resetDistance, bool updateRotation)
        {
            Initialize(((!projectileSprite) ? projectileTransform.position.XY() : projectileSprite.WorldCenter), projectileTransform, m_timeElapsed, m_currentDirection, shouldRotate);
        }
    }
}
