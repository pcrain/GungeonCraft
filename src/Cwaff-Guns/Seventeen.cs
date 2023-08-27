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
        private static float _Damage_After_Bounce                   = 40f;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.gunSwitchGroup                       = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup; // silent reload
                gun.DefaultModule.ammoCost               = 1;
                gun.DefaultModule.shootStyle             = ProjectileModule.ShootStyle.SemiAutomatic;
                gun.DefaultModule.sequenceStyle          = ProjectileModule.ProjectileSequenceStyle.Random;
                gun.reloadTime                           = 0.9f;
                gun.DefaultModule.cooldownTime           = 0.18f;
                gun.DefaultModule.numberOfShotsInClip    = 12;
                gun.gunClass                             = GunClass.PISTOL;
                gun.quality                              = PickupObject.ItemQuality.C;
                gun.barrelOffset.transform.localPosition = new Vector3(1.6875f, 0.5f, 0f); // should match "Casing" in JSON file
                // gun.SetFireAudio("paintball_shoot_sound");
                gun.SetBaseMaxAmmo(300);
                gun.SetAnimationFPS(gun.shootAnimation, 14);
                gun.SetAnimationFPS(gun.reloadAnimation, 4);

            var comp = gun.gameObject.AddComponent<Seventeen>();

            IntVector2 colliderSize = new IntVector2(1,1); // 1-pixel collider for accurate bounce animation

            _ProjSpriteInactive = AnimateBullet.CreateProjectileAnimation(
                new List<string> {
                    "bouncelet_gray_001",
                }, 10, true, new IntVector2(14, 14),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true, null, colliderSize);
            _ProjSpriteActive = AnimateBullet.CreateProjectileAnimation(
                new List<string> {
                    "bouncelet_001",
                }, 10, true, new IntVector2(14, 14),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true, null, colliderSize);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.AddAnimation(_ProjSpriteActive);
                projectile.AddAnimation(_ProjSpriteInactive);
                projectile.baseData.damage = _Damage_After_Bounce;
                projectile.baseData.speed  = 44;
                projectile.gameObject.AddComponent<HarmlessUntilBounce>();
        }
    }

    public class HarmlessUntilBounce : MonoBehaviour
    {
        private Projectile _projectile;
        private PlayerController _owner;
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

        private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            if (otherRigidbody.GetComponent<AIActor>() is AIActor && (!this._bounceFinished))
                PhysicsEngine.SkipCollision = true; // skip collision if we haven't bounced yet
        }

        private void OnBounce()
        {
            this._projectile = base.GetComponent<Projectile>();
            this._projectile.SetAnimation(Seventeen._ProjSpriteActive);
            this._projectile.StartCoroutine(DoElasticBounce());
        }

        private IEnumerator DoElasticBounce()
        {
            float oldSpeed = this._projectile.baseData.speed;
            Vector3 oldScale = this._projectile.spriteAnimator.transform.localScale;

            this._projectile.baseData.speed = 0.001f;
            this._projectile.UpdateSpeed();
            this._projectile.specRigidbody.Reinitialize();

            for (int i = 10; i > 3; --i)
            {
                this._projectile.spriteAnimator.transform.localScale = oldScale.WithX(((float)i)/10f);
                yield return null;
            }
            for (int i = 3; i < 10; ++i)
            {
                this._projectile.spriteAnimator.transform.localScale = oldScale.WithX(((float)i)/10f);
                yield return null;
            }
            this._projectile.spriteAnimator.transform.localScale = oldScale;

            this._projectile.baseData.speed = oldSpeed;
            this._projectile.UpdateSpeed();
            this._projectile.specRigidbody.Reinitialize();

            this._bounceFinished = true;
        }
    }
}
