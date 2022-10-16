using System.Text;
using UnityEngine;
using ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;
using System.Collections.Generic;
using Dungeonator;

namespace NevernamedsItems
{
    public class CartographersEye : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Cartographers Eye";
            string resourceName = "NevernamedsItems/Resources/cartographerseye_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<CartographersEye>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Shows the Way";
            string longDesc = "Grants vision of important rooms." + "\nGrants access to a randomly selected special room." + "\n\nCreated by legendary cartographer Woban to guide him in his old age as his vision failed.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.CanBeDropped = true;
            item.quality = PickupObject.ItemQuality.B;
            CartographersEyeID = item.PickupObjectId;
        }
        public static int CartographersEyeID;
        private void OnFloorLoaded()
        {
            int pickedType = UnityEngine.Random.Range(1, 4);
            for (int i = 0; i < GameManager.Instance.Dungeon.data.rooms.Count; i++)
            {
                RoomHandler roomHandler = GameManager.Instance.Dungeon.data.rooms[i];
                if (roomHandler != null)
                {
                    if (roomHandler.IsShop)
                    {
                        roomHandler.RevealedOnMap = true;
                        Minimap.Instance.RevealMinimapRoom(roomHandler, true, false, roomHandler == GameManager.Instance.PrimaryPlayer.CurrentRoom);
                        if (pickedType == 1) roomHandler.visibility = RoomHandler.VisibilityStatus.VISITED;
                    }
                    else if (roomHandler.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.REWARD)
                    {
                        roomHandler.RevealedOnMap = true;
                        Minimap.Instance.RevealMinimapRoom(roomHandler, true, false, roomHandler == GameManager.Instance.PrimaryPlayer.CurrentRoom);
                        if (pickedType == 2) roomHandler.visibility = RoomHandler.VisibilityStatus.VISITED;
                    }
                    else if (!string.IsNullOrEmpty(roomHandler.GetRoomName()) && roomHandler.GetRoomName().Contains("Boss Foyer"))
                    {
                        roomHandler.RevealedOnMap = true;
                        Minimap.Instance.RevealMinimapRoom(roomHandler, true, false, roomHandler == GameManager.Instance.PrimaryPlayer.CurrentRoom);
                        if (pickedType == 3 && GameManager.Instance.Dungeon.tileIndices.tilesetId != GlobalDungeonData.ValidTilesets.HELLGEON) roomHandler.visibility = RoomHandler.VisibilityStatus.VISITED;
                    }
                }
            }
            Minimap.Instance.m_shouldBuildTilemap = true;
        }
        public override void Pickup(PlayerController player)
        {
            if (!this.m_pickedUpThisRun)
            {
                OnFloorLoaded();
            }
            GameManager.Instance.OnNewLevelFullyLoaded += this.OnFloorLoaded;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            GameManager.Instance.OnNewLevelFullyLoaded -= this.OnFloorLoaded;

            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            GameManager.Instance.OnNewLevelFullyLoaded -= this.OnFloorLoaded;

            base.OnDestroy();
        }

    }
}
