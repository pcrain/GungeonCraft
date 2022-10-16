using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    class RingOfOddlySpecificBenefits : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Ring of Oddly Specific Benefits";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/ringofoddlyspecificbenefits_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<RingOfOddlySpecificBenefits>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Picky Picky";
            string longDesc = "Gives certain boons in... oddly specific situation." + "\n\nThis ring was created by the mad wizard Alben Smallbore, though even he has no clue what it actually does.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item            
            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.D;
        }
        private float currentArmour, lastArmour;
        private float currentCash, lastCash;
        private float currentGunAmmo, lastGunAmmo;
        private float currentCurse, lastCurse;
        private float currentCoolness, lastCoolness;
        bool hasRightArmour;
        bool hasRightCash;
        bool hasRightGunAmmo;
        bool curseAndCoolnessMatch;
        public override void Update()
        {
            if (Owner)
            {
                currentArmour = Owner.healthHaver.Armor;
                currentCash = Owner.carriedConsumables.Currency;
                currentGunAmmo = Owner.CurrentGun.ammo;
                currentCurse = Owner.stats.GetStatValue(PlayerStats.StatType.Curse);
                currentCoolness = Owner.stats.GetStatValue(PlayerStats.StatType.Coolness);
                if (currentArmour != lastArmour)
                {
                    if (currentArmour == 4f) hasRightArmour = true;
                    else hasRightArmour = false;
                }
                if (currentCash != lastCash)
                {
                    float lastDigitCash = currentCash % 10;
                    if (lastDigitCash == 4 || lastDigitCash == 8) hasRightCash = true;
                    else hasRightCash = false;
                }
                if (currentGunAmmo != lastGunAmmo)
                {
                    int firstDigitAmmo = (int)(currentGunAmmo / Math.Pow(10, (int)Math.Floor(Math.Log10(currentGunAmmo))));
                    if (firstDigitAmmo == 5) hasRightGunAmmo = true;
                    else hasRightGunAmmo = false;
                }
                if (currentCurse != lastCurse || currentCoolness != lastCoolness)
                {
                    bool coolnessAndCurseNull = currentCurse == 0 && currentCoolness == 0;
                    if (coolnessAndCurseNull)
                    {
                        curseAndCoolnessMatch = false;
                        return;
                    }
                    else
                    {
                        if (currentCurse == currentCoolness) curseAndCoolnessMatch = true;
                        else curseAndCoolnessMatch = false;
                    }
                }

                RemoveStat(PlayerStats.StatType.Damage);
                RemoveStat(PlayerStats.StatType.MovementSpeed);
                RemoveStat(PlayerStats.StatType.ProjectileSpeed);
                RemoveStat(PlayerStats.StatType.ReloadSpeed);
                DisableVFX(Owner);
                activeOutline = false;
                if (hasRightCash || hasRightArmour || hasRightGunAmmo || curseAndCoolnessMatch)
                {
                    GiveBoon(Owner);
                    EnableVFX(Owner);
                    activeOutline = true;
                }
                base.Owner.stats.RecalculateStats(base.Owner, true, false);
                lastArmour = currentArmour;
                lastCash = currentCash;
                lastGunAmmo = currentGunAmmo;
                lastCurse = currentCurse;
                lastCoolness = currentCoolness;
            }

            else { return; }
        }

        private void GiveBoon(PlayerController player)
        {
            AddStat(PlayerStats.StatType.Damage, 1.163f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            AddStat(PlayerStats.StatType.MovementSpeed, 1.163f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            AddStat(PlayerStats.StatType.ProjectileSpeed, 1.163f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            AddStat(PlayerStats.StatType.ReloadSpeed, 0.837f, StatModifier.ModifyMethod.MULTIPLICATIVE);
        }
        private void AddStat(PlayerStats.StatType statType, float amount, StatModifier.ModifyMethod method = StatModifier.ModifyMethod.ADDITIVE)
        {
            /*foreach (var m in passiveStatModifiers)
            {
                if (m.statToBoost == statType) return; //don't add duplicates
            }*/

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
        private void EnableVFX(PlayerController user)
        {
            Material outlineMaterial = SpriteOutlineManager.GetOutlineMaterial(user.sprite);
            outlineMaterial.SetColor("_OverrideColor", new Color(255f / 255f, 207f / 255f, 13f / 255f));
        }

        private void DisableVFX(PlayerController user)
        {
            Material outlineMaterial = SpriteOutlineManager.GetOutlineMaterial(user.sprite);
            outlineMaterial.SetColor("_OverrideColor", new Color(0f, 0f, 0f));
        }
        private void PlayerTookDamage(float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection)
        {
            if (activeOutline == true)
            {
                GameManager.Instance.StartCoroutine(this.GainOutline());
            }

            else if (activeOutline == false)
            {
                GameManager.Instance.StartCoroutine(this.LoseOutline());
            }
        }

        private IEnumerator GainOutline()
        {
            PlayerController user = Owner;
            yield return new WaitForSeconds(0.05f);
            EnableVFX(user);
            yield break;
        }

        private IEnumerator LoseOutline()
        {
            PlayerController user = Owner;
            yield return new WaitForSeconds(0.05f);
            DisableVFX(user);
            yield break;
        }

        bool activeOutline = false;
        public override void Pickup(PlayerController player)
        {
            player.healthHaver.OnDamaged += this.PlayerTookDamage;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DisableVFX(Owner);
            activeOutline = false;
            player.healthHaver.OnDamaged -= this.PlayerTookDamage;
            DebrisObject debrisObject = base.Drop(player);
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                DisableVFX(Owner);
                activeOutline = false;
                Owner.healthHaver.OnDamaged -= this.PlayerTookDamage;
            }
            base.OnDestroy();
        }
    }
}