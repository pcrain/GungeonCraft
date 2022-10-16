using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public class SpringloadedChamber : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Springloaded Chamber";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/springloadedchamber_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<SpringloadedChamber>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Marvellous Mechanism";
            string longDesc = "Increases damage by 30% for the first half of the clip, but decreases it by 20% for the second."+"\n\nA miraculous clockwork doodad cannibalised from the Wind Up Gun. Proof that springs are, and will always be, the best form of potential energy.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.ReloadSpeed, 0.8f, StatModifier.ModifyMethod.MULTIPLICATIVE);

            //Adds the actual passive effect to the item
            //ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.ProjectileSpeed, -1f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.B; //B
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_SPRINGLOADEDCHAMBER, true);
            item.AddItemToTrorcMetaShop(19);

            ID = item.PickupObjectId;
        }
        private int currentClip, lastClip;
        private Gun currentGun, lastGun;
        public static int ID;
        public override void Update()
        {
            if (Owner)
            {
                currentClip = Owner.CurrentGun.ClipShotsRemaining;
                currentGun = Owner.CurrentGun;
                if (currentClip != lastClip || currentGun != lastGun)
                {
                    int clipSizeThing = 2;
                    if (Owner.HasPickupID(334)) clipSizeThing = 4;
                    if (Owner.CurrentGun.ClipShotsRemaining > Owner.CurrentGun.ClipCapacity / clipSizeThing)
                    {
                        //ETGModConsole.Log("dmg up");
                        DamageBonusState();
                    }
                    else
                    {
                        //ETGModConsole.Log("dmg down");
                        DamageDownState();
                    }
                    lastClip = currentClip;
                    lastGun = currentGun;
                }

            }
        }
        private bool DamageIsUP;
        private void DamageBonusState()
        {
            if (!DamageIsUP)
            {
                RemoveStat(PlayerStats.StatType.Damage);
                RemoveStat(PlayerStats.StatType.PlayerBulletScale);                
                AddStat(PlayerStats.StatType.Damage, 1.3f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                AddStat(PlayerStats.StatType.PlayerBulletScale, 1.2f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                DamageIsUP = true;
                Owner.stats.RecalculateStats(Owner, true, false);
            }
        }
        private void DamageDownState()
        {
            if (DamageIsUP)
            {
                RemoveStat(PlayerStats.StatType.Damage);
                RemoveStat(PlayerStats.StatType.PlayerBulletScale);
                float damageMod = 0.8f;
                float scaleMod = 0.8f;
                if (Owner.HasPickupID(69))
                {
                    damageMod = 1;
                    scaleMod = 1;
                }
                AddStat(PlayerStats.StatType.PlayerBulletScale, scaleMod, StatModifier.ModifyMethod.MULTIPLICATIVE);
                AddStat(PlayerStats.StatType.Damage, damageMod, StatModifier.ModifyMethod.MULTIPLICATIVE);
                Owner.stats.RecalculateStats(Owner, true, false);
                DamageIsUP = false;
            }
        }
        private void AddStat(PlayerStats.StatType statType, float amount, StatModifier.ModifyMethod method = StatModifier.ModifyMethod.ADDITIVE)
        {
            StatModifier modifier = new StatModifier
            {
                amount = amount,
                statToBoost = statType,
                modifyType = method
            };
            if (this.passiveStatModifiers == null)
                this.passiveStatModifiers = new StatModifier[] { modifier };
            else
                this.passiveStatModifiers = this.passiveStatModifiers.Concat(new StatModifier[] { modifier }).ToArray();
        }

        private void RemoveStat(PlayerStats.StatType statType)
        {
            var newModifiers = new List<StatModifier>();
            for (int i = 0; i < passiveStatModifiers.Length; i++)
            {
                if (passiveStatModifiers[i].statToBoost != statType)
                    newModifiers.Add(passiveStatModifiers[i]);
            }
            this.passiveStatModifiers = newModifiers.ToArray();
        }
    }
}
