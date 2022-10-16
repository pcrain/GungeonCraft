using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    class OpulentBlank : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Opulent Blank";
            string resourceName = "NevernamedsItems/Resources/opulentblank_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<OpulentBlank>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Spin Bullets To Gold";
            string longDesc = "Turns all enemy bullets to gold. One use."+"\n\nAn extremely rare variant of the regular Blank.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.None, 1000);
            item.consumable = true;
            item.quality = ItemQuality.D;
            item.AddToSubShop(ItemBuilder.ShopType.OldRed);
        }
        public override void DoEffect(PlayerController user)
        {
            for (int i = 0; i < StaticReferenceManager.AllProjectiles.Count; i++)
            {
                Projectile projectile = StaticReferenceManager.AllProjectiles[i];
                if (projectile)
                {
                    if (!(projectile.Owner is PlayerController))
                    {
                        if (projectile.collidesWithPlayer || projectile.Owner is AIActor)
                        {
                            if (!projectile.ImmuneToBlanks)
                            {
                                LootEngine.SpawnCurrency(projectile.specRigidbody.UnitCenter, 1);
                            }
                        }
                    }
                }
            }
            user.ForceBlank();
        }       
    }
}