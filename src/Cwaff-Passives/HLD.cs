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
        public static string spritePath       = "CwaffingTheGungy/Resources/ItemSprites/88888888_icon";
        public static string shortDescription = "Hyper Light Dasher";
        public static string longDescription  = "(Pyoooom)";

        private static StatModifier noSpeed;

        private bool dodgeButtonHeld = false;
        private bool isDashing = false;
        private PlayerController owner = null;

        private static GameObject LinkVFXPrefab;

        public static void Init()
        {
            PickupObject item = Lazy.SetupItem<HLD>(passiveName, spritePath, shortDescription, longDescription, "cg");
            item.quality      = PickupObject.ItemQuality.C;

            noSpeed = new StatModifier
            {
                amount      = 0,
                statToBoost = PlayerStats.StatType.MovementSpeed,
                modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE
            };

            LinkVFXPrefab = FakePrefab.Clone(Game.Items["shock_rounds"].GetComponent<ComplexProjectileModifier>().ChainLightningVFX);
            FakePrefab.MarkAsFakePrefab(LinkVFXPrefab);
            UnityEngine.Object.DontDestroyOnLoad(LinkVFXPrefab);
        }
        private void PostProcessProjectile(Projectile bullet, float thing)
        {
            if (this.isDashing && this.owner)
                FinishDash(this.owner);
        }

        private IEnumerator DoDash(PlayerController player, float dashtime)
        {
            const float DASH_SPEED = 50.0f;
            Vector2 vel = DASH_SPEED * player.m_lastNonzeroCommandedDirection.normalized;
            string anim = (Mathf.Abs(vel.y) > Mathf.Abs(vel.x)) ? (vel.y > 0 ? "slide_up" : "slide_down") : "slide_right";
            bool hasAnim = player.spriteAnimator.GetClipByName(anim) != null;

            player.SetInputOverride("hld");
            for (float timer = 0.0f; timer < dashtime; )
            {
                timer += BraveTime.DeltaTime;
                player.specRigidbody.Velocity = vel;
                if (hasAnim && !player.spriteAnimator.IsPlaying(anim))
                    player.spriteAnimator.Play(anim);
                yield return null;
            }
            player.ClearInputOverride("hld");
        }

        // bool m_usedOverrideMaterial;
        private void StartDash(PlayerController player)
        {
            const float DASH_TIME   = 0.1f;
            const float EXPIRE_TIME = 0.5f;

            this.isDashing = true;
            Projectile p = SpawnManager.SpawnProjectile(
              TestLightning.defaultProjectile.gameObject,
              player.sprite.WorldCenter,
              Quaternion.Euler(0f, 0f, player.m_currentGunAngle),
              true).GetComponent<Projectile>();
                p.Owner = player;
                p.Shooter = player.specRigidbody;

                p.gameObject.AddComponent<FakeProjectileComponent>();
                p.gameObject.AddComponent<Expiration>().expirationTimer = EXPIRE_TIME;

                OwnerConnectLightningModifier oclm = p.gameObject.AddComponent<OwnerConnectLightningModifier>();
                    oclm.linkPrefab = LinkVFXPrefab;
                    oclm.disownTimer = DASH_TIME+0.05f;

            // TODO: dashing logic
            player.StartCoroutine(DoDash(player, DASH_TIME));

            if (this) AkSoundEngine.PostEvent("reflector", base.gameObject);

            // ChainLightningModifier cl = p.gameObject.AddComponent<ChainLightningModifier>();
            // cl.LinkVFXPrefab = LinkVFXPrefab;
            // cl.maximumLinkDistance = 7f;
            // cl.damagePerHit = 5f;
            // cl.damageCooldown = 1f;
            // cl.UseForcedLinkProjectile = true;
            // cl.ForcedLinkProjectile = SpawnManager.SpawnProjectile(
            //     TestLightning.defaultProjectile.gameObject,
            //     player.sprite.WorldCenter + new Vector2(2.0f,1.0f),
            //     Quaternion.Euler(0f, 0f, player.m_currentGunAngle),
            //     true).GetComponent<Projectile>();

            // BulletArcLightningController orAddComponent = p.gameObject.GetOrAddComponent<BulletArcLightningController>();
            // orAddComponent.Initialize(player.sprite.WorldCenter, 100, p.OwnerName, player.m_currentGunAngle, player.m_currentGunAngle+100f, 1.25f);
        }

        private void FinishDash(PlayerController player)
        {
            if (!player)
                return;

            this.isDashing = false;
        }

        public override void Update()
        {
            if (!this.owner)
                return;

            BraveInput instanceForPlayer = BraveInput.GetInstanceForPlayer(this.owner.PlayerIDX);
            if (instanceForPlayer.ActiveActions.DodgeRollAction.IsPressed)
            {
                instanceForPlayer.ConsumeButtonDown(GungeonActions.GungeonActionType.DodgeRoll);
                if (!(this.owner.IsDodgeRolling || dodgeButtonHeld || this.isDashing))
                {
                    this.dodgeButtonHeld = true;
                    StartDash(this.owner);
                }
            }
            else
            {
                this.dodgeButtonHeld = false;
                if (this.isDashing)
                {
                    FinishDash(this.owner);
                }
            }
        }

        private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherCollider)
        {
            if(!isDashing)
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
            player.PostProcessProjectile += PostProcessProjectile;
            base.Pickup(player);
        }

        public override DebrisObject Drop(PlayerController player)
        {
            this.owner = null;
            player.PostProcessProjectile -= PostProcessProjectile;
            isDashing = false;
            return base.Drop(player);
        }

        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessProjectile -= PostProcessProjectile;
            }
            isDashing = false;
            base.OnDestroy();
        }
    }
}

