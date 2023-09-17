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

/*
    TODO:
        - handle teleporting out of rooms or otherwise leaving them unceremoniously
*/

namespace CwaffingTheGungy
{
    public class CuratorsBadge : PassiveItem
    {
        public static string ItemName         = "Curator's Badge";
        public static string SpritePath       = "CwaffingTheGungy/Resources/ItemSprites/curators_badge_icon";
        public static string ShortDescription = "Neat and Tidy";
        public static string LongDescription  = "(Get shells for leaving minor breakables unscathed)";

        private int curRoomBreakables = 0;
        private int maxRoomBreakables = 0;
        private int chancesLeft = 3;

        public static void Init()
        {
            PickupObject item  = Lazy.SetupPassive<CuratorsBadge>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality       = PickupObject.ItemQuality.C;
        }

        public override void Pickup(PlayerController player)
        {
            player.OnEnteredCombat += this.OnEnteredCombat;

            if (this.m_pickedUpThisRun)
            {
                base.Pickup(player);
                return;
            }
            base.Pickup(player);

            string s = String.Join("\n",new[]{
                "Hey! Thanks for joining the curation crew! We like to keep an orderly Gungeon, so make sure you keep those mischievous Gundead from breaking everything.",
                "- Your Boss [sprite \"resourceful_rat_icon_001\"]"
                });
            CustomNoteDoer.CreateNote(player.sprite.WorldCenter, s);
        }

        public override DebrisObject Drop(PlayerController player)
        {
            player.OnEnteredCombat -= this.OnEnteredCombat;
            return base.Drop(player);
        }

        private void OnEnteredCombat()
        {
            if (!this.Owner)
                return;
            RoomHandler currentRoom = this.Owner.CurrentRoom;
            this.curRoomBreakables = 0;
            foreach (MinorBreakable minorBreakable in StaticReferenceManager.AllMinorBreakables)
            {
                if (minorBreakable && !minorBreakable.IsBroken && minorBreakable.CenterPoint.GetAbsoluteRoom() == currentRoom)
                {
                    ++this.curRoomBreakables;
                    minorBreakable.OnBreakContext += this.HandleBroken;
                }
            }
            this.maxRoomBreakables = this.curRoomBreakables;
            if (this.maxRoomBreakables == 0)
                return;
            currentRoom.OnEnemiesCleared += this.OnRoomCleared;
        }

        private void OnRoomCleared()
        {
            bool success = true;
            Vector2 clearSpot;
            float percentIntact = (float)this.curRoomBreakables / (float)this.maxRoomBreakables;
            // ETGModConsole.Log("ended with " + this.curRoomBreakables + " / " + this.maxRoomBreakables + " ("+percentIntact+") breakables");
            if (percentIntact < 0.4f)
            {
                if (this.Owner.CurrentRoom.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS)
                    return; // be a little more forgiving in boss rooms
                // angry note
                --chancesLeft;
                string angry;
                if (chancesLeft == 2)
                    angry = "2 chances left";
                else if (chancesLeft == 1)
                    angry = "1 chance left!!!";
                else
                    angry = "You're fired ):<";
                clearSpot = this.Owner.CurrentRoom.GetCenteredVisibleClearSpot(2,2, out success).ToVector2();
                if (success)
                    CustomNoteDoer.CreateNote(clearSpot, angry);
                if (chancesLeft == 0)
                    UnityEngine.Object.Destroy(this.Owner.DropPassiveItem(this));
                return;
            }
            if (percentIntact <= 0.7f)
                return; // nothing happens between 40 and 70%

            // 1 shell bonus for every 5% above 70%
            int percentBonus = Mathf.CeilToInt(20 * (percentIntact - 0.7f));
            // 1 shell bonus for every 10 breakables left standing
            int absoluteBonus = this.curRoomBreakables / 10;
            int shellBonus = 0;
            string happy;
            if (percentIntact == 1.0f)
            { // take the max of percent and absolute bonuses for 100% preservation
                shellBonus = Mathf.Max(percentBonus, absoluteBonus);
                happy = $"Marvelous *o*. Here's {shellBonus} casings, keep up the good work! :D\n\n- Management";
            }
            else
            { // otherwise, take the min
                shellBonus = Mathf.Min(percentBonus, absoluteBonus);
                happy = $"Here's {shellBonus} casing{(shellBonus==1?"":"s")}, keep up the good work! :D\n\n- Management";
            }
            clearSpot = this.Owner.CurrentRoom.GetCenteredVisibleClearSpot(2,2, out success).ToVector2();
            if (success)
                CustomNoteDoer.CreateNote(clearSpot, happy);
            LootEngine.SpawnCurrency(success ? clearSpot : this.Owner.CenterPosition, shellBonus, false, null, null, startingZForce: 40f);
        }

        private void HandleBroken(MinorBreakable mb)
        {
            --this.curRoomBreakables;
        }
    }
}
