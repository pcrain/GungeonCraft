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
    public class IronMaid : AdvancedGunBehavior
    {
        public static string ItemName         = "Iron Maid";
        public static string SpriteName       = "iron_maid";
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
                gun.reloadTime                           = 0.75f;
                gun.DefaultModule.cooldownTime           = 0.1f;
                gun.DefaultModule.numberOfShotsInClip    = 20;
                gun.quality                              = PickupObject.ItemQuality.D;
                gun.barrelOffset.transform.localPosition = new Vector3(1.875f, 0.375f, 0f); // should match "Casing" in JSON file
                gun.SetBaseMaxAmmo(400);
                gun.SetAnimationFPS(gun.shootAnimation, 24);
                gun.SetAnimationFPS(gun.reloadAnimation, 24);

            var comp = gun.gameObject.AddComponent<IronMaid>();
                // comp.SetFireAudio("knife_gun_fire");
                comp.SetFireAudio("knife_gun_launch");
                comp.SetReloadAudio("knife_gun_reload");

            _KunaiSprite = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("kunai").Base(),
                12, true, new IntVector2(16, 10),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.baseData.damage  = 5f;
                projectile.baseData.speed   = 40.0f;
                projectile.transform.parent = gun.barrelOffset;
                projectile.AddAnimation(_KunaiSprite);
                projectile.SetAnimation(_KunaiSprite);
                projectile.gameObject.AddComponent<RainCheckBullets>();
        }

        public override void OnReload(PlayerController player, Gun gun)
        {
            base.OnReload(player, gun);
            LaunchAllBullets(player);
        }

        public override void OnSwitchedToThisGun()
        {
            base.OnSwitchedToThisGun();
            LaunchAllBullets(this.Player);
        }


        public override void OnDropped()
        {
            base.OnDropped();
            LaunchAllBullets(this.Player);
            this.Player.OnReceivedDamage -= LaunchAllBullets;
        }

        public override void OnSwitchedAwayFromThisGun()
        {
            base.OnSwitchedAwayFromThisGun();
            LaunchAllBullets(this.Player);
        }

        public int GetNextIndex()
        {
            return ++this._nextIndex;
        }

        private void LaunchAllBullets(GameActor player)
        {
            if (player is PlayerController pc)
                LaunchAllBullets(pc);
        }

        private void LaunchAllBullets(PlayerController pc)
        {
            foreach (Projectile projectile in StaticReferenceManager.AllProjectiles)
            {
                if (projectile.GetComponent<RainCheckBullets>() is not RainCheckBullets rcb)
                    continue;
                rcb.StartLaunchSequenceForPlayer(pc);
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
        private const float _TIME_BEFORE_STASIS     = 0.2f;
        private const float _GLOW_TIME              = 0.5f;
        private const float _GLOW_MAX               = 10f;
        private const float _LAUNCH_DELAY           = 0.04f;
        private const float _LAUNCH_SPEED           = 50f;

        private PlayerController _owner;
        private Projectile       _projectile;
        private IronMaid        _raincheck;
        private float            _moveTimer;
        private bool             _launchSequenceStarted;
        private bool             _wasEverInStasis;
        private int              _index;

        private void Start()
        {
            this._projectile            = base.GetComponent<Projectile>();
            this._owner                 = _projectile.Owner as PlayerController;
            this._raincheck             = this._owner.CurrentGun.GetComponent<IronMaid>();
            this._launchSequenceStarted = false;
            this._wasEverInStasis       = false;
            this._index                 = this._raincheck.GetNextIndex();

            this._projectile.specRigidbody.OnCollision += (_) => {
                AkSoundEngine.PostEvent("knife_gun_hit", this._projectile.gameObject);
            };

            StartCoroutine(TakeARainCheck());
        }

        private IEnumerator TakeARainCheck()
        {
            // Phase 1 / 5 -- the initial fire
            this._moveTimer = _TIME_BEFORE_STASIS;
            float decel = this._projectile.baseData.speed / (C.FPS * _TIME_BEFORE_STASIS);
            while (this._moveTimer > 0 && !this._launchSequenceStarted)
            {
                this._moveTimer -= BraveTime.DeltaTime;
                yield return null;
                this._projectile.baseData.speed -= decel;
                this._projectile.UpdateSpeed();
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
            AkSoundEngine.PostEvent("knife_gun_glow_stop_all", this._projectile.gameObject);
            AkSoundEngine.PostEvent("knife_gun_glow", this._projectile.gameObject);
            this._moveTimer = _GLOW_TIME;
            while (this._moveTimer > 0)
            {
                float glowAmount = (_GLOW_TIME - this._moveTimer) / _GLOW_TIME;
                m.SetFloat("_EmissivePower", glowAmount * _GLOW_MAX);
                this._moveTimer -= BraveTime.DeltaTime;
                yield return null;
            }

            // Phase 4 / 5 -- the launch queue
            this._moveTimer = _LAUNCH_DELAY * this._index;
            while (this._moveTimer > 0)
            {
                this._moveTimer -= BraveTime.DeltaTime;
                yield return null;
            }

            // Phase 5 / 5 -- the launch
            this._projectile.baseData.speed = _LAUNCH_SPEED;
            _projectile.SendInDirection(targetDir, true);
            _projectile.UpdateSpeed();
            AkSoundEngine.PostEvent("knife_gun_launch", this._projectile.gameObject);

            yield break;
        }

        public void StartLaunchSequenceForPlayer(PlayerController pc)
        {
            if (pc != this._owner)
                return; // don't launch projectiles that don't belong to us

            this._launchSequenceStarted = true;
        }
    }
}
