﻿using System;
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
    public class Blackjack : AdvancedGunBehavior
    {
        public static string ItemName         = "Blackjack";
        public static string SpriteName       = "blackjack";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "Gambit's Queens";
        public static string LongDescription  = "(Stability and power both scale with accuracy. Hope you like 52 pickup.)";

        private const int _DECK_SIZE = 52; // does not include 2 jokers yet...need to finish up individual playing cards later
        private const int _CLIP_SIZE = 13; // 1 suit
        private const int _NUM_DECKS = 1;

        internal static tk2dSpriteAnimationClip _BulletSprite;
        internal static tk2dSpriteAnimationClip _BackSprite;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.gunSwitchGroup                    = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup;
                gun.DefaultModule.ammoCost            = 1;
                gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.Automatic;
                gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
                gun.reloadTime                        = 0.8f;
                gun.CanGainAmmo                       = false;
                gun.DefaultModule.angleVariance       = 30.0f;
                gun.DefaultModule.cooldownTime        = 0.15f;
                gun.DefaultModule.numberOfShotsInClip = _CLIP_SIZE;
                gun.quality                           = PickupObject.ItemQuality.C;
                gun.barrelOffset.transform.localPosition = new Vector3(1.625f, 1.375f, 0f); // should match "Casing" in JSON file
                gun.SetBaseMaxAmmo(_DECK_SIZE * _NUM_DECKS);
                gun.CurrentAmmo = _DECK_SIZE * _NUM_DECKS;
                gun.SetAnimationFPS(gun.shootAnimation, 30);
                gun.SetAnimationFPS(gun.reloadAnimation, 30);

            var comp = gun.gameObject.AddComponent<Blackjack>();
                comp.SetFireAudio(); // prevent fire audio, as it's handled in OnPostFired()
                comp.SetReloadAudio("card_shuffle_sound"); // todo: this is still playing the default reload sound as well, for some reason

            _BulletSprite = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("playing_card").Base(),
                0, true, new IntVector2(12, 8),
                false, tk2dBaseSprite.Anchor.MiddleLeft, true, true);

            _BackSprite = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("playing_card_back").Base(),
                0, true, new IntVector2(12, 8),
                false, tk2dBaseSprite.Anchor.MiddleLeft, true, true);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.AddAnimation(_BulletSprite);
                projectile.SetAnimation(_BulletSprite);

                projectile.baseData.damage = 7f;
                projectile.baseData.range  = 999f; // we implement a custom range-like behavior
                projectile.baseData.speed  = 18f;
                projectile.gameObject.AddComponent<ThrownCard>();
                projectile.transform.parent = gun.barrelOffset;
        }

        public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
        {
            if (projectile.GetComponent<ThrownCard>() is not ThrownCard tc)
                return projectile;
            if (gun.CurrentOwner is not PlayerController player)
                return projectile;

            tc.isAFreebie = (mod.ammoCost == 0 || gun.InfiniteAmmo || gun.LocalInfiniteAmmo || gun.CanGainAmmo || player.InfiniteAmmo.Value);
            return projectile;
        }
    }

    public class ThrownCard : MonoBehaviour
    {
        private const float _SPIN_SPEED = 2.0f;
        private const float _BASE_LIFE  = 0.25f;
        private const float _AIR_DRAG   = 0.93f;

        private Projectile       _projectile;
        private PlayerController _owner;
        private float            _lifetime         = 0.0f;
        private float            _distanceTraveled = 0.0f;
        private int              _cardFront        = 0;
        private int              _cardBack         = 0;
        private float            _timeAtMaxPower   = 0.0f;
        private bool             _faltering        = false;
        private float            _curveAmount      = 0.0f;
        private float            _startScale       = 1f;

        public bool isAFreebie = true; // false if we fired directly from the gun and it cost us ammo, true otherwise

        private void Start()
        {
            this._projectile = base.GetComponent<Projectile>();
            this._owner = this._projectile.Owner as PlayerController;

            this._projectile.OnDestruction += CreatePlayingCardPickup;

            // don't use an emissive / tinted shader so we can turn off the glowing yellow tint effect
            // this._projectile.sprite.usesOverrideMaterial = true; // keep this off so we still get nice lighting
            this._projectile.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitBlendUber");

            BounceProjModifier bounce = this._projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
                bounce.numberOfBounces     = 1;
                bounce.chanceToDieOnBounce = 0f;
                bounce.onlyBounceOffTiles  = false;
                bounce.OnBounce += () => {
                    this._faltering = true;
                    this._projectile.baseData.speed *= 0.4f;
                };

            CalculateStatsFromPlayerStats();

            this._cardFront  = Blackjack._BulletSprite.GetFrame(0).spriteId;
            this._cardBack   = Blackjack._BackSprite.GetFrame(0).spriteId;
            this._startScale = (Lazy.CoinFlip() ? -1f : 1f);

            AkSoundEngine.PostEvent("card_throw_sound_stop_all", this._projectile.gameObject);
            AkSoundEngine.PostEvent("card_throw_sound", this._projectile.gameObject);
        }

        private void CalculateStatsFromPlayerStats()
        {
            float acc            = this._owner.stats.GetStatModifier(PlayerStats.StatType.Accuracy);
            float inverseRootAcc = Mathf.Sqrt(1.0f / acc);
            this._timeAtMaxPower = _BASE_LIFE * inverseRootAcc * UnityEngine.Random.Range(0.8f, 1.2f);
            this._projectile.baseData.damage *= inverseRootAcc;
        }

        private void Update()
        {
            if (this._projectile.baseData.speed < 1f)
            {
                this._projectile.DieInAir(suppressInAirEffects: true);
                return;
            }

            if (BraveTime.DeltaTime == 0)
                return;

            float timeScale = BraveTime.DeltaTime * C.FPS;
            this._lifetime += BraveTime.DeltaTime;
            if (this._faltering || this._lifetime >= this._timeAtMaxPower)
            {
                if (!this._faltering)
                {
                    this._faltering = true;
                    this._curveAmount = (Lazy.CoinFlip() ? -1f : 1f) * 5f * UnityEngine.Random.value;
                }
                // this._projectile.baseData.speed *= _AIR_DRAG;
                this._projectile.baseData.speed *= Mathf.Pow(_AIR_DRAG, timeScale); // todo: see if this slows things down too much
                this._projectile.SendInDirection(
                    (this._projectile.m_currentDirection.ToAngle() + this._curveAmount * timeScale).ToVector(), true, true);
                this._projectile.UpdateSpeed();
            }

            this._distanceTraveled += BraveTime.DeltaTime * this._projectile.baseData.speed;
            float scale = this._startScale * Mathf.Cos(_SPIN_SPEED * this._distanceTraveled);
            this._projectile.sprite.scale = this._projectile.sprite.scale.WithY(scale);
            this._projectile.spriteAnimator.SetSprite(
                this._projectile.sprite.collection, scale > 0 ? this._cardFront : this._cardBack);
        }

        private void CreatePlayingCardPickup(Projectile p)
        {
            if (this.isAFreebie)
                return; // don't create free ammo from, e.g., scattershot

            MiniInteractable mi = MiniInteractable.CreateInteractableAtPosition(
              p.sprite, // correct transform w.r.t. MiddleLeft anchor
              p.sprite.transform.position + new Vector3(0.5f * 12f / C.PIXELS_PER_TILE, 0, 0),
              PickUpPlayingCardScript);
            mi.autoInteract = true;
            mi.transform.rotation = p.transform.rotation;
            // mi.sprite.usesOverrideMaterial = true;
            mi.sprite.renderer.material = p.sprite.renderer.material;
        }

        public IEnumerator PickUpPlayingCardScript(MiniInteractable i, PlayerController p)
        {
            if (p != this._owner)
            {
                i.interacting = false;
                yield break;
            }
            foreach (Gun gun in this._owner.inventory.AllGuns)
            {
                if (!gun.GetComponent<Blackjack>())
                    continue;
                if (gun.CurrentAmmo >= gun.AdjustedMaxAmmo)
                    break;
                gun.CurrentAmmo += 1;
                AkSoundEngine.PostEvent("card_pickup_sound_stop_all", p.gameObject);
                AkSoundEngine.PostEvent("card_pickup_sound", p.gameObject);
                SpawnManager.SpawnVFX(VFX.animations["MiniPickup"], i.sprite.WorldCenter, Lazy.RandomEulerZ());
                UnityEngine.Object.Destroy(i.gameObject);
                break;
            }
            i.interacting = false;
        }
    }
}