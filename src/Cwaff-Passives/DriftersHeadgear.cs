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
    public class DriftersHeadgear : PassiveItem
    {
        public static string ItemName         = "Drifter's Headgear";
        public static string SpritePath       = "CwaffingTheGungy/Resources/ItemSprites/drifters_headgear_icon";
        public static string ShortDescription = "Hyper Light Dodger";
        public static string LongDescription  = "(Grants a very quick dodge roll, but loses the ability to dodge through bullets...just go faster o.o)";

        internal static GameObject _LinkVFXPrefab;
        internal static Projectile _LightningProjectile;

        private HLDRoll _dodgeRoller = null;

        public static void Init()
        {
            PickupObject item = Lazy.SetupPassive<DriftersHeadgear>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality      = PickupObject.ItemQuality.A;

            _LinkVFXPrefab = FakePrefab.Clone(Game.Items["shock_rounds"].GetComponent<ComplexProjectileModifier>().ChainLightningVFX)
                .RegisterPrefab(deactivate: false);

            var comp = item.gameObject.AddComponent<HLDRoll>();

            _LightningProjectile = Lazy.PrefabProjectileFromGun((ItemHelper.Get(Items.GunslingersAshes) as Gun));
                _LightningProjectile.baseData.damage = 5f;
                _LightningProjectile.baseData.speed  = 0.001f;
        }

        public override void Update()
        {
            base.Update();

            if (!this.Owner)
                return;

            this._dodgeRoller.isHyped =
                this.Owner.PlayerHasActiveSynergy("Hype Yourself Up");
        }

        private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherCollider)
        {
            if(!(this._dodgeRoller.isDodging && this._dodgeRoller.isHyped))  // reflect projectiles with hyped synergy
                return;
            Projectile component = otherRigidbody.GetComponent<Projectile>();
            if (component != null && !(component.Owner is PlayerController))
            {
                PassiveReflectItem.ReflectBullet(component, true, Owner.specRigidbody.gameActor, 10f, 1f, 1f, 0f);
                PhysicsEngine.SkipCollision = true;
            }
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            this._dodgeRoller = this.gameObject.GetComponent<HLDRoll>();
                this._dodgeRoller.owner = player;
            player.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
        }

        public override DebrisObject Drop(PlayerController player)
        {
            player.specRigidbody.OnPreRigidbodyCollision -= this.OnPreCollision;
            this._dodgeRoller.AbortDodgeRoll();
            return base.Drop(player);
        }

        public override void OnDestroy()
        {
            this._dodgeRoller.AbortDodgeRoll();
            base.OnDestroy();
        }
    }

    public class HLDRoll : CustomDodgeRoll
    {
        const float DASH_SPEED  = 50.0f; // Speed of our dash
        const float DASH_TIME   = 0.1f; // Time we spend dashing
        const float DISOWN_TIME = DASH_TIME+0.05f; // Amount of time after our dash starts before lightning is no longer connected
        const float FADE_TIME   = 0.5f; // Amount of time lightning persists after being disowned

        public bool isHyped = false;  // whether the hyped synergy is active

        public override void BeginDodgeRoll()
        {
            if (!(this.isHyped && this.owner))
                return;
            Projectile p = SpawnManager.SpawnProjectile(
              DriftersHeadgear._LightningProjectile.gameObject,
              this.owner.sprite.WorldCenter,
              Quaternion.Euler(0f, 0f, this.owner.m_currentGunAngle),
              true).GetComponent<Projectile>();
                p.Owner = this.owner;
                p.Shooter = this.owner.specRigidbody;

                p.gameObject.AddComponent<FakeProjectileComponent>();
                p.gameObject.AddComponent<Expiration>().expirationTimer = DISOWN_TIME+FADE_TIME;

                OwnerConnectLightningModifier oclm = p.gameObject.AddComponent<OwnerConnectLightningModifier>();
                    oclm.linkPrefab = DriftersHeadgear._LinkVFXPrefab;
                    oclm.disownTimer = DISOWN_TIME;
                    oclm.fadeTimer = FADE_TIME;
                    oclm.MakeGlowy();
        }

        public override IEnumerator ContinueDodgeRoll()
        {
            float dashspeed = DASH_SPEED * (this.isHyped ? 1.2f : 1.0f);
            float dashtime = DASH_TIME;

            Vector2 vel = dashspeed * this.owner.m_lastNonzeroCommandedDirection.normalized;

            AkSoundEngine.PostEvent("teledasher", this.owner.gameObject);
            this.owner.SetInputOverride("hld");
            this.owner.SetIsFlying(true, "hld");

            DustUpVFX dusts = GameManager.Instance.Dungeon.dungeonDustups;
            for (int i = 0; i < 16; ++i)
            {
                float dir = UnityEngine.Random.Range(0.0f,360.0f);
                float rot = UnityEngine.Random.Range(0.0f,360.0f);
                float mag = UnityEngine.Random.Range(0.3f,1.25f);
                SpawnManager.SpawnVFX(
                    dusts.rollLandDustup,
                    this.owner.sprite.WorldCenter + BraveMathCollege.DegreesToVector(dir, mag),
                    Quaternion.Euler(0f, 0f, rot));
            }

            bool interrupted = false;
            for (float timer = 0.0f; timer < dashtime; )
            {
                this.owner.PlayerAfterImage();
                timer += BraveTime.DeltaTime;
                this.owner.specRigidbody.Velocity = vel;
                GameManager.Instance.Dungeon.dungeonDustups.InstantiateLandDustup(this.owner.sprite.WorldCenter);
                yield return null;
                if (this.owner.IsFalling)
                {
                    interrupted = true;
                    break;
                }
            }
            if (!interrupted)
            {
                this.owner.PlayerAfterImage();
                for (int i = 0; i < 8; ++i)
                {
                    float dir = UnityEngine.Random.Range(0.0f,360.0f);
                    float rot = UnityEngine.Random.Range(0.0f,360.0f);
                    float mag = UnityEngine.Random.Range(0.3f,1.0f);
                    SpawnManager.SpawnVFX(
                        dusts.rollLandDustup,
                        this.owner.sprite.WorldCenter + BraveMathCollege.DegreesToVector(dir, mag),
                        Quaternion.Euler(0f, 0f, rot));
                }
            }
            this.owner.spriteAnimator.Stop();
            this.owner.SetIsFlying(false, "hld");
            this.owner.ClearInputOverride("hld");
        }
    }
}
