using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Gungeon;
using Dungeonator;
using System.Reflection;
using Alexandria.ItemAPI;
using System.Collections;
using System.Globalization;


/* TODO:
    - fix one frame delay on HatDepthType transitions
*/

namespace Alexandria.cAPI
{
    public class Hat : BraveBehaviour
    {

        private const float BASE_FLIP_HEIGHT = 3f;

        public string   hatName;
        public Vector3 hatOffset;
        public HatDirectionality hatDirectionality;
        public HatRollReaction hatRollReaction;
        public HatAttachLevel attachLevel;
        public string FlipStartedSound;
        public string FlipEndedSound;
        public HatDepthType hatDepthType;
        public PlayerController hatOwner;
        public float SpinSpeedMultiplier;
        public float flipHeightMultiplier;
        public bool goldenPedastal;
        public bool flipHorizontalWithPlayer;
        public float flipXOffset;

        private FieldInfo commandedField, lastNonZeroField, lockedDodgeRollDirection, m_currentGunAngle;
        private HatDirection currentDirection;
        private HatState currentState;
        public tk2dSprite hatSprite;
        public tk2dSpriteAnimator hatSpriteAnimator;
        private tk2dSpriteAnimator hatOwnerAnimator;

        private tk2dSpriteDefinition cachedDef;
        private Vector2 cachedDefOffset;
        
        private float RollLength = 0.65f; //The time it takes for a player with no dodge roll effects to roll
        public Hat()
        {
            hatOffset = new Vector2(0, 0);
            hatOwner = null;
            SpinSpeedMultiplier = 1;
            flipHeightMultiplier = 1;
            hatRollReaction = HatRollReaction.FLIP;
            hatDirectionality = HatDirectionality.NONE;
            attachLevel = HatAttachLevel.HEAD_TOP;
            hatDepthType = HatDepthType.AlwaysInFront;
            FlipEndedSound = null;
            FlipStartedSound = null;
            goldenPedastal = false;
            flipHorizontalWithPlayer = true;
            flipXOffset = 0f;
            cachedDef = null;
            cachedDefOffset = Vector2.zero;
        }

        private void Start()
        {
            hatSprite = base.GetComponent<tk2dSprite>();
            hatSpriteAnimator = base.GetComponent<tk2dSpriteAnimator>();

            commandedField = typeof(PlayerController).GetField("m_playerCommandedDirection", BindingFlags.NonPublic | BindingFlags.Instance);
            lastNonZeroField = typeof(PlayerController).GetField("m_lastNonzeroCommandedDirection", BindingFlags.NonPublic | BindingFlags.Instance);           
            lockedDodgeRollDirection = typeof(PlayerController).GetField("lockedDodgeRollDirection", BindingFlags.NonPublic | BindingFlags.Instance);
            m_currentGunAngle = typeof(PlayerController).GetField("m_currentGunAngle", BindingFlags.NonPublic | BindingFlags.Instance);

            if (hatOwner != null)
            {
                SpriteOutlineManager.AddOutlineToSprite(hatSprite, Color.black, 1);
                GameObject playerSprite = hatOwner.transform.Find("PlayerSprite").gameObject;
                hatOwnerAnimator = playerSprite.GetComponent<tk2dSpriteAnimator>();
                hatOwner.OnPreDodgeRoll += this.HatReactToDodgeRoll;
                UpdateHatFacingDirection(FetchOwnerFacingDirection());
            }
            else Debug.LogError("hatOwner was somehow null in hat Start() ???");
        }

		public override void OnDestroy()
        {
            if (hatOwner)
            {
                hatOwner.OnPreDodgeRoll -= this.HatReactToDodgeRoll;
            }
            base.OnDestroy();
        }

        private void HatReactToDodgeRoll(PlayerController player)
        {
            
        }

        private void Update()
        {
            if (hatOwner)
            {
                if (currentState == HatState.SITTING)
                    StickHatToPlayer(hatOwner);
                //Make the Hat vanish upon pitfall, or when the player rolls if the hat is VANISH type
                HandleVanish();
                //if(ETGModGUI.CurrentMenu != ETGModGUI.MenuOpened.Console)
                    //ETGModConsole.Log(hatOwnerAnimator.CurrentClip.name);
                //UPDATE DIRECTIONS
                HatDirection checkedDir = FetchOwnerFacingDirection();
                if (checkedDir != currentDirection) UpdateHatFacingDirection(checkedDir);

                HandleAttachedSpriteDepth(m_currentGunAngle.GetTypedValue<float>(hatOwner));
                HandleFlip();
            }
        }

        private static Vector2 GetDefOffset(tk2dSpriteDefinition def)
        {
            Bounds b = new Bounds(
              new Vector3(
                def.boundsDataCenter.x,
                def.boundsDataCenter.y,
                def.boundsDataCenter.z),
              new Vector3(
                def.boundsDataExtents.x,
                def.boundsDataExtents.y,
                def.boundsDataExtents.z));
            return new Vector2(0f, b.min.y + b.extents.y * 2f);
        }

        /// <summary>Preemptively move hat immediately after the player's sprite changes to avoid a 1-frame delay on hat offsets</summary>
        [HarmonyPatch(typeof(tk2dSpriteAnimator), nameof(tk2dSpriteAnimator.SetFrameInternal))]
        private class UpdateHatAnimationPatch
        {
            static void Prefix(tk2dSpriteAnimator __instance, int currFrame)
            {
                if (currFrame != 0 && __instance.previousFrame == currFrame)
                    return; // nothing to do in the prefix if our frame hasn't changed or just been reset
                if (__instance.transform.parent is not Transform parent)
                    return; // unparented, not what we're looking for
                if (!parent.gameObject || parent.GetComponent<HatController>() is not HatController hatController)
                    return; // no player, nothing special needed
                if (__instance.GetComponent<Hat>())
                    return; // we are the sprite animator for the Hat itself, don't do anything
                if (hatController.CurrentHat is not Hat hat)
                    return; // no hat, nothing to do

                tk2dSpriteAnimationFrame frame = __instance.currentClip.frames[currFrame];
                tk2dSpriteDefinition def = frame.spriteCollection.spriteDefinitions[frame.spriteId];
                if (def == hat.cachedDef)
                    return; // sprite hasn't changed, so nothing to do

                hat.cachedDef = def;
                if (hat.hatOwner && hat.currentState == HatState.SITTING)
                    hat.transform.position = hat.GetHatPosition(hat.hatOwner); // update the hat position in light of the new sprite definition
            }
        }

		private void HandleVanish()
        {
            bool Visible = base.sprite.renderer.enabled;
            bool shouldBeVanished = false;

            if (hatOwner.IsFalling) 
                shouldBeVanished = true;

            if(hatOwnerAnimator.CurrentClip.name == "doorway" || hatOwnerAnimator.CurrentClip.name == "spinfall")
                shouldBeVanished = true;

            if ((PlayerHasAdditionalVanishOverride() || hatRollReaction == HatRollReaction.VANISH) && hatOwner.IsDodgeRolling) 
                shouldBeVanished = true;

			if (hatOwner.IsSlidingOverSurface) 
            {
               shouldBeVanished = true;
               StickHatToPlayer(hatOwner);
            }
                
            if (!Visible && !shouldBeVanished)
            {
                base.sprite.renderer.enabled = true;
                SpriteOutlineManager.AddOutlineToSprite(hatSprite, Color.black, 1);
                StickHatToPlayer(hatOwner);
                if (GameManager.AUDIO_ENABLED && !string.IsNullOrEmpty(FlipEndedSound))
                {
                    AkSoundEngine.PostEvent(FlipEndedSound, gameObject);
                }
            }
            else if (Visible && shouldBeVanished)
            {
                base.sprite.renderer.enabled = false;
                SpriteOutlineManager.RemoveOutlineFromSprite(hatSprite);
                StickHatToPlayer(hatOwner);
                if (GameManager.AUDIO_ENABLED && !string.IsNullOrEmpty(FlipStartedSound))
                {
                    AkSoundEngine.PostEvent(FlipStartedSound, gameObject);
                }
            }
        }

        private bool PlayerHasAdditionalVanishOverride()
        {
            bool shouldActuallyVanish = false;
            if (hatOwner && hatOwner.HasPickupID(436)) shouldActuallyVanish = true;
            return shouldActuallyVanish;
        }

		public void UpdateHatFacingDirection(HatDirection targetDir)
        {
            string animToPlay = null;

            // adjust the direction based on what our hat actually supports
            HatDirection adjustedDir = targetDir;
            if (hatDirectionality == HatDirectionality.NONE)
                adjustedDir = HatDirection.SOUTH;
            else if (hatDirectionality == HatDirectionality.TWOWAYHORIZONTAL)
                adjustedDir = hatOwner.sprite.FlipX ? HatDirection.WEST : HatDirection.EAST;
            else if (hatDirectionality == HatDirectionality.TWOWAYVERTICAL)
            {
                if (targetDir == HatDirection.NORTHWEST || targetDir == HatDirection.NORTHEAST || targetDir == HatDirection.NORTH)
                    adjustedDir = HatDirection.NORTH;
                else
                    adjustedDir = HatDirection.SOUTH;
            }
            else if (hatDirectionality == HatDirectionality.FOURWAY)
            {
                if (targetDir == HatDirection.NORTHWEST)
                    adjustedDir = HatDirection.WEST;
                else if (targetDir == HatDirection.NORTHEAST)
                    adjustedDir = HatDirection.EAST;
            }

            // pick the appropriate animation
            switch (adjustedDir)
            {
                case HatDirection.SOUTH:     { animToPlay = "hat_south"; }     break;
                case HatDirection.NORTH:     { animToPlay = "hat_north"; }     break;
                case HatDirection.WEST:      { animToPlay = "hat_west"; }      break;
                case HatDirection.EAST:      { animToPlay = "hat_east"; }      break;
                case HatDirection.NORTHWEST: { animToPlay = "hat_northwest"; } break;
                case HatDirection.NORTHEAST: { animToPlay = "hat_northeast"; } break;
                case HatDirection.NONE:
                    ETGModConsole.Log("ERROR: TRIED TO ROTATE HAT TO A NULL DIRECTION! (wtf?)");
                    break;
            }

            // cache the actual targetDir rather than adjustedDir so we don't call this every frame unnecessarily
            currentDirection = targetDir;

            // play the animation if non-null
            if (animToPlay != null)
                hatSpriteAnimator.Play(animToPlay);
        }

        public HatDirection FetchOwnerFacingDirection()
        {
            if (hatOwner == null || hatOwner.sprite == null)
                return HatDirection.EAST; // return a sane default if we're ownerless

            // figure out an approximate direction from the player's animation name
            string animName = hatOwner.sprite.GetCurrentSpriteDef().name;
            if (animName.Contains("north_"))       return HatDirection.NORTH;
            if (animName.Contains("back_"))        return HatDirection.NORTH;
            if (animName.Contains("south_"))       return HatDirection.SOUTH;
            if (animName.Contains("front_"))       return HatDirection.SOUTH;
            if (animName.Contains("back_right_"))  return hatOwner.sprite.FlipX ? HatDirection.NORTHWEST : HatDirection.NORTHEAST;
            if (animName.Contains("backwards_"))   return hatOwner.sprite.FlipX ? HatDirection.NORTHWEST : HatDirection.NORTHEAST;
            if (animName.Contains("backward_"))    return hatOwner.sprite.FlipX ? HatDirection.NORTHWEST : HatDirection.NORTHEAST;
            if (animName.Contains("bw_"))          return hatOwner.sprite.FlipX ? HatDirection.NORTHWEST : HatDirection.NORTHEAST;
            // if (animName.Contains("front_right_")) return HatDirection.EAST;
            // if (animName.Contains("right_front_")) return HatDirection.EAST;
            // if (animName.Contains("forward_"))     return HatDirection.EAST;

            return hatOwner.sprite.FlipX ? HatDirection.WEST : HatDirection.EAST; // return a sane default if we're ownerless
        }

        public int GetPlayerAnimFrame(PlayerController player)
		{
            return player.spriteAnimator.CurrentFrame; // return a sane default

        }

        private class FrameOffset
        {
            public Vector2 offset;
            public Vector2 flipOffset;
            public FrameOffset(Vector2 offset, Vector2? flipOffset = null)
            {
                this.offset     = 0.0625f * offset; // convert from pixels to tile size
                this.flipOffset = 0.0625f * (flipOffset ?? offset);
            }
        }

        private static readonly Dictionary<string, FrameOffset> HeadFrameOffsets = new(){
            {"convict_idle_002",                     new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_idle_front_002",               new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_idle_back_002",                new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_idle_bw_002",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_idle_003",                     new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_idle_front_003",               new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_idle_back_003",                new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_idle_bw_003",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_run_forward_001",              new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_run_forward_002",              new FrameOffset(offset: new Vector2( 0,  2))},
            {"convict_run_forward_004",              new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_run_forward_005",              new FrameOffset(offset: new Vector2( 0,  2))},
            {"convict_run_backwards_001",            new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_run_backwards_002",            new FrameOffset(offset: new Vector2( 0,  2))},
            {"convict_run_backwards_004",            new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_run_backwards_005",            new FrameOffset(offset: new Vector2( 0,  2))},
            {"convict_run_north_001",                new FrameOffset(offset: new Vector2( 0,  0))},
            {"convict_run_north_002",                new FrameOffset(offset: new Vector2( 0,  3))},
            {"convict_run_north_003",                new FrameOffset(offset: new Vector2( 0,  2))},
            {"convict_run_north_004",                new FrameOffset(offset: new Vector2( 0,  0))},
            {"convict_run_north_005",                new FrameOffset(offset: new Vector2( 0,  3))},
            {"convict_run_north_006",                new FrameOffset(offset: new Vector2( 0,  2))},
            {"convict_run_south_001",                new FrameOffset(offset: new Vector2( 0,  0))},
            {"convict_run_south_002",                new FrameOffset(offset: new Vector2( 0,  4))},
            {"convict_run_south_003",                new FrameOffset(offset: new Vector2( 0,  2))},
            {"convict_run_south_004",                new FrameOffset(offset: new Vector2( 0,  0))},
            {"convict_run_south_005",                new FrameOffset(offset: new Vector2( 0,  4))},
            {"convict_run_south_006",                new FrameOffset(offset: new Vector2( 0,  2))},
            {"guide_idle_right_front_002",           new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_idle_right_front_003",           new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_idle_front_002",                 new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_idle_front_003",                 new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_idle_back_002",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_idle_back_003",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_idle_back_right_002",            new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_idle_back_right_003",            new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_run_right_front_001",            new FrameOffset(offset: new Vector2( 0,  2))},
            {"guide_run_right_front_002",            new FrameOffset(offset: new Vector2( 0,  1))},
            {"guide_run_right_front_003",            new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_run_right_front_004",            new FrameOffset(offset: new Vector2( 0,  2))},
            {"guide_run_right_front_006",            new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_run_back_right_001",             new FrameOffset(offset: new Vector2( 0,  2))},
            {"guide_run_back_right_002",             new FrameOffset(offset: new Vector2( 0,  1))},
            {"guide_run_back_right_003",             new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_run_back_right_004",             new FrameOffset(offset: new Vector2( 0,  2))},
            {"guide_run_back_right_006",             new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_run_front_001",                  new FrameOffset(offset: new Vector2( 0,  2))},
            {"guide_run_front_003",                  new FrameOffset(offset: new Vector2( 0, -2))},
            {"guide_run_front_004",                  new FrameOffset(offset: new Vector2( 0,  2))},
            {"guide_run_front_006",                  new FrameOffset(offset: new Vector2( 0, -2))},
            {"guide_run_back_001",                   new FrameOffset(offset: new Vector2( 0,  2))},
            {"guide_run_back_003",                   new FrameOffset(offset: new Vector2( 0, -2))},
            {"guide_run_back_004",                   new FrameOffset(offset: new Vector2( 0,  2))},
            {"guide_run_back_006",                   new FrameOffset(offset: new Vector2( 0, -2))},
            {"marine_idle_front_right_002",          new FrameOffset(offset: new Vector2( 0, -1))},
            {"marine_idle_front_right_003",          new FrameOffset(offset: new Vector2( 0, -2))},
            {"marine_idle_front_right_004",          new FrameOffset(offset: new Vector2( 0, -1))},
            {"marine_idle_back_right_002",           new FrameOffset(offset: new Vector2( 0, -1))},
            {"marine_idle_back_right_003",           new FrameOffset(offset: new Vector2( 0, -2))},
            {"marine_idle_back_right_004",           new FrameOffset(offset: new Vector2( 0, -1))},
            {"marine_idle_back_002",                 new FrameOffset(offset: new Vector2( 0, -1))},
            {"marine_idle_back_003",                 new FrameOffset(offset: new Vector2( 0, -3))},
            {"marine_idle_back_004",                 new FrameOffset(offset: new Vector2( 0, -2))},
            {"marine_idle_front_002",                new FrameOffset(offset: new Vector2( 0, -1))},
            {"marine_idle_front_003",                new FrameOffset(offset: new Vector2( 0, -3))},
            {"marine_idle_front_004",                new FrameOffset(offset: new Vector2( 0, -2))},
            {"marine_run_front_right_001",           new FrameOffset(offset: new Vector2( 0,  2))},
            {"marine_run_front_right_002",           new FrameOffset(offset: new Vector2( 0,  1))},
            {"marine_run_front_right_003",           new FrameOffset(offset: new Vector2( 0, -2))},
            {"marine_run_front_right_004",           new FrameOffset(offset: new Vector2( 0,  2))},
            {"marine_run_front_right_005",           new FrameOffset(offset: new Vector2( 0,  1))},
            {"marine_run_front_right_006",           new FrameOffset(offset: new Vector2( 0, -2))},
            {"marine_run_back_right_001",            new FrameOffset(offset: new Vector2( 0,  2))},
            {"marine_run_back_right_002",            new FrameOffset(offset: new Vector2( 0,  1))},
            {"marine_run_back_right_003",            new FrameOffset(offset: new Vector2( 0, -2))},
            {"marine_run_back_right_004",            new FrameOffset(offset: new Vector2( 0,  2))},
            {"marine_run_back_right_005",            new FrameOffset(offset: new Vector2( 0,  1))},
            {"marine_run_back_right_006",            new FrameOffset(offset: new Vector2( 0, -2))},
            {"marine_run_front_001",                 new FrameOffset(offset: new Vector2( 0,  1))},
            {"marine_run_front_002",                 new FrameOffset(offset: new Vector2( 0,  0))},
            {"marine_run_front_003",                 new FrameOffset(offset: new Vector2( 0, -2))},
            {"marine_run_front_004",                 new FrameOffset(offset: new Vector2( 0,  1))},
            {"marine_run_front_005",                 new FrameOffset(offset: new Vector2( 0,  0))},
            {"marine_run_front_006",                 new FrameOffset(offset: new Vector2( 0, -2))},
            {"marine_run_back_001",                  new FrameOffset(offset: new Vector2( 0,  2))},
            {"marine_run_back_002",                  new FrameOffset(offset: new Vector2( 0,  1))},
            {"marine_run_back_003",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"marine_run_back_004",                  new FrameOffset(offset: new Vector2( 0,  2))},
            {"marine_run_back_005",                  new FrameOffset(offset: new Vector2( 0,  1))},
            {"marine_run_back_006",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"rogue_idle_001",                       new FrameOffset(offset: new Vector2( 1,  0), flipOffset: new Vector2(-1,  0))},
            {"rogue_idle_002",                       new FrameOffset(offset: new Vector2( 1, -1), flipOffset: new Vector2(-1, -1))},
            {"rogue_idle_003",                       new FrameOffset(offset: new Vector2( 1, -1), flipOffset: new Vector2(-1, -1))},
            {"rogue_idle_004",                       new FrameOffset(offset: new Vector2( 1,  0), flipOffset: new Vector2(-1,  0))},
            {"rogue_idle_backwards_001",             new FrameOffset(offset: new Vector2( 1,  0), flipOffset: new Vector2(-1,  0))},
            {"rogue_idle_backwards_002",             new FrameOffset(offset: new Vector2( 1, -1), flipOffset: new Vector2(-1, -1))},
            {"rogue_idle_backwards_003",             new FrameOffset(offset: new Vector2( 1, -1), flipOffset: new Vector2(-1, -1))},
            {"rogue_idle_backwards_004",             new FrameOffset(offset: new Vector2( 1,  0), flipOffset: new Vector2(-1,  0))},
            {"rogue_idle_back_002",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"rogue_idle_back_003",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"rogue_idle_front_002",                 new FrameOffset(offset: new Vector2( 0, -2))},
            {"rogue_idle_front_003",                 new FrameOffset(offset: new Vector2( 0, -2))},
            {"rogue_idle_front_004",                 new FrameOffset(offset: new Vector2( 0, -1))},
            {"rogue_run_forward_001",                new FrameOffset(offset: new Vector2( 0, -1), flipOffset: new Vector2( 0, -1))},
            {"rogue_run_forward_002",                new FrameOffset(offset: new Vector2(-1,  1), flipOffset: new Vector2( 1,  1))},
            {"rogue_run_forward_003",                new FrameOffset(offset: new Vector2(-1,  0), flipOffset: new Vector2( 1,  0))},
            {"rogue_run_forward_004",                new FrameOffset(offset: new Vector2( 0, -1), flipOffset: new Vector2( 0, -1))},
            {"rogue_run_forward_005",                new FrameOffset(offset: new Vector2( 1,  2), flipOffset: new Vector2(-1,  2))},
            {"rogue_run_forward_006",                new FrameOffset(offset: new Vector2( 1,  0), flipOffset: new Vector2(-1,  0))},
            {"rogue_run_backward_001",               new FrameOffset(offset: new Vector2( 0, -1))},
            {"rogue_run_backward_002",               new FrameOffset(offset: new Vector2( 0,  2))},
            {"rogue_run_backward_003",               new FrameOffset(offset: new Vector2( 0,  0))},
            {"rogue_run_backward_004",               new FrameOffset(offset: new Vector2( 0, -1))},
            {"rogue_run_backward_005",               new FrameOffset(offset: new Vector2( 0,  3))},
            {"rogue_run_backward_006",               new FrameOffset(offset: new Vector2( 0,  0))},
            {"rogue_run_front_001",                  new FrameOffset(offset: new Vector2( 0,  3))},
            {"rogue_run_front_002",                  new FrameOffset(offset: new Vector2( 0,  2))},
            {"rogue_run_front_003",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"rogue_run_front_004",                  new FrameOffset(offset: new Vector2( 0,  2))},
            {"rogue_run_front_005",                  new FrameOffset(offset: new Vector2( 0,  1))},
            {"rogue_run_front_006",                  new FrameOffset(offset: new Vector2( 0,  0))},
            {"rogue_run_back_001",                   new FrameOffset(offset: new Vector2( 0,  3))},
            {"rogue_run_back_002",                   new FrameOffset(offset: new Vector2( 0,  2))},
            {"rogue_run_back_003",                   new FrameOffset(offset: new Vector2( 0, -1))},
            {"rogue_run_back_004",                   new FrameOffset(offset: new Vector2( 0,  2))},
            {"rogue_run_back_005",                   new FrameOffset(offset: new Vector2( 0,  1))},
            {"rogue_run_back_006",                   new FrameOffset(offset: new Vector2( 0,  0))},
            {"robot_idle_001",                       new FrameOffset(offset: new Vector2( 1,  0), flipOffset: new Vector2(-1,  0))},
            {"robot_idle_002",                       new FrameOffset(offset: new Vector2( 1,  0), flipOffset: new Vector2(-1,  0))},
            {"robot_idle_003",                       new FrameOffset(offset: new Vector2( 1,  0), flipOffset: new Vector2(-1,  0))},
            {"robot_idle_004",                       new FrameOffset(offset: new Vector2( 1,  0), flipOffset: new Vector2(-1,  0))},
            {"robot_idle_front_001",                 new FrameOffset(offset: new Vector2( 0, -2))},
            {"robot_idle_front_002",                 new FrameOffset(offset: new Vector2( 0,  0))},
            {"robot_idle_front_003",                 new FrameOffset(offset: new Vector2( 0, -2))},
            {"robot_idle_front_004",                 new FrameOffset(offset: new Vector2( 0, -3))},
            {"robot_run_front_001",                  new FrameOffset(offset: new Vector2( 0,  0))},
            {"robot_run_front_002",                  new FrameOffset(offset: new Vector2( 0, -2))},
            {"robot_run_front_003",                  new FrameOffset(offset: new Vector2( 0, -2))},
            {"robot_run_front_004",                  new FrameOffset(offset: new Vector2( 0,  0))},
            {"robot_run_front_005",                  new FrameOffset(offset: new Vector2( 0, -2))},
            {"robot_run_front_006",                  new FrameOffset(offset: new Vector2( 0, -2))},
            {"cultist_idle_front_right_003",         new FrameOffset(offset: new Vector2( 0, -1))},
            {"cultist_idle_front_right_004",         new FrameOffset(offset: new Vector2( 0, -1))},
            {"cultist_idle_back_right_003",          new FrameOffset(offset: new Vector2( 0, -1))},
            {"cultist_idle_back_right_004",          new FrameOffset(offset: new Vector2( 0, -1))},
            {"cultist_idle_front_003",               new FrameOffset(offset: new Vector2( 0, -1))},
            {"cultist_idle_front_004",               new FrameOffset(offset: new Vector2( 0, -1))},
            {"cultist_idle_back_003",                new FrameOffset(offset: new Vector2( 0, -1))},
            {"cultist_idle_back_004",                new FrameOffset(offset: new Vector2( 0, -1))},
            {"cultist_move_front_right_001",         new FrameOffset(offset: new Vector2( 0,  2))},
            {"cultist_move_front_right_002",         new FrameOffset(offset: new Vector2( 0,  1))},
            {"cultist_move_front_right_003",         new FrameOffset(offset: new Vector2( 0,  0))},
            {"cultist_move_front_right_004",         new FrameOffset(offset: new Vector2( 0,  1))},
            {"cultist_move_front_right_005",         new FrameOffset(offset: new Vector2( 0,  0))},
            {"cultist_move_front_right_006",         new FrameOffset(offset: new Vector2( 0,  0))},
            {"cultist_move_back_right_001",          new FrameOffset(offset: new Vector2( 0,  2))},
            {"cultist_move_back_right_002",          new FrameOffset(offset: new Vector2( 0,  1))},
            {"cultist_move_back_right_003",          new FrameOffset(offset: new Vector2( 0,  0))},
            {"cultist_move_back_right_004",          new FrameOffset(offset: new Vector2( 0,  1))},
            {"cultist_move_back_right_005",          new FrameOffset(offset: new Vector2( 0,  0))},
            {"cultist_move_back_right_006",          new FrameOffset(offset: new Vector2( 0,  0))},
            {"cultist_move_front_001",               new FrameOffset(offset: new Vector2( 0,  2))},
            {"cultist_move_front_002",               new FrameOffset(offset: new Vector2( 0,  0))},
            {"cultist_move_front_003",               new FrameOffset(offset: new Vector2( 0, -3))},
            {"cultist_move_front_004",               new FrameOffset(offset: new Vector2( 0,  1))},
            {"cultist_move_front_005",               new FrameOffset(offset: new Vector2( 0, -1))},
            {"cultist_move_front_006",               new FrameOffset(offset: new Vector2( 0, -2))},
            {"cultist_move_back_001",                new FrameOffset(offset: new Vector2( 0,  2))},
            {"cultist_move_back_002",                new FrameOffset(offset: new Vector2( 0,  0))},
            {"cultist_move_back_003",                new FrameOffset(offset: new Vector2( 0, -3))},
            {"cultist_move_back_004",                new FrameOffset(offset: new Vector2( 0,  1))},
            {"cultist_move_back_005",                new FrameOffset(offset: new Vector2( 0, -1))},
            {"cultist_move_back_006",                new FrameOffset(offset: new Vector2( 0, -2))},
            {"bullet_player_move_front_right_001",   new FrameOffset(offset: new Vector2( 0,  1), flipOffset: new Vector2( 0,  1))},
            {"bullet_player_move_front_right_002",   new FrameOffset(offset: new Vector2( 4,  1), flipOffset: new Vector2(-4,  1))},
            {"bullet_player_move_front_right_003",   new FrameOffset(offset: new Vector2( 4,  0), flipOffset: new Vector2(-4,  0))},
            {"bullet_player_move_front_right_004",   new FrameOffset(offset: new Vector2( 0,  1), flipOffset: new Vector2( 0,  1))},
            {"bullet_player_move_front_right_005",   new FrameOffset(offset: new Vector2(-2,  1), flipOffset: new Vector2( 2,  1))},
            {"bullet_player_move_front_right_006",   new FrameOffset(offset: new Vector2(-2,  1), flipOffset: new Vector2( 2,  1))},
            {"bullet_player_move_front_001",         new FrameOffset(offset: new Vector2( 0,  1), flipOffset: new Vector2( 0,  1))},
            {"bullet_player_move_front_002",         new FrameOffset(offset: new Vector2( 1,  1), flipOffset: new Vector2(-1,  1))},
            {"bullet_player_move_front_003",         new FrameOffset(offset: new Vector2( 1,  1), flipOffset: new Vector2(-1,  1))},
            {"bullet_player_move_front_004",         new FrameOffset(offset: new Vector2( 0,  1), flipOffset: new Vector2( 0,  1))},
            {"bullet_player_move_front_005",         new FrameOffset(offset: new Vector2(-2,  1), flipOffset: new Vector2( 2,  1))},
            {"bullet_player_move_front_006",         new FrameOffset(offset: new Vector2(-1,  1), flipOffset: new Vector2( 1,  1))},
            {"bullet_player_move_back_right_001",    new FrameOffset(offset: new Vector2( 0,  1), flipOffset: new Vector2( 0,  1))},
            {"bullet_player_move_back_right_002",    new FrameOffset(offset: new Vector2( 4,  1), flipOffset: new Vector2(-4,  1))},
            {"bullet_player_move_back_right_003",    new FrameOffset(offset: new Vector2( 4,  0), flipOffset: new Vector2(-4,  0))},
            {"bullet_player_move_back_right_004",    new FrameOffset(offset: new Vector2( 0,  1), flipOffset: new Vector2( 0,  1))},
            {"bullet_player_move_back_right_005",    new FrameOffset(offset: new Vector2(-2,  1), flipOffset: new Vector2( 2,  1))},
            {"bullet_player_move_back_right_006",    new FrameOffset(offset: new Vector2(-2,  1), flipOffset: new Vector2( 2,  1))},
            {"bullet_player_move_back_001",          new FrameOffset(offset: new Vector2( 0,  1), flipOffset: new Vector2( 0,  1))},
            {"bullet_player_move_back_002",          new FrameOffset(offset: new Vector2( 1,  1), flipOffset: new Vector2(-1,  1))},
            {"bullet_player_move_back_003",          new FrameOffset(offset: new Vector2( 1,  1), flipOffset: new Vector2(-1,  1))},
            {"bullet_player_move_back_004",          new FrameOffset(offset: new Vector2( 0,  1), flipOffset: new Vector2( 0,  1))},
            {"bullet_player_move_back_005",          new FrameOffset(offset: new Vector2(-2,  1), flipOffset: new Vector2( 2,  1))},
            {"bullet_player_move_back_006",          new FrameOffset(offset: new Vector2(-1,  1), flipOffset: new Vector2( 1,  1))},
        };

        private static readonly Dictionary<string, FrameOffset> EyeFrameOffsets = new(){
            {"convict_idle_002",                     new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_idle_front_002",               new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_idle_back_002",                new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_idle_bw_002",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_idle_003",                     new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_idle_front_003",               new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_idle_back_003",                new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_idle_bw_003",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_run_forward_001",              new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_run_forward_002",              new FrameOffset(offset: new Vector2( 0,  2))},
            {"convict_run_forward_003",              new FrameOffset(offset: new Vector2( 0,  0))},
            {"convict_run_forward_004",              new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_run_forward_005",              new FrameOffset(offset: new Vector2( 0,  3))},
            {"convict_run_forward_006",              new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_run_backwards_001",            new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_run_backwards_002",            new FrameOffset(offset: new Vector2( 0,  2))},
            {"convict_run_backwards_003",            new FrameOffset(offset: new Vector2( 0,  0))},
            {"convict_run_backwards_004",            new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_run_backwards_005",            new FrameOffset(offset: new Vector2( 0,  3))},
            {"convict_run_backwards_006",            new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_run_north_001",                new FrameOffset(offset: new Vector2( 0,  3))},
            {"convict_run_north_002",                new FrameOffset(offset: new Vector2( 0,  1))},
            {"convict_run_north_003",                new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_run_north_004",                new FrameOffset(offset: new Vector2( 0,  4))},
            {"convict_run_north_005",                new FrameOffset(offset: new Vector2( 0,  1))},
            {"convict_run_north_006",                new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_run_south_001",                new FrameOffset(offset: new Vector2( 0,  3))},
            {"convict_run_south_002",                new FrameOffset(offset: new Vector2( 0,  1))},
            {"convict_run_south_003",                new FrameOffset(offset: new Vector2( 0, -1))},
            {"convict_run_south_004",                new FrameOffset(offset: new Vector2( 0,  4))},
            {"convict_run_south_005",                new FrameOffset(offset: new Vector2( 0,  1))},
            {"convict_run_south_006",                new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_idle_right_front_002",           new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_idle_right_front_003",           new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_idle_front_002",                 new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_idle_front_003",                 new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_idle_back_002",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_idle_back_003",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_idle_back_right_002",            new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_idle_back_right_003",            new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_run_right_front_001",            new FrameOffset(offset: new Vector2( 0,  2))},
            {"guide_run_right_front_002",            new FrameOffset(offset: new Vector2( 0,  1))},
            {"guide_run_right_front_003",            new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_run_right_front_004",            new FrameOffset(offset: new Vector2( 0,  2))},
            {"guide_run_right_front_006",            new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_run_back_right_001",             new FrameOffset(offset: new Vector2( 0,  2))},
            {"guide_run_back_right_002",             new FrameOffset(offset: new Vector2( 0,  1))},
            {"guide_run_back_right_003",             new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_run_back_right_004",             new FrameOffset(offset: new Vector2( 0,  2))},
            {"guide_run_back_right_006",             new FrameOffset(offset: new Vector2( 0, -1))},
            {"guide_run_front_001",                  new FrameOffset(offset: new Vector2( 0,  2))},
            {"guide_run_front_003",                  new FrameOffset(offset: new Vector2( 0, -2))},
            {"guide_run_front_004",                  new FrameOffset(offset: new Vector2( 0,  2))},
            {"guide_run_front_006",                  new FrameOffset(offset: new Vector2( 0, -2))},
            {"guide_run_back_001",                   new FrameOffset(offset: new Vector2( 0,  2))},
            {"guide_run_back_003",                   new FrameOffset(offset: new Vector2( 0, -2))},
            {"guide_run_back_004",                   new FrameOffset(offset: new Vector2( 0,  2))},
            {"guide_run_back_006",                   new FrameOffset(offset: new Vector2( 0, -2))},
            {"marine_idle_front_right_002",          new FrameOffset(offset: new Vector2( 0, -1))},
            {"marine_idle_front_right_003",          new FrameOffset(offset: new Vector2( 0, -2))},
            {"marine_idle_front_right_004",          new FrameOffset(offset: new Vector2( 0, -1))},
            {"marine_idle_back_right_002",           new FrameOffset(offset: new Vector2( 0, -1))},
            {"marine_idle_back_right_003",           new FrameOffset(offset: new Vector2( 0, -2))},
            {"marine_idle_back_right_004",           new FrameOffset(offset: new Vector2( 0, -1))},
            {"marine_idle_back_002",                 new FrameOffset(offset: new Vector2( 0, -1))},
            {"marine_idle_back_003",                 new FrameOffset(offset: new Vector2( 0, -3))},
            {"marine_idle_back_004",                 new FrameOffset(offset: new Vector2( 0, -2))},
            {"marine_idle_front_002",                new FrameOffset(offset: new Vector2( 0, -1))},
            {"marine_idle_front_003",                new FrameOffset(offset: new Vector2( 0, -3))},
            {"marine_idle_front_004",                new FrameOffset(offset: new Vector2( 0, -2))},
            {"marine_run_front_right_001",           new FrameOffset(offset: new Vector2( 0,  2))},
            {"marine_run_front_right_002",           new FrameOffset(offset: new Vector2( 0,  1))},
            {"marine_run_front_right_003",           new FrameOffset(offset: new Vector2( 0, -2))},
            {"marine_run_front_right_004",           new FrameOffset(offset: new Vector2( 0,  2))},
            {"marine_run_front_right_005",           new FrameOffset(offset: new Vector2( 0,  1))},
            {"marine_run_front_right_006",           new FrameOffset(offset: new Vector2( 0, -2))},
            {"marine_run_back_right_001",            new FrameOffset(offset: new Vector2( 0,  2))},
            {"marine_run_back_right_002",            new FrameOffset(offset: new Vector2( 0,  1))},
            {"marine_run_back_right_003",            new FrameOffset(offset: new Vector2( 0, -2))},
            {"marine_run_back_right_004",            new FrameOffset(offset: new Vector2( 0,  2))},
            {"marine_run_back_right_005",            new FrameOffset(offset: new Vector2( 0,  1))},
            {"marine_run_back_right_006",            new FrameOffset(offset: new Vector2( 0, -2))},
            {"marine_run_front_001",                 new FrameOffset(offset: new Vector2( 0,  2))},
            {"marine_run_front_002",                 new FrameOffset(offset: new Vector2( 0,  1))},
            {"marine_run_front_003",                 new FrameOffset(offset: new Vector2( 0, -1))},
            {"marine_run_front_004",                 new FrameOffset(offset: new Vector2( 0,  2))},
            {"marine_run_front_005",                 new FrameOffset(offset: new Vector2( 0,  1))},
            {"marine_run_front_006",                 new FrameOffset(offset: new Vector2( 0, -1))},
            {"marine_run_back_001",                  new FrameOffset(offset: new Vector2( 0,  2))},
            {"marine_run_back_002",                  new FrameOffset(offset: new Vector2( 0,  1))},
            {"marine_run_back_003",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"marine_run_back_004",                  new FrameOffset(offset: new Vector2( 0,  2))},
            {"marine_run_back_005",                  new FrameOffset(offset: new Vector2( 0,  1))},
            {"marine_run_back_006",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"rogue_idle_001",                       new FrameOffset(offset: new Vector2( 1,  0), flipOffset: new Vector2(-1,  0))},
            {"rogue_idle_002",                       new FrameOffset(offset: new Vector2( 1, -1), flipOffset: new Vector2(-1, -1))},
            {"rogue_idle_003",                       new FrameOffset(offset: new Vector2( 1, -1), flipOffset: new Vector2(-1, -1))},
            {"rogue_idle_004",                       new FrameOffset(offset: new Vector2( 1,  0), flipOffset: new Vector2(-1,  0))},
            {"rogue_idle_backwards_001",             new FrameOffset(offset: new Vector2( 1,  0), flipOffset: new Vector2(-1,  0))},
            {"rogue_idle_backwards_002",             new FrameOffset(offset: new Vector2( 1, -1), flipOffset: new Vector2(-1, -1))},
            {"rogue_idle_backwards_003",             new FrameOffset(offset: new Vector2( 1, -1), flipOffset: new Vector2(-1, -1))},
            {"rogue_idle_backwards_004",             new FrameOffset(offset: new Vector2( 1,  0), flipOffset: new Vector2(-1,  0))},
            {"rogue_idle_back_002",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"rogue_idle_back_003",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"rogue_idle_front_002",                 new FrameOffset(offset: new Vector2( 0, -2))},
            {"rogue_idle_front_003",                 new FrameOffset(offset: new Vector2( 0, -2))},
            {"rogue_idle_front_004",                 new FrameOffset(offset: new Vector2( 0, -1))},
            {"rogue_run_forward_001",                new FrameOffset(offset: new Vector2( 0, -1), flipOffset: new Vector2( 0, -1))},
            {"rogue_run_forward_002",                new FrameOffset(offset: new Vector2(-1,  2), flipOffset: new Vector2( 1,  2))},
            {"rogue_run_forward_003",                new FrameOffset(offset: new Vector2(-1,  0), flipOffset: new Vector2( 1,  0))},
            {"rogue_run_forward_004",                new FrameOffset(offset: new Vector2( 0, -1), flipOffset: new Vector2( 0, -1))},
            {"rogue_run_forward_005",                new FrameOffset(offset: new Vector2( 1,  3), flipOffset: new Vector2(-1,  3))},
            {"rogue_run_forward_006",                new FrameOffset(offset: new Vector2( 1,  0), flipOffset: new Vector2(-1,  0))},
            {"rogue_run_backward_001",               new FrameOffset(offset: new Vector2( 0, -1))},
            {"rogue_run_backward_002",               new FrameOffset(offset: new Vector2( 0,  2))},
            {"rogue_run_backward_003",               new FrameOffset(offset: new Vector2( 0,  0))},
            {"rogue_run_backward_004",               new FrameOffset(offset: new Vector2( 0, -1))},
            {"rogue_run_backward_005",               new FrameOffset(offset: new Vector2( 0,  3))},
            {"rogue_run_backward_006",               new FrameOffset(offset: new Vector2( 0,  0))},
            {"rogue_run_front_001",                  new FrameOffset(offset: new Vector2( 0,  2))},
            {"rogue_run_front_002",                  new FrameOffset(offset: new Vector2( 0,  1))},
            {"rogue_run_front_003",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"rogue_run_front_004",                  new FrameOffset(offset: new Vector2( 0,  3))},
            {"rogue_run_front_005",                  new FrameOffset(offset: new Vector2( 0,  2))},
            {"rogue_run_front_006",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"rogue_run_back_001",                   new FrameOffset(offset: new Vector2( 0,  3))},
            {"rogue_run_back_002",                   new FrameOffset(offset: new Vector2( 0,  2))},
            {"rogue_run_back_003",                   new FrameOffset(offset: new Vector2( 0, -1))},
            {"rogue_run_back_004",                   new FrameOffset(offset: new Vector2( 0,  2))},
            {"rogue_run_back_005",                   new FrameOffset(offset: new Vector2( 0,  1))},
            {"rogue_run_back_006",                   new FrameOffset(offset: new Vector2( 0,  0))},
            {"robot_idle_001",                       new FrameOffset(offset: new Vector2( 1,  0), flipOffset: new Vector2(-1,  0))},
            {"robot_idle_002",                       new FrameOffset(offset: new Vector2( 1,  0), flipOffset: new Vector2(-1,  0))},
            {"robot_idle_003",                       new FrameOffset(offset: new Vector2( 1,  0), flipOffset: new Vector2(-1,  0))},
            {"robot_idle_004",                       new FrameOffset(offset: new Vector2( 1,  0), flipOffset: new Vector2(-1,  0))},
            {"robot_idle_front_001",                 new FrameOffset(offset: new Vector2( 0, -1))},
            {"robot_idle_front_002",                 new FrameOffset(offset: new Vector2( 0,  1))},
            {"robot_idle_front_003",                 new FrameOffset(offset: new Vector2( 0, -1))},
            {"robot_idle_front_004",                 new FrameOffset(offset: new Vector2( 0, -2))},
            {"robot_run_front_001",                  new FrameOffset(offset: new Vector2( 0,  1))},
            {"robot_run_front_002",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"robot_run_front_003",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"robot_run_front_004",                  new FrameOffset(offset: new Vector2( 0,  1))},
            {"robot_run_front_005",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"robot_run_front_006",                  new FrameOffset(offset: new Vector2( 0, -1))},
            {"cultist_idle_front_right_001",         new FrameOffset(offset: new Vector2( 0,  0))},
            {"cultist_idle_front_right_002",         new FrameOffset(offset: new Vector2( 0, -1))},
            {"cultist_idle_front_right_003",         new FrameOffset(offset: new Vector2( 0, -2))},
            {"cultist_idle_front_right_004",         new FrameOffset(offset: new Vector2( 0, -1))},
            {"cultist_idle_back_right_003",          new FrameOffset(offset: new Vector2( 0, -1))},
            {"cultist_idle_back_right_004",          new FrameOffset(offset: new Vector2( 0, -1))},
            {"cultist_idle_front_001",               new FrameOffset(offset: new Vector2( 0,  0))},
            {"cultist_idle_front_002",               new FrameOffset(offset: new Vector2( 0, -1))},
            {"cultist_idle_front_003",               new FrameOffset(offset: new Vector2( 0, -1))},
            {"cultist_idle_front_004",               new FrameOffset(offset: new Vector2( 0, -1))},
            {"cultist_idle_front_005",               new FrameOffset(offset: new Vector2( 0,  0))},
            {"cultist_idle_front_006",               new FrameOffset(offset: new Vector2( 0,  0))},
            {"cultist_idle_back_003",                new FrameOffset(offset: new Vector2( 0, -1))},
            {"cultist_idle_back_004",                new FrameOffset(offset: new Vector2( 0, -1))},
            {"cultist_move_front_right_001",         new FrameOffset(offset: new Vector2( 1,  2), flipOffset: new Vector2(-1,  2))},
            {"cultist_move_front_right_002",         new FrameOffset(offset: new Vector2( 1,  1), flipOffset: new Vector2(-1,  1))},
            {"cultist_move_front_right_003",         new FrameOffset(offset: new Vector2( 0,  0))},
            {"cultist_move_front_right_004",         new FrameOffset(offset: new Vector2( 0,  1))},
            {"cultist_move_front_right_005",         new FrameOffset(offset: new Vector2( 0,  0))},
            {"cultist_move_front_right_006",         new FrameOffset(offset: new Vector2( 0,  0))},
            {"cultist_move_back_right_001",          new FrameOffset(offset: new Vector2( 0,  2))},
            {"cultist_move_back_right_002",          new FrameOffset(offset: new Vector2( 0,  1))},
            {"cultist_move_back_right_003",          new FrameOffset(offset: new Vector2( 0,  0))},
            {"cultist_move_back_right_004",          new FrameOffset(offset: new Vector2( 0,  1))},
            {"cultist_move_back_right_005",          new FrameOffset(offset: new Vector2( 0,  0))},
            {"cultist_move_back_right_006",          new FrameOffset(offset: new Vector2( 0,  0))},
            {"cultist_move_front_001",               new FrameOffset(offset: new Vector2( 2,  2), flipOffset: new Vector2(-2,  2))},
            {"cultist_move_front_002",               new FrameOffset(offset: new Vector2( 2,  0), flipOffset: new Vector2(-2,  0))},
            {"cultist_move_front_003",               new FrameOffset(offset: new Vector2( 0, -3))},
            {"cultist_move_front_004",               new FrameOffset(offset: new Vector2(-2,  1), flipOffset: new Vector2( 2,  1))},
            {"cultist_move_front_005",               new FrameOffset(offset: new Vector2(-2, -1), flipOffset: new Vector2( 2, -1))},
            {"cultist_move_front_006",               new FrameOffset(offset: new Vector2( 0, -2))},
            {"cultist_move_back_001",                new FrameOffset(offset: new Vector2( 0,  2))},
            {"cultist_move_back_002",                new FrameOffset(offset: new Vector2( 0,  0))},
            {"cultist_move_back_003",                new FrameOffset(offset: new Vector2( 0, -3))},
            {"cultist_move_back_004",                new FrameOffset(offset: new Vector2( 0,  1))},
            {"cultist_move_back_005",                new FrameOffset(offset: new Vector2( 0, -1))},
            {"cultist_move_back_006",                new FrameOffset(offset: new Vector2( 0, -2))},
            {"bullet_player_move_front_right_001",   new FrameOffset(offset: new Vector2( 0,  1), flipOffset: new Vector2( 0,  1))},
            {"bullet_player_move_front_right_002",   new FrameOffset(offset: new Vector2( 1,  0), flipOffset: new Vector2(-1,  0))},
            {"bullet_player_move_front_right_003",   new FrameOffset(offset: new Vector2( 1, -2), flipOffset: new Vector2(-1, -2))},
            {"bullet_player_move_front_right_004",   new FrameOffset(offset: new Vector2( 0,  1), flipOffset: new Vector2( 0,  1))},
            {"bullet_player_move_front_right_005",   new FrameOffset(offset: new Vector2( 0, -1), flipOffset: new Vector2( 0, -1))},
            {"bullet_player_move_front_right_006",   new FrameOffset(offset: new Vector2( 0,  2), flipOffset: new Vector2( 0,  2))},
            {"bullet_player_move_front_001",         new FrameOffset(offset: new Vector2( 0,  4), flipOffset: new Vector2( 0,  4))},
            {"bullet_player_move_front_002",         new FrameOffset(offset: new Vector2( 1,  4), flipOffset: new Vector2(-1,  4))},
            {"bullet_player_move_front_003",         new FrameOffset(offset: new Vector2( 1,  3), flipOffset: new Vector2(-1,  3))},
            {"bullet_player_move_front_004",         new FrameOffset(offset: new Vector2( 0,  4), flipOffset: new Vector2( 0,  4))},
            {"bullet_player_move_front_005",         new FrameOffset(offset: new Vector2(-2,  3), flipOffset: new Vector2( 2,  3))},
            {"bullet_player_move_front_006",         new FrameOffset(offset: new Vector2(-1,  2), flipOffset: new Vector2( 1,  2))},
            {"bullet_player_move_back_right_001",    new FrameOffset(offset: new Vector2( 0,  1), flipOffset: new Vector2( 0,  1))},
            {"bullet_player_move_back_right_002",    new FrameOffset(offset: new Vector2( 4,  1), flipOffset: new Vector2(-4,  1))},
            {"bullet_player_move_back_right_003",    new FrameOffset(offset: new Vector2( 4,  0), flipOffset: new Vector2(-4,  0))},
            {"bullet_player_move_back_right_004",    new FrameOffset(offset: new Vector2( 0,  1), flipOffset: new Vector2( 0,  1))},
            {"bullet_player_move_back_right_005",    new FrameOffset(offset: new Vector2(-2,  1), flipOffset: new Vector2( 2,  1))},
            {"bullet_player_move_back_right_006",    new FrameOffset(offset: new Vector2(-2,  1), flipOffset: new Vector2( 2,  1))},
            {"bullet_player_move_back_001",          new FrameOffset(offset: new Vector2( 0,  1), flipOffset: new Vector2( 0,  1))},
            {"bullet_player_move_back_002",          new FrameOffset(offset: new Vector2( 1,  1), flipOffset: new Vector2(-1,  1))},
            {"bullet_player_move_back_003",          new FrameOffset(offset: new Vector2( 1,  1), flipOffset: new Vector2(-1,  1))},
            {"bullet_player_move_back_004",          new FrameOffset(offset: new Vector2( 0,  1), flipOffset: new Vector2( 0,  1))},
            {"bullet_player_move_back_005",          new FrameOffset(offset: new Vector2(-2,  1), flipOffset: new Vector2( 2,  1))},
            {"bullet_player_move_back_006",          new FrameOffset(offset: new Vector2(-1,  1), flipOffset: new Vector2( 1,  1))},
        };

        private static string GetSpriteBaseName(string name)
        {
            return name.Replace("_hands2","").Replace("_hands","").Replace("_hand_left","").Replace("_hand_right","").Replace("_hand","").Replace("_twohands","").Replace("_armorless","");
        }

		public Vector3 GetHatPosition(PlayerController player)
        {
            cachedDef ??= player.sprite.GetCurrentSpriteDef();

            // get the base offset for every character
            Vector2 baseOffset = new Vector2(player.SpriteBottomCenter.x, player.sprite.transform.position.y);

            // get the base offset for every character
            // Vector2 baseOffset = new Vector2(player.SpriteBottomCenter.x, player.sprite.WorldTopCenter.y);

            // get the player specific offset
            Vector2 playerOffset = Vector2.zero;
            bool onEyes = (attachLevel == HatAttachLevel.EYE_LEVEL);
            var headOffsets = onEyes ? PlayerHatDatabase.CharacterNameEyeLevel : PlayerHatDatabase.CharacterNameHatHeadLevel;
            if (headOffsets.TryGetValue(player.name, out float headLevel))
                playerOffset = new Vector2(0f, headLevel);

            // get the flipped offset if applicable
            bool flipped = player.sprite.FlipX;
            Vector2 hatFlipOffset = (flipped && flipHorizontalWithPlayer) ? new Vector2(flipXOffset, 0f) : Vector2.zero;

            // get the animation frame specific offset if applicable
            Vector2 animationFrameOffset = GetDefOffset(cachedDef);
            string baseFrame = GetSpriteBaseName(cachedDef.name);
            if ((onEyes ? EyeFrameOffsets : HeadFrameOffsets).TryGetValue(baseFrame, out FrameOffset frameOffset))
                animationFrameOffset += flipped ? frameOffset.flipOffset : frameOffset.offset;
            cachedDefOffset = animationFrameOffset;

            // combine everything and return
            return baseOffset + hatOffset.XY() + playerOffset + animationFrameOffset + hatFlipOffset;
        }

        public void StickHatToPlayer(PlayerController player)
        {
            if (hatOwner == null)
                hatOwner = player;
            if (flipHorizontalWithPlayer && player.sprite)
                sprite.FlipX = player.sprite.FlipX;
            Vector2 vec = GetHatPosition(player);
            transform.position = vec;
            transform.rotation = hatOwner.transform.rotation;
            transform.parent = player.transform;
            player.sprite.AttachRenderer(gameObject.GetComponent<tk2dBaseSprite>());
            currentState = HatState.SITTING;
        }

        // Token: 0x0600818D RID: 33165 RVA: 0x0033787C File Offset: 0x00335A7C
        private void HandleAttachedSpriteDepth(float gunAngle)
        {
			if (hatDepthType == HatDepthType.BehindWhenFacingBack || hatDepthType == HatDepthType.InFrontWhenFacingBack)
			{
				if (hatOwner.CurrentGun is null)
				{
                    Vector2 m_playerCommandedDirection = commandedField.GetTypedValue<Vector2>(hatOwner);
                    Vector2 m_lastNonzeroCommandedDirection = lastNonZeroField.GetTypedValue<Vector2>(hatOwner);
                    gunAngle = BraveMathCollege.Atan2Degrees((!(m_playerCommandedDirection == Vector2.zero)) ? m_playerCommandedDirection : m_lastNonzeroCommandedDirection);
                }
                float forwardSign = 1f;
				float baseDepth;
				if (gunAngle <= 155f && gunAngle >= 25f)
				{
					forwardSign = -1f;
					baseDepth = 0.15f;
				}
				else
					baseDepth = -0.15f;

                if(hatDepthType == HatDepthType.BehindWhenFacingBack)
				    hatSprite.HeightOffGround = baseDepth + forwardSign * 1;
                else
                    hatSprite.HeightOffGround = baseDepth + forwardSign * -1;
            }
			else
			{
                if(hatDepthType == HatDepthType.AlwaysInFront)
                    hatSprite.HeightOffGround = 0.6f;
                else
                    hatSprite.HeightOffGround = -0.6f;
            }
        }
        private IEnumerator FlipHatIENum()
        {
            currentState = HatState.FLIPPING;
            startRolTime = BraveTime.ScaledTimeSinceStartup;
            endRollTime = startRolTime + RollLength;
			yield return new WaitForSeconds(RollLength);
            StickHatToPlayer(hatOwner);
            if (GameManager.AUDIO_ENABLED && !string.IsNullOrEmpty(FlipEndedSound))
            {
                AkSoundEngine.PostEvent(FlipEndedSound, gameObject);
            }
        }


        float startRolTime;
        float endRollTime;
        private void HandleFlip()
        {
            
            if (hatRollReaction == HatRollReaction.FLIP && !PlayerHasAdditionalVanishOverride())
            {
                if (hatOwnerAnimator == null) Debug.LogError("Attempted to flip a hat with a null hatOwnerAnimator!");
                else
                {
                    
                    if (hatOwner.IsDodgeRolling && currentState == HatState.SITTING && !hatOwner.IsSlidingOverSurface) 
                    {
                        if (GameManager.AUDIO_ENABLED && !string.IsNullOrEmpty(FlipStartedSound))
                        {
                            AkSoundEngine.PostEvent(FlipStartedSound, gameObject);
                        }
                        RollLength = hatOwner.rollStats.GetModifiedTime(hatOwner);
                        //ETGModConsole.Log(RollLength.ToString());
                        StartCoroutine(FlipHatIENum());
                    }
                    

                    if (currentState == HatState.FLIPPING)
                    {
                        if (!GameManager.Instance.IsPaused)
                        {
                           

                            if (hatOwnerAnimator.CurrentClip == null)
                            {
                                Debug.LogError("hatOwnerAnimator.CurrentClip is NULL!");
                            }
                            else if(!hatOwner.IsSlidingOverSurface)
                            {
                               
                                
                                Vector3 rotatePoint = sprite.WorldCenter;
                                float rollAmount = 360f * (BraveTime.DeltaTime / RollLength);
                                this.transform.RotateAround(this.sprite.WorldCenter, Vector3.forward, rollAmount * SpinSpeedMultiplier * (hatOwner.sprite.FlipX ? 1f : -1f));

                                float elapsed = BraveTime.ScaledTimeSinceStartup - startRolTime;
                                float percentDone = elapsed / RollLength;
                                this.transform.position = GetHatPosition(hatOwner) + new Vector3(0, BASE_FLIP_HEIGHT * flipHeightMultiplier * Mathf.Sin(Mathf.PI * percentDone), 0);
                            }
                        }
                        else
                        {
                            StickHatToPlayer(hatOwner);
                        }
                    }
                }
            }

        }
        #region enums
        public enum HatDepthType
        {
            AlwaysInFront,
            AlwaysBehind,
            BehindWhenFacingBack,
            InFrontWhenFacingBack
		}

        public enum HatDirectionality
        {
            NONE,
            TWOWAYHORIZONTAL,
            TWOWAYVERTICAL,
            FOURWAY,
            SIXWAY,
            EIGHTWAY,
        }
        public enum HatRollReaction
        {
            FLIP,
            VANISH,
            NONE,
        }
        public enum HatAttachLevel
        {
            HEAD_TOP,
            EYE_LEVEL,
        }
        public enum HatDirection
        {
            NORTH,
            SOUTH,
            WEST,
            EAST,
            NORTHWEST,
            NORTHEAST,
            SOUTHWEST,
            SOUTHEAST,
            NONE,
        }
        public enum HatState
        {
            SITTING,
            FLIPPING,
        }
		#endregion
	}


	static class ExtensionMethods { 
        public static T GetTypedValue<T>(this FieldInfo This, object instance) { return (T)This.GetValue(instance); }

        public static void MakeOffset(this tk2dSpriteDefinition def, Vector2 offset, bool changesCollider = false)
        {
            float xOffset = offset.x;
            float yOffset = offset.y;
            def.position0 += new Vector3(xOffset, yOffset, 0);
            def.position1 += new Vector3(xOffset, yOffset, 0);
            def.position2 += new Vector3(xOffset, yOffset, 0);
            def.position3 += new Vector3(xOffset, yOffset, 0);
            def.boundsDataCenter += new Vector3(xOffset, yOffset, 0);
            def.boundsDataExtents += new Vector3(xOffset, yOffset, 0);
            def.untrimmedBoundsDataCenter += new Vector3(xOffset, yOffset, 0);
            def.untrimmedBoundsDataExtents += new Vector3(xOffset, yOffset, 0);
            if (def.colliderVertices != null && def.colliderVertices.Length > 0 && changesCollider)
            {
                def.colliderVertices[0] += new Vector3(xOffset, yOffset, 0);
            }
        }

        public static void ConstructOffsetsFromAnchor(this tk2dSpriteDefinition def, tk2dBaseSprite.Anchor anchor, Vector2? scale = null, bool fixesScale = false, bool changesCollider = true)
        {
            if (!scale.HasValue)
            {
                scale = new Vector2?(def.position3);
            }
            if (fixesScale)
            {
                Vector2 fixedScale = scale.Value - def.position0.XY();
                scale = new Vector2?(fixedScale);
            }
            float xOffset = 0;
            if (anchor == tk2dBaseSprite.Anchor.LowerCenter || anchor == tk2dBaseSprite.Anchor.MiddleCenter || anchor == tk2dBaseSprite.Anchor.UpperCenter)
            {
                xOffset = -(scale.Value.x / 2f);
            }
            else if (anchor == tk2dBaseSprite.Anchor.LowerRight || anchor == tk2dBaseSprite.Anchor.MiddleRight || anchor == tk2dBaseSprite.Anchor.UpperRight)
            {
                xOffset = -scale.Value.x;
            }
            float yOffset = 0;
            if (anchor == tk2dBaseSprite.Anchor.MiddleLeft || anchor == tk2dBaseSprite.Anchor.MiddleCenter || anchor == tk2dBaseSprite.Anchor.MiddleLeft)
            {
                yOffset = -(scale.Value.y / 2f);
            }
            else if (anchor == tk2dBaseSprite.Anchor.UpperLeft || anchor == tk2dBaseSprite.Anchor.UpperCenter || anchor == tk2dBaseSprite.Anchor.UpperRight)
            {
                yOffset = -scale.Value.y;
            }
            def.MakeOffset(new Vector2(xOffset, yOffset), false);
            if (changesCollider && def.colliderVertices != null && def.colliderVertices.Length > 0)
            {
                float colliderXOffset = 0;
                if (anchor == tk2dBaseSprite.Anchor.LowerLeft || anchor == tk2dBaseSprite.Anchor.MiddleLeft || anchor == tk2dBaseSprite.Anchor.UpperLeft)
                {
                    colliderXOffset = (scale.Value.x / 2f);
                }
                else if (anchor == tk2dBaseSprite.Anchor.LowerRight || anchor == tk2dBaseSprite.Anchor.MiddleRight || anchor == tk2dBaseSprite.Anchor.UpperRight)
                {
                    colliderXOffset = -(scale.Value.x / 2f);
                }
                float colliderYOffset = 0;
                if (anchor == tk2dBaseSprite.Anchor.LowerLeft || anchor == tk2dBaseSprite.Anchor.LowerCenter || anchor == tk2dBaseSprite.Anchor.LowerRight)
                {
                    colliderYOffset = (scale.Value.y / 2f);
                }
                else if (anchor == tk2dBaseSprite.Anchor.UpperLeft || anchor == tk2dBaseSprite.Anchor.UpperCenter || anchor == tk2dBaseSprite.Anchor.UpperRight)
                {
                    colliderYOffset = -(scale.Value.y / 2f);
                }
                def.colliderVertices[0] += new Vector3(colliderXOffset, colliderYOffset, 0);
            }
        }

    }
}
