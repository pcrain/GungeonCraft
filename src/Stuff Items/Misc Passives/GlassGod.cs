using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    public class GlassGod : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Glass God";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/glassgod_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<GlassGod>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Fragile Divinity";
            string longDesc = "Grants great power, but shatters upon it's bearer taking damage."+"\n\nThe emblem of the Lady of Pane's greatest champion, a shining titan known only as 'The Glass One'. After his fall in battle, outnumbered ten to one, his crest somehow made it's way to the Gungeon.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Coolness, 5, StatModifier.ModifyMethod.ADDITIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Damage, 1.5f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.ReloadSpeed, 0.5f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.MovementSpeed, 1.2f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalItemCapacity, 5f, StatModifier.ModifyMethod.ADDITIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AmmoCapacityMultiplier, 1.5f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.RateOfFire, 1.5f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.ProjectileSpeed, 1.5f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Accuracy, 0.5f, StatModifier.ModifyMethod.MULTIPLICATIVE);

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.S;
        }
        private void breakItem(PlayerController player)
        {
            PickupObject byId = PickupObjectDatabase.GetById(565);
            player.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
            player.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
            player.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
            player.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
            player.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
            Owner.RemovePassiveItem(this.PickupObjectId);
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnReceivedDamage += this.breakItem;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnReceivedDamage -= this.breakItem;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner) Owner.OnReceivedDamage -= this.breakItem;
            base.OnDestroy();
        }

    }
}
