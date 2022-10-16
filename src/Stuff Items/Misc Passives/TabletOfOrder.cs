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
    public class TabletOfOrder : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Tablet Of Order";
            string resourceName = "NevernamedsItems/Resources/tabletoforder_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<TabletOfOrder>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Everything In It's Place";
            string longDesc = "Buffs enemies, but removes their ability to call in reinforcements." + "\n\nAn ancient magical artefact once used by the Order of the True Gun to quell dissent in their ranks.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.B;

            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.CHALLENGE_WHATARMY_BEATEN, true);
        }
        private RoomHandler lastCheckedRoom;
        public override void Update()
        {
            if (Owner && Owner.CurrentRoom != null)
            {
                if (Owner.CurrentRoom != lastCheckedRoom)
                {
                    Owner.CurrentRoom.ClearReinforcementLayers();
                    lastCheckedRoom = Owner.CurrentRoom;
                }
            }
            base.Update();
        }
        public void AIActorMods(AIActor target)
        {
            if (target && target.healthHaver && !target.healthHaver.IsBoss)
            {
                float premax = target.healthHaver.GetMaxHealth();
                target.healthHaver.SetHealthMaximum(premax * 1.5f);
                target.healthHaver.ForceSetCurrentHealth(premax * 1.5f);
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