using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using SaveAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class BlightShell : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Blight Shell";
            string resourceName = "NevernamedsItems/Resources/blightshell_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<BlightShell>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Apocryphal Armoury";
            string longDesc = "Grants a free shotgun every floor, as well as a free curse." + "\n\nShotgun Gundead are often neglected in death, despite their noble status. This artefact collects their souls, because nobody else will.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.B;
            item.AddToSubShop(ItemBuilder.ShopType.Cursula);

            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.JAMMEDSHOTGUNKIN_QUEST_REWARDED, true);
            BlightShellID = item.PickupObjectId;
        }
        public static int BlightShellID;
        private void OnNewFloor()
        {
            if (Owner)
            {
                giveNewShotgun();
            }
        }

        private void giveNewShotgun()
        {
            List<int> instanceList = new List<int>();
            instanceList.AddRange(shotgunIDs);
            instanceList = instanceList.RemoveInvalidIDListEntries();

            int selectedShotgun = BraveUtility.RandomElement(instanceList);

            var gameGun = (PickupObjectDatabase.GetById(selectedShotgun) as Gun);
            var gun = UnityEngine.Object.Instantiate<Gun>(gameGun);
            gun.AddPassiveStatModifier(PlayerStats.StatType.Curse, 1, StatModifier.ModifyMethod.ADDITIVE);

            Owner.inventory.AddGunToInventory(gun, true);
        }

        public static List<int> shotgunIDs = new List<int>()
        {
            347, //Shotgrub
            404, //Siren
            55, //Void Shotgun
            61, //Bundle of Wands
            93, //Old Goldie
            126, //Shotbow
            1, //Winchester
            202, //Sawed Off
            151, //The Membrane
            346, //Huntsman
            51, //Regular Shotgun
            175, //Tangler
            143, //Shotgun Full of Hate
            379, //Shotgun Full of Love
            122, //Blunderbuss
            601, //Big Shotgun
            541, //Casey
            550, //Knight's Gun
            512, //Shell
            363, //Trick Gun
            18, //Blooper
            82, //Elephant Gun
            231, //Gilded Hydra
            225, //Ice Breaker
            365, //Mass Shotgun
            123, //Pulse Cannon
            406, //Rattler
            329, //Zilla Shotgun
        };

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            if (!this.m_pickedUpThisRun)
            {
                List<int> ModdedGunIDs = new List<int>()
                {
                    JusticeGun.JusticeID,
                    Orgun.OrgunID,
                    Octagun.OctagunID, 
                    ClownShotgun.ClownShotgunID,
                    Ranger.RangerID,
                };
                shotgunIDs.AddRange(ModdedGunIDs);

                giveNewShotgun();
            }
            GameManager.Instance.OnNewLevelFullyLoaded += this.OnNewFloor;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject result = base.Drop(player);
            GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
            return result;
        }
        public override void OnDestroy()
        {
            GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
            base.OnDestroy();
        }
    }
}
