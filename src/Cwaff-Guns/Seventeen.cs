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
    public class Seventeen : AdvancedGunBehavior
    {
        public static string ItemName         = "Seventeen";
        public static string SpriteName       = "seventeen";
        public static string ProjectileName   = "38_special"; //for rotation niceness
        public static string ShortDescription = "Not Again";
        public static string LongDescription  = "(fires strong projectiles that do no damage until bouncing at least once)";

        internal static tk2dSpriteAnimationClip _ProjSpriteInactive = null;
        internal static tk2dSpriteAnimationClip _ProjSpriteActive   = null;
        internal static ExplosionData           _MiniExplosion      = null;
        internal static float                   _Damage_Factor      = 0.5f; // % of speed converted to damage
        internal static float                   _Force_Factor       = 0.5f; // % of speed converted to force

        internal const float _ACCELERATION = 1.9f;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.gunSwitchGroup                       = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup; // silent default sounds
                gun.DefaultModule.ammoCost               = 1;
                gun.DefaultModule.shootStyle             = ProjectileModule.ShootStyle.SemiAutomatic;
                gun.DefaultModule.sequenceStyle          = ProjectileModule.ProjectileSequenceStyle.Random;
                gun.reloadTime                           = 0.80f;
                gun.DefaultModule.cooldownTime           = 0.16f;
                gun.DefaultModule.numberOfShotsInClip    = 6;
                gun.gunClass                             = GunClass.PISTOL;
                gun.quality                              = PickupObject.ItemQuality.C;
                gun.gunHandedness                        = GunHandedness.OneHanded;
                gun.barrelOffset.transform.localPosition = new Vector3(1.6875f, 0.5f, 0f); // should match "Casing" in JSON file
                gun.SetBaseMaxAmmo(300);
                gun.SetAnimationFPS(gun.shootAnimation, 14);
                gun.SetAnimationFPS(gun.reloadAnimation, 20);

            var comp = gun.gameObject.AddComponent<Seventeen>();
                // comp.SetFireAudio("MC_Mushroom_Bounce");
                comp.SetFireAudio("MC_RocsCape");
                comp.SetReloadAudio("MC_Link_Grow");

            IntVector2 colliderSize = new IntVector2(1,1); // 1-pixel collider for accurate bounce animation

            _ProjSpriteInactive = AnimateBullet.CreateProjectileAnimation(
                new List<string> {
                    // "bouncelet_gray_001",
                    "energy_bounce1",
                    "energy_bounce2",
                    "energy_bounce3",
                    "energy_bounce4",
                }, 10, true, new IntVector2(10, 10), // reduced sprite size
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true, null, colliderSize);
            _ProjSpriteActive = AnimateBullet.CreateProjectileAnimation(
                new List<string> {
                    // "bouncelet_001",
                    "energy_bounce1",
                    "energy_bounce2",
                    "energy_bounce3",
                    "energy_bounce4",
                }, 10, true, new IntVector2(10, 10), // reduced sprite size
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true, null, colliderSize);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.AddAnimation(_ProjSpriteActive);
                projectile.AddAnimation(_ProjSpriteInactive);
                projectile.baseData.damage = _ACCELERATION;
                projectile.baseData.speed  = _ACCELERATION;
                projectile.baseData.range  = 9999f;
                projectile.gameObject.AddComponent<HarmlessUntilBounce>();

            // Initialize our explosion data
            ExplosionData defaultExplosion = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultSmallExplosionData;
            _MiniExplosion = new ExplosionData()
            {
                forceUseThisRadius     = true,
                pushRadius             = 0.5f,
                damageRadius           = 0.5f,
                damageToPlayer         = 0f,
                doDamage               = true,
                damage                 = 10,
                doDestroyProjectiles   = false,
                doForce                = true,
                debrisForce            = 10f,
                preventPlayerForce     = true,
                explosionDelay         = 0.01f,
                usesComprehensiveDelay = false,
                doScreenShake          = false,
                playDefaultSFX         = true,
                effect                 = defaultExplosion.effect,
                ignoreList             = defaultExplosion.ignoreList,
                ss                     = defaultExplosion.ss,
            };
        }

        public override void OnReload(PlayerController player, Gun gun)
        {
            gun.gunHandedness = GunHandedness.TwoHanded;
            base.OnReload(player, gun);
        }

        public override void OnReloadEnded(PlayerController player, Gun gun)
        {
            gun.gunHandedness = GunHandedness.TwoHanded;
            // gun.gunHandedness = GunHandedness.OneHanded;
            base.OnReloadEnded(player, gun);
        }
    }

    public class HarmlessUntilBounce : MonoBehaviour
    {
        private Projectile _projectile;
        private PlayerController _owner;
        private bool _bounceStarted = false;
        private bool _bounceFinished = false;

        private void Start()
        {
            this._projectile = base.GetComponent<Projectile>();
            if (this._projectile.Owner is not PlayerController pc)
                return;
            this._owner = pc;

            BounceProjModifier bounce = this._projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
                bounce.numberOfBounces     = 1; // needs to be more than 1 or projectile dies immediately in special handling code below
                bounce.chanceToDieOnBounce = 0f;
                bounce.OnBounce += OnBounce;
                bounce.onlyBounceOffTiles = true;

            this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;

            this._projectile.SetAnimation(Seventeen._ProjSpriteInactive); // TODO: this doesn't seem to work properly; default sprite is always first sprite added
        }

        private void Update()
        {
            if (_bounceStarted)
                return;
            this._projectile.baseData.speed += Seventeen._ACCELERATION;
            this._projectile.UpdateSpeed();
        }

        private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            if (this._bounceFinished)
                return;

            // skip non-tile collisions if we haven't bounced yet
            if (!otherRigidbody.PrimaryPixelCollider.IsTileCollider)
                PhysicsEngine.SkipCollision = true;
            else if (otherRigidbody.GetComponent<DungeonPlaceable>() != null)
                PhysicsEngine.SkipCollision = true;
            else if (otherRigidbody.GetComponent<MinorBreakable>() != null)
                PhysicsEngine.SkipCollision = true;
            else if (otherRigidbody.GetComponent<MajorBreakable>() != null)
                PhysicsEngine.SkipCollision = true;
        }

        private static float _LastBouncePlayed = 0;
        private const  float _MIN_SOUND_GAP = 0.25f;
        private void HandleBounceSounds()
        {
            float now = BraveTime.ScaledTimeSinceStartup;
            if ((now - _LastBouncePlayed) < _MIN_SOUND_GAP)
                return;
            _LastBouncePlayed = now;
            // AkSoundEngine.PostEvent("MC_Link_Grow_stop_all", this._projectile.gameObject);
            AkSoundEngine.PostEvent("MC_RocsCape", this._projectile.gameObject);
            // AkSoundEngine.PostEvent("MC_Mushroom_Bounce_stop_all", this._projectile.gameObject);
            AkSoundEngine.PostEvent("MC_Mushroom_Bounce", this._projectile.gameObject);
        }

        private void OnBounce()
        {
            this._bounceStarted = true;
            this._projectile = base.GetComponent<Projectile>();
            this._projectile.SetAnimation(Seventeen._ProjSpriteActive);

            this._projectile.StartCoroutine(DoElasticBounce());
            // AkSoundEngine.PostEvent("MC_Link_Lift_stop_all", this._projectile.gameObject);
            // AkSoundEngine.PostEvent("MC_Link_Lift", this._projectile.gameObject);
        }

        private const float BOUNCE_TIME = 15.0f; // frames for half a bounce
        private IEnumerator DoElasticBounce()
        {
            float oldSpeed = this._projectile.baseData.speed;
            Vector3 oldScale = this._projectile.spriteAnimator.transform.localScale;

            this._projectile.baseData.damage = oldSpeed * Seventeen._Damage_Factor;  // base damage should scale with speed
            this._projectile.baseData.force = oldSpeed * Seventeen._Force_Factor;  // force should scale with speed
            this._projectile.baseData.speed = 0.001f;
            this._projectile.UpdateSpeed();
            this._projectile.specRigidbody.Reinitialize();

            float bounceScale = 1.0f / BOUNCE_TIME;
            for (int i = (int)BOUNCE_TIME; i > 1; --i)
            {
                this._projectile.spriteAnimator.transform.localScale = oldScale.WithX(bounceScale*i);
                yield return null;
            }
            HandleBounceSounds();
            for (int i = 1; i < BOUNCE_TIME; ++i)
            {
                this._projectile.spriteAnimator.transform.localScale = oldScale.WithX(bounceScale*i);
                yield return null;
            }
            this._projectile.spriteAnimator.transform.localScale = oldScale;

            this._projectile.baseData.speed = oldSpeed;
            this._projectile.UpdateSpeed();
            this._projectile.specRigidbody.Reinitialize();
            this._projectile.OnDestruction += (Projectile p) => Exploder.Explode(
                this._projectile.sprite.WorldCenter, Seventeen._MiniExplosion, p.Direction);

            this._bounceFinished = true;
            this._projectile.sprite.usesOverrideMaterial = true;
            Material m = this._projectile.sprite.renderer.material;
                m.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
                m.SetFloat("_EmissivePower", 100f);
                m.SetFloat("_EmissiveColorPower", 1.55f);
                m.SetColor("_EmissiveColor", Color.yellow);
                m.SetColor("_OverrideColor", Color.yellow);
        }
    }
}
