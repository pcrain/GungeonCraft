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
            GameObject posObject = new GameObject("Position Object");
                // posObject.transform.parent = this.transform;
                posObject.transform.position = position.ToVector3ZisY();
            GameObject noteItem = new GameObject("Position Object Note Item");
                noteItem.transform.parent        = posObject.transform;
                noteItem.transform.localPosition = Vector3.zero;
                noteItem.transform.position      = Vector3.zero;
            tk2dSprite noteSpriteComp = noteItem.AddComponent<tk2dSprite>();
                noteSpriteComp.SetSprite(noteSprite.Collection, noteSprite.spriteId);
                noteSpriteComp.PlaceAtPositionByAnchor(noteItem.transform.parent.position, tk2dBaseSprite.Anchor.MiddleCenter);
                noteSpriteComp.transform.position = noteSpriteComp.transform.position.Quantize(0.0625f);
                DepthLookupManager.ProcessRenderer(noteSpriteComp.renderer);
                noteSpriteComp.UpdateZDepth();
                // noteSpriteComp.renderer.SetAlpha();
                // noteItem.transform.parent.gameObject.GetComponentInParent<tk2dSprite>()?.AttachRenderer(noteSpriteComp);
                // SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black, 0.1f, 0.05f);

            // noteItem.SetActive(false); //make sure the projectile isn't an active game object
            // FakePrefab.MarkAsFakePrefab(noteItem);  //mark the projectile as a prefab
            // UnityEngine.Object.DontDestroyOnLoad(noteItem); //make sure the projectile isn't destroyed when loaded as a prefab

            // GameObject newNote = SpawnObjectManager.SpawnObject(noteItem, position.ToVector3ZisY());
                // NoteDoer noteDoer = newNote.AddComponent<NoteDoer>();
                NoteDoer noteDoer = noteItem.AddComponent<NoteDoer>();
                    noteDoer.stringKey = formattedNoteText;
                    noteDoer.alreadyLocalized = true;
                    noteDoer.DestroyedOnFinish = true;
                    // noteDoer.textboxSpawnPoint = newNote.transform;
                    noteDoer.textboxSpawnPoint = noteItem.transform;
                    noteDoer.noteBackgroundType = NoteDoer.NoteBackgroundType.NOTE;
                    position.GetAbsoluteRoom().RegisterInteractable(noteDoer);
        }
    }
}
