using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace CwaffingTheGungy
{
    public class BlankChecks : PassiveItem
    {
        public static string ItemName         = "Blank Checks";
        public static string SpritePath       = "CwaffingTheGungy/Resources/ItemSprites/blank_checks_icon";
        public static string ShortDescription = "Write-off";
        public static string LongDescription  = "Trying to use a blank without one in your inventory gives you 3 blanks and +1 curse. Will not work if you already have 10 or more curse.\n\nRumor has it that blank checks were originally conceived of outside the domain of weaponry entirely, and were developed primarily for use in large-scale business transactions. As firearms are only very rarely involved in such transactions, why so many business people have any use for extra blanks remains a mystery to this day.";

        public static void Init()
        {
            PickupObject item  = Lazy.SetupPassive<BlankChecks>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality       = PickupObject.ItemQuality.B;
        }

        public override void Pickup(PlayerController player)
        {
            if (!this.m_pickedUpThisRun)
                GameManager.Instance.PrimaryPlayer.Blanks += 1;
            base.Pickup(player);
        }

        public override void Update()
        {
            base.Update();
            if (!this.Owner || this.Owner.Blanks > 0)
                return; // If we're ownerless or our owner has more than 1 blank, we have nothing to do

            if (Time.timeScale <= 0f || this.Owner.m_blankCooldownTimer > 0f)
                return; // Can't use blanks when paused or on cooldown

            if (PlayerStats.GetTotalCurse() >= 10)
                return; // Won't work past 10 curse

            BraveInput instanceForPlayer = BraveInput.GetInstanceForPlayer(this.Owner.PlayerIDX);
            GameOptions.ControllerBlankControl blanker =
                (this.Owner.IsPrimaryPlayer ? GameManager.Options.additionalBlankControl : GameManager.Options.additionalBlankControlTwo);

            bool tryingToUseBlank = false;
            tryingToUseBlank |= instanceForPlayer.GetButtonDown(GungeonActions.GungeonActionType.Blank);
            tryingToUseBlank |= this.Owner.m_activeActions.BlankAction.WasPressed;
            tryingToUseBlank |= (blanker == GameOptions.ControllerBlankControl.BOTH_STICKS_DOWN) && this.Owner.m_activeActions.CheckBothSticksButton();
            if (!tryingToUseBlank)
                return; // If we're not trying to use a blank, we have nothing to do

            this.Owner.Blanks += 3;

            StatModifier statModifier = new StatModifier();
                statModifier.amount = 1f;
                statModifier.modifyType = StatModifier.ModifyMethod.ADDITIVE;
                statModifier.statToBoost = PlayerStats.StatType.Curse;
            this.Owner.ownerlessStatModifiers.Add(statModifier);
            this.Owner.stats.RecalculateStats(this.Owner);
        }
    }
}
