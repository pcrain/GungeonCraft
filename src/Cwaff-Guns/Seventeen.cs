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
        private static float _Damage_After_Bounce                   = 40f;

        internal const float _ACCELERATION = 1.6f;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.gunSwitchGroup                       = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup; // silent default sounds
                gun.DefaultModule.ammoCost               = 1;
                gun.DefaultModule.shootStyle             = ProjectileModule.ShootStyle.SemiAutomatic;
                gun.DefaultModule.sequenceStyle          = ProjectileModule.ProjectileSequenceStyle.Random;
                gun.reloadTime                           = 0.72f;
                gun.DefaultModule.cooldownTime           = 0.18f;
                gun.DefaultModule.numberOfShotsInClip    = 4;
                gun.gunClass                             = GunClass.PISTOL;
                gun.quality                              = PickupObject.ItemQuality.C;
                gun.barrelOffset.transform.localPosition = new Vector3(1.6875f, 0.5f, 0f); // should match "Casing" in JSON file
                gun.SetBaseMaxAmmo(300);
                gun.SetAnimationFPS(gun.shootAnimation, 14);
                gun.SetAnimationFPS(gun.reloadAnimation, 4);

            var comp = gun.gameObject.AddComponent<Seventeen>();
                comp.SetFireAudio("MC_Mushroom_Bounce");
                comp.SetReloadAudio("MC_Link_Grow");

            IntVector2 colliderSize = new IntVector2(1,1); // 1-pixel collider for accurate bounce animation

            _ProjSpriteInactive = AnimateBullet.CreateProjectileAnimation(
                new List<string> {
                    "bouncelet_gray_001",
                }, 10, true, new IntVector2(10, 10), // reduced sprite size
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true, null, colliderSize);
            _ProjSpriteActive = AnimateBullet.CreateProjectileAnimation(
                new List<string> {
                    "bouncelet_001",
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

        private void OnBounce()
        {
            this._bounceStarted = true;
            this._projectile = base.GetComponent<Projectile>();
            this._projectile.SetAnimation(Seventeen._ProjSpriteActive);
            this._projectile.StartCoroutine(DoElasticBounce());
            AkSoundEngine.PostEvent("MC_Link_Lift_stop_all", this._projectile.gameObject);
            AkSoundEngine.PostEvent("MC_Link_Lift", this._projectile.gameObject);
        }

        private IEnumerator DoElasticBounce()
        {
            float oldSpeed = this._projectile.baseData.speed;
            Vector3 oldScale = this._projectile.spriteAnimator.transform.localScale;

            this._projectile.baseData.damage = oldSpeed;  // base damage should scale with speed
            this._projectile.baseData.force = oldSpeed;  // force should scale with speed
            this._projectile.baseData.speed = 0.001f;
            this._projectile.UpdateSpeed();
            this._projectile.specRigidbody.Reinitialize();

            for (int i = 10; i > 2; --i)
            {
                this._projectile.spriteAnimator.transform.localScale = oldScale.WithX(0.1f*i);
                yield return null;
            }
            for (int i = 2; i < 10; ++i)
            {
                this._projectile.spriteAnimator.transform.localScale = oldScale.WithX(0.1f*i);
                yield return null;
            }
            this._projectile.spriteAnimator.transform.localScale = oldScale;

            this._projectile.baseData.speed = oldSpeed;
            this._projectile.UpdateSpeed();
            this._projectile.specRigidbody.Reinitialize();
            this._projectile.OnDestruction += (Projectile p) => Exploder.Explode(
                this._projectile.sprite.WorldCenter, Seventeen._MiniExplosion, p.Direction);

            this._bounceFinished = true;
        }
    }
}
