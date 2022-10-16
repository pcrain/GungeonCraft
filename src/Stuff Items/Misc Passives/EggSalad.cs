using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alexandria.Misc;
using UnityEngine;
using Alexandria.ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public class EggSalad : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Egg Salad";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/egg_salad_001";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<EggSalad>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Tastes Fishy. Why.";
            string longDesc = "Somebody lost an egg salad down here a long, long time ago...";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Health, 1, StatModifier.ModifyMethod.ADDITIVE);

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.D;
            
        }
        public override void Pickup(PlayerController player)
        {
            if (player.ForceZeroHealthState && !m_pickedUpThisRun) { player.healthHaver.Armor += 1;  }
            base.Pickup(player);
        }

    }
    public class PrimaBean : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Prima Bean";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/primabean_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<PrimaBean>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Magic?";
            string longDesc = "There are strange magics coursing through this bean, like fiber! It’s good for your body damnit!";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Health, 3, StatModifier.ModifyMethod.ADDITIVE);

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.S;
            item.AddToSubShop(ItemBuilder.ShopType.Cursula);
        }
        public override void Pickup(PlayerController player)
        {
            if (player.ForceZeroHealthState && !m_pickedUpThisRun) { player.healthHaver.Armor += 3;  }
            base.Pickup(player);
        }

    }
    public class BashingBullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Bashing Bullets";
            string resourceName = "NevernamedsItems/Resources/BulletModifiers/bashingbullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<BashingBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Punch Out";
            string longDesc = "The thick leather gloves glued to the slugs of these bullets increases the kinetic force they apply to the target.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.KnockbackMultiplier, 10, StatModifier.ModifyMethod.ADDITIVE);
            item.quality = PickupObject.ItemQuality.D;
            item.SetTag("bullet_modifier");
            BashingBulletsID = item.PickupObjectId;
        }
        public static int BashingBulletsID;
    }
    public class TitanBullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Titan Bullets";
            string resourceName = "NevernamedsItems/Resources/titanbullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<TitanBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Absolute Unit";
            string longDesc = "Bullets increase massively in size, and slightly in damage."+"\n\nThese bullets are so big that enemies are left in shock and awe.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.PlayerBulletScale, 10, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Damage, 1.05f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            item.quality = PickupObject.ItemQuality.D;
            item.SetTag("bullet_modifier");
            item.SetupUnlockOnCustomStat(CustomTrackedStats.TITAN_KIN_KILLED, 4, DungeonPrerequisite.PrerequisiteOperation.GREATER_THAN);
            ID = item.PickupObjectId;
        }
        public static int ID;
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProjectile;
        }
        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            float activationChance = 0.3f;
            float num = activationChance * effectChanceScalar;
            if (UnityEngine.Random.value < num)
            {
                sourceProjectile.OnHitEnemy = (Action<Projectile, SpeculativeRigidbody, bool>)Delegate.Combine(sourceProjectile.OnHitEnemy, new Action<Projectile, SpeculativeRigidbody, bool>(this.AddStunEffect));
            }
        }
        private void AddStunEffect(Projectile arg1, SpeculativeRigidbody arg2, bool arg3)
        {
            if (arg2 != null && arg2.healthHaver.IsAlive && !arg2.healthHaver.IsBoss)
            {
                arg2.behaviorSpeculator.Stun(1f, true);
            }
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.PostProcessProjectile -= this.PostProcessProjectile;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {

            Owner.PostProcessProjectile -= this.PostProcessProjectile;
            }
            base.OnDestroy();
        }

    }

    public class SpareBlank : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Spare Blank";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/spareblank_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<SpareBlank>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "+1 to Blank";
            string longDesc = "Never hurts to have a spare.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalBlanksPerFloor, 1, StatModifier.ModifyMethod.ADDITIVE);

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.D;
            item.AddToSubShop(ItemBuilder.ShopType.OldRed);
        }
        public override void Pickup(PlayerController player)
        {
            bool hasntAlreadyBeenCollected = !this.m_pickedUpThisRun;
            if (hasntAlreadyBeenCollected)
            {
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(224).gameObject, player);
            }
            base.Pickup(player);
        }

    }


    public class PowerArmour : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Power Armour";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/powerarmour_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<PowerArmour>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Power Fantasy";
            string longDesc = "A high tech suit of armour from a small planet on the rim of explored space. It's layered plasteel composition makes it incredibly tough.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item
            item.CanBeDropped = false;

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.A;
            item.AddToSubShop(ItemBuilder.ShopType.Trorc);
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_POWERARMOUR, true);
            PowerArmourID = item.PickupObjectId;
        }
        public static int PowerArmourID;
        public override void Pickup(PlayerController player)
        {
            bool hasntAlreadyBeenCollected = !this.m_pickedUpThisRun;
            if (hasntAlreadyBeenCollected)
            {
                player.healthHaver.Armor += 8;
            }
            base.Pickup(player);
        }

    }
}
