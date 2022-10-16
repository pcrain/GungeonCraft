using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using UnityEngine;
using Alexandria.ItemAPI;
using System.Collections;
using TranslationAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    class ShadeHeart : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Shade Heart";
            string resourceName = "NevernamedsItems/Resources/shadeheart_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<ShadeHeart>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Heart of Darkness";
            string longDesc = "The ventricles of this shadowy organ are paper-thin, and ripple with a strange otherworldly energy." + "\n\nThough fragile, it holds fantastic power.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalShotPiercing, 10, StatModifier.ModifyMethod.ADDITIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.EnemyProjectileSpeedMultiplier, 0.95f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.GlobalPriceMultiplier, 0.7f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalBlanksPerFloor, 1, StatModifier.ModifyMethod.ADDITIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Coolness, 4, StatModifier.ModifyMethod.ADDITIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalItemCapacity, 1, StatModifier.ModifyMethod.ADDITIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Damage, 1.10f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            item.quality = PickupObject.ItemQuality.EXCLUDED;
            item.CanBeDropped = false;

            item.TranslateItemName(StringTableManager.GungeonSupportedLanguages.RUSSIAN, "Теневое Сердце");
            item.TranslateItemShortDescription(StringTableManager.GungeonSupportedLanguages.RUSSIAN, "Сердце Тьмы");
            item.TranslateItemLongDescription(StringTableManager.GungeonSupportedLanguages.RUSSIAN, "Тонкие, как бумага, стенки этого тёмного сосуда излучают энергию прямиком из другого мира.\n\nИ хотя оно хрупкое, внутри него заточена огромная сила.");
        }
        private float currentArmour, lastArmour;

        public override void Update()
        {
            if (Owner)
            {
                CalculateHealth(Owner);
                if (Owner.OverridePlayerSwitchState != PlayableCharacters.Pilot.ToString())
                {
                    Owner.OverridePlayerSwitchState = PlayableCharacters.Pilot.ToString();
                }
            }

            else { return; }
        }
        private bool hasDoneFirstArmourResetThisRun = false;
        private void CalculateHealth(PlayerController player)
        {
            currentArmour = player.healthHaver.Armor;
            if (currentArmour != lastArmour)
            {
                if (player.healthHaver.Armor > 1f)
                {
                    if (hasDoneFirstArmourResetThisRun)
                    {
                        int amountOfSurplus = (int)player.healthHaver.Armor - 1;
                        float percentPerArmour = 0.025f;
                        if (player.HasPickupID(FullArmourJacket.FullArmourJacketID)) percentPerArmour = 0.05f;

                        StatModifier statModifier = new StatModifier();
                        statModifier.amount = (percentPerArmour * amountOfSurplus) + 1;
                        statModifier.modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE;
                        statModifier.statToBoost = PlayerStats.StatType.Damage;
                        Owner.ownerlessStatModifiers.Add(statModifier);
                        Owner.stats.RecalculateStats(Owner, false, false);

                        LootEngine.SpawnCurrency(player.sprite.WorldCenter, (15 * amountOfSurplus));
                    }
                    hasDoneFirstArmourResetThisRun = true;
                    player.healthHaver.Armor = 1f;
                }
                lastArmour = currentArmour;
            }
        }
        public void OnNudgedHP(PlayerController playerCont, HealthPickup self)
        {
            if (self.armorAmount <= 0 && self.healAmount > 0)
            {
                if (playerCont.HasPickupID(BloodshotEye.BloodshotEyeID))
                {
                    float percentPerHalfHeart = 0.02f;
                    StatModifier statModifier = new StatModifier();
                    statModifier.amount = (percentPerHalfHeart * Mathf.CeilToInt(self.healAmount / 0.5f)) + 1;
                    statModifier.modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE;
                    statModifier.statToBoost = PlayerStats.StatType.Damage;
                    playerCont.ownerlessStatModifiers.Add(statModifier);
                    playerCont.stats.RecalculateStats(playerCont, false, false);
                }
                self.m_pickedUp = true;
                AkSoundEngine.PostEvent("Play_OBJ_coin_medium_01", self.gameObject);
                int amountToDrop = (self.healAmount >= 1f) ? UnityEngine.Random.Range(5, 12) : UnityEngine.Random.Range(3, 7);
                LootEngine.SpawnCurrency((!self.sprite) ? self.specRigidbody.UnitCenter : self.sprite.WorldCenter, amountToDrop, false);
                self.GetRidOfMinimapIcon();
                self.ToggleLabel(false);
                UnityEngine.Object.Destroy(self.gameObject);
            }
        }

        private DamageTypeModifier m_poisonImmunity;
        private DamageTypeModifier m_fireImmunity;
        public override void Pickup(PlayerController player)
        {
            //Setup Immunities
            this.m_poisonImmunity = new DamageTypeModifier();
            this.m_poisonImmunity.damageMultiplier = 0f;
            this.m_poisonImmunity.damageType = CoreDamageTypes.Poison;
            player.healthHaver.damageTypeModifiers.Add(this.m_poisonImmunity);
            this.m_fireImmunity = new DamageTypeModifier();
            this.m_fireImmunity.damageMultiplier = 0f;
            this.m_fireImmunity.damageType = CoreDamageTypes.Fire;
            player.healthHaver.damageTypeModifiers.Add(this.m_fireImmunity);

            //Make immune to pits
            player.ImmuneToPits.SetOverride("ShadeHeart", true, null);

            //Flight
            player.SetIsFlying(true, "Shadeheart", false, false);
            player.AdditionalCanDodgeRollWhileFlying.AddOverride("Shadeheart", null);

            //Other
            player.GetExtComp().OnNudgedHP += OnNudgedHP;
            base.Pickup(player);
        }
        public override void DisableEffect(PlayerController player)
        {
            //Remove immunities
            player.healthHaver.damageTypeModifiers.Remove(this.m_poisonImmunity);
            player.healthHaver.damageTypeModifiers.Remove(this.m_fireImmunity);
            player.ImmuneToPits.SetOverride("ShadeHeart", false, null);

            //Flight
            player.SetIsFlying(false, "Shadeheart", false, false);
            player.AdditionalCanDodgeRollWhileFlying.RemoveOverride("Shadeheart");

            //Other
            player.GetExtComp().OnNudgedHP -= OnNudgedHP;

            base.DisableEffect(player);
        }
    }
}