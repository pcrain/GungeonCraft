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
    public class QuarterPounder : AdvancedGunBehavior
    {
        public static string ItemName         = "Quarter Pounder";
        public static string SpriteName       = "quarter_pounder";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "Pay Per Pew";
        public static string LongDescription  = "(shoots money O:)";

        internal static tk2dSpriteAnimationClip _ProjSprite;
        internal static GameObject _MidasParticleVFX;

        private int _lastMoney = -1;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.gunSwitchGroup                    = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup;
                gun.DefaultModule.ammoCost            = 1;
                gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
                gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
                gun.DefaultModule.numberOfShotsInClip = 10;
                gun.DefaultModule.angleVariance       = 15.0f;
                gun.reloadTime                        = 1.1f;
                gun.quality                           = PickupObject.ItemQuality.D;
                gun.barrelOffset.transform.localPosition = new Vector3(1.8125f, 0.5f, 0f); // should match "Casing" in JSON file
                gun.CanGainAmmo                       = false;
                gun.SetBaseMaxAmmo(9999);
                gun.SetAnimationFPS(gun.shootAnimation, 24);
                gun.SetAnimationFPS(gun.reloadAnimation, 16);

            var comp = gun.gameObject.AddComponent<QuarterPounder>();
                comp.SetFireAudio("fire_coin_sound");
                comp.SetReloadAudio("coin_gun_reload");

            _ProjSprite = AnimateBullet.CreateProjectileAnimation(
                new List<string> {
                    "coin-gun-projectile1",
                    "coin-gun-projectile2",
                    "coin-gun-projectile3",
                    "coin-gun-projectile4",
                    "coin-gun-projectile5",
                    "coin-gun-projectile6",
                    "coin-gun-projectile7",
                    "coin-gun-projectile8",
                    "coin-gun-projectile9",
                    "coin-gun-projectile10",
                }, 2, true, new IntVector2(9, 6),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.baseData.speed   = 44.0f;
                projectile.baseData.damage  = 20f;
                projectile.transform.parent = gun.barrelOffset;
                projectile.AddAnimation(_ProjSprite);
                projectile.SetAnimation(_ProjSprite);
                projectile.gameObject.AddComponent<MidasProjectile>();

            _MidasParticleVFX = VFX.animations["MidasParticle"];
        }

        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            base.OnPickedUpByPlayer(player);
            AdjustAmmoToMoney();
        }

        public override void OnSwitchedToThisGun()
        {
            base.OnSwitchedToThisGun();
            AdjustAmmoToMoney();
        }

        protected override void NonCurrentGunUpdate()
        {
            base.NonCurrentGunUpdate();
            AdjustAmmoToMoney();
        }

        protected override void Update()
        {
            base.Update();
            AdjustAmmoToMoney();
        }

        public override void OnPostFired(PlayerController player, Gun gun)
        {
            base.OnPostFired(player, gun);
            --GameManager.Instance.PrimaryPlayer.carriedConsumables.Currency;
            AdjustAmmoToMoney();
        }

        private void AdjustAmmoToMoney()
        {
            int money = GameManager.Instance.PrimaryPlayer.carriedConsumables.Currency;
            if (money == this._lastMoney)
                return;
            this.gun.CurrentAmmo = money;
        }
    }

    public class MidasProjectile : MonoBehaviour
    {
        private void Start()
        {
            Projectile p = base.GetComponent<Projectile>();
            p.OnWillKillEnemy += this.OnWillKillEnemy;
        }

        private void OnWillKillEnemy(Projectile bullet, SpeculativeRigidbody enemy)
        {
            Texture2D goldSprite                = GetGoldenTextureForEnemyIdleAnimation(enemy.aiActor);
            GameObject g                        = UnityEngine.Object.Instantiate(new GameObject(), enemy.sprite.WorldBottomLeft.ToVector3ZisY(0f), Quaternion.identity);
            tk2dSpriteCollectionData collection = SpriteBuilder.ConstructCollection(g, "goldcollection");
            int spriteId                        = SpriteBuilder.AddSpriteToCollection(goldSprite, collection, "goldsprite");
            tk2dBaseSprite sprite               = g.AddComponent<tk2dSprite>();
            sprite.SetSprite(collection, spriteId);
            g.AddComponent<GoldenDeath>();

            enemy.aiActor.EraseFromExistenceWithRewards(true);

        }

        private const float _SHEEN_WIDTH = 20.0f;
        public static Color _Gold  = new Color(1f,1f,0f,1f);
        public static Color _White = new Color(1f,1f,1f,1f);
        public static Texture2D GetGoldenTextureForEnemyIdleAnimation(AIActor enemy)
        {
            tk2dSpriteDefinition bestIdleSprite = enemy.sprite.collection.spriteDefinitions[CwaffToolbox.GetIdForBestIdleAnimation(enemy)];
            // If the x coordinate of the first two UVs match, we're using a rotated sprite
            bool isRotated = (bestIdleSprite.uvs[0].x == bestIdleSprite.uvs[1].x);
            Texture2D spriteTexture = bestIdleSprite.DesheetTexture();
            Texture2D goldTexture = new Texture2D(
                isRotated ? spriteTexture.height : spriteTexture.width,
                isRotated ? spriteTexture.width : spriteTexture.height);
            for (int x = 0; x < goldTexture.width; x++)
            {
                for (int y = 0; y < goldTexture.height; y++)
                {
                    Color pixelColor = spriteTexture.GetPixel(isRotated ? y : x, isRotated ? x : y);
                    if (pixelColor.a > 0)
                    {
                        // Blend opaque pixels with gold
                        pixelColor = Color.Lerp(pixelColor, _Gold, 0.3f);
                        // Add a diagonal white sheen
                        pixelColor = Color.Lerp(pixelColor, _White, Mathf.Sin( 6.28f * ( ( (x+y) % _SHEEN_WIDTH) / _SHEEN_WIDTH )));
                    }
                    goldTexture.SetPixel(x, y, pixelColor);
                }
            }
            return goldTexture;
        }
    }

    public class GoldenDeath : MonoBehaviour
    {
        private const float _START_EMIT    = 30.0f;
        private const float _MAX_EMIT      = 50.0f;
        private const float _MIN_EMIT      = 0.5f;
        private const float _GROW_TIME     = 0.25f;
        private const float _DECAY_TIME    = 0.5f;
        private const int   _NUM_PARTICLES = 10;
        private const float _PART_SPEED    = 2f;
        private const float _PART_SPREAD   = 0.5f;
        private const float _PART_LIFE     = 0.5f;
        private const float _PART_EMIT     = 20f;

        private float _lifetime;
        private bool _decaying;
        private tk2dSprite _sprite;

        private void Start()
        {
            this._lifetime = 0.0f;
            this._decaying = false;

            this._sprite = base.gameObject.GetComponent<tk2dSprite>();
            this._sprite.usesOverrideMaterial = true;
            this._sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTiltedCutoutEmissive");
            this._sprite.renderer.material.DisableKeyword("BRIGHTNESS_CLAMP_OFF");
            this._sprite.renderer.material.EnableKeyword("BRIGHTNESS_CLAMP_ON");
            this._sprite.renderer.material.SetFloat("_EmissivePower", _lifetime);
            this._sprite.renderer.material.SetFloat("_EmissiveColorPower", 1.55f);
            this._sprite.renderer.material.SetColor("_EmissiveColor", ExtendedColours.paleYellow);

            Vector2 ppos = this._sprite.WorldCenter;
            for (int i = 0; i < _NUM_PARTICLES; ++i)
            {
                float angle = Lazy.RandomAngle();
                Vector2 finalpos = ppos + BraveMathCollege.DegreesToVector(angle, magnitude: _PART_SPREAD);
                GameObject v = SpawnManager.SpawnVFX(QuarterPounder._MidasParticleVFX, finalpos, Lazy.RandomEulerZ());
                FancyVFX f = v.AddComponent<FancyVFX>();
                    f.Setup(Lazy.RandomVector(_PART_SPEED), lifetime: _PART_LIFE, fadeOutTime: _PART_LIFE, emissivePower: _PART_EMIT, emissiveColor: Color.white);
            }

            AkSoundEngine.PostEvent("turn_to_gold", base.gameObject);
        }

        private void Update()
        {
            float emit;
            if (this._decaying)
            {
                if (this._lifetime >= _DECAY_TIME)
                    return;
                this._lifetime = Mathf.Min(this._lifetime + BraveTime.DeltaTime, _DECAY_TIME);
                emit = _MAX_EMIT - (_MAX_EMIT - _MIN_EMIT) * (this._lifetime / _DECAY_TIME);
                this._sprite.renderer.material.SetFloat("_EmissivePower", emit);
                return;
            }
            this._lifetime = Mathf.Min(this._lifetime + BraveTime.DeltaTime, _GROW_TIME);
            emit = _START_EMIT + (_MAX_EMIT - _START_EMIT) * (this._lifetime / _GROW_TIME);
            this._sprite.renderer.material.SetFloat("_EmissivePower", emit);
            if (this._lifetime >= _GROW_TIME)
            {
                this._decaying = true;
                this._lifetime = 0.0f;
            }
        }
    }

}
