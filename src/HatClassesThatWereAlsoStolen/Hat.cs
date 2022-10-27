using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Gungeon;
using Dungeonator;
using System.Reflection;
using ItemAPI;
using System.Collections;
using System.Globalization;

namespace CwaffingTheGungy
{
    static class OMITBReflectionHelpers
    {
        public static T GetTypedValue<T>(this FieldInfo This, object instance) { return (T)This.GetValue(instance); }
        public static T ReflectGetField<T>(Type classType, string fieldName, object o = null)
        {
            FieldInfo field = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | ((o != null) ? BindingFlags.Instance : BindingFlags.Static));
            return (T)field.GetValue(o);
        }
        public static void ReflectSetField<T>(Type classType, string fieldName, object value, object o = null)
        {
            FieldInfo field = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | ((o != null) ? BindingFlags.Instance : BindingFlags.Static));
            field.SetValue(o, value);
        }
    }
    public class Hat : BraveBehaviour
    {
        public Hat()
        {
            hatOffset = new Vector2(0, 0);
            hatOwner = null;
            hatFlipSpeedMult = 1;
            flipHeightMultiplier = 1;
            hatRollReaction = HatRollReaction.FLIP;
            hatDirectionality = HatDirectionality.NONE;
            attachLevel = HatAttachLevel.HEAD_TOP;
            hatDepthType = HatDepthType.AlwaysInFront;
        }

        //PUBLIC VARIABLES
        public PlayerController hatOwner;
        public string hatName;
        public Vector2 hatOffset;
        public HatDirectionality hatDirectionality;
        public HatRollReaction hatRollReaction;
        public HatAttachLevel attachLevel;
        public float hatFlipSpeedMult;
        public float flipHeightMultiplier;
        public bool backDiagonalUseNorth;
        public bool frontDiagonalUseLeftRight;
        public HatDepthType hatDepthType;

        //PRIVATE VARIABLES
        private FieldInfo commandedField, lastNonZeroField, lockedDodgeRollDirection, m_currentGunAngle;
        private HatDirection currentDirection;
        private tk2dSprite hatSprite;
        private tk2dSpriteAnimator hatSpriteAnimator;
        private tk2dSpriteAnimator hatOwnerAnimator;
        private HatState currentState; //Whether the hat is flipping or still

        //Variables for shit related to adjusting the hat to accomodate sprite flip
        private bool hatIsAccomodatingPlayerFlip;
        private bool lastCheckedPlayerFlipped;

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
                //Make the Hat vanish upon pitfall, or when the player rolls if the hat is VANISH type
                HandleVanish();

                //Makes sure the hat gets it's position updated for player sprite flip
                if (lastCheckedPlayerFlipped != hatOwner.SpriteFlipped) UpdateOffsetForGunHandedness();

                //UPDATE DIRECTIONS
                HatDirection checkedDir = FetchOwnerFacingDirection();
                if (checkedDir != currentDirection) UpdateHatFacingDirection(checkedDir);

                HandleAttachedSpriteDepth(m_currentGunAngle.GetTypedValue<float>(hatOwner));
            }
        }
        private void FixedUpdate()
        {
            HandleFlip();
        }
        private void UpdateOffsetForGunHandedness()
        {
            if (hatOwner && hatOwner.IsDodgeRolling == false)
            {
                lastCheckedPlayerFlipped = hatOwner.SpriteFlipped;
                if (PlayerHatDatabase.SpecialCharacterFlipOffsets.ContainsKey(hatOwner.characterIdentity))
                {
                    if (hatOwner.SpriteFlipped && !hatIsAccomodatingPlayerFlip) //Add flip offset
                    {
                        this.transform.position += new Vector3(PlayerHatDatabase.SpecialCharacterFlipOffsets[hatOwner.characterIdentity], 0, 0);
                        hatIsAccomodatingPlayerFlip = true;
                    }
                    else if (!hatOwner.SpriteFlipped && hatIsAccomodatingPlayerFlip) //Remove flip offset
                    {
                        this.transform.position -= new Vector3(PlayerHatDatabase.SpecialCharacterFlipOffsets[hatOwner.characterIdentity], 0, 0);
                        hatIsAccomodatingPlayerFlip = false;
                    }
                }
            }
        }
        private void HandleVanish()
        {
            bool isVanished = base.sprite.renderer.enabled;
            bool shouldBeVanished = false;

            if (hatOwner.IsFalling)
                shouldBeVanished = true;

            if (hatOwnerAnimator.CurrentClip.name == "doorway" || hatOwnerAnimator.CurrentClip.name == "spinfall")
                shouldBeVanished = true;

            if ((PlayerHasAdditionalVanishOverride() || hatRollReaction == HatRollReaction.VANISH) && hatOwner.IsDodgeRolling)
                shouldBeVanished = true;

            if (hatOwner.IsSlidingOverSurface)
                shouldBeVanished = true;

            if (!isVanished && !shouldBeVanished)
                base.sprite.renderer.enabled = true;
            else if (isVanished && shouldBeVanished)
                base.sprite.renderer.enabled = false;
        }
        private bool PlayerHasAdditionalVanishOverride()
        {
            bool shouldActuallyVanish = false;
            if (hatOwner && hatOwner.HasPickupID(436)) shouldActuallyVanish = true;
            return shouldActuallyVanish;
        }
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
                        else if (hatDirectionality != HatDirectionality.TWOWAYVERTICAL && frontDiagonalUseLeftRight) { animToPlay = "hat_west"; }
                        break;
                    case HatDirection.SOUTHEAST:
                        if (hatDirectionality == HatDirectionality.EIGHTWAY) { animToPlay = "hat_southeast"; }
                        else if (hatDirectionality != HatDirectionality.TWOWAYVERTICAL && frontDiagonalUseLeftRight) { animToPlay = "hat_east"; }
                        break;
                    case HatDirection.NORTHWEST:
                        if (hatDirectionality == HatDirectionality.SIXWAY || hatDirectionality == HatDirectionality.EIGHTWAY) { animToPlay = "hat_northwest"; }
                        else if (hatDirectionality != HatDirectionality.TWOWAYHORIZONTAL && backDiagonalUseNorth) { animToPlay = "hat_north"; }
                        break;
                    case HatDirection.NORTHEAST:
                        if (hatDirectionality == HatDirectionality.SIXWAY || hatDirectionality == HatDirectionality.EIGHTWAY) { animToPlay = "hat_northeast"; }
                        else if (hatDirectionality != HatDirectionality.TWOWAYHORIZONTAL && backDiagonalUseNorth) { animToPlay = "hat_north"; }
                        break;
                    case HatDirection.NONE:
                        ETGModConsole.Log("ERROR: TRIED TO ROTATE HAT TO A NULL DIRECTION!");
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
                        case 180:
                            hatDir = HatDirection.WEST;
                            break;
                        case 135:
                            hatDir = HatDirection.NORTHWEST;
                            break;
                        case -45:
                            hatDir = HatDirection.SOUTHEAST;
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
                        case 180:
                            hatDir = HatDirection.WEST;
                            break;
                        case 135:
                            hatDir = HatDirection.NORTHWEST;
                            break;
                    }
                }
            }
            else Debug.LogError("Attempted to get hatOwner facing direction with a null hatOwner!");
            if (hatDir == HatDirection.NONE) hatDir = HatDirection.SOUTH;
            return hatDir;
        }
        public Vector2 GetHatPosition(PlayerController player)
        {
            Vector2 vec = new Vector2();
            if (attachLevel == HatAttachLevel.HEAD_TOP)
            {
                if (PlayerHatDatabase.CCOverideHeadOffset.ContainsKey(player.name))
                {
                    vec = (player.sprite.WorldCenter + PlayerHatDatabase.CCOverideHeadOffset[player.name]);
                }
                else if (PlayerHatDatabase.CharacterIDHeadOffset.ContainsKey(player.characterIdentity))
                {
                    vec = (player.sprite.WorldCenter + PlayerHatDatabase.CharacterIDHeadOffset[player.characterIdentity]);
                }
                else { vec = (player.sprite.WorldCenter + new Vector2(0, PlayerHatDatabase.defaultHeadLevelOffset)); }
            }
            else if (attachLevel == HatAttachLevel.EYE_LEVEL)
            {
                if (PlayerHatDatabase.CharacterNameEyeLevel.ContainsKey(player.name))
                {
                    vec = (player.sprite.WorldCenter + new Vector2(0, PlayerHatDatabase.CharacterNameEyeLevel[player.name]));
                }
                else { vec = (player.sprite.WorldCenter + new Vector2(0, PlayerHatDatabase.defaultEyeLevelOffset)); }
            }
            if (PlayerHatDatabase.SpecialCharacterFlipOffsets.ContainsKey(player.characterIdentity) && player.SpriteFlipped)
            {
                vec += new Vector2(PlayerHatDatabase.SpecialCharacterFlipOffsets[player.characterIdentity], 0);
            }
            vec += hatOffset;
            return vec;
        }
        public void StickHatToPlayer(PlayerController player)
        {
            if (hatOwner == null) hatOwner = player;
            Vector2 vec = GetHatPosition(player);
            base.transform.position = vec;
            base.transform.rotation = Quaternion.Euler(0, 0, 0);
            base.transform.parent = player.transform;
            player.sprite.AttachRenderer(gameObject.GetComponent<tk2dBaseSprite>());
            currentState = HatState.SITTING;
        }
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

                if (hatDepthType == HatDepthType.BehindWhenFacingBack)
                    hatSprite.HeightOffGround = num2 + num * 1;
                else
                    hatSprite.HeightOffGround = num2 + num * -1;
            }
            else
            {
                if (hatDepthType == HatDepthType.AlwaysInFront)
                    hatSprite.HeightOffGround = 0.6f;
                else
                    hatSprite.HeightOffGround = -0.6f;
            }
        }
        private IEnumerator FlipHatIENum()
        {
            RollLength = hatOwner.rollStats.GetModifiedTime(hatOwner);
            currentState = HatState.FLIPPING;
            yield return new WaitForSecondsRealtime(RollLength);
            currentState = HatState.SITTING;
            StickHatToPlayer(hatOwner);
        }
        private float RollLength = 0.65f;
        private void HandleFlip()
        {
            if (hatRollReaction == HatRollReaction.FLIP && !PlayerHasAdditionalVanishOverride())
            {
                if (hatOwnerAnimator == null) Debug.LogError("Attempted to flip a hat with a null hatOwnerAnimator!");
                else
                {
                    if (hatOwner.IsDodgeRolling && currentState != HatState.FLIPPING) StartCoroutine(FlipHatIENum());

                    if (currentState == HatState.FLIPPING)
                    {
                        if (!GameManager.Instance.IsPaused)
                        {
                            if (hatOwnerAnimator.CurrentClip == null) Debug.LogError("hatOwnerAnimator.CurrentClip is NULL!");
                            else
                            {
                                if (hatOwnerAnimator.CurrentClip == null || !hatOwnerAnimator.CurrentClip.name.StartsWith("dodge"))
                                {
                                    Debug.LogError("hatOwnerAnimator.CurrentClip is NULL! (or the current anim isnt a roll)");
                                }
                                else
                                {
                                    float CurrentRollTime = hatOwnerAnimator.ClipTimeSeconds;

                                    this.transform.RotateAround(this.sprite.WorldCenter, Vector3.forward, 13);

                                    if (CurrentRollTime < RollLength / 6)
                                        this.transform.position += new Vector3(0, flipHeightMultiplier * 0.3f, 0);
                                    else if (CurrentRollTime < RollLength / 5)
                                        this.transform.position += new Vector3(0, flipHeightMultiplier * 0.25f, 0);
                                    else if (CurrentRollTime < RollLength / 4)
                                        this.transform.position += new Vector3(0, flipHeightMultiplier * 0.15f, 0);
                                    else if (CurrentRollTime < RollLength / 3)
                                        this.transform.position += new Vector3(0, flipHeightMultiplier * 0.1f, 0);
                                    else if (CurrentRollTime < RollLength / 2)
                                        this.transform.position += new Vector3(0, flipHeightMultiplier * 0.01f, 0);
                                    else if (CurrentRollTime < RollLength * 0.6)
                                        this.transform.position -= new Vector3(0, flipHeightMultiplier * 0.01f, 0);
                                    else if (CurrentRollTime < RollLength * 0.7)
                                        this.transform.position -= new Vector3(0, flipHeightMultiplier * 0.15f, 0);
                                    else if (CurrentRollTime < RollLength * 0.8)
                                        this.transform.position -= new Vector3(0, flipHeightMultiplier * 0.25f, 0);
                                    else
                                        this.transform.position -= new Vector3(0, flipHeightMultiplier * 0.25f, 0);
                                }
                            }
                        }
                        else { StickHatToPlayer(hatOwner); }
                    }
                }
            }
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
        public enum HatDepthType
        {
            AlwaysInFront,
            AlwaysBehind,
            BehindWhenFacingBack,
            InFrontWhenFacingBack
        }
    }
}
