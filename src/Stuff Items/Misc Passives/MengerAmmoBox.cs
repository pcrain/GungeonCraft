using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Alexandria.ItemAPI;
using Alexandria.Misc;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace NevernamedsItems
{
    public class MengerAmmoBox : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Menger Ammo Box";
            string resourceName = "NevernamedsItems/Resources/mengerammobox_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<MengerAmmoBox>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Fractal Replenishment";
            string longDesc = "Equalises regular and spread ammo boxes."+"\n\nA delicate fractal of infinitely patterned bullets. Occasionally coughs up lead from the void.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.B;

            MengerAmmoBoxID = item.PickupObjectId;
        }
        public static int MengerAmmoBoxID;
        public void OnAmmoCollected(PlayerController player, AmmoPickup self)
        {
            if (self.mode == AmmoPickup.AmmoPickupMode.FULL_AMMO)
            {
                for (int i = 0; i < player.inventory.AllGuns.Count; i++)
                {
                    if (player.inventory.AllGuns[i] && player.CurrentGun != player.inventory.AllGuns[i])
                    {
                        player.inventory.AllGuns[i].GainAmmo(Mathf.FloorToInt((float)player.inventory.AllGuns[i].AdjustedMaxAmmo * 0.2f));
                    }
                }
                player.CurrentGun.ForceImmediateReload(false);
                if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
                {
                    PlayerController otherPlayer = GameManager.Instance.GetOtherPlayer(player);
                    if (!otherPlayer.IsGhost)
                    {
                        for (int j = 0; j < otherPlayer.inventory.AllGuns.Count; j++)
                        {
                            if (otherPlayer.inventory.AllGuns[j])
                            {
                                otherPlayer.inventory.AllGuns[j].GainAmmo(Mathf.FloorToInt((float)otherPlayer.inventory.AllGuns[j].AdjustedMaxAmmo * 0.2f));
                            }
                        }
                        otherPlayer.CurrentGun.ForceImmediateReload(false);
                    }
                }
            }
            else if (self.mode == AmmoPickup.AmmoPickupMode.SPREAD_AMMO)
            {
                if (player.CurrentGun != null && player.CurrentGun.CanGainAmmo)
                {
                    player.CurrentGun.GainAmmo(player.CurrentGun.AdjustedMaxAmmo);
                    player.CurrentGun.ForceImmediateReload(false);
                }
            }
        }
        public override void Pickup(PlayerController player)
        {
            if (player.GetExtComp()) player.GetExtComp().OnPickedUpAmmo += OnAmmoCollected;
            base.Pickup(player);
        }
        public override void DisableEffect(PlayerController player)
        {
            if (player.GetExtComp()) player.GetExtComp().OnPickedUpAmmo -= OnAmmoCollected;
            base.DisableEffect(player);
        }
    }
}