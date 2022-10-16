using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;
using SaveAPI;

namespace NevernamedsItems
{
    public class ArmouredArmour : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Armoured Armour";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/armouredarmour_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<ArmouredArmour>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Why ARE they shields?";
            string longDesc = "Chance to double armour pickups." + "\n\nA suit of armour made out of smaller, less effective pieces of armour." + "\nIt's genius!";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item


            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.B;
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PLAYERHELDMORETHANFIVEARMOUR, true);
            ArmouredArmourID = item.PickupObjectId;
        }
        public static int ArmouredArmourID;
        Hook healthPickupHook = new Hook(
                typeof(HealthPickup).GetMethod("Pickup", BindingFlags.Instance | BindingFlags.Public),
                typeof(ArmouredArmour).GetMethod("heartPickupHookMethod")
            );
        public static void heartPickupHookMethod(Action<HealthPickup, PlayerController> orig, HealthPickup self, PlayerController player)
        {
            orig(self, player);
            if (self.armorAmount > 0)
            {
                if (player.HasPickupID(Gungeon.Game.Items["nn:armoured_armour"].PickupObjectId))
                {
                    if (canGiveArmor)
                    {
                        float procChance = 0.5f;
                        if (player.PlayerHasActiveSynergy("Armoured Armoured Armour")) procChance = 0.75f;
                        if (UnityEngine.Random.value >= procChance)
                        {
                            player.healthHaver.Armor += 1;
                        }
                        canGiveArmor = false;
                    }
                    else
                    {
                        canGiveArmor = true;
                    }
                }
            }
        }
        static bool canGiveArmor = false;
        public override void Pickup(PlayerController player)
        {
            bool hasntAlreadyBeenCollected = !this.m_pickedUpThisRun;
            if (hasntAlreadyBeenCollected)
            {
                player.healthHaver.Armor += 1;
            }
            base.Pickup(player);
        }
    }
}