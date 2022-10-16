using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    class PurpleProse : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Purple Prose";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/purpleprose_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<PurpleProse>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Pity Is A Powerful Weapon";
            string longDesc = "Charms all enemies in the room upon taking damage."+"\n\nA beautiful rose grown in some long lost floral garden deep within the gungeon. As you peel away it's petals, you find more inside, with seemingly no end. \n\nI guess you'll never find out if 'she loves you'.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.D;
        }
        GameActorCharmEffect charmEffect = Gungeon.Game.Items["charming_rounds"].GetComponent<BulletStatusEffectItem>().CharmModifierEffect;
        private void charmAll(PlayerController user)
        {
            List<AIActor> activeEnemies = user.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            
            if (activeEnemies != null)
            {
                for (int i = 0; i < activeEnemies.Count; i++)
                {
                    AIActor aiactor = activeEnemies[i];
                    if (aiactor.IsNormalEnemy)
                    {                      
                        aiactor.gameActor.ApplyEffect(this.charmEffect, 1f, null);
                    }
                }
            }
        }    
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnReceivedDamage += this.charmAll;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnReceivedDamage -= this.charmAll;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {

            Owner.OnReceivedDamage -= this.charmAll;
            }
            base.OnDestroy();
        }
    }
}
