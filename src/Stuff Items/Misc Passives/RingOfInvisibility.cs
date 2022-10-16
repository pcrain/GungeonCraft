using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;
using System.Collections;
using SaveAPI;

namespace NevernamedsItems
{
    public class RingOfInvisibility : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Ring of Invisibility";
            string resourceName = "NevernamedsItems/Resources/ringofinvisibility_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<RingOfInvisibility>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Precious";
            string longDesc = "Grants invisibility while standing perfectly still." + "\n\nThis ancient ring has been coveted throughout generations of Gungeoneers and Gundead alike. The idea of removing it seems unthinkable.";
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1, StatModifier.ModifyMethod.ADDITIVE);

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.AddToSubShop(ItemBuilder.ShopType.Cursula);
            item.CanBeDropped = false;
            item.quality = PickupObject.ItemQuality.S;

            RingOfInvisibilityID = item.PickupObjectId;
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.CHALLENGE_INVISIBLEO_BEATEN, true);

        }
        public static int RingOfInvisibilityID;
        private float unstealthyTimer;
        private bool isCurrentlyStealthed;
        public override void Update()
        {
            if (Owner != null)
            {
                if (unstealthyTimer >= 0)
                {
                    unstealthyTimer -= BraveTime.DeltaTime;
                }
                UpdateShouldBeStealthed();
            }
            base.Update();
        }
        private void UpdateShouldBeStealthed()
        {
            if (unstealthyTimer <= 0 && Owner.specRigidbody.Velocity == Vector2.zero)
            {
                if (!isCurrentlyStealthed)
                {
                    StealthEffect();
                }
            }
            else
            {
                if (isCurrentlyStealthed)
                {
                    BreakStealth(Owner);
                }
            }
        }
        private void StealthEffect()
        {
            PlayerController owner = base.Owner;
            //this.BreakStealth(owner);
            owner.ChangeSpecialShaderFlag(1, 1f);
            owner.SetIsStealthed(true, "invisibilityRing");
            owner.SetCapableOfStealing(true, "invisibilityRing", null);
            isCurrentlyStealthed = true;
        }
        private void BreakStealth(PlayerController player, bool brokeStealthUnstealthily = false)
        {
            player.ChangeSpecialShaderFlag(1, 0f);
            player.SetIsStealthed(false, "invisibilityRing");
            player.SetCapableOfStealing(false, "invisibilityRing", null);
            AkSoundEngine.PostEvent("Play_ENM_wizardred_appear_01", base.gameObject);
            isCurrentlyStealthed = false;
            if (brokeStealthUnstealthily) unstealthyTimer += 5;
        }
        private void OnUnstealthy(PlayerController playa)
        {
          if (isCurrentlyStealthed)  BreakStealth(playa, true);
        }
        public override void Pickup(PlayerController player)
        {
            player.OnDidUnstealthyAction += this.OnUnstealthy;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.OnDidUnstealthyAction -= this.OnUnstealthy;
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.OnDidUnstealthyAction -= this.OnUnstealthy;
            }
            base.OnDestroy();
        }
    }
}