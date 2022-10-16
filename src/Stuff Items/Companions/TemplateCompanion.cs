

using System;
using System.Collections.Generic;
using Gungeon;
using ItemAPI;
using UnityEngine;
//using AnimationType = ItemAPI.EnemyBuilder.AnimationType;
using System.Collections;
using Dungeonator;
using System.Linq;
using Brave.BulletScript;
using Pathfinding;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace NevernamedsItems
{
    public enum ActorType
    {
        COMPANION,
        ENEMY,
        BOSS,
        MINIBOSS
    }
    public enum AnimationType
    {
        IDLE,
        WALK,
        HIT,
        FLIGHT,
        TALK,
        FIDGET,
        OTHER
    }
    public class AllAnimations
    {
        public WholeAnimationData idleAnimation;
        public WholeAnimationData walkAnimation;
        public WholeAnimationData hitAnimation;
        public WholeAnimationData flightAnimation;
        public WholeAnimationData talkAnimation;
        public WholeAnimationData fidgetAnimation;
        //Extra animations
        public WholeAnimationData deathAnimation;
        public WholeAnimationData pitfallAnimation;
        public WholeAnimationData spawnAnimation;
        public WholeAnimationData awakenAnimation;
        //Other
        public List<WholeAnimationData> otherAnimations;
    }
    public class WholeAnimationData
    {
        public List<DirectionalAnimationData> DirectionalAnimations;
        public DirectionalAnimation.DirectionType Directionality;
        public DirectionalAnimation.FlipType flipType;
        public string animName;
        public string animShortname;
    }
    public class DirectionalAnimationData
    {
        public List<AnimationFrameData> Frames;
        public tk2dSpriteAnimationClip.WrapMode wrap;
        public int fps;
        public string suffix;
    }
    public class AnimationFrameData
    {
        public string filePath;
        public int frameXOffset;
        public int frameYOffset;
        public string frameAudioEvent;
        public int hitboxXOffset;
        public int hitboxYOffset;
        public int hitboxWidth;
        public int hitboxHeight;
    }

    public static class ActorTemplateGenerator
    {
        public static Dictionary<string, Color> colourNameToDef = new Dictionary<string, Color>()
        {
            {"red", Color.red},
            {"orange",  new Color(255f / 255f, 144f / 255f, 41f / 255f)},
            {"yellow", Color.yellow},
            {"green", Color.green},
            {"blue", Color.blue},
            {"purple", new Color(171f / 255f, 22f / 255f, 240f / 255f)},
            {"white", Color.white},
            {"black", Color.black},
            {"pink", new Color(242f / 255f, 116f / 255f, 225f / 255f)},
            {"brown", new Color(74f / 255f, 22f / 255f, 5f / 255f)},
        };

        public static string GenerateActorTemplate(
            string TEMPLATEACTORNAME,
            string INSERTGUID,
            string MODPREFIX,
            ActorType ACTORTYPE,

            bool FLIGHTSTATE,
            AllAnimations Animations,
            float movementSpeed,
            float contactDamage,
            bool hasShadow,
            bool ignoreForRoomClear,
            bool killOnRoomClear,
            bool targetPlayersTrueEnemiesFalse,
            bool immuneToPits,
            float collisionKnockback,
            bool hasOutlines,
            bool canBeJammed,
            float actorWeight,
           //HEALTH VARIABLES
           float HEALTHMAX,
           bool INVULNERABLE,
           //BODY VARIABLES
           bool rigidBodyCollideWithOthers,
           bool rigidBodyCollideWithWalls,
           int RigidBodyOffsetX,
           int RigidBodyOffsetY,
           int RigidBodyWidth,
           int RigidBodyHeight,
           //ANIMATOR VARIABLES
           bool faceSouthWhenStopped,
           bool faceTargetWhenStopped,
           float hitReactionChance,
           //BOSSNESS
           bool DOBOSSINTRO,
           bool DOBOSSSPLASHSCREEN,
           string introAnimationName,
           string bossSplashScreenPath,
           string bossMusicEventName,
           string bossSplashScreenQuote,
           string bossSplashScreenSubtitle,
           bool verticalBossBar,
           string bossSplashScreenColour,
            //BEHAVIOUR
            bool useDefaultTargetBehaviour,
            bool useDefaultMovementBehaviour,
                bool stopWalkingWhenInRange,
                float desiredRange,
                bool lineOfSightRequired,
                bool returnToSpawnPointWithoutTarget,
                float spawnTetherRange,
                bool specificRange,
                float minRange,
                float maxRange,
            bool companionFollowsPlayer,
            //AMMONOMICON
            string AmmonomiconSprite,
            string AmmonomiconPageSprite,
            bool showInAmmonomicon,
            string AmmonomiconShortDesc,
            string AmmonomiconLongDesc,
            int positionInAmmonomicon
            )
        {
            List<string> WorkingListOfPaths = new List<string>();
            string frameAudioEvents = "";
            string frameHitboxOffsets = "";

            //Actor Variables
            string FLIGHTCODE = " ";
            if (FLIGHTSTATE) { FLIGHTCODE = "companion.aiActor.SetIsFlying(true, \"Flying Entity\");"; }

            string ActorNameNoSpaces = TEMPLATEACTORNAME;
            ActorNameNoSpaces = ActorNameNoSpaces.Replace(" ", "");
            string ActorNameUnderscored = TEMPLATEACTORNAME;
            ActorNameUnderscored = ActorNameUnderscored.Replace(" ", "_");
            //Body Variables
            //Handle Animations
            string IDLEINSERT = " ";
            string WALKINSERT = " ";
            string HITINSERT = " ";
            string FLIGHTINSERT = " ";
            string TALKINSERT = " ";
            string FIDGETINSERT = " ";
            string OTHERANIMINSERINSERT = " ";

            string ANIMATIONADDERS = "";
            if (Animations.idleAnimation != null)
            {
                IDLEINSERT = SetupAnimationSegment(Animations.idleAnimation);
                foreach (DirectionalAnimationData dirAn in Animations.idleAnimation.DirectionalAnimations)
                {
                    string spriteBuilderSegment = $"SpriteBuilder.AddAnimation(companion.spriteAnimator, {ActorNameNoSpaces}Collection, new List<int>" + "{";
                    List<int> indexes = new List<int>();
                    foreach (AnimationFrameData frame in dirAn.Frames)
                    {
                        WorkingListOfPaths.Add(frame.filePath);
                        indexes.Add(WorkingListOfPaths.Count - 1);
                        string animToTarget = Animations.idleAnimation.animShortname;
                        if (!string.IsNullOrEmpty(dirAn.suffix)) animToTarget += ("_" + dirAn.suffix);
                        if (!string.IsNullOrEmpty(frame.frameAudioEvent))
                        {
                            frameAudioEvents += $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].eventAudio = \"{frame.frameAudioEvent}\";\n" +
                            $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].triggerEvent = true;\n";
                        }
                        frameHitboxOffsets += $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].colliderVertices[0] = new Vector3({frame.frameXOffset / 16}f, {frame.frameYOffset / 16}f, 0f);\n" +
                        $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].colliderVertices[1] = new Vector3({frame.hitboxXOffset / 16}f, {frame.hitboxYOffset / 16}f, 0f);\n";
                    }
                    string indexList = ConvertIntListToString(indexes);
                    spriteBuilderSegment += indexList;
                    if (string.IsNullOrEmpty(dirAn.suffix))
                    {
                        spriteBuilderSegment += "}, " +
                       $"\"{Animations.idleAnimation.animShortname}\", " +
                       $"tk2dSpriteAnimationClip.WrapMode.{dirAn.wrap}).fps = {dirAn.fps};\n";
                    }
                    else
                    {
                        spriteBuilderSegment += "}, " +
                            $"\"{Animations.idleAnimation.animShortname}_{dirAn.suffix}\", " +
                            $"tk2dSpriteAnimationClip.WrapMode.{dirAn.wrap}).fps = {dirAn.fps};\n";
                    }
                    ANIMATIONADDERS += spriteBuilderSegment;
                }
            }
            if (Animations.walkAnimation != null)
            {
                WALKINSERT = SetupAnimationSegment(Animations.walkAnimation);
                foreach (DirectionalAnimationData dirAn in Animations.walkAnimation.DirectionalAnimations)
                {
                    string spriteBuilderSegment = $"SpriteBuilder.AddAnimation(companion.spriteAnimator, {ActorNameNoSpaces}Collection, new List<int>" + "{";
                    List<int> indexes = new List<int>();
                    foreach (AnimationFrameData frame in dirAn.Frames)
                    {
                        WorkingListOfPaths.Add(frame.filePath);
                        indexes.Add(WorkingListOfPaths.Count - 1);
                        string animToTarget = Animations.walkAnimation.animShortname;
                        if (!string.IsNullOrEmpty(dirAn.suffix)) animToTarget += ("_" + dirAn.suffix);
                        if (!string.IsNullOrEmpty(frame.frameAudioEvent))
                        {
                            frameAudioEvents += $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].eventAudio = \"{frame.frameAudioEvent}\";\n" +
                            $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].triggerEvent = true;\n";
                        }
                        frameHitboxOffsets += $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].colliderVertices[0] = new Vector3({frame.frameXOffset / 16}f, {frame.frameYOffset / 16}f, 0f);\n" +
                        $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].colliderVertices[1] = new Vector3({frame.hitboxXOffset / 16}f, {frame.hitboxYOffset / 16}f, 0f);\n";
                    }
                    string indexList = ConvertIntListToString(indexes);
                    spriteBuilderSegment += indexList;
                    if (string.IsNullOrEmpty(dirAn.suffix))
                    {
                        spriteBuilderSegment += "}, " +
                       $"\"{Animations.walkAnimation.animShortname}\", " +
                       $"tk2dSpriteAnimationClip.WrapMode.{dirAn.wrap}).fps = {dirAn.fps};\n";
                    }
                    else
                    {
                        spriteBuilderSegment += "}, " +
                            $"\"{Animations.walkAnimation.animShortname}_{dirAn.suffix}\", " +
                            $"tk2dSpriteAnimationClip.WrapMode.{dirAn.wrap}).fps = {dirAn.fps};\n";
                    }
                    ANIMATIONADDERS += spriteBuilderSegment;
                }
            }
            if (Animations.hitAnimation != null)
            {
                HITINSERT = SetupAnimationSegment(Animations.hitAnimation);
                foreach (DirectionalAnimationData dirAn in Animations.hitAnimation.DirectionalAnimations)
                {
                    string spriteBuilderSegment = $"SpriteBuilder.AddAnimation(companion.spriteAnimator, {ActorNameNoSpaces}Collection, new List<int>" + "{";
                    List<int> indexes = new List<int>();
                    foreach (AnimationFrameData frame in dirAn.Frames)
                    {
                        WorkingListOfPaths.Add(frame.filePath);
                        indexes.Add(WorkingListOfPaths.Count - 1);
                        string animToTarget = Animations.hitAnimation.animShortname;
                        if (!string.IsNullOrEmpty(dirAn.suffix)) animToTarget += ("_" + dirAn.suffix);
                        if (!string.IsNullOrEmpty(frame.frameAudioEvent))
                        {
                            frameAudioEvents += $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].eventAudio = \"{frame.frameAudioEvent}\";\n" +
                            $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].triggerEvent = true;\n";
                        }
                        frameHitboxOffsets += $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].colliderVertices[0] = new Vector3({frame.frameXOffset / 16}f, {frame.frameYOffset / 16}f, 0f);\n" +
                        $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].colliderVertices[1] = new Vector3({frame.hitboxXOffset / 16}f, {frame.hitboxYOffset / 16}f, 0f);\n";
                    }
                    string indexList = ConvertIntListToString(indexes);
                    spriteBuilderSegment += indexList;
                    if (string.IsNullOrEmpty(dirAn.suffix))
                    {
                        spriteBuilderSegment += "}, " +
                       $"\"{Animations.hitAnimation.animShortname}\", " +
                       $"tk2dSpriteAnimationClip.WrapMode.{dirAn.wrap}).fps = {dirAn.fps};\n";
                    }
                    else
                    {
                        spriteBuilderSegment += "}, " +
                            $"\"{Animations.hitAnimation.animShortname}_{dirAn.suffix}\", " +
                            $"tk2dSpriteAnimationClip.WrapMode.{dirAn.wrap}).fps = {dirAn.fps};\n";
                    }
                    ANIMATIONADDERS += spriteBuilderSegment;
                }
            }
            if (Animations.flightAnimation != null)
            {
                FLIGHTINSERT = SetupAnimationSegment(Animations.flightAnimation);
                foreach (DirectionalAnimationData dirAn in Animations.flightAnimation.DirectionalAnimations)
                {
                    string spriteBuilderSegment = $"SpriteBuilder.AddAnimation(companion.spriteAnimator, {ActorNameNoSpaces}Collection, new List<int>" + "{";
                    List<int> indexes = new List<int>();
                    foreach (AnimationFrameData frame in dirAn.Frames)
                    {
                        WorkingListOfPaths.Add(frame.filePath);
                        indexes.Add(WorkingListOfPaths.Count - 1);
                        string animToTarget = Animations.flightAnimation.animShortname;
                        if (!string.IsNullOrEmpty(dirAn.suffix)) animToTarget += ("_" + dirAn.suffix);
                        if (!string.IsNullOrEmpty(frame.frameAudioEvent))
                        {
                            frameAudioEvents += $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].eventAudio = \"{frame.frameAudioEvent}\";\n" +
                            $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].triggerEvent = true;\n";
                        }
                        frameHitboxOffsets += $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].colliderVertices[0] = new Vector3({frame.frameXOffset / 16}f, {frame.frameYOffset / 16}f, 0f);\n" +
                        $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].colliderVertices[1] = new Vector3({frame.hitboxXOffset / 16}f, {frame.hitboxYOffset / 16}f, 0f);\n";
                    }
                    string indexList = ConvertIntListToString(indexes);
                    spriteBuilderSegment += indexList;
                    if (string.IsNullOrEmpty(dirAn.suffix))
                    {
                        spriteBuilderSegment += "}, " +
                       $"\"{Animations.flightAnimation.animShortname}\", " +
                       $"tk2dSpriteAnimationClip.WrapMode.{dirAn.wrap}).fps = {dirAn.fps};\n";
                    }
                    else
                    {
                        spriteBuilderSegment += "}, " +
                            $"\"{Animations.flightAnimation.animShortname}_{dirAn.suffix}\", " +
                            $"tk2dSpriteAnimationClip.WrapMode.{dirAn.wrap}).fps = {dirAn.fps};\n";
                    }
                    ANIMATIONADDERS += spriteBuilderSegment;
                }
            }
            if (Animations.talkAnimation != null)
            {
                TALKINSERT = SetupAnimationSegment(Animations.talkAnimation);
                foreach (DirectionalAnimationData dirAn in Animations.talkAnimation.DirectionalAnimations)
                {
                    string spriteBuilderSegment = $"SpriteBuilder.AddAnimation(companion.spriteAnimator, {ActorNameNoSpaces}Collection, new List<int>" + "{";
                    List<int> indexes = new List<int>();
                    foreach (AnimationFrameData frame in dirAn.Frames)
                    {
                        WorkingListOfPaths.Add(frame.filePath);
                        indexes.Add(WorkingListOfPaths.Count - 1);
                            string animToTarget = Animations.talkAnimation.animShortname;
                            if (!string.IsNullOrEmpty(dirAn.suffix)) animToTarget += ("_" + dirAn.suffix);
                        if (!string.IsNullOrEmpty(frame.frameAudioEvent))
                        {
                            frameAudioEvents += $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].eventAudio = \"{frame.frameAudioEvent}\";\n" +
                            $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].triggerEvent = true;\n";
                        }
                        frameHitboxOffsets += $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].colliderVertices[0] = new Vector3({frame.frameXOffset / 16}f, {frame.frameYOffset / 16}f, 0f);\n" +
                        $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].colliderVertices[1] = new Vector3({frame.hitboxXOffset / 16}f, {frame.hitboxYOffset / 16}f, 0f);\n";
                    }
                    string indexList = ConvertIntListToString(indexes);
                    spriteBuilderSegment += indexList;
                    if (string.IsNullOrEmpty(dirAn.suffix))
                    {
                        spriteBuilderSegment += "}, " +
                       $"\"{Animations.talkAnimation.animShortname}\", " +
                       $"tk2dSpriteAnimationClip.WrapMode.{dirAn.wrap}).fps = {dirAn.fps}\n;";
                    }
                    else
                    {
                        spriteBuilderSegment += "}, " +
                            $"\"{Animations.talkAnimation.animShortname}_{dirAn.suffix}\", " +
                            $"tk2dSpriteAnimationClip.WrapMode.{dirAn.wrap}).fps = {dirAn.fps}\n;";
                    }
                    ANIMATIONADDERS += spriteBuilderSegment;
                }
            }
            if (Animations.fidgetAnimation != null)
            {
                FIDGETINSERT = SetupAnimationSegment(Animations.fidgetAnimation, true);
                foreach (DirectionalAnimationData dirAn in Animations.fidgetAnimation.DirectionalAnimations)
                {
                    string spriteBuilderSegment = $"SpriteBuilder.AddAnimation(companion.spriteAnimator, {ActorNameNoSpaces}Collection, new List<int>" + "{";
                    List<int> indexes = new List<int>();
                    foreach (AnimationFrameData frame in dirAn.Frames)
                    {
                        WorkingListOfPaths.Add(frame.filePath);
                        indexes.Add(WorkingListOfPaths.Count - 1);
                            string animToTarget = Animations.fidgetAnimation.animShortname;
                            if (!string.IsNullOrEmpty(dirAn.suffix)) animToTarget += ("_" + dirAn.suffix);
                        if (!string.IsNullOrEmpty(frame.frameAudioEvent))
                        {
                            frameAudioEvents += $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].eventAudio = \"{frame.frameAudioEvent}\";\n" +
                            $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].triggerEvent = true;\n";
                        }
                        frameHitboxOffsets += $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].colliderVertices[0] = new Vector3({frame.frameXOffset / 16}f, {frame.frameYOffset / 16}f, 0f);\n" +
                        $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].colliderVertices[1] = new Vector3({frame.hitboxXOffset / 16}f, {frame.hitboxYOffset / 16}f, 0f);\n";
                    }
                    string indexList = ConvertIntListToString(indexes);
                    spriteBuilderSegment += indexList;
                    if (string.IsNullOrEmpty(dirAn.suffix))
                    {
                        spriteBuilderSegment += "}, " +
                       $"\"{Animations.fidgetAnimation.animShortname}\", " +
                       $"tk2dSpriteAnimationClip.WrapMode.{dirAn.wrap}).fps = {dirAn.fps}\n;";
                    }
                    else
                    {
                        spriteBuilderSegment += "}, " +
                            $"\"{Animations.fidgetAnimation.animShortname}_{dirAn.suffix}\", " +
                            $"tk2dSpriteAnimationClip.WrapMode.{dirAn.wrap}).fps = {dirAn.fps}\n;";
                    }
                    ANIMATIONADDERS += spriteBuilderSegment;
                }
            }
            //Add weird animations to Otheranimations
            if (Animations.deathAnimation != null) Animations.otherAnimations.Add(Animations.deathAnimation);
            if (Animations.pitfallAnimation != null) Animations.otherAnimations.Add(Animations.pitfallAnimation);
            if (Animations.spawnAnimation != null) Animations.otherAnimations.Add(Animations.spawnAnimation);
            if (Animations.awakenAnimation != null) Animations.otherAnimations.Add(Animations.awakenAnimation);

            if (Animations.otherAnimations != null && Animations.otherAnimations.Count > 0)
            {
                OTHERANIMINSERINSERT = SetupOtherAnimationSegments(Animations.otherAnimations);
                foreach (WholeAnimationData animation in Animations.otherAnimations)
                {
                    foreach (DirectionalAnimationData dirAn in animation.DirectionalAnimations)
                    {
                        string spriteBuilderSegment = $"SpriteBuilder.AddAnimation(companion.spriteAnimator, {ActorNameNoSpaces}Collection, new List<int>" + "{";
                        List<int> indexes = new List<int>();
                        foreach (AnimationFrameData frame in dirAn.Frames)
                        {
                            WorkingListOfPaths.Add(frame.filePath);
                            indexes.Add(WorkingListOfPaths.Count - 1);
                                string animToTarget = animation.animShortname;
                                if (!string.IsNullOrEmpty(dirAn.suffix)) animToTarget += ("_" + dirAn.suffix);
                            if (!string.IsNullOrEmpty(frame.frameAudioEvent))
                            {
                                frameAudioEvents += $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].eventAudio = \"{frame.frameAudioEvent}\";\n" +
                                $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].triggerEvent = true;\n";
                            }
                            frameHitboxOffsets += $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].colliderVertices[0] = new Vector3({frame.frameXOffset / 16}f, {frame.frameYOffset / 16}f, 0f);\n" +
                        $"companion.GetComponent<tk2dSpriteAnimator>().GetClipByName(\"{animToTarget}\").frames[{WorkingListOfPaths.Count - 1}].colliderVertices[1] = new Vector3({frame.hitboxXOffset / 16}f, {frame.hitboxYOffset / 16}f, 0f);\n";
                        }
                        string indexList = ConvertIntListToString(indexes);
                        spriteBuilderSegment += indexList;
                        if (string.IsNullOrEmpty(dirAn.suffix))
                        {
                            spriteBuilderSegment += "}, " +
                           $"\"{animation.animShortname}\", " +
                           $"tk2dSpriteAnimationClip.WrapMode.{dirAn.wrap}).fps = {dirAn.fps};\n";
                        }
                        else
                        {
                            spriteBuilderSegment += "}, " +
                                $"\"{animation.animShortname}_{dirAn.suffix}\", " +
                                $"tk2dSpriteAnimationClip.WrapMode.{dirAn.wrap}).fps = {dirAn.fps};\n";
                        }
                        ANIMATIONADDERS += spriteBuilderSegment;
                    }
                }
            }

            //BEHAVIOURS
            string TARGETBEHAVIOURS = "";
            string MOVEMENTBEHAVIOURS = "";
            TARGETBEHAVIOURS = "bs.TargetBehaviors = new List<TargetBehaviorBase>{ //Add your target behaviours here!\n";
            if (useDefaultTargetBehaviour) TARGETBEHAVIOURS += "new TargetPlayerBehavior{" +
                    "Radius = 1000," +
                    "LineOfSight = false," +
                    "ObjectPermanence = true," +
                    "SearchInterval = 0.25f," +
                    "PauseOnTargetSwitch = false," +
                    "PauseTime = 0.25f," +
                    "},\n";
            TARGETBEHAVIOURS += "};";

            MOVEMENTBEHAVIOURS = "bs.MovementBehaviors = new List<MovementBehaviorBase>() { //Add your movement behaviours here!\n";
            if (useDefaultMovementBehaviour) MOVEMENTBEHAVIOURS += "new SeekTargetBehavior() {" +
                   $" StopWhenInRange = {(stopWalkingWhenInRange).ToString().ToLower()}," +
                   $"CustomRange = {desiredRange}," +
                   $"LineOfSight = {lineOfSightRequired.ToString().ToLower()}," +
                   $"ReturnToSpawn = {returnToSpawnPointWithoutTarget.ToString().ToLower()}," +
                   $"SpawnTetherDistance = {spawnTetherRange}," +
                   "PathInterval = 0.25f," +
                   $"SpecifyRange = {(specificRange).ToString().ToLower()}," +
                   $"MinActiveRange = {minRange}," +
                   $"MaxActiveRange = {maxRange}" +
                    "},\n";
            if (ACTORTYPE == ActorType.COMPANION && companionFollowsPlayer) MOVEMENTBEHAVIOURS += "new CompanionFollowPlayerBehavior{" +
                "IdleAnimations = new string[]{\"idle\"}," +
                "}\n";
            MOVEMENTBEHAVIOURS += "};";

            //COMPANION
            string CompanionInsert = "";
            if (ACTORTYPE == ActorType.COMPANION)
            {
                CompanionInsert = "var companionController = companion.AddComponent<CompanionController>(); //Replace with a component of your choice that extends CompanionController if you have one, for special behaviours.\n" +
                "companionController.companionID = CompanionController.CompanionIdentifier.NONE;";
            }
            //AMMONOMICON
            string AMMONOMICONINSERT = "";
            if (!(ACTORTYPE == ActorType.COMPANION || !showInAmmonomicon))
            {

                string stringKeyNameVer = TEMPLATEACTORNAME;
                stringKeyNameVer = stringKeyNameVer.Replace(" ", "_");

                AMMONOMICONINSERT = "SpriteBuilder.AddSpriteToCollection(\"AmmonomiconSprite\", AdvEnemyBuilder.ammonomiconCollection);\n" +

                "companion.encounterTrackable = companion.gameObject.AddComponent<EncounterTrackable>();\n" +
                "companion.encounterTrackable.journalData = new JournalEntry();\n" +
               $"companion.encounterTrackable.EncounterGuid = \"{MODPREFIX}:{stringKeyNameVer.ToLower()}\".ToLower();\n" +
                "companion.encounterTrackable.prerequisites = new DungeonPrerequisite[0];\n" +
                "companion.encounterTrackable.journalData.SuppressKnownState = false;\n" +
                "companion.encounterTrackable.journalData.IsEnemy = true;\n" +
                "companion.encounterTrackable.journalData.SuppressInAmmonomicon = false;\n" +
                "companion.encounterTrackable.ProxyEncounterGuid = \"\";\n" +
               $"companion.encounterTrackable.journalData.AmmonomiconSprite = \"{AmmonomiconSprite}\";\n" +
               $"companion.encounterTrackable.journalData.enemyPortraitSprite = ItemAPI.ResourceExtractor.GetTextureFromResource(\"{AmmonomiconPageSprite}\");\n" +

               $" AdvEnemyBuilder.Strings.Enemies.Set(\"#{stringKeyNameVer.ToUpper()}\", \"{TEMPLATEACTORNAME}\");\n" +
               $"AdvEnemyBuilder.Strings.Enemies.Set(\"#{stringKeyNameVer.ToUpper()}_SHORTDESC\", \"{AmmonomiconShortDesc}\");\n" +
               $"AdvEnemyBuilder.Strings.Enemies.Set(\"#{stringKeyNameVer.ToUpper()}_LONGDESC\", \"{AmmonomiconLongDesc}\");\n" +

               $"companion.encounterTrackable.journalData.PrimaryDisplayName = \"#{stringKeyNameVer.ToUpper()}\";\n" +
               $"companion.encounterTrackable.journalData.NotificationPanelDescription = \"#{stringKeyNameVer.ToUpper()}_SHORTDESC\";\n" +
               $"companion.encounterTrackable.journalData.AmmonomiconFullEntry = \"#{stringKeyNameVer.ToUpper()}_LONGDESC\";\n" +
               $"AdvEnemyBuilder.AddEnemyToDatabase(companion.gameObject, \"{MODPREFIX}:{stringKeyNameVer}\".ToLower());\n" +
               $" EnemyDatabase.GetEntry(\"{MODPREFIX}:{stringKeyNameVer.ToLower()}\").ForcedPositionInAmmonomicon = {positionInAmmonomicon};\n" +
               $" EnemyDatabase.GetEntry(\"{MODPREFIX}:{stringKeyNameVer.ToLower()}\").isInBossTab = {(ACTORTYPE == ActorType.BOSS).ToString().ToLower()};\n" +
               $"EnemyDatabase.GetEntry(\"{MODPREFIX}:{stringKeyNameVer.ToLower()}\").isNormalEnemy = true;\n";
            }
            string BOSSINTROINSERT = "";
            string BossIntroTextureMaker = "";
            if (ACTORTYPE == ActorType.BOSS || ACTORTYPE == ActorType.MINIBOSS)
            {
                if (DOBOSSINTRO)
                {
                    BOSSINTROINSERT = "GenericIntroDoer miniBossIntroDoer = companion.AddComponent<GenericIntroDoer>(); \n" +
                    "miniBossIntroDoer.triggerType = GenericIntroDoer.TriggerType.PlayerEnteredRoom; \n" +
                    "miniBossIntroDoer.initialDelay = 0.15f; \n" +
                    "miniBossIntroDoer.cameraMoveSpeed = 14; \n" +
                    "miniBossIntroDoer.specifyIntroAiAnimator = null;\n" +
                   $"miniBossIntroDoer.BossMusicEvent = \"{bossMusicEventName}\";\n" +
                   $"miniBossIntroDoer.PreventBossMusic = {string.IsNullOrEmpty(bossMusicEventName)};\n" +
                    "miniBossIntroDoer.InvisibleBeforeIntroAnim = false;\n" +
                    "miniBossIntroDoer.preIntroAnim = string.Empty;\n" +
                    "miniBossIntroDoer.preIntroDirectionalAnim = string.Empty;\n" +
                   $"miniBossIntroDoer.introAnim = \"{introAnimationName}\";\n" +
                    "miniBossIntroDoer.introDirectionalAnim = string.Empty;\n" +
                    "miniBossIntroDoer.continueAnimDuringOutro = false;\n" +
                    "miniBossIntroDoer.cameraFocus = null;\n" +
                    "miniBossIntroDoer.roomPositionCameraFocus = Vector2.zero;\n" +
                    "miniBossIntroDoer.restrictPlayerMotionToRoom = false;\n" +
                    "miniBossIntroDoer.fusebombLock = false;\n" +
                    "miniBossIntroDoer.AdditionalHeightOffset = 0;\n" +
                   $"AdvEnemyBuilder.Strings.Enemies.Set(\"#{TEMPLATEACTORNAME.ToUpper()}_NAME\", \"{TEMPLATEACTORNAME.ToUpper()}\");\n" +
                   $"AdvEnemyBuilder.Strings.Enemies.Set(\"#{TEMPLATEACTORNAME.ToUpper()}_BOSSSUBTITLE\", \"{bossSplashScreenSubtitle}\");\n" +
                   $"AdvEnemyBuilder.Strings.Enemies.Set(\"#{TEMPLATEACTORNAME.ToUpper()}_BOSSQUOTE\", \"{bossSplashScreenQuote}\");\n" +

                    "miniBossIntroDoer.portraitSlideSettings = new PortraitSlideSettings(){\n" +
                       $"bossNameString = \"#{TEMPLATEACTORNAME.ToUpper()}_NAME\",\n" +
                       $"bossSubtitleString = \"#{TEMPLATEACTORNAME.ToUpper()}_BOSSSUBTITLE\",\n" +
                       $"bossQuoteString = \"#{TEMPLATEACTORNAME.ToUpper()}_BOSSQUOTE\",\n" +
                        "bossSpritePxOffset = IntVector2.Zero,\n" +
                        "topLeftTextPxOffset = IntVector2.Zero,\n" +
                        "bottomRightTextPxOffset = IntVector2.Zero,\n" +
                        "bgColor = Color.blue};\n";


                    if (DOBOSSSPLASHSCREEN && string.IsNullOrEmpty(bossSplashScreenPath))
                    {
                        BossIntroTextureMaker = $"private static Texture2D BossCardTexture = ItemAPI.ResourceExtractor.GetTextureFromResource(\"{bossSplashScreenPath}\");";
                        BOSSINTROINSERT += "miniBossIntroDoer.portraitSlideSettings.bossArtSprite = BossCardTexture;\n" +
                        "miniBossIntroDoer.SkipBossCard = false;\n";
                    }
                    else
                    {
                        BOSSINTROINSERT += "miniBossIntroDoer.SkipBossCard = true;\n";
                    }
                }

                if (ACTORTYPE == ActorType.BOSS)
                {
                    if (verticalBossBar) BOSSINTROINSERT += "companion.aiActor.healthHaver.bossHealthBar = HealthHaver.BossBarType.VerticalBar;\n";
                    else BOSSINTROINSERT += "companion.aiActor.healthHaver.bossHealthBar = HealthHaver.BossBarType.MainBar;\n";
                }
                else if (ACTORTYPE == ActorType.MINIBOSS)
                {
                    BOSSINTROINSERT += "companion.aiActor.healthHaver.bossHealthBar = HealthHaver.BossBarType.SubbossBar;\n";
                }
            }

            //SETUP SPRITE OFFSETS
            string SPRITEOFFSETINSERT = "tk2dSpriteAnimator EnemySpriteAnimator = companion.GetComponent<tk2dSpriteAnimator>()\n";
            #region SpriteOffsetCalculator
            if (Animations.idleAnimation != null) SPRITEOFFSETINSERT += AnimationOffsetMakerMaker(Animations.idleAnimation);
            if (Animations.walkAnimation != null) SPRITEOFFSETINSERT += AnimationOffsetMakerMaker(Animations.walkAnimation);
            if (Animations.hitAnimation != null) SPRITEOFFSETINSERT += AnimationOffsetMakerMaker(Animations.hitAnimation);
            if (Animations.flightAnimation != null) SPRITEOFFSETINSERT += AnimationOffsetMakerMaker(Animations.flightAnimation);
            if (Animations.talkAnimation != null) SPRITEOFFSETINSERT += AnimationOffsetMakerMaker(Animations.talkAnimation);
            if (Animations.fidgetAnimation != null) SPRITEOFFSETINSERT += AnimationOffsetMakerMaker(Animations.fidgetAnimation);
            if (Animations.otherAnimations != null && Animations.otherAnimations.Count > 0)
            {
                foreach (WholeAnimationData anim in Animations.otherAnimations) SPRITEOFFSETINSERT += AnimationOffsetMakerMaker(anim);
            }
            #endregion

            string KillOnRoomClear = "";
            if (killOnRoomClear) KillOnRoomClear = "companion.gameObject.AddComponent<KillOnRoomClear>();\n";

            //Sprite list builder
            string SpriteListInsert = "";
            if (WorkingListOfPaths != null && WorkingListOfPaths.Count > 0)
            {
                foreach (string str in WorkingListOfPaths)
                {
                    SpriteListInsert += ("\"" + str + "\",\n");
                }
            }

            //FINAL SETUP

            string nameSetter = $"AdvEnemyBuilder.Strings.Enemies.Set(\"#{ActorNameUnderscored.ToUpper()}_NAME_SMALL\", \"{TEMPLATEACTORNAME}\");\n" +
                $"companion.aiActor.OverrideDisplayName = \"#{ActorNameUnderscored.ToUpper()}_NAME_SMALL\";";

            string EnemySetupCode = $"public class {ActorNameNoSpaces}Class : AIActor\n" +
                "{\npublic static GameObject prefab;\n" +
               $"public static readonly string guid = \"{INSERTGUID}\";\n" +
               $"private static tk2dSpriteCollectionData {ActorNameNoSpaces}Collection;\n" +
               $"public static GameObject shootpoint;\n{BossIntroTextureMaker}" +
                "public static void Init()\n{\n" +
               $"{ActorNameNoSpaces}Class.BuildPrefab();\n" + "}\n" +

                "public static void BuildPrefab()\n{\n" +
               $"if (!(prefab != null || AdvEnemyBuilder.Dictionary.ContainsKey(guid)))\n" + "{\n" +
               $"prefab = AdvEnemyBuilder.BuildPrefab(\"{TEMPLATEACTORNAME}\", guid, spritePaths[0], new IntVector2({RigidBodyOffsetX}, {RigidBodyOffsetY}), new IntVector2({RigidBodyWidth}, {RigidBodyHeight}), false);\n" +
                "var companion = prefab.AddComponent<EnemyBehavior>();\n" +
                "//Actor Variables\n" +
               $"companion.aiActor.MovementSpeed = {movementSpeed}f;\n" +
               $"companion.aiActor.CollisionDamage = {contactDamage}f;\n" +
               $"companion.aiActor.HasShadow = {hasShadow.ToString().ToLower()};\n" +
               $"companion.aiActor.IgnoreForRoomClear = {ignoreForRoomClear.ToString().ToLower()};\n" + KillOnRoomClear +
               $"companion.aiActor.CanTargetPlayers = {targetPlayersTrueEnemiesFalse.ToString().ToLower()};\n" +
               $"companion.aiActor.CanTargetEnemies = {(!targetPlayersTrueEnemiesFalse).ToString().ToLower()};\n" +
               $"companion.aiActor.PreventFallingInPitsEver = {immuneToPits.ToString().ToLower()};\n" +
               $"companion.aiActor.CollisionKnockbackStrength = {collisionKnockback}f;\n" +
               $"companion.aiActor.procedurallyOutlined = {hasOutlines.ToString().ToLower()};\n" +
               $"companion.aiActor.PreventBlackPhantom = {(!canBeJammed).ToString().ToLower()};\n" +
                "//Body Variables\n" +
               $"companion.aiActor.specRigidbody.CollideWithOthers = {rigidBodyCollideWithOthers.ToString().ToLower()};\n" +
               $"companion.aiActor.specRigidbody.CollideWithTileMap = {rigidBodyCollideWithWalls.ToString().ToLower()};\n" +

               $"{CompanionInsert}\n" +

                "//Health Variables\n" +
               $"companion.aiActor.healthHaver.PreventAllDamage = {INVULNERABLE.ToString().ToLower()};\n" +
               $"companion.aiActor.healthHaver.SetHealthMaximum({HEALTHMAX}f, null, false);\n" +
               $"companion.aiActor.healthHaver.ForceSetCurrentHealth({HEALTHMAX}f);\n" +

                "//Other Variables\n" +
               $"companion.aiActor.knockbackDoer.weight = {actorWeight}f;\n" +

                "//AnimatorVariables\n" +
               $"companion.aiAnimator.HitReactChance = {hitReactionChance}f;\n" +
               $"companion.aiAnimator.faceSouthWhenStopped = {faceSouthWhenStopped.ToString().ToLower()};\n" +
               $"companion.aiAnimator.faceTargetWhenStopped = {faceTargetWhenStopped.ToString().ToLower()};\n" +

               $"{FLIGHTCODE}\n" +
               $"{nameSetter}\n" +

                "companion.aiActor.specRigidbody.PixelColliders.Clear();\n" +
                "companion.aiActor.gameObject.AddComponent<tk2dSpriteAttachPoint>();\n" +
                "companion.aiActor.gameObject.AddComponent<ObjectVisibilityManager>();\n" +
                "companion.aiActor.specRigidbody.PixelColliders.Add(new PixelCollider{\n" +
                    "ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Tk2dPolygon,\n" +
                   " CollisionLayer = CollisionLayer.EnemyCollider,\n" +
                   " IsTrigger = false,\n" +
                    "BagleUseFirstFrameOnly = false,\n" +
                   " SpecifyBagelFrame = string.Empty,\n" +
                    "BagelColliderNumber = 0,\n" +
                   $"ManualOffsetX = {RigidBodyOffsetX},\n" +
                   $"ManualOffsetY = {RigidBodyOffsetY},\n" +
                   $"ManualWidth = {RigidBodyWidth},\n" +
                   $"ManualHeight = {RigidBodyHeight},\n" +
                    "ManualDiameter = 0,\n" +
                    "ManualLeftX = 0,\n" +
                    "ManualLeftY = 0,\n" +
                    "ManualRightX = 0,\n" +
                    "ManualRightY = 0\n" + "});\n" +
                "companion.aiActor.specRigidbody.PixelColliders.Add(new PixelCollider{\n" +
                    "ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Tk2dPolygon,\n" +
                    "CollisionLayer = CollisionLayer.EnemyHitBox,\n" +
                    "IsTrigger = false,\n" +
                    "BagleUseFirstFrameOnly = false,\n" +
                    "SpecifyBagelFrame = string.Empty,\n" +
                    "BagelColliderNumber = 0,\n" +
                   $"ManualOffsetX = {RigidBodyOffsetX},\n" +
                   $"ManualOffsetY = {RigidBodyOffsetY},\n" +
                   $"ManualWidth = {RigidBodyWidth},\n" +
                   $"ManualHeight = {RigidBodyHeight},\n" +
                    "ManualDiameter = 0,\n" +
                    "ManualLeftX = 0,\n" +
                    "ManualLeftY = 0,\n" +
                    "ManualRightX = 0,\n" +
                    "ManualRightY = 0,\n });\n" +

                "AIAnimator aiAnimator = companion.aiAnimator;\n" +
                $"{IDLEINSERT}\n" +
                $"{WALKINSERT}\n" +
                $"{FLIGHTINSERT}\n" +
                $"{HITINSERT}\n" +
                $"{TALKINSERT}\n" +
                $"{FIDGETINSERT}\n" +
                $"{OTHERANIMINSERINSERT}\n" +

                $"if ({ActorNameNoSpaces}Collection == null)\n" + "{\n" +
                    $"{ActorNameNoSpaces}Collection = SpriteBuilder.ConstructCollection(prefab, \"{ActorNameNoSpaces}Collection\");\n" +
                    $"UnityEngine.Object.DontDestroyOnLoad({ActorNameNoSpaces}Collection);\n" +
                    "for (int i = 0; i < spritePaths.Length; i++)\n" + "{\n" +
                    $"SpriteBuilder.AddSpriteToCollection(spritePaths[i], {ActorNameNoSpaces}Collection);\n" + "}\n" +
                    ANIMATIONADDERS + "\n}\n" +

                "var bs = prefab.GetComponent<BehaviorSpeculator>();\n" +
            "prefab.GetComponent<ObjectVisibilityManager>();\n" +
            "BehaviorSpeculator behaviorSpeculator = EnemyDatabase.GetOrLoadByGuid(\"01972dee89fc4404a5c408d50007dad5\").behaviorSpeculator;\n" +
            "bs.OverrideBehaviors = behaviorSpeculator.OverrideBehaviors;\n" +
            "bs.OtherBehaviors = behaviorSpeculator.OtherBehaviors;\n\n" +

            "//ATTACK BEHAVIOUR SETUP (Must be done BY HAND)\n" +
            "shootpoint = new GameObject(\"fuck\");\n" +
            "shootpoint.transform.parent = companion.transform;\n" +
            "shootpoint.transform.position = (companion.sprite.WorldCenter + new Vector2(0, 0));\n" +
            "GameObject m_CachedGunAttachPoint = companion.transform.Find(\"fuck\").gameObject;\n" +

            $"{TARGETBEHAVIOURS}\n\n" +
            $"{MOVEMENTBEHAVIOURS}\n\n" +
            " bs.AttackBehaviors = new List<AttackBehaviorBase>()\n{\n//Attack behaviours must be added here MANUALLY\n};\n\n" +

            "bs.InstantFirstTick = behaviorSpeculator.InstantFirstTick;\n" +
            "bs.TickInterval = behaviorSpeculator.TickInterval;\n" +
            "bs.StartingFacingDirection = behaviorSpeculator.StartingFacingDirection;\n" +
            "bs.PostAwakenDelay = behaviorSpeculator.PostAwakenDelay;\n" +
            "bs.RemoveDelayOnReinforce = behaviorSpeculator.RemoveDelayOnReinforce;\n" +
            "bs.OverrideStartingFacingDirection = behaviorSpeculator.OverrideStartingFacingDirection;\n" +
            "bs.SkipTimingDifferentiator = behaviorSpeculator.SkipTimingDifferentiator;\n\n" +

           $"{SPRITEOFFSETINSERT}\n\n" +
           $"{frameAudioEvents}\n\n" +
           $"#region HitboxOffsetters\n{frameHitboxOffsets}#endregion\n\n" +

            "if (companion.GetComponent<EncounterTrackable>() != null) {\n" +
            "UnityEngine.Object.Destroy(companion.GetComponent<EncounterTrackable>());}\n" +

            $"Game.Enemies.Add(\"{MODPREFIX}:{ActorNameUnderscored}\".ToLower(), companion.aiActor);\n" +
            $"{AMMONOMICONINSERT}\n" + $"{BOSSINTROINSERT}\n" + "}}\n" +
             "private static string[] spritePaths = new string[]{\n" +
            $"{SpriteListInsert}\n" + " };}\n";

            return EnemySetupCode;
        }
        public static string AnimationOffsetMakerMaker(WholeAnimationData animation)
        {
            string OffsetCodes = "";
            foreach (DirectionalAnimationData dirAn in animation.DirectionalAnimations)
            {
                bool hasFoundFrameOffsetsForThisAnimation = false;
                string DirectionalAnimName = animation.animName;
                if (string.IsNullOrEmpty(dirAn.suffix)) DirectionalAnimName += $"_{dirAn.suffix}";
                int iterator = 0;
                foreach (AnimationFrameData frame in dirAn.Frames)
                {
                    if (frame.frameXOffset != 0 || frame.frameYOffset != 0)
                    {
                        if (!hasFoundFrameOffsetsForThisAnimation)
                        {
                            OffsetCodes += $"tk2dSpriteAnimationClip {DirectionalAnimName}AnimForOffset = EnemySpriteAnimator.GetClipByName({DirectionalAnimName});\n";
                        }
                        OffsetCodes += $"{DirectionalAnimName}AnimForOffset.frames[{iterator}].spriteCollection.spriteDefinitions[{DirectionalAnimName}AnimForOffset.frames[{iterator}].spriteId].MakeOffset(new Vector2({frame.frameXOffset}, {frame.frameYOffset}));\n";
                    }
                    iterator++;
                }
            }
            return OffsetCodes;
        }
        public static string ConvertIntListToString(List<int> ints)
        {
            string result = "";
            foreach (int i in ints)
            {
                result += (i.ToString() + ",");
            }
            return result;
        }
        public static string CompileListBetweenValues(int start, int end)
        {
            string list = "";
            for (int i = start; i < end; i++)
            {
                list += (i.ToString() + ",");
            }
            return list;
        }
        public static string SetupAnimationSegment(WholeAnimationData animation, bool isFidget = false)
        {
            string animnamesinsert = "";
            foreach (DirectionalAnimationData dirAn in animation.DirectionalAnimations)
            {
                if (!string.IsNullOrEmpty(dirAn.suffix))
                {
                    animnamesinsert += ("\"" + animation.animShortname + "_" + dirAn.suffix + "\",");
                }
                else
                {
                    animnamesinsert += ("\"" + animation.animShortname + "\",");
                }
            }
            string idleSetup = "";
            if (isFidget)
            {
                idleSetup = $"aiAnimator.IdleFidgetAnimations.Add(new DirectionalAnimation" +
                              "{" +
                              $"Type = DirectionalAnimation.DirectionType.{animation.Directionality}," +
                              $"Flipped = new DirectionalAnimation.FlipType[{(int)animation.flipType}]," +
                              "AnimNames = new string[]{" +
                              $"{animnamesinsert}" + "}});";
            }
            else
            {
                idleSetup = $"aiAnimator.{animation.animName} = new DirectionalAnimation" +
                               "{" +
                               $"Type = DirectionalAnimation.DirectionType.{animation.Directionality}," +
                               $"Flipped = new DirectionalAnimation.FlipType[{(int)animation.flipType}]," +
                               "AnimNames = new string[]{" +
                               $"{animnamesinsert}" + "}};";
            }

            return idleSetup;
        }
        public static string SetupOtherAnimationSegments(List<WholeAnimationData> animationList)
        {
            string allOtherAnims = "aiAnimator.OtherAnimations = new List<AIAnimator.NamedDirectionalAnimation>{";

            foreach (WholeAnimationData animation in animationList)
            {
                string animNamesInsert = "";
                foreach (DirectionalAnimationData dirAn in animation.DirectionalAnimations)
                {
                    if (!string.IsNullOrEmpty(dirAn.suffix))
                    {
                        animNamesInsert += ("\"" + animation.animShortname + "_" + dirAn.suffix + "\",");
                    }
                    else
                    {
                        animNamesInsert += ("\"" + animation.animShortname + "\",");
                    }
                }

                string OtherDirectionalAnim = "new AIAnimator.NamedDirectionalAnimation{" +
                   $"name = \"{animation.animShortname}\"," +
                   "anim = new DirectionalAnimation" +
                   " {" +
                        $"Prefix = \"{animation.animShortname}\"," +
                        $"Type = DirectionalAnimation.DirectionType.{animation.Directionality}," +
                        $"Flipped = new DirectionalAnimation.FlipType[{(int)animation.flipType}]," +
                        "AnimNames = new string[]" +
                        "{" + $"{animNamesInsert}" + "}" +
                    "}}, ";

                allOtherAnims += OtherDirectionalAnim;
            }
            allOtherAnims += "};";
            return allOtherAnims;
        }
    }

}

