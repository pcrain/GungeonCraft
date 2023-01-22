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
        public static string spritePath       = "CwaffingTheGungy/Resources/ItemSprites/88888888_icon";
        public static string shortDescription = "That Ain't Falco";
        public static string longDescription  = "(Melee)";

        private static StatModifier noSpeed;

        private bool dodgeButtonHeld = false;
        private bool isShining = false;
        private PlayerController owner = null;
        private GameObject theShine = null;

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

        bool m_usedOverrideMaterial;
        private void ShineOn(PlayerController player)
        {
            this.isShining = true;
            theShine = Instantiate<GameObject>(
                VFX.animations["Shine"], player.specRigidbody.sprite.WorldCenter, Quaternion.identity, player.specRigidbody.transform);
            this.Update();
            // VFX.SpawnVFXPool("Shine",player.specRigidbody.sprite.WorldCenter, relativeTo: player.gameObject);
            // VFX.SpawnVFXPool("Shine", player.specRigidbody.sprite.WorldCenter);

            // theShine = Instantiate<GameObject>(VFX.animations["Shine"], player.sprite.WorldCenter, Quaternion.identity, player.specRigidbody.transform);
            m_usedOverrideMaterial = player.sprite.usesOverrideMaterial;
            player.sprite.usesOverrideMaterial = true;
            player.SetOverrideShader(ShaderCache.Acquire("Brave/ItemSpecific/MetalSkinShader"));
            SpeculativeRigidbody specRigidbody = player.specRigidbody;
            specRigidbody.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Combine(specRigidbody.OnPreRigidbodyCollision, new SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate(this.OnPreCollision));
            player.healthHaver.IsVulnerable = false;
            RecomputePlayerSpeed(player);
            if (this) AkSoundEngine.PostEvent("reflector", base.gameObject);
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
                    this.owner.ForceStartDodgeRoll();
                }
            }
            if (!theShine)
                return;
            float curscale = 0.25f+0.25f*Mathf.Abs(Mathf.Sin(20*BraveTime.ScaledTimeSinceStartup));
            theShine.transform.localScale = new Vector3(curscale,curscale,curscale);
        }

        private void ShineOff(PlayerController player)
        {
            if (!player)
                return;

            this.isShining = false;
            if (theShine)
                UnityEngine.Object.Destroy(theShine);
            player.healthHaver.IsVulnerable = true;
            player.ClearOverrideShader();
            player.sprite.usesOverrideMaterial = this.m_usedOverrideMaterial;
            SpeculativeRigidbody specRigidbody2 = player.specRigidbody;
            specRigidbody2.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Remove(specRigidbody2.OnPreRigidbodyCollision, new SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate(this.OnPreCollision));
            RecomputePlayerSpeed(player);
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
                Owner.PostProcessProjectile -= PostProcessProjectile;
            isShining = false;
            base.OnDestroy();
        }
    }
}

