using System;
using System.Collections.Generic;
using System.Linq;
using Gungeon;
using System.Text;

using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    public class TimeFuddlersRobe : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Time Fuddler's Robe";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/timefuddlersrobe_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<TimeFuddlersRobe>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Timeline Twister";
            string longDesc = "Chance to freeze time upon killing an enemy."+"\n\nThe robes of a young bullet who broke the timeline so badly that even his garments maintain an echo of the events.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.A;

            //SYNERGY WITH PIG OR BULLET TIME --> "Epsiode" 

            List<string> mandatorySynergyItems = new List<string>() { "nn:time_fuddler's_robe" };
            List<string> optionalSynergyItems = new List<string>() { "pig", "bullet_time" };
            CustomSynergies.Add("Epsiode", mandatorySynergyItems, optionalSynergyItems);

        }

        protected void activateSlow(PlayerController user)
        {
            var timeSlow = new RadialSlowInterface();
            timeSlow.DoesSepia = false;
            timeSlow.RadialSlowHoldTime = 3f;
            timeSlow.RadialSlowTimeModifier = 0.01f;
            timeSlow.DoRadialSlow(user.specRigidbody.UnitCenter, user.CurrentRoom); //or whatever
        }
        public override void OnDestroy()
        {
            if (Owner) Owner.OnAnyEnemyReceivedDamage -= this.OnEnemyDamaged;
            base.OnDestroy();
        }

        private void OnEnemyDamaged(float damage, bool fatal, HealthHaver enemyHealth)
        {
            if (enemyHealth.aiActor && enemyHealth && !enemyHealth.IsBoss && fatal && enemyHealth.aiActor && enemyHealth.aiActor.IsNormalEnemy)
            {
                if (Owner.HasPickupID(69) || Owner.HasPickupID(451))
                {
                    if (UnityEngine.Random.value < 0.35) activateSlow(Owner);
                }
                else
                {
                    if (UnityEngine.Random.value < 0.15) activateSlow(Owner);
                }
            }
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnAnyEnemyReceivedDamage += this.OnEnemyDamaged;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnAnyEnemyReceivedDamage -= this.OnEnemyDamaged;
            return debrisObject;
        }
    }
}
