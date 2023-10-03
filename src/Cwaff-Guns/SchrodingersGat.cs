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
    /* TODO:
        - make enemy projectiles flash and disappear with enemy
        - add nicer particles and sounds for disappearance
    */
    public class SchrodingersGat : AdvancedGunBehavior
    {
        public static string ItemName         = "Schrodinger's Gat";
        public static string SpriteName       = "schrodingers_gat";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "TBD";
        public static string LongDescription  = "TBD";

        internal static tk2dSpriteAnimationClip _BulletSprite;

        private const float _NATASHA_PROJECTILE_SCALE = 0.5f;
        private float _speedMult                      = 1.0f;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.gunSwitchGroup                    = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup;
                gun.DefaultModule.ammoCost            = 1;
                gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.Automatic;
                gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
                gun.reloadTime                        = 1.1f;
                gun.DefaultModule.angleVariance       = 15.0f;
                gun.DefaultModule.cooldownTime        = 0.125f;
                gun.DefaultModule.numberOfShotsInClip = -1;
                gun.quality                           = PickupObject.ItemQuality.B;
                gun.barrelOffset.transform.localPosition = new Vector3(1.625f, 0.25f, 0f); // should match "Casing" in JSON file
                gun.SetBaseMaxAmmo(2500);
                gun.CurrentAmmo = 2500;
                gun.SetAnimationFPS(gun.idleAnimation, 24);
                gun.SetAnimationFPS(gun.shootAnimation, 24);

            var comp = gun.gameObject.AddComponent<SchrodingersGat>();
                comp.SetFireAudio(); // prevent fire audio, as it's handled in OnPostFired()

            _BulletSprite = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("natascha_bullet").Base(),
                12, true, new IntVector2((int)(_NATASHA_PROJECTILE_SCALE * 15), (int)(_NATASHA_PROJECTILE_SCALE * 7)),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.AddAnimation(_BulletSprite);
                projectile.SetAnimation(_BulletSprite);
                projectile.baseData.damage  = 0f;
                projectile.baseData.speed   = 30.0f;
                projectile.transform.parent = gun.barrelOffset;

            projectile.gameObject.AddComponent<SchrodingersProjectile>();
        }

        public override void OnPostFired(PlayerController player, Gun gun)
        {
            base.OnPostFired(player, gun);
            AkSoundEngine.PostEvent("schrodingers_gat_fire_sound", gun.gameObject);
        }

        private void RecalculateGunStats()
        {
            if (!this.Player)
                return;

            this.gun.RemoveStatFromGun(PlayerStats.StatType.MovementSpeed);
            this.gun.AddStatToGun(PlayerStats.StatType.MovementSpeed, (float)Math.Sqrt(this._speedMult), StatModifier.ModifyMethod.MULTIPLICATIVE);
            this.gun.RemoveStatFromGun(PlayerStats.StatType.RateOfFire);
            this.gun.AddStatToGun(PlayerStats.StatType.RateOfFire, 1.0f / this._speedMult, StatModifier.ModifyMethod.MULTIPLICATIVE);
            this.Player.stats.RecalculateStats(this.Player);
        }
    }

    public class SchrodingersProjectile : MonoBehaviour
    {
        private void Start()
        {
            base.GetComponent<Projectile>().specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
            base.GetComponent<Projectile>().OnHitEnemy += (Projectile p, SpeculativeRigidbody enemy, bool _) => {
                if (enemy.GetComponent<AIActor>()?.IsAliveAndNotABoss() ?? false)
                    enemy.aiActor.gameObject.GetOrAddComponent<SchrodingersStat>();
            };
        }

        private void OnPreCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, SpeculativeRigidbody other, PixelCollider otherPixelCollider)
        {
            if (other.gameActor is not AIActor enemy)
                return;
            if (enemy.gameObject.GetComponent<SchrodingersStat>())
                PhysicsEngine.SkipCollision = true; // can't apply Schrodinger's Stat to an enemy who already has it
        }
    }

    public class SchrodingersStat : MonoBehaviour
    {
        private bool _diesNextHit;
        private bool _observed;
        private bool _doneUpdating;
        private bool _enemyVisible;
        private AIActor _enemy;
        private Renderer _renderer;

        private void Start()
        {
            this._enemy                        = base.GetComponent<AIActor>();
            this._observed                     = false;
            this._doneUpdating                 = false;
            this._enemyVisible                 = true;
            this._diesNextHit                  = Lazy.CoinFlip();
            this._enemy.healthHaver.OnDamaged += this.OnDamaged;
            this._renderer                     = this._enemy.GetComponent<Renderer>();

            string dies = this._diesNextHit ? "die" : "not die";
            ETGModConsole.Log($"Enemy {this._enemy.name} will {dies} next hit");
            AkSoundEngine.PostEvent("schrodinger_bullet_hit", base.gameObject);
        }

        private void LateUpdate()
        {
            if (this._doneUpdating)
                return;
            if (this._observed)
            {
                this._renderer.enabled = true;
                this._enemy.sprite.usesOverrideMaterial = false;
                this._doneUpdating = true;
                return;
            }
            this._enemyVisible ^= true;
            this._renderer.enabled = this._enemyVisible;
        }

        private void OnDamaged(float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection)
        {
            if (!this._enemy)
                return;

            this._observed = true;
            this._enemy.healthHaver.OnDamaged -= this.OnDamaged;
            if (!this._diesNextHit)
                return;

            AkSoundEngine.PostEvent("schrodinger_dead_sound", base.gameObject);
            this._enemy?.EraseFromExistenceWithRewards(false);
        }
    }
}
