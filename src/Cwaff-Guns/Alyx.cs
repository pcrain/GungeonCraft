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
        public static string LongDescription  = "(Poisons and ignites enemies; current and max ammo decay exponentially leaving radiactive waste; gun decays completely at 10 max ammo)";

        internal const float _AMMO_HALF_LIFE_SECS = 90.0f;
        internal const float _GUN_HALF_LIFE_SECS  = 300.0f;
        internal const float _MIN_CALC_RATE       = 0.1f; // we don't need to recalculate every single gosh darn frame
        internal const int   _BASE_MAX_AMMO       = 1000;
        internal const int   _MIN_AMMO_TO_PERSIST = 10;

        internal static readonly float _AMMO_DECAY_LAMBDA = Mathf.Log(2) / _AMMO_HALF_LIFE_SECS;
        internal static readonly float _GUN_DECAY_LAMBDA  = Mathf.Log(2) / _GUN_HALF_LIFE_SECS;

        internal static tk2dSpriteAnimationClip _BulletSprite;

        private float _timeAtSpawn        = 0.0f;
        private float _timeAtLastRecalc   = 0.0f;
        private Coroutine _decayCoroutine = null;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.gunSwitchGroup                    = (ItemHelper.Get(Items.MarineSidearm) as Gun).gunSwitchGroup;
                gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.Automatic;
                gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
                gun.DefaultModule.numberOfShotsInClip = 10;
                gun.quality                           = PickupObject.ItemQuality.A;
                gun.barrelOffset.transform.localPosition = new Vector3(1.0625f, 1.25f, 0f); // should match "Casing" in JSON file
                gun.SetBaseMaxAmmo(_BASE_MAX_AMMO);
                gun.CurrentAmmo = _BASE_MAX_AMMO;
                gun.SetAnimationFPS(gun.reloadAnimation, 20);
                gun.SetAnimationFPS(gun.shootAnimation, 20);

            var comp = gun.gameObject.AddComponent<Alyx>();
                comp.SetFireAudio(); // prevent fire audio, as it's handled in OnPostFired()
                comp.SetReloadAudio("alyx_reload_sound");

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
                projectile.baseData.damage   = 15f;
                projectile.baseData.speed    = 20.0f;
                projectile.transform.parent  = gun.barrelOffset;

                projectile.healthEffect      = ItemHelper.Get(Items.IrradiatedLead).GetComponent<BulletStatusEffectItem>().HealthModifierEffect;
                projectile.AppliesPoison     = true;
                projectile.PoisonApplyChance = 1.0f;

                projectile.fireEffect        = ItemHelper.Get(Items.HotLead).GetComponent<BulletStatusEffectItem>().FireModifierEffect;
                projectile.AppliesFire       = true;
                projectile.FireApplyChance   = 1.0f;
        }

        public override void OnPostFired(PlayerController player, Gun gun)
        {
            base.OnPostFired(player, gun);
            AkSoundEngine.PostEvent("alyx_shoot_sound", gun.gameObject);
        }

        public override void Start()
        {
            base.Start();
            this._timeAtLastRecalc = BraveTime.ScaledTimeSinceStartup;
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
            if (this._decayCoroutine != null)
            {
                StopCoroutine(this._decayCoroutine);
                this._decayCoroutine = null;
            }
        }

        public override void OnSwitchedToThisGun()
        {
            base.OnSwitchedToThisGun();
            RecalculateAmmo();
            if (this._decayCoroutine != null)
            {
                StopCoroutine(this._decayCoroutine);
                this._decayCoroutine = null;
            }
        }

        public override void OnSwitchedAwayFromThisGun()
        {
            base.OnSwitchedAwayFromThisGun();
            if (this._decayCoroutine == null)
                this._decayCoroutine = this.Owner.StartCoroutine(DecayWhileInactive());
        }

        public override void OnDestroy()
        {
            if (this._decayCoroutine != null)
            {
                StopCoroutine(this._decayCoroutine);
                this._decayCoroutine = null;
            }
            base.OnDestroy();
        }

        private IEnumerator DecayWhileInactive()
        {
            while (this.gameObject != null)
            {
                if (!GameManager.Instance.IsPaused && !GameManager.Instance.IsLoadingLevel)
                    RecalculateAmmo();
                yield return null;
            }
        }

        internal static int ComputeDecayFromHalfLife(float startAmount, float halfLifeInSeconds, float timeElapsed)
        {
            return ComputeExponentialDecay(startAmount, Mathf.Log(2) / halfLifeInSeconds, timeElapsed);
        }

        internal static int ComputeExponentialDecay(float startAmount, float lambda, float timeElapsed)
        {
            return Lazy.RoundWeighted(startAmount * Mathf.Exp(-lambda * timeElapsed));
        }

        private void RecalculateAmmo()
        {
            float timeSinceLastRecalc = BraveTime.ScaledTimeSinceStartup - this._timeAtLastRecalc;
            if (timeSinceLastRecalc <= _MIN_CALC_RATE)
                return;
            this._timeAtLastRecalc = BraveTime.ScaledTimeSinceStartup;

            int newAmmo = ComputeExponentialDecay((float)this.gun.CurrentAmmo, _AMMO_DECAY_LAMBDA, timeSinceLastRecalc);
            int newMaxAmmo = ComputeExponentialDecay((float)this.gun.GetBaseMaxAmmo(), _GUN_DECAY_LAMBDA, timeSinceLastRecalc);

            // If we've decayed at all, create poison goop under our feet
            if (newAmmo < this.gun.CurrentAmmo || newMaxAmmo < this.gun.GetBaseMaxAmmo())
            {
                DeadlyDeadlyGoopManager poisonGooper = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.PoisonDef);
                if (this.Owner is PlayerController player)
                    poisonGooper.AddGoopCircle(player.sprite.WorldBottomCenter - player.m_currentGunAngle.ToVector(1f), 0.75f);
                else
                    poisonGooper.AddGoopCircle(this.gun.sprite.WorldCenter, 1f);
            }

            this.gun.CurrentAmmo = newAmmo;
            this.gun.SetBaseMaxAmmo(newMaxAmmo);

            if (newMaxAmmo <= _MIN_AMMO_TO_PERSIST)
            {
                if (this.Owner is PlayerController player)
                    player.inventory.DestroyGun(this.gun);
                else // vanish in a puff of smoke on the ground
                {
                    Lazy.DoSmokeAt(this.gun.sprite.WorldCenter);
                    UnityEngine.Object.Destroy(this.gun.gameObject);
                }
            }
        }
    }
}