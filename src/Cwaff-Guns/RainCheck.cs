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
    public class RainCheck : AdvancedGunBehavior
    {
        public static string ItemName         = "Rain Check";
        public static string SpriteName       = "knife_gun";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "For a Rainy Day";
        public static string LongDescription  = "(Upon firing, bullets are delayed from moving until reloading, then move towards player. Switching away from this gun keeps bullets in stasis until switching back to this gun.)";

        internal static tk2dSpriteAnimationClip _KunaiSprite;

        private int _nextIndex = 0;
        private Vector2 _whereIsThePlayerLooking;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.gunSwitchGroup                       = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup;
                gun.DefaultModule.ammoCost               = 1;
                gun.DefaultModule.shootStyle             = ProjectileModule.ShootStyle.SemiAutomatic;
                gun.DefaultModule.sequenceStyle          = ProjectileModule.ProjectileSequenceStyle.Random;
                gun.reloadTime                           = 1.1f;
                gun.DefaultModule.cooldownTime           = 0.1f;
                gun.DefaultModule.numberOfShotsInClip    = 20;
                gun.quality                              = PickupObject.ItemQuality.D;
                gun.barrelOffset.transform.localPosition = new Vector3(2.4375f, 0.4375f, 0f); // should match "Casing" in JSON file
                gun.SetBaseMaxAmmo(250);
                gun.SetAnimationFPS(gun.shootAnimation, 24);

            var comp = gun.gameObject.AddComponent<RainCheck>();

            _KunaiSprite = AnimateBullet.CreateProjectileAnimation(
                new List<string> {
                    "kunai",
                }, 12, true, new IntVector2(16, 10),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);

            Projectile projectile       = Lazy.PrefabProjectileFromGun(gun);
                projectile.baseData.damage  = 5f;
                projectile.baseData.speed   = 20.0f;
                projectile.transform.parent = gun.barrelOffset;
                projectile.AddAnimation(_KunaiSprite);
                projectile.SetAnimation(_KunaiSprite);
                projectile.gameObject.AddComponent<RainCheckBullets>();
        }

        public override void OnReload(PlayerController player, Gun gun)
        {
            base.OnReload(player, gun);
            LaunchAllBullets();
        }

        public override void OnSwitchedToThisGun()
        {
            base.OnSwitchedToThisGun();
            LaunchAllBullets();
        }

        public override void OnSwitchedAwayFromThisGun()
        {
            base.OnSwitchedAwayFromThisGun();
            LaunchAllBullets();
        }

        public int GetNextIndex()
        {
            return ++this._nextIndex;
        }

        private void LaunchAllBullets()
        {
            foreach (Projectile projectile in StaticReferenceManager.AllProjectiles)
            {
                if (projectile.GetComponent<RainCheckBullets>() is not RainCheckBullets rcb)
                    continue;
                rcb.StartLaunchSequenceForPlayer(this.Player);
            }
            this._nextIndex = 0;
        }

        protected override void Update()
        {
            base.Update();
            if (this.Player is not PlayerController pc)
                return;

            this._whereIsThePlayerLooking =
                Raycast.ToNearestWallOrEnemyOrObject(pc.sprite.WorldCenter, pc.CurrentGun.CurrentAngle);
        }

        public Vector2 PointWherePlayerIsLooking()
        {
            return this._whereIsThePlayerLooking;
        }
    }

    public class RainCheckBullets : MonoBehaviour
    {
        private const float _TIME_BEFORE_STASIS     = 0.25f;
        private const float _GLOW_TIME              = 0.5f;
        private const float _GLOW_MAX               = 40f;
        private const float _RAINCHECK_LAUNCH_DELAY = 0.04f;

        private PlayerController _owner;
        private Projectile _projectile;
        private RainCheck _raincheck;
        private float _initialSpeed;
        private float _moveTimer;
        private bool _launchSequenceStarted;
        private bool _wasEverInStasis;
        private int _index;

        private void Start()
        {
            this._projectile            = base.GetComponent<Projectile>();
            this._owner                 = _projectile.Owner as PlayerController;
            this._raincheck             = this._owner.CurrentGun.GetComponent<RainCheck>();
            this._initialSpeed          = _projectile.baseData.speed;
            this._launchSequenceStarted = false;
            this._wasEverInStasis       = false;
            this._index                 = this._raincheck.GetNextIndex();

            StartCoroutine(TakeARainCheck());
        }

        private IEnumerator TakeARainCheck()
        {
            // Phase 1 / 5 -- the initial fire
            this._moveTimer = _TIME_BEFORE_STASIS;
            while (this._moveTimer > 0 && !this._launchSequenceStarted)
            {
                this._moveTimer -= BraveTime.DeltaTime;
                yield return null;
            }

            // Phase 2 / 5 -- the freeze
            this._projectile.baseData.speed = 0.01f;
            this._projectile.UpdateSpeed();
            this._wasEverInStasis = true;
            Vector2 pos = this._projectile.sprite.WorldCenter;
            Vector2 targetDir = Vector2.zero;
            while (true)
            {
                targetDir = this._raincheck.PointWherePlayerIsLooking() - pos;
                _projectile.SendInDirection(targetDir, true); // rotate the projectile
                if (this._launchSequenceStarted)
                    break; // awkward loop construct to make sure we set our targetDir at least once
                yield return null;
            }

            // Phase 3 / 5 -- the glow
            this._projectile.sprite.usesOverrideMaterial = true;
            Material m = this._projectile.sprite.renderer.material;
                m.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
                m.SetFloat("_EmissivePower", 0f);
                m.SetFloat("_EmissiveColorPower", 1.55f);
                m.SetColor("_EmissiveColor", Color.cyan);
            this._moveTimer = _GLOW_TIME;
            while (this._moveTimer > 0)
            {
                float glowAmount = (_GLOW_TIME - this._moveTimer) / _GLOW_TIME;
                m.SetFloat("_EmissivePower", glowAmount * _GLOW_MAX);
                this._moveTimer -= BraveTime.DeltaTime;
                yield return null;
            }

            // Phase 4 / 5 -- the launch queue
            this._moveTimer = _RAINCHECK_LAUNCH_DELAY * this._index;
            while (this._moveTimer > 0)
            {
                this._moveTimer -= BraveTime.DeltaTime;
                yield return null;
            }

            // Phase 5 / 5 -- the launch
            this._projectile.baseData.speed = this._initialSpeed;
            _projectile.SendInDirection(targetDir, true);
            _projectile.UpdateSpeed();

            yield break;
        }

        public void StartLaunchSequenceForPlayer(PlayerController pc)
        {
            if (pc != this._owner)
                return;

            this._launchSequenceStarted = true;
        }
    }
}
