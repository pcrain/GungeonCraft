using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using ItemAPI;
using Gungeon;

namespace NevernamedsItems
{
    public class HiveHolster : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Hive Holster";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/hiveholster_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<HiveHolster>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Beeload";
            string longDesc = "A small hive of bees seems to have taken up residence in this old holster." + "\n\nBears striking resemblance to THE Hive Holster, that sits on the hip of the legendary Gunstinger. But it couldn't be THAT one... right?";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.B;

            //LIST OF SYNERGIES


        }
        public bool canActivate = true;
        private void HandleGunReloaded(PlayerController player, Gun playerGun)
        {
            if (playerGun.ClipShotsRemaining == 0 && canActivate == true)
            {
                StartCoroutine(HandleBees(player));
                canActivate = false;
                Invoke("Reset", 2f);
            }
        }
        private IEnumerator HandleBees(PlayerController player)
        {
            int beesToSpawn = 5;
            if (Owner.HasPickupID(14) || Owner.HasPickupID(432) || Owner.HasPickupID(138) || Owner.HasPickupID(630))
            {
                beesToSpawn += 2;
            }
            for (int i = 0; i < beesToSpawn; i++)
            {
                Projectile projectile = ((Gun)ETGMod.Databases.Items[14]).DefaultModule.projectiles[0];
                GameObject gameObject = SpawnManager.SpawnProjectile(projectile.gameObject, base.Owner.sprite.WorldCenter, Quaternion.Euler(0f, 0f, (base.Owner.CurrentGun == null) ? 0f : base.Owner.CurrentGun.CurrentAngle), true);
                Projectile component = gameObject.GetComponent<Projectile>();
                if (component != null)
                {
                    component.Owner = base.Owner;
                    component.Shooter = base.Owner.specRigidbody;
                    component.baseData.damage = 3f * player.stats.GetStatValue(PlayerStats.StatType.Damage); ;
                }
                yield return new WaitForSeconds(.1f);
            }
            if (Owner.HasPickupID(92) && base.Owner.CurrentGun.PickupObjectId == 92)
            {
                Projectile projectile = ((Gun)ETGMod.Databases.Items[92]).DefaultModule.projectiles[0];
                GameObject gameObject = SpawnManager.SpawnProjectile(projectile.gameObject, base.Owner.sprite.WorldCenter, Quaternion.Euler(0f, 0f, (base.Owner.CurrentGun == null) ? 0f : base.Owner.CurrentGun.CurrentAngle), true);
                Projectile component = gameObject.GetComponent<Projectile>();
                if (component != null)
                {
                    component.Owner = base.Owner;
                    component.Shooter = base.Owner.specRigidbody;
                    component.baseData.speed *= 2f;
                    component.baseData.damage = 10f * player.stats.GetStatValue(PlayerStats.StatType.Damage); ;
                }
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