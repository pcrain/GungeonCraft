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
    public class FourDBullets : PassiveItem
    {
        public static string ItemName         = "4D Bullets";
        public static string SpritePath       = "CwaffingTheGungy/Resources/ItemSprites/4d_bullets_icon";
        public static string ShortDescription = "Thinking Outside the Tesseract";
        public static string LongDescription  = "Bullets can phase through the inner walls of a room, but lose half their power for every wall they phase through.\n\n";

        internal const float _PHASE_DAMAGE_SCALING = 0.5f;

        public static void Init()
        {
            PickupObject item  = Lazy.SetupPassive<FourDBullets>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality       = PickupObject.ItemQuality.B;
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProjectile;
        }

        public override DebrisObject Drop(PlayerController player)
        {
            player.PostProcessProjectile -= this.PostProcessProjectile;
            return base.Drop(player);
        }

        private void PostProcessProjectile(Projectile proj, float effectChanceScalar)
        {
            if (this.Owner is not PlayerController player)
                return;

            proj.gameObject.AddComponent<PhaseThroughInnerWallsBehavior>();
        }
    }

    public class PhaseThroughInnerWallsBehavior : MonoBehaviour
    {
        private const float _ROOM_BORDER_WIDTH = 1f; // number of cell lengths that make up each room's border
        private const float _LENIENCE = 0.5f; // prevents certain projectiles that leave debris from getting stuck in the wall
        private const float _INSET = _ROOM_BORDER_WIDTH + _LENIENCE;
        private const float _UNPHASE_TIMER = 0.05f; // 3 frames

        private Projectile _projectile;
        private PlayerController _owner;
        private RoomHandler _startingRoom;
        private Shader _originalShader;

        private bool _leftStartingRoom = false;
        private bool _collidedWithWall = false;
        private bool _phased = false;
        private float _unphaseTimer = 0.0f;

        private void Start()
        {
            this._projectile = base.GetComponent<Projectile>();
            this._owner = this._projectile.Owner as PlayerController;

            // this._projectile.BulletScriptSettings.surviveTileCollisions = true;
            this._startingRoom = this._projectile.transform.position.GetAbsoluteRoom();
            this._projectile.specRigidbody.OnPreTileCollision += this.OnPreTileCollision;
            this._originalShader = this._projectile.sprite.renderer.material.shader;

            // DoPhaseFX();
        }

        private void OnPreTileCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, PhysicsEngine.Tile other, PixelCollider otherPixelCollider)
        {
            RoomHandler currentRoom = this._projectile.transform.position.GetAbsoluteRoom();
            if (currentRoom != this._startingRoom)
                return;

            if (!currentRoom.GetBoundingRect().Inset(_INSET).Contains(this._projectile.transform.position))
                return; // outside the room

            this._unphaseTimer = _UNPHASE_TIMER;
            PhysicsEngine.SkipCollision = true;
            if (this._phased)
                return;

            DoPhaseFX();
        }

        private void Update()
        {
            if (!this._phased)
                return;

            RoomHandler currentRoom = this._projectile.transform.position.GetAbsoluteRoom();
            if (!currentRoom.GetBoundingRect().Inset(_INSET).Contains(this._projectile.transform.position))
                return;

            this._unphaseTimer -= BraveTime.DeltaTime;
            if (this._unphaseTimer <= 0f)
                this._phased = false;
        }

        private void DoPhaseFX()
        {
            this._phased = true;
            this._projectile.baseData.damage *= FourDBullets._PHASE_DAMAGE_SCALING;

            tk2dBaseSprite sprite = this._projectile.sprite;
            sprite.usesOverrideMaterial = true;
            sprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/HologramShader");
            AkSoundEngine.PostEvent("phase_through_wall_sound_stop_all", base.gameObject);
            AkSoundEngine.PostEvent("phase_through_wall_sound", base.gameObject);
        }
    }
}
