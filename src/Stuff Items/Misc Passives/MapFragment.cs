using System.Text;
using UnityEngine;
using ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;
using System.Collections.Generic;
using Dungeonator;

namespace NevernamedsItems
{
    public class MapFragment : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Map Fragment";
            string resourceName = "NevernamedsItems/Resources/mapfragment_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<MapFragment>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Selective Information";
            string longDesc = "Reveals nearby rooms." + "\n\nSeemingly torn from a larger map.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.CanBeDropped = true;
            item.CustomCost = 20;
            item.UsesCustomCost = true;
            item.quality = PickupObject.ItemQuality.D;
            MapFragmentID = item.PickupObjectId;
        }
        public static int MapFragmentID;
        public RoomHandler lastRoom;
        public override void Update()
        {
            if (Owner && Owner.CurrentRoom != null)
            {
                if (Owner.CurrentRoom != lastRoom)
                {
                    foreach (RoomHandler adjacentRoom in Owner.CurrentRoom.connectedRooms)
                    {
                        if (!adjacentRoom.IsSecretRoom || Owner.PlayerHasActiveSynergy("Trust In The All-Seeing"))
                        {
                            Minimap.Instance.RevealMinimapRoom(adjacentRoom, true, true, false);
                            if (Owner.PlayerHasActiveSynergy("Restoration"))
                            {
                                foreach (RoomHandler adjacentRoom2 in adjacentRoom.connectedRooms)
                                {
                                    if (!adjacentRoom.IsSecretRoom || Owner.PlayerHasActiveSynergy("Trust In The All-Seeing"))
                                    {
                                        Minimap.Instance.RevealMinimapRoom(adjacentRoom2, true, true, false);

                                    }
                                }
                            }
                        }
                        lastRoom = Owner.CurrentRoom;
                    }
                }
                base.Update();
            }
        }

    }
}
