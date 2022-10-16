using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using Alexandria.ItemAPI;
using SaveAPI;


namespace NevernamedsItems
{
    public class BreachingRounds : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Breaching Rounds";
            string resourceName = "NevernamedsItems/Resources/NeoItemSprites/breachrounds_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<BreachingRounds>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Breach and Clear";
            string longDesc = "Gives a damage boost upon entering combat, which quickly deteriorates over time. Speed is key."+"\n\nUsed by ancient dungeon crawlers to blast open locks and hidden walls, though the Gungeon's secret rooms are a little too tough for that.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;
            item.SetTag("bullet_modifier");
            item.AddToSubShop(ItemBuilder.ShopType.Trorc);
            ID = item.PickupObjectId;
        }
        public static int ID;
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnEnteredCombat += EnteredCombat;

        }
        private void EnteredCombat()
        {
            if (Owner)
            {
                Owner.StopCoroutine(HandleDamageDrainCoroutine(Owner));
                AlterItemStats.RemoveStatFromPassive(this, PlayerStats.StatType.Damage);
                AlterItemStats.RemoveStatFromPassive(this, PlayerStats.StatType.PlayerBulletScale);
                Owner.StartCoroutine(HandleDamageDrainCoroutine(Owner));
            }
        }
        private IEnumerator HandleDamageDrainCoroutine(PlayerController coroutineTarget)
        {
            float multiplier = 3.5f;
            float scaleMult = 2f;
            float realTime = 7.5f;
            
            float elapsed = 0f;
            while (elapsed < realTime)
            {
                elapsed += BraveTime.DeltaTime;
                float t = Mathf.Clamp01(elapsed / realTime);
                float damagemodifier = Mathf.Lerp(multiplier, 1, t);
                float scaleMod = Mathf.Lerp(scaleMult, 1, t);

                AlterItemStats.RemoveStatFromPassive(this, PlayerStats.StatType.Damage);
                AlterItemStats.RemoveStatFromPassive(this, PlayerStats.StatType.PlayerBulletScale);
                AlterItemStats.AddStatToPassive(this, PlayerStats.StatType.Damage, damagemodifier, StatModifier.ModifyMethod.MULTIPLICATIVE);
                AlterItemStats.AddStatToPassive(this, PlayerStats.StatType.PlayerBulletScale, scaleMod, StatModifier.ModifyMethod.MULTIPLICATIVE);
               if (Owner) Owner.stats.RecalculateStats(Owner, false, false);
              
                yield return null;
            }
            yield break;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnEnteredCombat -= EnteredCombat;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.OnEnteredCombat -= EnteredCombat;
            }
            base.OnDestroy();
        }

    }
}

