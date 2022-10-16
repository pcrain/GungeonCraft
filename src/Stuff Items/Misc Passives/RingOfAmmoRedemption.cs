using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    public class RingOfAmmoRedemption : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Ring of Ammo Redemption";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/ringofammoredemption_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<RingOfAmmoRedemption>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Thrown Guns Reload";
            string longDesc = "Killing an enemy with a thrown gun restores 10% of that gun's ammo."+"\n\nThis ring once belonged to Peale-4, an infamously loot-oriented Gungeoneer."+"\nHis ring is so hungry for loot that it encourages your guns to steal ammo directly from the dead.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.D; //D
            RingOfAmmoRedemptionID = item.PickupObjectId;
        }
        public static int RingOfAmmoRedemptionID;
        private void PostProcessThrownGun(Projectile thrownGunProjectile)
        {
            thrownGunProjectile.OnHitEnemy += this.RestoreAmmo;
            //foreach (Component component in thrownGunProjectile.GetComponents<Component>())
            //{
               // ETGModConsole.Log(component.GetType().ToString());
            //}
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessThrownGun += this.PostProcessThrownGun;
        }
        private void RestoreAmmo(Projectile arg1, SpeculativeRigidbody arg2, bool arg3)
        {
            if (arg3)
            {
                float restorePercent = 0.10f;
                if ((arg1.Owner as PlayerController).PlayerHasActiveSynergy("Ammo Economy Inflation")) restorePercent = 0.20f;
                Gun gun = arg1.GetComponentInChildren<Gun>();
                if (gun)
                {
                    gun.GainAmmo(Mathf.FloorToInt(gun.AdjustedMaxAmmo * restorePercent));
                }
                else
                {
                    ETGModConsole.Log("Could not find gun component.");
                }
            }
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.PostProcessThrownGun -= this.PostProcessThrownGun;
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner != null)
            {
            Owner.PostProcessThrownGun -= this.PostProcessThrownGun;
            }
            base.OnDestroy();
        }
    }
}
