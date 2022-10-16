using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    public class Accelerant : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Accelerant";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/accelerant_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<Accelerant>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Burning Sensation";
            string longDesc = "Triples the damage dealt to enemies by fire."+"\n\nWatching their skin melt off is actually rather horrifying. Avert your eyes.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.D;
            item.AddToSubShop(ItemBuilder.ShopType.Goopton);
        }
        private DamageTypeModifier m_fireMultiplier;
        //private DamageTypeModifier m_poisonMultiplier;
        public void AIActorMods(AIActor target)
        {
            this.m_fireMultiplier = new DamageTypeModifier();
            this.m_fireMultiplier.damageMultiplier = 3f;
            this.m_fireMultiplier.damageType = CoreDamageTypes.Fire;
            target.healthHaver.damageTypeModifiers.Add(this.m_fireMultiplier);
            /*this.m_poisonMultiplier = new DamageTypeModifier();
            this.m_poisonMultiplier.damageMultiplier = 300f;
            this.m_poisonMultiplier.damageType = CoreDamageTypes.;
            target.healthHaver.damageTypeModifiers.Add(this.m_poisonMultiplier);*/
        }
        private void OnFires(Projectile bullet, float chanceshit)
        {
            if (fireyGuns.Contains(Owner.CurrentGun.PickupObjectId))
            {
                bullet.baseData.damage *= 2;
            }
        }
        public static List<int> fireyGuns = new List<int>()
        {
            382, //Phoenix
            125, //Flame Hand
            336, //Pitchfork
        };
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.OnFires;
            ETGMod.AIActor.OnPreStart += AIActorMods;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.PostProcessProjectile -= this.OnFires;
            ETGMod.AIActor.OnPreStart -= AIActorMods;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            ETGMod.AIActor.OnPreStart -= AIActorMods;
            base.OnDestroy();
        }
    }
}
