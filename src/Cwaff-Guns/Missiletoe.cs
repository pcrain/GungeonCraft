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

        internal static tk2dSpriteAnimationClip _BulletSprite;
        internal static GameObject _WrapVFX;
        internal static GameObject _UnwrapVFX;
        internal static GameObject _StaticVFX;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun<Missiletoe>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.SetAttributes(quality: PickupObject.ItemQuality.B, gunClass: GunClass.CHARM, reloadTime: 1.2f, ammo: 80, canReloadNoMatterAmmo: true);
                gun.SetAnimationFPS(gun.shootAnimation, 30);
                gun.SetAnimationFPS(gun.reloadAnimation, 40);
                gun.SetFireAudio("blowgun_fire_sound");
                gun.SetReloadAudio("blowgun_reload_sound");

            ProjectileModule mod = gun.DefaultModule;
                mod.ammoCost            = 1;
                mod.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime        = 0.1f;
                mod.numberOfShotsInClip = 10;

            _BulletSprite = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("blue_gift_projectile").Base(),
                1, true, new IntVector2(9, 8),
                false, tk2dBaseSprite.Anchor.MiddleLeft, true, true);

            int fps = 10;
            _WrapVFX = VFX.RegisterVFXObject("Wrap", ResMap.Get("blue_gift_wrap"), fps,
                loops: false, anchor: tk2dBaseSprite.Anchor.LowerCenter, scale: 0.75f);

            _UnwrapVFX = VFX.RegisterVFXObject("Unwrap", ResMap.Get("blue_gift_unwrap"), fps,
                loops: false, anchor: tk2dBaseSprite.Anchor.LowerCenter, scale: 0.75f);

            _StaticVFX = VFX.RegisterVFXObject("Gift", ResMap.Get("blue_gift_static"), fps,
                loops: true, anchor: tk2dBaseSprite.Anchor.LowerCenter, scale: 1.0f);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.AddDefaultAnimation(_BulletSprite);
                projectile.transform.parent = gun.barrelOffset;
                projectile.gameObject.AddComponent<TranquilizerBehavior>();
        }

        public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
        {
            base.OnReloadPressed(player, gun, manualReload);
            WrapPresent();
        }

        private void WrapPresent()
        {
            PickupObject nearestPickup = null;
            float nearestDist = float.MaxValue;
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
                return;

            WrappableGift.Spawn(this.gun.barrelOffset.position, nearestPickup);
        }
    }

    public class WrappableGift : MonoBehaviour
    {
        private FancyVFX _vfx;
        private tk2dBaseSprite _sprite;
        private tk2dSpriteAnimator _animator;
        private Vector3 _position;
        private PickupObject _pickup;

        public static WrappableGift Spawn(Vector3 position, PickupObject pickup)
        {
            GameObject go = UnityEngine.Object.Instantiate(new GameObject(), position, Quaternion.identity);
            WrappableGift gift = go.AddComponent<WrappableGift>();
            gift.Setup(position, pickup);
            return gift;
        }

        public void Setup(Vector3 position, PickupObject pickup)
        {
            this._position = position;
            this._pickup = pickup;
            this._vfx = FancyVFX.SpawnUnpooled(Missiletoe._WrapVFX, this._position, Quaternion.identity,
                velocity: Vector2.zero, lifetime: 2f, fadeOutTime: 0.25f);
            this._sprite = this._vfx.sprite;
            this._animator = this._sprite.spriteAnimator;
            StartCoroutine(WrapItUp());
        }

        // private void OnDestroy()
        // {
        //     ETGModConsole.Log($"  ded");
        //     MiniInteractable mi = MiniInteractable.CreateInteractableAtPosition(
        //       this._sprite,
        //       this._sprite.WorldBottomCenter,
        //       GiftInteractScript);
        // }

        private const float _MIN_SCALE          = 0.4f;
        private const float _VANISH_PERCENT     = 0.5f; // percent of the way through the wrap animation the pickup should vanish
        private const float _INV_VANISH_PERCENT = 1f / _VANISH_PERCENT;

        private IEnumerator WrapItUp()
        {
            // Create a VFX object for the pickup
            float animLength = this._animator.DefaultClip.BaseClipLength;
            FancyVFX pickupvfx = FancyVFX.FromCurrentFrame(this._pickup.sprite);
                pickupvfx.Setup(velocity: Vector2.zero, lifetime: animLength * _VANISH_PERCENT, fadeOutTime: animLength * _VANISH_PERCENT);

            // Make it magically hover over to the present
            Vector2 startPosition = pickupvfx.sprite.WorldCenter;
            Vector2 targetPosition   = this._position.XY() + new Vector2(0f, 0.75f);
            for (float elapsed = 0f; elapsed < animLength; elapsed += BraveTime.DeltaTime)
            {
                float percentDone = Mathf.Clamp01(_INV_VANISH_PERCENT * elapsed / animLength);
                float cubicLerp = Ease.OutCubic(percentDone);

                Vector2 extraOffset = new Vector2(0f, 2f * Mathf.Sin(Mathf.PI * cubicLerp));
                Vector2 curPosition = extraOffset + Vector2.Lerp(startPosition, targetPosition, cubicLerp);
                float scale = 1f - ((1f - _MIN_SCALE) * cubicLerp);
                if (pickupvfx)
                {
                    pickupvfx.sprite.transform.localScale = new Vector3(scale, scale, 1f);
                    pickupvfx.sprite.PlaceAtScaledPositionByAnchor(curPosition, tk2dBaseSprite.Anchor.MiddleCenter);
                }
                yield return null;
            }
            // pickupvfx.sprite.PlaceAtScaledPositionByAnchor(targetPosition, tk2dBaseSprite.Anchor.MiddleCenter);

            // Create the mini interactible
            tk2dSpriteAnimationClip clip = Missiletoe._StaticVFX.GetComponent<tk2dSpriteAnimator>().DefaultClip;
            tk2dSpriteAnimationFrame frame = clip.GetFrame(clip.frames.Length - 1);
            MiniInteractable mi = MiniInteractable.CreateInteractableAtPosition(
              collection: frame.spriteCollection, spriteId: frame.spriteId, position: this._position, iscript: GiftInteractScript);
            mi.GetComponent<tk2dSprite>().PlaceAtPositionByAnchor(this._position, tk2dBaseSprite.Anchor.LowerCenter);
            yield break;
        }

        private static IEnumerator GiftInteractScript(MiniInteractable i, PlayerController p)
        {
            yield break;
        }
    }
}
