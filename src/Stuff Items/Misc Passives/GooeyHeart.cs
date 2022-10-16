using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class GooeyHeart : PassiveItem
    {
        public static int GooeyHeartID;
        public static void Init()
        {
            //The name of the item
            string itemName = "Gooey Heart";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/gooeyheart_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<GooeyHeart>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Squelchy";
            string longDesc = "Chance to heal upon killing a blob." + "\n\nThe heart of the Mighty Blobulord, gained through showing enough skill to leave it intact throughout the entire fight." + "\n\nWatch as it jiggles";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.C;
            item.RemovePickupFromLootTables();

            GooeyHeartID = item.PickupObjectId;
            //item.AddAsChamberGunMastery("OnceMoreIntoTheBreach", 4);

        }
        public static List<string> blobEnemies = new List<string>()
        {
            EnemyGuidDatabase.Entries["blobulon"],
            EnemyGuidDatabase.Entries["blobuloid"],
            EnemyGuidDatabase.Entries["blobulin"],
            EnemyGuidDatabase.Entries["poisbulon"],
            EnemyGuidDatabase.Entries["poisbuloid"],
            EnemyGuidDatabase.Entries["poisbulin"],
            EnemyGuidDatabase.Entries["blizzbulon"],
            EnemyGuidDatabase.Entries["leadbulon"],
            EnemyGuidDatabase.Entries["bloodbulon"],
            EnemyGuidDatabase.Entries["cubulon"],
            EnemyGuidDatabase.Entries["chancebulon"],
            EnemyGuidDatabase.Entries["cubulead"],
            EnemyGuidDatabase.Entries["poopulon"],
            EnemyGuidDatabase.Entries["tiny_blobulord"],
            EnemyGuidDatabase.Entries["blobulord"],
        };
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnAnyEnemyReceivedDamage -= this.HandleHeal;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            Owner.OnAnyEnemyReceivedDamage -= this.HandleHeal;
            base.OnDestroy();
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnAnyEnemyReceivedDamage += this.HandleHeal;
        }
        private void HandleHeal(float damage, bool fatal, HealthHaver enemy)
        {
            if (blobEnemies.Contains(enemy.aiActor.EnemyGuid) && fatal == true)
            {
                if (UnityEngine.Random.value < 0.05f)
                {
                    if (Owner.ForceZeroHealthState)
                    {
                        if (UnityEngine.Random.value < 0.5f)
                        {
                            Owner.PlayEffectOnActor(ResourceCache.Acquire("Global VFX/vfx_healing_sparkles_001") as GameObject, Vector3.zero, true, false, false);
                            Owner.healthHaver.Armor = Owner.healthHaver.Armor + 1;
                        }
                    }
                    else
                    {
                        Owner.PlayEffectOnActor(ResourceCache.Acquire("Global VFX/vfx_healing_sparkles_001") as GameObject, Vector3.zero, true, false, false);
                        Owner.healthHaver.ApplyHealing(0.5f);
                    }
                }
            }
        }
    }
}
