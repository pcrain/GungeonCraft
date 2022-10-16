using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using ItemAPI;
using Alexandria.DungeonAPI;

namespace NevernamedsItems
{
    class Pestilence : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Pestilence";
            string resourceName = "NevernamedsItems/Resources/pestilence_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Pestilence>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Yersinia Bestis";
            string longDesc = "Afflicts one enemy per room with a deadly plague." + "\n\nA tiny bundle of DNA molecules that directly attacks the shells that make up Gundead bodies.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;
            item.AddToSubShop(ItemBuilder.ShopType.Goopton);
        }

        public override void Pickup(PlayerController player)
        {
            player.OnEnteredCombat += this.OnEnteredCombat;
            base.Pickup(player);
        }

        public override DebrisObject Drop(PlayerController player)
        {
            player.OnEnteredCombat -= this.OnEnteredCombat;
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner) Owner.OnEnteredCombat -= this.OnEnteredCombat;
            base.OnDestroy();
        }
        private void OnEnteredCombat()
        {
            if (Owner)
            {
                if (Owner.PlayerHasActiveSynergy("Multimorbidities"))
                {
                    List<AIActor> enemies = Owner.CurrentRoom.GetXEnemiesInRoom(2, true, true);
                    foreach (AIActor enemy in enemies)
                    {
                        enemy.ApplyEffect(StaticStatusEffects.StandardPlagueEffect);
                    }
                }
                else
                {
                    AIActor randomActiveEnemy = Owner.CurrentRoom.GetRandomActiveEnemy(false);
                    if (randomActiveEnemy && randomActiveEnemy.healthHaver)
                    {
                        randomActiveEnemy.ApplyEffect(StaticStatusEffects.StandardPlagueEffect);
                    }
                }
            }
        }
    }
}
