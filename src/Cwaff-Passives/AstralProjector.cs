using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using Mono.Cecil.Cil; //Instruction

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace CwaffingTheGungy
{

    /* TODO:
        -
    */

    public class AstralProjector : PassiveItem
    {
        public static string ItemName         = "Astral Projector";
        public static string SpritePath       = "CwaffingTheGungy/Resources/ItemSprites/astral_projector_icon";
        public static string ShortDescription = "No Clipping Life";
        public static string LongDescription  = "TBD.\n\n";

        private const float _UNPHASE_TIMER = 0.5f; // delay before being unphasing and being able to perform non-movement actions

        private bool _phased = false;
        private int _insideWalls = 0;
        private float _unphaseTimer = 0.0f;
        private Shader _originalShader;
        private Position _lastGoodPosition;
        private RoomHandler _phasedRoom;

        private static int astralProjectorId;
        private static Hook astralProjectorHook;
        private static ILHook astralProjectorILHook;

        public static void Init()
        {
            PickupObject item  = Lazy.SetupPassive<AstralProjector>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality       = PickupObject.ItemQuality.A;

            astralProjectorId   = item.PickupObjectId;
            // astralProjectorHook = new Hook(
            //     typeof(PlayerController).GetMethod("HandlePlayerInput", BindingFlags.Instance | BindingFlags.NonPublic),
            //     typeof(AstralProjector).GetMethod("HandlePlayerPhasingInput", BindingFlags.Static | BindingFlags.NonPublic)
            //     );
            astralProjectorILHook = new ILHook(
                typeof(PlayerController).GetMethod("HandlePlayerInput", BindingFlags.Instance | BindingFlags.NonPublic),
                HandlePlayerPhasingInputIL
                );
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            this._lastGoodPosition = this.Owner.specRigidbody.Position;
            player.specRigidbody.OnPreTileCollision += this.OnPreTileCollision;
        }

        public override DebrisObject Drop(PlayerController player)
        {
            player.specRigidbody.OnPreTileCollision -= this.OnPreTileCollision;
            return base.Drop(player);
        }

        private static PrototypeDungeonRoom.RoomCategory[] _BannedRoomTypes = {
            PrototypeDungeonRoom.RoomCategory.BOSS,
            PrototypeDungeonRoom.RoomCategory.CONNECTOR,
            PrototypeDungeonRoom.RoomCategory.ENTRANCE,
            PrototypeDungeonRoom.RoomCategory.EXIT,
        };

        private bool CanStartPhase()
        {
            if (!this.Owner)
                return false; // can't phase if we're not owner
            if (this.Owner.CurrentInputState != PlayerInputState.AllInput)
                return false; // can't phase if we're not fully mobile
            if (_BannedRoomTypes.Contains(this.Owner.CurrentRoom.area.PrototypeRoomCategory))
                return false; // can only phase in normal rooms
            if (this.Owner.CurrentRoom.area.IsProceduralRoom || (this.Owner.CurrentRoom.area.proceduralCells?.Count ?? 0) > 0)
                return false; // can only phase in non-procedural rooms
            return true;
        }

        public override void Update()
        {
            base.Update();
            this.CanBeDropped = !this._phased;
            if (!this._phased)
                return;

            if (this.Owner.ForceConstrainToRoom(this._phasedRoom))
                return;

            this._lastGoodPosition = this.Owner.specRigidbody.Position;
            this._insideWalls -= 1;
            this._unphaseTimer -= BraveTime.DeltaTime;
            if (this._unphaseTimer <= 0f)
                DoUnphase();
        }

        private void OnPreTileCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, PhysicsEngine.Tile other, PixelCollider otherPixelCollider)
        {
            bool xwithin;
            bool ywithin;
            RoomHandler targetRoom = this._phased ? this._phasedRoom : this.Owner.CurrentRoom;
            if (this._phased)
                this.Owner.ForceConstrainToRoom(targetRoom);
            else if (!CanStartPhase())
                return; // can't phase here
            else if (!this.Owner.FullyWithinRoom(out xwithin, out ywithin, targetRoom))
                return; // outside the room

            this._phasedRoom = this.Owner.CurrentRoom;
            this._insideWalls = 2;  // 2 frames of leniency for checking if we're inside walls
            this._unphaseTimer = _UNPHASE_TIMER;
            PhysicsEngine.SkipCollision = true;

            if (!this._phased)
                DoPhase();
        }

        private void PushOutOfWalls(bool xwithin, bool ywithin)
        {
            Position curPosition = this.Owner.specRigidbody.Position;
            if (xwithin)
                curPosition.Y = this._lastGoodPosition.Y;
            else if (ywithin)
                curPosition.X = this._lastGoodPosition.X;
            else
                curPosition = new Position(this._lastGoodPosition);
            this.Owner.specRigidbody.Position = curPosition;
        }

        private void DoPhase()
        {
            this._phased = true;

            // this.Owner.SetInputOverride()
            this.Owner.CurrentInputState = PlayerInputState.FoyerInputOnly;
            tk2dBaseSprite sprite = this.Owner.sprite;
            sprite.usesOverrideMaterial = true;
            this._originalShader = sprite.renderer.material.shader;
            sprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/HologramShader");
            AkSoundEngine.PostEvent("phase_through_wall_sound_stop_all", this.Owner.gameObject);
            AkSoundEngine.PostEvent("phase_through_wall_sound", this.Owner.gameObject);
        }

        private void DoUnphase()
        {
            this._phased = false;

            this.Owner.CurrentInputState = PlayerInputState.AllInput;
            tk2dBaseSprite sprite = this.Owner.sprite;
            sprite.usesOverrideMaterial = true;
            sprite.renderer.material.shader = this._originalShader;
            AkSoundEngine.PostEvent("phase_through_wall_sound_stop_all", this.Owner.gameObject);
            AkSoundEngine.PostEvent("phase_through_wall_sound", this.Owner.gameObject);
        }

        static bool didOnce = false;
        /* References:
            https://en.wikipedia.org/wiki/List_of_CIL_instructions
            https://github.com/StrawberryJam2021/StrawberryJam2021/blob/21079f1c2521aa704fc5ddc91f67ff3ebc95c317/Triggers/SkateboardTrigger.cs#L18
            https://github.com/lostinnowhere314/CelesteCollabUtils2/blob/b6b7fde825a6bdc218d201bf1a4feaa709487f3b/Entities/MiniHeartDoor.cs#L17
        */
        private static void HandlePlayerPhasingInputIL(ILContext il)
        {
            if (didOnce)
                return;
            didOnce = true;
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(0.01f)))
            {
                // ETGModConsole.Log($"FOUND");
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_R4, 999f);
            }
            cursor.Index = 0;
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(-0.01f)))
            {
                // ETGModConsole.Log($"FOUND");
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_R4, -999f);
            }

            // {
            //     ETGModConsole.Log($"found cursor:");
            //     foreach (Instruction c in cursor.Instrs)
            //     {
            //         try
            //         {
            //             ETGModConsole.Log($"  {c.ToStringSafe()}");
            //             if (c.MatchLdcR4(0.01f)) // checking if speed vector.x/y is greater than 0.01f
            //             {
            //                 cursor.Emit(OpCodes.Pop);
            //                 cursor.Emit(OpCodes.Ldc_R4, 999f);
            //                 // ETGModConsole.Log($"    FOUND");
            //             }
            //             else if (c.MatchLdcR4(0.01f)) // checking if speed vector.x/y is greater than 0.01f
            //             {
            //                 cursor.Emit(OpCodes.Pop);
            //                 cursor.Emit(OpCodes.Ldc_R4, -999f);
            //                 // ETGModConsole.Log($"    FOUND");
            //             }
            //         }
            //         catch (Exception e)
            //         {
            //             ETGModConsole.Log($"  <error>");
            //         }
            //     }
            //     break;
            // }
            return;// Vector2.zero;
        }


        // we have to do this nonsense because even when we're ignoring tile collisions, HandlePlayerInput insists on zeroing our movement unless we're rolling
        private static Vector2 HandlePlayerPhasingInput(Func<PlayerController, Vector2> orig, PlayerController player)
        {
            // Run the original movement function and return its output if we don't have this item
            Vector2 ovec = orig(player);
            if (!player.passiveItems.Contains(astralProjectorId))
                return ovec;

            #region Perform all of the original checks to make sure we're not doing illegal movements
                if (player.m_activeActions == null)
                    return ovec; // If we have no active actions, we're not doing anything, so return
                if (player.CurrentInputState == PlayerInputState.NoMovement)
                    return ovec; // AdjustInputVector never gets called if we are in the NoMovement input state, so return
                if (player.IsGhost)
                    return ovec; // Original function checks if we're a ghost and returns immediately if so. Ironically, we need to respect walls if we're a ghost
            #endregion

            // If we've made it here, then only the RigidbodyCast() functions could have reset our movement vector to zero, so recalculate AdjustInputVector()
            Vector2 moveVector = player.AdjustInputVector(player.m_activeActions.Move.Vector, BraveInput.MagnetAngles.movementCardinal, BraveInput.MagnetAngles.movementOrdinal);
            if (moveVector.magnitude > 1f)
                moveVector.Normalize();
            return moveVector;
        }
    }
}
