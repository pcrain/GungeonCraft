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
    public class Alyx : AdvancedGunBehavior
    {
        public static string ItemName         = "Alyx";
        public static string SpriteName       = "alyx";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "Welcome to the New Age";
        public static string LongDescription  = "(Poisons and ignites enemies; current and max ammo both suffer from exponential decay)";

        internal const float _AMMO_HALF_LIFE_SECS =  90.0f;
        internal const float _GUN_HALF_LIFE_SECS  = 300.0f;
        internal const float _MIN_CALC_RATE       = 0.1f; // we don't need to recalculate every single gosh darn frame
        internal const int   _BASE_MAX_AMMO       = 1000;
        internal const int   _MIN_AMMO_TO_PERSIST = 10;

        internal static tk2dSpriteAnimationClip _BulletSprite;

        private float _timeAtSpawn      = 0.0f;
        private float _timeAtLastRecalc = 0.0f;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.gunSwitchGroup                    = (ItemHelper.Get(Items.MarineSidearm) as Gun).gunSwitchGroup;
                gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.Automatic;
                gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
                gun.DefaultModule.numberOfShotsInClip = 16;
                gun.quality                           = PickupObject.ItemQuality.A;
                gun.barrelOffset.transform.localPosition = new Vector3(1.0625f, 0.3125f, 0f); // should match "Casing" in JSON file
                gun.SetBaseMaxAmmo(_BASE_MAX_AMMO);
                gun.CurrentAmmo = _BASE_MAX_AMMO;

            var comp = gun.gameObject.AddComponent<Alyx>();
                comp.SetFireAudio(); // prevent fire audio, as it's handled in OnPostFired()

            _BulletSprite = AnimateBullet.CreateProjectileAnimation(
                new List<string> {
                    "alyx-projectile1",
                    "alyx-projectile2",
                    "alyx-projectile3",
                    "alyx-projectile4",
                }, 16, true, new IntVector2(9, 9),
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
            AkSoundEngine.PostEvent("alyx_shoot_sound", gun.gameObject);
        }

        public override void Start()
        {
            base.Start();
            RecalculateAmmo();
        }

        protected override void NonCurrentGunUpdate()
        {
            base.NonCurrentGunUpdate();
            RecalculateAmmo();
        }

        protected override void Update()
        {
            base.Update();
            RecalculateAmmo();
        }

        protected override void OnPickup(GameActor owner)
        {
            base.OnPickup(owner);
            RecalculateAmmo();
        }

        public override void OnSwitchedToThisGun()
        {
            base.OnSwitchedToThisGun();
            RecalculateAmmo();
        }

        internal static int ComputeExponentialDecay(float startAmount, float halfLifeInSeconds, float timeElapsed)
        {
            float lambda = Mathf.Log(2) / halfLifeInSeconds;
            float decay = Mathf.Exp(-lambda * timeElapsed);
            float newAmount = startAmount * decay;
            return (UnityEngine.Random.Range(0f, 1f) <= (newAmount - Math.Truncate(newAmount))
                ? Mathf.CeilToInt(newAmount)
                : Mathf.FloorToInt(newAmount)); // handle rounding gracefully
        }

        private void RecalculateAmmo()
        {
            if (this._timeAtLastRecalc == 0)
                this._timeAtLastRecalc = BraveTime.ScaledTimeSinceStartup;

            float timeSinceLastRecalc = BraveTime.ScaledTimeSinceStartup - this._timeAtLastRecalc;
            if (timeSinceLastRecalc <= _MIN_CALC_RATE)
                return;
            this._timeAtLastRecalc = BraveTime.ScaledTimeSinceStartup;

            this.gun.CurrentAmmo    = ComputeExponentialDecay((float)this.gun.CurrentAmmo, _AMMO_HALF_LIFE_SECS, timeSinceLastRecalc);
            this.gun.SetBaseMaxAmmo(ComputeExponentialDecay((float)this.gun.GetBaseMaxAmmo(), _GUN_HALF_LIFE_SECS, timeSinceLastRecalc));
        }
    }
}
