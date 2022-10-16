using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;
using Dungeonator;
using Alexandria.ChestAPI;

namespace NevernamedsItems
{
    class PocketChest : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Pocket Chest";
            string resourceName = PocketChest.spritePaths[0]; 
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<PocketChest>();

            PocketChest.spriteIDs = new int[PocketChest.spritePaths.Length];

            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Baby Box";
            string longDesc = "An infant chest, containing many mysteries."+"\n\nLevels up as you deal damage, and grows up when used.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            PocketChest.spriteIDs[0] = item.sprite.spriteId; //Brown
            PocketChest.spriteIDs[1] = SpriteBuilder.AddSpriteToCollection(PocketChest.spritePaths[1], item.sprite.Collection); //Blue
            PocketChest.spriteIDs[2] = SpriteBuilder.AddSpriteToCollection(PocketChest.spritePaths[2], item.sprite.Collection); //Green
            PocketChest.spriteIDs[3] = SpriteBuilder.AddSpriteToCollection(PocketChest.spritePaths[3], item.sprite.Collection); //Red
            PocketChest.spriteIDs[4] = SpriteBuilder.AddSpriteToCollection(PocketChest.spritePaths[4], item.sprite.Collection); //Synergy
            PocketChest.spriteIDs[5] = SpriteBuilder.AddSpriteToCollection(PocketChest.spritePaths[5], item.sprite.Collection); //Black
            PocketChest.spriteIDs[6] = SpriteBuilder.AddSpriteToCollection(PocketChest.spritePaths[6], item.sprite.Collection); //Rainbow


            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.None, 500);

            item.consumable = true;
            item.quality = ItemQuality.D;

        }
        public PocketChestTier MemorisedTier = PocketChestTier.BROWN;
        public float storedDamage = 0f;
        public enum PocketChestTier
        {
            BROWN,
            BLUE,
            GREEN,
            RED,
            BLACK,
            SYNERGY,
            RAINBOW
        }

        public override void Update()
        {
            if (storedDamage >= 6500 && (MemorisedTier == PocketChestTier.RED)) //Red --> Black/Rainbow
            {
                if (UnityEngine.Random.value <= 0.01f)
                {
                    MemorisedTier = PocketChestTier.RAINBOW;
                    base.sprite.SetSprite(PocketChest.spriteIDs[6]);
                }
                else
                {
                    MemorisedTier = PocketChestTier.BLACK;
                    base.sprite.SetSprite(PocketChest.spriteIDs[5]);
                }
            }
            else if (storedDamage >= 3500 && (MemorisedTier == PocketChestTier.GREEN || MemorisedTier == PocketChestTier.SYNERGY)) //Green/Synergy --> Red
            {
                    MemorisedTier = PocketChestTier.RED;
                    base.sprite.SetSprite(PocketChest.spriteIDs[3]);
            }
            else if (storedDamage >= 1500 && MemorisedTier == PocketChestTier.BLUE) //Blue --> Green / Synergy
            {
                if (UnityEngine.Random.value <= 0.25f)
                {
                    MemorisedTier = PocketChestTier.SYNERGY;
                    base.sprite.SetSprite(PocketChest.spriteIDs[4]);

                }
                else
                {

                    MemorisedTier = PocketChestTier.GREEN;
                    base.sprite.SetSprite(PocketChest.spriteIDs[2]);
                }

            }
            else if (storedDamage >= 500 && MemorisedTier == PocketChestTier.BROWN) //Brown --> Blue
            {
                MemorisedTier = PocketChestTier.BLUE;
                base.sprite.SetSprite(PocketChest.spriteIDs[1]);

            }

            base.Update();
        }
        private void HurtEnemy(float damage, bool fatal, HealthHaver enemy)
        {
            storedDamage += damage;
        }
        public override void Pickup(PlayerController player)
        {
            if (!this.m_pickedUpThisRun)
            {
                MemorisedTier = PocketChestTier.BROWN;
                base.sprite.SetSprite(PocketChest.spriteIDs[0]);
                storedDamage = 0f;
            }
            base.Pickup(player);
            player.OnAnyEnemyReceivedDamage += this.HurtEnemy;
        }
        public override void OnPreDrop(PlayerController user)
        {
            user.OnAnyEnemyReceivedDamage -= this.HurtEnemy;
            base.OnPreDrop(user);
        }
        public override void DoEffect(PlayerController user)
        {
            ChestUtility.ChestTier chestToSpawn = ChestUtility.ChestTier.OTHER;
            switch (MemorisedTier)
            {
                case PocketChestTier.BROWN:
                    chestToSpawn = ChestUtility.ChestTier.BROWN;
                    break;
                case PocketChestTier.BLUE:
                    chestToSpawn = ChestUtility.ChestTier.BLUE;
                    break;
                case PocketChestTier.GREEN:
                    chestToSpawn = ChestUtility.ChestTier.GREEN;
                    break;
                case PocketChestTier.RED:
                    chestToSpawn = ChestUtility.ChestTier.RED;
                    break;
                case PocketChestTier.BLACK:
                    chestToSpawn = ChestUtility.ChestTier.BLACK;
                    break;
                case PocketChestTier.SYNERGY:
                    chestToSpawn = ChestUtility.ChestTier.SYNERGY;
                    break;
                case PocketChestTier.RAINBOW:
                    chestToSpawn = ChestUtility.ChestTier.RAINBOW;
                    break;
            }
            if (chestToSpawn != ChestUtility.ChestTier.OTHER)
            {
                IntVector2 bestRewardLocation2 = user.CurrentRoom.GetBestRewardLocation(IntVector2.One * 3, RoomHandler.RewardLocationStyle.PlayerCenter, true);
                ChestUtility.SpawnChestEasy(bestRewardLocation2, chestToSpawn, !(chestToSpawn == ChestUtility.ChestTier.RAINBOW || chestToSpawn == ChestUtility.ChestTier.BROWN), Chest.GeneralChestType.UNSPECIFIED);

            }
        }
        public override bool CanBeUsed(PlayerController user)
        {
            return base.CanBeUsed(user);
        }
        private static readonly string[] spritePaths = new string[]
        {
            "NevernamedsItems/Resources/pocketchest_brown_icon",
            "NevernamedsItems/Resources/pocketchest_blue_icon",
            "NevernamedsItems/Resources/pocketchest_green_icon",
            "NevernamedsItems/Resources/pocketchest_red_icon",
            "NevernamedsItems/Resources/pocketchest_synergy_icon",
            "NevernamedsItems/Resources/pocketchest_black_icon",
            "NevernamedsItems/Resources/pocketchest_rainbow_icon"
        };
        public static List<Chest.GeneralChestType> ChestyBois = new List<Chest.GeneralChestType>()
        {
            Chest.GeneralChestType.ITEM,
            Chest.GeneralChestType.WEAPON,
        };

        private static int[] spriteIDs;
    }
}