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

            Projectile proj1a = Lazy.PrefabProjectileFromGun(gun, setGunDefaultProjectile: false);
                proj1a.AddDefaultAnimation(proj1Sprite);
                proj1a.transform.parent = gun.barrelOffset;
                proj1a.gameObject.AddComponent<AimuHakureiProjectileBehavior>()
                    .Setup(invert: false, amplitude: 0.75f);

            Projectile proj1b = Lazy.PrefabProjectileFromGun(gun, setGunDefaultProjectile: false);
                proj1b.AddDefaultAnimation(proj1Sprite);
                proj1b.transform.parent = gun.barrelOffset;
                proj1b.gameObject.AddComponent<AimuHakureiProjectileBehavior>()
                    .Setup(invert: true, amplitude: 0.75f);

            Projectile proj1c = Lazy.PrefabProjectileFromGun(gun, setGunDefaultProjectile: false);
                proj1c.AddDefaultAnimation(proj1Sprite);
                proj1c.transform.parent = gun.barrelOffset;
                proj1c.gameObject.AddComponent<AimuHakureiProjectileBehavior>()
                    .Setup(invert: false, amplitude: 1.25f);

            Projectile proj1d = Lazy.PrefabProjectileFromGun(gun, setGunDefaultProjectile: false);
                proj1d.AddDefaultAnimation(proj1Sprite);
                proj1d.transform.parent = gun.barrelOffset;
                proj1d.gameObject.AddComponent<AimuHakureiProjectileBehavior>()
                    .Setup(invert: true, amplitude: 1.25f);

            tk2dBaseSprite basesprite = GasterBlaster._GasterBlaster.GetComponent<tk2dBaseSprite>();

            // Projectile proj1e = Lazy.PrefabProjectileFromGun(ItemHelper.Get(Items.FlashRay) as Gun, setGunDefaultProjectile: false);
            // Projectile proj1e = Lazy.PrefabProjectileFromGun(ItemHelper.Get(Items._38Special) as Gun, setGunDefaultProjectile: false);
            Projectile proj1e = Lazy.PrefabProjectileFromGun(ItemHelper.Get(Items._38Special) as Gun, setGunDefaultProjectile: false);
                proj1e.baseData.speed = 300f;
                if (proj1e.gameObject.transform.Find("Trail 1")?.gameObject is GameObject trail)
                {
                    // Dissect.DumpComponents(trail.gameObject);

                    // UnityEngine.Object.Destroy(trail.GetComponent<tk2dTiledSprite>());
                    // tk2dTiledSprite tsprite = trail.gameObject.GetOrAddComponent<tk2dTiledSprite>();
                        // tsprite.SetSprite(basesprite.collection, basesprite.spriteId);
                        // tsprite.Build();

                    // ETGModConsole.Log($"tiledsprite***");
                    // Dissect.DumpFieldsAndProperties<tk2dTiledSprite>(trail.GetComponent<tk2dTiledSprite>());
                    // ETGModConsole.Log($"trail***");
                    // Dissect.DumpFieldsAndProperties<TrailController>(trail.GetComponent<TrailController>());
                    // ETGModConsole.Log($"tk2dSpriteAnimator***");
                    // Dissect.DumpFieldsAndProperties<tk2dSpriteAnimator>(trail.GetComponent<tk2dSpriteAnimator>());

                    UnityEngine.Object.Destroy(trail.GetComponent<TrailController>());
                    UnityEngine.Object.Destroy(trail.GetComponent<tk2dTiledSprite>());
                    UnityEngine.Object.Destroy(trail.GetComponent<tk2dSpriteAnimator>());
                    UnityEngine.Object.Destroy(trail);
                }
                TrailController tc = proj1e.AddTrailToProjectile(ResMap.Get("gaster_beam_mid")[0], new Vector2(25, 39), new Vector2(0, 0),
                // TrailController tc = proj1e.AddTrailToProjectile(ResMap.Get("gaster_beam_mid")[0], new Vector2(2, 2), new Vector2(1, 1),
                // TrailController tc = trail.AddTrailToObject(ResMap.Get("gaster_beam_mid")[0], new Vector2(2, 2), new Vector2(1, 1),
                // TrailController tc = trail.AddTrailToObject(ResMap.Get("gaster_beam_mid")[0], new Vector2(25, 39), new Vector2(12, 19),
                    ResMap.Get("gaster_beam_mid"), 30, ResMap.Get("gaster_beam_start"), 30, 0.01f, destroyOnEmpty: true);
                    // tc.cascadeTimer = 0.1f;
                    // tc.usesGlobalTimer = false;
                    // tc.UsesDispersalParticles = false;


                // TrailController tc = proj1e.AddTrailToProjectile(ResMap.Get("gaster_beam_mid")[0], new Vector2(35, 39), new Vector2(1, 1),
                //     ResMap.Get("gaster_beam_mid"), 30, ResMap.Get("gaster_beam_mid"), 30);

                // tc.usesAnimation = true;
                // tc.usesStartAnimation = true;
                // tc.animation = "gaster_beam_mid";
                // tc.startAnimation = "gaster_beam_mid";
                // TrailController tc = trail.gameObject.GetComponent<TrailController>();
                //     tc.sprite.SetSprite(basesprite.collection, basesprite.spriteId);

                // UnityEngine.Object.Destroy(trail.GetComponent<tk2dSpriteAnimator>());
                // tk2dSpriteAnimator anim = trail.gameObject.GetComponent<tk2dSpriteAnimator>();
                //     anim.currentClip = proj1Sprite;
                //     anim.library = basesprite.collection.anima;
                    // anim.defaultClipId = basesprite.spriteId;
                    // anim.SetSprite(basesprite.collection, basesprite.spriteId);

                // tk2dSpriteAnimation anim = trail.gameObject.GetOrAddComponent<tk2dSpriteAnimation>();
                //     anim.clips = new[]{proj1Sprite};

            // // Projectile proj1f = Lazy.PrefabProjectileFromGun(gun, setGunDefaultProjectile: false);
            // // Projectile proj1f = Lazy.PrefabProjectileFromGun(ItemHelper.Get(Items.MarineSidearm) as Gun, false);
            // Projectile proj1f = Lazy.PrefabProjectileFromGun(gun);

            //     BasicBeamController beamComp = proj1f.SetupBeamSprites(
            //         spriteName: "gaster_beam", fps: 60, dims: new Vector2(35, 39), impactDims: new Vector2(36, 36), impactFps: 16);
            //         beamComp.boneType = BasicBeamController.BeamBoneType.Projectile;
            //         beamComp.ContinueBeamArtToWall = false;
            //         beamComp.PenetratesCover       = true;
            //         beamComp.penetration           = 1000;

            // gun.DefaultModule.projectiles[0] = proj1f;

            ProjectileModule mod1 = gun.DefaultModule;
                mod1.ammoCost            = 1;
                mod1.shootStyle          = ProjectileModule.ShootStyle.Burst;
                mod1.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Ordered;
                mod1.burstShotCount      = 5;
                mod1.burstCooldownTime   = C.FRAME;
                mod1.cooldownTime        = 0.20f;
                mod1.numberOfShotsInClip = 10 * mod1.burstShotCount;
                mod1.angleVariance       = 0f;
                mod1.angleFromAim        = 0f;
                mod1.alternateAngle      = true;
                mod1.projectiles         = new(){ proj1a, proj1b, proj1c, proj1d, proj1e, };
        }

        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
            // TrailController tc = projectile.GetComponentInChildren<TrailController>();
            // if (tc)
            // {
            //     ETGModConsole.Log($"found TrailController");
            //     Dissect.DumpFieldsAndProperties<TrailController>(tc);
            // }
            tk2dSpriteAnimator animator = projectile.GetComponentInChildren<tk2dSpriteAnimator>();
            if (animator)
            {
                ETGModConsole.Log($"found animator with fps {animator.ClipFps}");
            }
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
