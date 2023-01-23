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
using ItemAPI;
using Alexandria.Misc;

namespace CwaffingTheGungy
{
    public class CuratorsBadge : PassiveItem
    {
        public static string passiveName      = "Curator's Badge";
        public static string spritePath       = "CwaffingTheGungy/Resources/ItemSprites/rat_poison_icon";
        public static string shortDescription = "Neat and Tidy";
        public static string longDescription  = "(Get shells for leaving minor breakables unscathed)";

        private int curRoomBreakables = 0;
        private int maxRoomBreakables = 0;

        public static void Init()
        {
            PickupObject item  = Lazy.SetupItem<CuratorsBadge>(passiveName, spritePath, shortDescription, longDescription, "cg");
            item.quality       = PickupObject.ItemQuality.C;
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnEnteredCombat += this.OnEnteredCombat;

            CustomNoteDoer.CreateNote(player.sprite.WorldCenter, "hello, world C:",
                customSprite: this.GetComponent<tk2dSprite>());
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
            RoomHandler currentRoom = GameManager.Instance.PrimaryPlayer.CurrentRoom;
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
            ETGModConsole.Log("started with " + this.maxRoomBreakables + " breakables");
            currentRoom.OnEnemiesCleared += this.OnRoomCleared;
        }

        private void OnRoomCleared()
        {
            ETGModConsole.Log("ended with " + this.curRoomBreakables + " / " + this.maxRoomBreakables + " breakables");
        }

        private void HandleBroken(MinorBreakable mb)
        {
            --this.curRoomBreakables;
        }
    }
}
