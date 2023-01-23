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
            GameObject posObject = new GameObject("Position Object");
                posObject.transform.position = position.ToVector3ZisY(-1f);
            GameObject noteItem = new GameObject("Position Object Note Item");
                noteItem.transform.parent        = posObject.transform;
                noteItem.transform.localPosition = Vector3.zero;
                noteItem.transform.position      = Vector3.zero;
            tk2dSprite noteSpriteComp = noteItem.GetOrAddComponent<tk2dSprite>();
                noteSpriteComp.SetSprite(noteSprite.Collection, noteSprite.spriteId);
                noteSpriteComp.PlaceAtPositionByAnchor(noteItem.transform.parent.position, tk2dBaseSprite.Anchor.LowerCenter);
                // noteSpriteComp.transform.position = noteSpriteComp.transform.position.Quantize(0.0625f);
                // noteSpriteComp.renderLayer += 1;
                // noteSpriteComp.depthUsesTrimmedBounds = true;
                // noteSpriteComp.HeightOffGround = 1.25f;
                // noteSpriteComp.UpdateZDepth();
                // DepthLookupManager.ProcessRenderer(noteSpriteComp.renderer);

                NoteDoer noteDoer = noteItem.AddComponent<NoteDoer>();
                    noteDoer.stringKey = formattedNoteText;
                    noteDoer.DestroyedOnFinish = true;
                    noteDoer.alreadyLocalized = true;
                    noteDoer.textboxSpawnPoint = noteItem.transform;
                    noteDoer.noteBackgroundType = NoteDoer.NoteBackgroundType.NOTE;
                    position.GetAbsoluteRoom().RegisterInteractable(noteDoer);

                SpriteOutlineManager.ToggleOutlineRenderers(noteSpriteComp, false);

                // GameObject gameObject2 = (GameObject)UnityEngine.Object.Instantiate(ResourceCache.Acquire("Global VFX/VFX_Item_Spawn_Poof"));
                // tk2dBaseSprite component2 = gameObject2.GetComponent<tk2dBaseSprite>();
                // component2.PlaceAtPositionByAnchor(item.sprite.WorldCenter.ToVector3ZUp(), tk2dBaseSprite.Anchor.MiddleCenter);
                // component2.transform.position = component2.transform.position.Quantize(0.0625f);
                // component2.HeightOffGround = 5f;
                // component2.UpdateZDepth();
        }
    }
}
