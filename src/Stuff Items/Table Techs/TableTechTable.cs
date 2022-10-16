using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Dungeonator;
using ItemAPI;

namespace NevernamedsItems
{
    public class TableTechTable : TableFlipItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Table Tech Table";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/tabletechtable_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<TableTechTable>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Flip Recursion";
            string longDesc = "This ancient technique has a chance to create a new table whenever a table is flipped." + "\n\nChapter 8 of the \"Tabla Sutra.\" Flip unto flip unto flip unto flip unto flip unto flip unto flip unto flip unto flip. Never stop flipping.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item


            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.C;


        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnTableFlipped = (Action<FlippableCover>)Delegate.Combine(player.OnTableFlipped, new Action<FlippableCover>(this.SpeedEffect));
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject result = base.Drop(player);
            player.OnTableFlipped = (Action<FlippableCover>)Delegate.Remove(player.OnTableFlipped, new Action<FlippableCover>(this.SpeedEffect));
            return result;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.OnTableFlipped -= SpeedEffect;
            }
            base.OnDestroy();
        }

        public FlippableCover TableToSpawn;
        private void SpeedEffect(FlippableCover obj)
        {
            if (UnityEngine.Random.value < .50f)
            {
                Vector2 nearbyPoint = Owner.CenterPosition + (Owner.unadjustedAimPoint.XY() - Owner.CenterPosition).normalized;
                IntVector2? nearestAvailableCell = Owner.CurrentRoom.GetNearestAvailableCell(nearbyPoint, new IntVector2?(IntVector2.One), new CellTypes?(CellTypes.FLOOR), false, null);
                FoldingTableItem component6 = PickupObjectDatabase.GetById(644).GetComponent<FoldingTableItem>();
                GameObject gameObject5 = component6.TableToSpawn.gameObject;
                GameObject gameObject6 = UnityEngine.Object.Instantiate<GameObject>(gameObject5.gameObject, nearestAvailableCell.Value.ToVector2(), Quaternion.identity);
                SpeculativeRigidbody componentInChildren = gameObject6.GetComponentInChildren<SpeculativeRigidbody>();
                FlippableCover component7 = gameObject6.GetComponent<FlippableCover>();
                component7.transform.position.XY().GetAbsoluteRoom().RegisterInteractable(component7);
                component7.ConfigureOnPlacement(component7.transform.position.XY().GetAbsoluteRoom());
                componentInChildren.Initialize();
                PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(componentInChildren, null, false);
            }
        }
    }
}
