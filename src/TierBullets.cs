using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    class TierBullets : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Tier Bullets";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/tierbullets_default_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<TierBullets>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Typecast";
            string longDesc = "Picks an item tier at random when first collected. Gives a damage bonus for every item of that tier held." + "\n\nThese bullets were made eagerly, as a proof of concept for something much, much cooler...";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.EXCLUDED;


            TierBullets.spriteIDs = new int[TierBullets.spritePaths.Length];
            TierBullets.spriteIDs[0] = SpriteBuilder.AddSpriteToCollection(TierBullets.spritePaths[0], item.sprite.Collection);
            TierBullets.spriteIDs[1] = SpriteBuilder.AddSpriteToCollection(TierBullets.spritePaths[1], item.sprite.Collection);
            TierBullets.spriteIDs[2] = SpriteBuilder.AddSpriteToCollection(TierBullets.spritePaths[2], item.sprite.Collection);
            TierBullets.spriteIDs[2] = SpriteBuilder.AddSpriteToCollection(TierBullets.spritePaths[3], item.sprite.Collection);
            TierBullets.spriteIDs[2] = SpriteBuilder.AddSpriteToCollection(TierBullets.spritePaths[4], item.sprite.Collection);
        }
        private static int[] spriteIDs;
        private static readonly string[] spritePaths = new string[]
            {
            "NevernamedsItems/Resources/tierbullets_d_icon",
        "NevernamedsItems/Resources/tierbullets_c_icon",
        "NevernamedsItems/Resources/tierbullets_b_icon",
        "NevernamedsItems/Resources/tierbullets_a_icon",
        "NevernamedsItems/Resources/tierbullets_s_icon",
    };
        int id;
        private int currentItems, lastItems;
        private int currentGuns, lastGuns;
        private int currentActives, lastActives;
        public override void Update()
        {
            if (Owner)
            {
                CalculateStats(Owner);
            }

            else { return; }
        }

        private void CalculateStats(PlayerController player)
        {
            currentItems = player.passiveItems.Count;
            currentGuns = player.inventory.AllGuns.Count;
            currentActives = player.activeItems.Count;
            bool itemsChanged = currentItems != lastItems;
            bool gunsChanged = currentGuns != lastGuns;
            bool activesChanged = currentActives != lastActives;
            if (itemsChanged || gunsChanged || activesChanged)
            {
                RemoveStat(PlayerStats.StatType.Damage);
                foreach (PassiveItem item in player.passiveItems)
                {
                    if (item.quality == selectedTier)
                    {
                        if (selectedTier == PickupObject.ItemQuality.A || selectedTier == PickupObject.ItemQuality.S)
                        {
                            AddStat(PlayerStats.StatType.Damage, 1.2f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                        }
                        else
                        {
                            AddStat(PlayerStats.StatType.Damage, 1.1f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                        }
                    }
                }
                foreach (PlayerItem item in player.activeItems)
                {
                    if (item.quality == selectedTier)
                    {
                        if (selectedTier == PickupObject.ItemQuality.A || selectedTier == PickupObject.ItemQuality.S)
                        {
                            AddStat(PlayerStats.StatType.Damage, 1.2f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                        }
                        else
                        {
                            AddStat(PlayerStats.StatType.Damage, 1.1f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                        }
                    }
                }
                foreach (Gun gun in player.inventory.AllGuns)
                {
                    if (gun.quality == selectedTier)
                    {
                        if (selectedTier == PickupObject.ItemQuality.A || selectedTier == PickupObject.ItemQuality.S)
                        {
                            AddStat(PlayerStats.StatType.Damage, 1.2f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                        }
                        else
                        {
                            AddStat(PlayerStats.StatType.Damage, 1.1f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                        }
                    }
                }
                    lastItems = currentItems;
                player.stats.RecalculateStats(player, true, false);
            }
        }
        private void AlterSprite(int spriteId)
        {
            sprite.SetSprite(spriteId);
            this.SetDockItemSprite(spriteId);
        }
        private void SetDockItemSprite(int id)
        {
            List<Tuple<tk2dSprite, PassiveItem>> list = (List<Tuple<tk2dSprite, PassiveItem>>)this.m_dockItems.GetValue(Minimap.Instance.UIMinimap);
            for (int i = 0; i < list.Count; i++)
            {
                bool flag = list[i].Second is TierBullets;
                if (flag)
                {
                    list[i].First.SetSprite(base.sprite.Collection, id);
                }
            }
        }
        private FieldInfo m_dockItems = typeof(MinimapUIController).GetField("dockItems", BindingFlags.Instance | BindingFlags.NonPublic);
        public ItemQuality selectedTier;
        public override void Pickup(PlayerController player)
        {
            bool hasntAlreadyBeenCollected = !this.m_pickedUpThisRun;
            if (hasntAlreadyBeenCollected)
            {
                int tierroroftheseas = UnityEngine.Random.Range(1, 6);
                if (tierroroftheseas == 1) selectedTier = PickupObject.ItemQuality.D;
                if (tierroroftheseas == 2) selectedTier = PickupObject.ItemQuality.C;
                if (tierroroftheseas == 3) selectedTier = PickupObject.ItemQuality.B;
                if (tierroroftheseas == 4) selectedTier = PickupObject.ItemQuality.A;
                if (tierroroftheseas == 5) selectedTier = PickupObject.ItemQuality.S;
                id = tierroroftheseas;
                AlterSprite(id);
            }
            base.Pickup(player);
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
    }
}