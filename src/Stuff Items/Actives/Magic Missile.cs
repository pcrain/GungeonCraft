using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using ItemAPI;
using SaveAPI;
using UnityEngine;

namespace NevernamedsItems
{
    class MagicMissile : PlayerItem
    {
        //Call this method from the Start() method of your ETGModule extension class
        public static void Init()
        {
            //The name of the item
            string itemName = "Magic Missile";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it.
            string resourceName = "NevernamedsItems/Resources/magicmissile_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<MagicMissile>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Dank Dungeon Walls";
            string longDesc = "An ancient art, kept sacred by a sect of Gunjurers deep within the Hollow's caverns." + "\n\nImbued with physical form in an attempt to preserve it for future generations." + "\n\nWorks best in the dark.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //"kts" here is the item pool. In the console you'd type kts:sweating_bullets
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 540);

            //Set some other fields
            item.consumable = false;
            item.quality = ItemQuality.C;
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_MAGICMISSILE, true);
            item.AddItemToDougMetaShop(20);

            ID = item.PickupObjectId;
        }
        public static int ID;
        public bool isGivingDarknessImmunity = false;
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnEnteredCombat += OnEnteredCombat;
            if (!isGivingDarknessImmunity)
            {
                CustomDarknessHandler.shouldBeLightOverride.SetOverride("MagicMissile", true);
                isGivingDarknessImmunity = true;
            }
        }
        private void OnEnteredCombat()
        {
            if (LastOwner != null && LastOwner.CurrentRoom != null)
            {
                if (LastOwner.CurrentRoom.IsDarkAndTerrifying)
                {
                    LastOwner.CurrentRoom.EndTerrifyingDarkRoom();
                }
            }
        }
        public override void OnDestroy()
        {
            if (LastOwner)
            {
                LastOwner.OnEnteredCombat += OnEnteredCombat;
            }
            if (isGivingDarknessImmunity)
            {
                CustomDarknessHandler.shouldBeLightOverride.RemoveOverride("MagicMissile");
                isGivingDarknessImmunity = false;
            }
            base.OnDestroy();
        }
        public override void OnPreDrop(PlayerController user)
        {
            base.OnPreDrop(user);
            user.OnEnteredCombat -= OnEnteredCombat;

            if (isGivingDarknessImmunity)
            {
                CustomDarknessHandler.shouldBeLightOverride.RemoveOverride("MagicMissile");
                isGivingDarknessImmunity = false;
            }
        }
        public override void DoEffect(PlayerController user)
        {
            SpawnMissile();
            if (user.PlayerHasActiveSynergy("Magic-er Missile"))
            {
                Invoke("SpawnMissile", 1f);
            }
        }
        private void SpawnMissile()
        {
            PlayerController user = LastOwner;
            Projectile projectile = ((Gun)ETGMod.Databases.Items[372]).DefaultModule.projectiles[0];
            GameObject gameObject = SpawnManager.SpawnProjectile(projectile.gameObject, user.sprite.WorldCenter, Quaternion.Euler(0f, 0f, (user.CurrentGun == null) ? 0f : user.CurrentGun.CurrentAngle), true);
            Projectile component = gameObject.GetComponent<Projectile>();
            if (component != null)
            {
                component.Owner = user;
                component.Shooter = user.specRigidbody;
                component.baseData.damage *= 2f;
                component.AdjustPlayerProjectileTint(ExtendedColours.lime, 1);
                user.DoPostProcessProjectile(component);
            }
        }
    }
}