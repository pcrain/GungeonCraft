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

    /* TODO:
        - fix uneven border sizes on each edge of rooms (i.e., right border is too narrow and left border is too wide)
    */

    public class AstralProjector : PassiveItem
    {
        public static string ItemName         = "Astral Projector";
        public static string SpritePath       = "CwaffingTheGungy/Resources/ItemSprites/astral_projector_icon";
        public static string ShortDescription = "No Clipping Life";
        public static string LongDescription  = "TBD.\n\n";

        internal const float _PHASE_DAMAGE_SCALING = 0.5f;

        private const float _ROOM_BORDER_WIDTH = 1f; // number of cell lengths that make up each room's border
        private const float _LENIENCE = 0.5f; // prevents certain projectiles that leave debris from getting stuck in the wall
        private const float _INSET = _ROOM_BORDER_WIDTH + _LENIENCE;

        private const float _INSET_TOP    = 0f;
        private const float _INSET_RIGHT  = 1f;
        private const float _INSET_BOTTOM = 1f;
        private const float _INSET_LEFT   = 0f;

        private const float _UNPHASE_TIMER = 0.5f; // half a second

        private bool _phased = false;
        private int _insideWalls = 0;
        private float _unphaseTimer = 0.0f;
        private Shader _originalShader;
        private Position _lastGoodPosition;
        private RoomHandler _phasedRoom;
        // private GameObject _pseudoCollider = null;

        private static int astralProjectorId;
        private static Hook astralProjectorHook;

        public static void Init()
        {
            PickupObject item  = Lazy.SetupPassive<AstralProjector>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality       = PickupObject.ItemQuality.A;

            astralProjectorId   = IDs.Passives["astral_projector"];
            astralProjectorHook = new Hook(
                typeof(PlayerController).GetMethod("HandlePlayerInput", BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(AstralProjector).GetMethod("HandlePlayerPhasingInput", BindingFlags.Static | BindingFlags.NonPublic)
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

        public override void Update()
        {
            base.Update();
            this.CanBeDropped = !this._phased;
            if (!this._phased)
                return;

            // bool xwithin;
            // bool ywithin;
            // if (!this.Owner.FullyWithinRoom(out xwithin, out ywithin, this._phasedRoom))
            // {
            //     PushOutOfWalls(xwithin, ywithin);
            //     this._lastGoodPosition = this.Owner.specRigidbody.Position;
            //     this._unphaseTimer = _UNPHASE_TIMER;
            //     return;
            // }
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
            else if (!this.Owner.FullyWithinRoom(out xwithin, out ywithin, targetRoom))
            {
                // if (this._phased)
                // {
                //     this.Owner.ForceConstrainToRoom(this._phasedRoom);
                //     // PushOutOfWalls(xwithin, ywithin);
                //     // this._lastGoodPosition = this.Owner.specRigidbody.Position;
                // }
                return; // outside the room
            }

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
            ETGModConsole.Log($"PHASED");
            this._phased = true;

            tk2dBaseSprite sprite = this.Owner.sprite;
            sprite.usesOverrideMaterial = true;
            this._originalShader = sprite.renderer.material.shader;
            sprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/HologramShader");
            AkSoundEngine.PostEvent("phase_through_wall_sound_stop_all", this.Owner.gameObject);
            AkSoundEngine.PostEvent("phase_through_wall_sound", this.Owner.gameObject);
        }

        private void DoUnphase()
        {
            ETGModConsole.Log($"UNPHASED");
            this._phased = false;

            tk2dBaseSprite sprite = this.Owner.sprite;
            sprite.usesOverrideMaterial = true;
            sprite.renderer.material.shader = this._originalShader;
            AkSoundEngine.PostEvent("phase_through_wall_sound_stop_all", this.Owner.gameObject);
            AkSoundEngine.PostEvent("phase_through_wall_sound", this.Owner.gameObject);
        }

        // we have to do this nonsense because even when we're ignoring tile collisions, HandlePlayerInput insists on setting our movement vector to zero
        private static Vector2 HandlePlayerPhasingInput(Func<PlayerController, Vector2> orig, PlayerController player)
        {
            Vector2 ovec = orig(player);
            if (!player.passiveItems.Contains(astralProjectorId))
                return ovec;

            Vector2 moveVector = player.AdjustInputVector(player.m_activeActions.Move.Vector, BraveInput.MagnetAngles.movementCardinal, BraveInput.MagnetAngles.movementOrdinal);
            if (moveVector.magnitude > 1f)
                moveVector.Normalize();
            return moveVector;
        }
    }
}
