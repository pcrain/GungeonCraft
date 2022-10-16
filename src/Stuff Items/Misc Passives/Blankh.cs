using UnityEngine;
using ItemAPI;
using Dungeonator;
using System.Collections;
using System;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Gungeon;
using System.Collections.Generic;


namespace NevernamedsItems
{
    public class Blankh : BlankModificationItem
    {
        public static void Init()
        {
            string itemName = "Blankh";
            string resourceName = "NevernamedsItems/Resources/blankh_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Blankh>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "My Body Is A Temple";
            string longDesc = "Gives ones' body to Kaliber in order to recieve her bullet-banishing blessings."+"\n\nTriggered by attempting to Blank with no Blanks remaining.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.A;

            item.AddToSubShop(ItemBuilder.ShopType.OldRed);
            item.AddToSubShop(ItemBuilder.ShopType.Cursula);

            BlankhID = item.PickupObjectId;
        }

        private static int BlankhID;

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
        }

        private static Hook BlankHook = new Hook(
    typeof(PlayerController).GetMethod("DoConsumableBlank", BindingFlags.Instance | BindingFlags.NonPublic),
    typeof(Blankh).GetMethod("TriggerHealthBlank", BindingFlags.Static | BindingFlags.Public)
);

        public static void TriggerHealthBlank(Action<PlayerController> orig, PlayerController user)
        {
            if (user.Blanks <= 0 && user.HasPickupID(BlankhID))
            {
                if(user.characterIdentity == OMITBChars.Shade)
                {
                    if (user.carriedConsumables.Currency > 15)
                    {
                        user.carriedConsumables.Currency -= 15;
                        user.ForceBlank(25f, 0.5f, false, true, null, true, -1f);
                    }
                }
                else
                {
                    if (user.healthHaver && user.healthHaver.GetCurrentHealth() > 0.5f)
                    {
                        user.healthHaver.ApplyHealing(-0.5f);
                        user.ForceBlank(25f, 0.5f, false, true, null, true, -1f);
                    }
                    else
                    {
                        if (user.ForceZeroHealthState && user.healthHaver.Armor > 1)
                        {
                            user.healthHaver.Armor -= 1;
                            user.ForceBlank(25f, 0.5f, false, true, null, true, -1f);
                        }
                        else if ((user.healthHaver.Armor > 0 && user.healthHaver.GetCurrentHealth() > 0) || user.healthHaver.Armor > 1)
                        {
                            user.healthHaver.Armor -= 1;
                            user.ForceBlank(25f, 0.5f, false, true, null, true, -1f);
                        }
                    }
                }
            }
            orig(user);
        }

        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            return debrisObject;
        }
    }
}

