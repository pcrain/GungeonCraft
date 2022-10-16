using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    class BlueWhitePill : PlayerItem
    {
        //Call this method from the Start() method of your ETGModule extension class
        public static void Init()
        {
            //The name of the item
            string itemName = "Blue & White Pillet";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it.
            string resourceName = "NevernamedsItems/Resources/bluewhitepillet_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<BlueWhitePill>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "The Blooneys";
            string longDesc = "";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //"kts" here is the item pool. In the console you'd type kts:sweating_bullets
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Timed, 5);

            //Adds a passive modifier, like curse, coolness, damage, etc. to the item. Works for passives and actives.

            //Set some other fields
            item.consumable = true;
            item.quality = ItemQuality.EXCLUDED;
        }

        //Add the item's functionality down here! I stole most of this from the Stuffed Star active item code!

        public override void DoEffect(PlayerController user)
        {
            PillEffect effect = pilleffects[BlueWhitePillEffect];
            effect.action.Invoke(effect, user);
            Notify("null", effect.notificationText);
        }
        public static List<PillEffect> pilleffects = new List<PillEffect>();
        public struct PillEffect
        {
            public PlayerStats.StatType statToEffect;
            public float amount;
            public int pickupID;
            public int pickupAmount;
            public float min, max;
            public StatModifier.ModifyMethod modifyMethod;
            public Action<PillEffect, PlayerController> action;
            public string notificationHeader;
            public string notificationText;
            public List<PillEffect> subEffects;
        }
        public static void DefineEffects()
        {
            pilleffects.Add(new PillEffect()
            {
                notificationText = "Health Up",
                action = HealthModifier,
                statToEffect = PlayerStats.StatType.Health,
                modifyMethod = StatModifier.ModifyMethod.ADDITIVE,
                amount = 1f,
            });
            pilleffects.Add(new PillEffect()
            {
                notificationText = "Health Down",
                action = HealthModifier,
                statToEffect = PlayerStats.StatType.Health,
                modifyMethod = StatModifier.ModifyMethod.ADDITIVE,
                amount = -1f,
            });
        }
        public static void HealthModifier(PillEffect effect, PlayerController user)
        {
            float currentStatValue = user.stats.GetBaseStatValue(PlayerStats.StatType.Health);
            if (user.characterIdentity == PlayableCharacters.Robot)
            {
                if (effect.amount >= 0)
                {
                    user.healthHaver.Armor += effect.amount;
                }
                else if (effect.amount < 0 && user.healthHaver.Armor > 1)
                {
                    user.healthHaver.Armor += effect.amount;
                }
                else if (effect.amount < 0 && user.healthHaver.Armor == 1)
                {
                    user.healthHaver.Armor += 1;
                }
                else return;
            }
            else if (effect.amount >= 0)
            {
                user.stats.SetBaseStatValue(PlayerStats.StatType.Health, currentStatValue + effect.amount, user);
            }
            else if (effect.amount < 0 && currentStatValue > 1)
            {
                user.stats.SetBaseStatValue(PlayerStats.StatType.Health, currentStatValue + effect.amount, user);
            }
            else if (effect.amount < 0 && currentStatValue == 1)
            {
                user.stats.SetBaseStatValue(PlayerStats.StatType.Health, currentStatValue + 1, user);
            }
            else return;
        }
        private void Notify(string header, string text)
        {
            var sprite = GameUIRoot.Instance.notificationController.notificationObjectSprite;
            GameUIRoot.Instance.notificationController.DoCustomNotification(
                header,
                text,
                sprite.Collection,
                sprite.spriteId,
                UINotificationController.NotificationColor.PURPLE,
                false,
                false);
        }
        public static int BlueWhitePillEffect;
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            BlueWhitePillEffect = UnityEngine.Random.Range(0, pilleffects.Count);            
        }
    }
}