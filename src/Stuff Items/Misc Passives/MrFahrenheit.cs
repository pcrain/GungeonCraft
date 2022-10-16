using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using ItemAPI;
using Gungeon;

namespace NevernamedsItems
{
    class MrFahrenheit : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Mr. Fahrenheit";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/mrfahrenheit_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<MrFahrenheit>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "200 Degrees";
            string longDesc = "Sprint around, leaving a firey trail!" + "\n\nThere's no stopping you!";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item            
            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.B;

            AssetBundle assetBundle = ResourceManager.LoadAssetBundle("shared_auto_001");
            MrFahrenheit.goopDefs = new List<GoopDefinition>();
            foreach (string text in MrFahrenheit.goops)
            {
                GoopDefinition goopDefinition;
                try
                {
                    GameObject gameObject = assetBundle.LoadAsset(text) as GameObject;
                    goopDefinition = gameObject.GetComponent<GoopDefinition>();
                }
                catch
                {
                    goopDefinition = (assetBundle.LoadAsset(text) as GoopDefinition);
                }
                goopDefinition.name = text.Replace("assets/data/goops/", "").Replace(".asset", "");
                MrFahrenheit.goopDefs.Add(goopDefinition);
            }
            List<GoopDefinition> list = MrFahrenheit.goopDefs;

            Game.Items.Rename("nn:mr._fahrenheit", "nn:mr_fahrenheit");
        }
        private int currentItems, lastItems;

        public override void Update()
        {
            if (Owner)
            {
                if (!Owner.IsDodgeRolling && Challenges.CurrentChallenge != ChallengeType.KEEP_IT_COOL)
                {
                    if (Owner.HasPickupID(Gungeon.Game.Items["nn:supersonic_shots"].PickupObjectId))
                    {
                        GoopDefinition GreenFireDef = (PickupObjectDatabase.GetById(698) as Gun).DefaultModule.projectiles[0].GetComponent<GoopModifier>().goopDefinition;
                        var ddgm = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(GreenFireDef);
                        ddgm.AddGoopCircle(Owner.sprite.WorldBottomCenter, 1);
                    }
                    else
                    {
                        var ddgm = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(MrFahrenheit.goopDefs[0]);
                        ddgm.AddGoopCircle(Owner.sprite.WorldBottomCenter, 1);
                    }
                }
                currentItems = Owner.passiveItems.Count;
                if (currentItems != lastItems)
                {
                    if (Owner.HasPickupID(Gungeon.Game.Items["nn:supersonic_shots"].PickupObjectId))
                    {
                        handleSpeeds(true);
                    }
                    else
                    {
                        handleSpeeds(false);
                    }
                    lastItems = currentItems;
                }
            }
        }
        private void handleSpeeds(bool IncreasedSpeed)
        {
            RemoveStat(PlayerStats.StatType.MovementSpeed);
            if (IncreasedSpeed == true)
            {
                AddStat(PlayerStats.StatType.MovementSpeed, 4, StatModifier.ModifyMethod.ADDITIVE);
            }
            else
            {
                AddStat(PlayerStats.StatType.MovementSpeed, 2, StatModifier.ModifyMethod.ADDITIVE);
            }
            Owner.stats.RecalculateStats(Owner, true, false);
        }
        private void OnFired(Projectile bullet, float khajhfdjsfhfdjs)
        {
            if (Owner.CurrentGun.PickupObjectId == 597)
            {
                if (bullet.gameObject.name == "Planet_Mercury_Projectile(Clone)")
                {
                    bullet.AdjustPlayerProjectileTint(ExtendedColours.pink, 1);
                    bullet.AdditionalScaleMultiplier *= 3;
                    bullet.OnHitEnemy += this.ApplyCharmEffect;
                }
            }
        }
        private void ApplyCharmEffect(Projectile bullet, SpeculativeRigidbody target, bool he)
        {
            target.aiActor.ApplyEffect(GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultPermanentCharmEffect, 1f, null);
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.OnFired;
            this.m_fireImmunity = new DamageTypeModifier();
            this.m_fireImmunity.damageMultiplier = 0f;
            this.m_fireImmunity.damageType = CoreDamageTypes.Fire;
            Owner.healthHaver.damageTypeModifiers.Add(this.m_fireImmunity);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.PostProcessProjectile -= this.OnFired;
            Owner.healthHaver.damageTypeModifiers.Remove(this.m_fireImmunity);

            DebrisObject debrisObject = base.Drop(player);
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.healthHaver.damageTypeModifiers.Remove(this.m_fireImmunity);
                Owner.PostProcessProjectile -= this.OnFired;
            }
            base.OnDestroy();
        }
        private DamageTypeModifier m_fireImmunity;
        private static List<GoopDefinition> goopDefs;

        private static string[] goops = new string[]
        {
            "assets/data/goops/napalmgoopthatworks.asset",
            "assets/data/goops/napalmgoopthatworks.asset",
        };
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
