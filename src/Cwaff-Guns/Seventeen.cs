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

            _ProjSpriteInactive = AnimateBullet.CreateProjectileAnimation(
                new List<string> {
                    "bouncelet_gray_001",
                }, 10, true, new IntVector2(14, 14),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);
            _ProjSpriteActive = AnimateBullet.CreateProjectileAnimation(
                new List<string> {
                    "bouncelet_001",
                }, 10, true, new IntVector2(14, 14),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.AddAnimation(_ProjSpriteActive);
                projectile.AddAnimation(_ProjSpriteInactive);
                projectile.baseData.damage = _Damage_After_Bounce;
                projectile.gameObject.AddComponent<HarmlessUntilBounce>();
        }
    }

    public class HarmlessUntilBounce : MonoBehaviour
    {
        private Projectile _projectile;
        private PlayerController _owner;
        private bool _bounceFinished = false;
        private bool _bounceStarted = false;
        private Vector3? _lastBouncePos = null;

        private void Start()
        {
            this._projectile = base.GetComponent<Projectile>();
            if (this._projectile.Owner is not PlayerController pc)
                return;
            this._owner = pc;

            BounceProjModifier bounce = this._projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
                bounce.numberOfBounces     = 10; // needs to be more than 1 or projectile dies immediately in special handling code below
                bounce.chanceToDieOnBounce = 0f;
                bounce.OnBounce += OnBounce;

            this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
            this._projectile.specRigidbody.OnPreTileCollision += this.OnPreTileCollision;
            this._projectile.specRigidbody.OnTileCollision += this.OnTileCollision;
            // this._projectile.BulletScriptSettings.surviveTileCollisions = true;

            this._projectile.SetAnimation(Seventeen._ProjSpriteInactive); // TODO: this doesn't seem to work properly; default sprite is always first sprite added
        }

        private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            // if (this._bounceStarted && !this._bounceFinished)
            // {
            //     PhysicsEngine.SkipCollision = true;
            //     return;
            // }

            if (otherRigidbody.GetComponent<AIActor>() is AIActor enemy)
            {
                if (!this._bounceFinished)
                    PhysicsEngine.SkipCollision = true; // skip collision if we haven't bounced yet
                return;
            }
        }

        private void OnPreTileCollision(SpeculativeRigidbody myrigidbody, PixelCollider mypixelcollider, PhysicsEngine.Tile tile, PixelCollider tilepixelcollider)
        {
            if (this._bounceFinished)
            {
                PhysicsEngine.SkipCollision = false;
                return;
            }

            if (this._bounceStarted)
            {
                PhysicsEngine.SkipCollision = true;
                return;
            }

            this._bounceStarted = true;
            _lastBouncePos = PhysicsEngine.PendingCastResult.Contact.ToVector3ZisY(0f);
            ETGModConsole.Log($"bounce at {_lastBouncePos}");
        }


        private void OnTileCollision(CollisionData tileCollision)
        {
            ETGModConsole.Log($"  tile collision");
        }

        private void OnBounce()
        {
            ETGModConsole.Log($"  onbounce");
            this._projectile = base.GetComponent<Projectile>();
            this._projectile.SetAnimation(Seventeen._ProjSpriteActive);
            this._projectile.StartCoroutine(DoElasticBounce());
        }

        private IEnumerator DoElasticBounce()
        {
            float oldSpeed = this._projectile.baseData.speed;
            Vector3 oldScale = this._projectile.spriteAnimator.transform.localScale;

            this._bounceStarted = true;
            this._projectile.specRigidbody.CollideWithOthers = false;
            this._projectile.specRigidbody.CollideWithTileMap = false;
            this._projectile.BulletScriptSettings.surviveTileCollisions = true;
            this._projectile.BulletScriptSettings.surviveRigidbodyCollisions = true;

            // if (_lastBouncePos.HasValue)
            //     this._projectile.transform.position = _lastBouncePos.Value;
            this._projectile.baseData.speed = 0.001f;
            this._projectile.UpdateSpeed();
            this._projectile.specRigidbody.Reinitialize();
            ETGModConsole.Log($"  elastic start");
            for (int i = 10; i > 3; --i)
            {
                this._projectile.spriteAnimator.transform.localScale = oldScale.WithX(((float)i)/10f);
                yield return null;
            }
            yield return new WaitForSeconds(0.1f);
            for (int i = 3; i < 10; ++i)
            {
                this._projectile.spriteAnimator.transform.localScale = oldScale.WithX(((float)i)/10f);
                yield return null;
            }
            this._projectile.spriteAnimator.transform.localScale = oldScale;
            this._projectile.baseData.speed = oldSpeed;
            this._projectile.UpdateSpeed();

            this._bounceFinished = true;
            this._projectile.specRigidbody.CollideWithOthers = true;
            this._projectile.specRigidbody.CollideWithTileMap = true;
            this._projectile.BulletScriptSettings.surviveTileCollisions = false;
            this._projectile.BulletScriptSettings.surviveRigidbodyCollisions = false;
            ETGModConsole.Log($"  elastic finish");
        }
    }
}
