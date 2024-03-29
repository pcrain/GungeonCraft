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

namespace Alexandria.cAPI
{
    public class Hat : BraveBehaviour
    {
        private const float BASE_FLIP_HEIGHT = 3f;

        public string hatName = null;
        public Vector2 hatOffset = Vector2.zero;
        public HatDirectionality hatDirectionality = HatDirectionality.NONE;
        public HatRollReaction hatRollReaction = HatRollReaction.FLIP;
        public HatAttachLevel attachLevel = HatAttachLevel.HEAD_TOP;
        public string FlipStartedSound = null;
        public string FlipEndedSound = null;
        public HatDepthType hatDepthType = HatDepthType.AlwaysInFront;
        public PlayerController hatOwner = null;
        public float SpinSpeedMultiplier = 1f;
        public float flipHeightMultiplier = 1f;
        public bool goldenPedastal = false;
        public bool flipHorizontalWithPlayer = true;

        private HatDirection currentDirection = HatDirection.NONE;
        private HatState currentState = HatState.SITTING;
        private tk2dSprite hatSprite = null;
        private tk2dSpriteAnimator hatSpriteAnimator = null;
        private tk2dSpriteAnimator hatOwnerAnimator = null;
        private tk2dSpriteDefinition cachedDef = null;
        private Vector2 cachedDefOffset = Vector2.zero;
        private float RollLength = 0.65f; //The time it takes for a player with no dodge roll effects to roll
        private float startRolTime = 0.0f;

        private void Start()
        {
            if (!hatOwner)
                return;
            hatSprite = base.GetComponent<tk2dSprite>();
            hatSpriteAnimator = base.GetComponent<tk2dSpriteAnimator>();
            SpriteOutlineManager.AddOutlineToSprite(hatSprite, Color.black, 1);
            GameObject playerSprite = hatOwner.transform.Find("PlayerSprite").gameObject;
            hatOwnerAnimator = playerSprite.GetComponent<tk2dSpriteAnimator>();
            hatOwner.OnPreDodgeRoll += this.HatReactToDodgeRoll;
            UpdateHatFacingDirection();
        }

		public override void OnDestroy()
        {
            if (hatOwner)
                hatOwner.OnPreDodgeRoll -= this.HatReactToDodgeRoll;
            base.OnDestroy();
        }

        private void HatReactToDodgeRoll(PlayerController player)
        {
            /* unfinished */
        }

        private void Update()
        {
            if (!hatOwner)
                return;

            if (currentState == HatState.SITTING)
                StickHatToPlayer(hatOwner);
            HandleVanish(); //Make the Hat vanish upon pitfall, or when the player rolls if the hat is VANISH type
            UpdateHatFacingDirection();
            HandleAttachedSpriteDepth();
            HandleFlip();
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

                hat.cachedDef = def; // cache the new sprite definition
                if (hat.hatOwner && hat.currentState == HatState.SITTING)
                {
                    hat.transform.position = hat.GetHatPosition(hat.hatOwner); // update the hat position in light of the new sprite definition
                    hat.UpdateHatFacingDirection();
                    hat.HandleAttachedSpriteDepth();
                }
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
            return (hatOwner && hatOwner.HasPickupID(436)); // 436 == Bloodied Scarf
        }

		public void UpdateHatFacingDirection()
        {
            HatDirection targetDir = FetchOwnerFacingDirection();
            if (targetDir == currentDirection)
                return; // nothing to update
            currentDirection = targetDir; // cache the actual targetDir rather than adjustedDir so we don't call this every frame unnecessarily

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
                case HatDirection.SOUTH:     { hatSpriteAnimator.Play("hat_south"); }     break;
                case HatDirection.NORTH:     { hatSpriteAnimator.Play("hat_north"); }     break;
                case HatDirection.WEST:      { hatSpriteAnimator.Play("hat_west"); }      break;
                case HatDirection.EAST:      { hatSpriteAnimator.Play("hat_east"); }      break;
                case HatDirection.NORTHWEST: { hatSpriteAnimator.Play("hat_northwest"); } break;
                case HatDirection.NORTHEAST: { hatSpriteAnimator.Play("hat_northeast"); } break;
                case HatDirection.NONE:
                    ETGModConsole.Log("ERROR: TRIED TO ROTATE HAT TO A NULL DIRECTION! (wtf?)");
                    break;
            }
        }

        public HatDirection FetchOwnerFacingDirection()
        {
            if (hatOwner == null || hatOwner.sprite == null)
                return HatDirection.EAST; // return a sane default if we're ownerless

            // figure out an approximate direction from the player's animation name
            string animName = cachedDef.name;
            if (animName.Contains("north_"))       return HatDirection.NORTH;
            if (animName.Contains("back_"))        return HatDirection.NORTH;
            if (animName.Contains("south_"))       return HatDirection.SOUTH;
            if (animName.Contains("front_"))       return HatDirection.SOUTH;
            if (animName.Contains("back_right_"))  return hatOwner.sprite.FlipX ? HatDirection.NORTHWEST : HatDirection.NORTHEAST;
            if (animName.Contains("backwards_"))   return hatOwner.sprite.FlipX ? HatDirection.NORTHWEST : HatDirection.NORTHEAST;
            if (animName.Contains("backward_"))    return hatOwner.sprite.FlipX ? HatDirection.NORTHWEST : HatDirection.NORTHEAST;
            if (animName.Contains("bw_"))          return hatOwner.sprite.FlipX ? HatDirection.NORTHWEST : HatDirection.NORTHEAST;
            //NOTE: these are technically all unnecessary since we defaut to WEST / EAST anyway
            // if (animName.Contains("front_right_")) return hatOwner.sprite.FlipX ? HatDirection.WEST : HatDirection.EAST;
            // if (animName.Contains("right_front_")) return hatOwner.sprite.FlipX ? HatDirection.WEST : HatDirection.EAST;
            // if (animName.Contains("forward_"))     return hatOwner.sprite.FlipX ? HatDirection.WEST : HatDirection.EAST;

            return hatOwner.sprite.FlipX ? HatDirection.WEST : HatDirection.EAST; // return a sane default if we're ownerless
        }

        private static string GetSpriteBaseName(string name)
        {
            return name.Replace("_hands2","").Replace("_hands","").Replace("_hand_left","").Replace("_hand_right","").Replace("_hand","").Replace("_twohands","").Replace("_armorless","");
        }

        private static Vector2 GetDefOffset(tk2dSpriteDefinition def)
        {
            return new Vector2(0f, def.boundsDataCenter.y + 0.5f * def.boundsDataExtents.y);
        }

		public Vector3 GetHatPosition(PlayerController player)
        {
            cachedDef ??= player.sprite.GetCurrentSpriteDef();

            // get the base offset for every character
            Vector2 baseOffset = new Vector2(player.SpriteBottomCenter.x, player.sprite.transform.position.y);

            // get the player specific offset
            Vector2 playerSpecificOffset = Vector2.zero;
            bool onEyes = (attachLevel == HatAttachLevel.EYE_LEVEL);
            var headOffsets = onEyes ? Hatabase.CharacterNameEyeLevel : Hatabase.CharacterNameHatHeadLevel;
            if (headOffsets.TryGetValue(player.name, out float headLevel))
                playerSpecificOffset = new Vector2(0f, headLevel);

            // get the hat specific offset
            bool flipped = player.sprite.FlipX;
            Vector2 hatSpecificOffset = (flipped ? hatOffset.WithX(-hatOffset.x) : hatOffset);

            // get the animation frame specific offset, if one is available
            Vector2 animationFrameSpecificOffset = GetDefOffset(cachedDef);
            string baseFrame = GetSpriteBaseName(cachedDef.name);
            if ((onEyes ? Hatabase.EyeFrameOffsets : Hatabase.HeadFrameOffsets).TryGetValue(baseFrame, out Hatabase.FrameOffset frameOffset))
                animationFrameSpecificOffset += flipped ? frameOffset.flipOffset : frameOffset.offset;
            cachedDefOffset = animationFrameSpecificOffset;

            // combine everything and return
            return baseOffset + hatSpecificOffset + playerSpecificOffset + animationFrameSpecificOffset;
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

        private void HandleAttachedSpriteDepth()
        {
            if (hatDepthType == HatDepthType.AlwaysInFront)
            {
                hatSprite.HeightOffGround = 0.6f;
                return;
            }
            if (hatDepthType == HatDepthType.AlwaysBehind)
            {
                hatSprite.HeightOffGround = -0.6f;
                return;
            }

            bool facingBack = (currentDirection == HatDirection.NORTH || currentDirection == HatDirection.NORTHEAST || currentDirection == HatDirection.NORTHWEST);
            if(hatDepthType == HatDepthType.BehindWhenFacingBack)
			    hatSprite.HeightOffGround = facingBack ? -0.85f :  0.85f;
            else
                hatSprite.HeightOffGround = facingBack ?  1.15f : -1.15f;
        }

        private IEnumerator FlipHatIENum()
        {
            currentState = HatState.FLIPPING;
            startRolTime = BraveTime.ScaledTimeSinceStartup;
			yield return new WaitForSeconds(RollLength);
            StickHatToPlayer(hatOwner);
            if (GameManager.AUDIO_ENABLED && !string.IsNullOrEmpty(FlipEndedSound))
            {
                AkSoundEngine.PostEvent(FlipEndedSound, gameObject);
            }
        }

        private void HandleFlip()
        {
            if (BraveTime.DeltaTime == 0.0f)
                return; // don't do anything while time is frozen

            if (hatRollReaction != HatRollReaction.FLIP || PlayerHasAdditionalVanishOverride())
                return; // no flipping needed

            if (hatOwnerAnimator == null)
            {
                Debug.LogError("Attempted to flip a hat with a null hatOwnerAnimator!");
                return;
            }

            if (hatOwner.IsDodgeRolling && currentState == HatState.SITTING && !hatOwner.IsSlidingOverSurface)
            {
                if (GameManager.AUDIO_ENABLED && !string.IsNullOrEmpty(FlipStartedSound))
                    AkSoundEngine.PostEvent(FlipStartedSound, gameObject);
                RollLength = hatOwner.rollStats.GetModifiedTime(hatOwner);
                StartCoroutine(FlipHatIENum());
            }

            if (currentState != HatState.FLIPPING)
                return; // not flipping, so nothing to do

            if (GameManager.Instance.IsPaused)
            {
                StickHatToPlayer(hatOwner);
                return;
            }

            if (hatOwnerAnimator.CurrentClip == null)
            {
                Debug.LogError("hatOwnerAnimator.CurrentClip is NULL!");
                return;
            }

            if(hatOwner.IsSlidingOverSurface)
                return; // no flipping needed while sliding

            // logic for doing the actual flipping
            float rollAmount = 360f * (BraveTime.DeltaTime / RollLength);
            this.transform.RotateAround(this.sprite.WorldCenter, Vector3.forward, rollAmount * SpinSpeedMultiplier * (hatOwner.sprite.FlipX ? 1f : -1f));
            float elapsed = BraveTime.ScaledTimeSinceStartup - startRolTime;
            float percentDone = elapsed / RollLength;
            this.transform.position = GetHatPosition(hatOwner) + new Vector3(0, BASE_FLIP_HEIGHT * flipHeightMultiplier * Mathf.Sin(Mathf.PI * percentDone), 0);
        }

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
	}
}
