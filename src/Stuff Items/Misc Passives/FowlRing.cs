using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    class FowlRing : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Fowl Ring";
            string resourceName = "NevernamedsItems/Resources/NeoItemSprites/fowlring_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<FowlRing>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Cluck Up";
            string longDesc = "One enemy per room becomes a chicken."+"\n\nA symbol of poultry affinity, manifesting one's most fowl desires.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.D;
            FowlRingID = item.PickupObjectId;
        }
        public static int FowlRingID;
        private void EnteredCombat()
        {
            if (Owner != null && Owner.CurrentRoom != null && Owner.CurrentRoom.HasActiveEnemies(RoomHandler.ActiveEnemyType.RoomClear))
            {
                AIActor enemy = Owner.CurrentRoom.GetRandomActiveEnemy(false);
                enemy.Transmogrify(EnemyDatabase.GetOrLoadByGuid(EnemyGuidDatabase.Entries["chicken"]), (GameObject)ResourceCache.Acquire("Global VFX/VFX_Item_Spawn_Poof"));
            }
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnEnteredCombat += this.EnteredCombat;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnEnteredCombat -= this.EnteredCombat;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.OnEnteredCombat -= this.EnteredCombat;
            }
            base.OnDestroy();
        }
    }
}