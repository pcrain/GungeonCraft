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

            GenerateNoteAtPosition(player.sprite.WorldCenter,"hello, world C:", this.GetComponent<tk2dSprite>());

            // GameObject debrisObject = SpriteBuilder.SpriteFromResource("CwaffingTheGungy/Resources/ItemSprites/zoolander_icon", null);
            // FakePrefab.MarkAsFakePrefab(debrisObject);
            // tk2dSprite tk2dsprite = debrisObject.GetComponent<tk2dSprite>();
            // GenerateNoteAtPosition(player.sprite.WorldCenter,"hello, world C:", tk2dsprite);
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

        public void GenerateNoteAtPosition(Vector2 position, String formattedNoteText, tk2dSprite noteSprite)
        {
            GameObject noteItem = new GameObject("Custom Note Item");
            tk2dSprite noteSpriteComp = noteItem.GetOrAddComponent<tk2dSprite>();
                noteSpriteComp.SetSprite(noteSprite.Collection, noteSprite.spriteId);
                noteSpriteComp.PlaceAtPositionByAnchor(noteItem.transform.position, tk2dBaseSprite.Anchor.LowerCenter);
            NoteDoer noteDoerProto = noteItem.AddComponent<NoteDoer>();

            noteDoerProto.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(noteDoerProto.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(noteDoerProto);

            NoteDoer noteDoer = UnityEngine.Object.Instantiate(noteDoerProto.gameObject,position.ToVector3ZisY(-1f),Quaternion.identity).GetComponent<NoteDoer>();
                noteDoer.stringKey = formattedNoteText;
                noteDoer.DestroyedOnFinish = true;
                noteDoer.alreadyLocalized = true;
                noteDoer.textboxSpawnPoint = noteDoer.transform;
                noteDoer.noteBackgroundType = NoteDoer.NoteBackgroundType.NOTE;
                position.GetAbsoluteRoom().RegisterInteractable(noteDoer);
        }
    }
}
