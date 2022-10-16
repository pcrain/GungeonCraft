using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Dungeonator;

namespace NevernamedsItems
{
    public class EqualityItem : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Equality";
            string resourceName = "NevernamedsItems/Resources/equality_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<EqualityItem>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Blanks And Keys Are Equal";
            string longDesc = "Constantly equalises the bearer's stocks of blanks and keys."+"\n\nOf debatable usefulness.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.CanBeDropped = false;
            item.quality = PickupObject.ItemQuality.B;

        }
        private void DoInitialBalancing()
        {
            if (Owner)
            {
                int blanks = Owner.Blanks;
                int keys = Owner.carriedConsumables.KeyBullets;

                if (blanks != keys)
                {
                    if (blanks > keys) Owner.carriedConsumables.KeyBullets = blanks;
                    else if (keys > blanks) Owner.Blanks = keys;
                }
                cachedBlanks = blanks;
                cachedKeys = keys;
            }
        }
        int cachedKeys;
        int cachedBlanks;
        public override void Update()
        {
            if (Owner)
            {
                if (Owner.carriedConsumables.KeyBullets != cachedKeys)
                {
                   if (Owner.carriedConsumables.KeyBullets != Owner.Blanks)
                    {
                        Owner.Blanks = Owner.carriedConsumables.KeyBullets;
                    }
                    cachedBlanks = Owner.Blanks;
                    cachedKeys = Owner.carriedConsumables.KeyBullets;
                }
                if (Owner.Blanks != cachedBlanks)
                {
                    if (Owner.carriedConsumables.KeyBullets != Owner.Blanks)
                    {
                        Owner.carriedConsumables.KeyBullets = Owner.Blanks;
                    }
                    cachedKeys = Owner.carriedConsumables.KeyBullets;                   
                    cachedBlanks = Owner.Blanks;
                }
            }
            base.Update();
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            if (!this.m_pickedUpThisRun)
            {
                DoInitialBalancing();
            }
        }

        public override DebrisObject Drop(PlayerController player)
        {
            return base.Drop(player);
        }
    }
}