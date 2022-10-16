using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public class DiamondBracelet : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Diamond Bracelet";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/diamondbracelet_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<DiamondBracelet>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Slinger's Best Friend";
            string longDesc = "Thrown guns deal massive damage, and return safely to their owner." + "\n\nDespite the seeming societal progress marked by the reforging of the Ruby Bracelet, it seems there are still some bumpkins in the Gungeon's depths who insist on chucking their guns.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.ThrownGunDamage, 7f, StatModifier.ModifyMethod.ADDITIVE);

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.D;

            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.KILLEDENEMYWITHTHROWNGUN, true);
            DiamondBraceletID = item.PickupObjectId;
        }
        public static int DiamondBraceletID;
        private void HandleReturnLikeBoomerang(DebrisObject obj)
        {
            obj.PreventFallingInPits = true;
            obj.OnGrounded = (Action<DebrisObject>)Delegate.Remove(obj.OnGrounded, new Action<DebrisObject>(this.HandleReturnLikeBoomerang));
            PickupMover pickupMover = obj.gameObject.AddComponent<PickupMover>();
            if (pickupMover.specRigidbody)
            {
                pickupMover.specRigidbody.CollideWithTileMap = false;
            }
            pickupMover.minRadius = 1f;
            pickupMover.moveIfRoomUnclear = true;
            pickupMover.stopPathingOnContact = false;
        }
        private void PostProcessThrownGun(Projectile thrownGunProjectile)
        {
            thrownGunProjectile.pierceMinorBreakables = true;
            thrownGunProjectile.IgnoreTileCollisionsFor(0.01f);
            thrownGunProjectile.OnBecameDebrisGrounded = (Action<DebrisObject>)Delegate.Combine(thrownGunProjectile.OnBecameDebrisGrounded, new Action<DebrisObject>(this.HandleReturnLikeBoomerang));
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessThrownGun += this.PostProcessThrownGun;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.PostProcessThrownGun -= this.PostProcessThrownGun;
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner) Owner.PostProcessThrownGun -= this.PostProcessThrownGun;
            base.OnDestroy();
        }
    }
}