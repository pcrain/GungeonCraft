using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    public class BlankBoots : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Blank Boots";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/blankboots_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<BlankBoots>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Boots of Banishment";
            string longDesc = "Rolling over enemy bullets has a chance to trigger a microblank." + "\n\nMade by a senile old man who misheard a conversation about the legendary 'Blank Bullets'.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.A;
            item.AddToSubShop(ItemBuilder.ShopType.OldRed);

        }


        private void onDodgeRolledOverBullet(Projectile bullet)
        {
            float procChance = 0.2f;
            //Synergy with Full Metal Jacket, True Blank, False Blank, Spare Blank, or Blank Stare
            if (Owner.HasPickupID(564) || Owner.HasPickupID(Gungeon.Game.Items["nn:true_blank"].PickupObjectId) || Owner.HasPickupID(Gungeon.Game.Items["nn:false_blank"].PickupObjectId) || Owner.HasPickupID(Gungeon.Game.Items["nn:spare_blank"].PickupObjectId) || Owner.HasPickupID(Gungeon.Game.Items["nn:blank_stare"].PickupObjectId))
            {
                procChance = 0.4f;
            }
            if (UnityEngine.Random.value < procChance)
            {
                //Synergy with Blank Bullets.
                if (Owner.HasPickupID(579) && UnityEngine.Random.value < 0.25f)
                {
                    Owner.ForceBlank(25f, 0.5f, false, true, null, true, -1f);
                }
                else
                {
                    DoMicroBlank(Owner.specRigidbody.UnitCenter);
                }
            }
        }
        private void DoMicroBlank(Vector2 center)
        {
            PlayerController owner = base.Owner;
            GameObject silencerVFX = (GameObject)ResourceCache.Acquire("Global VFX/BlankVFX_Ghost");
            AkSoundEngine.PostEvent("Play_OBJ_silenceblank_small_01", base.gameObject);
            GameObject gameObject = new GameObject("silencer");
            SilencerInstance silencerInstance = gameObject.AddComponent<SilencerInstance>();
            float additionalTimeAtMaxRadius = 0.25f;
            silencerInstance.TriggerSilencer(center, 25f, 5f, silencerVFX, 0f, 3f, 3f, 3f, 250f, 5f, additionalTimeAtMaxRadius, owner, false, false);
        }

        public override void Pickup(PlayerController player)
        {
            player.OnDodgedProjectile += this.onDodgeRolledOverBullet;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject result = base.Drop(player);
            player.OnDodgedProjectile -= this.onDodgeRolledOverBullet;
            return result;
        }
        public override void OnDestroy()
        {
            if (Owner) Owner.OnDodgedProjectile -= this.onDodgeRolledOverBullet;
            base.OnDestroy();
        }
    }
}