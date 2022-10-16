using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Dungeonator;
using System.Collections;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class TinHeart : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Tin Heart";
            string resourceName = "NevernamedsItems/Resources/tinheart_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<TinHeart>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "If I Only Had A Heart";
            string longDesc = "The empty heart of a loving gungeoneer who was tragically turned to tin by Meduzi, the jealous gunwitch." + "\n\nWhen you are truly empty inside, it sacrifices the only things you have left to keep you alive.";
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Health, 1, StatModifier.ModifyMethod.ADDITIVE);

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.CanBeDropped = true;
            item.quality = PickupObject.ItemQuality.B;
            TinHeartID = item.PickupObjectId;
        }
        public static int TinHeartID;
        private void ModifyDamage(HealthHaver player, HealthHaver.ModifyDamageEventArgs args)
        {
            if (player.gameActor is PlayerController)
            {
                PlayerController playerCont = player.gameActor as PlayerController;
                if (playerCont.ForceZeroHealthState)
                {
                    if (UnityEngine.Random.value <= 0.25f) { args.ModifiedDamage = 0; HandleRemoveHeart(); }
                }
                else
                {
                    if (playerCont.NextHitWillKillPlayer(args.InitialDamage))
                    {
                        if (player.GetCurrentHealth() > 0.5f) args.ModifiedDamage = 0.5f;
                        else if ((player.GetCurrentHealth() == 0.5f && player.Armor == 0) || (player.GetCurrentHealth() == 0f && player.Armor == 1))
                        {
                            if (player.GetMaxHealth() > 1)
                            {
                                args.ModifiedDamage = 0;
                                HandleRemoveHeart();
                            }
                        }
                    }
                }
                if (playerCont.PlayerHasActiveSynergy("Oil Can What?") && UnityEngine.Random.value <= 0.1f)
                { args.ModifiedDamage = 0; }
            }
        }
        private void HandleRemoveHeart()
        {
            Owner.ForceBlank();
            StartCoroutine(HandleShield(Owner, 7));
            if (Owner.PlayerHasActiveSynergy("Woodcutter") && Owner.CurrentGun.PickupObjectId == 346 && Owner.CurrentGun.IsReloading) return;
            if (!Owner.ForceZeroHealthState)
            {
                StatModifier healthDown = new StatModifier();
                healthDown.statToBoost = PlayerStats.StatType.Health;
                healthDown.amount = -1f;
                healthDown.modifyType = StatModifier.ModifyMethod.ADDITIVE;
                Owner.ownerlessStatModifiers.Add(healthDown);
                Owner.stats.RecalculateStats(Owner, false, false);
            }
        }
        private void OnPreDeath(Vector2 dir)
        {
            if (Owner.healthHaver.GetMaxHealth() > 1)
            {
                Owner.healthHaver.ApplyHealing(0.5f);
                HandleRemoveHeart();
            }
        }
        public override void Pickup(PlayerController player)
        {
            if (!this.m_pickedUpThisRun)
            {
                if (player.ForceZeroHealthState)
                {
                    for (int i = 0; i < 2; i++) LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, player);
                }
                else
                {
                    if (player.healthHaver.GetCurrentHealth() > 1)
                    {
                        player.healthHaver.ApplyHealing(-1);
                    }
                }
                
            }
            player.healthHaver.ModifyDamage += this.ModifyDamage;
            player.healthHaver.OnPreDeath += this.OnPreDeath;
            base.Pickup(player);
        }
        bool hadOilCanWhatLastWeChecked;
        public override void Update()
        {
            if (Owner)
            {
                if (Owner.PlayerHasActiveSynergy("Oil Can What?") && !hadOilCanWhatLastWeChecked)
                {
                    RemoveStat(PlayerStats.StatType.MovementSpeed);
                    Owner.stats.RecalculateStats(Owner, true, false);
                    AddStat(PlayerStats.StatType.MovementSpeed, 1f, StatModifier.ModifyMethod.ADDITIVE);
                    Owner.stats.RecalculateStats(Owner, true, false);
                    hadOilCanWhatLastWeChecked = Owner.PlayerHasActiveSynergy("Oil Can What?");
                }
                else if (!Owner.PlayerHasActiveSynergy("Oil Can What?") && hadOilCanWhatLastWeChecked)
                {
                    RemoveStat(PlayerStats.StatType.MovementSpeed);
                    Owner.stats.RecalculateStats(Owner, true, false);
                    hadOilCanWhatLastWeChecked = Owner.PlayerHasActiveSynergy("Oil Can What?");
                }
            }
            base.Update();
        }

        public override DebrisObject Drop(PlayerController player)
        {
            player.healthHaver.ModifyDamage -= this.ModifyDamage;
            player.healthHaver.OnPreDeath -= this.OnPreDeath;
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.healthHaver.OnPreDeath -= this.OnPreDeath;
                Owner.healthHaver.ModifyDamage -= this.ModifyDamage;
            }
            base.OnDestroy();
        }
        float m_activeDuration = 1f;
        bool m_usedOverrideMaterial;
        private IEnumerator HandleShield(PlayerController user, float duration)
        {
            m_activeDuration = duration;
            m_usedOverrideMaterial = user.sprite.usesOverrideMaterial;
            user.sprite.usesOverrideMaterial = true;
            user.SetOverrideShader(ShaderCache.Acquire("Brave/ItemSpecific/MetalSkinShader"));
            SpeculativeRigidbody specRigidbody = user.specRigidbody;
            specRigidbody.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Combine(specRigidbody.OnPreRigidbodyCollision, new SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate(this.OnPreCollision));
            user.healthHaver.IsVulnerable = false;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += BraveTime.DeltaTime;
                user.healthHaver.IsVulnerable = false;
                yield return null;
            }
            if (user)
            {
                user.healthHaver.IsVulnerable = true;
                user.ClearOverrideShader();
                user.sprite.usesOverrideMaterial = this.m_usedOverrideMaterial;
                SpeculativeRigidbody specRigidbody2 = user.specRigidbody;
                specRigidbody2.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Remove(specRigidbody2.OnPreRigidbodyCollision, new SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate(this.OnPreCollision));
                //IsCurrentlyActive = false;
            }
            if (this)
            {
                AkSoundEngine.PostEvent("Play_OBJ_metalskin_end_01", base.gameObject);
            }
            yield break;
        }
        private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherCollider)
        {
            Projectile component = otherRigidbody.GetComponent<Projectile>();
            if (component != null && !(component.Owner is PlayerController))
            {
                PassiveReflectItem.ReflectBullet(component, true, Owner.specRigidbody.gameActor, 10f, 1f, 1f, 0f);
                PhysicsEngine.SkipCollision = true;
            }
        }
        private void AddStat(PlayerStats.StatType statType, float amount, StatModifier.ModifyMethod method = StatModifier.ModifyMethod.ADDITIVE)
        {
            StatModifier modifier = new StatModifier
            {
                amount = amount,
                statToBoost = statType,
                modifyType = method
            };

            if (this.passiveStatModifiers == null)
                this.passiveStatModifiers = new StatModifier[] { modifier };
            else
                this.passiveStatModifiers = this.passiveStatModifiers.Concat(new StatModifier[] { modifier }).ToArray();
        }

        private void RemoveStat(PlayerStats.StatType statType)
        {
            var newModifiers = new List<StatModifier>();
            for (int i = 0; i < passiveStatModifiers.Length; i++)
            {
                if (passiveStatModifiers[i].statToBoost != statType)
                    newModifiers.Add(passiveStatModifiers[i]);
            }
            this.passiveStatModifiers = newModifiers.ToArray();
        }
    }

}