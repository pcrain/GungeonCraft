using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using Dungeonator;
using SaveAPI;

namespace NevernamedsItems
{
    public class GoomperorsCrown : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Goomperors Crown";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/goomperorscrown_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<GoomperorsCrown>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "The Slime Must Flow";
            string longDesc = "The crown of the ancient Blobulonian emperor Gool Atinous." + "\n\nChance to slow down entire rooms!";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.S;
            item.AddToSubShop(ItemBuilder.ShopType.Goopton);

            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_GOOMPERORSCROWN, true);
            item.AddItemToGooptonMetaShop(30);
        }
        public void AIActorMods(AIActor target)
        {
            if (ShouldSlowThisRoom)
            {
                if (target && target.aiActor && target.aiActor.EnemyGuid != null)
                {
                    ApplyDirectStatusEffects.ApplyDirectSlow(target, 10000000000f, 0.75f, Color.white, Color.white, EffectResistanceType.None, "Goomperors Crown", false, false);
                }
            }
        }
        public bool ShouldSlowThisRoom;
        public void onEnteredCombat()
        {
            if (UnityEngine.Random.value < 0.25f) ShouldSlowThisRoom = true;
            else ShouldSlowThisRoom = false;
            if (ShouldSlowThisRoom)
            {
                List<AIActor> activeEnemies = Owner.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                if (activeEnemies != null)
                {
                    for (int i = 0; i < activeEnemies.Count; i++)
                    {
                        AIActor aiactor = activeEnemies[i];
                        if (aiactor.IsNormalEnemy)
                        {
                            ApplyDirectStatusEffects.ApplyDirectSlow(aiactor.gameActor, 10000000000f, 0.75f, Color.white, Color.white, EffectResistanceType.None, "Goomperors Crown", false, false);
                        }
                    }
                }
            }
        }
        private void onRoomCleared(PlayerController player)
        {
            ShouldSlowThisRoom = false;
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            ETGMod.AIActor.OnPreStart += AIActorMods;
            player.OnEnteredCombat += this.onEnteredCombat;
            player.OnRoomClearEvent += this.onRoomCleared;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            ETGMod.AIActor.OnPreStart -= AIActorMods;
            player.OnEnteredCombat -= this.onEnteredCombat;
            player.OnRoomClearEvent -= this.onRoomCleared;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            ETGMod.AIActor.OnPreStart -= AIActorMods;
            base.OnDestroy();
        }
    }
}
