using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using ItemAPI;
using TranslationAPI;

namespace NevernamedsItems
{
    class ShadeHand : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Hand of Night";
            string resourceName = "NevernamedsItems/Resources/shadehand_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<ShadeHand>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Giveth and Taketh Away";
            string longDesc = "The cold and clammy hands of a long-dead, particularly wrathful shade.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 800);
            item.consumable = false;
            item.quality = ItemQuality.EXCLUDED;
            item.CanBeDropped = false;

            item.TranslateItemName(StringTableManager.GungeonSupportedLanguages.RUSSIAN, "Рука Ночи");
            item.TranslateItemShortDescription(StringTableManager.GungeonSupportedLanguages.RUSSIAN, "Даёт и Забирает");
            item.TranslateItemLongDescription(StringTableManager.GungeonSupportedLanguages.RUSSIAN, "Холодные и липкие руки давно сгинувшей и особенно яростной тени.");
        }
        public override void DoEffect(PlayerController user)
        {
            StealthEffect();
            base.StartCoroutine(ItemBuilder.HandleDuration(this, this.duration, user, new Action<PlayerController>(this.BreakStealth)));
        }
        private void StealthEffect()
        {
            PlayerController owner = base.LastOwner;
            this.BreakStealth(owner);
            owner.OnItemStolen += this.BreakStealthOnSteal;
            owner.ChangeSpecialShaderFlag(1, 1f);
            owner.healthHaver.OnDamaged += this.OnDamaged;
            owner.SetIsStealthed(true, "shade");
            owner.SetCapableOfStealing(true, "shade", null);
            GameManager.Instance.StartCoroutine(this.Unstealthy());
        }
        private IEnumerator Unstealthy()
        {
            yield return new WaitForSeconds(0.15f);
            LastOwner.OnDidUnstealthyAction += this.BreakStealth;
            yield break;
        }
        private void OnDamaged(float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection)
        {
            this.BreakStealth(LastOwner);
        }
        private void BreakStealthOnSteal(PlayerController arg1, ShopItemController arg2)
        {
            this.BreakStealth(arg1);
        }
        private void BreakStealth(PlayerController player)
        {
            player.ChangeSpecialShaderFlag(1, 0f);
            player.OnItemStolen -= this.BreakStealthOnSteal;
            player.SetIsStealthed(false, "shade");
            player.healthHaver.OnDamaged -= this.OnDamaged;
            player.SetCapableOfStealing(false, "shade", null);
            player.OnDidUnstealthyAction -= this.BreakStealth;
            AkSoundEngine.PostEvent("Play_ENM_wizardred_appear_01", base.gameObject);
        }
        private float duration = 10f;
    }
}
