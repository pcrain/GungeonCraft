using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using Mono.Cecil.Cil; //Instruction

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

        internal static tk2dSpriteAnimationClip _BulletSprite = null;
        internal static Projectile _ProjBase;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun<AimuHakurei>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.SetAttributes(quality: PickupObject.ItemQuality.B, gunClass: GunClass.FULLAUTO, reloadTime: 1.2f, ammo: 100, infiniteAmmo: true, canGainAmmo: false);
                gun.Volley.ModulesAreTiers = true;
                // gun.SetAnimationFPS(gun.shootAnimation, 30);
                // gun.SetAnimationFPS(gun.reloadAnimation, 40);
                gun.SetFireAudio("aimu_shoot_sound");
                // gun.SetReloadAudio("blowgun_reload_sound");

            _BulletSprite = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("soul_kaliber_projectile").Base(),
                2, true, new IntVector2(10, 10),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);

            _ProjBase = Lazy.PrefabProjectileFromGun(gun, setGunDefaultProjectile: false);
                _ProjBase.baseData.speed  = 44f;
                _ProjBase.baseData.damage = 7f;
                _ProjBase.baseData.range  = 100f;
                _ProjBase.baseData.force  = 3f;
                _ProjBase.transform.parent = gun.barrelOffset;

            Projectile beamProj = Lazy.PrefabProjectileFromGun(ItemHelper.Get(Items._38Special) as Gun, setGunDefaultProjectile: false);
                beamProj.baseData.speed = 300f;
                beamProj.baseData.damage = 20f;
                TrailController tc = beamProj.AddTrailToProjectile(ResMap.Get("aimu_beam_mid")[0], new Vector2(25, 39), new Vector2(0, 0),
                    ResMap.Get("aimu_beam_mid"), 60, ResMap.Get("aimu_beam_start"), 60, /*timeTillAnimStart: 0.01f,*/ cascadeTimer: C.FRAME, destroyOnEmpty: true);
                    tc.UsesDispersalParticles = true;
                    tc.DispersalParticleSystemPrefab = (ItemHelper.Get(Items.FlashRay) as Gun).DefaultModule.projectiles[0].GetComponentInChildren<TrailController>().DispersalParticleSystemPrefab;

            VFXPool impactFVX = VFX.RegisterVFXPool(ItemName+" Impact", ResMap.Get("gaster_beam_impact"), fps: 20, loops: false, scale: 1.0f, anchor: tk2dBaseSprite.Anchor.MiddleCenter);
                beamProj.SetHorizontalImpactVFX(impactFVX);
                beamProj.SetVerticalImpactVFX(impactFVX);
                beamProj.SetEnemyImpactVFX(impactFVX);
                beamProj.SetAirImpactVFX(impactFVX);

            gun.Volley.projectiles = new(){
                // Tier 1
                AimuMod(fireRate: 3, projectiles: new(){
                    AimuProj(invert: false, amplitude: 0.75f),
                    AimuProj(invert: true,  amplitude: 0.75f),
                    AimuProj(invert: false, amplitude: 2.25f),
                    AimuProj(invert: true,  amplitude: 2.25f),
                    beamProj,
                    }),
            };

            gun.gameObject.AddComponent<AimuHakureiAmmoDisplay>();
        }

        public class AimuHakureiAmmoDisplay : CustomAmmoDisplay
        {
            public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
            {
                uic.SetAmmoCountLabelColor(Color.magenta);
                uic.GunAmmoCountLabel.Text = "O: neat";
                return true;
            }
        }

        private static ProjectileModule AimuMod(List<Projectile> projectiles, float fireRate)
        {
            ProjectileModule mod = new ProjectileModule();
                mod.ammoType            = GameUIAmmoType.AmmoType.BEAM;
                mod.projectiles         = projectiles;
                mod.ammoCost            = 0;
                mod.numberOfShotsInClip = -1;
                mod.shootStyle          = ProjectileModule.ShootStyle.Burst;
                mod.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Ordered;
                mod.angleVariance       = 5f;
                mod.angleFromAim        = 0f;
                mod.burstShotCount      = mod.projectiles.Count();
                mod.burstCooldownTime   = C.FRAME * fireRate;
                mod.cooldownTime        = C.FRAME * fireRate;
            return mod;
        }

        private static Projectile AimuProj(bool invert, float amplitude)
        {
            Projectile proj = _ProjBase.ClonePrefab<Projectile>();
                proj.AddDefaultAnimation(_BulletSprite);
                proj.gameObject.AddComponent<AimuHakureiProjectileBehavior>()
                    .Setup(invert: invert, amplitude: amplitude);
                AddTrail(proj);
            return proj;
        }

        private static void AddTrail(Projectile p)
        {
            EasyTrailBullet trail = p.gameObject.AddComponent<EasyTrailBullet>();
                trail.TrailPos   = trail.transform.position;
                trail.StartWidth = 0.5f;
                trail.EndWidth   = 0.05f;
                trail.LifeTime   = 0.1f;
                trail.BaseColor  = Color.magenta;
                trail.EndColor   = Color.magenta;
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
