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
    public class VacuumCleaner : AdvancedGunBehavior
    {
        public static string ItemName         = "Vacuum Cleaner";
        public static string SpriteName       = "vacuum_cleaner";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "Mean Cleaning Machine";
        public static string LongDescription  = "(:D)";

        internal static GameObject _VacuumVFX = null;

        internal const float _REACH     = 8.00f; // how far (in tiles) the gun reaches
        internal const float _SPREAD    =   10f; // width (in degrees) of how wide our cone of suction is at the end of our reach
        internal const float _BEG_WIDTH = 0.40f; // width (in tiles) of cone of suction at the beginning of the gun's muzzle
        internal const float _BEG_ACCEL = 0.05f; // speed (in tiles per frame) at which debris accelerates towards the gun near the muzzle
        internal const float _END_WIDTH = 1.00f; // width (in tiles) of cone of suction at the end of the gun's range
        internal const float _END_ACCEL = 0.03f; // speed (in tiles per frame) at which debris accelerates towards the gun near the end of the gun's reach

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.gunClass                          = GunClass.CHARGE;
                gun.gunSwitchGroup                    = (ItemHelper.Get(Items.MarineSidearm) as Gun).gunSwitchGroup;
                gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.Charged;
                gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
                gun.DefaultModule.numberOfShotsInClip = 16;
                gun.quality                           = PickupObject.ItemQuality.A;
                gun.InfiniteAmmo                      = true;
                gun.barrelOffset.transform.localPosition = new Vector3(1.8125f, 0.4375f, 0f); // should match "Casing" in JSON file

            var comp = gun.gameObject.AddComponent<VacuumCleaner>();
                comp.SetFireAudio(); // prevent fire audio, as it's handled in OnPostFired()

            gun.DefaultModule.chargeProjectiles = new(){ new(){
                Projectile = Lazy.PrefabProjectileFromGun(gun),
                ChargeTime = 999999f, // absurdly high value so we never actually shoot
            }};

            _VacuumVFX = VFX.animations["VacuumParticle"];
        }

        public override void OnPostFired(PlayerController player, Gun gun)
        {
            base.OnPostFired(player, gun);
            AkSoundEngine.PostEvent("alyx_shoot_sound", gun.gameObject);
        }

        protected override void Update()
        {
            base.Update();
            if (!this.gun.IsCharging)
                return;

            if (UnityEngine.Random.value < 0.66f)
            {
                Vector2 gunpos = this.gun.barrelOffset.position;
                float angleFromGun = this.gun.CurrentAngle + UnityEngine.Random.Range(-_SPREAD, _SPREAD);
                GameObject o = SpawnManager.SpawnVFX(_VacuumVFX, gunpos + angleFromGun.ToVector(_REACH), Lazy.RandomEulerZ());
                VacuumParticle v = o.AddComponent<VacuumParticle>();
                    v.Setup(this.gun);
            }
        }
    }

    // TODO: setting alpha on the first frame doesn't work, so we create a dummy sprite
    public class VacuumParticle : MonoBehaviour
    {
        private const float _MAX_LIFE = 1.0f;
        private const float _MIN_DIST_TO_VACUUM = 0.5f;
        private const float _MIN_ALPHA = 0.01f;
        private const float _MAX_ALPHA = 0.5f;
        private const float _DLT_ALPHA = 0.01f;

        private Gun _gun           = null;
        private tk2dSprite _sprite = null;
        private float _accel       = 0.0f;
        private Vector2 _velocity  = Vector2.zero;
        private float _lifetime    = 0.0f;
        private float _alpha       = _MIN_ALPHA;

        public void Setup(Gun g)
        {
            this._gun    = g;
            this._sprite = base.gameObject.GetComponent<tk2dSprite>();
            // this._sprite.renderer.material.color = this._sprite.renderer.material.color.WithAlpha(_MIN_ALPHA);
            // this._sprite.color = this._sprite.color.WithAlpha(_MIN_ALPHA);
        }

        private void Start()
        {
            // this._sprite = base.gameObject.GetComponent<tk2dSprite>();
            // this._sprite.renderer.material.color = this._sprite.renderer.material.color.WithAlpha(_MIN_ALPHA);
            // this._sprite.color = this._sprite.color.WithAlpha(_MIN_ALPHA);
        }

        private void LateUpdate()
        {
            this._alpha = Mathf.Min(this._alpha + _DLT_ALPHA, _MAX_ALPHA);
            this._sprite.renderer.SetAlpha(this._alpha);
            this._lifetime += BraveTime.DeltaTime;
            if (!this._gun || this._lifetime > _MAX_LIFE)
            {
                UnityEngine.GameObject.Destroy(base.gameObject);
                return;
            }
            // this._sprite.renderer.SetAlpha(0.03f);

            // if (!this._gun.IsCharging)
            //     return;

            Vector2 towardsVacuum = (this._gun.barrelOffset.position - this._sprite.transform.position);
            if (towardsVacuum.magnitude < _MIN_DIST_TO_VACUUM)
            {
                UnityEngine.GameObject.Destroy(base.gameObject);
                return;
            }

            // Compute our natural velocity from accelerating towards the vacuum
            Vector2 naturalVelocity = this._velocity + VacuumCleaner._END_ACCEL * towardsVacuum.normalized;
            // Compute a direct velocity from redirecting all of our momentum towards the vacuum
            Vector2 directVelocity  = this._velocity.magnitude * towardsVacuum.normalized;
            // Average the natural and direct velocity to make sure ouf particles get to the vacuum eventually
            this._velocity = (1 - 0.5f) * naturalVelocity + 0.5f * directVelocity;
            this.gameObject.transform.position += this._velocity.ToVector3ZUp(0f);
        }
    }
}
