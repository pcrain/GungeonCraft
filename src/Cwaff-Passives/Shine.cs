using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace CwaffingTheGungy
{
    public class Shine : PassiveItem
    {
        public static string passiveName      = "Shine";
        public static string spritePath       = "CwaffingTheGungy/Resources/NeoItemSprites/88888888_icon";
        public static string shortDescription = "Blip Blip";
        public static string longDescription  = "(Melee)";

        private static StatModifier noSpeed;

        private bool isShining = false;
        private PlayerController owner = null;

        public static void Init()
        {
            PickupObject item = Lazy.SetupItem<Shine>(passiveName, spritePath, shortDescription, longDescription, "cg");
            item.quality      = PickupObject.ItemQuality.C;

            noSpeed = new StatModifier
            {
                amount      = 0,
                statToBoost = PlayerStats.StatType.MovementSpeed,
                modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE
            };
        }
        private void PostProcessProjectile(Projectile bullet, float thing)
        {
            if (this.isShining && this.owner)
                ShineOff(this.owner);
        }
        private void OnPreDodgeRoll(PlayerController player)
        {
            // player.ClearDodgeRollState();
            // player.ForceStopDodgeRoll();
            // player.m_dodgeRollTimer = -1f;
            // player.m_dodgeRollState = PlayerController.DodgeRollState.Blink;
        }
        private void OnRollStarted(PlayerController player, Vector2 direction)
        {
            // player.ClearDodgeRollState();
            // player.ForceStopDodgeRoll();
            // player.m_dodgeRollTimer = -1f;
            // player.m_dodgeRollState = PlayerController.DodgeRollState.Blink;
            // player.m_dodgeRollState = PlayerController.DodgeRollState.Blink; //Blink, None, AdditionalDelay, InAir, OnGround, PreRollDelay
            if (this.isShining)
                ShineOff(player);
            else
                ShineOn(player);
        }
        private void OnIsRolling(PlayerController player)
        {
            if (!this.isShining)
                return;

            ETGModConsole.Log("Shining");
            // SND_CHR_dodge_roll_01
            player.ClearDodgeRollState();
            player.ForceStopDodgeRoll();
            // player.StartCoroutine(TemporarilyDisableInput(player,0.03f));
        }

        bool m_usedOverrideMaterial;
        private void ShineOn(PlayerController player)
        {
            this.isShining = true;
            m_usedOverrideMaterial = player.sprite.usesOverrideMaterial;
            player.sprite.usesOverrideMaterial = true;
            player.SetOverrideShader(ShaderCache.Acquire("Brave/ItemSpecific/MetalSkinShader"));
            SpeculativeRigidbody specRigidbody = player.specRigidbody;
            specRigidbody.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Combine(specRigidbody.OnPreRigidbodyCollision, new SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate(this.OnPreCollision));
            player.healthHaver.IsVulnerable = false;
            RecomputePlayerSpeed(player);
            if (this) AkSoundEngine.PostEvent("Play_OBJ_metalskin_end_01", base.gameObject);
        }

        private void ShineOff(PlayerController player)
        {
            if (!player)
                return;

            this.isShining = false;
            player.healthHaver.IsVulnerable = true;
            player.ClearOverrideShader();
            player.sprite.usesOverrideMaterial = this.m_usedOverrideMaterial;
            SpeculativeRigidbody specRigidbody2 = player.specRigidbody;
            specRigidbody2.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Remove(specRigidbody2.OnPreRigidbodyCollision, new SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate(this.OnPreCollision));
            RecomputePlayerSpeed(player);
            if (this) AkSoundEngine.PostEvent("Play_OBJ_metalskin_end_01", base.gameObject);
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

        private static IEnumerator TemporarilyDisableInput(PlayerController player, float timeout)
        {
            PlayerInputState oldInputState = player.CurrentInputState;
            player.CurrentInputState = PlayerInputState.NoMovement;
            yield return new WaitForSeconds(timeout);
            player.CurrentInputState = oldInputState;
            yield break;
        }

        public override void Pickup(PlayerController player)
        {
            this.owner = player;
            player.PostProcessProjectile += PostProcessProjectile;
            player.OnPreDodgeRoll += this.OnPreDodgeRoll;
            player.OnRollStarted += this.OnRollStarted;
            player.OnIsRolling += this.OnIsRolling;
            base.Pickup(player);
        }

        public override DebrisObject Drop(PlayerController player)
        {
            this.owner = null;
            player.PostProcessProjectile -= PostProcessProjectile;
            player.OnPreDodgeRoll -= this.OnPreDodgeRoll;
            player.OnRollStarted -= this.OnRollStarted;
            player.OnIsRolling -= this.OnIsRolling;
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

