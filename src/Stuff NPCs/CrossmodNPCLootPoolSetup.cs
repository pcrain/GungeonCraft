using Gungeon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LootTableAPI;
using UnityEngine;

namespace NevernamedsItems
{
    class CrossmodNPCLootPoolSetup
    {
        public static void CheckItems()
        {
            foreach (string text in Game.Items.IDs)
            {
                if (Game.Items[text.Replace("gungeon:", "")] != null && Game.Items[text.Replace("gungeon:", "")] is PickupObject)
                {
                    PickupObject item = Game.Items[text.Replace("gungeon:", "")];
                    foreach (Component component in item.GetComponents<Component>())
                    {
                        if (component.GetType().ToString().Contains("BoomhildrItemPool"))
                        {
                            Boomhildr.BoomhildrLootTable.AddItemToPool(item.PickupObjectId);
                        }
                        else if (component.GetType().ToString().Contains("IronsideItemPool"))
                        {
                            Ironside.IronsideLootTable.AddItemToPool(item.PickupObjectId);
                        }
                        else if (component.GetType().ToString().Contains("RustyItemPool"))
                        {
                            Rusty.RustyLootTable.AddItemToPool(item.PickupObjectId);
                        }
                    }
                }
            }
        }
    }
}
