using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using Dungeonator;

namespace NevernamedsItems
{
    public class Citrinitas : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Citrinitas";
            string resourceName = "NevernamedsItems/Resources/citrinitas_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Citrinitas>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Wisdom";
            string longDesc = "Entering secret rooms grants fortune, and permanent upgrades."+"\n\nThe third phase of the Philosopher's Stone formation process, in which Solar Wisdom drives away the Lunar Energy of the Albedo.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;
            item.AddToSubShop(ItemBuilder.ShopType.Goopton);
        }
        private RoomHandler lastRoom;
        private List<RoomHandler> previouslyEnteredRooms = new List<RoomHandler>();
        private void GiveRandomPermanentStatBuff()
        {
            int random = UnityEngine.Random.Range(1, 10);
            StatModifier statModifier = new StatModifier();         
            switch (random)
            {
                case 1:
                    statModifier.amount = 1.1f;
                    statModifier.modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE;
                    statModifier.statToBoost = PlayerStats.StatType.Damage;
                    break;
                case 2:
                    statModifier.amount = 1.1f;
                    statModifier.modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE;
                    statModifier.statToBoost = PlayerStats.StatType.RateOfFire;
                    break;
                case 3:
                    statModifier.amount = 1.1f;
                    statModifier.modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE;
                    statModifier.statToBoost = PlayerStats.StatType.MovementSpeed;
                    break;
                case 4:
                    statModifier.amount = 1.1f;
                    statModifier.modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE;
                    statModifier.statToBoost = PlayerStats.StatType.ProjectileSpeed;
                    break;
                case 5:
                    statModifier.amount = 0.9f;
                    statModifier.modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE;
                    statModifier.statToBoost = PlayerStats.StatType.ReloadSpeed;
                    break;
                case 6:
                    statModifier.amount = 0.9f;
                    statModifier.modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE;
                    statModifier.statToBoost = PlayerStats.StatType.Accuracy;
                    break;
                case 7:
                    statModifier.amount = 1.1f;
                    statModifier.modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE;
                    statModifier.statToBoost = PlayerStats.StatType.AdditionalClipCapacityMultiplier;
                    break;
                case 8:
                    statModifier.amount = 1.1f;
                    statModifier.modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE;
                    statModifier.statToBoost = PlayerStats.StatType.AmmoCapacityMultiplier;
                    break;
                case 9:
                    statModifier.amount = 1f;
                    statModifier.modifyType = StatModifier.ModifyMethod.ADDITIVE;
                    statModifier.statToBoost = PlayerStats.StatType.Coolness;
                    break;
            }
            //Debug.Log("Citrinitas: Added stat " + statModifier.statToBoost);
            Owner.ownerlessStatModifiers.Add(statModifier);
            Owner.stats.RecalculateStats(Owner, false, false);
        }
        public override void Update()
        {
            if (Owner)
            {
                if (Owner.CurrentRoom != null && !string.IsNullOrEmpty(Owner.CurrentRoom.GetRoomName()))
                {
                    if (Owner.CurrentRoom != lastRoom)
                    {
                    if (Owner.CurrentRoom.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.SECRET)
                    {
                        if (!previouslyEnteredRooms.Contains(Owner.CurrentRoom))
                        {
                            IntVector2 location = Owner.CurrentRoom.GetCenteredVisibleClearSpot(2, 2);
                            LootEngine.SpawnCurrency(location.ToVector2(), UnityEngine.Random.Range(20, 51));
                            GiveRandomPermanentStatBuff();
                            previouslyEnteredRooms.Add(Owner.CurrentRoom);
                        }
                    }
                        lastRoom = Owner.CurrentRoom;
                    }
                }
            }
            else { return; }
        }
    }
}
