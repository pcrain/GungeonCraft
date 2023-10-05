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
    public class TennisRocket : AdvancedGunBehavior
    {
        public static string ItemName         = "Tennis Rocket";
        public static string SpriteName       = "paddle";
        public static string ProjectileName   = "86"; //marine sidearm
        public static string ShortDescription = "TBD";
        public static string LongDescription  = "TBD";

        internal const float _MAX_REFLECT_DISTANCE = 2f;

        internal static tk2dSpriteAnimationClip _BulletSprite;

        private List<TennisBall> _extantTennisBalls = new();

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
            var comp = gun.gameObject.AddComponent<TennisRocket>();
                comp.SetFireAudio();

            gun.DefaultModule.ammoCost               = 0;
            gun.DefaultModule.shootStyle             = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle          = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                           = 0;
            gun.DefaultModule.cooldownTime           = 0.3f;
            gun.muzzleFlashEffects.type              = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip    = 100;
            gun.barrelOffset.transform.localPosition = new Vector3(1.0f, 1.875f, 0);
            gun.quality                              = PickupObject.ItemQuality.D;
            gun.gunClass                             = GunClass.SILLY;
            gun.gunSwitchGroup                       = (ItemHelper.Get(Items.Blasphemy) as Gun).gunSwitchGroup;
            gun.CanReloadNoMatterAmmo                = true;
            gun.CurrentAmmo                          = 100;
            gun.SetBaseMaxAmmo(100);
            gun.SetAnimationFPS(gun.shootAnimation, 60);
            gun.SetAnimationFPS(gun.idleAnimation, 24);
            gun.LoopAnimation(gun.idleAnimation, 0);

            _BulletSprite = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("tennis_ball").Base(),
                12, true, new IntVector2(9, 9),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);

            Projectile projectile              = Lazy.PrefabProjectileFromGun(gun);
                projectile.AddAnimation(_BulletSprite);
                projectile.SetAnimation(_BulletSprite);
                projectile.baseData.damage         = 9f;
                projectile.baseData.speed          = 30f;
                projectile.baseData.range          = 300f;
                projectile.BulletScriptSettings.surviveRigidbodyCollisions = true;

            projectile.gameObject.AddComponent<TennisBall>();

            foreach (ProjectileModule mod in gun.Volley.projectiles)
                mod.ammoCost = 0;
        }

        public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
        {
            if (this._extantTennisBalls.Count == 0 && gun.CurrentAmmo > 0)
            {
                gun.LoseAmmo(1);
                return projectile;
            }
            Vector2 racketpos = gun.barrelOffset.transform.position;
            foreach (TennisBall ball in this._extantTennisBalls)
            {
                if (!ball.Whackable())
                    continue;
                float dist = (racketpos - ball.Position()).magnitude;
                if (dist < _MAX_REFLECT_DISTANCE)
                    ball.GotWhacked(gun.CurrentAngle.ToVector());
            }
            return Lazy.NoProjectile();
        }

        public override void OnPostFired(PlayerController player, Gun gun)
        {
            base.OnPostFired(player, gun);
            // gun.IsGunBlocked()
        }

        public override void OnAmmoChangedSafe(PlayerController player, Gun gun)
        {
            gun.ClipShotsRemaining = gun.CurrentAmmo;
        }

        public override void OnReloadPressedSafe(PlayerController player, Gun gun, bool manualReload)
        {
            if (!manualReload)
                return;
            base.OnReloadPressedSafe(player, gun, manualReload);
            for (int i = this._extantTennisBalls.Count - 1; i >= 0; --i)
                this._extantTennisBalls[i]?.DieInAir();
            this._extantTennisBalls.Clear();
        }

        public void AddExtantTennisBall(TennisBall tennisBall)
        {
            this._extantTennisBalls.Add(tennisBall);
        }

        public void RemoveExtantTennisBall(TennisBall tennisBall)
        {
            this._extantTennisBalls.Remove(tennisBall);
        }
    }

    public class TennisBall : MonoBehaviour
    {
        const float _RETURN_HOMING_STRENGTH = 0.2f;
        const float _SPREAD = 10f;

        private Projectile       _projectile;
        private PlayerController _owner;
        private int              _volleys = 0;
        private bool             _returning = false;
        private TennisRocket     _parentGun = null;

        private void Start()
        {
            this._projectile = base.GetComponent<Projectile>();
            if (this._projectile.Owner is not PlayerController pc)
                return;
            this._owner = pc;

            if (pc.CurrentGun.GetComponent<TennisRocket>() is TennisRocket tr)
                this._parentGun = tr;
            else foreach (Gun g in pc.inventory.AllGuns)
            {
                if (g.GetComponent<TennisRocket>() is TennisRocket tr2)
                {
                    this._parentGun = tr2;
                    break;
                }
            }
            if (this._parentGun)
            {
                this._parentGun.AddExtantTennisBall(this);
                this._projectile.BulletScriptSettings.surviveRigidbodyCollisions = true;
                this._projectile.specRigidbody.OnRigidbodyCollision += (CollisionData rigidbodyCollision) => {
                    this._projectile.SendInDirection(rigidbodyCollision.Normal, false);
                    ReturnToSender();
                };
                this._projectile.OnDestruction += (Projectile p) => {
                    if (p.GetComponent<TennisBall>() is TennisBall tc)
                        this._parentGun.RemoveExtantTennisBall(tc);
                };
                AkSoundEngine.PostEvent("racket_hit", this._projectile.gameObject);
            }


            // don't use an emissive / tinted shader so we can turn off the glowing yellow tint effect
            // this._projectile.sprite.usesOverrideMaterial = true; // keep this off so we still get nice lighting
            // this._projectile.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitBlendUber");

            BounceProjModifier bounce = this._projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
                bounce.numberOfBounces     = 9999;
                bounce.chanceToDieOnBounce = 0f;
                bounce.onlyBounceOffTiles  = false;
                bounce.ExplodeOnEnemyBounce = true;
                bounce.OnBounce += this.ReturnToSender;

            // AkSoundEngine.PostEvent("racket_hit", this._projectile.gameObject);
        }

        public Vector2 Position()
        {
            return this._projectile.sprite.WorldCenter;
        }

        public void DieInAir()
        {
            this._projectile.DieInAir();
        }

        public bool Whackable()
        {
            return this._returning;
        }

        public void GotWhacked(Vector2 direction)
        {
            if (!this._returning)
                return;
            ++this._volleys;
            this._returning = false;
            this._projectile.SendInDirection(direction, true);
            AkSoundEngine.PostEvent("racket_hit", this._projectile.gameObject);
        }

        private void ReturnToSender()
        {
            if (this._returning)
            {
                this._projectile.DieInAir();
                return;
            }
            this._returning = true;
            float dirToOwner = (this._owner.sprite.WorldCenter - this._projectile.sprite.WorldCenter).ToAngle();
            float acc = this._owner.stats.GetStatValue(PlayerStats.StatType.Accuracy);
            this._projectile.SendInDirection(dirToOwner.AddRandomSpread(_SPREAD * Mathf.Sqrt(acc)).ToVector(), true);
            AkSoundEngine.PostEvent("racket_hit", this._projectile.gameObject);
        }

        // private void Update()
        // {
        //     if (this._returning)
        //     {
        //         Vector2 curVelocity = this._projectile.LastVelocity.normalized;
        //         Vector2 targetVelocity = (this._owner.sprite.WorldCenter - this._projectile.sprite.WorldCenter).normalized;
        //         Vector2 newVelocty = (_RETURN_HOMING_STRENGTH * targetVelocity) + ((1 - _RETURN_HOMING_STRENGTH) * curVelocity);
        //         this._projectile.SendInDirection(newVelocty, false);
        //     }
        // }
    }
}
