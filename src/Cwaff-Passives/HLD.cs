using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;

using Gungeon;

namespace CwaffingTheGungy
{
    public class HLD : PassiveItem
    {
        public static string passiveName      = "HLD";
        public static string spritePath       = "CwaffingTheGungy/Resources/ItemSprites/yellow_bandana_icon";
        public static string shortDescription = "Hyper Light Dasher";
        public static string longDescription  = "(Pyoooom)";

        private PlayerController owner = null;
        private HLDRoll dodgeRoller = null;

        internal static GameObject LinkVFXPrefab;
        internal static Projectile lightningProjectile;

        public static void Init()
        {
            PickupObject item = Lazy.SetupItem<HLD>(passiveName, spritePath, shortDescription, longDescription, "cg");
            item.quality      = PickupObject.ItemQuality.A;

            LinkVFXPrefab = FakePrefab.Clone(Game.Items["shock_rounds"].GetComponent<ComplexProjectileModifier>().ChainLightningVFX);
            FakePrefab.MarkAsFakePrefab(LinkVFXPrefab);
            UnityEngine.Object.DontDestroyOnLoad(LinkVFXPrefab);

            var comp = item.gameObject.AddComponent<HLDRoll>();

            lightningProjectile = Lazy.PrefabProjectileFromGun((PickupObjectDatabase.GetById(198) as Gun));
                lightningProjectile.baseData.damage = 5f;
                lightningProjectile.baseData.speed  = 0.001f;
        }

        public override void Update()
        {
            base.Update();

            if (!this.owner)
                return;

            dodgeRoller.isHyped =
                this.owner.PlayerHasActiveSynergy("Hype Yourself Up");
        }

        private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherCollider)
        {
            if(!(dodgeRoller.isDodging && dodgeRoller.isHyped))  // reflect projectiles with hyped synergy
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
            this.owner = player;
            dodgeRoller = this.gameObject.GetComponent<HLDRoll>();
            dodgeRoller.owner = this.owner;
            SpeculativeRigidbody specRigidbody = player.specRigidbody;
            specRigidbody.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Combine(specRigidbody.OnPreRigidbodyCollision, new SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate(this.OnPreCollision));
            base.Pickup(player);
        }

        public override DebrisObject Drop(PlayerController player)
        {
            SpeculativeRigidbody specRigidbody2 = player.specRigidbody;
            specRigidbody2.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Remove(specRigidbody2.OnPreRigidbodyCollision, new SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate(this.OnPreCollision));
            this.owner = null;
            dodgeRoller.AbortDodgeRoll();
            return base.Drop(player);
        }

        public override void OnDestroy()
        {
            dodgeRoller.AbortDodgeRoll();
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
              HLD.lightningProjectile.gameObject,
              this.owner.sprite.WorldCenter,
              Quaternion.Euler(0f, 0f, this.owner.m_currentGunAngle),
              true).GetComponent<Projectile>();
                p.Owner = this.owner;
                p.Shooter = this.owner.specRigidbody;

                p.gameObject.AddComponent<FakeProjectileComponent>();
                p.gameObject.AddComponent<Expiration>().expirationTimer = DISOWN_TIME+FADE_TIME;

                OwnerConnectLightningModifier oclm = p.gameObject.AddComponent<OwnerConnectLightningModifier>();
                    oclm.linkPrefab = HLD.LinkVFXPrefab;
                    oclm.disownTimer = DISOWN_TIME;
                    oclm.fadeTimer = FADE_TIME;
                    oclm.MakeGlowy();
        }

        public override IEnumerator ContinueDodgeRoll()
        {
            float dashspeed = DASH_SPEED * (this.isHyped ? 1.2f : 1.0f);
            float dashtime = DASH_TIME;

            Vector2 vel = dashspeed * this.owner.m_lastNonzeroCommandedDirection.normalized;
            // string anim = (Mathf.Abs(vel.y) > Mathf.Abs(vel.x)) ? (vel.y > 0 ? "slide_up" : "slide_down") : "slide_right";
            // bool hasAnim = player.spriteAnimator.GetClipByName(anim) != null;

            AkSoundEngine.PostEvent("teledash", this.owner.gameObject);
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
                    this.owner.sprite.WorldCenter + Lazy.AngleToVector(dir, mag),
                    Quaternion.Euler(0f, 0f, rot));
            }

            bool interrupted = false;
            for (float timer = 0.0f; timer < dashtime; )
            {
                this.owner.PlayerAfterImage();
                timer += BraveTime.DeltaTime;
                this.owner.specRigidbody.Velocity = vel;
                GameManager.Instance.Dungeon.dungeonDustups.InstantiateLandDustup(this.owner.sprite.WorldCenter);
                // if (hasAnim && !this.owner.spriteAnimator.IsPlaying(anim))
                //     this.owner.spriteAnimator.Play(anim);  //TODO: the sliding animation itself causes the player to be invincible??? (QueryGroundedFrame())
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
                        this.owner.sprite.WorldCenter + Lazy.AngleToVector(dir, mag),
                        Quaternion.Euler(0f, 0f, rot));
                }
            }
            this.owner.spriteAnimator.Stop();
            this.owner.SetIsFlying(false, "hld");
            this.owner.ClearInputOverride("hld");
        }
    }
}

