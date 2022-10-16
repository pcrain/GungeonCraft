using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    class BookOfMimicAnatomy : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Book of Mimic Anatomy";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/bookofmimicanatomy_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<BookOfMimicAnatomy>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Look closer...";
            string longDesc = "This book, while bound and covered identically to the Book of Chest Anatomy, is in fact a much more interesting tome on the anatomy of the creature known as the Mimic." + "\n\nIt appears to be a sequel to the Book of Chest Anatomy from the same author, which is good ‘cause that one left off on a cliffhanger.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.A;

            item.AddToSubShop(ItemBuilder.ShopType.Flynt);
        }
        private List<string> mimicGuids = new List<string>
        {
            "2ebf8ef6728648089babb507dec4edb7", //Brown
            "d8d651e3484f471ba8a2daa4bf535ce6", //Blue
            "abfb454340294a0992f4173d6e5898a8", //Green
            "d8fd592b184b4ac9a3be217bc70912a2", //Red
            "ac9d345575444c9a8d11b799e8719be0", //Rat
            "6450d20137994881aff0ddd13e3d40c8", //Black
            "479556d05c7c44f3b6abb3b2067fc778", //Wall
        };
        private void OnEnemyDamaged(float damage, bool fatal, HealthHaver enemy)
        {
            if (fatal == true && enemy.aiActor)
            {
                if (GameStatsManager.Instance.IsRainbowRun == true && mimicGuids.Contains(enemy.aiActor.EnemyGuid))
                {
                    LootEngine.SpawnItem(PickupObjectDatabase.GetById(120).gameObject, enemy.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                    LootEngine.SpawnItem(PickupObjectDatabase.GetById(120).gameObject, enemy.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                    LootEngine.SpawnItem(PickupObjectDatabase.GetById(120).gameObject, enemy.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                }
                else
                {
                    var itemQuality = PickupObject.ItemQuality.D;
                    if (enemy.aiActor.EnemyGuid == "2ebf8ef6728648089babb507dec4edb7") //BROWN CHEST MIMIC
                    {
                        itemQuality = PickupObject.ItemQuality.D;
                        SpawnBonusItem(enemy, itemQuality);
                    }
                    else if (enemy.aiActor.EnemyGuid == "d8d651e3484f471ba8a2daa4bf535ce6") //BLUE CHEST MIMIC
                    {
                        itemQuality = PickupObject.ItemQuality.C;
                        SpawnBonusItem(enemy, itemQuality);
                    }
                    else if (enemy.aiActor.EnemyGuid == "abfb454340294a0992f4173d6e5898a8") //GREEN CHEST MIMIC
                    {
                        itemQuality = PickupObject.ItemQuality.B;
                        SpawnBonusItem(enemy, itemQuality);
                    }
                    else if (enemy.aiActor.EnemyGuid == "d8fd592b184b4ac9a3be217bc70912a2" || enemy.aiActor.EnemyGuid == "ac9d345575444c9a8d11b799e8719be0") //RED CHEST MIMIC & RAT CHEST MIMIC
                    {
                        itemQuality = PickupObject.ItemQuality.A;
                        SpawnBonusItem(enemy, itemQuality);
                    }
                    else if (enemy.aiActor.EnemyGuid == "6450d20137994881aff0ddd13e3d40c8") //Black CHEST MIMIC
                    {
                        itemQuality = PickupObject.ItemQuality.S;
                        SpawnBonusItem(enemy, itemQuality);
                    }
                    else if (enemy.aiActor.EnemyGuid == "479556d05c7c44f3b6abb3b2067fc778") // WALL MIMIC
                    {
                        int randomTierSelectionNumber = UnityEngine.Random.Range(1, 100);
                        if (randomTierSelectionNumber <= 50) itemQuality = PickupObject.ItemQuality.D; //Make Tier D
                        else if (randomTierSelectionNumber <= 67) itemQuality = PickupObject.ItemQuality.C; //Make Tier C
                        else if (randomTierSelectionNumber <= 87) itemQuality = PickupObject.ItemQuality.B; //Make Tier B
                        else if (randomTierSelectionNumber <= 98) itemQuality = PickupObject.ItemQuality.A; //Make Tier A               
                        else if (randomTierSelectionNumber <= 100) itemQuality = PickupObject.ItemQuality.S; //Make Tier S
                        SpawnBonusItem(enemy, itemQuality);
                    }
                    else if (enemy.aiActor.EnemyGuid == "796a7ed4ad804984859088fc91672c7f") //PEDESTAL MIMIC
                    {
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(120).gameObject, enemy.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(120).gameObject, enemy.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(78).gameObject, enemy.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                    }
                    else if (enemy.aiActor.EnemyGuid == "9189f46c47564ed588b9108965f975c9") //DOOR LORD
                    {
                        itemQuality = PickupObject.ItemQuality.S;
                        GameManager.Instance.RewardManager.SpawnTotallyRandomItem(enemy.specRigidbody.UnitCenter, itemQuality, itemQuality);
                        itemQuality = PickupObject.ItemQuality.A;
                        GameManager.Instance.RewardManager.SpawnTotallyRandomItem(enemy.specRigidbody.UnitCenter, itemQuality, itemQuality);
                        itemQuality = PickupObject.ItemQuality.B;
                        GameManager.Instance.RewardManager.SpawnTotallyRandomItem(enemy.specRigidbody.UnitCenter, itemQuality, itemQuality);
                    }
                }
            }

        }

        private void SpawnBonusItem(HealthHaver enemy, ItemQuality itemQuality)
        {
            GameManager.Instance.RewardManager.SpawnTotallyRandomItem(enemy.specRigidbody.UnitCenter, itemQuality, itemQuality);
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnAnyEnemyReceivedDamage += this.OnEnemyDamaged;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnAnyEnemyReceivedDamage -= this.OnEnemyDamaged;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner) Owner.OnAnyEnemyReceivedDamage -= this.OnEnemyDamaged;
            base.OnDestroy();
        }
    }
}
