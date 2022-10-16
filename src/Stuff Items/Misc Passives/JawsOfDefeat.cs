using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ItemAPI;
using SaveAPI;
using UnityEngine;

namespace NevernamedsItems
{
    public class JawsOfDefeat : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Jaws Of Defeat";
            string resourceName = "NevernamedsItems/Resources/jawsofdefeat_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<JawsOfDefeat>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Press To See Graveyard";
            string longDesc = "Increases Damage and Firerate by 0.5% for every death on the current save file, up to 1000 deaths."+"\n\nA charm worn by the very first adventurer... ever.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 3f, StatModifier.ModifyMethod.ADDITIVE);
            item.quality = PickupObject.ItemQuality.S;
            JawsOfDefeatID = item.PickupObjectId;
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.BOSSRUSH_SHADE, true);

        }
        public override void Pickup(PlayerController player)
        {
            if (!m_pickedUpThisRun)
            {
                float deaths = GameStatsManager.Instance.GetPlayerStatValue(TrackedStats.NUMBER_DEATHS);
                if (deaths > 0)
                {
                    float multiplier = (0.005f * deaths) + 1;
                    float finalMult = Mathf.Min(multiplier, 6);
                    AlterItemStats.AddStatToPassive(this, PlayerStats.StatType.Damage, finalMult, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    AlterItemStats.AddStatToPassive(this, PlayerStats.StatType.RateOfFire, finalMult, StatModifier.ModifyMethod.MULTIPLICATIVE);
                }
            }
            base.Pickup(player);
        }
        public static int JawsOfDefeatID;
    }
}
