using System;
using System.Collections.Generic;
using System.Linq;
using Gungeon;
using System.Text;

using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public class TheBeholster : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "The Beholster";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/thebeholster_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<TheBeholster>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "CreEyetion";
            string longDesc = "Summons disgusting Beholsterspawn upon reloading an empty clip."+"\n\nThe infamous holster of the... Beholster."+"\nSome of these names sound strange when used in a sentence.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.B;

            //LIST OF SYNERGIES
            item.SetupUnlockOnCustomStat(CustomTrackedStats.BEHOLSTER_KILLS, 14, DungeonPrerequisite.PrerequisiteOperation.GREATER_THAN);

        }
        public bool canActivate = true;
        private void HandleGunReloaded(PlayerController player, Gun playerGun)
        {
            if (playerGun.ClipShotsRemaining == 0 && canActivate == true)
            {
                Projectile projectile = ((Gun)ETGMod.Databases.Items[90]).DefaultModule.finalProjectile;
                GameObject gameObject = SpawnManager.SpawnProjectile(projectile.gameObject, base.Owner.sprite.WorldCenter, Quaternion.Euler(0f, 0f, (base.Owner.CurrentGun == null) ? 0f : base.Owner.CurrentGun.CurrentAngle), true);
                Projectile component = gameObject.GetComponent<Projectile>();
                if (component != null)
                {
                    component.Owner = base.Owner;
                    component.Shooter = base.Owner.specRigidbody;
                    component.baseData.speed = 1f;
                    if (Owner.HasPickupID(90))
                    {
                        component.baseData.damage = 24f * player.stats.GetStatValue(PlayerStats.StatType.Damage); ;
                        GameObject gameObject2 = SpawnManager.SpawnProjectile(projectile.gameObject, base.Owner.sprite.WorldCenter, Quaternion.Euler(0f, 0f, (base.Owner.CurrentGun == null) ? 0f : base.Owner.CurrentGun.CurrentAngle), true);
                        Projectile component2 = gameObject2.GetComponent<Projectile>();
                        if (component2 != null)
                        {
                            component2.Owner = base.Owner;
                            component2.Shooter = base.Owner.specRigidbody;
                            component2.baseData.speed = 1f;
                            component2.baseData.damage = 24f * player.stats.GetStatValue(PlayerStats.StatType.Damage); ;
                        }
                    }
                    else
                    {
                        component.baseData.damage = 12f * player.stats.GetStatValue(PlayerStats.StatType.Damage); ;

                    }
                }
                canActivate = false;
                Invoke("Reset", 2f);
            }
        }
        void Reset()
        {
            canActivate = true;
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnReloadedGun += this.HandleGunReloaded;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnReloadedGun -= this.HandleGunReloaded;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner) Owner.OnReloadedGun -= this.HandleGunReloaded;
            base.OnDestroy();
        }
    }

}