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
        private bool isShining = false;
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
            if (this.isShining && this.owner)
                ShineOff(this.owner);
        }

        // bool m_usedOverrideMaterial;
        private void ShineOn(PlayerController player)
        {
            this.isShining = true;
            Projectile p = SpawnManager.SpawnProjectile(
                TestLightning.defaultProjectile.gameObject,
                player.sprite.WorldCenter,
                Quaternion.Euler(0f, 0f, player.m_currentGunAngle),
                true).GetComponent<Projectile>();
            p.Owner = player;
            p.Shooter = player.specRigidbody;

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

            OwnerConnectLightningModifier oclm = p.gameObject.AddComponent<OwnerConnectLightningModifier>();
            oclm.linkPrefab = LinkVFXPrefab;

            p.gameObject.AddComponent<FakeProjectileComponent>();
            p.gameObject.AddComponent<Expiration>().expirationTimer = 1f;

            // BulletArcLightningController orAddComponent = p.gameObject.GetOrAddComponent<BulletArcLightningController>();
            // orAddComponent.Initialize(player.sprite.WorldCenter, 100, p.OwnerName, player.m_currentGunAngle, player.m_currentGunAngle+100f, 1.25f);

            // this.Update();

            // m_usedOverrideMaterial = player.sprite.usesOverrideMaterial;
            // player.sprite.usesOverrideMaterial = true;
            // player.SetOverrideShader(ShaderCache.Acquire("Brave/ItemSpecific/MetalSkinShader"));
            // SpeculativeRigidbody specRigidbody = player.specRigidbody;
            // specRigidbody.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Combine(specRigidbody.OnPreRigidbodyCollision, new SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate(this.OnPreCollision));
            // player.healthHaver.IsVulnerable = false;
            // RecomputePlayerSpeed(player);
            // if (this) AkSoundEngine.PostEvent("reflector", base.gameObject);
        }

        private void ShineOff(PlayerController player)
        {
            if (!player)
                return;

            this.isShining = false;
            // player.healthHaver.IsVulnerable = true;
            // player.ClearOverrideShader();
            // player.sprite.usesOverrideMaterial = this.m_usedOverrideMaterial;
            // SpeculativeRigidbody specRigidbody2 = player.specRigidbody;
            // specRigidbody2.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Remove(specRigidbody2.OnPreRigidbodyCollision, new SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate(this.OnPreCollision));
            // RecomputePlayerSpeed(player);
        }

        public override void Update()
        {
            if (!this.owner)
                return;
            BraveInput instanceForPlayer = BraveInput.GetInstanceForPlayer(this.owner.PlayerIDX);
            if (instanceForPlayer.ActiveActions.DodgeRollAction.IsPressed)
            {
                instanceForPlayer.ConsumeButtonDown(GungeonActions.GungeonActionType.DodgeRoll);
                if (!(this.owner.IsDodgeRolling || dodgeButtonHeld || this.isShining))
                {
                    this.dodgeButtonHeld = true;
                    ShineOn(this.owner);
                }
            }
            else
            {
                this.dodgeButtonHeld = false;
                if (this.isShining)
                {
                    ShineOff(this.owner);
                    // this.owner.ForceStartDodgeRoll();
                }
            }
        }

        private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherCollider)
        {
            if(!isShining)
                return;
            Projectile component = otherRigidbody.GetComponent<Projectile>();
            if (component != null && !(component.Owner is PlayerController))
            {
                PassiveReflectItem.ReflectBullet(component, true, Owner.specRigidbody.gameActor, 10f, 1f, 1f, 0f);
                PhysicsEngine.SkipCollision = true;
            }
        }

        private void RecomputePlayerSpeed(PlayerController p)
        {
            if (isShining)
                this.passiveStatModifiers = (new StatModifier[] { noSpeed }).ToArray();
            else
                this.passiveStatModifiers = (new StatModifier[] {  }).ToArray();
            p.stats.RecalculateStats(p, false, false);
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
            isShining = false;
            return base.Drop(player);
        }

        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessProjectile -= PostProcessProjectile;
            }
            isShining = false;
            base.OnDestroy();
        }
    }
}

