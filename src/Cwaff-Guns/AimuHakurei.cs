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

        internal const float _GRAZE_THRES              = 1.6f;
        internal const float _GRAZE_THRES_SQUARED      = _GRAZE_THRES * _GRAZE_THRES;
        internal const float _GRAZE_DECAY_RATE         = 0.75f;
        internal const int   _GRAZE_MAX                = 120;
        internal const int   _MAX_GRAZE_PER_PROJECTILE = 5;

        internal static readonly int[] _GRAZE_TIER_THRESHOLDS  = {10, 30, 60, 100};

        internal static tk2dSpriteAnimationClip _BulletSprite = null;
        internal static Projectile _ProjBase;

        public int graze = 0;

        private float _lastDecayTime = 0f;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun<AimuHakurei>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.SetAttributes(quality: PickupObject.ItemQuality.B, gunClass: GunClass.FULLAUTO, reloadTime: 1.2f, ammo: 100, infiniteAmmo: true, canGainAmmo: false);
                gun.Volley.ModulesAreTiers = true;
                // gun.SetAnimationFPS(gun.shootAnimation, 30);
                // gun.SetAnimationFPS(gun.reloadAnimation, 40);
                // gun.SetFireAudio("aimu_shoot_sound");
                // gun.SetReloadAudio("blowgun_reload_sound");

            _BulletSprite = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("aimu_projectile").Base(),
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

            // set up tiered projectiles
            gun.Volley.projectiles = new(){
                // Tier 0
                AimuMod(fireRate: 20, projectiles: new(){
                    AimuProj(invert: false, amplitude: 0.0f, sound: "aimu_shoot_sound"),
                    }),
                // Tier 1
                AimuMod(fireRate: 15, projectiles: new(){
                    AimuProj(invert: false, amplitude: 0.75f, sound: "aimu_shoot_sound"),
                    AimuProj(invert: true,  amplitude: 0.75f, sound: "aimu_shoot_sound"),
                    }),
                // Tier 2
                AimuMod(fireRate: 10, projectiles: new(){
                    AimuProj(invert: false, amplitude: 0.75f, sound: "aimu_shoot_sound"),
                    AimuProj(invert: true,  amplitude: 0.75f, sound: "aimu_shoot_sound"),
                    }),
                // Tier 3
                AimuMod(fireRate: 5, projectiles: new(){
                    AimuProj(invert: false, amplitude: 0.75f, sound: "aimu_shoot_sound"),
                    AimuProj(invert: true,  amplitude: 0.75f, sound: "aimu_shoot_sound"),
                    AimuProj(invert: false, amplitude: 2.25f, sound: "aimu_shoot_sound_alt", trailColor: Color.white),
                    AimuProj(invert: true,  amplitude: 2.25f, sound: "aimu_shoot_sound_alt", trailColor: Color.white),
                    }),
                // Tier 4
                AimuMod(fireRate: 3, projectiles: new(){
                    AimuProj(invert: false, amplitude: 0.75f, sound: "aimu_shoot_sound"),
                    AimuProj(invert: true,  amplitude: 0.75f, sound: "aimu_shoot_sound"),
                    AimuProj(invert: false, amplitude: 2.25f, sound: "aimu_shoot_sound_alt", trailColor: Color.white),
                    AimuProj(invert: true,  amplitude: 2.25f, sound: "aimu_shoot_sound_alt", trailColor: Color.white),
                    beamProj,
                    }),
            };

            gun.gameObject.AddComponent<AimuHakureiAmmoDisplay>();
        }

        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
            if (projectile.GetComponentInChildren<TrailController>())
            {
                AkSoundEngine.PostEvent("aimu_beam_sound_2_stop_all", this.Owner.gameObject);
                AkSoundEngine.PostEvent("aimu_beam_sound_2", this.Owner.gameObject);
            }
        }

        protected override void OnPickup(GameActor owner)
        {
            base.OnPickup(owner);
            this.graze                   = 0; // reset graze when dropped
            this.gun.CurrentStrengthTier = 0;
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

        private static Projectile AimuProj(bool invert, float amplitude, string sound, Color? trailColor = null)
        {
            Projectile proj = _ProjBase.ClonePrefab<Projectile>();
                proj.AddDefaultAnimation(_BulletSprite);
                proj.gameObject.AddComponent<AimuHakureiProjectileBehavior>()
                    .Setup(invert: invert, amplitude: amplitude, sound: sound);
                AddTrail(proj, trailColor);
            return proj;
        }

        private static void AddTrail(Projectile p, Color? trailColor = null)
        {
            EasyTrailBullet trail = p.gameObject.AddComponent<EasyTrailBullet>();
                trail.TrailPos   = p.transform.position.XY() + new Vector2(5f / C.PIXELS_PER_TILE, 5f / C.PIXELS_PER_TILE); // offset by middle of the sprite
                trail.StartWidth = 0.5f;
                trail.EndWidth   = 0.05f;
                trail.LifeTime   = 0.1f;
                trail.BaseColor  = trailColor ?? Color.Lerp(Color.magenta, Color.red, 0.5f);
                trail.EndColor   = trailColor ?? Color.Lerp(Color.magenta, Color.red, 0.5f);
        }

        protected override void Update()
        {
            base.Update();
            CheckForGraze();
        }

        private void PowerUp()
        {
            ++this.gun.CurrentStrengthTier;
            // GameObject aura = SpawnManager.SpawnVFX(WarriorsGi._ZenkaiAura, this.Owner.sprite.WorldBottomCenter, Quaternion.identity);
            //     aura.transform.parent = this.Owner.transform;
            //     aura.ExpireIn(0.5f, 0.5f);
            AkSoundEngine.PostEvent("aimu_power_up_sound", this.Owner.gameObject);
        }

        private void PowerDown()
        {
            --this.gun.CurrentStrengthTier;
        }

        // private static List<Projectile> shortList; // cache projectiles that are nearby so we can look them up every other frame
        private static Dictionary<Projectile, int> _GrazeDict = new();
        private static Dictionary<Projectile, float> _GrazeTimeDict = new();
        private void CheckForGraze()
        {
            if (this.Owner is not PlayerController pc)
                return; // if our owner isn't a player, we have nothing to do

            if (this.graze > 0 && (this._lastDecayTime + _GRAZE_DECAY_RATE <= BraveTime.ScaledTimeSinceStartup))
            {
                --this.graze;
                if (this.gun.CurrentStrengthTier > 0 && graze < _GRAZE_TIER_THRESHOLDS[this.gun.CurrentStrengthTier-1])
                    PowerDown();
                this._lastDecayTime = BraveTime.ScaledTimeSinceStartup;
            }

            if (!pc.healthHaver.IsVulnerable)
                return; // can't graze if we're invincible, that's cheating!!!

            Vector2 ppos = pc.sprite.WorldCenter;
            foreach (Projectile p in StaticReferenceManager.AllProjectiles)
            {
                if (!p.collidesWithPlayer || p.Owner == this.Owner)
                    continue; // if the projectile can't collide with us, we're not impressed

                if (p.sprite?.WorldCenter is not Vector2 epos)
                    continue; // don't care about projectiles without sprites

                if ((epos-ppos).sqrMagnitude < _GRAZE_THRES_SQUARED)
                {
                    // Shenanigans to make sure pooled projectiles don't count as already-grazed when they respawn
                    if (!_GrazeTimeDict.ContainsKey(p))
                        _GrazeTimeDict[p] = 0;
                    if (_GrazeTimeDict[p] + 1f < BraveTime.ScaledTimeSinceStartup)
                        _GrazeDict[p] = 0; // reset our grazedict timer if we haven't been near it for at least one second
                    _GrazeTimeDict[p] = BraveTime.ScaledTimeSinceStartup;

                    if (_GrazeDict[p] < _MAX_GRAZE_PER_PROJECTILE)
                    {
                        ++_GrazeDict[p];
                        if (++this.graze > _GRAZE_MAX)
                            this.graze = _GRAZE_MAX;
                    }

                    Vector2 finalpos = ppos + BraveMathCollege.DegreesToVector(Lazy.RandomAngle());
                    FancyVFX.Spawn(SoulLinkStatus._SoulLinkSoulVFX, finalpos, Quaternion.identity, parent: p.transform,
                        velocity: 3f * Vector2.up, lifetime: 0.5f, fadeOutTime: 0.5f, emissivePower: 50f, emissiveColor: Color.white);

                    if (this.gun.CurrentStrengthTier < _GRAZE_TIER_THRESHOLDS.Count() && graze >= _GRAZE_TIER_THRESHOLDS[this.gun.CurrentStrengthTier])
                        PowerUp();
                }
            }
        }
    }

    public class AimuHakureiAmmoDisplay : CustomAmmoDisplay
    {
        private Gun _gun;
        private AimuHakurei _aimu;
        private PlayerController _owner;
        private void Start()
        {
            this._gun = base.GetComponent<Gun>();
            this._aimu = this._gun.GetComponent<AimuHakurei>();
            this._owner = this._gun.CurrentOwner as PlayerController;
        }

        private void Update()
        {
          // enter update code here
        }

        public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
        {
            if (!this._owner)
                return false;

            float phase = Mathf.Sin(4f * BraveTime.ScaledTimeSinceStartup);
            uic.SetAmmoCountLabelColor(Color.Lerp(Color.magenta, Color.white, Mathf.Abs(phase)));
            uic.GunAmmoCountLabel.Text = $"{this._aimu.graze}";
            return true;
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
        public string sound;

        public void Setup(bool invert, float amplitude, string sound)
        {
            this.invert = invert;
            this.amplitude = amplitude;
            this.sound = sound;
        }

        private void Start()
        {
            this._projectile = base.GetComponent<Projectile>();
            this._owner = this._projectile.Owner as PlayerController;
            this._aimu = new AimuHakureiProjectileMotionModule();
                this._aimu.ForceInvert = this.invert;
                this._aimu.amplitude = this.amplitude;
            this._projectile.OverrideMotionModule = this._aimu;

            AkSoundEngine.PostEvent(this.sound, base.gameObject);
        }
    }

    // modified from HelixProjectileMotionModule
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
            _initialRightVector  = ((!shouldRotate) ? m_currentDirection : projectileTransform.right.XY());
            _initialUpVector     = ((!shouldRotate) ? (Quaternion.Euler(0f, 0f, 90f) * m_currentDirection) : projectileTransform.up);
            _initialized         = true;
            _xDisplacement       = 0f;
            _yDisplacement       = 0f;
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
