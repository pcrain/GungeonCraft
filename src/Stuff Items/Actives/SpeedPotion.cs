using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ItemAPI;
using SaveAPI;
using UnityEngine;

namespace NevernamedsItems
{
    class SpeedPotion : PlayerItem
    {
        //Call this method from the Start() method of your ETGModule extension class
        public static void Init()
        {
            //The name of the item
            string itemName = "Speed Potion";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it.
            string resourceName = "NevernamedsItems/Resources/speedpotion2_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<SpeedPotion>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Gotta Go Fast";
            string longDesc = "This is either made of pure magic, or pure back-alleyway-snowflakes if ya know what I mean.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //"kts" here is the item pool. In the console you'd type kts:sweating_bullets
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 500);

            //Adds a passive modifier, like curse, coolness, damage, etc. to the item. Works for passives and actives.


            //Set some other fields
            item.consumable = false;
            item.quality = ItemQuality.C;
            item.AddToSubShop(ItemBuilder.ShopType.Goopton);
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_SPEEDPOTION, true);
            item.AddItemToGooptonMetaShop(12);
            SpeedPotionID = item.PickupObjectId;
        }
        public static int SpeedPotionID;
        //Add the item's functionality down here! I stole most of this from the Stuffed Star active item code!
        float movementBuff = -1;
        float duration = 15f;
        public override void DoEffect(PlayerController user)
        {
            //Play a sound effect
            AkSoundEngine.PostEvent("Play_OBJ_power_up_01", base.gameObject);

            //Activates the effect
            StartEffect(user);

            //start a coroutine which calls the EndEffect method when the item's effect duration runs out
            StartCoroutine(ItemBuilder.HandleDuration(this, duration, user, EndEffect));
        }
        //MAKES THE OUTLINES AND SHIT
        private void EnableVFX(PlayerController user)
        {
            Material outlineMaterial = SpriteOutlineManager.GetOutlineMaterial(user.sprite);
            outlineMaterial.SetColor("_OverrideColor", new Color(70f, 1f, 90f));
        }

        private void DisableVFX(PlayerController user)
        {
            Material outlineMaterial = SpriteOutlineManager.GetOutlineMaterial(user.sprite);
            outlineMaterial.SetColor("_OverrideColor", new Color(0f, 0f, 0f));
        }

        bool activeOutline = false;

        //Doubles the movement speed
        private void StartEffect(PlayerController user)
        {
            float curMovement = user.stats.GetBaseStatValue(PlayerStats.StatType.MovementSpeed); //Get's the player's speed and stores it in a var called 'curMovement'
            float newMovement = curMovement * 2f; //Makes a variable named 'newMovement by multiplying 'curMovement' by 2
            user.stats.SetBaseStatValue(PlayerStats.StatType.MovementSpeed, newMovement, user); //Sets their movement speed to 'newMovement'
            movementBuff = newMovement - curMovement;
            EnableVFX(user);
            activeOutline = true;
        }



        //Resets the player back to their original stats
        private void EndEffect(PlayerController user)
        {
            if (movementBuff <= 0) return;
            float curMovement = user.stats.GetBaseStatValue(PlayerStats.StatType.MovementSpeed);
            float newMovement = curMovement - movementBuff;
            user.stats.SetBaseStatValue(PlayerStats.StatType.MovementSpeed, newMovement, user);
            movementBuff = -1;
            DisableVFX(user);
            activeOutline = false;
        }
        private void PlayerTookDamage(float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection)
        {
            if (activeOutline == true)
            {
                GameManager.Instance.StartCoroutine(this.GainOutline());
            }

            else if (activeOutline == false)
            {
                GameManager.Instance.StartCoroutine(this.LoseOutline());
            }
        }

        private IEnumerator GainOutline()
        {
            PlayerController user = this.LastOwner;
            yield return new WaitForSeconds(0.05f);
            EnableVFX(user);
            yield break;
        }

        private IEnumerator LoseOutline()
        {
            PlayerController user = this.LastOwner;
            yield return new WaitForSeconds(0.05f);
            DisableVFX(user);
            yield break;
        }
        public override void Pickup(PlayerController player)
        {
            player.healthHaver.OnDamaged += this.PlayerTookDamage;
            base.Pickup(player);
            CanBeDropped = true;
        }
        public override void OnPreDrop(PlayerController user)
        {
            if (base.IsCurrentlyActive)
            {
                base.IsCurrentlyActive = false;
                EndEffect(user);
            }
        }

        public DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.healthHaver.OnDamaged -= this.PlayerTookDamage;
            base.IsCurrentlyActive = false;
            EndEffect(player);
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (LastOwner)
            {
                LastOwner.healthHaver.OnDamaged -= this.PlayerTookDamage;
                base.IsCurrentlyActive = false;
                EndEffect(LastOwner);
            }
            base.OnDestroy();
        }

        //Disable or enable the active whenever you need!
        public override bool CanBeUsed(PlayerController user)
        {
            return base.CanBeUsed(user);
        }
    }
}
