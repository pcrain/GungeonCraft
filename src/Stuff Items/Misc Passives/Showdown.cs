using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using Dungeonator;

namespace NevernamedsItems
{
    public class Showdown : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Showdown";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/showdown_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<Showdown>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Now it's just you and me...";
            string longDesc = "Prevents bosses from being able to spawn additional backup." + "\n\nAn icon of the one-on-one gunfights of days gone by.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.D; 
        }
        public void AIActorMods(AIActor target)
        {
            if (!target.healthHaver.IsBoss)
            {
                if (target.GetAbsoluteParentRoom().area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS && (!target.IsHarmlessEnemy || target.EnemyGuid == EnemyGuidDatabase.Entries["mine_flayers_claymore"]) && target.GetAbsoluteParentRoom().IsSealed)
                {
                    if (Owner.PlayerHasActiveSynergy("Frenemies"))
                    {
                        target.ApplyEffect(GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultPermanentCharmEffect, 1f, null);
                        target.IsHarmlessEnemy = true;
                    }
                    else
                    {
                        target.EraseFromExistenceWithRewards(true);
                    }
                    if (Owner.PlayerHasActiveSynergy("Dirty Tricks"))
                    {
                        List<AIActor> activeEnemies = target.GetAbsoluteParentRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                        if (activeEnemies != null)
                        {
                            for (int i = 0; i < activeEnemies.Count; i++)
                            {
                                AIActor aiactor = activeEnemies[i];
                                if (aiactor.healthHaver.IsBoss)
                                {
                                    aiactor.healthHaver.ApplyDamage(50, Vector2.zero, "Dirty Tricks", CoreDamageTypes.None, DamageCategory.Unstoppable, true, null, true);
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            ETGMod.AIActor.OnPreStart += AIActorMods;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            ETGMod.AIActor.OnPreStart -= AIActorMods;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            ETGMod.AIActor.OnPreStart -= AIActorMods;
            base.OnDestroy();
        }
    }
}

