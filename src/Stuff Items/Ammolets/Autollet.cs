using System.Text;
using UnityEngine;
using ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;
using System.Collections.Generic;
using Dungeonator;

namespace NevernamedsItems
{
    public class Autollet : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Autollet";
            string resourceName = "NevernamedsItems/Resources/autollet_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Autollet>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Automatic and Effective";
            string longDesc = "Automatically triggers a free blank upon entering an unvisited room with enemies." + "\n\nThe end product of using science to reverse engineer the strange and esoteric Elder Blank.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.CanBeDropped = true;
            item.quality = PickupObject.ItemQuality.C;

            item.AddToSubShop(ItemBuilder.ShopType.OldRed);
            AutolletID = item.PickupObjectId;
        }
        public static int AutolletID;

        public List<RoomHandler> roomsVisitedThisFloor = new List<RoomHandler>() { };
        public RoomHandler lastRoom;
        public RoomHandler currentRoom;
        private void TriggerBlankIfAppropriate()
        {
            if (Owner.CurrentRoom.GetActiveEnemiesCount(RoomHandler.ActiveEnemyType.All) > 0 || Owner.PlayerHasActiveSynergy("Code Blanks"))
            {
                Owner.ForceBlank(45f, 0.5f, false, true, null, true, -1f);
            }
        }
        public override void Update()
        {
            if (Owner && Owner.CurrentRoom != null)
            {
                currentRoom = Owner.CurrentRoom;
                if (currentRoom != lastRoom)
                {
                    if (!roomsVisitedThisFloor.Contains(currentRoom))
                    {

                        TriggerBlankIfAppropriate();
                        roomsVisitedThisFloor.Add(currentRoom);

                    }
                    lastRoom = currentRoom;
                }
            }
            base.Update();
        }
        private void NewFloor()
        {
            roomsVisitedThisFloor.Clear();
        }
        public override void Pickup(PlayerController player)
        {
            GameManager.Instance.OnNewLevelFullyLoaded += this.NewFloor;
            base.Pickup(player);
        }

        public override DebrisObject Drop(PlayerController player)
        {
            GameManager.Instance.OnNewLevelFullyLoaded -= this.NewFloor;

            return base.Drop(player);
        }
    }

}
