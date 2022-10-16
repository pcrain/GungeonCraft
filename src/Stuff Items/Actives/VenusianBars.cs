using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Alexandria.ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    class VenusianBars : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Venusian Bars";
            string resourceName = "NevernamedsItems/Resources/NeoActiveSprites/venusianbars_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<VenusianBars>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Verified";
            string longDesc = "A charm forged out of pure, condensed talent by the one and only Gun God. While it doesn't allow the bearer to come even close to matching his skillz, they can still spit some mad bars and bullets.\n" + "Works best on Automatic weapons.\n\n\n\n" + "Pop Pop.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 1000);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Coolness, 1f, StatModifier.ModifyMethod.ADDITIVE);
            item.consumable = false;
            item.quality = ItemQuality.S;
        }
        public override void DoEffect(PlayerController user)
        {
            AkSoundEngine.PostEvent("Play_WPN_LowerCaseR_Bye_GameOver_01", base.gameObject);
            user.StartCoroutine(HandleTimedStatModifier(user));
        }
        private IEnumerator HandleTimedStatModifier(PlayerController player)
        {
            StatModifier firerateMod = new StatModifier()
            {
                amount = 100,
                statToBoost = PlayerStats.StatType.RateOfFire,
                modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE,
            };
            StatModifier chargeMod = new StatModifier()
            {
                amount = 100,
                statToBoost = PlayerStats.StatType.ChargeAmountMultiplier,
                modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE,
            };
            StatModifier reloadMod = new StatModifier()
            {
                amount = 0.01f,
                statToBoost = PlayerStats.StatType.ReloadSpeed,
                modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE,
            };

            player.ownerlessStatModifiers.Add(firerateMod);
            player.ownerlessStatModifiers.Add(chargeMod);
            player.ownerlessStatModifiers.Add(reloadMod);
            player.stats.RecalculateStats(player);

            yield return new WaitForSeconds(10);

            player.ownerlessStatModifiers.Remove(firerateMod);
            player.ownerlessStatModifiers.Remove(chargeMod);
            player.ownerlessStatModifiers.Remove(reloadMod);
            player.stats.RecalculateStats(player);
            yield break;
        }     
    }
}
