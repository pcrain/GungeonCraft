using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Alexandria.ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;
using SaveAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class RustyCasing : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Rusty Casing";
            string resourceName = "NevernamedsItems/Resources/rustycasing_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<RustyCasing>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Heheheheheheh";
            string longDesc = "Yesyesyoulikestufffyouneeedmoney." + "\nThisgiveyoumoneyyesyesyes." + "\n\nHeheheheheheh";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.D;

            RustyCasingID = item.PickupObjectId;
            item.SetupUnlockOnCustomStat(CustomTrackedStats.RUSTY_ITEMS_PURCHASED, 2, DungeonPrerequisite.PrerequisiteOperation.GREATER_THAN);
        }
        public static int RustyCasingID;
        public override void Pickup(PlayerController player)
        {
            player.GetExtComp().OnPickedUpAmmo += OnAmmo;
            player.GetExtComp().OnPickedUpBlank += OnBlank;
            player.GetExtComp().OnPickedUpKey += OnKey;
            player.GetExtComp().OnPickedUpHP += OnHealth;
            base.Pickup(player);
        }
        public override void DisableEffect(PlayerController player)
        {
            player.GetExtComp().OnPickedUpAmmo -= OnAmmo;
            player.GetExtComp().OnPickedUpBlank -= OnBlank;
            player.GetExtComp().OnPickedUpKey -= OnKey;
            player.GetExtComp().OnPickedUpHP -= OnHealth;
            base.DisableEffect(player);
        }
        public void OnAmmo(PlayerController player, AmmoPickup ammo) { GiveCash(player, 6); }
        public void OnBlank(SilencerItem blank, PlayerController player) {  GiveCash(player, 4); }
        public void OnKey(PlayerController player, KeyBulletPickup key)
        {
            if (key.IsRatKey) GiveCash(player, 10);
            else GiveCash(player, 6);
        }
        public void OnHealth(PlayerController player, HealthPickup hp)
        {
            if (hp.armorAmount > 0) GiveCash(player, 8);
            else GiveCash(player, Mathf.CeilToInt(hp.healAmount * 8f));
        }
        public static void GiveCash(PlayerController player, int cashAmount)
        {
            if (player.PlayerHasActiveSynergy("Lost, Never Found")) cashAmount += 5;
            if (cashAmount > 0) LootEngine.SpawnCurrency(player.sprite.WorldCenter, cashAmount);
        }
    }
}
