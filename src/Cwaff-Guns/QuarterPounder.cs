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
        public static string LongDescription  = "Uses casings as ammo. Fires high-powered projectiles that transmute enemies to gold upon death.\n\nLegend says that Dionysus granted King Midas' wish that everything he touched would turn to gold. Midas was overjoyed at first, but upon turning his food and daughter to gold, realized his wish was ill thought out, and eventually died of starvation.\n\nThe average person might interpret King Midas as a cautionary tale to be mindful of what you wish for. One gunsmith, however, heard the tale and thought, \"wow, turning my enemies to gold sure would be useful!\". Despite completely missing the moral of King Midas, the gunsmith did succeed in forging a rather powerful weapon, proving that the meaning of art is indeed up to the beholder.";

        internal static tk2dSpriteAnimationClip _ProjSprite;
        internal static GameObject _MidasParticleVFX;

        private int _lastMoney = -1;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.gameObject.AddComponent<QuarterPounder>();
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
                gun.ClearDefaultAudio();
                gun.SetFireAudio("fire_coin_sound");
                gun.SetReloadAudio("coin_gun_reload");

            _ProjSprite = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("coin_gun_projectile").Base(),
                2, true, new IntVector2(9, 6),
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
            if (this.gun.ClipShotsRemaining > money)
                this.gun.ClipShotsRemaining = money;
        }
    }

    public class MidasProjectile : MonoBehaviour
    {
        private const float _SHEEN_WIDTH = 20.0f;
        internal static Color _Gold      = new Color(1f,1f,0f,1f);
        internal static Color _White     = new Color(1f,1f,1f,1f);
        internal static Dictionary<string, Texture2D> _GoldenTextures = new();

        private void Start()
        {
            Projectile p = base.GetComponent<Projectile>();
            p.OnWillKillEnemy += this.OnWillKillEnemy;
        }

        private void OnWillKillEnemy(Projectile bullet, SpeculativeRigidbody enemy)
        {
            Texture2D goldSprite;
            if (_GoldenTextures.ContainsKey(enemy.aiActor.EnemyGuid))
                goldSprite = _GoldenTextures[enemy.aiActor.EnemyGuid]; // If we've already computed a texture for this enemy, don't do it again
            else
            {
                goldSprite = Lazy.GetTexturedEnemyIdleAnimation(enemy.aiActor, _Gold, 0.3f, _White, _SHEEN_WIDTH);
                _GoldenTextures[enemy.aiActor.EnemyGuid] = goldSprite; // Cache the texture for this enemy for later
            }
            GameObject g                        = UnityEngine.Object.Instantiate(new GameObject(), enemy.sprite.WorldBottomLeft.ToVector3ZisY(0f), Quaternion.identity);
            tk2dSpriteCollectionData collection = SpriteBuilder.ConstructCollection(g, "goldcollection");
            int spriteId                        = SpriteBuilder.AddSpriteToCollection(goldSprite, collection, "goldsprite");
            tk2dBaseSprite sprite               = g.AddComponent<tk2dSprite>();
            sprite.SetSprite(collection, spriteId);
            g.AddComponent<GoldenDeath>();

            enemy.aiActor.EraseFromExistenceWithRewards(true);

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
                FancyVFX.Spawn(QuarterPounder._MidasParticleVFX, finalpos, Lazy.RandomEulerZ(), ignoresPools: false,
                    velocity: Lazy.RandomVector(_PART_SPEED), lifetime: _PART_LIFE, fadeOutTime: _PART_LIFE, emissivePower: _PART_EMIT, emissiveColor: Color.white);
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
