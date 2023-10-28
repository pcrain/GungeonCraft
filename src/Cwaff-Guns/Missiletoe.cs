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
    public class Missiletoe : AdvancedGunBehavior
    {
        public static string ItemName         = "Missiletoe";
        public static string SpriteName       = "missiletoe";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "TBD";
        public static string LongDescription  = "TBD";

        internal static GameObject _WrapVFXS;
        internal static GameObject _WrapVFXA;
        internal static GameObject _WrapVFXB;
        internal static GameObject _WrapVFXC;
        internal static GameObject _WrapVFXD;

        internal static GameObject _UnwrapVFXS;
        internal static GameObject _UnwrapVFXA;
        internal static GameObject _UnwrapVFXB;
        internal static GameObject _UnwrapVFXC;
        internal static GameObject _UnwrapVFXD;

        internal static List<PickupObject> _WrappedGifts = new();
        internal static List<PickupObject.ItemQuality> _WrappedQualities = new();
        internal static List<PickupObject.ItemQuality> _ShuffledQualities = new();
        internal static float _WrapAnimLength;

        internal static Projectile _GiftProjectileS;
        internal static Projectile _GiftProjectileA;
        internal static Projectile _GiftProjectileB;
        internal static Projectile _GiftProjectileC;
        internal static Projectile _GiftProjectileD;

        private const int _WRAP_FPS = 16;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun<Missiletoe>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.SetAttributes(quality: PickupObject.ItemQuality.B, gunClass: GunClass.CHARM, reloadTime: 1f, ammo: 80, canReloadNoMatterAmmo: true);
                gun.SetAnimationFPS(gun.shootAnimation, 30);
                gun.SetAnimationFPS(gun.reloadAnimation, 40);
                gun.SetFireAudio("blowgun_fire_sound");
                gun.SetReloadAudio("blowgun_reload_sound");

            ProjectileModule mod = gun.DefaultModule;
                mod.ammoCost            = 1;
                mod.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime        = 0.33f;
                mod.numberOfShotsInClip = 1;

            _WrapVFXS   = SetupVFX("black_gift_wrap");
            _WrapVFXA   = SetupVFX("red_gift_wrap");
            _WrapVFXB   = SetupVFX("green_gift_wrap");
            _WrapVFXC   = SetupVFX("blue_gift_wrap");
            _WrapVFXD   = SetupVFX("brown_gift_wrap");

            _UnwrapVFXS = SetupVFX("black_gift_unwrap");
            _UnwrapVFXA = SetupVFX("red_gift_unwrap");
            _UnwrapVFXB = SetupVFX("green_gift_unwrap");
            _UnwrapVFXC = SetupVFX("blue_gift_unwrap");
            _UnwrapVFXD = SetupVFX("brown_gift_unwrap");

            _WrapAnimLength = _WrapVFXB.GetComponent<tk2dSpriteAnimator>().DefaultClip.BaseClipLength;

            _GiftProjectileS = SetupProjectile(gun: gun, name: "gift_projectile_black");
            _GiftProjectileA = SetupProjectile(gun: gun, name: "gift_projectile_red");
            _GiftProjectileB = SetupProjectile(gun: gun, name: "gift_projectile_green");
            _GiftProjectileC = SetupProjectile(gun: gun, name: "gift_projectile_blue");
            _GiftProjectileD = SetupProjectile(gun: gun, name: "gift_projectile_brown");
        }

        private static GameObject SetupVFX(string name)
        {
            return VFX.RegisterVFXObject($"VFX_{name}", ResMap.Get(name), _WRAP_FPS,
                loops: false, anchor: tk2dBaseSprite.Anchor.LowerCenter, scale: 0.75f, persist: true);
        }

        private static Projectile SetupProjectile(Gun gun, string name)
        {
            tk2dSpriteAnimationClip clip = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get(name).Base(),
                1, true, new IntVector2(14, 12),
                false, tk2dBaseSprite.Anchor.MiddleLeft, true, true);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun, setGunDefaultProjectile: false);
                projectile.AddDefaultAnimation(clip);
                projectile.transform.parent = gun.barrelOffset;

            return projectile;
        }

        public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
        {
            switch (_ShuffledQualities[mod.numberOfShotsInClip - gun.ClipShotsRemaining])
            {
                case PickupObject.ItemQuality.S: return _GiftProjectileS;
                case PickupObject.ItemQuality.A: return _GiftProjectileA;
                case PickupObject.ItemQuality.B: return _GiftProjectileB;
                case PickupObject.ItemQuality.C: return _GiftProjectileC;
                case PickupObject.ItemQuality.D: return _GiftProjectileD;
                default                        : return _GiftProjectileD;
            }
        }

        public static GameObject GetGiftVFX(PickupObject.ItemQuality quality, bool wrap)
        {
            switch (quality)
            {
                case PickupObject.ItemQuality.S: return wrap ? _WrapVFXS : _UnwrapVFXS;
                case PickupObject.ItemQuality.A: return wrap ? _WrapVFXA : _UnwrapVFXA;
                case PickupObject.ItemQuality.B: return wrap ? _WrapVFXB : _UnwrapVFXB;
                case PickupObject.ItemQuality.C: return wrap ? _WrapVFXC : _UnwrapVFXC;
                case PickupObject.ItemQuality.D: return wrap ? _WrapVFXD : _UnwrapVFXD;
                default                        : return wrap ? _WrapVFXD : _UnwrapVFXD;
            }
        }

        protected override void OnPickup(GameActor owner)
        {
            if (!this.everPickedUpByPlayer)
            {
                _WrappedGifts.Clear();
                _WrappedQualities.Clear();
                _ShuffledQualities.Clear();
            }
            RecalculateClip();
            base.OnPickup(owner);
        }

        public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
        {
            if (manualReload && gun.DefaultModule.numberOfShotsInClip == gun.ClipShotsRemaining)
                WrapPresent();
            else
                RecalculateClip();
            base.OnReloadPressed(player, gun, manualReload);
        }

        private const float _MAX_DIST = 5f;
        private void WrapPresent()
        {
            PickupObject nearestPickup = null;
            float nearestDist = _MAX_DIST;
            foreach (DebrisObject debris in StaticReferenceManager.AllDebris)
            {
                if (!debris.IsPickupObject)
                    continue;
                if (debris.GetComponentInChildren<PickupObject>() is not PickupObject pickup)
                    continue;
                if (pickup.IsBeingSold)
                    continue;

                float pickupDist = (debris.sprite.WorldCenter - this.Owner.sprite.WorldCenter).magnitude;
                if (pickupDist >= nearestDist)
                    continue;

                nearestPickup = pickup;
                nearestDist   = pickupDist;
            }
            if (!nearestPickup)
            {
                UnwrapPresent();
                return;
            }

            WrappableGift.Spawn(this, this.gun.barrelOffset.position, nearestPickup, unwrapping: false);
        }

        internal void RecalculateClip()
        {
            _WrappedQualities.Add(PickupObject.ItemQuality.D);  // make sure our list has at least one item
            _ShuffledQualities = _WrappedQualities.CopyAndShuffle();
            _WrappedQualities.Pop();
            this.gun.DefaultModule.numberOfShotsInClip = _ShuffledQualities.Count();
        }

        private void UnwrapPresent()
        {
            if (_WrappedGifts.Count() == 0)
                return;
            PickupObject gift = _WrappedGifts.Pop();
            _WrappedQualities.Pop();
            RecalculateClip();
            WrappableGift.Spawn(this, this.gun.barrelOffset.position, gift, unwrapping: true);
        }
    }

    public class WrappableGift : MonoBehaviour
    {
        private const float _GROW_TIME      = 0.5f; // amount of time it takes for our present to grow in
        private const float _MIN_SCALE      = 0.4f; // minimum scale our pickup can shrink down to
        private const float _VANISH_PERCENT = 0.5f; // percent of the way through the wrap animation the pickup should vanish

        private static readonly Vector2 _EXTRA_OFFSET = new Vector2(0f, 0.75f); // make the pickup enter near the center of the present

        private FancyVFX _vfx;
        private tk2dBaseSprite _sprite;
        private tk2dSpriteAnimator _animator;
        private Vector3 _position;
        private PickupObject _pickup;
        private Missiletoe _gun;

        public static WrappableGift Spawn(Missiletoe gun, Vector3 position, PickupObject pickup, bool unwrapping)
        {
            GameObject go = UnityEngine.Object.Instantiate(new GameObject(), position, Quaternion.identity);
            WrappableGift gift = go.AddComponent<WrappableGift>();
            gift.Setup(gun, position, pickup, unwrapping);
            return gift;
        }

        public void Setup(Missiletoe gun, Vector3 position, PickupObject pickup, bool unwrapping)
        {
            this._gun      = gun;
            this._position = position;
            this._pickup   = pickup;
            this._vfx      = FancyVFX.Spawn(Missiletoe.GetGiftVFX(pickup.quality, !unwrapping), this._position, Quaternion.identity,
                velocity: Vector2.zero, lifetime: Missiletoe._WrapAnimLength + 0.5f, fadeOutTime: 0.25f);
            this._sprite   = this._vfx.sprite;
            this._animator = this._sprite.spriteAnimator;

            StartCoroutine(WrapItUp(unwrapping));
        }

        private IEnumerator WrapItUp(bool unwrapping)
        {
            // Set up some useful variables
            bool wrapping          = !unwrapping;
            bool isGun             = this._pickup.GetComponent<Gun>() is Gun gun;
            Vector2 targetPosition = this._position.XY() + _EXTRA_OFFSET;
            float animLength       = Missiletoe._WrapAnimLength;

            // Create a VFX object for the pickup
            FancyVFX pickupvfx = null;
            if (wrapping)
                pickupvfx = FancyVFX.FromCurrentFrame(this._pickup.sprite);

            // Clone and destroy the pickup itself
            if (wrapping)
            {
                Missiletoe._WrappedQualities.Add(this._pickup.quality);
                if (isGun)
                {
                    Missiletoe._WrappedGifts.Add(UnityEngine.Object.Instantiate(this._pickup));
                    if (this._pickup.transform.parent != null)
                        UnityEngine.Object.Destroy(this._pickup.transform.parent?.gameObject);
                    else
                        UnityEngine.Object.Destroy(this._pickup);
                }
                else if (this._pickup.GetComponent<PlayerItem>() is PlayerItem active)
                {
                    Missiletoe._WrappedGifts.Add(this._pickup);
                    SpriteOutlineManager.RemoveOutlineFromSprite(active.sprite, true);
                    this._pickup.renderer.enabled = false;
                    UnityEngine.Object.Destroy(this._pickup.gameObject.GetComponent<DebrisObject>());
                }
                else if (this._pickup.GetComponent<PassiveItem>() is PassiveItem passive)
                {
                    Missiletoe._WrappedGifts.Add(this._pickup);
                    SpriteOutlineManager.RemoveOutlineFromSprite(passive.sprite, true);
                    this._pickup.renderer.enabled = false;
                    UnityEngine.Object.Destroy(this._pickup.gameObject.GetComponent<DebrisObject>());
                }
                this._gun.RecalculateClip();
            }

            // Pause the gift's default animation and let it grow into existence first
            this._vfx.gameObject.SetAlphaImmediate(0f); // make sure we start invisible to avoid first-frame glitches
            yield return null;
            this._vfx.gameObject.SetAlpha(1f);
            this._animator.StopAndResetFrame();
            for (float elapsed = 0f; elapsed < _GROW_TIME; elapsed += BraveTime.DeltaTime)
            {
                float percentDone = elapsed / _GROW_TIME;
                this._sprite.transform.localScale = new Vector3(percentDone, percentDone, 1.0f);
                yield return null;
            }
            this._animator.Play();

            // Make it magically hover over to the present
            if (unwrapping)// Wait for the appropriate point in the animation, then drop the original pickup
            {
                yield return new WaitForSeconds(animLength * (1f - _VANISH_PERCENT));
                Vector2 trueTarget = targetPosition - this._pickup.sprite.GetRelativePositionFromAnchor(tk2dBaseSprite.Anchor.LowerCenter);
                if (isGun)
                    trueTarget += _EXTRA_OFFSET; // guns are weirdly offset for some reason
                LootEngine.DropItemWithoutInstantiating(this._pickup.gameObject, trueTarget, Vector2.zero, 0f, true, false, true);
                yield break;
            }

            // Setup the VFX object for the pickup
            pickupvfx.Setup(velocity: Vector2.zero, lifetime: animLength * _VANISH_PERCENT, fadeOutTime: animLength * _VANISH_PERCENT, fadeIn: unwrapping);

            // Suck the pickup into the present and wait for the animation to play out
            Vector2 startPosition  = unwrapping ? targetPosition : pickupvfx.sprite.WorldCenter;
            float loopLength = animLength * _VANISH_PERCENT;
            for (float elapsed = 0f; elapsed < loopLength; elapsed += BraveTime.DeltaTime)
            {
                if (!pickupvfx)
                    break;

                float percentDone = Mathf.Clamp01(elapsed / loopLength);
                float cubicLerp = Ease.OutCubic(percentDone);
                Vector2 extraOffset = new Vector2(0f, 2f * Mathf.Sin(Mathf.PI * cubicLerp));
                Vector2 curPosition = extraOffset + Vector2.Lerp(startPosition, targetPosition, cubicLerp);
                float scale = ((1f - _MIN_SCALE) * cubicLerp);
                if (wrapping)
                    scale = 1f - scale;
                pickupvfx.sprite.transform.localScale = new Vector3(scale, scale, 1f);
                pickupvfx.sprite.PlaceAtScaledPositionByAnchor(curPosition, tk2dBaseSprite.Anchor.MiddleCenter);
                yield return null;
            }
        }
    }
}
