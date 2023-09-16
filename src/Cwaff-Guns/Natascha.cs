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
    public class Natascha : AdvancedGunBehavior
    {
        public static string ItemName         = "Natascha";
        public static string SpriteName       = "natascha";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "Fear no Man";
        public static string LongDescription  = "(Gets more powerful the longer you fire, but you slow down as well.)";

        internal static tk2dSpriteAnimationClip _BulletSprite;
        internal static float                   _BaseCooldownTime = 0.4f;
        internal static int                     _FireAnimationFrames = 8;

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
                gun.DefaultModule.cooldownTime        = _BaseCooldownTime;
                gun.DefaultModule.numberOfShotsInClip = -1;
                gun.quality                           = PickupObject.ItemQuality.D;
                gun.barrelOffset.transform.localPosition = new Vector3(2.0625f, 0.5f, 0f); // should match "Casing" in JSON file
                gun.SetBaseMaxAmmo(2500);
                gun.CurrentAmmo = 2500;
                gun.SetAnimationFPS(gun.shootAnimation, (int)((float)_FireAnimationFrames / _BaseCooldownTime) + 1);

            var comp = gun.gameObject.AddComponent<Natascha>();
                comp.SetFireAudio(); // prevent fire audio, as it's handled in OnPostFired()

            _BulletSprite = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("natascha_bullet").Base(),
                12, true, new IntVector2((int)(_NATASHA_PROJECTILE_SCALE * 15), (int)(_NATASHA_PROJECTILE_SCALE * 7)),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.AddAnimation(_BulletSprite);
                projectile.SetAnimation(_BulletSprite);
                projectile.baseData.damage  = 3f;
                projectile.baseData.speed   = 20.0f;
                projectile.transform.parent = gun.barrelOffset;
        }

        public override void OnPostFired(PlayerController player, Gun gun)
        {
            base.OnPostFired(player, gun);
            AkSoundEngine.PostEvent("tomislav_shoot", gun.gameObject);
            if (this._speedMult <= 0.15f)
                return;

            this._speedMult *= 0.85f;
            float secondsBetweenShots = this._speedMult * _BaseCooldownTime;
            gun.AdjustAnimation( // add 1 to FPS to make sure the animation doesn't skip a loop
                gun.shootAnimation, fps: (int)((float)_FireAnimationFrames / secondsBetweenShots) + 1);
            this.RecalculateGunStats();
        }

        public override void OnFinishAttack(PlayerController player, Gun gun)
        {
            this._speedMult = 1.0f;
            gun.AdjustAnimation( // add 1 to FPS to make sure the animation doesn't skip a loop
                gun.shootAnimation, fps: (int)((float)_FireAnimationFrames / _BaseCooldownTime) + 1);
            this.RecalculateGunStats();
        }

        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            base.OnPickedUpByPlayer(player);
            player.OnRollStarted += this.OnDodgeRoll;
        }

        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            base.OnPostDroppedByPlayer(player);
            player.OnRollStarted -= this.OnDodgeRoll;
        }

        private void OnDodgeRoll(PlayerController player, Vector2 dirVec)
        {
            this._speedMult = 1.0f;
            this.RecalculateGunStats();
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
}
