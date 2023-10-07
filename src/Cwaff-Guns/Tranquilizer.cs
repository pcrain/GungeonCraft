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
    public class Tranquilizer : AdvancedGunBehavior
    {
        public static string ItemName         = "Tranquilizer";
        public static string SpriteName       = "tranquilizer";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "Zzzzzz";
        public static string LongDescription  = "Fires projectiles that permastun enemies after a few seconds, scaling logarithmically with their current health.\n\nMost commonly used for sedating loudly-opinionated supermarket shoppers and other similarly aggressive wild animals, the tranquilizer gun is the pinnacle of non-lethal firearm technology. What it lacks in visual spectacle or firepower it more than makes up for with raw practicality, able to completely pacify all but the mightiest of the Gungeon's denizens with a single shot and a few seconds of your time. As long as you have a plan in place for not getting shot for those few precious seconds, it's hard to beat in terms of ammo-efficiency for dispatching the Gundead.";

        internal static GameObject _DrowsyVFX = null;
        internal static tk2dSpriteAnimationClip _BulletSprite;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.gunSwitchGroup                    = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup;
                gun.DefaultModule.ammoCost            = 1;
                gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
                gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
                gun.reloadTime                        = 1.2f;
                gun.DefaultModule.cooldownTime        = 0.1f;
                gun.DefaultModule.numberOfShotsInClip = 1;
                gun.quality                           = PickupObject.ItemQuality.C;
                gun.barrelOffset.transform.localPosition = new Vector3(1.75f, 0.25f, 0f); // should match "Casing" in JSON file
                gun.SetBaseMaxAmmo(60);
                gun.SetAnimationFPS(gun.shootAnimation, 30);
                gun.SetAnimationFPS(gun.reloadAnimation, 40);

            var comp = gun.gameObject.AddComponent<Tranquilizer>();
                comp.SetFireAudio("blowgun_fire_sound");
                comp.SetReloadAudio("blowgun_reload_sound");

            _BulletSprite = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("tranquilizer_projectile").Base(),
                12, true, new IntVector2(13, 9),
                false, tk2dBaseSprite.Anchor.MiddleLeft, true, true);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.AddAnimation(_BulletSprite);
                projectile.SetAnimation(_BulletSprite);
                projectile.transform.parent = gun.barrelOffset;
                projectile.gameObject.AddComponent<TranquilizerBehavior>();

            _DrowsyVFX = VFX.animations["DrowsyParticle"];
        }

    }

    public class TranquilizerBehavior : MonoBehaviour
    {
        private const int _STUN_DELAY = 10;
        private const int _STUN_TIME  = 3600; // one hour

        private void Start()
        {
            base.GetComponent<Projectile>().OnHitEnemy += (Projectile _, SpeculativeRigidbody enemy, bool _) => {
                if (enemy.aiActor?.IsAliveAndNotABoss() ?? false)
                    enemy.aiActor.gameObject.GetOrAddComponent<EnemyTranquilizedBehavior>();
            };
        }

        private class EnemyTranquilizedBehavior : MonoBehaviour
        {
            private AIActor _enemy = null;
            private OrbitalEffect _orb = null;

            private void Start()
            {
                this._enemy = base.GetComponent<AIActor>();
                if ((this._enemy?.healthHaver?.currentHealth ?? 0) <= 0)
                    return;

                this._orb = this._enemy.gameObject.AddComponent<OrbitalEffect>();
                    this._orb.SetupOrbitals(vfx: Tranquilizer._DrowsyVFX, numOrbitals: 1, rps: 0.2f, isEmissive: false, isOverhead: true);

                AkSoundEngine.PostEvent("drowsy_sound", this._enemy.gameObject);
                Invoke("Permastun", Mathf.Max(1, Mathf.CeilToInt(Mathf.Log(this._enemy.healthHaver.currentHealth) / Mathf.Log(2))));
            }

            private void Permastun()
            {
                this._enemy.behaviorSpeculator?.Stun(_STUN_TIME, createVFX: false);
                this._enemy.IgnoreForRoomClear         = true;
                this._enemy.CollisionDamage            = 0f;
                this._enemy.CollisionKnockbackStrength = 0f;

                this._orb.AddOrbital(vfx: Tranquilizer._DrowsyVFX);
                this._orb.AddOrbital(vfx: Tranquilizer._DrowsyVFX);
            }
        }
    }

}
