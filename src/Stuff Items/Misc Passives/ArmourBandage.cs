using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace NevernamedsItems
{
    public class ArmourBandage : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Armour Bandage";
            string resourceName = "NevernamedsItems/Resources/armourbandage_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<ArmourBandage>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Hurtful Heals";
            string longDesc = "Taking damage to armour heals half a heart." + "\n\nA simple recipe for recycling broken armour into medical supplies.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.D;
            ArmourBandageID = item.PickupObjectId;
            item.ArmorToGainOnInitialPickup = 1;
        }
        public static int ArmourBandageID;
        public override void Pickup(PlayerController player)
        {
            player.LostArmor = (Action)Delegate.Combine(player.LostArmor, new Action(this.OnLostArmor));
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.LostArmor = (Action)Delegate.Remove(player.LostArmor, new Action(this.OnLostArmor));
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.LostArmor = (Action)Delegate.Remove(Owner.LostArmor, new Action(this.OnLostArmor));
            }
            base.OnDestroy();
        }
        private void OnLostArmor()
        {
            if (Owner)
            {
                Owner.healthHaver.ApplyHealing(0.5f);
                AkSoundEngine.PostEvent("Play_OBJ_heart_heal_01", Owner.gameObject);
                Owner.PlayEffectOnActor((PickupObjectDatabase.GetById(73).GetComponent<HealthPickup>().healVFX), Vector3.zero, true, false, false);
            }
        }
    }
}
