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

        private FieldInfo commandedField, lastNonZeroField, lockedDodgeRollDirection, m_currentGunAngle;
        private HatDirection currentDirection;
        private HatState currentState;
        public tk2dSprite hatSprite;
        public tk2dSpriteAnimator hatSpriteAnimator;
        private tk2dSpriteAnimator hatOwnerAnimator;
        
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
		#region vanishing
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
		#endregion
		#region facingCode
		public void UpdateHatFacingDirection(HatDirection targetDir)
        {
            string animToPlay = "null";
            if (hatDirectionality == HatDirectionality.NONE)
            {
                animToPlay = "hat_south";
                currentDirection = HatDirection.SOUTH;
            }
            else
            {
                switch (targetDir)
                {
                    case HatDirection.SOUTH:
                        if (hatDirectionality != HatDirectionality.TWOWAYHORIZONTAL) { animToPlay = "hat_south"; }
                        break;
                    case HatDirection.NORTH:
                        if (hatDirectionality != HatDirectionality.TWOWAYHORIZONTAL) { animToPlay = "hat_north"; }
                        break;
                    case HatDirection.WEST:
                        if (hatDirectionality != HatDirectionality.TWOWAYVERTICAL) { animToPlay = "hat_west"; }
                        break;
                    case HatDirection.EAST:
                        if (hatDirectionality != HatDirectionality.TWOWAYVERTICAL) { animToPlay = "hat_east"; }
                        break;
                    case HatDirection.SOUTHWEST:
                        if (hatDirectionality == HatDirectionality.EIGHTWAY) { animToPlay = "hat_southwest"; }
                        break;
                    case HatDirection.SOUTHEAST:
                        if (hatDirectionality == HatDirectionality.EIGHTWAY) { animToPlay = "hat_southeast"; }
                        break;
                    case HatDirection.NORTHWEST:
                        if (hatDirectionality == HatDirectionality.SIXWAY || hatDirectionality == HatDirectionality.EIGHTWAY) { animToPlay = "hat_northwest"; }
                        break;
                    case HatDirection.NORTHEAST:
                        if (hatDirectionality == HatDirectionality.SIXWAY || hatDirectionality == HatDirectionality.EIGHTWAY) { animToPlay = "hat_northeast"; }
                        break;
                    case HatDirection.NONE:
                        ETGModConsole.Log("ERROR: TRIED TO ROTATE HAT TO A NULL DIRECTION! (wtf?)");
                        break;
                }
                currentDirection = targetDir;
            }
            if (animToPlay != "null")
            {
                hatSpriteAnimator.Play(animToPlay);
            }
        }

        public HatDirection FetchOwnerFacingDirection()
        {
            HatDirection hatDir = HatDirection.NONE;
            if (hatOwner != null)
            {
                if (hatOwner.CurrentGun == null)
                {
                    Vector2 m_playerCommandedDirection = commandedField.GetTypedValue<Vector2>(hatOwner);
                    Vector2 m_lastNonzeroCommandedDirection = lastNonZeroField.GetTypedValue<Vector2>(hatOwner);

                    float playerCommandedDir = BraveMathCollege.Atan2Degrees((!(m_playerCommandedDirection == Vector2.zero)) ? m_playerCommandedDirection : m_lastNonzeroCommandedDirection);

                    switch (playerCommandedDir)
                    {
                        case 90:
                            hatDir = HatDirection.NORTH;
                            break;
                        case 45:
                            hatDir = HatDirection.NORTHEAST;
                            break;
                        case -90:
                            hatDir = HatDirection.SOUTH;
                            break;
                        case -135:
                            hatDir = HatDirection.SOUTHWEST;
                            break;
                        case -180:
                            hatDir = HatDirection.WEST;
                            break;
                        case 135:
                            hatDir = HatDirection.NORTHWEST;
                            break;
                        case -45:
                            hatDir = HatDirection.SOUTHEAST;
                            break;
                        case 180:
                            hatDir = HatDirection.WEST;
                            break;
                    }
                    if (playerCommandedDir == 0 && hatOwner.Velocity != new Vector2(0f, 0))
                    {
                        hatDir = HatDirection.EAST;
                    }
                }
                else
                {
                    int FacingDirection = Mathf.RoundToInt(hatOwner.FacingDirection / 45) * 45;
                    switch (FacingDirection)
                    {
                        case 90:
                            hatDir = HatDirection.NORTH;
                            break;
                        case 45:
                            hatDir = HatDirection.NORTHEAST;
                            break;
                        case 0:
                            hatDir = HatDirection.EAST;
                            break;
                        case -45:
                            hatDir = HatDirection.SOUTHEAST;
                            break;
                        case -90:
                            hatDir = HatDirection.SOUTH;
                            break;
                        case -135:
                            hatDir = HatDirection.SOUTHWEST;
                            break;
                        case -180:
                            hatDir = HatDirection.WEST;
                            break;
                        case 135:
                            hatDir = HatDirection.NORTHWEST;
                            break;
                        case 180:                           
                            hatDir = HatDirection.WEST;
                            break;
                    }
                }
            }

            else Debug.LogError("Attempted to get hatOwner facing direction with a null hatOwner!");
            if (hatDir == HatDirection.NONE) hatDir = HatDirection.SOUTH;
            
            return hatDir;
        }
		#endregion

        public int GetPlayerAnimFrame(PlayerController player)
		{
            return player.spriteAnimator.CurrentFrame;

        }

        public static readonly Dictionary<string, float> BobOffsets = new(){
            {"convict_idle_002", -1/16f},
            {"convict_idle_front_002", -1/16f},
            {"convict_idle_back_002", -1/16f},
            {"convict_idle_bw_002", -1/16f},
            {"convict_idle_003", -1/16f},
            {"convict_idle_front_003", -1/16f},
            {"convict_idle_back_003", -1/16f},
            {"convict_idle_bw_003", -1/16f},
            {"convict_run_forward_001", -1/16f},
            {"convict_run_forward_002", 2/16f},
            {"convict_run_forward_004", -1/16f},
            {"convict_run_forward_005", 2/16f},
            {"convict_run_backwards_001", -1/16f},
            {"convict_run_backwards_002", 2/16f},
            {"convict_run_backwards_004", -1/16f},
            {"convict_run_backwards_005", 2/16f},
            {"convict_run_north_001", -1/16f},
            {"convict_run_north_002", 2/16f},
            {"convict_run_north_004", -1/16f},
            {"convict_run_north_005", 2/16f},
            {"convict_run_south_001", -1/16f},
            {"convict_run_south_002", 2/16f},
            {"convict_run_south_004", -1/16f},
            {"convict_run_south_005", 2/16f},

            {"guide_idle_right_front_002", -1/16f},
            {"guide_idle_right_front_003", -1/16f},
            {"guide_idle_front_002", -1/16f},
            {"guide_idle_front_003", -1/16f},
            {"guide_idle_back_002", -1/16f},
            {"guide_idle_back_003", -1/16f},
            {"guide_idle_back_right_002", -1/16f},
            {"guide_idle_back_right_003", -1/16f},
            {"guide_run_right_front_001", 2/16f},
            {"guide_run_right_front_002", 1/16f},
            {"guide_run_right_front_003", -1/16f},
            {"guide_run_right_front_004", 2/16f},
            {"guide_run_right_front_006", -1/16f},
            {"guide_run_back_right_001", 2/16f},
            {"guide_run_back_right_002", 1/16f},
            {"guide_run_back_right_003", -1/16f},
            {"guide_run_back_right_004", 2/16f},
            {"guide_run_back_right_006", -1/16f},
            {"guide_run_front_001", 2/16f},
            {"guide_run_front_003", -2/16f},
            {"guide_run_front_004", 2/16f},
            {"guide_run_front_006", -2/16f},
            {"guide_run_back_001", 2/16f},
            {"guide_run_back_003", -2/16f},
            {"guide_run_back_004", 2/16f},
            {"guide_run_back_006", -2/16f},

            {"marine_idle_front_right_002", -1/16f},
            {"marine_idle_front_right_003", -2/16f},
            {"marine_idle_front_right_004", -1/16f},
            {"marine_idle_back_right_002", -1/16f},
            {"marine_idle_back_right_003", -2/16f},
            {"marine_idle_back_right_004", -1/16f},
            {"marine_idle_back_002", -1/16f},
            {"marine_idle_back_003", -3/16f},
            {"marine_idle_back_004", -2/16f},
            {"marine_idle_front_002", -1/16f},
            {"marine_idle_front_003", -3/16f},
            {"marine_idle_front_004", -2/16f},
            {"marine_run_front_right_001", 2/16f},
            {"marine_run_front_right_002", 1/16f},
            {"marine_run_front_right_003", -2/16f},
            {"marine_run_front_right_004", 2/16f},
            {"marine_run_front_right_005", 1/16f},
            {"marine_run_front_right_006", -2/16f},
            {"marine_run_back_right_001", 2/16f},
            {"marine_run_back_right_002", 1/16f},
            {"marine_run_back_right_003", -2/16f},
            {"marine_run_back_right_004", 2/16f},
            {"marine_run_back_right_005", 1/16f},
            {"marine_run_back_right_006", -2/16f},
            {"marine_run_front_001", 1/16f},
            {"marine_run_front_002", 0/16f},
            {"marine_run_front_003", -2/16f},
            {"marine_run_front_004", 1/16f},
            {"marine_run_front_005", 0/16f},
            {"marine_run_front_006", -2/16f},
            {"marine_run_back_001", 2/16f},
            {"marine_run_back_002", 1/16f},
            {"marine_run_back_003", -1/16f},
            {"marine_run_back_004", 2/16f},
            {"marine_run_back_005", 1/16f},
            {"marine_run_back_006", -1/16f},

            {"rogue_idle_002", -1/16f},
            {"rogue_idle_003", -1/16f},
            {"rogue_idle_back_002", -1/16f},
            {"rogue_idle_back_003", -1/16f},
            {"rogue_idle_backwards_002", -1/16f},
            {"rogue_idle_backwards_003", -1/16f},
            {"rogue_idle_front_002", -1/16f},
            {"rogue_idle_front_003", -1/16f},
            {"rogue_run_forward_001", -1/16f},
            {"rogue_run_forward_002", 2/16f},
            {"rogue_run_forward_003", 0/16f},
            {"rogue_run_forward_004", -1/16f},
            {"rogue_run_forward_005", 3/16f},
            {"rogue_run_forward_006", 0/16f},
            {"rogue_run_backward_001", -1/16f},
            {"rogue_run_backward_002", 2/16f},
            {"rogue_run_backward_003", 0/16f},
            {"rogue_run_backward_004", -1/16f},
            {"rogue_run_backward_005", 3/16f},
            {"rogue_run_backward_006", 0/16f},
            {"rogue_run_front_001", 3/16f},
            {"rogue_run_front_002", 2/16f},
            {"rogue_run_front_003", -1/16f},
            {"rogue_run_front_004", 2/16f},
            {"rogue_run_front_005", 1/16f},
            {"rogue_run_front_006", 0/16f},
            {"rogue_run_back_001", 3/16f},
            {"rogue_run_back_002", 2/16f},
            {"rogue_run_back_003", -1/16f},
            {"rogue_run_back_004", 2/16f},
            {"rogue_run_back_005", 1/16f},
            {"rogue_run_back_006", 0/16f},

            {"robot_idle_front_001", -2/16f},
            {"robot_idle_front_002", 0/16f},
            {"robot_idle_front_003", -2/16f},
            {"robot_idle_front_004", -3/16f},
            {"robot_run_front_001", 0/16f},
            {"robot_run_front_002", -2/16f},
            {"robot_run_front_003", -2/16f},
            {"robot_run_front_004", 0/16f},
            {"robot_run_front_005", -2/16f},
            {"robot_run_front_006", -2/16f},


        };

		public Vector3 GetHatPosition(PlayerController player)
        {
            // get the base offset for every character
            Vector2 baseOffset = new Vector2(player.SpriteBottomCenter.x, player.sprite.WorldTopCenter.y);

            // get the player specific offset
            Vector2 playerOffset = Vector2.zero;
            if (PlayerHatDatabase.CharacterNameHatHeadLevel.TryGetValue(player.name, out float headLevel))
                playerOffset = new Vector2(0f, headLevel);

            // determine the hat direction from the current animation name
            HatDirection hatDirection = HatDirection.NONE;
            string curAnim = player.spriteAnimator.currentClip.name;
            bool flipped = player.sprite.FlipX;
            if (curAnim.Contains("forward") || curAnim.Contains("run_down"))
                hatDirection = HatDirection.SOUTH;
            else if (curAnim.Contains("backward") || curAnim.Contains("run_up"))
                hatDirection = HatDirection.NORTH;
            else if (curAnim.Contains("bw"))
                hatDirection = HatDirection.NORTHEAST;
            else
                hatDirection = HatDirection.EAST;

            Vector2 directionalOffset = Vector2.zero;
            if (PlayerHatDatabase.CharacterDirectionalHatOffsets.TryGetValue(player.name, out HatOffsets playerDirectionalHatOffsets))
                directionalOffset = hatDirection switch {
                    HatDirection.SOUTH     => (flipped ? playerDirectionalHatOffsets.frontFlipped : playerDirectionalHatOffsets.front),
                    HatDirection.NORTH     => (flipped ? playerDirectionalHatOffsets.backFlipped : playerDirectionalHatOffsets.back),
                    HatDirection.NORTHEAST => (flipped ? playerDirectionalHatOffsets.bwFlipped : playerDirectionalHatOffsets.bw),
                    HatDirection.EAST      => (flipped ? playerDirectionalHatOffsets.fwFlipped : playerDirectionalHatOffsets.fw),
                    _                      => Vector2.zero,
                };

            Vector2 bobOffset = Vector2.zero;
            string baseFrame = player.sprite.GetCurrentSpriteDef().name.Replace("_hands2","").Replace("_hands","").Replace("_hand_left","").Replace("_hand_right","").Replace("_hand","").Replace("_twohands","").Replace("_armorless","");
            // ETGModConsole.Log($"  frame is {player.sprite.GetCurrentSpriteDef().name}");
            if (BobOffsets.TryGetValue(baseFrame, out float bobAmount))
                bobOffset = new Vector2(0f, bobAmount);


            return baseOffset + hatOffset.XY() + playerOffset + directionalOffset + bobOffset;
            // if (attachLevel == HatAttachLevel.HEAD_TOP)
            // {
            //     if (PlayerHatDatabase.CharacterNameHatHeadLevel.ContainsKey(player.name))
            //     {
            //         vec = new Vector3(basePos.x, basePos.y + PlayerHatDatabase.CharacterNameHatHeadLevel[player.name], player.transform.position.z +1);
            //     }
            //     else { vec = (basePos + new Vector2(0, PlayerHatDatabase.defaultHeadLevelOffset)); }
            // }
            // else if (attachLevel == HatAttachLevel.EYE_LEVEL)
            // {
            //     if (PlayerHatDatabase.CharacterNameEyeLevel.ContainsKey(player.name))
            //     {
            //         vec = (basePos + new Vector2(0, PlayerHatDatabase.CharacterNameEyeLevel[player.name]));
            //     }
            //     else { vec = (basePos + new Vector2(0, PlayerHatDatabase.defaultEyeLevelOffset)); }
            // }
            // vec += new Vector3(0, 0.03f, player.transform.position.z + 1);
            // vec += hatOffset;
            // if (player.sprite.FlipX)
            // {
            //     if (PlayerHatDatabase.CharacterNameFlippedOffset.ContainsKey(player.name))
            //         vec += new Vector3(PlayerHatDatabase.CharacterNameFlippedOffset[player.name], 0f, 0f);
            //     else
            //         vec += new Vector3(PlayerHatDatabase.defaultFlippedOffset, 0f, 0f);
            // }
            // return vec;
        }

        public void StickHatToPlayer(PlayerController player)
        {
            if (hatOwner == null)
                hatOwner = player;
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
				float num = 1f;
				if (hatOwner.CurrentGun is null)
				{
                    Vector2 m_playerCommandedDirection = commandedField.GetTypedValue<Vector2>(hatOwner);
                    Vector2 m_lastNonzeroCommandedDirection = lastNonZeroField.GetTypedValue<Vector2>(hatOwner);
                    gunAngle = BraveMathCollege.Atan2Degrees((!(m_playerCommandedDirection == Vector2.zero)) ? m_playerCommandedDirection : m_lastNonzeroCommandedDirection);
                }
				float num2;
				if (gunAngle <= 155f && gunAngle >= 25f)
				{
					num = -1f;
					if (gunAngle < 120f && gunAngle >= 60f)
					{
						num2 = 0.15f;
					}
					else
					{
						num2 = 0.15f;
					}
				}
				else if (gunAngle <= -60f && gunAngle >= -120f)
				{
					num2 = -0.15f;
				}
				else
				{
					num2 = -0.15f;
				}

                if(hatDepthType == HatDepthType.BehindWhenFacingBack)
				    hatSprite.HeightOffGround = num2 + num * 1;
                else
                    hatSprite.HeightOffGround = num2 + num * -1;
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
                                if (FetchOwnerFacingDirection() == HatDirection.SOUTH || FetchOwnerFacingDirection() == HatDirection.NORTHEAST || FetchOwnerFacingDirection() == HatDirection.SOUTHEAST || FetchOwnerFacingDirection() == HatDirection.EAST)
                                {
                                    this.transform.RotateAround(this.sprite.WorldCenter, Vector3.forward, -rollAmount * SpinSpeedMultiplier);
                                }
                                else
                                {
                                    this.transform.RotateAround(this.sprite.WorldCenter, Vector3.forward, rollAmount * SpinSpeedMultiplier);
                                }

                                float elapsed = BraveTime.ScaledTimeSinceStartup - startRolTime;
                                float percentDone = elapsed / RollLength;
                                this.transform.position = GetHatPosition(hatOwner) + new Vector3(0, 2f * flipHeightMultiplier * Mathf.Sin(Mathf.PI * percentDone), 0);

                                // if (elapsed < (endRollTime - startTime) * 0.01)
                                //     this.transform.position = GetHatPosition(hatOwner);
                                // else if (elapsed < (endRollTime - startTime) * 0.2)
                                //     this.transform.position += new Vector3(0, flipHeightMultiplier * 0.35f, 0);
                                // else if (elapsed < (endRollTime - startTime) * 0.2)
                                //     this.transform.position += new Vector3(0, flipHeightMultiplier * 0.3f, 0);
                                // else if (elapsed < (endRollTime - startTime) * 0.3)
                                //     this.transform.position += new Vector3(0, flipHeightMultiplier * 0.2f, 0);
                                // else if (elapsed < (endRollTime - startTime) * 0.4)
                                //     this.transform.position += new Vector3(0, flipHeightMultiplier * 0.15f, 0);
                                // else if (elapsed < (endRollTime - startTime) * 0.5)
                                //     this.transform.position += new Vector3(0, flipHeightMultiplier * 0.05f, 0);
                                // else if (elapsed < (endRollTime - startTime) * 0.6)
                                //     this.transform.position -= new Vector3(0, flipHeightMultiplier * 0.05f, 0);
                                // else if (elapsed < (endRollTime - startTime) * 0.7)
                                //     this.transform.position -= new Vector3(0, flipHeightMultiplier * 0.15f, 0);
                                // else if (elapsed < (endRollTime - startTime) * 0.8)
                                //     this.transform.position -= new Vector3(0, flipHeightMultiplier * 0.2f, 0);
                                // else if (elapsed < (endRollTime - startTime) * 0.9)
                                //     this.transform.position -= new Vector3(0, flipHeightMultiplier * 0.3f, 0);
                                // else
                                //     this.transform.position -= new Vector3(0, flipHeightMultiplier * 0.35f, 0);
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
