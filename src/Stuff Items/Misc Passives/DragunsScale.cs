using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    class DragunsScale : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Dragun Scale";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/dragunscale_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<DragunsScale>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Burning Rage";
            string longDesc = "This shelldrake scale is full of heat energy, that may be released in a fiery inferno if certain conditions are met.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.B;
        }
        GameActorFireEffect fireEffect = Gungeon.Game.Items["hot_lead"].GetComponent<BulletStatusEffectItem>().FireModifierEffect;
        private void IgniteAll(PlayerController user)
        {
            List<AIActor> activeEnemies = user.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            if (activeEnemies != null)
            {
                for (int i = 0; i < activeEnemies.Count; i++)
                {
                    AIActor aiactor = activeEnemies[i];
                    if (aiactor.IsNormalEnemy)
                    {
                        aiactor.gameActor.ApplyEffect(this.fireEffect, 1f, null);
                    }
                }
            }
        }
        private void HandleActiveItemUsed(PlayerController arg1, PlayerItem arg2)
        {
            IgniteAll(Owner);
        }
        private void CheckForSynergy(FlippableCover obj)
        {
            if (Owner.HasPickupID(666))
            {
                IgniteAll(Owner);
            }
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnReceivedDamage += this.IgniteAll;
            player.OnUsedPlayerItem += this.HandleActiveItemUsed;
            player.OnTableFlipped = (Action<FlippableCover>)Delegate.Combine(player.OnTableFlipped, new Action<FlippableCover>(this.CheckForSynergy));
            bool hasntAlreadyBeenCollected = !this.m_pickedUpThisRun;
            if (hasntAlreadyBeenCollected)
            {
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, player);
            }
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnReceivedDamage -= this.IgniteAll;
            player.OnUsedPlayerItem -= this.HandleActiveItemUsed;
            player.OnTableFlipped = (Action<FlippableCover>)Delegate.Remove(player.OnTableFlipped, new Action<FlippableCover>(this.CheckForSynergy));
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.OnReceivedDamage -= this.IgniteAll;
                Owner.OnUsedPlayerItem -= this.HandleActiveItemUsed;
                Owner.OnTableFlipped = (Action<FlippableCover>)Delegate.Remove(Owner.OnTableFlipped, new Action<FlippableCover>(this.CheckForSynergy));
            }
            base.OnDestroy();
        }
    }
}
