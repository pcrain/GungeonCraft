using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    class KeyBulletEffigy : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Keybullet Effigy";
            string resourceName = "NevernamedsItems/Resources/keybulleteffigy_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<KeyBulletEffigy>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Aimgels on High";
            string longDesc = "Keybullet Kin drop bonus keys." + "\n\nA holy item from a historical sect of Gun Cultists that worshipped Keybullet Kin as Aimgels of Kaliber, sent down from Bullet Heaven to deliver holy gifts.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.D;
            item.AddToSubShop(ItemBuilder.ShopType.Cursula);
            item.AddToSubShop(ItemBuilder.ShopType.Flynt);
            KeybulletEffigyID = item.PickupObjectId;
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.KILLEDJAMMEDKEYBULLETKIN, true);
        }
        public static int KeybulletEffigyID;
        private void OnPreSpawn(AIActor actor)
        {
            if (Owner && actor && (actor.EnemyGuid == "699cd24270af4cd183d671090d8323a1" || actor.EnemyGuid == "a446c626b56d4166915a4e29869737fd"))
            {
                actor.AdditionalSafeItemDrops.Add(PickupObjectDatabase.GetById(67));
                if (Owner.PlayerHasActiveSynergy("Spare Kin")) actor.AdditionalSafeItemDrops.Add(PickupObjectDatabase.GetById(67));
            }
        }
        private void OnEnteredCombat()
        {
            if (Owner && UnityEngine.Random.value <= 0.015f)
            {
                var KeyBulletKin = EnemyDatabase.GetOrLoadByGuid("699cd24270af4cd183d671090d8323a1");
                IntVector2? spawnPos = Owner.CurrentRoom.GetRandomVisibleClearSpot(1, 1);
                if (spawnPos != null)
                {
                    AIActor TargetActor = AIActor.Spawn(KeyBulletKin.aiActor, spawnPos.Value, GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(spawnPos.Value), true, AIActor.AwakenAnimationType.Default, true);
                    PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(TargetActor.specRigidbody, null, false);
                    TargetActor.HandleReinforcementFallIntoRoom(0f);
                }
            }
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            ETGMod.AIActor.OnPreStart += this.OnPreSpawn;
            player.OnEnteredCombat += this.OnEnteredCombat;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnEnteredCombat -= this.OnEnteredCombat;
            ETGMod.AIActor.OnPreStart -= this.OnPreSpawn;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner) Owner.OnEnteredCombat -= this.OnEnteredCombat;
            ETGMod.AIActor.OnPreStart -= this.OnPreSpawn;
            base.OnDestroy();
        }
    }
}
