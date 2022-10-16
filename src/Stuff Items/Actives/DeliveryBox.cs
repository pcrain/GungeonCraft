using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Alexandria.ItemAPI;
using Alexandria.Misc;
using SaveAPI;
using Dungeonator;

namespace NevernamedsItems
{
    class DeliveryBox : LabelablePlayerItem
    {
        public static void Init()
        {
            string itemName = "Delivery Box";
            string resourceName = "NevernamedsItems/Resources/MultiStageActives/deliverybox_closed";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<DeliveryBox>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Ready To Order";
            string longDesc = "Allows for the high-speed delivery of goods purchased straight from the manufacturer!"+"\n\nCut out the middle man!";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Timed, 0.2f);

            item.currentLabel = "-1";

            DeliveryBox.spriteIDs = new int[DeliveryBox.spritePaths.Length];

            DeliveryBox.spriteIDs[0] = item.sprite.spriteId; //empty
            DeliveryBox.spriteIDs[1] = SpriteBuilder.AddSpriteToCollection(DeliveryBox.spritePaths[1], item.sprite.Collection); //Ammo
            DeliveryBox.spriteIDs[2] = SpriteBuilder.AddSpriteToCollection(DeliveryBox.spritePaths[2], item.sprite.Collection); //SpreadAmmo
            DeliveryBox.spriteIDs[3] = SpriteBuilder.AddSpriteToCollection(DeliveryBox.spritePaths[3], item.sprite.Collection); //Armour
            DeliveryBox.spriteIDs[4] = SpriteBuilder.AddSpriteToCollection(DeliveryBox.spritePaths[4], item.sprite.Collection); //Half Heart
            DeliveryBox.spriteIDs[5] = SpriteBuilder.AddSpriteToCollection(DeliveryBox.spritePaths[5], item.sprite.Collection); //full heart
            DeliveryBox.spriteIDs[6] = SpriteBuilder.AddSpriteToCollection(DeliveryBox.spritePaths[6], item.sprite.Collection); //Key
            DeliveryBox.spriteIDs[7] = SpriteBuilder.AddSpriteToCollection(DeliveryBox.spritePaths[7], item.sprite.Collection); //Glass Guon
            DeliveryBox.spriteIDs[8] = SpriteBuilder.AddSpriteToCollection(DeliveryBox.spritePaths[8], item.sprite.Collection); //blank

            item.consumable = false;
            item.quality = ItemQuality.B;
        }
        private Dictionary<ConsumableType, int> spriteDefDictionary = new Dictionary<ConsumableType, int>()
        {
            {ConsumableType.BLANK,  8},
            {ConsumableType.AMMO,  1},
            {ConsumableType.SPREAD_AMMO,  2},
            {ConsumableType.ARMOR,  3},
            {ConsumableType.HALF_HEART,  4},
            {ConsumableType.HEART,  5},
            {ConsumableType.KEY,  6},
            {ConsumableType.GLASS_GUON_STONE,  7},
        };
        private Dictionary<ConsumableType, int> consumableIDDictionary = new Dictionary<ConsumableType, int>()
        {
            {ConsumableType.BLANK,  224},
            {ConsumableType.AMMO,  78},
            {ConsumableType.SPREAD_AMMO,  600},
            {ConsumableType.ARMOR,  120},
            {ConsumableType.HALF_HEART,  73},
            {ConsumableType.HEART,  85},
            {ConsumableType.KEY,  67},
            {ConsumableType.GLASS_GUON_STONE,  565},
        };
        public override void Pickup(PlayerController player)
        {
            if (!this.m_pickedUpThisRun)
            {
                SetConsumableTo(RandomConsumable());
            }
            else
            {
                SetConsumableTo(CurrentConsumable);
            }
            base.Pickup(player);
        }
        public override void OnPreDrop(PlayerController user)
        {
            base.sprite.SetSprite(DeliveryBox.spriteIDs[0]);
            base.OnPreDrop(user);
        }
        public override void DoEffect(PlayerController user)
        {
            user.carriedConsumables.Currency -= CalculatePrice(CurrentConsumable);
            SpawnCrate(consumableIDDictionary[CurrentConsumable]);
            SetConsumableTo(RandomConsumable());
        }
        public override void Update()
        {
            if (int.Parse(this.currentLabel) != CalculatePrice(CurrentConsumable))
            {
                this.currentLabel = CalculatePrice(CurrentConsumable).ToString();
            }
            base.Update();
        }
        private int CalculatePrice(ConsumableType consumable)
        {
            if (consumableIDDictionary.ContainsKey(consumable))
            {
                PickupObject databaseObject = PickupObjectDatabase.GetById(consumableIDDictionary[consumable]);
                if (databaseObject != null)
                {
                    int price = databaseObject.PurchasePrice;
                    GameLevelDefinition lastLoadedLevelDefinition = GameManager.Instance.GetLastLoadedLevelDefinition();
                    float num4 = (lastLoadedLevelDefinition == null) ? 1f : lastLoadedLevelDefinition.priceMultiplier;
                    float moddedPrice = price * num4;
                    if (LastOwner)
                    {
                        moddedPrice = price * LastOwner.stats.GetStatValue(PlayerStats.StatType.GlobalPriceMultiplier);
                        return Mathf.RoundToInt(moddedPrice);
                    }
                    else
                    {
                        return Mathf.RoundToInt(moddedPrice);
                    }
                }
                else return 420;
            }
            else return 69;
        }
        private void SpawnCrate(int item)
        {
            IntVector2 bestRewardLocation = LastOwner.CurrentRoom.GetBestRewardLocation(new IntVector2(1, 1), RoomHandler.RewardLocationStyle.CameraCenter, true);
            SupplyDropDoer.SpawnSupplyDrop(bestRewardLocation.ToVector2(), item);
        }
        private ConsumableType RandomConsumable()
        {
            return RandomEnum<ConsumableType>.Get();
        }
        private void SetConsumableTo(ConsumableType consumable)
        {
            if (spriteDefDictionary.ContainsKey(consumable))
            {
                base.sprite.SetSprite(DeliveryBox.spriteIDs[spriteDefDictionary[consumable]]);
            }
            else
            {
                base.sprite.SetSprite(DeliveryBox.spriteIDs[0]);
            }

            CurrentConsumable = consumable;
        }
        public ConsumableType CurrentConsumable;
        public enum ConsumableType
        {
            KEY,
            BLANK,
            AMMO,
            SPREAD_AMMO,
            ARMOR,
            HEART,
            HALF_HEART,
            GLASS_GUON_STONE,
        }

        public override bool CanBeUsed(PlayerController user)
        {
            if (user && user.carriedConsumables.Currency >= CalculatePrice(CurrentConsumable))
            {
                return true;
            }
            return false;
        }
        private static readonly string[] spritePaths = new string[]
        {
            "NevernamedsItems/Resources/MultiStageActives/deliverybox_closed",
            "NevernamedsItems/Resources/MultiStageActives/deliverybox_ammo",
            "NevernamedsItems/Resources/MultiStageActives/deliverybox_spreadammo",
            "NevernamedsItems/Resources/MultiStageActives/deliverybox_armour",
            "NevernamedsItems/Resources/MultiStageActives/deliverybox_halfheart",
            "NevernamedsItems/Resources/MultiStageActives/deliverybox_heart",
            "NevernamedsItems/Resources/MultiStageActives/deliverybox_key",
            "NevernamedsItems/Resources/MultiStageActives/deliverybox_glassguon",
            "NevernamedsItems/Resources/MultiStageActives/deliverybox_blank",
        };
        private static int[] spriteIDs;
    }
}

