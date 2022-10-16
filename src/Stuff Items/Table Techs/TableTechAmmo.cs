using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Dungeonator;
using System.Collections.ObjectModel;
using ItemAPI;

namespace NevernamedsItems
{
    public class TableTechAmmo : TableFlipItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Table Tech Ammo";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/tabletechammo_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<TableTechAmmo>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Flip Replenishment";
            string longDesc = "Grants a small amount of ammo each time a table is flipped." + "\n\nChapter 10 of the \"Tabla Sutra.\" And he who flips shall never be hungry, for he will always have the flip within his heart.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //SYNERGY WITH AMMO BELT --> Tabletop
            List<string> mandatorySynergyItems = new List<string>() { "nn:table_tech_ammo", "ammo_belt" };
            CustomSynergies.Add("Tabletop", mandatorySynergyItems);

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.C;
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnTableFlipped = (Action<FlippableCover>)Delegate.Combine(player.OnTableFlipped, new Action<FlippableCover>(this.SpeedEffect));
            player.OnTableFlipCompleted = (Action<FlippableCover>)Delegate.Combine(player.OnTableFlipCompleted, new Action<FlippableCover>(this.DoEffectCompleted));
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject result = base.Drop(player);
            player.OnTableFlipped = (Action<FlippableCover>)Delegate.Remove(player.OnTableFlipped, new Action<FlippableCover>(this.SpeedEffect));
            player.OnTableFlipCompleted = (Action<FlippableCover>)Delegate.Remove(player.OnTableFlipCompleted, new Action<FlippableCover>(this.DoEffectCompleted));
            return result;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.OnTableFlipped -= SpeedEffect;
                Owner.OnTableFlipCompleted -= TableFlipCompleted;
            }
            base.OnDestroy();
        }

        private void SpeedEffect(FlippableCover obj)
        {
            int ammoToGive = -1;
            if (Owner.HasPickupID(134)) ammoToGive = UnityEngine.Random.Range(5, 10);
            else ammoToGive = UnityEngine.Random.Range(1, 5);
            Owner.CurrentGun.GainAmmo(ammoToGive);
        }
        public bool TableFlockingYes = true;
        private void TableFlipCompleted(FlippableCover obj)
        {
            this.DoTableFlocking(obj);
        }
        private void DoTableFlocking(FlippableCover table)
        {
            if (this.TableFlockingYes)
            {
                RoomHandler currentRoom = base.Owner.CurrentRoom;
                ReadOnlyCollection<IPlayerInteractable> roomInteractables = currentRoom.GetRoomInteractables();
                for (int i = 0; i < roomInteractables.Count; i++)
                {
                    if (currentRoom.IsRegistered(roomInteractables[i]))
                    {
                        FlippableCover flippableCover = roomInteractables[i] as FlippableCover;
                        if (flippableCover != null && !flippableCover.IsFlipped && !flippableCover.IsGilded)
                        {
                            if (flippableCover.flipStyle == FlippableCover.FlipStyle.ANY)
                            {
                                flippableCover.ForceSetFlipper(base.Owner);
                                flippableCover.Flip(table.DirectionFlipped);
                            }
                            else if (flippableCover.flipStyle == FlippableCover.FlipStyle.ONLY_FLIPS_LEFT_RIGHT)
                            {
                                if (table.DirectionFlipped == DungeonData.Direction.NORTH || table.DirectionFlipped == DungeonData.Direction.SOUTH)
                                {
                                    flippableCover.ForceSetFlipper(base.Owner);
                                    flippableCover.Flip((UnityEngine.Random.value <= 0.5f) ? DungeonData.Direction.WEST : DungeonData.Direction.EAST);
                                }
                                else
                                {
                                    flippableCover.ForceSetFlipper(base.Owner);
                                    flippableCover.Flip(table.DirectionFlipped);
                                }
                            }
                            else if (flippableCover.flipStyle == FlippableCover.FlipStyle.ONLY_FLIPS_UP_DOWN)
                            {
                                if (table.DirectionFlipped == DungeonData.Direction.EAST || table.DirectionFlipped == DungeonData.Direction.WEST)
                                {
                                    flippableCover.ForceSetFlipper(base.Owner);
                                    flippableCover.Flip((UnityEngine.Random.value <= 0.5f) ? DungeonData.Direction.SOUTH : DungeonData.Direction.NORTH);
                                }
                                else
                                {
                                    flippableCover.ForceSetFlipper(base.Owner);
                                    flippableCover.Flip(table.DirectionFlipped);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

//user.CurrentGun.GainAmmo(5);