using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    class BomberJacket : PlayerItem
    {
        //Call this method from the Start() method of your ETGModule extension class
        public static void Init()
        {
            //The name of the item
            string itemName = "Bomber Jacket";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it.
            string resourceName = "NevernamedsItems/Resources/bomberjacket_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<BomberJacket>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Kamakablooey";
            string longDesc = "Use to create an explosion around yourself."+"\n\nBrought to the Gungeon by a rather unscrupulous individual, it has since been modified to be safe to the user, and been renamed so it can be shown on the internet without losing one's entire career.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //"kts" here is the item pool. In the console you'd type kts:sweating_bullets
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 120);

            //Adds a passive modifier, like curse, coolness, damage, etc. to the item. Works for passives and actives.
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalItemCapacity, 2f, StatModifier.ModifyMethod.ADDITIVE);

            //Set some other fields
            item.consumable = false;
            item.quality = ItemQuality.D;

            item.AddToSubShop(ItemBuilder.ShopType.Trorc);
            ID = item.PickupObjectId;
        }
        public static int ID;

        //Add the item's functionality down here! I stole most of this from the Stuffed Star active item code!
        public override void DoEffect(PlayerController user)
        {
            DoSafeExplosion(user.specRigidbody.UnitCenter);
        }
        ExplosionData smallPlayerSafeExplosion = new ExplosionData()
        {
            damageRadius = 2.5f,
            damageToPlayer = 0f,
            doDamage = true,
            damage = 25,
            doDestroyProjectiles = true,
            doForce = true,
            debrisForce = 30f,
            preventPlayerForce = true,
            explosionDelay = 0.1f,
            usesComprehensiveDelay = false,
            doScreenShake = true,
            playDefaultSFX = true,
        };
        ExplosionData bigPlayerSafeExplosion = new ExplosionData()
        {
            damageRadius = 4f,
            damageToPlayer = 0f,
            doDamage = true,
            damage = 50,
            doDestroyProjectiles = true,
            doForce = true,
            debrisForce = 60f,
            preventPlayerForce = true,
            explosionDelay = 0.1f,
            usesComprehensiveDelay = false,
            doScreenShake = true,
            playDefaultSFX = true,
        };
        public void DoSafeExplosion(Vector3 position)
        {
            if (LastOwner.HasPickupID(332))
            {
                var defaultExplosion = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultExplosionData;
                bigPlayerSafeExplosion.effect = defaultExplosion.effect;
                bigPlayerSafeExplosion.ignoreList = defaultExplosion.ignoreList;
                bigPlayerSafeExplosion.ss = defaultExplosion.ss;
                Exploder.Explode(position, bigPlayerSafeExplosion, Vector2.zero);
            }
            else
            {
                var defaultExplosion = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultSmallExplosionData;
                smallPlayerSafeExplosion.effect = defaultExplosion.effect;
                smallPlayerSafeExplosion.ignoreList = defaultExplosion.ignoreList;
                smallPlayerSafeExplosion.ss = defaultExplosion.ss;
                Exploder.Explode(position, smallPlayerSafeExplosion, Vector2.zero);
            }
        }
    }
}
