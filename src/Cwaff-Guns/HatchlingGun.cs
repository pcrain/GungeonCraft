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
    public class HatchlingGun : AdvancedGunBehavior
    {
        public static string ItemName         = "Hatchling Gun";
        public static string SpriteName       = "hatchling_gun";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "Yolked In";
        public static string LongDescription  = $"Fires eggs which spawn chicks on impact. Chicks randomly wander the room, blocking enemies and their projectiles until taking damage.\n\nThe age-old question \"which came first, the chicken or the egg?\" is mostly of academic interest. Questions of more practical interest to gunsmiths include \"what is the fastest an egg can be fired out of a gun without it breaking in transit?\" and \"how much damage can a singular egg inflict on the Gundead?\" The answers to these questions turn out to be \"not very fast\" and \"not very much,\" respectively. As such, most gunsmiths have no interest in forging guns that fire eggs as projectiles, and the {ItemName}'s existence can be largely attributed to an excessive supply of eggs moreso than an excessive demans of egg-shooting firearms.";

        internal static tk2dSpriteAnimationClip _BulletSprite;

        private const float _NATASHA_PROJECTILE_SCALE = 0.5f;
        private float _speedMult                      = 1.0f;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun<HatchlingGun>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.SetAttributes(quality: PickupObject.ItemQuality.D, gunClass: GunClass.RIFLE, reloadTime: 25f / 20f /* frames / fps*/, ammo: 500);
                gun.SetAnimationFPS(gun.shootAnimation, 40);
                gun.SetAnimationFPS(gun.reloadAnimation, 20);
                gun.SetReloadAudio("hatchling_gun_bounce_sound", frame: 0);
                gun.SetReloadAudio("hatchling_gun_bounce_sound", frame: 6);
                gun.SetReloadAudio("hatchling_gun_bounce_sound", frame: 14);

            ProjectileModule mod = gun.DefaultModule;
                mod.ammoCost            = 1;
                mod.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.angleVariance       = 15.0f;
                mod.cooldownTime        = 0.2f;
                mod.numberOfShotsInClip = 12;

            _BulletSprite = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("egg").Base(),
                12, true, new IntVector2(12, 12), // sprite is 8x8 -> 1.5x scale
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.AddDefaultAnimation(_BulletSprite);
                projectile.baseData.damage  = 3f;
                projectile.baseData.speed   = 24.0f;
                projectile.transform.parent = gun.barrelOffset;
                projectile.gameObject.AddComponent<HatchlingProjectile>();

            // Must be done AFTER gun / projectile setup so impact effects don't bleed onto other guns
            VFXPool impactFVX = VFX.RegisterVFXPool("EggBreak", ResMap.Get("egg_break"), fps: 16, loops: false, scale: 0.75f, anchor: tk2dBaseSprite.Anchor.MiddleCenter);
                gun.SetHorizontalImpactVFX(impactFVX);
                gun.SetVerticalImpactVFX(impactFVX);
                gun.SetEnemyImpactVFX(impactFVX);
                gun.SetAirImpactVFX(impactFVX);
        }

        public override void OnPostFired(PlayerController player, Gun gun)
        {
            base.OnPostFired(player, gun);
            AkSoundEngine.PostEvent("hatchling_gun_shoot_sound", gun.gameObject);
        }
    }

    public class HatchlingProjectile : MonoBehaviour
    {
        private const float _HATCH_CHANCE = 1.0f;
        private const float _PATH_INTERVAL = 10.0f;

        private Projectile _projectile;
        private PlayerController _owner;

        private void Start()
        {
            this._projectile = base.GetComponent<Projectile>();
            this._owner = this._projectile.Owner as PlayerController;

            if (this._owner)
                this._projectile.OnDestruction += this.Hatch;
        }

        // Code adapted from CompanionItem::CreateCompanion()
        private void Hatch(Projectile p)
        {
            if (UnityEngine.Random.value > _HATCH_CHANCE)
                return;

            // Create a baby chicken
            GameObject chickum = AIActor.Spawn(EnemyDatabase.GetOrLoadByGuid(Enemies.Cucco), (Vector2)p.LastPosition, p.transform.position.GetAbsoluteRoom(), true).gameObject;
            CompanionController cc = chickum.GetOrAddComponent<CompanionController>();

            // From CompanionItem.Initialize()
            cc.m_owner                        = null; // original was player
            cc.aiActor.IsNormalEnemy          = false;
            cc.aiActor.CompanionOwner         = null; // original was player
            cc.aiActor.CanTargetPlayers       = false;
            cc.aiActor.CanTargetEnemies       = true;  // original was true
            cc.aiActor.State                  = AIActor.ActorState.Normal;
            cc.healthHaver.OnDamaged += (float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection) => {
                AkSoundEngine.PostEvent("bird_chirp", cc.gameObject);
                UnityEngine.Object.Destroy(cc.gameObject);
            };
            cc.aiActor.ParentRoom = p.transform.position.GetAbsoluteRoom(); // needed to avoid null deref for MoveErraticallyBehavior

            if (cc.specRigidbody is SpeculativeRigidbody srb)
            {
                srb.AddCollisionLayerIgnoreOverride(CollisionMask.LayerToMask(CollisionLayer.PlayerHitBox, CollisionLayer.PlayerCollider));
                PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(srb);
            }

            // Remove normal behavior speculators that follow the player and use our own
            if (cc.behaviorSpeculator is BehaviorSpeculator bs)
            {
                bs.m_aiActor = cc.aiActor;
                bs._serializedStateKeys.Clear();
                bs._serializedStateValues.Clear();
                bs.TargetBehaviors.Clear();
                bs.MovementBehaviors.Clear();

                bs.MovementBehaviors.Add(new MoveErraticallyBehavior {
                    PathInterval = _PATH_INTERVAL,
                    StayOnScreen = false,
                    UseTargetsRoom = false,
                    AvoidTarget = false,
                });
                bs.RegisterBehaviors(bs.TargetBehaviors);
                bs.RegisterBehaviors(bs.MovementBehaviors);
                bs.RefreshBehaviors();
            }

            // Make it smol
            cc.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            cc.sprite.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            cc.aiActor.procedurallyOutlined = false; // procedural outlining doesn't respect scale, so remove it
            cc.aiActor.HasShadow = false; // don't cast a blob shadow on the ground to save some rendering juice

            // Make it yellow
            cc.sprite.usesOverrideMaterial = true;
            cc.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
            cc.aiActor.RegisterOverrideColor(new Color(1.0f, 1.0f, 0.0f, 0.5f) , "little chicky");

            // Add HatchlingBehavior
            cc.gameObject.AddComponent<HatchlingBehavior>().Setup(this._owner);
        }
    }

    public class HatchlingBehavior : MonoBehaviour
    {
        private const float _CHECK_INTERVAL = 1.0f;

        private const float _CAMERA_CACHE_INTERVAL = 1.0f;
        private static float _LastCameraCacheTime = 0.0f;
        private static Vector2 _CachedCameraMin;
        private static Vector2 _CachedCameraMax;

        private RoomHandler _startRoom = null;
        private PlayerController _owner = null;
        private AIActor _actor = null;
        private float _lastCheck = 0.0f;

        private void Start()
        {
            this._startRoom = this.gameObject.transform.position.GetAbsoluteRoom();
            this._actor = base.gameObject.GetComponent<AIActor>();
            AkSoundEngine.PostEvent("bird_chirp", base.gameObject);
        }

        public void Setup(PlayerController pc)
        {
            this._owner = pc;
        }

        private void Update()
        {
            this._lastCheck += BraveTime.DeltaTime;
            if (this._lastCheck < _CHECK_INTERVAL)
                return;
            this._lastCheck = 0.0f;

            if (this._owner.CurrentRoom == _startRoom)
                return; // don't despawn even if we're offscreen, so long as the player is in the room we spawned in

            // Conservatively compute the camera coordinates at most once per frame
            if (_LastCameraCacheTime != BraveTime.ScaledTimeSinceStartup)
            {
                _CachedCameraMin     = BraveUtility.ViewportToWorldpoint(new Vector2(0f, 0f), ViewportType.Gameplay);
                _CachedCameraMax     = BraveUtility.ViewportToWorldpoint(new Vector2(1f, 1f), ViewportType.Gameplay);
                _LastCameraCacheTime = BraveTime.ScaledTimeSinceStartup;
            }

            // Check if we're offscreen, and destroy if so
            Vector3 pos = this._actor.Position;
            bool offscreen = pos.x < _CachedCameraMin.x || pos.x > _CachedCameraMax.x || pos.y < _CachedCameraMin.y || pos.y > _CachedCameraMax.y;
            if (offscreen)
                UnityEngine.Object.Destroy(base.gameObject);
        }
    }
}
