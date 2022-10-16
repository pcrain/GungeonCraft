using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;
using SaveAPI;

namespace NevernamedsItems
{
    public class CursedTumbler : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Cursed Tumbler";
            string resourceName = "NevernamedsItems/Resources/cursedtumbler_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<CursedTumbler>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "What's In The Box?";
            string longDesc = "Gives chests a chance to be Jammed."+"\n\nAn 4th dimensional set of mechanisms designed to lock away all evil in this world.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);
            item.CanBeDropped = true;
            item.quality = PickupObject.ItemQuality.B;
           
            CursedTumblerID = item.PickupObjectId;
            item.SetupUnlockOnCustomStat(CustomTrackedStats.JAMMED_CHESTS_OPENED, 0, DungeonPrerequisite.PrerequisiteOperation.GREATER_THAN);

        }
        public static int CursedTumblerID;        
    }       
}